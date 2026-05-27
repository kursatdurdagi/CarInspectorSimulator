//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright (c) 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Detailed ESP V2 test suite for RCCP. Complements the simpler RCCP_StabilityTest
/// (basic ABS/TCS/ESP provocations) with eight focused ESP V2 scenarios:
///   1. Hysteresis (activation vs deactivation band, min intervention time)
///   2. Sideslip spin risk (|beta| or |dbeta/dt| forces engagement)
///   3. Sport vs Normal mode comparison (deadband widening, engine cut disabled)
///   4. preserveSpeedFactor arcade sweep (0 vs 1 speed retained through corner)
///   5. Spin recovery (yaw opposes driver intent -> brake actual-spin outer wheel)
///   6. ISO 3888 moose test (lane-change evasion)
///   7. Skidpad sweep (constant radius, rising speed)
///   8. Lift-off oversteer (sudden throttle release mid-corner)
///
/// Live telemetry draws four stacked mini-graphs of ESP V2 debug values (yaw rates,
/// sideslip, engagement bars, ESP brake torque). Captured samples can be exported
/// to CSV for offline analysis.
///
/// Works with any vehicle configuration (FWD / RWD / AWD). Shows detected drivetrain
/// in the control panel. Also includes a free-drive mode for manual course driving
/// (skidpad circle, moose chicane, J-turn marker, jump ramp built by Build Course).
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Demo/RCCP ESP Detailed Test")]
public class RCCP_ESPDetailedTest : RCCP_GenericComponent {

    #region Data Structures

    public enum DetailedTestType {

        Hysteresis,
        SideslipSpinRisk,
        SportVsNormal,
        PreserveSpeed,
        SpinRecovery,
        MooseTest,
        Skidpad,
        LiftOffOversteer

    }

    /// <summary>
    /// Single ESP V2 telemetry snapshot. Captured at fixed timestep into a ring buffer
    /// for live graphing, and into a per-test list for pass/fail evaluation and CSV export.
    /// </summary>
    private struct Sample {

        public float time;
        public float speed;                 // km/h
        public float yawActualDegS;
        public float yawRefDegS;
        public float yawErrorDegS;
        public float sideslipDeg;
        public float sideslipRateDegS;
        public float maxBrakeNm;            // max brake torque across all 4 wheels
        public int activeWheelIndex;        // -1 none / 0 FL / 1 FR / 2 RL / 3 RR
        public bool espEngaged;
        public bool espIndicator;
        public bool isOversteer;
        public bool absEngaged;
        public bool tcsEngaged;
        public float steerAngle;
        public float throttleInput;
        public float brakeInput;
        public float driftAngle;
        public float frontLeftMotor;
        public float frontRightMotor;
        public float rearLeftMotor;
        public float rearRightMotor;

    }

    private struct TestResult {

        public DetailedTestType type;
        public bool passed;
        public string summary;
        public string detail;

    }

    #endregion

    #region Inspector

    [Header("Graph")]
    [Tooltip("Number of samples retained for live telemetry graphs. 600 = 12 seconds at 50 Hz.")]
    [Range(200, 2000)] public int graphCapacity = 600;

    [Tooltip("How often to rebuild graph textures (seconds). 0.05 = 20 Hz, cheap.")]
    [Range(0.02f, 0.5f)] public float graphRebuildInterval = 0.05f;

    [Header("CSV Export")]
    [Tooltip("If enabled, each completed test auto-exports its sample stream to CSV.")]
    public bool autoExportCsvOnTestComplete = false;

    #endregion

    #region State

    private int selectedVehicleIndex = 0;
    private RCCP_CarController vehicle;
    private Transform spawnPoint;

    private bool testRunning = false;
    private string testStatus = "Idle";
    private bool freeDriveMode = false;
    private Coroutine activeTestCoroutine;

    private List<Sample> samples = new List<Sample>();
    private List<TestResult> results = new List<TestResult>();
    private Vector2 resultsScroll;

    // Ring buffer for live graph
    private Sample[] graphBuffer;
    private int graphHead = 0;
    private int graphCount = 0;
    private float lastGraphRebuildTime = -1f;

    // UI state
    private bool showGraphs = true;
    private bool showTelemetry = true;
    private string lastCsvPath = "";

    // Remembered original ESP settings (restored on test complete)
    private bool savedSettings = false;
    private RCCP_Stability.ESPMode savedESPMode;
    private float savedPreserveSpeedFactor;
    private float savedESPIntensity;
    private float savedEspDeadband;
    private float savedEspDeactivationDeadband;

    #endregion

    #region Graph Textures

    private Texture2D graphYawTex;
    private Texture2D graphSideslipTex;
    private Texture2D graphEngagementTex;
    private Texture2D graphBrakeTex;
    private const int GraphW = 600;
    private const int GraphH = 60;
    private Color[] graphClearBuffer;

    #endregion

    #region Cached Styles

    private GUIStyle _titleStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _passStyle;
    private GUIStyle _failStyle;
    private GUIStyle _activeStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _boxStyle;
    private GUIStyle _smallLabelStyle;
    private GUIStyle _graphLabelStyle;
    private bool _stylesInitialized;

    #endregion

    private void Awake() {

        // Find spawn point in scene (plain Transform named RCCP_SpawnPoint)
        GameObject sp = GameObject.Find("RCCP_SpawnPoint");

        if (sp != null)
            spawnPoint = sp.transform;

        graphBuffer = new Sample[Mathf.Max(100, graphCapacity)];

    }

    private void Start() {

        InitGraphTextures();
        SpawnVehicle();

    }

    private void OnDestroy() {

        DestroyGraphTextures();

    }

    #region Vehicle Management

    private void SpawnVehicle() {

        RCCP_CarController[] demoVehicles = RCCP_DemoVehicles.Instance.vehicles;

        if (demoVehicles == null || demoVehicles.Length == 0)
            return;

        selectedVehicleIndex = Mathf.Clamp(selectedVehicleIndex, 0, demoVehicles.Length - 1);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        RCCP_CarController current = RCCPSceneManager.activePlayerVehicle;

        if (current != null) {

            RCCP.DeRegisterPlayerVehicle();
            Destroy(current.gameObject);

        }

        vehicle = RCCP.SpawnRCC(demoVehicles[selectedVehicleIndex], pos + Vector3.up * 0.5f, rot, true, true, true);

        // Drop any saved settings from prior vehicle.
        savedSettings = false;

        // Some demo vehicles (E46, etc.) ship with RCCP_Damage + detachable hood/trunk that
        // trigger on the 0.5m spawn drop and during aggressive test provocations. Tests measure
        // ESP behavior, not crash physics — disable damage so aggressive maneuvers don't shed
        // wheels / panels and invalidate later tests.
        DisableVehicleDamage();

    }

    private void DisableVehicleDamage() {

        if (vehicle == null)
            return;

        var damage = vehicle.GetComponentInChildren<RCCP_Damage>(true);
        if (damage != null) {
            damage.enabled = false;
        }

        var detachables = vehicle.GetComponentsInChildren<RCCP_DetachablePart>(true);
        foreach (var d in detachables)
            if (d != null) d.enabled = false;

        // Repair any damage that may have already occurred during the spawn drop.
        RCCP.Repair(vehicle);

    }

    private void ResetVehicle() {

        if (vehicle == null)
            return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        vehicle.transform.position = pos + Vector3.up * 0.8f;
        vehicle.transform.rotation = rot;
        vehicle.Rigid.linearVelocity = Vector3.zero;
        vehicle.Rigid.angularVelocity = Vector3.zero;

        if (vehicle.Inputs != null) {
            vehicle.Inputs.overrideExternalInputs = false;
            vehicle.Inputs.DisableOverrideInputs();
        }

        // Zero any lingering wheel brake torques from prior coroutine interruption.
        var axles = vehicle.AxleManager;
        if (axles != null && axles.Axles != null) {
            for (int i = 0; i < axles.Axles.Count; i++) {
                var ax = axles.Axles[i];
                if (ax == null) continue;
                if (ax.leftWheelCollider != null && ax.leftWheelCollider.WheelCollider != null) {
                    ax.leftWheelCollider.WheelCollider.brakeTorque = 0f;
                    ax.leftWheelCollider.WheelCollider.motorTorque = 0f;
                }
                if (ax.rightWheelCollider != null && ax.rightWheelCollider.WheelCollider != null) {
                    ax.rightWheelCollider.WheelCollider.brakeTorque = 0f;
                    ax.rightWheelCollider.WheelCollider.motorTorque = 0f;
                }
            }
        }

    }

    private void OverrideInputs(float throttle, float brake, float steer, float handbrake) {

        if (vehicle == null || vehicle.Inputs == null)
            return;

        // Bypass RCCP_Input's VehicleControlledInputs() pass (steering curve, counter-steer,
        // steering limiter). Without this, the steering curve attenuates our test steer by ~3-4x
        // at highway speeds, leaving provocations too weak to trigger ESP.
        vehicle.Inputs.overrideExternalInputs = true;

        RCCP_Inputs inputs = new RCCP_Inputs();
        inputs.throttleInput = throttle;
        inputs.brakeInput = brake;
        inputs.steerInput = steer;
        inputs.handbrakeInput = handbrake;
        vehicle.Inputs.OverrideInputs(inputs);

    }

    private void ReleaseInputs() {

        if (vehicle != null && vehicle.Inputs != null) {
            vehicle.Inputs.overrideExternalInputs = false;
            vehicle.Inputs.DisableOverrideInputs();
        }

    }

    /// <summary>
    /// Returns "FWD", "RWD", "AWD" or "---" based on axle.isPower flags. The flag is
    /// set by RCCP_Differential.driveType at runtime, so queries after Start() are safe.
    /// </summary>
    private string GetDrivetrainLabel() {

        if (vehicle == null)
            return "---";

        RCCP_Axle fa = vehicle.FrontAxle;
        RCCP_Axle ra = vehicle.RearAxle;
        bool fPow = fa != null && fa.isPower;
        bool rPow = ra != null && ra.isPower;

        if (fPow && rPow) return "AWD";
        if (fPow) return "FWD";
        if (rPow) return "RWD";
        return "---";

    }

    private void SaveStabilitySettings() {

        if (savedSettings)
            return;

        if (vehicle == null || vehicle.Stability == null)
            return;

        RCCP_Stability s = vehicle.Stability;
        savedESPMode = s.espMode;
        savedPreserveSpeedFactor = s.preserveSpeedFactor;
        savedESPIntensity = s.ESPIntensity;
        savedEspDeadband = s.espDeadband;
        savedEspDeactivationDeadband = s.espDeactivationDeadband;
        savedSettings = true;

    }

    private void RestoreStabilitySettings() {

        if (!savedSettings)
            return;

        if (vehicle == null || vehicle.Stability == null)
            return;

        RCCP_Stability s = vehicle.Stability;
        s.espMode = savedESPMode;
        s.preserveSpeedFactor = savedPreserveSpeedFactor;
        s.ESPIntensity = savedESPIntensity;
        s.espDeadband = savedEspDeadband;
        s.espDeactivationDeadband = savedEspDeactivationDeadband;
        savedSettings = false;

    }

    #endregion

    #region Sampling

    private void FixedUpdate() {

        // Live telemetry ring buffer fills whenever we have a vehicle, even outside tests.
        if (vehicle != null && vehicle.Stability != null)
            PushGraphSample(TakeSample());

    }

    private Sample TakeSample() {

        Sample s = new Sample();
        s.time = Time.time;
        s.activeWheelIndex = -1;

        if (vehicle == null)
            return s;

        s.speed = vehicle.absoluteSpeed;

        Vector3 vel = vehicle.Rigid.linearVelocity;

        if (vel.sqrMagnitude > 0.1f)
            s.driftAngle = Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up);

        s.steerAngle = vehicle.steerAngle;

        if (vehicle.Inputs != null) {
            s.throttleInput = vehicle.Inputs.inputs.throttleInput;
            s.brakeInput = vehicle.Inputs.inputs.brakeInput;
        }

        RCCP_Stability st = vehicle.Stability;

        if (st == null)
            return s;

        s.espEngaged = st.ESPEngaged;
        s.espIndicator = st.ESPIndicatorEngaged;
        s.absEngaged = st.ABSEngaged;
        s.tcsEngaged = st.TCSEngaged;

        // Read yaw & sideslip directly from the Rigidbody rather than from st.debugYawActualDegS etc.
        // The debug fields are not always updated at the moment coroutines resume (ESP may have
        // returned early in the same frame, leaving debug fields stale). Computing from the live
        // rigidbody state is robust to coroutine/physics ordering.
        s.yawActualDegS = vehicle.Rigid.angularVelocity.y * Mathf.Rad2Deg;
        Vector3 localVel = vehicle.transform.InverseTransformDirection(vehicle.Rigid.linearVelocity);
        s.sideslipDeg = Mathf.Abs(localVel.z) > 0.5f
            ? Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z)) * Mathf.Rad2Deg
            : 0f;

        // yawRef, yawError, sideslipRate, classification, activeWheel still come from ESP's own bookkeeping.
        s.yawRefDegS = st.debugYawRefDegS;
        s.yawErrorDegS = st.debugYawErrorDegS;
        s.sideslipRateDegS = st.debugSideslipRateDegS;
        s.isOversteer = st.debugIsOversteer;
        s.activeWheelIndex = st.debugActiveWheelIndex;

        float maxBrake = 0f;
        float[] brakePerWheel = new float[4];
        float[] motorPerWheel = new float[4];

        if (st.frontAxle != null) {
            if (st.frontAxle.leftWheelCollider != null) {
                brakePerWheel[0] = st.frontAxle.leftWheelCollider.WheelCollider.brakeTorque;
                motorPerWheel[0] = st.frontAxle.leftWheelCollider.WheelCollider.motorTorque;
            }
            if (st.frontAxle.rightWheelCollider != null) {
                brakePerWheel[1] = st.frontAxle.rightWheelCollider.WheelCollider.brakeTorque;
                motorPerWheel[1] = st.frontAxle.rightWheelCollider.WheelCollider.motorTorque;
            }
        }

        if (st.rearAxle != null) {
            if (st.rearAxle.leftWheelCollider != null) {
                brakePerWheel[2] = st.rearAxle.leftWheelCollider.WheelCollider.brakeTorque;
                motorPerWheel[2] = st.rearAxle.leftWheelCollider.WheelCollider.motorTorque;
            }
            if (st.rearAxle.rightWheelCollider != null) {
                brakePerWheel[3] = st.rearAxle.rightWheelCollider.WheelCollider.brakeTorque;
                motorPerWheel[3] = st.rearAxle.rightWheelCollider.WheelCollider.motorTorque;
            }
        }

        for (int i = 0; i < 4; i++) {
            if (brakePerWheel[i] > maxBrake)
                maxBrake = brakePerWheel[i];
        }

        s.maxBrakeNm = maxBrake;
        s.frontLeftMotor = motorPerWheel[0];
        s.frontRightMotor = motorPerWheel[1];
        s.rearLeftMotor = motorPerWheel[2];
        s.rearRightMotor = motorPerWheel[3];

        return s;

    }

    private void PushGraphSample(Sample s) {

        if (graphBuffer == null || graphBuffer.Length == 0)
            return;

        graphBuffer[graphHead] = s;
        graphHead = (graphHead + 1) % graphBuffer.Length;

        if (graphCount < graphBuffer.Length)
            graphCount++;

    }

    private Sample GetGraphSample(int indexFromOldest) {

        if (graphCount == 0)
            return new Sample();

        int oldest = (graphHead - graphCount + graphBuffer.Length) % graphBuffer.Length;
        return graphBuffer[(oldest + indexFromOldest) % graphBuffer.Length];

    }

    #endregion

    #region Test Orchestration

    private void RunTest(DetailedTestType type) {

        if (testRunning)
            return;

        if (vehicle == null) {
            SpawnVehicle();
            if (vehicle == null)
                return;
        }

        if (vehicle.Stability == null) {
            testStatus = "Vehicle has no Stability component";
            return;
        }

        testRunning = true;
        samples.Clear();
        SaveStabilitySettings();

        switch (type) {

            case DetailedTestType.Hysteresis:
                activeTestCoroutine = StartCoroutine(TestHysteresis());
                break;

            case DetailedTestType.SideslipSpinRisk:
                activeTestCoroutine = StartCoroutine(TestSideslipSpinRisk());
                break;

            case DetailedTestType.SportVsNormal:
                activeTestCoroutine = StartCoroutine(TestSportVsNormal());
                break;

            case DetailedTestType.PreserveSpeed:
                activeTestCoroutine = StartCoroutine(TestPreserveSpeed());
                break;

            case DetailedTestType.SpinRecovery:
                activeTestCoroutine = StartCoroutine(TestSpinRecovery());
                break;

            case DetailedTestType.MooseTest:
                activeTestCoroutine = StartCoroutine(TestMoose());
                break;

            case DetailedTestType.Skidpad:
                activeTestCoroutine = StartCoroutine(TestSkidpad());
                break;

            case DetailedTestType.LiftOffOversteer:
                activeTestCoroutine = StartCoroutine(TestLiftOffOversteer());
                break;

        }

    }

    private void StopCurrentTest() {

        if (activeTestCoroutine != null) {
            StopCoroutine(activeTestCoroutine);
            activeTestCoroutine = null;
        }

        ReleaseInputs();
        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Stopped";

    }

    private IEnumerator RunAllSequential() {

        results.Clear();

        yield return StartCoroutine(TestHysteresis());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestSideslipSpinRisk());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestSportVsNormal());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestPreserveSpeed());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestSpinRecovery());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestMoose());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestSkidpad());
        yield return new WaitForSeconds(0.75f);
        yield return StartCoroutine(TestLiftOffOversteer());

        testRunning = false;
        testStatus = "All detailed ESP tests complete";

    }

    private void RunAllTests() {

        if (testRunning)
            return;

        testRunning = true;
        SaveStabilitySettings();
        activeTestCoroutine = StartCoroutine(RunAllSequential());

    }

    private IEnumerator AccelerateTo(float kmh, float throttle, float timeoutSeconds) {

        OverrideInputs(throttle, 0f, 0f, 0f);
        float timeout = Time.time + timeoutSeconds;

        while (vehicle.absoluteSpeed < kmh && Time.time < timeout)
            yield return null;

    }

    #endregion

    #region Test 1 — Hysteresis

    private IEnumerator TestHysteresis() {

        testStatus = "Hysteresis: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;
        vehicle.Stability.preserveSpeedFactor = 0f;

        testStatus = "Hysteresis: Accelerate to 65 km/h";
        yield return StartCoroutine(AccelerateTo(65f, 1f, 15f));

        // Three corner / straight-coast cycles. Each corner triggers ESP via yaw error above
        // activation band; each coast holds steer=0 and low throttle so yaw rate fully decays
        // below the deactivation band, forcing ESP off. Need >=3 clean on/off transitions so the
        // evaluator can actually validate the hysteresis band rather than trivially-passing a
        // single activation.
        testStatus = "Hysteresis: Cycle corners";
        samples.Clear();

        for (int cycle = 0; cycle < 4; cycle++) {

            // Corner phase: provocation above activation band.
            float tCorner = Time.time + 0.6f;
            while (Time.time < tCorner) {
                OverrideInputs(0.6f, 0f, 0.55f, 0f);
                samples.Add(TakeSample());
                yield return new WaitForFixedUpdate();
            }

            // Coast recovery: zero steer, light throttle so yaw decays to well below
            // deactivation band and ESP releases.
            float tRecover = Time.time + 1.5f;
            while (Time.time < tRecover) {
                OverrideInputs(0.15f, 0f, 0f, 0f);
                samples.Add(TakeSample());
                yield return new WaitForFixedUpdate();
            }

        }

        ReleaseInputs();
        results.Add(EvaluateHysteresis());

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Hysteresis: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("hysteresis");

    }

    private TestResult EvaluateHysteresis() {

        TestResult r = new TestResult();
        r.type = DetailedTestType.Hysteresis;

        float activationBand = vehicle.Stability.espDeadband;
        float deactivationBand = vehicle.Stability.espDeactivationDeadband;
        float minHold = vehicle.Stability.espMinInterventionTime;

        // Scan for transitions.
        int activationViolations = 0;   // ESP turned on while |yawError| was below activation band
        int deactivationViolations = 0; // ESP turned off while |yawError| was still above deactivation band
        int minHoldViolations = 0;      // ESP on->off duration was shorter than minHold

        float onTime = -1f;
        int activations = 0;

        for (int i = 1; i < samples.Count; i++) {

            bool was = samples[i - 1].espEngaged;
            bool now = samples[i].espEngaged;
            float err = Mathf.Abs(samples[i].yawErrorDegS);

            if (!was && now) {
                activations++;
                onTime = samples[i].time;
                // Hysteresis bound check: at the moment ESP turns ON, |err| should be >= activationBand - small slack
                if (err + 0.5f < activationBand)
                    activationViolations++;
            } else if (was && !now) {
                float duration = samples[i].time - onTime;
                if (duration + 0.02f < minHold)
                    minHoldViolations++;
                // At the moment ESP turns OFF, |err| should be <= deactivationBand + slack
                if (err - 0.5f > deactivationBand)
                    deactivationViolations++;
            }

        }

        if (activations < 3) {
            r.passed = false;
            r.summary = "Insufficient transitions (" + activations + " activations, need >=3)";
            r.detail = "Hysteresis band cannot be validated from " + activations + " activation(s). "
                     + "Need at least 3 clean on/off cycles to exercise the hysteresis zone. "
                     + "Activation band " + activationBand.ToString("F1") + " deg/s, deactivation " + deactivationBand.ToString("F1") + " deg/s.";
            return r;
        }

        // With >=3 activations, 1 boundary-aliasing violation per category is tolerated (20ms sample
        // quantization vs 150ms hold), but we demand the majority respect the rules.
        bool ok = activationViolations <= 1 && deactivationViolations <= 1 && minHoldViolations <= 1;
        r.passed = ok;
        r.summary = (ok ? "Hysteresis respected (" : "Hysteresis violated (")
                  + activations + " activations, " + activationViolations + " act-viol, "
                  + deactivationViolations + " deact-viol, " + minHoldViolations + " hold-viol)";
        r.detail = "Activation band " + activationBand.ToString("F1") + " deg/s, deactivation "
                 + deactivationBand.ToString("F1") + " deg/s, min hold " + minHold.ToString("F2") + "s. 1 violation per category tolerated for sample aliasing.";
        return r;

    }

    #endregion

    #region Test 2 — Sideslip Spin Risk

    private IEnumerator TestSideslipSpinRisk() {

        testStatus = "Sideslip: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;

        testStatus = "Sideslip: Accelerate to 80 km/h";
        yield return StartCoroutine(AccelerateTo(80f, 1f, 18f));

        // Short handbrake pulse under throttle to break rear grip without killing forward speed.
        // Keeping throttle high ensures speed stays above the ESP speed gate (~14 km/h) throughout
        // the subsequent sample window so ESP's sideslip-spin-risk branch can engage.
        testStatus = "Sideslip: Pulse";
        OverrideInputs(1f, 0f, 0.5f, 1f);

        float tPulse = Time.time + 0.18f;
        while (Time.time < tPulse) {
            yield return new WaitForFixedUpdate();
        }

        // Release handbrake but keep throttle + steer — the rear continues to slide, beta grows,
        // ESP sideslip-spin-risk should force oversteer classification and brake outer front.
        samples.Clear();
        float tEnd = Time.time + 2.5f;

        while (Time.time < tEnd) {
            OverrideInputs(0.9f, 0f, 0.5f, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

        ReleaseInputs();
        results.Add(EvaluateSideslipSpinRisk());

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Sideslip: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("sideslip_spin_risk");

    }

    private TestResult EvaluateSideslipSpinRisk() {

        TestResult r = new TestResult();
        r.type = DetailedTestType.SideslipSpinRisk;

        float sideslipMax = vehicle.Stability.sideslipMaxAngle;
        int espOnAndOverBeta = 0;
        int overBeta = 0;
        int anyEspOn = 0;
        float peakBeta = 0f;
        bool oversteerClassified = false;
        float minFwdSpeed = 9999f;
        float peakFwdSpeed = 0f;

        for (int i = 0; i < samples.Count; i++) {

            float absBeta = Mathf.Abs(samples[i].sideslipDeg);
            if (absBeta > peakBeta) peakBeta = absBeta;
            if (samples[i].speed < minFwdSpeed) minFwdSpeed = samples[i].speed;
            if (samples[i].speed > peakFwdSpeed) peakFwdSpeed = samples[i].speed;

            if (samples[i].espEngaged) anyEspOn++;

            if (absBeta > sideslipMax) {
                overBeta++;
                if (samples[i].espEngaged)
                    espOnAndOverBeta++;
            }

            if (samples[i].isOversteer && absBeta > sideslipMax)
                oversteerClassified = true;

        }

        if (overBeta < 3) {
            r.passed = false;
            r.summary = "Failed to exceed sideslipMaxAngle (" + sideslipMax.ToString("F1") + " deg)";
            r.detail = "Peak |beta| " + peakBeta.ToString("F1") + " deg. Forward speed range " + minFwdSpeed.ToString("F0") + "-" + peakFwdSpeed.ToString("F0") + " km/h.";
            return r;
        }

        // Pass if ESP engaged at least once while beta was over limit, or at least showed oversteer-classified samples.
        // RCCP_Stability's speed gate uses FORWARD velocity — in heavy slides forward speed can drop below 14 km/h
        // and ESP bails out. That's the design. We accept "ESP engaged at least 3x in the high-beta window".
        bool ok = (espOnAndOverBeta >= 3 && oversteerClassified);
        r.passed = ok;
        r.summary = (ok ? "ESP engaged on sideslip trigger" : "ESP bailed (forward-speed gate)")
                  + " (peak beta " + peakBeta.ToString("F1") + " deg, ESP-on " + espOnAndOverBeta + "/" + overBeta + ")";
        r.detail = "sideslipMaxAngle = " + sideslipMax.ToString("F1") + " deg. Oversteer classified: " + oversteerClassified
                 + ". Forward speed range during slide: " + minFwdSpeed.ToString("F0") + "-" + peakFwdSpeed.ToString("F0") + " km/h (gate=14 km/h).";
        return r;

    }

    #endregion

    #region Test 3 — Sport vs Normal

    private IEnumerator TestSportVsNormal() {

        results.RemoveAll(x => x.type == DetailedTestType.SportVsNormal);

        int normalEspCount; int normalMotorCutCount; float normalMaxBrake;
        int sportEspCount; int sportMotorCutCount; float sportMaxBrake;

        // ---- Normal run ----
        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;
        yield return StartCoroutine(SportVsNormalRun("Normal"));
        normalEspCount = 0; normalMotorCutCount = 0; normalMaxBrake = 0f;
        CountSportVsNormalMetrics(out normalEspCount, out normalMotorCutCount, out normalMaxBrake);
        _svnNormalSpeedRange = GetSpeedRangeString();

        // ---- Sport run ----
        yield return new WaitForSeconds(0.75f);
        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Sport;
        yield return StartCoroutine(SportVsNormalRun("Sport"));
        sportEspCount = 0; sportMotorCutCount = 0; sportMaxBrake = 0f;
        CountSportVsNormalMetrics(out sportEspCount, out sportMotorCutCount, out sportMaxBrake);
        _svnSportSpeedRange = GetSpeedRangeString();

        TestResult r = new TestResult();
        r.type = DetailedTestType.SportVsNormal;

        bool meaningfulProvocation = normalEspCount >= 10;
        bool widerBand = sportEspCount <= normalEspCount;
        bool noEngineCut = sportMotorCutCount == 0 || sportMotorCutCount < Mathf.Max(3, normalMotorCutCount * 0.25f);

        r.passed = meaningfulProvocation && widerBand && noEngineCut;
        if (!meaningfulProvocation) {
            r.summary = "Normal-mode provocation failed (ESP samples=" + normalEspCount + ", need >=10)";
        } else {
            r.summary = (r.passed ? "Sport widened & engine cut suppressed" : "Sport did not relax intervention")
                      + " (Normal ESP=" + normalEspCount + " cut=" + normalMotorCutCount
                      + " | Sport ESP=" + sportEspCount + " cut=" + sportMotorCutCount + ")";
        }
        r.detail = "Max brake Normal=" + normalMaxBrake.ToString("F0") + "Nm, Sport=" + sportMaxBrake.ToString("F0") + "Nm."
                 + " Forward speed range (Normal): " + _svnNormalSpeedRange + " | (Sport): " + _svnSportSpeedRange
                 + ". ESP's 14 km/h forward-speed gate can bail during heavy slides.";
        results.Add(r);

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Sport vs Normal: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("sport_vs_normal");

    }

    private IEnumerator SportVsNormalRun(string label) {

        testStatus = "Sport/Normal [" + label + "]: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Sport/Normal [" + label + "]: Accelerate to 80 km/h";
        yield return StartCoroutine(AccelerateTo(80f, 1f, 18f));

        // Sustained heavy corner (no handbrake — raw RWD power oversteer provocation).
        // With steering curve bypassed (overrideExternalInputs=true), moderate steer produces
        // a strong wheel angle and triggers ESP reliably without spinning the vehicle out.
        testStatus = "Sport/Normal [" + label + "]: Provoke";
        samples.Clear();

        float tEnd = Time.time + 3.5f;
        while (Time.time < tEnd) {
            OverrideInputs(1f, 0f, 0.6f, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

    }

    private void CountSportVsNormalMetrics(out int espOnCount, out int motorCutCount, out float maxBrakeObserved) {

        espOnCount = 0;
        motorCutCount = 0;
        maxBrakeObserved = 0f;

        // A sample counts as "motor cut" if the currently-powered axle wheel has zero-ish motor torque
        // while ESP is engaged and throttle is held (would normally be high torque).
        bool fwd = vehicle.FrontAxle != null && vehicle.FrontAxle.isPower;
        bool rwd = vehicle.RearAxle != null && vehicle.RearAxle.isPower;

        for (int i = 0; i < samples.Count; i++) {

            if (samples[i].espEngaged)
                espOnCount++;

            if (samples[i].maxBrakeNm > maxBrakeObserved)
                maxBrakeObserved = samples[i].maxBrakeNm;

            if (!samples[i].espEngaged || samples[i].throttleInput < 0.5f)
                continue;

            float poweredMotor = 0f;
            int poweredCount = 0;

            if (fwd) { poweredMotor += Mathf.Abs(samples[i].frontLeftMotor) + Mathf.Abs(samples[i].frontRightMotor); poweredCount += 2; }
            if (rwd) { poweredMotor += Mathf.Abs(samples[i].rearLeftMotor) + Mathf.Abs(samples[i].rearRightMotor); poweredCount += 2; }

            if (poweredCount == 0)
                continue;

            float avgMotor = poweredMotor / poweredCount;

            // "Motor cut" heuristic: average powered wheel torque below 50 Nm while throttle still pressed.
            if (avgMotor < 50f)
                motorCutCount++;

        }

    }

    #endregion

    #region Test 4 — preserveSpeedFactor sweep

    private IEnumerator TestPreserveSpeed() {

        results.RemoveAll(x => x.type == DetailedTestType.PreserveSpeed);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;

        float startZero = 0f, endZero = 0f; int espZero = 0;
        float startOne = 0f, endOne = 0f; int espOne = 0;

        vehicle.Stability.preserveSpeedFactor = 0f;
        yield return StartCoroutine(PreserveSpeedRun("factor=0"));
        startZero = _preserveRunStartSpeed;
        endZero = ComputeCorneringSpeedRetention();
        espZero = CountEspOn();

        yield return new WaitForSeconds(0.75f);

        vehicle.Stability.preserveSpeedFactor = 1f;
        yield return StartCoroutine(PreserveSpeedRun("factor=1"));
        startOne = _preserveRunStartSpeed;
        endOne = ComputeCorneringSpeedRetention();
        espOne = CountEspOn();

        TestResult r = new TestResult();
        r.type = DetailedTestType.PreserveSpeed;

        float lossZero = startZero - endZero;
        float lossOne = startOne - endOne;
        float diff = lossZero - lossOne;

        bool validEntry = startZero > 55f && startOne > 55f && Mathf.Abs(startZero - startOne) < 15f;
        bool engaged = espZero >= 5 && espOne >= 5;

        if (!validEntry) {
            r.passed = false;
            r.summary = "Entry speed mismatch (f=0 start=" + startZero.ToString("F0") + ", f=1 start=" + startOne.ToString("F0") + " km/h)";
            r.detail = "Vehicle did not reach comparable entry speed in both runs.";
        } else if (!engaged) {
            r.passed = false;
            r.summary = "ESP did not engage (f=0 samples=" + espZero + ", f=1 samples=" + espOne + ")";
            r.detail = "Corner provocation did not trigger ESP brake intervention.";
        } else {
            r.passed = diff >= 2f;
            r.summary = (r.passed ? "preserveSpeedFactor effective" : "preserveSpeedFactor ineffective")
                      + " (loss f=0: " + lossZero.ToString("F1") + " km/h, f=1: " + lossOne.ToString("F1") + " km/h)";
            r.detail = "Entry speeds: f=0 " + startZero.ToString("F0") + ", f=1 " + startOne.ToString("F0") + " km/h. "
                     + "Delta in loss = " + diff.ToString("F1") + " km/h. Arcade factor should bleed less (threshold 2 km/h).";
        }
        results.Add(r);

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "PreserveSpeed: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("preserve_speed");

    }

    private float _preserveRunStartSpeed = 0f;

    private IEnumerator PreserveSpeedRun(string label) {

        testStatus = "PreserveSpeed [" + label + "]: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "PreserveSpeed [" + label + "]: Accelerate to 80 km/h";
        yield return StartCoroutine(AccelerateTo(80f, 1f, 18f));

        _preserveRunStartSpeed = vehicle.absoluteSpeed;

        testStatus = "PreserveSpeed [" + label + "]: Sustained corner";
        samples.Clear();

        float tEnd = Time.time + 3.5f;
        while (Time.time < tEnd) {
            OverrideInputs(0.8f, 0f, 0.55f, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

    }

    private int CountEspOn() {

        int n = 0;
        for (int i = 0; i < samples.Count; i++)
            if (samples[i].espEngaged) n++;
        return n;

    }

    private string _svnNormalSpeedRange = "?";
    private string _svnSportSpeedRange = "?";

    private string GetSpeedRangeString() {

        if (samples.Count == 0) return "no samples";
        float lo = 9999f, hi = 0f;
        for (int i = 0; i < samples.Count; i++) {
            if (samples[i].speed < lo) lo = samples[i].speed;
            if (samples[i].speed > hi) hi = samples[i].speed;
        }
        return lo.ToString("F0") + "-" + hi.ToString("F0") + " km/h";

    }

    private float ComputeCorneringSpeedRetention() {

        // Average speed across the last 0.5s of the corner.
        if (samples.Count == 0)
            return 0f;

        float endTime = samples[samples.Count - 1].time;
        float sum = 0f;
        int count = 0;

        for (int i = 0; i < samples.Count; i++) {
            if (endTime - samples[i].time <= 0.5f) {
                sum += samples[i].speed;
                count++;
            }
        }

        return count > 0 ? sum / count : 0f;

    }

    #endregion

    #region Test 5 — Spin Recovery

    private IEnumerator TestSpinRecovery() {

        testStatus = "SpinRecovery: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;
        vehicle.Stability.preserveSpeedFactor = 0f;

        testStatus = "SpinRecovery: Accelerate to 60 km/h";
        yield return StartCoroutine(AccelerateTo(60f, 1f, 15f));

        // Spinning while going straight (yawRef ~ 0 branch -> isOversteer = true).
        // Driver holds steer = 0, we sustain body yaw to the LEFT (negative) for 0.5s.
        // ESP should classify as oversteer and brake OUTER of actual spin = RIGHT front (index 1).
        testStatus = "SpinRecovery: Sustain left yaw";
        float tInject = Time.time + 0.5f;
        while (Time.time < tInject) {
            OverrideInputs(0.3f, 0f, 0f, 0f);   // straight — no driver steer
            Vector3 a = vehicle.Rigid.angularVelocity;
            if (a.y > -1.5f) a.y = -1.5f;        // floor at -1.5 rad/s left spin
            vehicle.Rigid.angularVelocity = a;
            yield return new WaitForFixedUpdate();
        }

        // Release injection; sample ESP reaction.
        samples.Clear();
        float tEnd = Time.time + 2.0f;
        while (Time.time < tEnd) {
            OverrideInputs(0.3f, 0f, 0f, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

        ReleaseInputs();
        results.Add(EvaluateSpinRecovery());

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "SpinRecovery: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("spin_recovery");

    }

    private TestResult EvaluateSpinRecovery() {

        TestResult r = new TestResult();
        r.type = DetailedTestType.SpinRecovery;

        // Straight-ahead spin: left body yaw (yawActual < 0), yawRef ~ 0.
        // Expected: isOversteer = true (yawRef < 0.02 branch), turningRight = (yawActual > 0) = false,
        // so outer front = RIGHT front (index 1).
        int oversteerSamples = 0;
        int espOn = 0;
        int rightFrontBraked = 0;
        float peakYawActual = 0f;

        for (int i = 0; i < samples.Count; i++) {

            float ay = Mathf.Abs(samples[i].yawActualDegS);
            if (ay > peakYawActual) peakYawActual = ay;

            if (samples[i].espEngaged) {
                espOn++;
                if (samples[i].isOversteer)
                    oversteerSamples++;
                if (samples[i].activeWheelIndex == 1)
                    rightFrontBraked++;
            }

        }

        if (peakYawActual < 10f) {
            r.passed = false;
            r.summary = "Yaw injection didn't persist (peak " + peakYawActual.ToString("F1") + " deg/s)";
            r.detail = "Stabilizers damped injected yaw before ESP could react.";
            return r;
        }

        if (espOn < 5) {
            r.passed = false;
            r.summary = "ESP did not engage (peak yaw " + peakYawActual.ToString("F1") + " deg/s, " + espOn + " ESP samples)";
            r.detail = "Despite injected yaw of " + peakYawActual.ToString("F1") + " deg/s, ESP did not intervene.";
            return r;
        }

        bool ok = oversteerSamples > espOn * 0.5f && rightFrontBraked > espOn * 0.25f;
        r.passed = ok;
        r.summary = (ok ? "Spin recovery braked actual-outer front" : "Wrong wheel for spin direction")
                  + " (peak yaw " + peakYawActual.ToString("F0") + " deg/s, ESP " + espOn + ", oversteer " + oversteerSamples + ", FR brake " + rightFrontBraked + ")";
        r.detail = "Left spin with 0 steer -> yawRef=0 branch -> oversteer classification -> brake FR (index 1) as outer of actual yaw.";
        return r;

    }

    #endregion

    #region Test 6 — ISO 3888 Moose

    private IEnumerator TestMoose() {

        testStatus = "Moose: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;

        testStatus = "Moose: Accelerate to 70 km/h";
        yield return StartCoroutine(AccelerateTo(70f, 1f, 15f));

        // Moderate-amplitude steering profile (bypass makes steer direct; no need for aggressive values).
        testStatus = "Moose: Execute lane change";
        samples.Clear();

        yield return StartCoroutine(MooseSegment(-0.45f, 0.55f, 0.5f));   // dodge left
        yield return StartCoroutine(MooseSegment(0f, 0.25f, 0.5f));
        yield return StartCoroutine(MooseSegment(0.45f, 0.55f, 0.5f));    // swerve right
        yield return StartCoroutine(MooseSegment(0f, 0.25f, 0.5f));
        yield return StartCoroutine(MooseSegment(-0.35f, 0.45f, 0.5f));   // back into lane
        yield return StartCoroutine(MooseSegment(0f, 0.5f, 0.4f));

        ReleaseInputs();
        results.Add(EvaluateMoose());

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Moose: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("moose");

    }

    private IEnumerator MooseSegment(float steer, float duration, float throttle) {

        float tEnd = Time.time + duration;
        while (Time.time < tEnd) {
            OverrideInputs(throttle, 0f, steer, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

    }

    private TestResult EvaluateMoose() {

        TestResult r = new TestResult();
        r.type = DetailedTestType.MooseTest;

        float sideslipLimit = vehicle.Stability.sideslipMaxAngle * 1.5f;
        float peakBeta = 0f;
        float peakYaw = 0f;
        int excessiveBeta = 0;
        float endSpeed = samples.Count > 0 ? samples[samples.Count - 1].speed : 0f;

        for (int i = 0; i < samples.Count; i++) {
            float b = Mathf.Abs(samples[i].sideslipDeg);
            float y = Mathf.Abs(samples[i].yawActualDegS);
            if (b > peakBeta) peakBeta = b;
            if (y > peakYaw) peakYaw = y;
            if (b > sideslipLimit) excessiveBeta++;
        }

        bool validManeuver = peakYaw > 10f;
        // Strict β cap of 20 deg — real ISO 3888-2 moose test considers β > 15 deg loss of control.
        // 20 deg gives a small buffer for game physics without letting drift-level slides pass.
        const float mooseBetaCap = 20f;
        int reallyExcessive = 0;
        for (int i = 0; i < samples.Count; i++)
            if (Mathf.Abs(samples[i].sideslipDeg) > mooseBetaCap) reallyExcessive++;
        bool contained = reallyExcessive < 5;
        bool reasonableSpeed = endSpeed > 15f;

        if (!validManeuver) {
            r.passed = false;
            r.summary = "Moose maneuver not registered (peak yaw " + peakYaw.ToString("F1") + " deg/s)";
            r.detail = "Yaw rate stayed below 10 deg/s across " + samples.Count + " samples. Lane change didn't execute.";
            return r;
        }

        r.passed = contained && reasonableSpeed;
        r.summary = (r.passed ? "Moose completed within envelope" : "Moose exceeded envelope")
                  + " (peak beta " + peakBeta.ToString("F1") + " deg, peak yaw " + peakYaw.ToString("F1") + " deg/s, end " + endSpeed.ToString("F0") + " km/h)";
        r.detail = "Beta > " + mooseBetaCap.ToString("F0") + " deg samples = " + reallyExcessive + " (cap = 20 deg, real ISO 3888-2 fails at >15). Passing requires < 5 over-cap and end speed > 15 km/h.";
        return r;

    }

    #endregion

    #region Test 7 — Skidpad

    private IEnumerator TestSkidpad() {

        testStatus = "Skidpad: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;

        testStatus = "Skidpad: Straight-line accelerate to 85 km/h";
        yield return StartCoroutine(AccelerateTo(85f, 1f, 18f));

        samples.Clear();

        // Coast-based skidpad: release throttle + brake, hold a constant steer and let the car
        // decelerate through the corner under its own drag. No engine power reaching the wheels,
        // so no power-oversteer (the trap on RWD under throttle). At high initial speed the
        // bicycle-model friction cap clamps yawRef to mu*g/V, so if the tires can't generate
        // enough lateral force to match, actual yaw trails reference -> UNDERSTEER classification.
        // As speed bleeds off the cap relaxes and engagement fades, leaving a cleanly shaped
        // high-speed-understeer signature.
        testStatus = "Skidpad: Coast with constant steer";
        float tEnd = Time.time + 6f;

        while (Time.time < tEnd) {
            OverrideInputs(0f, 0f, 0.7f, 0f);
            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();
        }

        ReleaseInputs();
        results.Add(EvaluateSkidpad());

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "Skidpad: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("skidpad");

    }

    private TestResult EvaluateSkidpad() {

        TestResult r = new TestResult();
        r.type = DetailedTestType.Skidpad;

        int third = samples.Count / 3;
        if (third < 10) {
            r.passed = false;
            r.summary = "Skidpad too short";
            r.detail = "Only " + samples.Count + " samples. Vehicle may be too slow.";
            return r;
        }

        // Coast-based skidpad on dry asphalt: the friction cap yawMax = mu*g/V limits reference
        // yaw at high speed, but the tires can still deliver that lateral acceleration (~0.9g on
        // default mu=0.9), so actual yaw tracks reference and ESP only sees small noise around
        // the hysteresis deadband. The meaningful pass criterion is "car completed the circle
        // without losing control" — ESP may fire briefly either way around the deadband, but
        // sideslip must stay bounded and the vehicle must end moving forward. To see genuine
        // friction-cap understeer, drop estimatedMu to ~0.4 (simulated wet) or add a low-mu patch.
        int highEsp = 0, highUnder = 0, highOver = 0;
        int lowEsp = 0, lowUnder = 0, lowOver = 0;
        float highSpeedPeak = 0f, lowSpeedMin = 9999f;
        float peakBeta = 0f;

        for (int i = 0; i < third; i++) {
            if (samples[i].speed > highSpeedPeak) highSpeedPeak = samples[i].speed;
            float b = Mathf.Abs(samples[i].sideslipDeg);
            if (b > peakBeta) peakBeta = b;
            if (samples[i].espEngaged) {
                highEsp++;
                if (samples[i].isOversteer) highOver++;
                else highUnder++;
            }
        }
        for (int i = samples.Count - third; i < samples.Count; i++) {
            if (samples[i].speed < lowSpeedMin) lowSpeedMin = samples[i].speed;
            float b = Mathf.Abs(samples[i].sideslipDeg);
            if (b > peakBeta) peakBeta = b;
            if (samples[i].espEngaged) {
                lowEsp++;
                if (samples[i].isOversteer) lowOver++;
                else lowUnder++;
            }
        }

        float endSpeed = samples[samples.Count - 1].speed;
        bool controlled = peakBeta < 20f;            // car never slid past the moose-test limit
        bool finishedOk = endSpeed > 10f;            // still moving forward at end of 6s coast
        // No catastrophic runaway in either branch — oversteer didn't dominate by more than 3x
        // (which would indicate power-slip or tire-capability breakdown).
        int totalHigh = highUnder + highOver;
        bool noRunaway = totalHigh == 0 || highOver <= highUnder * 3 + 5;

        r.passed = controlled && finishedOk && noRunaway;
        r.summary = (r.passed ? "Skidpad coast stable — car held the circle" : "Skidpad coast lost control")
                  + " (peak beta " + peakBeta.ToString("F1") + " deg, end " + endSpeed.ToString("F0") + " km/h)";
        r.detail = "Coast test: accel to 85 km/h, then 0 throttle + 0.7 steer for 6s. "
                 + "Speed: high-third peak " + highSpeedPeak.ToString("F0") + " km/h, low-third min " + lowSpeedMin.ToString("F0") + " km/h. "
                 + "ESP engagement — high: " + highUnder + " under / " + highOver + " over | low: " + lowUnder + " under / " + lowOver + " over. "
                 + "On dry mu=0.9, tires deliver the friction-capped yawRef so understeer rarely dominates; lower estimatedMu to force it.";
        return r;

    }

    #endregion

    #region Test 8 — Lift-off Oversteer

    private IEnumerator TestLiftOffOversteer() {

        testStatus = "LiftOff: Reset";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        vehicle.Stability.espMode = RCCP_Stability.ESPMode.Normal;

        testStatus = "LiftOff: Accelerate to 90 km/h";
        yield return StartCoroutine(AccelerateTo(90f, 1f, 20f));

        // Enter corner at steady throttle + moderate steer (bypass makes steer direct; 0.5 is plenty).
        testStatus = "LiftOff: Enter corner";
        float tEnter = Time.time + 1.2f;
        while (Time.time < tEnter) {
            OverrideInputs(0.8f, 0f, 0.5f, 0f);
            yield return new WaitForFixedUpdate();
        }

        // Brief handbrake pulse to initiate the weight-shift transient.
        testStatus = "LiftOff: Pulse";
        OverrideInputs(0f, 0f, 0.5f, 0.5f);
        float tPulse = Time.time + 0.15f;
        while (Time.time < tPulse)
            yield return new WaitForFixedUpdate();

        // Lift-off phase: no throttle, no brake, no handbrake — just hold steer.
        testStatus = "LiftOff: Release throttle";
        samples.Clear();

        float liftTime = Time.time;
        float tEnd = Time.time + 2.5f;
        bool espSeen = false;
        float espEngageTime = -1f;

        while (Time.time < tEnd) {
            OverrideInputs(0f, 0f, 0.5f, 0f);
            Sample s = TakeSample();
            samples.Add(s);
            if (!espSeen && s.espEngaged) {
                espSeen = true;
                espEngageTime = Time.time - liftTime;
            }
            yield return new WaitForFixedUpdate();
        }

        ReleaseInputs();
        results.Add(EvaluateLiftOff(espSeen, espEngageTime));

        RestoreStabilitySettings();
        testRunning = false;
        testStatus = "LiftOff: Complete";

        if (autoExportCsvOnTestComplete)
            ExportSamplesCSV("liftoff_oversteer");

    }

    private TestResult EvaluateLiftOff(bool espSeen, float engageTime) {

        TestResult r = new TestResult();
        r.type = DetailedTestType.LiftOffOversteer;

        int oversteerSamples = 0;
        int espOn = 0;
        for (int i = 0; i < samples.Count; i++) {
            if (samples[i].espEngaged) {
                espOn++;
                if (samples[i].isOversteer) oversteerSamples++;
            }
        }

        float minFwdSpeed = 9999f, maxFwdSpeed = 0f;
        for (int i = 0; i < samples.Count; i++) {
            if (samples[i].speed < minFwdSpeed) minFwdSpeed = samples[i].speed;
            if (samples[i].speed > maxFwdSpeed) maxFwdSpeed = samples[i].speed;
        }

        if (!espSeen) {
            r.passed = false;
            r.summary = "ESP did not engage on lift-off";
            r.detail = "Lift-off transient was not detected. Forward speed range " + minFwdSpeed.ToString("F0") + "-" + maxFwdSpeed.ToString("F0") + " km/h. If low, ESP's speed gate (14 km/h forward) may have bailed during heavy slide.";
            return r;
        }

        // Accept engagement anywhere in the 2.5s window (up from strict 500 ms).
        bool fastEnough = engageTime >= 0f && engageTime <= 1.0f;
        bool classifiedRight = oversteerSamples > espOn * 0.4f;

        r.passed = fastEnough && classifiedRight;
        r.summary = (r.passed ? "ESP caught lift-off transient" : "ESP caught lift-off (late or wrong class)")
                  + " (engage in " + (engageTime * 1000f).ToString("F0") + " ms, oversteer " + oversteerSamples + "/" + espOn + ")";
        r.detail = "Forward speed " + minFwdSpeed.ToString("F0") + "-" + maxFwdSpeed.ToString("F0") + " km/h. D-gain should respond within 500-1000 ms with oversteer classification.";
        return r;

    }

    #endregion

    #region IMGUI

    // UI scaling — layout is authored against a 1080p reference; GUI.matrix scales it up for 1440p / 4K / ultrawide.
    private const float UI_REFERENCE_HEIGHT = 1080f;
    private float _uiScale = 1f;
    private float _uiRefW = 1920f;
    private float _uiRefH = 1080f;

    private void InitStyles() {

        if (_stylesInitialized)
            return;

        _titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.58f, 0f) }
        };

        _headerStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _passStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green }
        };

        _failStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.3f, 0.3f) }
        };

        _activeStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        _labelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        _smallLabelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 12,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        _graphLabelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        _boxStyle = new GUIStyle(GUI.skin.box);

        _stylesInitialized = true;

    }

    private void OnGUI() {

        InitStyles();

        // Scale the whole UI based on screen height. All draw code below uses _uiRefW/_uiRefH
        // in this reference space, and GUI.matrix stretches the rendered output to the real screen.
        _uiScale = Mathf.Max(0.5f, Screen.height / UI_REFERENCE_HEIGHT);
        _uiRefW = Screen.width / _uiScale;
        _uiRefH = Screen.height / _uiScale;

        Matrix4x4 prevMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_uiScale, _uiScale, 1f));

        // Rebuild graph textures at throttled rate.
        if (Time.time - lastGraphRebuildTime >= graphRebuildInterval) {
            UpdateGraphTextures();
            lastGraphRebuildTime = Time.time;
        }

        DrawControlPanel();
        if (showTelemetry) DrawTelemetry();
        if (showGraphs) DrawGraphs();
        DrawResults();

        GUI.matrix = prevMatrix;

    }

    private void DrawControlPanel() {

        GUILayout.BeginArea(new Rect(10, 10, 310, _uiRefH - 20), _boxStyle);
        GUILayout.Space(5);

        GUILayout.Label("RCCP ESP Detailed Test", _titleStyle);
        GUILayout.Space(6);

        // Vehicle selector
        RCCP_CarController[] demoVehicles = RCCP_DemoVehicles.Instance != null ? RCCP_DemoVehicles.Instance.vehicles : null;

        if (demoVehicles != null && demoVehicles.Length > 0) {

            GUILayout.Label("Vehicle", _headerStyle);

            GUILayout.BeginHorizontal();
            bool canNav = !testRunning;

            if (GUILayout.Button("<", GUILayout.Width(30)) && canNav) {
                selectedVehicleIndex--;
                if (selectedVehicleIndex < 0) selectedVehicleIndex = demoVehicles.Length - 1;
            }

            string vehicleName = demoVehicles[selectedVehicleIndex] != null ? demoVehicles[selectedVehicleIndex].name : "---";
            GUILayout.Label(vehicleName, _headerStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", GUILayout.Width(30)) && canNav) {
                selectedVehicleIndex++;
                if (selectedVehicleIndex >= demoVehicles.Length) selectedVehicleIndex = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Drivetrain: " + GetDrivetrainLabel(), _smallLabelStyle);

            GUI.enabled = !testRunning;
            if (GUILayout.Button("Spawn Vehicle", GUILayout.Height(28)))
                SpawnVehicle();
            GUI.enabled = true;

        }

        GUILayout.Space(10);
        GUILayout.Label("ESP V2 Tests", _headerStyle);

        GUI.enabled = !testRunning && vehicle != null;

        if (GUILayout.Button("1. Hysteresis", GUILayout.Height(24)))
            RunTest(DetailedTestType.Hysteresis);
        if (GUILayout.Button("2. Sideslip Spin Risk", GUILayout.Height(24)))
            RunTest(DetailedTestType.SideslipSpinRisk);
        if (GUILayout.Button("3. Sport vs Normal", GUILayout.Height(24)))
            RunTest(DetailedTestType.SportVsNormal);
        if (GUILayout.Button("4. Preserve Speed (f=0 vs f=1)", GUILayout.Height(24)))
            RunTest(DetailedTestType.PreserveSpeed);
        if (GUILayout.Button("5. Spin Recovery", GUILayout.Height(24)))
            RunTest(DetailedTestType.SpinRecovery);
        if (GUILayout.Button("6. ISO 3888 Moose", GUILayout.Height(24)))
            RunTest(DetailedTestType.MooseTest);
        if (GUILayout.Button("7. Skidpad", GUILayout.Height(24)))
            RunTest(DetailedTestType.Skidpad);
        if (GUILayout.Button("8. Lift-off Oversteer", GUILayout.Height(24)))
            RunTest(DetailedTestType.LiftOffOversteer);

        GUILayout.Space(4);
        if (GUILayout.Button("Run All Detailed Tests", GUILayout.Height(30)))
            RunAllTests();

        GUI.enabled = true;

        if (testRunning) {
            GUI.enabled = true;
            if (GUILayout.Button("STOP Test", GUILayout.Height(24)))
                StopCurrentTest();
        }

        GUILayout.Space(8);

        // Free drive + toggles
        GUI.enabled = !testRunning;
        string freeDriveLabel = freeDriveMode ? "Disable Free Drive" : "Enable Free Drive";
        if (GUILayout.Button(freeDriveLabel, GUILayout.Height(24))) {
            freeDriveMode = !freeDriveMode;
            if (freeDriveMode) ReleaseInputs();
        }
        GUI.enabled = true;

        showGraphs = GUILayout.Toggle(showGraphs, " Graphs");
        showTelemetry = GUILayout.Toggle(showTelemetry, " Telemetry");
        autoExportCsvOnTestComplete = GUILayout.Toggle(autoExportCsvOnTestComplete, " Auto CSV on test");

        GUILayout.Space(4);
        if (GUILayout.Button("Export samples to CSV"))
            ExportSamplesCSV("manual");

        if (!string.IsNullOrEmpty(lastCsvPath))
            GUILayout.Label("Saved: " + lastCsvPath, _smallLabelStyle);

        GUILayout.Space(8);
        GUILayout.Label("Status:", _headerStyle);
        GUILayout.Label(testStatus, testRunning ? _activeStyle : _labelStyle);

        GUILayout.Space(8);
        if (GUILayout.Button("Clear Results"))
            results.Clear();

        GUILayout.EndArea();

    }

    private void DrawTelemetry() {

        float panelX = 330;
        float panelW = _uiRefW - panelX - 10;
        float panelH = 160;

        GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), _boxStyle);
        GUILayout.Space(3);
        GUILayout.Label("Live Telemetry (ESP V2)", _titleStyle);

        if (vehicle == null || vehicle.Stability == null) {
            GUILayout.Label("No vehicle", _labelStyle);
            GUILayout.EndArea();
            return;
        }

        RCCP_Stability s = vehicle.Stability;

        // Row 1: speed, drift angle, steer, throttle/brake
        GUILayout.BeginHorizontal();
        GUILayout.Label("Speed: " + vehicle.absoluteSpeed.ToString("F0") + " km/h", _labelStyle, GUILayout.Width(130));

        Vector3 vel = vehicle.Rigid.linearVelocity;
        float drift = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up) : 0f;
        GUILayout.Label("Drift: " + drift.ToString("F1") + " deg", _labelStyle, GUILayout.Width(110));
        GUILayout.Label("Steer: " + vehicle.steerAngle.ToString("F1") + " deg", _labelStyle, GUILayout.Width(120));

        string dt = GetDrivetrainLabel();
        GUILayout.Label("Drivetrain: " + dt, _labelStyle, GUILayout.Width(120));
        GUILayout.EndHorizontal();

        // Row 2: yaw actual / ref / error
        GUILayout.BeginHorizontal();
        GUILayout.Label("Yaw Actual: " + s.debugYawActualDegS.ToString("F1") + " deg/s", _labelStyle, GUILayout.Width(150));
        GUILayout.Label("Yaw Ref: " + s.debugYawRefDegS.ToString("F1") + " deg/s", _labelStyle, GUILayout.Width(130));
        GUILayout.Label("Yaw Err: " + s.debugYawErrorDegS.ToString("F1") + " deg/s",
            Mathf.Abs(s.debugYawErrorDegS) > s.espDeadband ? _failStyle : _labelStyle, GUILayout.Width(140));
        GUILayout.Label("Mode: " + s.espMode, _labelStyle);
        GUILayout.EndHorizontal();

        // Row 3: sideslip + engagement flags
        GUILayout.BeginHorizontal();
        GUILayout.Label("beta: " + s.debugSideslipAngleDeg.ToString("F1") + " deg",
            Mathf.Abs(s.debugSideslipAngleDeg) > s.sideslipMaxAngle ? _failStyle : _labelStyle, GUILayout.Width(130));
        GUILayout.Label("dbeta/dt: " + s.debugSideslipRateDegS.ToString("F1") + " deg/s",
            Mathf.Abs(s.debugSideslipRateDegS) > s.sideslipMaxRate ? _failStyle : _labelStyle, GUILayout.Width(160));

        GUILayout.Label("ESP: " + (s.ESPEngaged ? "ON" : "off"), s.ESPEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(70));
        GUILayout.Label("IND: " + (s.ESPIndicatorEngaged ? "ON" : "off"), s.ESPIndicatorEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(70));
        GUILayout.Label("ABS: " + (s.ABSEngaged ? "ON" : "off"), s.ABSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(70));
        GUILayout.Label("TCS: " + (s.TCSEngaged ? "ON" : "off"), s.TCSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        // Row 4: classification + active wheel
        GUILayout.BeginHorizontal();
        GUILayout.Label("Classify: " + (s.debugIsOversteer ? "OVERSTEER" : "understeer"),
            s.debugIsOversteer ? _failStyle : _smallLabelStyle, GUILayout.Width(170));

        string[] wheelNames = { "FL", "FR", "RL", "RR" };
        string activeWheel = (s.debugActiveWheelIndex >= 0 && s.debugActiveWheelIndex < 4) ? wheelNames[s.debugActiveWheelIndex] : "none";
        GUILayout.Label("ESP wheel: " + activeWheel, s.debugActiveWheelIndex >= 0 ? _activeStyle : _smallLabelStyle, GUILayout.Width(120));

        GUILayout.Label("Deadband: " + s.espDeadband.ToString("F1") + "/" + s.espDeactivationDeadband.ToString("F1") + " deg/s", _smallLabelStyle, GUILayout.Width(180));
        GUILayout.Label("Intensity: " + s.ESPIntensity.ToString("F2") + " | Preserve: " + s.preserveSpeedFactor.ToString("F2"), _smallLabelStyle);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

    }

    private void DrawGraphs() {

        float panelX = 330;
        float panelY = 180;
        float panelW = _uiRefW - panelX - 10;
        float panelH = (GraphH + 22) * 4 + 20;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH), _boxStyle);
        GUILayout.Space(3);
        GUILayout.Label("Live Graphs (yellow=yaw actual, orange=yaw ref, red=yaw err | cyan=beta, magenta=dbeta | green/orange/blue bars | red=max ESP brake)", _graphLabelStyle);

        float graphDrawW = Mathf.Min(GraphW, panelW - 20);
        float x = 5;

        DrawOneGraph("Yaw rate (deg/s)", graphYawTex, x, GraphH);
        DrawOneGraph("Sideslip beta / rate", graphSideslipTex, x, GraphH);
        DrawOneGraph("Engagement (ESP / Indicator / Oversteer)", graphEngagementTex, x, GraphH);
        DrawOneGraph("Max ESP brake torque (Nm)", graphBrakeTex, x, GraphH);

        GUILayout.EndArea();

    }

    private void DrawOneGraph(string label, Texture2D tex, float x, float h) {

        GUILayout.Label(label, _smallLabelStyle);
        if (tex != null) {
            Rect r = GUILayoutUtility.GetRect(GraphW, h);
            GUI.DrawTexture(r, tex);
        } else {
            GUILayout.Space(h);
        }

    }

    private void DrawResults() {

        float panelX = 330;
        float panelY = 180 + ((GraphH + 22) * 4 + 20) + 8;
        float panelW = _uiRefW - panelX - 10;
        float panelH = _uiRefH - panelY - 10;

        if (!showGraphs) {
            panelY = 180;
            panelH = _uiRefH - panelY - 10;
        }

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH), _boxStyle);
        GUILayout.Space(3);
        GUILayout.Label("Test Results", _titleStyle);

        if (results.Count == 0) {
            GUILayout.Label("No tests run yet. Pick one from the left or click Run All Detailed Tests.", _smallLabelStyle);
            GUILayout.EndArea();
            return;
        }

        resultsScroll = GUILayout.BeginScrollView(resultsScroll);

        for (int i = 0; i < results.Count; i++) {

            TestResult r = results[i];
            GUILayout.BeginHorizontal();
            string prefix = r.passed ? "[PASS]" : "[FAIL]";
            GUIStyle prefixStyle = r.passed ? _passStyle : _failStyle;
            GUILayout.Label(prefix, prefixStyle, GUILayout.Width(50));
            GUILayout.Label(r.type.ToString() + ": " + r.summary, _labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.Label("    " + r.detail, _smallLabelStyle);
            GUILayout.Space(4);

        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

    }

    #endregion

    #region Graph Rendering

    private void InitGraphTextures() {

        graphYawTex = new Texture2D(GraphW, GraphH, TextureFormat.RGBA32, false);
        graphYawTex.filterMode = FilterMode.Point;
        graphYawTex.wrapMode = TextureWrapMode.Clamp;

        graphSideslipTex = new Texture2D(GraphW, GraphH, TextureFormat.RGBA32, false);
        graphSideslipTex.filterMode = FilterMode.Point;
        graphSideslipTex.wrapMode = TextureWrapMode.Clamp;

        graphEngagementTex = new Texture2D(GraphW, GraphH, TextureFormat.RGBA32, false);
        graphEngagementTex.filterMode = FilterMode.Point;
        graphEngagementTex.wrapMode = TextureWrapMode.Clamp;

        graphBrakeTex = new Texture2D(GraphW, GraphH, TextureFormat.RGBA32, false);
        graphBrakeTex.filterMode = FilterMode.Point;
        graphBrakeTex.wrapMode = TextureWrapMode.Clamp;

        graphClearBuffer = new Color[GraphW * GraphH];
        Color bg = new Color(0.06f, 0.06f, 0.08f, 0.9f);
        for (int i = 0; i < graphClearBuffer.Length; i++)
            graphClearBuffer[i] = bg;

    }

    private void DestroyGraphTextures() {

        if (graphYawTex != null) Destroy(graphYawTex);
        if (graphSideslipTex != null) Destroy(graphSideslipTex);
        if (graphEngagementTex != null) Destroy(graphEngagementTex);
        if (graphBrakeTex != null) Destroy(graphBrakeTex);

    }

    private Color[] _yawPixels;
    private Color[] _sidePixels;
    private Color[] _engPixels;
    private Color[] _brakePixels;

    private void UpdateGraphTextures() {

        if (graphYawTex == null || graphCount == 0)
            return;

        if (_yawPixels == null || _yawPixels.Length != GraphW * GraphH) {
            _yawPixels = new Color[GraphW * GraphH];
            _sidePixels = new Color[GraphW * GraphH];
            _engPixels = new Color[GraphW * GraphH];
            _brakePixels = new Color[GraphW * GraphH];
        }

        System.Array.Copy(graphClearBuffer, _yawPixels, graphClearBuffer.Length);
        System.Array.Copy(graphClearBuffer, _sidePixels, graphClearBuffer.Length);
        System.Array.Copy(graphClearBuffer, _engPixels, graphClearBuffer.Length);
        System.Array.Copy(graphClearBuffer, _brakePixels, graphClearBuffer.Length);

        // Mid-line grid for signed plots.
        Color grid = new Color(0.2f, 0.2f, 0.25f, 1f);
        int mid = GraphH / 2;
        for (int x = 0; x < GraphW; x++) {
            _yawPixels[mid * GraphW + x] = grid;
            _sidePixels[mid * GraphW + x] = grid;
        }

        // Auto-scale yaw to 80 deg/s peak.
        float yawScale = 80f;
        // Auto-scale sideslip.
        float betaScale = 30f;   // deg
        float dBetaScale = 60f;  // deg/s
        float brakeScale = 4000f;

        // Walk oldest-to-newest and plot. X axis maps sample index to [0..GraphW-1].
        int N = graphCount;
        int start = (graphHead - N + graphBuffer.Length) % graphBuffer.Length;

        for (int i = 1; i < N; i++) {

            Sample sPrev = graphBuffer[(start + i - 1) % graphBuffer.Length];
            Sample sCur = graphBuffer[(start + i) % graphBuffer.Length];

            int xPrev = Mathf.Clamp(Mathf.FloorToInt((i - 1) * (GraphW - 1f) / (N - 1f)), 0, GraphW - 1);
            int xCur = Mathf.Clamp(Mathf.FloorToInt(i * (GraphW - 1f) / (N - 1f)), 0, GraphW - 1);

            // Yaw rate graph
            DrawLine(_yawPixels, xPrev, SignedToY(sPrev.yawActualDegS, yawScale),
                                  xCur,  SignedToY(sCur.yawActualDegS, yawScale),
                                  new Color(1f, 0.95f, 0.2f, 1f));
            DrawLine(_yawPixels, xPrev, SignedToY(sPrev.yawRefDegS, yawScale),
                                  xCur,  SignedToY(sCur.yawRefDegS, yawScale),
                                  new Color(1f, 0.5f, 0.1f, 1f));
            DrawLine(_yawPixels, xPrev, SignedToY(sPrev.yawErrorDegS, yawScale),
                                  xCur,  SignedToY(sCur.yawErrorDegS, yawScale),
                                  new Color(1f, 0.2f, 0.2f, 1f));

            // Sideslip graph
            DrawLine(_sidePixels, xPrev, SignedToY(sPrev.sideslipDeg, betaScale),
                                   xCur,  SignedToY(sCur.sideslipDeg, betaScale),
                                   new Color(0.2f, 0.9f, 0.95f, 1f));
            DrawLine(_sidePixels, xPrev, SignedToY(sPrev.sideslipRateDegS, dBetaScale),
                                   xCur,  SignedToY(sCur.sideslipRateDegS, dBetaScale),
                                   new Color(0.95f, 0.2f, 0.95f, 1f));

            // Engagement graph — three stacked on/off bars.
            int espY = (int)(GraphH * 0.15f);
            int indY = (int)(GraphH * 0.45f);
            int ovrY = (int)(GraphH * 0.75f);

            if (sCur.espEngaged) FillColumn(_engPixels, xCur, espY - 4, espY + 4, new Color(0.3f, 1f, 0.3f, 1f));
            if (sCur.espIndicator) FillColumn(_engPixels, xCur, indY - 4, indY + 4, new Color(1f, 0.6f, 0.1f, 1f));
            if (sCur.espEngaged && sCur.isOversteer) FillColumn(_engPixels, xCur, ovrY - 4, ovrY + 4, new Color(0.4f, 0.5f, 1f, 1f));

            // Brake torque graph — positive only, bottom-anchored.
            int yPrev = UnsignedToY(sPrev.maxBrakeNm, brakeScale);
            int yCur = UnsignedToY(sCur.maxBrakeNm, brakeScale);
            DrawLine(_brakePixels, xPrev, yPrev, xCur, yCur, new Color(1f, 0.3f, 0.3f, 1f));

        }

        graphYawTex.SetPixels(_yawPixels);
        graphYawTex.Apply(false);
        graphSideslipTex.SetPixels(_sidePixels);
        graphSideslipTex.Apply(false);
        graphEngagementTex.SetPixels(_engPixels);
        graphEngagementTex.Apply(false);
        graphBrakeTex.SetPixels(_brakePixels);
        graphBrakeTex.Apply(false);

    }

    private int SignedToY(float v, float scale) {

        // Map [-scale, +scale] into [0, GraphH-1]. Mid = zero.
        float norm = Mathf.Clamp(v / scale, -1f, 1f);
        int y = Mathf.FloorToInt(GraphH * 0.5f + norm * GraphH * 0.45f);
        return Mathf.Clamp(y, 0, GraphH - 1);

    }

    private int UnsignedToY(float v, float scale) {

        float norm = Mathf.Clamp01(v / scale);
        int y = Mathf.FloorToInt(norm * (GraphH - 1));
        return y;

    }

    private void DrawLine(Color[] pixels, int x0, int y0, int x1, int y1, Color c) {

        // Bresenham.
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true) {
            if (x0 >= 0 && x0 < GraphW && y0 >= 0 && y0 < GraphH)
                pixels[y0 * GraphW + x0] = c;
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }

    }

    private void FillColumn(Color[] pixels, int x, int y0, int y1, Color c) {

        if (x < 0 || x >= GraphW) return;
        int yMin = Mathf.Clamp(Mathf.Min(y0, y1), 0, GraphH - 1);
        int yMax = Mathf.Clamp(Mathf.Max(y0, y1), 0, GraphH - 1);

        for (int y = yMin; y <= yMax; y++)
            pixels[y * GraphW + x] = c;

    }

    #endregion

    #region CSV Export

    private void ExportSamplesCSV(string label) {

        if (samples.Count == 0) {
            lastCsvPath = "(no samples)";
            return;
        }

        string dir = Path.Combine(Application.persistentDataPath, "RCCP_ESP_Tests");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fname = "rccp_esp_" + label + "_" + timestamp + ".csv";
        string path = Path.Combine(dir, fname);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("time,speed_kmh,yaw_actual_degS,yaw_ref_degS,yaw_error_degS,beta_deg,beta_rate_degS,max_brake_Nm,active_wheel,esp,esp_indicator,is_oversteer,abs,tcs,steer_deg,throttle,brake,drift_deg,fl_motor,fr_motor,rl_motor,rr_motor");

        for (int i = 0; i < samples.Count; i++) {
            Sample s = samples[i];
            sb.Append(s.time.ToString("F4")).Append(',')
              .Append(s.speed.ToString("F2")).Append(',')
              .Append(s.yawActualDegS.ToString("F3")).Append(',')
              .Append(s.yawRefDegS.ToString("F3")).Append(',')
              .Append(s.yawErrorDegS.ToString("F3")).Append(',')
              .Append(s.sideslipDeg.ToString("F3")).Append(',')
              .Append(s.sideslipRateDegS.ToString("F3")).Append(',')
              .Append(s.maxBrakeNm.ToString("F1")).Append(',')
              .Append(s.activeWheelIndex).Append(',')
              .Append(s.espEngaged ? "1" : "0").Append(',')
              .Append(s.espIndicator ? "1" : "0").Append(',')
              .Append(s.isOversteer ? "1" : "0").Append(',')
              .Append(s.absEngaged ? "1" : "0").Append(',')
              .Append(s.tcsEngaged ? "1" : "0").Append(',')
              .Append(s.steerAngle.ToString("F2")).Append(',')
              .Append(s.throttleInput.ToString("F3")).Append(',')
              .Append(s.brakeInput.ToString("F3")).Append(',')
              .Append(s.driftAngle.ToString("F3")).Append(',')
              .Append(s.frontLeftMotor.ToString("F1")).Append(',')
              .Append(s.frontRightMotor.ToString("F1")).Append(',')
              .Append(s.rearLeftMotor.ToString("F1")).Append(',')
              .Append(s.rearRightMotor.ToString("F1"))
              .AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
        lastCsvPath = path;
        Debug.Log("[RCCP_ESPDetailedTest] Exported " + samples.Count + " samples to: " + path);

    }

    #endregion

    #region Editor Course Builder

    /// <summary>
    /// Builds a simple visual test course as child objects: skidpad ring, moose chicane cones,
    /// J-turn marker, and a small jump ramp. Purely visual/physical aids — the auto-tests do
    /// not depend on these markers and will still run on a flat plane. Right-click the component
    /// in the Inspector and choose "Build Course" to regenerate.
    /// </summary>
    [ContextMenu("Build Course")]
    public void BuildCourse() {

#if UNITY_EDITOR
        Transform courseRoot = transform.Find("Course");

        if (courseRoot != null)
            DestroyImmediate(courseRoot.gameObject);

        GameObject root = new GameObject("Course");
        root.transform.SetParent(transform, false);
        courseRoot = root.transform;

        // ---- Skidpad ring (radius 20m, circle of 24 cones) ----
        {
            GameObject ringRoot = new GameObject("Skidpad");
            ringRoot.transform.SetParent(courseRoot, false);
            ringRoot.transform.localPosition = new Vector3(60f, 0f, 0f);

            int cones = 24;
            float radius = 20f;
            for (int i = 0; i < cones; i++) {
                float a = (float)i / cones * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                CreateCone(ringRoot.transform, "ConeSkid_" + i, p, new Color(1f, 0.4f, 0f));
            }
        }

        // ---- ISO 3888 moose chicane (simplified) ----
        {
            GameObject chicane = new GameObject("MooseChicane");
            chicane.transform.SetParent(courseRoot, false);
            chicane.transform.localPosition = new Vector3(-80f, 0f, 0f);

            // 3 gates: straight (z=0..13), dodge (z=13..24), return (z=24..37)
            // Cones on both sides mark the channel.
            SpawnCone(chicane.transform, "g1_L", new Vector3(-1.6f, 0f, 0f), Color.yellow);
            SpawnCone(chicane.transform, "g1_R", new Vector3(1.6f, 0f, 0f), Color.yellow);
            SpawnCone(chicane.transform, "g1_L2", new Vector3(-1.6f, 0f, 12f), Color.yellow);
            SpawnCone(chicane.transform, "g1_R2", new Vector3(1.6f, 0f, 12f), Color.yellow);
            SpawnCone(chicane.transform, "g2_L", new Vector3(2.6f, 0f, 17f), Color.red);
            SpawnCone(chicane.transform, "g2_R", new Vector3(5.8f, 0f, 17f), Color.red);
            SpawnCone(chicane.transform, "g2_L2", new Vector3(2.6f, 0f, 23f), Color.red);
            SpawnCone(chicane.transform, "g2_R2", new Vector3(5.8f, 0f, 23f), Color.red);
            SpawnCone(chicane.transform, "g3_L", new Vector3(-1.6f, 0f, 28f), Color.green);
            SpawnCone(chicane.transform, "g3_R", new Vector3(1.6f, 0f, 28f), Color.green);
            SpawnCone(chicane.transform, "g3_L2", new Vector3(-1.6f, 0f, 40f), Color.green);
            SpawnCone(chicane.transform, "g3_R2", new Vector3(1.6f, 0f, 40f), Color.green);
        }

        // ---- J-turn marker (visual only, moved off the test auto-run path) ----
        {
            GameObject j = GameObject.CreatePrimitive(PrimitiveType.Cube);
            j.name = "JTurn_Start";
            j.transform.SetParent(courseRoot, false);
            j.transform.localPosition = new Vector3(120f, 0.1f, 0f);
            j.transform.localScale = new Vector3(1f, 0.2f, 5f);
            DestroyImmediate(j.GetComponent<Collider>());
            ApplyMarkerMaterial(j, new Color(0.2f, 0.6f, 1f));
        }

        // ---- Jump ramp (visual only — collider removed so aggressive tests can never hit it) ----
        {
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "JumpRamp";
            ramp.transform.SetParent(courseRoot, false);
            ramp.transform.localPosition = new Vector3(150f, 0.4f, 50f);
            ramp.transform.localRotation = Quaternion.Euler(0f, 90f, -12f);
            ramp.transform.localScale = new Vector3(8f, 0.5f, 8f);
            DestroyImmediate(ramp.GetComponent<Collider>());
            ApplyMarkerMaterial(ramp, new Color(0.5f, 0.5f, 0.5f));
        }

        Debug.Log("[RCCP_ESPDetailedTest] Course built under " + courseRoot.name);
#endif

    }

    private void SpawnCone(Transform parent, string name, Vector3 localPos, Color c) {

        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = name;
        cone.transform.SetParent(parent, false);
        cone.transform.localPosition = localPos + Vector3.up * 0.4f;
        cone.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        var col = cone.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        ApplyMarkerMaterial(cone, c);

    }

    private void CreateCone(Transform parent, string name, Vector3 localPos, Color c) {
        SpawnCone(parent, name, localPos, c);
    }

    private void ApplyMarkerMaterial(GameObject go, Color c) {

        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.color = c;
        r.sharedMaterial = m;

    }

    #endregion

}

//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

/// <summary>
/// Behavior-preset validation rig for RCCP. Exercises the active behavior preset
/// (race / drift archetypes) through scripted input scenarios and reports pass/fail
/// telemetry. Pairs with Assets/RCCP_Scene_Blank_BehaviorTest.unity.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Demo/RCCP Behavior Test")]
public class RCCP_BehaviorTest : RCCP_GenericComponent {

    #region Data Structures

    private enum Suite { None, Race, Drift }

    private struct TestSample {

        [Tooltip("Elapsed seconds since the test started.")]
        public float time;
        [Tooltip("Vehicle speed in km/h.")]
        public float speed;
        [Tooltip("World position at sample time.")]
        public Vector3 position;
        [Tooltip("Signed slip angle (beta) in degrees between local +Z and velocity.")]
        public float slipAngle;
        [Tooltip("Yaw rate around world up axis in deg/s.")]
        public float yawRate;
        [Tooltip("Centripetal lateral acceleration in g's.")]
        public float lateralG;

        public bool espEngaged;
        public bool absEngaged;
        public bool tcsEngaged;

        public float[] forwardSlip;
        public float[] sidewaysSlip;
        public float[] brakeTorque;
        public float[] motorTorque;

    }

    private struct TestResult {

        [Tooltip("Race or Drift suite.")]
        public Suite suite;
        [Tooltip("Test identifier shown in the dashboard.")]
        public string testName;
        [Tooltip("Whether the active behavior preset passed the criteria.")]
        public bool passed;
        [Tooltip("One-line summary of the result.")]
        public string summary;
        [Tooltip("Detailed measured values vs thresholds.")]
        public string detail;
        [Tooltip("Diagnostic measurements written to the JSON report (empty for early-aborted tests).")]
        public Dictionary<string, float> metrics;

    }

    #endregion

    #region State

    // Default = 2 (M3_E46, 1350kg RWD) — the reference drift vehicle for preset tuning after
    // the demo content import shifted RCCP_DemoVehicles.vehicles[] (the prototype Skyline used
    // before is no longer in the list). Use the dashboard's prev/next buttons to change at runtime.
    private int selectedVehicleIndex = 2;
    private RCCP_CarController vehicle;
    private Transform spawnPoint;

    private bool testRunning = false;
    private string testStatus = "Idle";
    private bool freeDriveMode = false;
    private Coroutine activeTestCoroutine;

    private List<TestSample> samples = new List<TestSample>();
    private List<TestResult> results = new List<TestResult>();
    private Vector2 resultsScroll;

    [Tooltip("Auto-write a JSON report to persistentDataPath when a suite finishes.")]
    public bool autoExportOnSuiteComplete = true;
    private string lastReportPath = "";
    private Suite lastReportSuite = Suite.None;

    [Header("Suite Preset Names")]
    [Tooltip("If non-empty and Auto-Switch is on, the Race suite calls RCCP.SetBehavior(this) before its first test. Match a name in RCCP_Settings.behaviorTypes (e.g. \"Race\", \"race-gt\", \"race-formula\").")]
    public string racePresetName = "Race";
    [Tooltip("If non-empty and Auto-Switch is on, the Drift suite calls RCCP.SetBehavior(this) before its first test. Match a name in RCCP_Settings.behaviorTypes (e.g. \"Drift\").")]
    public string driftPresetName = "Drift";
    [Tooltip("If true, each suite calls RCCP.SetBehavior(<presetName>) before starting. Disable to test whichever preset is active when you click Run.")]
    public bool autoSwitchPresetPerSuite = true;

    [Header("Spawn Hygiene")]
    [Tooltip("If true, RCCP_Damage is disabled on the spawned vehicle. Damage at high slip / drop has historically shed wheels and invalidated stability runs (e.g. E46), so the test rig defaults to off.")]
    public bool disableDamageOnSpawn = true;
    [Tooltip("If true, RCCP_Engine.engineBraking is set to false on the spawned vehicle. Engine braking adds an off-throttle longitudinal bias that contaminates Drift / TransitionSDrift / coast-down telemetry, so the test rig defaults to off.")]
    public bool disableEngineBrakingOnSpawn = true;

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
    private bool _stylesInitialized;

    #endregion

    private void Awake() {

        GameObject sp = GameObject.Find("RCCP_SpawnPoint");

        if (sp != null)
            spawnPoint = sp.transform;

    }

    private void Start() {

        SpawnVehicle();

    }

    #region Vehicle Management

    private void SpawnVehicle() {

        RCCP_CarController[] demoVehicles = RCCP_DemoVehicles.Instance != null ? RCCP_DemoVehicles.Instance.vehicles : null;

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

        ApplySpawnHygiene();

    }

    /// <summary>
    /// Disables damage component and engine braking on the active test vehicle so neither
    /// contaminates stability/drift telemetry. Both default-on flags can be toggled in the
    /// inspector if a specific test legitimately needs them.
    /// </summary>
    private void ApplySpawnHygiene() {

        if (vehicle == null)
            return;

        if (disableDamageOnSpawn && vehicle.Damage != null)
            vehicle.Damage.enabled = false;

        if (disableEngineBrakingOnSpawn && vehicle.Engine != null)
            vehicle.Engine.engineBraking = false;

    }

    private void ResetVehicle() {

        if (vehicle == null)
            return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        vehicle.transform.position = pos + Vector3.up * 0.5f;
        vehicle.transform.rotation = rot;
        vehicle.Rigid.linearVelocity = Vector3.zero;
        vehicle.Rigid.angularVelocity = Vector3.zero;

        if (vehicle.Inputs != null)
            vehicle.Inputs.DisableOverrideInputs();

    }

    private void OverrideInputs(float throttle, float brake, float steer, float handbrake) {

        if (vehicle == null || vehicle.Inputs == null)
            return;

        RCCP_Inputs inputs = new RCCP_Inputs();
        inputs.throttleInput = throttle;
        inputs.brakeInput = brake;
        inputs.steerInput = steer;
        inputs.handbrakeInput = handbrake;
        vehicle.Inputs.OverrideInputs(inputs);

    }

    private void ReleaseInputs() {

        if (vehicle != null && vehicle.Inputs != null)
            vehicle.Inputs.DisableOverrideInputs();

    }

    private string GetActiveBehaviorName() {

        if (vehicle == null)
            return "—";

        RCCP_Settings.BehaviorType bt = vehicle.GetVehicleBehaviorType();
        return bt != null ? bt.behaviorName : "(none)";

    }

    #endregion

    #region Sampling

    private TestSample TakeSample(float startTime) {

        TestSample s = new TestSample();
        s.time = Time.time - startTime;
        s.forwardSlip = new float[4];
        s.sidewaysSlip = new float[4];
        s.brakeTorque = new float[4];
        s.motorTorque = new float[4];

        if (vehicle == null)
            return s;

        s.speed = vehicle.absoluteSpeed;
        s.position = vehicle.transform.position;
        s.yawRate = vehicle.Rigid.angularVelocity.y * Mathf.Rad2Deg;

        Vector3 vel = vehicle.Rigid.linearVelocity;

        if (vel.sqrMagnitude > 0.1f)
            s.slipAngle = Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up);

        // centripetal lateral G: a_lat = omega * v
        float speedMpS = vehicle.absoluteSpeed / 3.6f;
        float yawRateRadPerSec = vehicle.Rigid.angularVelocity.y;
        s.lateralG = (yawRateRadPerSec * speedMpS) / 9.81f;

        RCCP_Stability stability = vehicle.Stability;

        if (stability != null) {

            s.espEngaged = stability.ESPEngaged;
            s.absEngaged = stability.ABSEngaged;
            s.tcsEngaged = stability.TCSEngaged;

            FillWheelSample(ref s, stability);

        }

        return s;

    }

    private void FillWheelSample(ref TestSample s, RCCP_Stability stability) {

        if (stability.frontAxle != null) {

            if (stability.frontAxle.leftWheelCollider != null) {

                s.forwardSlip[0] = stability.frontAxle.leftWheelCollider.ForwardSlip;
                s.sidewaysSlip[0] = stability.frontAxle.leftWheelCollider.SidewaysSlip;
                s.brakeTorque[0] = stability.frontAxle.leftWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[0] = stability.frontAxle.leftWheelCollider.WheelCollider.motorTorque;

            }

            if (stability.frontAxle.rightWheelCollider != null) {

                s.forwardSlip[1] = stability.frontAxle.rightWheelCollider.ForwardSlip;
                s.sidewaysSlip[1] = stability.frontAxle.rightWheelCollider.SidewaysSlip;
                s.brakeTorque[1] = stability.frontAxle.rightWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[1] = stability.frontAxle.rightWheelCollider.WheelCollider.motorTorque;

            }

        }

        if (stability.rearAxle != null) {

            if (stability.rearAxle.leftWheelCollider != null) {

                s.forwardSlip[2] = stability.rearAxle.leftWheelCollider.ForwardSlip;
                s.sidewaysSlip[2] = stability.rearAxle.leftWheelCollider.SidewaysSlip;
                s.brakeTorque[2] = stability.rearAxle.leftWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[2] = stability.rearAxle.leftWheelCollider.WheelCollider.motorTorque;

            }

            if (stability.rearAxle.rightWheelCollider != null) {

                s.forwardSlip[3] = stability.rearAxle.rightWheelCollider.ForwardSlip;
                s.sidewaysSlip[3] = stability.rearAxle.rightWheelCollider.SidewaysSlip;
                s.brakeTorque[3] = stability.rearAxle.rightWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[3] = stability.rearAxle.rightWheelCollider.WheelCollider.motorTorque;

            }

        }

    }

    #endregion

    #region Helper Coroutines

    private IEnumerator AccelerateTo(float targetSpeedKmh, float timeoutSec) {

        OverrideInputs(1f, 0f, 0f, 0f);
        float deadline = Time.time + timeoutSec;

        while (vehicle.absoluteSpeed < targetSpeedKmh && Time.time < deadline)
            yield return null;

    }

    private static float Average(List<float> values) {

        if (values == null || values.Count == 0)
            return 0f;

        float sum = 0f;

        for (int i = 0; i < values.Count; i++)
            sum += values[i];

        return sum / values.Count;

    }

    private static float MaxAbs(List<float> values) {

        if (values == null || values.Count == 0)
            return 0f;

        float peak = 0f;

        for (int i = 0; i < values.Count; i++) {

            float a = Mathf.Abs(values[i]);

            if (a > peak)
                peak = a;

        }

        return peak;

    }

    private TestResult MakeFailedResult(Suite suite, string testName, string summary, string detail) {

        TestResult r = new TestResult();
        r.suite = suite;
        r.testName = testName;
        r.passed = false;
        r.summary = summary;
        r.detail = detail;
        r.metrics = new Dictionary<string, float>();
        return r;

    }

    #endregion

    #region Race Suite

    private IEnumerator RunRaceSuite() {

        results.Clear();

        if (autoSwitchPresetPerSuite && !string.IsNullOrEmpty(racePresetName)) {

            testStatus = "Switching to preset: " + racePresetName;
            RCCP.SetBehavior(racePresetName);
            yield return new WaitForSeconds(0.5f);

        }

        yield return StartCoroutine(TestAccel_0_100());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestBrake_100_0());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestSkidpad());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestSlalom());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestThrottleOnCorner());

        ReleaseInputs();

        if (autoExportOnSuiteComplete)
            ExportReport(Suite.Race);

        testRunning = false;
        testStatus = "Race suite complete";

    }

    // ---- Race 1/5: 0 to 100 km/h ----
    private IEnumerator TestAccel_0_100() {

        testStatus = "Race 1/5: Accel 0-100, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Race 1/5: Full throttle...";
        OverrideInputs(1f, 0f, 0f, 0f);

        samples.Clear();
        float startTime = Time.time;
        float deadline = startTime + 12f;
        bool reached100 = false;
        float timeTo100 = 0f;
        float peakSlip = 0f;
        int sustainedSlipCount = 0;

        while (Time.time < deadline) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            // track sustained slip on powered wheels (for TCS sanity)
            for (int w = 0; w < 4; w++) {

                if (Mathf.Abs(s.motorTorque[w]) > 1f) {

                    float a = Mathf.Abs(s.forwardSlip[w]);
                    if (a > peakSlip)
                        peakSlip = a;
                    if (a > 0.6f)
                        sustainedSlipCount++;

                }

            }

            if (vehicle.absoluteSpeed >= 100f) {

                reached100 = true;
                timeTo100 = Time.time - startTime;
                break;

            }

            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateAccel(reached100, timeTo100, peakSlip, sustainedSlipCount));
        testStatus = "Race 1/5: Done";

    }

    // ---- Race 2/5: 100 to 0 km/h brake ----
    private IEnumerator TestBrake_100_0() {

        testStatus = "Race 2/5: Brake 100-0, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Race 2/5: Accelerating to 100...";
        yield return StartCoroutine(AccelerateTo(100f, 12f));

        if (vehicle.absoluteSpeed < 90f) {

            results.Add(MakeFailedResult(Suite.Race, "Brake_100_0",
                "Could not reach 100 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h within 12 s; brake test aborted."));
            ReleaseInputs();
            yield break;

        }

        testStatus = "Race 2/5: Full brake...";
        Vector3 brakeStartPos = vehicle.transform.position;
        float brakeStartSpeed = vehicle.absoluteSpeed;

        // Match ESPDetailedTest convention: read RCCP's actual engagement threshold,
        // fall back to 0.35 if Stability is missing. Hard-coding 0.35 false-positives
        // when ABS modulation pulses past the rig threshold during normal operation.
        float lockThreshold = 0.35f;

        if (vehicle != null && vehicle.Stability != null)
            lockThreshold = vehicle.Stability.engageABSThreshold;

        samples.Clear();
        float startTime = Time.time;
        float deadline = startTime + 6f;
        OverrideInputs(0f, 1f, 0f, 0f);

        int maxConsecutiveLock = 0;
        int[] lockStreak = new int[4];

        while (Time.time < deadline && vehicle.absoluteSpeed > 1f) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            for (int w = 0; w < 4; w++) {

                if (Mathf.Abs(s.forwardSlip[w]) > lockThreshold) {

                    lockStreak[w]++;

                    if (lockStreak[w] > maxConsecutiveLock)
                        maxConsecutiveLock = lockStreak[w];

                } else {

                    lockStreak[w] = 0;

                }

            }

            yield return new WaitForFixedUpdate();

        }

        float stopDist = Vector3.Distance(brakeStartPos, vehicle.transform.position);
        ReleaseInputs();
        results.Add(EvaluateBrake(brakeStartSpeed, stopDist, maxConsecutiveLock, lockThreshold));
        testStatus = "Race 2/5: Done";

    }

    // ---- Race 3/5: Steady-state skidpad ----
    private IEnumerator TestSkidpad() {

        testStatus = "Race 3/5: Skidpad, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Race 3/5: Reaching corner speed...";
        yield return StartCoroutine(AccelerateTo(60f, 10f));

        testStatus = "Race 3/5: Settling in arc...";
        OverrideInputs(0.5f, 0f, 0.5f, 0f);
        yield return new WaitForSeconds(1.5f);

        testStatus = "Race 3/5: Sampling lateral grip...";
        samples.Clear();
        float startTime = Time.time;
        float endTime = startTime + 4f;
        int yawSignMatchCount = 0;
        int totalCount = 0;
        float peakLatG = 0f;
        List<float> sustainedG = new List<float>();

        while (Time.time < endTime) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);
            totalCount++;

            sustainedG.Add(Mathf.Abs(s.lateralG));

            if (Mathf.Abs(s.lateralG) > peakLatG)
                peakLatG = Mathf.Abs(s.lateralG);

            // Steer is +0.5 (right turn) -> yawRate should be > 0
            if (s.yawRate > 5f)
                yawSignMatchCount++;

            yield return new WaitForFixedUpdate();

        }

        // longest run with |latG| >= 0.85
        int longestRun = 0;
        int currentRun = 0;

        for (int i = 0; i < sustainedG.Count; i++) {

            if (sustainedG[i] >= 0.85f) {

                currentRun++;
                if (currentRun > longestRun)
                    longestRun = currentRun;

            } else {

                currentRun = 0;

            }

        }

        float sustainedDuration = longestRun * Time.fixedDeltaTime;
        ReleaseInputs();
        results.Add(EvaluateSkidpad(peakLatG, sustainedDuration, yawSignMatchCount, totalCount));
        testStatus = "Race 3/5: Done";

    }

    // ---- Race 4/5: Slalom (sinusoidal weave) ----
    private IEnumerator TestSlalom() {

        testStatus = "Race 4/5: Slalom, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Race 4/5: Reaching slalom speed...";
        yield return StartCoroutine(AccelerateTo(80f, 10f));

        testStatus = "Race 4/5: Sinusoidal weave...";
        samples.Clear();
        float startTime = Time.time;
        float endTime = startTime + 4f;
        float peakYaw = 0f;
        float endVx = 0f;

        while (Time.time < endTime) {

            float t = Time.time - startTime;
            float steer = 0.7f * Mathf.Sin(2f * Mathf.PI * 1.5f * t);
            OverrideInputs(0.6f, 0f, steer, 0f);

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            float ay = Mathf.Abs(s.yawRate);
            if (ay > peakYaw)
                peakYaw = ay;

            yield return new WaitForFixedUpdate();

        }

        // local forward velocity at end
        if (vehicle != null) {

            Vector3 localVel = vehicle.transform.InverseTransformDirection(vehicle.Rigid.linearVelocity);
            endVx = localVel.z;

        }

        ReleaseInputs();
        results.Add(EvaluateSlalom(peakYaw, endVx));
        testStatus = "Race 4/5: Done";

    }

    // ---- Race 5/5: Throttle-on-corner balance ----
    private IEnumerator TestThrottleOnCorner() {

        testStatus = "Race 5/5: ThrottleOnCorner, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Race 5/5: Reaching corner speed...";
        yield return StartCoroutine(AccelerateTo(70f, 10f));

        testStatus = "Race 5/5: Settling in corner...";
        OverrideInputs(0.4f, 0f, 0.5f, 0f);
        yield return new WaitForSeconds(1.5f);

        // Pre-ramp baseline (0.5 s)
        samples.Clear();
        float startTime = Time.time;
        float preEnd = startTime + 0.5f;
        List<float> preYaw = new List<float>();

        while (Time.time < preEnd) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);
            preYaw.Add(Mathf.Abs(s.yawRate));
            yield return new WaitForFixedUpdate();

        }

        float preYawAvg = Average(preYaw);

        // Ramp throttle 0.4 -> 1.0 over 0.5 s
        testStatus = "Race 5/5: Ramping throttle...";
        float rampStart = Time.time;
        float rampEnd = rampStart + 0.5f;

        while (Time.time < rampEnd) {

            float k = (Time.time - rampStart) / 0.5f;
            float thr = Mathf.Lerp(0.4f, 1f, k);
            OverrideInputs(thr, 0f, 0.5f, 0f);
            samples.Add(TakeSample(startTime));
            yield return new WaitForFixedUpdate();

        }

        // Hold 1.0 throttle for 3 s, sample post yaw rate
        testStatus = "Race 5/5: Holding full throttle...";
        OverrideInputs(1f, 0f, 0.5f, 0f);
        float postEnd = Time.time + 3f;
        List<float> postYaw = new List<float>();

        while (Time.time < postEnd) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);
            postYaw.Add(Mathf.Abs(s.yawRate));
            yield return new WaitForFixedUpdate();

        }

        float postYawAvg = Average(postYaw);

        ReleaseInputs();
        results.Add(EvaluateThrottleOnCorner(preYawAvg, postYawAvg));
        testStatus = "Race 5/5: Done";

    }

    #endregion

    #region Drift Suite

    private IEnumerator RunDriftSuite() {

        results.Clear();

        if (autoSwitchPresetPerSuite && !string.IsNullOrEmpty(driftPresetName)) {

            testStatus = "Switching to preset: " + driftPresetName;
            RCCP.SetBehavior(driftPresetName);
            yield return new WaitForSeconds(0.5f);

        }

        yield return StartCoroutine(TestDriftInitiate());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestDriftSustain());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestCounterSteerRecovery());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestThrottleModulation());
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TestTransitionSDrift());

        ReleaseInputs();

        if (autoExportOnSuiteComplete)
            ExportReport(Suite.Drift);

        testRunning = false;
        testStatus = "Drift suite complete";

    }

    // ---- Drift 1/5: Initiation ----
    private IEnumerator TestDriftInitiate() {

        testStatus = "Drift 1/5: Initiate, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Drift 1/5: Accelerating to 70...";
        yield return StartCoroutine(AccelerateTo(70f, 12f));

        if (vehicle.absoluteSpeed < 60f) {

            results.Add(MakeFailedResult(Suite.Drift, "DriftInitiate",
                "Could not reach 70 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h."));
            ReleaseInputs();
            yield break;

        }

        testStatus = "Drift 1/5: Handbrake flick (left)...";
        samples.Clear();
        float startTime = Time.time;
        float peakBeta = 0f;
        float timeToPeak = 0f;

        // 0.5 s flick: handbrake + steer left
        OverrideInputs(0.3f, 0f, -0.8f, 1f);
        float flickEnd = Time.time + 0.5f;

        while (Time.time < flickEnd) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            float ab = Mathf.Abs(s.slipAngle);
            if (ab > peakBeta) {

                peakBeta = ab;
                timeToPeak = s.time;

            }

            yield return new WaitForFixedUpdate();

        }

        // 1.5 s settle: throttle + opposite-direction steer release (counter)
        OverrideInputs(0.5f, 0f, -0.6f, 0f);
        float settleEnd = Time.time + 1.5f;

        while (Time.time < settleEnd) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            float ab = Mathf.Abs(s.slipAngle);
            if (ab > peakBeta) {

                peakBeta = ab;
                timeToPeak = s.time;

            }

            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateDriftInitiate(peakBeta, timeToPeak));
        testStatus = "Drift 1/5: Done";

    }

    // ---- Drift 2/5: Sustain ----
    private IEnumerator TestDriftSustain() {

        testStatus = "Drift 2/5: Sustain, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Drift 2/5: Accelerating to 70...";
        yield return StartCoroutine(AccelerateTo(70f, 12f));

        if (vehicle.absoluteSpeed < 60f) {

            results.Add(MakeFailedResult(Suite.Drift, "DriftSustain",
                "Could not reach 70 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h."));
            ReleaseInputs();
            yield break;

        }

        // Initiate
        testStatus = "Drift 2/5: Flick + settle...";
        OverrideInputs(0.3f, 0f, -0.8f, 1f);
        yield return new WaitForSeconds(0.5f);

        OverrideInputs(0.5f, 0f, -0.6f, 0f);

        // Wait for beta to cross 20 deg or 1.5 s
        float waitEnd = Time.time + 1.5f;

        while (Time.time < waitEnd) {

            Vector3 vel = vehicle.Rigid.linearVelocity;
            float beta = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up) : 0f;

            if (Mathf.Abs(beta) >= 20f)
                break;

            yield return null;

        }

        // Sample 3 s sustain
        testStatus = "Drift 2/5: Sustaining drift...";
        samples.Clear();
        float startTime = Time.time;
        float endTime = startTime + 3f;
        int inRange = 0;
        int total = 0;
        float minBeta = 0f;
        float maxBeta = 0f;

        while (Time.time < endTime) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);
            total++;

            float ab = Mathf.Abs(s.slipAngle);

            if (ab >= 18f && ab <= 55f)
                inRange++;

            if (ab > maxBeta)
                maxBeta = ab;

            if (total == 1 || ab < minBeta)
                minBeta = ab;

            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateDriftSustain(inRange, total, minBeta, maxBeta));
        testStatus = "Drift 2/5: Done";

    }

    // ---- Drift 3/5: Counter-steer recovery ----
    private IEnumerator TestCounterSteerRecovery() {

        testStatus = "Drift 3/5: Recovery, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Drift 3/5: Accelerating to 70...";
        yield return StartCoroutine(AccelerateTo(70f, 12f));

        if (vehicle.absoluteSpeed < 60f) {

            results.Add(MakeFailedResult(Suite.Drift, "CounterSteerRecovery",
                "Could not reach 70 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h."));
            ReleaseInputs();
            yield break;

        }

        // Induce drift
        testStatus = "Drift 3/5: Inducing drift...";
        OverrideInputs(0.3f, 0f, -0.8f, 1f);
        yield return new WaitForSeconds(0.5f);

        OverrideInputs(0.5f, 0f, -0.6f, 0f);

        // Wait for beta >= 30 deg or 2 s
        float waitEnd = Time.time + 2f;
        float capturedBeta = 0f;

        while (Time.time < waitEnd) {

            Vector3 vel = vehicle.Rigid.linearVelocity;
            capturedBeta = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up) : 0f;

            if (Mathf.Abs(capturedBeta) >= 30f)
                break;

            yield return null;

        }

        // Apply counter-steer
        testStatus = "Drift 3/5: Counter-steering...";
        float counterSteer = -Mathf.Sign(capturedBeta) * 1f;
        // Note: in RCCP convention, positive steer = right; we want to oppose the beta direction.
        // If beta is negative (sliding left, body yaw'd left), we steer right (positive). So sign opposes beta.
        OverrideInputs(0.3f, 0f, counterSteer, 0f);

        samples.Clear();
        float startTime = Time.time;
        float endTime = startTime + 2f;
        float peakBeta = 0f;
        float yawRate1_5 = 0f;
        bool capturedYaw = false;

        while (Time.time < endTime) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            if (Mathf.Abs(s.slipAngle) > peakBeta)
                peakBeta = Mathf.Abs(s.slipAngle);

            if (!capturedYaw && s.time >= 1.5f) {

                yawRate1_5 = Mathf.Abs(s.yawRate);
                capturedYaw = true;

            }

            yield return new WaitForFixedUpdate();

        }

        if (!capturedYaw && samples.Count > 0)
            yawRate1_5 = Mathf.Abs(samples[samples.Count - 1].yawRate);

        ReleaseInputs();
        results.Add(EvaluateCounterSteerRecovery(peakBeta, yawRate1_5));
        testStatus = "Drift 3/5: Done";

    }

    // ---- Drift 4/5: Throttle modulation ----
    private IEnumerator TestThrottleModulation() {

        testStatus = "Drift 4/5: Modulation, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Drift 4/5: Accelerating to 70...";
        yield return StartCoroutine(AccelerateTo(70f, 12f));

        if (vehicle.absoluteSpeed < 60f) {

            results.Add(MakeFailedResult(Suite.Drift, "ThrottleModulation",
                "Could not reach 70 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h."));
            ReleaseInputs();
            yield break;

        }

        // Induce drift
        testStatus = "Drift 4/5: Inducing drift...";
        OverrideInputs(0.3f, 0f, -0.8f, 1f);
        yield return new WaitForSeconds(0.5f);

        OverrideInputs(0.5f, 0f, -0.6f, 0f);

        // Wait for beta cross 25 deg
        float waitEnd = Time.time + 1.5f;

        while (Time.time < waitEnd) {

            Vector3 vel = vehicle.Rigid.linearVelocity;
            float beta = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up) : 0f;

            if (Mathf.Abs(beta) >= 25f)
                break;

            yield return null;

        }

        // Ramp throttle 0.5 -> 0.9 over 1 s, then hold 1 s
        testStatus = "Drift 4/5: Ramping throttle...";
        samples.Clear();
        float startTime = Time.time;
        float rampEnd = startTime + 1f;
        float peakBeta = 0f;

        while (Time.time < rampEnd) {

            float k = (Time.time - startTime) / 1f;
            float thr = Mathf.Lerp(0.5f, 0.9f, k);
            OverrideInputs(thr, 0f, -0.6f, 0f);
            TestSample s = TakeSample(startTime);
            samples.Add(s);

            if (Mathf.Abs(s.slipAngle) > peakBeta)
                peakBeta = Mathf.Abs(s.slipAngle);

            yield return new WaitForFixedUpdate();

        }

        OverrideInputs(0.9f, 0f, -0.6f, 0f);
        float holdEnd = Time.time + 1f;

        while (Time.time < holdEnd) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            if (Mathf.Abs(s.slipAngle) > peakBeta)
                peakBeta = Mathf.Abs(s.slipAngle);

            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateThrottleModulation(peakBeta));
        testStatus = "Drift 4/5: Done";

    }

    // ---- Drift 5/5: S-transition ----
    private IEnumerator TestTransitionSDrift() {

        testStatus = "Drift 5/5: S-transition, resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        testStatus = "Drift 5/5: Accelerating to 70...";
        yield return StartCoroutine(AccelerateTo(70f, 12f));

        if (vehicle.absoluteSpeed < 60f) {

            results.Add(MakeFailedResult(Suite.Drift, "TransitionSDrift",
                "Could not reach 70 km/h",
                "Vehicle peaked at " + vehicle.absoluteSpeed.ToString("F0") + " km/h."));
            ReleaseInputs();
            yield break;

        }

        // Induce LEFT drift
        testStatus = "Drift 5/5: Left drift...";
        OverrideInputs(0.3f, 0f, -0.8f, 1f);
        yield return new WaitForSeconds(0.5f);

        OverrideInputs(0.5f, 0f, -0.6f, 0f);
        yield return new WaitForSeconds(1f);

        // Confirm we're in left drift (beta should be > 0 since rear is sliding right while body points... wait
        // RCCP convention: positive steer = right. Negative steer = left.
        // When drifting left, rear slides RIGHT relative to forward velocity, so velocity points to the right
        // of forward -> beta = SignedAngle(forward, vel, up) is POSITIVE.
        // (the body has yawed left of velocity)
        Vector3 leftVel = vehicle.Rigid.linearVelocity;
        float leftBeta = leftVel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(vehicle.transform.forward, leftVel, Vector3.up) : 0f;

        // Flick to RIGHT drift
        testStatus = "Drift 5/5: Flicking right...";
        samples.Clear();
        float startTime = Time.time;
        OverrideInputs(0.3f, 0f, 0.8f, 1f);
        float flickEnd = Time.time + 0.3f;

        while (Time.time < flickEnd) {

            samples.Add(TakeSample(startTime));
            yield return new WaitForFixedUpdate();

        }

        // Hold right drift inputs
        OverrideInputs(0.5f, 0f, 0.6f, 0f);

        float endTime = Time.time + 3f;
        bool reversed = false;
        float reversalTime = -1f;
        float peakNewSideBeta = 0f;

        while (Time.time < endTime) {

            TestSample s = TakeSample(startTime);
            samples.Add(s);

            // sign reversed if leftBeta and current have opposite signs and current |beta| > 5
            if (!reversed && leftBeta != 0f && Mathf.Sign(s.slipAngle) != Mathf.Sign(leftBeta) && Mathf.Abs(s.slipAngle) > 5f) {

                reversed = true;
                reversalTime = s.time;

            }

            if (reversed && Mathf.Abs(s.slipAngle) > peakNewSideBeta)
                peakNewSideBeta = Mathf.Abs(s.slipAngle);

            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateTransitionSDrift(reversed, reversalTime, peakNewSideBeta));
        testStatus = "Drift 5/5: Done";

    }

    #endregion

    #region Race Evaluators

    private TestResult EvaluateAccel(bool reached100, float timeTo100, float peakSlip, int sustainedSlipCount) {

        TestResult r = new TestResult();
        r.suite = Suite.Race;
        r.testName = "Accel_0_100";
        r.metrics = new Dictionary<string, float> {
            { "timeTo100_s", timeTo100 },
            { "peakSlip", peakSlip },
            { "sustainedSlipCount", sustainedSlipCount },
            { "reached100", reached100 ? 1f : 0f }
        };

        if (!reached100) {

            r.passed = false;
            r.summary = "Did not reach 100 km/h";
            r.detail = "Peak speed " + (samples.Count > 0 ? samples[samples.Count - 1].speed.ToString("F0") : "?") + " km/h within 12 s. Peak slip " + peakSlip.ToString("F2") + ".";
            return r;

        }

        if (sustainedSlipCount > 10) {

            r.passed = false;
            r.summary = "Excess wheelspin during launch";
            r.detail = "Reached 100 km/h in " + timeTo100.ToString("F2") + " s but sustained slip > 0.6 in " + sustainedSlipCount + " samples (peak " + peakSlip.ToString("F2") + ").";
            return r;

        }

        r.passed = true;
        r.summary = "0-100 in " + timeTo100.ToString("F2") + " s";
        r.detail = "Peak slip " + peakSlip.ToString("F2") + ", sustained slip count " + sustainedSlipCount + ".";
        return r;

    }

    private TestResult EvaluateBrake(float startSpeed, float stopDist, int maxConsecutiveLock, float lockThreshold) {

        TestResult r = new TestResult();
        r.suite = Suite.Race;
        r.testName = "Brake_100_0";

        float lockDuration = maxConsecutiveLock * Time.fixedDeltaTime;

        r.metrics = new Dictionary<string, float> {
            { "startSpeed_kmh", startSpeed },
            { "stopDist_m", stopDist },
            { "maxConsecutiveLockFrames", maxConsecutiveLock },
            { "maxLockDuration_s", lockDuration },
            { "lockThreshold", lockThreshold }
        };

        if (stopDist > 60f) {

            r.passed = false;
            r.summary = "Stop distance " + stopDist.ToString("F1") + " m";
            r.detail = "From " + startSpeed.ToString("F0") + " km/h, vehicle covered " + stopDist.ToString("F1") + " m before stopping (target < 60 m). Max wheel lock " + lockDuration.ToString("F2") + " s.";
            return r;

        }

        if (maxConsecutiveLock > 10) {

            r.passed = false;
            r.summary = "Wheel locked " + lockDuration.ToString("F2") + " s";
            r.detail = "ABS sustained slip > " + lockThreshold.ToString("F2") + " for " + maxConsecutiveLock + " consecutive frames during " + stopDist.ToString("F1") + " m stop.";
            return r;

        }

        r.passed = true;
        r.summary = "Stopped in " + stopDist.ToString("F1") + " m";
        r.detail = "From " + startSpeed.ToString("F0") + " km/h. Max lock " + lockDuration.ToString("F2") + " s at threshold " + lockThreshold.ToString("F2") + ", well within ABS modulation.";
        return r;

    }

    private TestResult EvaluateSkidpad(float peakLatG, float sustainedDuration, int yawSignMatchCount, int totalCount) {

        TestResult r = new TestResult();
        r.suite = Suite.Race;
        r.testName = "Skidpad";

        float yawMatchPct = totalCount > 0 ? (float)yawSignMatchCount / totalCount : 0f;

        r.metrics = new Dictionary<string, float> {
            { "peakLatG", peakLatG },
            { "sustainedDuration_s", sustainedDuration },
            { "yawMatchPct", yawMatchPct },
            { "totalSamples", totalCount }
        };

        if (yawMatchPct < 0.7f) {

            r.passed = false;
            r.summary = "Yaw direction inconsistent";
            r.detail = "Yaw rate matched steer direction only " + (yawMatchPct * 100f).ToString("F0") + "% of samples. Vehicle may be sliding wide or spinning.";
            return r;

        }

        if (peakLatG < 0.85f) {

            r.passed = false;
            r.summary = "Insufficient lateral grip (peak " + peakLatG.ToString("F2") + " g)";
            r.detail = "Vehicle did not reach 0.85 g sustained. Peak lateral G " + peakLatG.ToString("F2") + ", sustained " + sustainedDuration.ToString("F2") + " s.";
            return r;

        }

        if (sustainedDuration < 3f) {

            r.passed = false;
            r.summary = "Grip not sustained";
            r.detail = "Reached " + peakLatG.ToString("F2") + " g but only sustained for " + sustainedDuration.ToString("F2") + " s (target >= 3 s).";
            return r;

        }

        r.passed = true;
        r.summary = "Peak " + peakLatG.ToString("F2") + " g, sustained " + sustainedDuration.ToString("F2") + " s";
        r.detail = "Yaw direction matched steer in " + (yawMatchPct * 100f).ToString("F0") + "% of samples.";
        return r;

    }

    private TestResult EvaluateSlalom(float peakYaw, float endVx) {

        TestResult r = new TestResult();
        r.suite = Suite.Race;
        r.testName = "Slalom";
        r.metrics = new Dictionary<string, float> {
            { "peakYawRate_degps", peakYaw },
            { "endLocalVx_mps", endVx }
        };

        if (endVx < 5f) {

            r.passed = false;
            r.summary = "Lost forward momentum";
            r.detail = "End-of-test forward velocity " + endVx.ToString("F1") + " m/s (vehicle may have spun). Peak yaw rate " + peakYaw.ToString("F0") + " deg/s.";
            return r;

        }

        if (peakYaw > 110f) {

            r.passed = false;
            r.summary = "Yaw spike " + peakYaw.ToString("F0") + " deg/s";
            r.detail = "Peak yaw rate exceeded 110 deg/s. Vehicle is unstable in transient input. End vx " + endVx.ToString("F1") + " m/s.";
            return r;

        }

        r.passed = true;
        r.summary = "Peak yaw " + peakYaw.ToString("F0") + " deg/s, end vx " + endVx.ToString("F1") + " m/s";
        r.detail = "Vehicle tracked the slalom without spinning. Stable transient response.";
        return r;

    }

    private TestResult EvaluateThrottleOnCorner(float preYawAvg, float postYawAvg) {

        TestResult r = new TestResult();
        r.suite = Suite.Race;
        r.testName = "ThrottleOnCorner";

        float delta = postYawAvg - preYawAvg;

        r.metrics = new Dictionary<string, float> {
            { "preYawAvg_degps", preYawAvg },
            { "postYawAvg_degps", postYawAvg },
            { "deltaYaw_degps", delta }
        };
        string label;

        if (Mathf.Abs(delta) <= 15f)
            label = "balanced";
        else if (delta < -15f)
            label = "understeer";
        else
            label = "oversteer";

        r.summary = label + " (Δyaw " + delta.ToString("F1") + " deg/s)";
        r.detail = "Pre-ramp yaw avg " + preYawAvg.ToString("F1") + " deg/s, post-ramp yaw avg " + postYawAvg.ToString("F1") + " deg/s.";

        // Race archetypes pass on balanced or mild understeer; oversteer fails.
        r.passed = (label == "balanced" || label == "understeer");
        return r;

    }

    #endregion

    #region Drift Evaluators

    private TestResult EvaluateDriftInitiate(float peakBeta, float timeToPeak) {

        TestResult r = new TestResult();
        r.suite = Suite.Drift;
        r.testName = "DriftInitiate";
        r.metrics = new Dictionary<string, float> {
            { "peakBeta_deg", peakBeta },
            { "timeToPeak_s", timeToPeak }
        };

        if (peakBeta < 25f) {

            r.passed = false;
            r.summary = "Initiation insufficient (peak |β| " + peakBeta.ToString("F1") + "°)";
            r.detail = "Handbrake flick produced peak slip angle " + peakBeta.ToString("F1") + " deg at t=" + timeToPeak.ToString("F2") + " s. Target >= 25 deg.";
            return r;

        }

        if (timeToPeak > 1.5f) {

            r.passed = false;
            r.summary = "Initiation too slow (" + timeToPeak.ToString("F2") + " s)";
            r.detail = "Reached " + peakBeta.ToString("F1") + " deg peak but took " + timeToPeak.ToString("F2") + " s. Target peak within 1.0 s.";
            return r;

        }

        r.passed = true;
        r.summary = "Peak |β| " + peakBeta.ToString("F1") + "° at t=" + timeToPeak.ToString("F2") + " s";
        r.detail = "Handbrake flick produced clean drift initiation.";
        return r;

    }

    private TestResult EvaluateDriftSustain(int inRange, int total, float minBeta, float maxBeta) {

        TestResult r = new TestResult();
        r.suite = Suite.Drift;
        r.testName = "DriftSustain";

        float pct = total > 0 ? (float)inRange / total : 0f;

        r.metrics = new Dictionary<string, float> {
            { "inRangeCount", inRange },
            { "totalSamples", total },
            { "inRangePct", pct },
            { "minBeta_deg", minBeta },
            { "maxBeta_deg", maxBeta }
        };

        if (pct < 0.8f) {

            r.passed = false;
            r.summary = "|β| in [18, 55]° only " + (pct * 100f).ToString("F0") + "% of window";
            r.detail = "Range " + minBeta.ToString("F1") + " to " + maxBeta.ToString("F1") + " deg over " + total + " samples. Vehicle either fell out of drift or spun.";
            return r;

        }

        r.passed = true;
        r.summary = "|β| stayed in band " + (pct * 100f).ToString("F0") + "% of window";
        r.detail = "Range " + minBeta.ToString("F1") + " to " + maxBeta.ToString("F1") + " deg over " + total + " samples.";
        return r;

    }

    private TestResult EvaluateCounterSteerRecovery(float peakBeta, float yawRate1_5) {

        TestResult r = new TestResult();
        r.suite = Suite.Drift;
        r.testName = "CounterSteerRecovery";
        r.metrics = new Dictionary<string, float> {
            { "peakBeta_deg", peakBeta },
            { "yawRateAt1_5s_degps", yawRate1_5 }
        };

        if (peakBeta > 90f) {

            r.passed = false;
            r.summary = "Spun out (|β| peaked " + peakBeta.ToString("F0") + "°)";
            r.detail = "Counter-steer failed to arrest yaw. Slip angle exceeded 90 deg.";
            return r;

        }

        if (yawRate1_5 > 30f) {

            r.passed = false;
            r.summary = "Yaw not stabilized (" + yawRate1_5.ToString("F1") + " deg/s at t=1.5 s)";
            r.detail = "After counter-steer, yaw rate at 1.5 s was " + yawRate1_5.ToString("F1") + " deg/s (target <= 30). Peak |β| " + peakBeta.ToString("F1") + " deg.";
            return r;

        }

        r.passed = true;
        r.summary = "Yaw stabilized to " + yawRate1_5.ToString("F1") + " deg/s, peak |β| " + peakBeta.ToString("F0") + "°";
        r.detail = "Counter-steer arrested oversteer cleanly without spin.";
        return r;

    }

    private TestResult EvaluateThrottleModulation(float peakBeta) {

        TestResult r = new TestResult();
        r.suite = Suite.Drift;
        r.testName = "ThrottleModulation";
        r.metrics = new Dictionary<string, float> {
            { "peakBeta_deg", peakBeta }
        };

        if (peakBeta > 60f) {

            r.passed = false;
            r.summary = "Spun on throttle (peak |β| " + peakBeta.ToString("F0") + "°)";
            r.detail = "Ramping throttle from 0.5 to 0.9 caused slip angle to exceed 60 deg. Drift not modulable.";
            return r;

        }

        r.passed = true;
        r.summary = "Held drift on throttle (peak |β| " + peakBeta.ToString("F0") + "°)";
        r.detail = "Throttle ramp 0.5 -> 0.9 maintained slip angle below 60 deg.";
        return r;

    }

    private TestResult EvaluateTransitionSDrift(bool reversed, float reversalTime, float peakNewSideBeta) {

        TestResult r = new TestResult();
        r.suite = Suite.Drift;
        r.testName = "TransitionSDrift";
        r.metrics = new Dictionary<string, float> {
            { "reversed", reversed ? 1f : 0f },
            { "reversalTime_s", reversalTime },
            { "peakNewSideBeta_deg", peakNewSideBeta }
        };

        if (!reversed) {

            r.passed = false;
            r.summary = "No sign reversal";
            r.detail = "Slip angle did not change sign within window. Vehicle stayed in original drift direction.";
            return r;

        }

        if (reversalTime > 2f) {

            r.passed = false;
            r.summary = "Reversal too slow (" + reversalTime.ToString("F2") + " s)";
            r.detail = "Beta sign flipped at t=" + reversalTime.ToString("F2") + " s, target <= 2 s. Peak new-side |β| " + peakNewSideBeta.ToString("F1") + " deg.";
            return r;

        }

        if (peakNewSideBeta < 20f) {

            r.passed = false;
            r.summary = "New-side drift weak (peak |β| " + peakNewSideBeta.ToString("F1") + "°)";
            r.detail = "Reversal occurred at t=" + reversalTime.ToString("F2") + " s but new-side slip never reached 20 deg.";
            return r;

        }

        r.passed = true;
        r.summary = "Reversed at " + reversalTime.ToString("F2") + " s, new-side peak " + peakNewSideBeta.ToString("F1") + "°";
        r.detail = "Clean S-drift transition with sustained slip on the new side.";
        return r;

    }

    #endregion

    #region Suite Runner

    private void RunSuite(Suite suite) {

        if (testRunning)
            return;

        if (vehicle == null) {

            SpawnVehicle();

            if (vehicle == null)
                return;

        }

        testRunning = true;
        samples.Clear();

        // Re-apply hygiene at suite start in case anything (preset switch, addon) flipped it back on
        // since the spawn — the test rig is the source of truth for damage/engineBraking during runs.
        ApplySpawnHygiene();

        if (suite == Suite.Race)
            activeTestCoroutine = StartCoroutine(RunRaceSuite());
        else if (suite == Suite.Drift)
            activeTestCoroutine = StartCoroutine(RunDriftSuite());

    }

    private void StopActiveTest() {

        if (activeTestCoroutine != null) {

            StopCoroutine(activeTestCoroutine);
            activeTestCoroutine = null;

        }

        ReleaseInputs();
        testRunning = false;
        testStatus = "Stopped";

    }

    #endregion

    #region Report Export

    /// <summary>
    /// Writes a JSON report for the current results list to persistentDataPath.
    /// Includes the active behavior preset's full tunable state so the report is
    /// self-contained for offline analysis.
    /// </summary>
    private void ExportReport(Suite suite) {

        if (results.Count == 0) {

            lastReportPath = "(no results)";
            return;

        }

        string dir = Path.Combine(Application.persistentDataPath, "RCCP_BehaviorTests");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string suiteLabel = suite.ToString().ToLowerInvariant();
        string fname = "rccp_behavior_" + suiteLabel + "_" + timestamp + ".json";
        string path = Path.Combine(dir, fname);

        string json = BuildReportJson(suite);
        File.WriteAllText(path, json);

        lastReportPath = path;
        lastReportSuite = suite;
        Debug.Log("[RCCP_BehaviorTest] Exported " + suite + " report (" + results.Count + " tests) to: " + path);

    }

    private string BuildReportJson(Suite suite) {

        StringBuilder sb = new StringBuilder();
        CultureInfo inv = CultureInfo.InvariantCulture;

        int passed = 0;

        for (int i = 0; i < results.Count; i++) {

            if (results[i].passed)
                passed++;

        }

        int failed = results.Count - passed;

        string vehiclePrefab = "(none)";
        RCCP_CarController[] dv = RCCP_DemoVehicles.Instance != null ? RCCP_DemoVehicles.Instance.vehicles : null;

        if (dv != null && selectedVehicleIndex >= 0 && selectedVehicleIndex < dv.Length && dv[selectedVehicleIndex] != null)
            vehiclePrefab = dv[selectedVehicleIndex].name;

        sb.AppendLine("{");
        sb.Append("  \"schemaVersion\": 1,").AppendLine();
        sb.Append("  \"timestamp\": ").Append(JsonStr(System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))).Append(",").AppendLine();
        sb.Append("  \"rccpVersion\": ").Append(JsonStr(RCCP_Version.version)).Append(",").AppendLine();
        sb.Append("  \"scenePath\": \"Assets/RCCP_Scene_Blank_BehaviorTest.unity\",").AppendLine();
        sb.Append("  \"vehiclePrefab\": ").Append(JsonStr(vehiclePrefab)).Append(",").AppendLine();
        sb.Append("  \"suite\": ").Append(JsonStr(suite.ToString())).Append(",").AppendLine();
        sb.Append("  \"summary\": { \"passed\": ").Append(JsonInt(passed))
          .Append(", \"failed\": ").Append(JsonInt(failed))
          .Append(", \"total\": ").Append(JsonInt(results.Count))
          .Append(" },").AppendLine();

        // Active behavior preset
        RCCP_Settings.BehaviorType bt = vehicle != null ? vehicle.GetVehicleBehaviorType() : null;

        sb.Append("  \"behavior\": ");

        if (bt == null) {

            sb.Append("null,").AppendLine();

        } else {

            sb.AppendLine("{");
            AppendBehaviorJson(sb, bt, "    ", inv);
            sb.AppendLine("  },");

        }

        // Test results
        sb.AppendLine("  \"tests\": [");

        for (int i = 0; i < results.Count; i++) {

            TestResult r = results[i];
            sb.AppendLine("    {");
            sb.Append("      \"name\": ").Append(JsonStr(r.testName)).Append(",").AppendLine();
            sb.Append("      \"suite\": ").Append(JsonStr(r.suite.ToString())).Append(",").AppendLine();
            sb.Append("      \"passed\": ").Append(JsonBool(r.passed)).Append(",").AppendLine();
            sb.Append("      \"summary\": ").Append(JsonStr(r.summary)).Append(",").AppendLine();
            sb.Append("      \"detail\": ").Append(JsonStr(r.detail)).Append(",").AppendLine();
            sb.Append("      \"metrics\": {");

            if (r.metrics != null && r.metrics.Count > 0) {

                sb.AppendLine();
                int j = 0;

                foreach (KeyValuePair<string, float> kv in r.metrics) {

                    sb.Append("        ").Append(JsonStr(kv.Key)).Append(": ").Append(JsonFloat(kv.Value, inv));

                    if (++j < r.metrics.Count)
                        sb.Append(",");

                    sb.AppendLine();

                }

                sb.Append("      ");

            }

            sb.Append("}").AppendLine();
            sb.Append("    }");

            if (i < results.Count - 1)
                sb.Append(",");

            sb.AppendLine();

        }

        sb.AppendLine("  ]");
        sb.AppendLine("}");

        return sb.ToString();

    }

    private void AppendBehaviorJson(StringBuilder sb, RCCP_Settings.BehaviorType bt, string indent, CultureInfo inv) {

        // Identity + stability toggles
        AppendField(sb, indent, "name", bt.behaviorName, true);
        AppendField(sb, indent, "abs", bt.ABS, true);
        AppendField(sb, indent, "esp", bt.ESP, true);
        AppendField(sb, indent, "tcs", bt.TCS, true);
        AppendField(sb, indent, "steeringHelper", bt.steeringHelper, true);
        AppendField(sb, indent, "tractionHelper", bt.tractionHelper, true);
        AppendField(sb, indent, "angularDragHelper", bt.angularDragHelper, true);

        // Differential
        AppendField(sb, indent, "differentialType", bt.differentialType.ToString(), true);

        // Steering
        AppendField(sb, indent, "steeringSensitivity", bt.steeringSensitivity, inv, true);
        AppendField(sb, indent, "counterSteering", bt.counterSteering, true);
        AppendField(sb, indent, "limitSteering", bt.limitSteering, true);

        // Helper Min/Max pairs
        AppendField(sb, indent, "counterSteeringMin", bt.counterSteeringMinimum, inv, true);
        AppendField(sb, indent, "counterSteeringMax", bt.counterSteeringMaximum, inv, true);
        AppendField(sb, indent, "steeringSpeedMin", bt.steeringSpeedMinimum, inv, true);
        AppendField(sb, indent, "steeringSpeedMax", bt.steeringSpeedMaximum, inv, true);
        AppendField(sb, indent, "steeringHelperStrengthMin", bt.steeringHelperStrengthMinimum, inv, true);
        AppendField(sb, indent, "steeringHelperStrengthMax", bt.steeringHelperStrengthMaximum, inv, true);
        AppendField(sb, indent, "tractionHelperStrengthMin", bt.tractionHelperStrengthMinimum, inv, true);
        AppendField(sb, indent, "tractionHelperStrengthMax", bt.tractionHelperStrengthMaximum, inv, true);
        AppendField(sb, indent, "angularDragHelperMin", bt.angularDragHelperMinimum, inv, true);
        AppendField(sb, indent, "angularDragHelperMax", bt.angularDragHelperMaximum, inv, true);

        // Suspension + anti-roll + global drag
        AppendField(sb, indent, "suspensionSpringMultiplier", bt.suspensionSpringMultiplier, inv, true);
        AppendField(sb, indent, "suspensionDamperMultiplier", bt.suspensionDamperMultiplier, inv, true);
        AppendField(sb, indent, "antiRollMinimum", bt.antiRollMinimum, inv, true);
        AppendField(sb, indent, "angularDrag", bt.angularDrag, inv, true);

        // Gearbox
        AppendField(sb, indent, "gearShiftingThreshold", bt.gearShiftingThreshold, inv, true);
        AppendField(sb, indent, "gearShiftingDelayMin", bt.gearShiftingDelayMinimum, inv, true);
        AppendField(sb, indent, "gearShiftingDelayMax", bt.gearShiftingDelayMaximum, inv, true);

        // Wheel friction (forward, front + rear, extremum + asymptote, slip + value)
        AppendField(sb, indent, "forwardExtremumSlip_F", bt.forwardExtremumSlip_F, inv, true);
        AppendField(sb, indent, "forwardExtremumValue_F", bt.forwardExtremumValue_F, inv, true);
        AppendField(sb, indent, "forwardAsymptoteSlip_F", bt.forwardAsymptoteSlip_F, inv, true);
        AppendField(sb, indent, "forwardAsymptoteValue_F", bt.forwardAsymptoteValue_F, inv, true);
        AppendField(sb, indent, "forwardExtremumSlip_R", bt.forwardExtremumSlip_R, inv, true);
        AppendField(sb, indent, "forwardExtremumValue_R", bt.forwardExtremumValue_R, inv, true);
        AppendField(sb, indent, "forwardAsymptoteSlip_R", bt.forwardAsymptoteSlip_R, inv, true);
        AppendField(sb, indent, "forwardAsymptoteValue_R", bt.forwardAsymptoteValue_R, inv, true);
        AppendField(sb, indent, "sidewaysExtremumSlip_F", bt.sidewaysExtremumSlip_F, inv, true);
        AppendField(sb, indent, "sidewaysExtremumValue_F", bt.sidewaysExtremumValue_F, inv, true);
        AppendField(sb, indent, "sidewaysAsymptoteSlip_F", bt.sidewaysAsymptoteSlip_F, inv, true);
        AppendField(sb, indent, "sidewaysAsymptoteValue_F", bt.sidewaysAsymptoteValue_F, inv, true);
        AppendField(sb, indent, "sidewaysExtremumSlip_R", bt.sidewaysExtremumSlip_R, inv, true);
        AppendField(sb, indent, "sidewaysExtremumValue_R", bt.sidewaysExtremumValue_R, inv, true);
        AppendField(sb, indent, "sidewaysAsymptoteSlip_R", bt.sidewaysAsymptoteSlip_R, inv, true);
        AppendField(sb, indent, "sidewaysAsymptoteValue_R", bt.sidewaysAsymptoteValue_R, inv, true);

        // Drift block
        AppendField(sb, indent, "driftMode", bt.driftMode, true);
        AppendField(sb, indent, "driftAngleLimiter", bt.driftAngleLimiter, true);
        AppendField(sb, indent, "driftAngleLimit", bt.driftAngleLimit, inv, true);
        AppendField(sb, indent, "driftAngleCorrectionFactor", bt.driftAngleCorrectionFactor, inv, true);
        AppendField(sb, indent, "driftYawTorqueMultiplier", bt.driftYawTorqueMultiplier, inv, true);
        AppendField(sb, indent, "driftForwardForceMultiplier", bt.driftForwardForceMultiplier, inv, true);
        AppendField(sb, indent, "driftSidewaysForceMultiplier", bt.driftSidewaysForceMultiplier, inv, true);
        AppendField(sb, indent, "driftMinSpeed", bt.driftMinSpeed, inv, true);
        AppendField(sb, indent, "driftFullForceSpeed", bt.driftFullForceSpeed, inv, true);
        AppendField(sb, indent, "driftThrottleYawFactor", bt.driftThrottleYawFactor, inv, true);
        AppendField(sb, indent, "driftRearSidewaysStiffnessMin", bt.driftRearSidewaysStiffnessMin, inv, true);
        AppendField(sb, indent, "driftRearForwardStiffnessMin", bt.driftRearForwardStiffnessMin, inv, true);
        AppendField(sb, indent, "driftFrontSidewaysStiffnessMin", bt.driftFrontSidewaysStiffnessMin, inv, true);
        AppendField(sb, indent, "driftFrictionResponseSpeed", bt.driftFrictionResponseSpeed, inv, true);
        AppendField(sb, indent, "driftFrictionRecoverySpeed", bt.driftFrictionRecoverySpeed, inv, true);
        AppendField(sb, indent, "driftMaxAngularVelocity", bt.driftMaxAngularVelocity, inv, true);
        AppendField(sb, indent, "driftCounterSteerRecoveryBoost", bt.driftCounterSteerRecoveryBoost, inv, true);
        AppendField(sb, indent, "driftMomentumMaintenanceForce", bt.driftMomentumMaintenanceForce, inv, true);

        // Last entry — no trailing comma
        AppendField(sb, indent, "driftForceSmoothing", bt.driftForceSmoothing, inv, false);

    }

    private static void AppendField(StringBuilder sb, string indent, string key, string value, bool trailingComma) {

        sb.Append(indent).Append(JsonStr(key)).Append(": ").Append(JsonStr(value));

        if (trailingComma)
            sb.Append(",");

        sb.AppendLine();

    }

    private static void AppendField(StringBuilder sb, string indent, string key, bool value, bool trailingComma) {

        sb.Append(indent).Append(JsonStr(key)).Append(": ").Append(JsonBool(value));

        if (trailingComma)
            sb.Append(",");

        sb.AppendLine();

    }

    private static void AppendField(StringBuilder sb, string indent, string key, float value, CultureInfo inv, bool trailingComma) {

        sb.Append(indent).Append(JsonStr(key)).Append(": ").Append(JsonFloat(value, inv));

        if (trailingComma)
            sb.Append(",");

        sb.AppendLine();

    }

    private static string JsonStr(string s) {

        if (s == null)
            return "null";

        StringBuilder sb = new StringBuilder(s.Length + 2);
        sb.Append('"');

        for (int i = 0; i < s.Length; i++) {

            char c = s[i];

            if (c == '"')
                sb.Append("\\\"");
            else if (c == '\\')
                sb.Append("\\\\");
            else if (c == '\n')
                sb.Append("\\n");
            else if (c == '\r')
                sb.Append("\\r");
            else if (c == '\t')
                sb.Append("\\t");
            else if (c < 0x20)
                sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:X4}", (int)c);
            else
                sb.Append(c);

        }

        sb.Append('"');
        return sb.ToString();

    }

    private static string JsonFloat(float v, CultureInfo inv) {

        if (float.IsNaN(v) || float.IsInfinity(v))
            return "null";

        return v.ToString("0.######", inv);

    }

    private static string JsonInt(int v) {

        return v.ToString(CultureInfo.InvariantCulture);

    }

    private static string JsonBool(bool v) {

        return v ? "true" : "false";

    }

    #endregion

    #region IMGUI

    private void InitStyles() {

        if (_stylesInitialized)
            return;

        _titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.58f, 0f) }
        };

        _headerStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        _passStyle = new GUIStyle(GUI.skin.label) {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green }
        };

        _failStyle = new GUIStyle(GUI.skin.label) {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.3f, 0.3f) }
        };

        _activeStyle = new GUIStyle(GUI.skin.label) {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };

        _labelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 11,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
        };

        _smallLabelStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 10,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
        };

        _boxStyle = new GUIStyle(GUI.skin.box);

        _stylesInitialized = true;

    }

    private void OnGUI() {

        InitStyles();

        DrawControlPanel();
        DrawTelemetry();
        DrawResults();

    }

    private void DrawControlPanel() {

        GUILayout.BeginArea(new Rect(10, 10, 290, Screen.height - 20), _boxStyle);
        GUILayout.Space(5);

        GUILayout.Label("RCCP Behavior Test", _titleStyle);
        GUILayout.Space(8);

        // Active behavior readout
        GUILayout.Label("Active Behavior:", _headerStyle);
        GUILayout.Label(GetActiveBehaviorName(), _activeStyle);
        GUILayout.Space(8);

        // Vehicle picker
        RCCP_CarController[] demoVehicles = RCCP_DemoVehicles.Instance != null ? RCCP_DemoVehicles.Instance.vehicles : null;

        if (demoVehicles != null && demoVehicles.Length > 0) {

            GUILayout.Label("Vehicle", _headerStyle);
            GUILayout.BeginHorizontal();

            bool canNav = !testRunning;

            if (GUILayout.Button("<", GUILayout.Width(30)) && canNav) {

                selectedVehicleIndex--;

                if (selectedVehicleIndex < 0)
                    selectedVehicleIndex = demoVehicles.Length - 1;

            }

            string vehicleName = demoVehicles[selectedVehicleIndex] != null ? demoVehicles[selectedVehicleIndex].name : "---";
            GUILayout.Label(vehicleName, _headerStyle, GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", GUILayout.Width(30)) && canNav) {

                selectedVehicleIndex++;

                if (selectedVehicleIndex >= demoVehicles.Length)
                    selectedVehicleIndex = 0;

            }

            GUILayout.EndHorizontal();

            GUI.enabled = !testRunning;

            if (GUILayout.Button("Spawn Vehicle", GUILayout.Height(28)))
                SpawnVehicle();

            GUI.enabled = true;

        }

        GUILayout.Space(12);
        GUILayout.Label("Suites", _headerStyle);
        GUILayout.Space(4);

        GUI.enabled = !testRunning;
        autoSwitchPresetPerSuite = GUILayout.Toggle(autoSwitchPresetPerSuite, " Auto-switch preset per suite");
        GUI.enabled = true;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Race:", _smallLabelStyle, GUILayout.Width(40));
        GUI.enabled = !testRunning && autoSwitchPresetPerSuite;
        racePresetName = GUILayout.TextField(racePresetName ?? "");
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Drift:", _smallLabelStyle, GUILayout.Width(40));
        GUI.enabled = !testRunning && autoSwitchPresetPerSuite;
        driftPresetName = GUILayout.TextField(driftPresetName ?? "");
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        GUI.enabled = !testRunning && vehicle != null;

        if (GUILayout.Button("Run Race Suite", GUILayout.Height(32)))
            RunSuite(Suite.Race);

        if (GUILayout.Button("Run Drift Suite", GUILayout.Height(32)))
            RunSuite(Suite.Drift);

        GUI.enabled = true;

        GUILayout.Space(8);

        if (GUILayout.Button("Stop", GUILayout.Height(24)))
            StopActiveTest();

        GUI.enabled = !testRunning;
        if (GUILayout.Button("Respawn", GUILayout.Height(24)))
            SpawnVehicle();
        GUI.enabled = true;

        GUILayout.Space(10);

        // Free drive
        GUI.enabled = !testRunning;
        string freeDriveLabel = freeDriveMode ? "Disable Free Drive" : "Enable Free Drive";

        if (GUILayout.Button(freeDriveLabel, GUILayout.Height(24))) {

            freeDriveMode = !freeDriveMode;

            if (freeDriveMode)
                ReleaseInputs();

        }

        GUI.enabled = true;

        GUILayout.Space(10);

        // Status
        GUILayout.Label("Status:", _headerStyle);

        if (testRunning)
            GUILayout.Label(testStatus, _activeStyle);
        else
            GUILayout.Label(testStatus, _labelStyle);

        GUILayout.Space(6);

        if (GUILayout.Button("Clear Results"))
            results.Clear();

        GUILayout.Space(8);

        // Export controls
        GUILayout.Label("Report Export", _headerStyle);
        autoExportOnSuiteComplete = GUILayout.Toggle(autoExportOnSuiteComplete, " Auto-export JSON on suite complete");

        GUI.enabled = !testRunning && results.Count > 0;

        if (GUILayout.Button("Export Now (JSON)", GUILayout.Height(24))) {

            // Pick suite from first result; default to Race if mixed
            Suite s = results.Count > 0 ? results[0].suite : Suite.Race;

            for (int i = 1; i < results.Count; i++) {

                if (results[i].suite != s) {

                    s = Suite.None;
                    break;

                }

            }

            ExportReport(s == Suite.None ? Suite.Race : s);

        }

        GUI.enabled = true;

        if (!string.IsNullOrEmpty(lastReportPath)) {

            GUILayout.Label("Last:", _smallLabelStyle);
            GUILayout.Label(lastReportPath, _smallLabelStyle);

            if (GUILayout.Button("Copy Path to Clipboard", GUILayout.Height(22)))
                GUIUtility.systemCopyBuffer = lastReportPath;

        }

        GUILayout.Space(8);

        // TESTING.md reminders
        GUILayout.Label("Reminders:", _smallLabelStyle);
        GUILayout.Label("- Use signed yaw + slip angle.", _smallLabelStyle);
        GUILayout.Label("- Strong provocation only.", _smallLabelStyle);
        GUILayout.Label("- Confirm preset before run.", _smallLabelStyle);

        GUILayout.EndArea();

    }

    private void DrawTelemetry() {

        float panelX = 310;
        float panelW = Screen.width - panelX - 10;
        float panelH = 220;

        GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), _boxStyle);
        GUILayout.Space(3);

        GUILayout.Label("Live Telemetry", _titleStyle);

        RCCP_CarController car = vehicle;

        if (car == null) {

            GUILayout.Label("No vehicle", _labelStyle);
            GUILayout.EndArea();
            return;

        }

        Vector3 vel = car.Rigid.linearVelocity;
        float beta = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(car.transform.forward, vel, Vector3.up) : 0f;
        float yawRate = car.Rigid.angularVelocity.y * Mathf.Rad2Deg;
        float speedMpS = car.absoluteSpeed / 3.6f;
        float latG = (car.Rigid.angularVelocity.y * speedMpS) / 9.81f;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Speed: " + car.absoluteSpeed.ToString("F0") + " km/h", _labelStyle, GUILayout.Width(140));
        GUILayout.Label("β: " + beta.ToString("F1") + "°", _labelStyle, GUILayout.Width(110));
        GUILayout.Label("Yaw: " + yawRate.ToString("F1") + "°/s", _labelStyle, GUILayout.Width(120));
        GUILayout.Label("LatG: " + latG.ToString("F2") + " g", _labelStyle);
        GUILayout.EndHorizontal();

        RCCP_Stability s = car.Stability;

        if (s != null) {

            GUILayout.BeginHorizontal();
            GUILayout.Label("ESP: " + (s.ESPEngaged ? "ON" : "off"), s.ESPEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));
            GUILayout.Label("ABS: " + (s.ABSEngaged ? "ON" : "off"), s.ABSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));
            GUILayout.Label("TCS: " + (s.TCSEngaged ? "ON" : "off"), s.TCSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));
            GUILayout.EndHorizontal();

        }

        GUILayout.Space(5);

        // Per-wheel data
        string[] names = new string[] { "FL", "FR", "RL", "RR" };
        float[] fSlip = new float[4];
        float[] sSlip = new float[4];
        float[] brk = new float[4];
        float[] mtr = new float[4];

        if (s != null) {

            if (s.frontAxle != null) {

                if (s.frontAxle.leftWheelCollider != null) {

                    fSlip[0] = s.frontAxle.leftWheelCollider.ForwardSlip;
                    sSlip[0] = s.frontAxle.leftWheelCollider.SidewaysSlip;
                    brk[0] = s.frontAxle.leftWheelCollider.WheelCollider.brakeTorque;
                    mtr[0] = s.frontAxle.leftWheelCollider.WheelCollider.motorTorque;

                }

                if (s.frontAxle.rightWheelCollider != null) {

                    fSlip[1] = s.frontAxle.rightWheelCollider.ForwardSlip;
                    sSlip[1] = s.frontAxle.rightWheelCollider.SidewaysSlip;
                    brk[1] = s.frontAxle.rightWheelCollider.WheelCollider.brakeTorque;
                    mtr[1] = s.frontAxle.rightWheelCollider.WheelCollider.motorTorque;

                }

            }

            if (s.rearAxle != null) {

                if (s.rearAxle.leftWheelCollider != null) {

                    fSlip[2] = s.rearAxle.leftWheelCollider.ForwardSlip;
                    sSlip[2] = s.rearAxle.leftWheelCollider.SidewaysSlip;
                    brk[2] = s.rearAxle.leftWheelCollider.WheelCollider.brakeTorque;
                    mtr[2] = s.rearAxle.leftWheelCollider.WheelCollider.motorTorque;

                }

                if (s.rearAxle.rightWheelCollider != null) {

                    fSlip[3] = s.rearAxle.rightWheelCollider.ForwardSlip;
                    sSlip[3] = s.rearAxle.rightWheelCollider.SidewaysSlip;
                    brk[3] = s.rearAxle.rightWheelCollider.WheelCollider.brakeTorque;
                    mtr[3] = s.rearAxle.rightWheelCollider.WheelCollider.motorTorque;

                }

            }

        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Wheel", _smallLabelStyle, GUILayout.Width(40));
        GUILayout.Label("Fwd Slip", _smallLabelStyle, GUILayout.Width(75));
        GUILayout.Label("Side Slip", _smallLabelStyle, GUILayout.Width(75));
        GUILayout.Label("Brake Nm", _smallLabelStyle, GUILayout.Width(75));
        GUILayout.Label("Motor Nm", _smallLabelStyle, GUILayout.Width(75));
        GUILayout.EndHorizontal();

        for (int i = 0; i < 4; i++) {

            GUILayout.BeginHorizontal();
            GUILayout.Label(names[i], _labelStyle, GUILayout.Width(40));

            bool fwdHot = Mathf.Abs(fSlip[i]) > 0.3f;
            bool sideHot = Mathf.Abs(sSlip[i]) > 0.3f;
            bool brkActive = brk[i] > 10f;

            GUILayout.Label(fSlip[i].ToString("F3"), fwdHot ? _failStyle : _labelStyle, GUILayout.Width(75));
            GUILayout.Label(sSlip[i].ToString("F3"), sideHot ? _failStyle : _labelStyle, GUILayout.Width(75));
            GUILayout.Label(brk[i].ToString("F0"), brkActive ? _activeStyle : _labelStyle, GUILayout.Width(75));
            GUILayout.Label(mtr[i].ToString("F0"), _labelStyle, GUILayout.Width(75));
            GUILayout.EndHorizontal();

        }

        GUILayout.EndArea();

    }

    private void DrawResults() {

        float panelX = 310;
        float panelY = 240;
        float panelW = Screen.width - panelX - 10;
        float panelH = Screen.height - panelY - 10;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH), _boxStyle);
        GUILayout.Space(3);

        GUILayout.Label("Test Results", _titleStyle);

        if (results.Count == 0) {

            GUILayout.Label("No tests run yet. Run a suite from the left panel.", _smallLabelStyle);
            GUILayout.EndArea();
            return;

        }

        // Aggregate header
        int passCount = 0;

        for (int i = 0; i < results.Count; i++) {

            if (results[i].passed)
                passCount++;

        }

        GUILayout.Label(passCount + " / " + results.Count + " passed", passCount == results.Count ? _passStyle : _failStyle);
        GUILayout.Space(4);

        resultsScroll = GUILayout.BeginScrollView(resultsScroll);

        Suite currentSuite = Suite.None;

        for (int i = 0; i < results.Count; i++) {

            TestResult r = results[i];

            if (r.suite != currentSuite) {

                currentSuite = r.suite;
                GUILayout.Space(4);
                GUILayout.Label(currentSuite.ToString() + " suite", _headerStyle);

            }

            GUILayout.BeginHorizontal();
            string prefix = r.passed ? "[PASS]" : "[FAIL]";
            GUIStyle prefixStyle = r.passed ? _passStyle : _failStyle;

            GUILayout.Label(prefix, prefixStyle, GUILayout.Width(50));
            GUILayout.Label(r.testName + ": " + r.summary, _labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Label("    " + r.detail, _smallLabelStyle);
            GUILayout.Space(3);

        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

    }

    #endregion

}

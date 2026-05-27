//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Automated stability test suite for RCCP vehicles. Validates ESP, ABS, TCS
/// and helper systems with real-time telemetry. Supports any vehicle configuration
/// (FWD, RWD, AWD). Includes free-drive mode for manual testing.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Demo/RCCP Stability Test")]
public class RCCP_StabilityTest : RCCP_GenericComponent {

    #region Data Structures

    private enum TestType { Oversteer, Understeer, ABS, TCS }

    private struct TestSample {

        [Tooltip("Elapsed time in seconds since the test started.")]
        public float time;
        [Tooltip("Vehicle speed in km/h at the time of this sample.")]
        public float speed;
        [Tooltip("Angle in degrees between velocity and forward direction.")]
        public float driftAngle;
        [Tooltip("Rotational velocity around the up axis in degrees per second.")]
        public float yawRate;
        [Tooltip("Whether ESP stability control was active during this sample.")]
        public bool espEngaged;
        [Tooltip("Whether ABS anti-lock braking was active during this sample.")]
        public bool absEngaged;
        [Tooltip("Whether TCS traction control was active during this sample.")]
        public bool tcsEngaged;

        // Per wheel [FL, FR, RL, RR]
        [Tooltip("Forward slip ratio per wheel [FL, FR, RL, RR].")]
        public float[] forwardSlip;
        [Tooltip("Sideways slip ratio per wheel [FL, FR, RL, RR].")]
        public float[] sidewaysSlip;
        [Tooltip("Brake torque applied per wheel [FL, FR, RL, RR] in Nm.")]
        public float[] brakeTorque;
        [Tooltip("Motor torque applied per wheel [FL, FR, RL, RR] in Nm.")]
        public float[] motorTorque;

    }

    private struct TestResult {

        [Tooltip("Which stability test scenario this result belongs to.")]
        public TestType testType;
        [Tooltip("Whether the vehicle passed the stability criteria.")]
        public bool passed;
        [Tooltip("One-line pass/fail summary of the test outcome.")]
        public string summary;
        [Tooltip("Detailed explanation of measured values and thresholds.")]
        public string detail;

    }

    #endregion

    #region State

    private int selectedVehicleIndex = 0;
    private RCCP_CarController vehicle;
    private Transform spawnPoint;

    private bool testRunning = false;
    private string testStatus = "Idle";
    private bool freeDriveMode = false;
    private Coroutine activeTestCoroutine;

    private List<TestSample> samples = new List<TestSample>();
    private List<TestResult> results = new List<TestResult>();
    private Vector2 resultsScroll;

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

        // Find spawn point in scene (plain Transform named RCCP_SpawnPoint)
        GameObject sp = GameObject.Find("RCCP_SpawnPoint");

        if (sp != null)
            spawnPoint = sp.transform;

    }

    private void Start() {

        SpawnVehicle();

    }

    #region Vehicle Management

    private void SpawnVehicle() {

        RCCP_CarController[] demoVehicles = RCCP_DemoVehicles.Instance.vehicles;

        if (demoVehicles == null || demoVehicles.Length == 0)
            return;

        selectedVehicleIndex = Mathf.Clamp(selectedVehicleIndex, 0, demoVehicles.Length - 1);

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.up;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        // Destroy current vehicle
        RCCP_CarController current = RCCPSceneManager.activePlayerVehicle;

        if (current != null) {

            RCCP.DeRegisterPlayerVehicle();
            Destroy(current.gameObject);

        }

        vehicle = RCCP.SpawnRCC(demoVehicles[selectedVehicleIndex], pos + Vector3.up * 0.5f, rot, true, true, true);

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

    #endregion

    #region Sampling

    private TestSample TakeSample() {

        TestSample s = new TestSample();
        s.time = Time.time;
        s.forwardSlip = new float[4];
        s.sidewaysSlip = new float[4];
        s.brakeTorque = new float[4];
        s.motorTorque = new float[4];

        if (vehicle == null)
            return s;

        s.speed = vehicle.absoluteSpeed;
        s.yawRate = vehicle.Rigid.angularVelocity.y * Mathf.Rad2Deg;

        Vector3 vel = vehicle.Rigid.linearVelocity;

        if (vel.sqrMagnitude > 0.1f)
            s.driftAngle = Vector3.SignedAngle(vehicle.transform.forward, vel, Vector3.up);

        RCCP_Stability stability = vehicle.Stability;

        if (stability != null) {

            s.espEngaged = stability.ESPEngaged;
            s.absEngaged = stability.ABSEngaged;
            s.tcsEngaged = stability.TCSEngaged;

            // FL
            if (stability.frontAxle != null && stability.frontAxle.leftWheelCollider != null) {

                s.forwardSlip[0] = stability.frontAxle.leftWheelCollider.ForwardSlip;
                s.sidewaysSlip[0] = stability.frontAxle.leftWheelCollider.SidewaysSlip;
                s.brakeTorque[0] = stability.frontAxle.leftWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[0] = stability.frontAxle.leftWheelCollider.WheelCollider.motorTorque;

            }

            // FR
            if (stability.frontAxle != null && stability.frontAxle.rightWheelCollider != null) {

                s.forwardSlip[1] = stability.frontAxle.rightWheelCollider.ForwardSlip;
                s.sidewaysSlip[1] = stability.frontAxle.rightWheelCollider.SidewaysSlip;
                s.brakeTorque[1] = stability.frontAxle.rightWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[1] = stability.frontAxle.rightWheelCollider.WheelCollider.motorTorque;

            }

            // RL
            if (stability.rearAxle != null && stability.rearAxle.leftWheelCollider != null) {

                s.forwardSlip[2] = stability.rearAxle.leftWheelCollider.ForwardSlip;
                s.sidewaysSlip[2] = stability.rearAxle.leftWheelCollider.SidewaysSlip;
                s.brakeTorque[2] = stability.rearAxle.leftWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[2] = stability.rearAxle.leftWheelCollider.WheelCollider.motorTorque;

            }

            // RR
            if (stability.rearAxle != null && stability.rearAxle.rightWheelCollider != null) {

                s.forwardSlip[3] = stability.rearAxle.rightWheelCollider.ForwardSlip;
                s.sidewaysSlip[3] = stability.rearAxle.rightWheelCollider.SidewaysSlip;
                s.brakeTorque[3] = stability.rearAxle.rightWheelCollider.WheelCollider.brakeTorque;
                s.motorTorque[3] = stability.rearAxle.rightWheelCollider.WheelCollider.motorTorque;

            }

        }

        return s;

    }

    #endregion

    #region Test Coroutines

    private void RunTest(TestType type) {

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

        switch (type) {

            case TestType.Oversteer:
                activeTestCoroutine = StartCoroutine(TestOversteer());
                break;

            case TestType.Understeer:
                activeTestCoroutine = StartCoroutine(TestUndersteer());
                break;

            case TestType.ABS:
                activeTestCoroutine = StartCoroutine(TestABS());
                break;

            case TestType.TCS:
                activeTestCoroutine = StartCoroutine(TestTCS());
                break;

        }

    }

    private void RunAllTests() {

        if (testRunning)
            return;

        testRunning = true;
        activeTestCoroutine = StartCoroutine(RunAllSequential());

    }

    private IEnumerator RunAllSequential() {

        results.Clear();

        yield return StartCoroutine(TestOversteer());
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(TestUndersteer());
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(TestABS());
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(TestTCS());

        testRunning = false;
        testStatus = "All tests complete";

    }

    // ---- Oversteer: accelerate, then full throttle + steer left ----
    private IEnumerator TestOversteer() {

        testStatus = "Oversteer: Resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        // Accelerate
        testStatus = "Oversteer: Accelerating to 100 km/h...";
        OverrideInputs(1f, 0f, 0f, 0f);

        float timeout = Time.time + 15f;

        while (vehicle.absoluteSpeed < 100f && Time.time < timeout)
            yield return null;

        // Provoke: full throttle + steer left (positive steer in RCCP = right, so use negative for left)
        // Then inject a mild rear-breakaway state so grippy cars reliably enter true oversteer
        // instead of just saturating the front axle at high speed.
        testStatus = "Oversteer: Provoking (throttle + steer)...";
        OverrideInputs(1f, 0f, -0.9f, 0f);

        // Force a left-turn oversteer state:
        // - velocity points slightly to the right of the car's forward axis (positive drift angle)
        // - body yaw is already rotating left (negative yaw rate)
        Vector3 oversteerVelocity = Quaternion.AngleAxis(12f, Vector3.up) * vehicle.transform.forward * (vehicle.absoluteSpeed / 3.6f);
        vehicle.Rigid.linearVelocity = oversteerVelocity;
        vehicle.Rigid.angularVelocity = new Vector3(vehicle.Rigid.angularVelocity.x, -1.0f, vehicle.Rigid.angularVelocity.z);

        samples.Clear();
        float endTime = Time.time + 3f;

        while (Time.time < endTime) {

            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateOversteer());

        testRunning = false;
        testStatus = "Oversteer: Complete";

    }

    // ---- Understeer: accelerate, then throttle + steer right ----
    private IEnumerator TestUndersteer() {

        testStatus = "Understeer: Resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        // Accelerate
        testStatus = "Understeer: Accelerating to 120 km/h...";
        OverrideInputs(1f, 0f, 0f, 0f);

        float timeout = Time.time + 20f;

        while (vehicle.absoluteSpeed < 120f && Time.time < timeout) {

            // If speed plateaus below target, boost velocity directly
            if (Time.time > timeout - 5f && vehicle.absoluteSpeed < 80f)
                vehicle.Rigid.linearVelocity = vehicle.transform.forward * (120f / 3.6f);

            yield return null;

        }

        // Provoke: throttle + full right steer
        testStatus = "Understeer: Provoking (throttle + steer)...";
        OverrideInputs(0.7f, 0f, 1f, 0f);

        samples.Clear();
        float endTime = Time.time + 3f;

        while (Time.time < endTime) {

            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateUndersteer());

        testRunning = false;
        testStatus = "Understeer: Complete";

    }

    // ---- ABS: accelerate, then full brake ----
    private IEnumerator TestABS() {

        testStatus = "ABS: Resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        // Accelerate
        testStatus = "ABS: Accelerating to 80 km/h...";
        OverrideInputs(1f, 0f, 0f, 0f);

        float timeout = Time.time + 12f;

        while (vehicle.absoluteSpeed < 80f && Time.time < timeout)
            yield return null;

        // Full brake
        testStatus = "ABS: Full braking...";
        OverrideInputs(0f, 1f, 0f, 0f);

        samples.Clear();
        float endTime = Time.time + 3f;

        while (Time.time < endTime) {

            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateABS());

        testRunning = false;
        testStatus = "ABS: Complete";

    }

    // ---- TCS: from standstill, full throttle ----
    private IEnumerator TestTCS() {

        testStatus = "TCS: Resetting...";
        ResetVehicle();
        yield return new WaitForSeconds(0.5f);

        // Ensure stopped
        testStatus = "TCS: Holding brakes...";
        OverrideInputs(0f, 1f, 0f, 0f);
        yield return new WaitForSeconds(1f);

        // Full throttle from standstill
        testStatus = "TCS: Full throttle launch...";
        OverrideInputs(1f, 0f, 0f, 0f);

        samples.Clear();
        float endTime = Time.time + 4f;

        while (Time.time < endTime) {

            samples.Add(TakeSample());
            yield return new WaitForFixedUpdate();

        }

        ReleaseInputs();
        results.Add(EvaluateTCS());

        testRunning = false;
        testStatus = "TCS: Complete";

    }

    #endregion

    #region Evaluation

    private TestResult EvaluateOversteer() {

        TestResult r = new TestResult();
        r.testType = TestType.Oversteer;

        // Steer is -0.9 (left). Rear slides right (positive rearSlip).
        // ESP should brake outside front = FRONT RIGHT (index 1).
        int espCount = 0;
        int correctBrakeCount = 0;
        float peakBrake = 0f;

        for (int i = 0; i < samples.Count; i++) {

            if (samples[i].espEngaged) {

                espCount++;

                // Check front right (outside) has more brake than front left (inside)
                if (samples[i].brakeTorque[1] > 50f && samples[i].brakeTorque[1] > samples[i].brakeTorque[0])
                    correctBrakeCount++;

                if (samples[i].brakeTorque[1] > peakBrake)
                    peakBrake = samples[i].brakeTorque[1];

            }

        }

        if (espCount < 5) {

            r.passed = false;
            r.summary = "ESP never engaged";
            r.detail = "ESP engaged in only " + espCount + " of " + samples.Count + " samples. Rear slip may not have exceeded threshold.";

        } else if (correctBrakeCount < espCount * 0.3f) {

            r.passed = false;
            r.summary = "ESP braked wrong wheel";
            r.detail = "ESP engaged " + espCount + "x but front-right (outside) braked correctly only " + correctBrakeCount + "x.";

        } else {

            r.passed = true;
            r.summary = "ESP braked front-right (outside), peak " + peakBrake.ToString("F0") + " Nm";
            r.detail = "ESP engaged " + espCount + "/" + samples.Count + " samples, correct wheel " + correctBrakeCount + "x.";

        }

        return r;

    }

    private TestResult EvaluateUndersteer() {

        TestResult r = new TestResult();
        r.testType = TestType.Understeer;

        // Steer is +1.0 (right turn). ESP should brake inside rear = REAR RIGHT (index 3).
        int espCount = 0;
        int correctBrakeCount = 0;
        float peakBrake = 0f;

        for (int i = 0; i < samples.Count; i++) {

            if (samples[i].espEngaged) {

                espCount++;

                // Turning right → inner rear = right rear (index 3)
                if (samples[i].brakeTorque[3] > 50f && samples[i].brakeTorque[3] > samples[i].brakeTorque[2])
                    correctBrakeCount++;

                if (samples[i].brakeTorque[3] > peakBrake)
                    peakBrake = samples[i].brakeTorque[3];

            }

        }

        if (espCount < 5) {

            r.passed = false;
            r.summary = "ESP never engaged";
            r.detail = "ESP engaged in only " + espCount + " of " + samples.Count + " samples.";

        } else if (correctBrakeCount < espCount * 0.3f) {

            r.passed = false;
            r.summary = "ESP braked wrong wheel";
            r.detail = "ESP engaged " + espCount + "x but inside rear braked correctly only " + correctBrakeCount + "x.";

        } else {

            r.passed = true;
            r.summary = "ESP braked rear-right (inside), peak " + peakBrake.ToString("F0") + " Nm";
            r.detail = "ESP engaged " + espCount + "/" + samples.Count + " samples, correct wheel " + correctBrakeCount + "x.";

        }

        return r;

    }

    private TestResult EvaluateABS() {

        TestResult r = new TestResult();
        r.testType = TestType.ABS;

        int absCount = 0;
        int maxConsecutiveLock = 0;
        string lockedWheel = "";
        float threshold = 0.35f;

        if (vehicle != null && vehicle.Stability != null)
            threshold = vehicle.Stability.engageABSThreshold;

        // Track consecutive lock per wheel
        int[] consecutiveLock = new int[4];
        string[] wheelNames = new string[] { "FL", "FR", "RL", "RR" };

        for (int i = 0; i < samples.Count; i++) {

            if (samples[i].absEngaged)
                absCount++;

            for (int w = 0; w < 4; w++) {

                if (Mathf.Abs(samples[i].forwardSlip[w]) > threshold) {

                    consecutiveLock[w]++;

                    if (consecutiveLock[w] > maxConsecutiveLock) {

                        maxConsecutiveLock = consecutiveLock[w];
                        lockedWheel = wheelNames[w];

                    }

                } else {

                    consecutiveLock[w] = 0;

                }

            }

        }

        float lockDuration = maxConsecutiveLock * Time.fixedDeltaTime;

        if (absCount < 5) {

            r.passed = false;
            r.summary = "ABS never engaged";
            r.detail = "ABS engaged in only " + absCount + " of " + samples.Count + " samples.";

        } else if (maxConsecutiveLock > 10) {

            r.passed = false;
            r.summary = lockedWheel + " locked for " + lockDuration.ToString("F2") + "s";
            r.detail = "ABS engaged " + absCount + "x but " + lockedWheel + " sustained lock for " + maxConsecutiveLock + " consecutive samples.";

        } else {

            r.passed = true;
            r.summary = "ABS modulated brakes, max lock " + lockDuration.ToString("F2") + "s";
            r.detail = "ABS engaged " + absCount + "/" + samples.Count + " samples. Longest lock: " + maxConsecutiveLock + " samples on " + (lockedWheel != "" ? lockedWheel : "none") + ".";

        }

        return r;

    }

    private TestResult EvaluateTCS() {

        TestResult r = new TestResult();
        r.testType = TestType.TCS;

        int tcsCount = 0;
        float peakSlip = 0f;
        string peakWheel = "";
        bool anySlipDetected = false;

        float threshold = 0.35f;

        if (vehicle != null && vehicle.Stability != null)
            threshold = vehicle.Stability.engageTCSThreshold;

        // Determine which wheels are powered (check all 4, powered wheels have motor torque)
        string[] wheelNames = new string[] { "FL", "FR", "RL", "RR" };

        for (int i = 0; i < samples.Count; i++) {

            if (samples[i].tcsEngaged)
                tcsCount++;

            for (int w = 0; w < 4; w++) {

                // Only check wheels that received motor torque
                if (Mathf.Abs(samples[i].motorTorque[w]) > 1f) {

                    float slip = Mathf.Abs(samples[i].forwardSlip[w]);

                    if (slip > threshold)
                        anySlipDetected = true;

                    if (slip > peakSlip) {

                        peakSlip = slip;
                        peakWheel = wheelNames[w];

                    }

                }

            }

        }

        if (!anySlipDetected) {

            r.passed = false;
            r.summary = "No wheelspin detected";
            r.detail = "Powered wheels never exceeded TCS threshold. Vehicle may be too heavy or underpowered for this test.";

        } else if (tcsCount < 5) {

            r.passed = false;
            r.summary = "TCS never engaged";
            r.detail = "Wheelspin detected but TCS engaged only " + tcsCount + " times.";

        } else if (peakSlip > 1.2f) {

            r.passed = false;
            r.summary = "Excessive wheelspin on " + peakWheel + " (peak " + peakSlip.ToString("F2") + ")";
            r.detail = "TCS engaged " + tcsCount + "x but failed to limit slip below 1.2.";

        } else {

            r.passed = true;
            r.summary = "TCS limited slip, peak " + peakSlip.ToString("F2") + " on " + peakWheel;
            r.detail = "TCS engaged " + tcsCount + "/" + samples.Count + " samples. Peak slip controlled.";

        }

        return r;

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

        GUILayout.BeginArea(new Rect(10, 10, 270, Screen.height - 20), _boxStyle);
        GUILayout.Space(5);

        GUILayout.Label("RCCP Stability Test", _titleStyle);
        GUILayout.Space(10);

        // Vehicle selector
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

            if (GUILayout.Button("Spawn Vehicle", GUILayout.Height(30)))
                SpawnVehicle();

            GUI.enabled = true;

        }

        GUILayout.Space(15);
        GUILayout.Label("Tests", _headerStyle);
        GUILayout.Space(5);

        GUI.enabled = !testRunning && vehicle != null;

        if (GUILayout.Button("Oversteer (ESP)", GUILayout.Height(28)))
            RunTest(TestType.Oversteer);

        if (GUILayout.Button("Understeer (ESP)", GUILayout.Height(28)))
            RunTest(TestType.Understeer);

        if (GUILayout.Button("ABS", GUILayout.Height(28)))
            RunTest(TestType.ABS);

        if (GUILayout.Button("TCS", GUILayout.Height(28)))
            RunTest(TestType.TCS);

        GUILayout.Space(5);

        if (GUILayout.Button("Run All Tests", GUILayout.Height(32)))
            RunAllTests();

        GUI.enabled = true;

        GUILayout.Space(15);

        // Free drive toggle
        GUI.enabled = !testRunning;
        string freeDriveLabel = freeDriveMode ? "Disable Free Drive" : "Enable Free Drive";

        if (GUILayout.Button(freeDriveLabel, GUILayout.Height(28))) {

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

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Results"))
            results.Clear();

        GUILayout.EndArea();

    }

    private void DrawTelemetry() {

        float panelX = 290;
        float panelW = Screen.width - panelX - 10;
        float panelH = 200;

        GUILayout.BeginArea(new Rect(panelX, 10, panelW, panelH), _boxStyle);
        GUILayout.Space(3);

        GUILayout.Label("Live Telemetry", _titleStyle);

        RCCP_CarController car = vehicle;

        if (car == null || car.Stability == null) {

            GUILayout.Label("No vehicle", _labelStyle);
            GUILayout.EndArea();
            return;

        }

        RCCP_Stability s = car.Stability;

        // Speed / angle / yaw
        GUILayout.BeginHorizontal();

        GUILayout.Label("Speed: " + car.absoluteSpeed.ToString("F0") + " km/h", _labelStyle, GUILayout.Width(130));

        Vector3 vel = car.Rigid.linearVelocity;
        float driftAngle = vel.sqrMagnitude > 0.1f ? Vector3.SignedAngle(car.transform.forward, vel, Vector3.up) : 0f;
        GUILayout.Label("Drift: " + driftAngle.ToString("F1") + "\u00b0", _labelStyle, GUILayout.Width(110));

        float yawRate = car.Rigid.angularVelocity.y * Mathf.Rad2Deg;
        GUILayout.Label("Yaw: " + yawRate.ToString("F1") + "\u00b0/s", _labelStyle, GUILayout.Width(110));

        GUILayout.Label("Steer: " + car.steerAngle.ToString("F1") + "\u00b0", _labelStyle);

        GUILayout.EndHorizontal();

        // ESP / ABS / TCS indicators
        GUILayout.BeginHorizontal();

        GUILayout.Label("ESP: " + (s.ESPEngaged ? "ON" : "off"), s.ESPEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));
        GUILayout.Label("ABS: " + (s.ABSEngaged ? "ON" : "off"), s.ABSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));
        GUILayout.Label("TCS: " + (s.TCSEngaged ? "ON" : "off"), s.TCSEngaged ? _activeStyle : _smallLabelStyle, GUILayout.Width(80));

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Per-wheel data
        string[] names = new string[] { "FL", "FR", "RL", "RR" };
        float[] fSlip = new float[4];
        float[] sSlip = new float[4];
        float[] brk = new float[4];
        float[] mtr = new float[4];

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

        // Header row
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

        float panelX = 290;
        float panelY = 220;
        float panelW = Screen.width - panelX - 10;
        float panelH = Screen.height - panelY - 10;

        GUILayout.BeginArea(new Rect(panelX, panelY, panelW, panelH), _boxStyle);
        GUILayout.Space(3);

        GUILayout.Label("Test Results", _titleStyle);

        if (results.Count == 0) {

            GUILayout.Label("No tests run yet. Select a test from the left panel.", _smallLabelStyle);
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
            GUILayout.Label(r.testType.ToString() + ": " + r.summary, _labelStyle);

            GUILayout.EndHorizontal();

            GUILayout.Label("    " + r.detail, _smallLabelStyle);
            GUILayout.Space(5);

        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

    }

    #endregion

}

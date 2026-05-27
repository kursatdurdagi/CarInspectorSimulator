//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Controller for the RCCP Setup Wizard. Manages all setup data, validation,
/// wheel detection, collider scanning, and the finish pipeline. Extracted from
/// the monolithic RCCP_SetupWizard to separate logic from presentation.
/// </summary>
public class RCCP_SetupWizardController {

    //───────────────────────────────────────────────────────────────────────//
    #region Enums
    //───────────────────────────────────────────────────────────────────────//

    public enum DriveType { FWD, RWD, AWD }
    public enum WheelType { Balanced, Stable, Realistic, Slippy }
    public enum HandlingType { Balanced, Stable, Realistic }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Data Container
    //───────────────────────────────────────────────────────────────────────//

    [Serializable]
    public class SetupData {

        [Tooltip("Display name assigned to the vehicle GameObject.")]
        public string vehicleName = "";
        [Tooltip("Vehicle rigidbody mass in kilograms.")]
        public float mass = 1350f;
        [Tooltip("Wheel mesh GameObjects assigned to the front axle.")]
        public List<GameObject> frontWheels = new List<GameObject> { null, null };
        [Tooltip("Wheel mesh GameObjects assigned to the rear axle.")]
        public List<GameObject> rearWheels = new List<GameObject> { null, null };
        [Tooltip("Suspension travel distance in meters.")]
        public float suspensionDistance = 0.2f;
        [Tooltip("Suspension spring force in Newtons.")]
        public float springForce = 35000f;
        [Tooltip("Suspension damper force in Newtons.")]
        public float damperForce = 4500f;
        [Tooltip("Minimum idle engine RPM.")]
        public float minEngineRPM = 800;
        [Tooltip("Maximum engine RPM at redline.")]
        public float maxEngineRPM = 7000;
        [Tooltip("Peak engine torque in Nm.")]
        public float maxEngineTorque = 300f;
        [Tooltip("Target top speed in km/h.")]
        public float maxSpeed = 240f;
        [Tooltip("Drivetrain layout (FWD, RWD, or AWD).")]
        public DriveType driveType = DriveType.RWD;
        [Tooltip("Wheel friction preset affecting grip behavior.")]
        public WheelType wheelType = WheelType.Balanced;
        [Tooltip("Handling preset affecting stability assist levels.")]
        public HandlingType handlingType = HandlingType.Balanced;

        [Tooltip("Add RCCP_Input component.")]
        public bool addInputs = true;
        [Tooltip("Add RCCP_AeroDynamics component.")]
        public bool addDynamics = true;
        [Tooltip("Add RCCP_Stability component.")]
        public bool addStability = true;
        [Tooltip("Add RCCP_Audio component.")]
        public bool addAudio = true;
        [Tooltip("Add RCCP_Customizer component.")]
        public bool addCustomizer = true;
        [Tooltip("Add RCCP_Lights component.")]
        public bool addLights = true;
        [Tooltip("Add RCCP_Damage component.")]
        public bool addDamage = true;
        [Tooltip("Add RCCP_Particles component.")]
        public bool addParticles = true;
        [Tooltip("Add RCCP_Lod component.")]
        public bool addLOD = true;
        [Tooltip("Add RCCP_OtherAddons component.")]
        public bool addOtherAddons = true;

        [Tooltip("Spawn the RCCP UI canvas prefab into the scene.")]
        public bool addUICanvas = true;

        // Pre-finish decisions (collected inline, no dialog bomb)
        [Tooltip("Whether the selected object is a prefab asset.")]
        public bool isPrefab = false;
        [Tooltip("Unpack the prefab before applying vehicle setup.")]
        public bool unpackPrefab = true;
        [Tooltip("Whether existing rigidbodies were detected in the hierarchy.")]
        public bool hasExistingRigidbodies = false;
        [Tooltip("Remove child rigidbodies that may conflict with vehicle physics.")]
        public bool removeExistingRigidbodies = true;
        [Tooltip("Whether existing WheelColliders were detected in the hierarchy.")]
        public bool hasExistingWheelColliders = false;
        [Tooltip("Remove existing WheelColliders before creating new ones.")]
        public bool removeExistingWheelColliders = true;
        [Tooltip("Center the mesh pivot at the vehicle's geometric center.")]
        public bool fixPivot = true;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Constants
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Total amount of wizard steps (0-based index => 0 ... TOTAL_STEPS-1).</summary>
    public const int TOTAL_STEPS = 7;

    /// <summary>Lower bound for a plausible idle RPM (below this is unrealistic for any combustion engine).</summary>
    public const float MIN_ENGINE_RPM_FLOOR = 500f;
    /// <summary>Upper bound for a plausible idle RPM.</summary>
    public const float MIN_ENGINE_RPM_CEIL = 2000f;
    /// <summary>Lower bound for a plausible redline RPM.</summary>
    public const float MAX_ENGINE_RPM_FLOOR = 4000f;
    /// <summary>Upper bound for a plausible redline RPM (motorcycle / F1-era territory).</summary>
    public const float MAX_ENGINE_RPM_CEIL = 15000f;
    /// <summary>Lower bound for a plausible top speed in km/h.</summary>
    public const float MAX_SPEED_FLOOR = 50f;
    /// <summary>Upper bound for a plausible top speed in km/h.</summary>
    public const float MAX_SPEED_CEIL = 500f;
    /// <summary>Lower bound for a plausible peak engine torque in Nm.</summary>
    public const float MAX_TORQUE_FLOOR = 100f;

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Properties & Fields
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>All user-editable setup parameters.</summary>
    public SetupData Data { get; } = new SetupData();

    private int currentStep = 0;

    /// <summary>
    /// Current wizard step index (0-based). Setting a new value fires
    /// <see cref="OnStepChanged"/> so the hosting EditorWindow can refresh
    /// the rendered step without needing the caller to know about it.
    /// </summary>
    public int CurrentStep {
        get => currentStep;
        set {
            if (currentStep == value) return;
            currentStep = value;
            OnStepChanged?.Invoke();
        }
    }

    /// <summary>
    /// Fires whenever <see cref="CurrentStep"/> changes or <see cref="ReloadCurrentStep"/>
    /// is called. The hosting EditorWindow subscribes to this and rebuilds the visible step.
    /// </summary>
    public event System.Action OnStepChanged;

    /// <summary>
    /// Force a re-render of the current step without changing the step index.
    /// Useful after mutating scene state (e.g. adding an RCCP Camera) so that
    /// inline checks like "No camera in scene" can update.
    /// </summary>
    public void ReloadCurrentStep() {
        OnStepChanged?.Invoke();
    }

    /// <summary>
    /// Backing reference for the vehicle the wizard is operating on. Captured
    /// when the user first lands on Step 0 with a valid selection (or when
    /// they advance out of Step 0) so later steps don't re-query the live
    /// Hierarchy selection — clicking elsewhere mid-wizard must not switch
    /// the target.
    /// </summary>
    private GameObject capturedVehicle;

    /// <summary>
    /// The vehicle the wizard is configuring. Returns the captured target
    /// once the wizard has locked onto a GameObject; otherwise falls back
    /// to the current Hierarchy selection so Step 0 can offer it up as a
    /// candidate on first render.
    /// </summary>
    public GameObject SelectedVehicle {
        get {
            // Unity's overloaded == handles destroyed objects correctly here.
            if (capturedVehicle != null)
                return capturedVehicle;
            return Selection.activeGameObject;
        }
    }

    /// <summary>True when a vehicle has been captured and is still alive.</summary>
    public bool HasCapturedVehicle => capturedVehicle != null;

    /// <summary>
    /// Stores <paramref name="vehicle"/> as the wizard's lock-in target. All
    /// lazy-default flags are reset so the new vehicle gets a fresh pass
    /// (name cleanup, wheel detect, suspension auto-calc, etc.).
    /// </summary>
    public void CaptureVehicle(GameObject vehicle) {

        if (capturedVehicle == vehicle)
            return;

        capturedVehicle = vehicle;

        NameCleanedUp = false;
        WheelAutoDetectDone = false;
        ColliderCandidatesRefreshed = false;
        HasDetectionResult = false;
        DetectionMessage = "";
        SuspensionAutoCalcDone = false;
        EngineAutoCalcDone = false;

    }

    /// <summary>Clears the captured vehicle reference.</summary>
    public void ReleaseVehicle() {

        capturedVehicle = null;

    }

    /// <summary>
    /// If no vehicle is captured yet and the current Hierarchy selection is a
    /// valid RCCP setup candidate, captures it. Called on wizard open and on
    /// Step 0 entry so the user doesn't have to "select again" after opening.
    /// </summary>
    /// <returns>True if a vehicle was newly captured.</returns>
    public bool TryCaptureFromSelection() {

        if (HasCapturedVehicle)
            return false;

        // Ambiguous when the user has several things selected.
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 1)
            return false;

        GameObject go = Selection.activeGameObject;

        if (!IsValidVehicleCandidate(go))
            return false;

        CaptureVehicle(go);
        return true;

    }

    /// <summary>
    /// Criteria for a GameObject that can serve as the wizard's target vehicle:
    /// a scene-based, active root that does not already carry an RCCP vehicle.
    /// Used by the Step 0 ObjectField and the initial auto-capture pass.
    /// </summary>
    public static bool IsValidVehicleCandidate(GameObject go) {

        if (!go) return false;
        if (EditorUtility.IsPersistent(go)) return false;       // Project window asset.
        if (!go.scene.IsValid()) return false;                   // Not in a valid scene.
        if (!go.activeInHierarchy) return false;                 // Inactive.
        if (go.GetComponentInParent<RCCP_CarController>()) return false;
        return true;

    }

    // ── Wheel detection ──

    /// <summary>Result of the last wheel auto-detection pass.</summary>
    public RCCP_DetectPossibleWheels.WheelClassification LastDetection { get; private set; }

    /// <summary>Whether a wheel detection has been performed at least once.</summary>
    public bool HasDetectionResult { get; private set; }

    /// <summary>Human-readable message describing the detection outcome.</summary>
    public string DetectionMessage { get; private set; } = "";

    /// <summary>Severity of the detection message (Info, Warning, Error).</summary>
    public MessageType DetectionMessageType { get; private set; }

    /// <summary>Whether the automatic wheel detect has run for the current session.</summary>
    public bool WheelAutoDetectDone { get; set; }

    // ── Body colliders ──

    /// <summary>Mesh transforms eligible for body collider assignment.</summary>
    public List<Transform> ColliderCandidates { get; } = new List<Transform>();

    /// <summary>Per-candidate selection state (parallel to ColliderCandidates).</summary>
    public bool[] ColliderSelected { get; set; } = new bool[0];

    /// <summary>Whether generated MeshColliders should be convex.</summary>
    public bool ColliderConvex { get; set; } = true;

    /// <summary>Whether the collider candidate list has been refreshed for the current session.</summary>
    public bool ColliderCandidatesRefreshed { get; set; }

    // ── Lazy-default flags ──

    /// <summary>Whether suspension auto-calc from mass has been applied.</summary>
    public bool SuspensionAutoCalcDone { get; set; }

    /// <summary>Whether engine torque auto-calc from mass has been applied.</summary>
    public bool EngineAutoCalcDone { get; set; }

    /// <summary>Whether the vehicle name has been cleaned up from the raw GameObject name.</summary>
    public bool NameCleanedUp { get; set; }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Selection Validation
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Returns true when the wizard has a valid target to work on — either
    /// a captured vehicle from an earlier step, or a live Hierarchy selection
    /// that meets the new-vehicle requirements.
    /// </summary>
    public bool IsSelectionValid() {

        if (HasCapturedVehicle)
            return true;

        return IsValidVehicleCandidate(Selection.activeGameObject);

    }

    /// <summary>
    /// Returns true if the user has completed the required inputs for the given step.
    /// </summary>
    public bool CanAdvanceFromStep(int step) {

        switch (step) {

            case 0: // Basic Settings - require valid vehicle selection
                return IsSelectionValid();

            case 1: // Wheel Setup - require all 4 wheels assigned
                return Data.frontWheels.Count >= 2 &&
                       Data.frontWheels[0] != null && Data.frontWheels[1] != null &&
                       Data.rearWheels.Count >= 2 &&
                       Data.rearWheels[0] != null && Data.rearWheels[1] != null;

            default: // Steps 2-6 have sensible defaults or are optional
                return true;

        }

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Vehicle State Detection
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Detects prefab state, existing rigidbodies, and existing WheelColliders
    /// on the currently selected vehicle.
    /// </summary>
    public void DetectVehicleState() {

        if (SelectedVehicle == null)
            return;

        Data.isPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(SelectedVehicle);
        Data.hasExistingRigidbodies = SelectedVehicle.GetComponentInChildren<Rigidbody>(true) != null;
        Data.hasExistingWheelColliders = SelectedVehicle.GetComponentInChildren<WheelCollider>(true) != null;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Name Cleanup
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Regex-based cleanup of a raw GameObject name. Removes common suffixes
    /// like "(Prototype)", "(Proto)", "by ..." patterns, parenthetical text,
    /// "Model_" prefix, and replaces underscores with spaces.
    /// </summary>
    public string CleanVehicleName(string raw) {

        string name = raw;

        // Remove common suffixes and patterns
        name = Regex.Replace(name, @"\s*\(Prototype\)", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s*\(Proto\)", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+by\s+[^()]+", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\([^)]*\)", "");  // Remove remaining parenthetical text

        // Remove common prefixes
        if (name.StartsWith("Model_", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(6);

        // Replace underscores with spaces and trim
        name = name.Replace('_', ' ').Trim();

        // Fallback to original if we stripped everything
        if (string.IsNullOrWhiteSpace(name))
            name = raw;

        return name;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Wheel Detection
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Runs automatic wheel detection on the selected vehicle using
    /// RCCP_DetectPossibleWheels. Populates Data.frontWheels and Data.rearWheels,
    /// and sets detection status message fields.
    /// </summary>
    public void AutoDetectAllWheels() {

        if (SelectedVehicle == null) {
            EditorUtility.DisplayDialog("No Vehicle Selected",
                "Please select your vehicle GameObject in the hierarchy first.", "OK");
            return;
        }

        LastDetection = RCCP_DetectPossibleWheels.ClassifyWheels(SelectedVehicle);
        HasDetectionResult = true;

        // Fill data from classification.
        if (LastDetection.frontLeft) Data.frontWheels[0] = LastDetection.frontLeft;
        if (LastDetection.frontRight) Data.frontWheels[1] = LastDetection.frontRight;
        if (LastDetection.rearLeft) Data.rearWheels[0] = LastDetection.rearLeft;
        if (LastDetection.rearRight) Data.rearWheels[1] = LastDetection.rearRight;

        // Determine status message.
        int assigned = (LastDetection.frontLeft ? 1 : 0) + (LastDetection.frontRight ? 1 : 0) +
                       (LastDetection.rearLeft ? 1 : 0) + (LastDetection.rearRight ? 1 : 0);

        if (assigned == 4 && LastDetection.totalCandidates == 4) {
            DetectionMessage = "4 wheels detected and assigned.";
            DetectionMessageType = MessageType.Info;
        } else if (assigned == 4) {
            DetectionMessage = $"Found {LastDetection.totalCandidates} candidates \u2014 4 assigned, review extras below.";
            DetectionMessageType = MessageType.Warning;
        } else if (LastDetection.totalCandidates > 0) {
            DetectionMessage = $"Only {assigned} of 4 wheels could be classified. Assign the remaining slots manually.";
            DetectionMessageType = MessageType.Warning;
        } else {
            DetectionMessage = "No wheel candidates found. Assign wheels manually using the fields below.\nTip: Look for child objects named 'wheel', 'tire', or 'rim'.";
            DetectionMessageType = MessageType.Error;
        }

        Debug.Log($"[RCCP Setup Wizard] Wheel detection: {assigned}/4 assigned from {LastDetection.totalCandidates} candidates.");

    }

    /// <summary>Swaps the front-left and front-right wheel assignments.</summary>
    public void SwapFrontWheels() {

        (Data.frontWheels[0], Data.frontWheels[1]) = (Data.frontWheels[1], Data.frontWheels[0]);

    }

    /// <summary>Swaps the rear-left and rear-right wheel assignments.</summary>
    public void SwapRearWheels() {

        (Data.rearWheels[0], Data.rearWheels[1]) = (Data.rearWheels[1], Data.rearWheels[0]);

    }

    /// <summary>
    /// Returns true if the given wheel is on the right side of the vehicle
    /// (positive local X).
    /// </summary>
    public bool IsOnRight(GameObject vehicle, GameObject wheel) {

        Vector3 localPosition = vehicle.transform.InverseTransformPoint(wheel.transform.position);
        return localPosition.x > 0;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Body Colliders
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Wheel name patterns used to exclude wheel meshes from collider candidates.</summary>
    private static readonly string[] wheelPatterns = new string[] {
        "wheel", "tire", "tyre", "rim", "whl",
        "fl_", "fr_", "rl_", "rr_",
        "_fl", "_fr", "_rl", "_rr"
    };

    /// <summary>
    /// Scans MeshFilters on the selected vehicle, excludes wheel-named objects
    /// and assigned wheel GameObjects, sorts by bounding box volume (largest first),
    /// and auto-selects meshes whose volume is at least 10% of the largest.
    /// </summary>
    public void RefreshColliderCandidates() {

        ColliderCandidates.Clear();

        if (!SelectedVehicle) {
            ColliderSelected = new bool[0];
            return;
        }

        // Collect wheel transforms to exclude
        List<Transform> excludedTransforms = new List<Transform>();
        foreach (GameObject w in Data.frontWheels)
            if (w != null) excludedTransforms.Add(w.transform);
        foreach (GameObject w in Data.rearWheels)
            if (w != null) excludedTransforms.Add(w.transform);

        List<MeshFilter> mfs = new List<MeshFilter>(SelectedVehicle.GetComponentsInChildren<MeshFilter>(true));
        List<MeshFilter> properMfs = new List<MeshFilter>();

        for (int i = 0; i < mfs.Count; i++) {

            if (mfs[i] == null)
                continue;

            if (excludedTransforms.Contains(mfs[i].transform))
                continue;

            // Skip wheel-named objects
            string lowerName = mfs[i].name.ToLowerInvariant();
            bool isWheel = false;
            foreach (string pattern in wheelPatterns) {
                if (lowerName.Contains(pattern)) { isWheel = true; break; }
            }
            if (isWheel) continue;

            properMfs.Add(mfs[i]);

        }

        // Sort by mesh volume (largest first)
        properMfs.Sort((a, b) => {
            float volA = a.sharedMesh ? a.sharedMesh.bounds.size.x * a.sharedMesh.bounds.size.y * a.sharedMesh.bounds.size.z : 0f;
            float volB = b.sharedMesh ? b.sharedMesh.bounds.size.x * b.sharedMesh.bounds.size.y * b.sharedMesh.bounds.size.z : 0f;
            return volB.CompareTo(volA);
        });

        foreach (MeshFilter mf in properMfs)
            ColliderCandidates.Add(mf.transform);

        ColliderSelected = new bool[ColliderCandidates.Count];

        // Auto-select all meshes whose volume is >= 10% of the largest.
        if (ColliderCandidates.Count > 0) {

            MeshFilter largestMf = ColliderCandidates[0].GetComponent<MeshFilter>();
            float largestVol = largestMf && largestMf.sharedMesh
                ? largestMf.sharedMesh.bounds.size.x * largestMf.sharedMesh.bounds.size.y * largestMf.sharedMesh.bounds.size.z
                : 0f;
            float threshold = largestVol * 0.1f;

            for (int i = 0; i < ColliderCandidates.Count; i++) {

                MeshFilter mf = ColliderCandidates[i].GetComponent<MeshFilter>();
                float vol = mf && mf.sharedMesh
                    ? mf.sharedMesh.bounds.size.x * mf.sharedMesh.bounds.size.y * mf.sharedMesh.bounds.size.z
                    : 0f;
                ColliderSelected[i] = vol >= threshold;

            }

        }

    }

    /// <summary>Sets all collider candidate selection states to the given value.</summary>
    public void SetAllColliders(bool state) {

        for (int i = 0; i < ColliderSelected.Length; i++)
            ColliderSelected[i] = state;

    }

    /// <summary>
    /// Iterates ColliderCandidates where ColliderSelected is true and adds
    /// a MeshCollider component (with Undo support) to each.
    /// </summary>
    public void ApplyBodyColliders() {

        if (ColliderCandidates == null || ColliderSelected == null)
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add Body Colliders");
        int undoGroup = Undo.GetCurrentGroup();
        int added = 0;

        for (int i = 0; i < ColliderCandidates.Count; i++) {

            if (!ColliderSelected[i] || !ColliderCandidates[i])
                continue;

            MeshCollider mc = ColliderCandidates[i].GetComponent<MeshCollider>();
            if (!mc) {
                mc = Undo.AddComponent<MeshCollider>(ColliderCandidates[i].gameObject);
                mc.convex = ColliderConvex;
                added++;
            } else {
                mc.convex = ColliderConvex;
            }

        }

        Undo.CollapseUndoOperations(undoGroup);

        if (added > 0)
            Debug.Log($"[RCCP Setup Wizard] Added MeshCollider to {added} body part(s).");

    }

    /// <summary>Returns true if the user selected at least one body part in the colliders step.</summary>
    public bool HasColliderSelections() {

        if (ColliderCandidates == null || ColliderSelected == null)
            return false;

        for (int i = 0; i < ColliderSelected.Length; i++) {

            if (ColliderSelected[i] && i < ColliderCandidates.Count && ColliderCandidates[i] != null)
                return true;

        }

        return false;

    }

    /// <summary>
    /// Returns true if the given vehicle contains at least one non-trigger,
    /// non-WheelCollider whose volume is at least 25% of the model's overall
    /// render bounds.
    /// </summary>
    public bool HasProperBodyCollider(out Collider bodyCollider) {

        bodyCollider = null;

        if (SelectedVehicle == null)
            return false;

        // 1) Gather every collider in the hierarchy except wheel colliders.
        Collider[] colliders = SelectedVehicle.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders) {

            if (col is WheelCollider)
                continue;

            if (col.isTrigger)
                continue;

            bodyCollider = col;
            break;

        }

        if (bodyCollider == null)
            return false;

        // 2) Compare collider volume with the visual render bounds.
        Renderer[] renderers = SelectedVehicle.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return true;    // no renderers to compare - accept collider

        Bounds renderBounds = new Bounds(SelectedVehicle.transform.position, Vector3.zero);

        foreach (Renderer r in renderers)
            renderBounds.Encapsulate(r.bounds);

        float modelVolume = renderBounds.size.x * renderBounds.size.y * renderBounds.size.z;
        float colliderVolume = bodyCollider.bounds.size.x * bodyCollider.bounds.size.y * bodyCollider.bounds.size.z;

        // Accept if collider covers at least 25% of the model volume.
        return colliderVolume >= modelVolume * .25f;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Validation
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Runs every validation routine in sequence. Returns false on first failure.</summary>
    public bool ValidateAll() {

        return ValidateWheels() &&
               ValidateSuspension() &&
               ValidateEngine() &&
               ValidateBodyCollider() &&
               ValidateMeshesReadable();

    }

    /// <summary>Checks that all 4 wheel slots are assigned.</summary>
    public bool ValidateWheels() {

        if (Data.frontWheels.Count < 2 || Data.rearWheels.Count < 2 ||
           !Data.frontWheels[0] || !Data.frontWheels[1] ||
           !Data.rearWheels[0] || !Data.rearWheels[1]) {

            EditorUtility.DisplayDialog("Missing Wheels",
                "Please make sure both front and rear wheels are properly assigned before proceeding.", "OK");
            return false;
        }
        return true;

    }

    /// <summary>Checks that spring and damper values are above minimum thresholds.</summary>
    public bool ValidateSuspension() {

        if (Data.springForce <= 1000f || Data.damperForce <= 100f) {
            EditorUtility.DisplayDialog("Suspension Warning",
                "Suspension settings seem too low. Please check spring and damper values.", "OK");
            return false;
        }
        return true;

    }

    /// <summary>
    /// Collects every engine-related validation issue (RPM range, ordering, torque, top speed).
    /// Returns an empty list when all values are within reasonable ranges.
    /// Shared by <see cref="ValidateEngine"/> (finalize dialog) and the Engine step UI (live warning).
    /// </summary>
    public List<string> GetEngineValidationIssues() {

        var issues = new List<string>();

        // Min RPM.
        if (Data.minEngineRPM < 0f)
            issues.Add("Min RPM cannot be negative.");
        else if (Data.minEngineRPM < MIN_ENGINE_RPM_FLOOR)
            issues.Add($"Min RPM ({Data.minEngineRPM:0}) is too low. Idle RPM should be at least {MIN_ENGINE_RPM_FLOOR:0}.");
        else if (Data.minEngineRPM > MIN_ENGINE_RPM_CEIL)
            issues.Add($"Min RPM ({Data.minEngineRPM:0}) is too high. Idle RPM should stay below {MIN_ENGINE_RPM_CEIL:0}.");

        // Max RPM.
        if (Data.maxEngineRPM < 0f)
            issues.Add("Max RPM cannot be negative.");
        else if (Data.maxEngineRPM < MAX_ENGINE_RPM_FLOOR)
            issues.Add($"Max RPM ({Data.maxEngineRPM:0}) is too low. Redline should be at least {MAX_ENGINE_RPM_FLOOR:0}.");
        else if (Data.maxEngineRPM > MAX_ENGINE_RPM_CEIL)
            issues.Add($"Max RPM ({Data.maxEngineRPM:0}) is too high. Redline should stay below {MAX_ENGINE_RPM_CEIL:0}.");

        // Ordering (only meaningful when both are non-negative).
        if (Data.minEngineRPM >= 0f && Data.maxEngineRPM >= 0f && Data.maxEngineRPM <= Data.minEngineRPM)
            issues.Add("Max RPM must be greater than Min RPM.");

        // Torque.
        if (Data.maxEngineTorque <= 0f)
            issues.Add("Max Torque must be greater than zero.");
        else if (Data.maxEngineTorque < MAX_TORQUE_FLOOR)
            issues.Add($"Max Torque ({Data.maxEngineTorque:0} Nm) seems too low. Expected at least {MAX_TORQUE_FLOOR:0} Nm.");

        // Max speed.
        if (Data.maxSpeed < 0f)
            issues.Add("Max Speed cannot be negative.");
        else if (Data.maxSpeed < MAX_SPEED_FLOOR)
            issues.Add($"Max Speed ({Data.maxSpeed:0} km/h) is too low. Expected at least {MAX_SPEED_FLOOR:0} km/h.");
        else if (Data.maxSpeed > MAX_SPEED_CEIL)
            issues.Add($"Max Speed ({Data.maxSpeed:0} km/h) is too high. Expected below {MAX_SPEED_CEIL:0} km/h.");

        return issues;

    }

    /// <summary>Checks that engine torque, RPM, and top-speed values are within sane ranges.</summary>
    public bool ValidateEngine() {

        var issues = GetEngineValidationIssues();
        if (issues.Count == 0)
            return true;

        string message = "Please correct the following engine settings:\n\n- " + string.Join("\n- ", issues);
        EditorUtility.DisplayDialog("Engine Settings", message, "OK");
        return false;

    }

    /// <summary>
    /// Makes sure the vehicle carries at least one solid body collider.
    /// If none exist it can optionally guide the user through adding them.
    /// Returns false only if the user chooses "Go Back".
    /// </summary>
    public bool ValidateBodyCollider() {

        // 1) Do we already have a good collider?
        if (HasProperBodyCollider(out _))
            return true;

        // 2) User already selected body parts in Step 5 - they'll be applied by ApplyBodyColliders().
        if (HasColliderSelections())
            return true;

        // 3) Offer the wizard to create them.
        int reply = EditorUtility.DisplayDialogComplex(
            "Missing Body Collider",
            "Your vehicle needs a body collider to interact with the environment.\n\n" +
            "Would you like to add convex MeshColliders to the main body parts now?",
            "Add Colliders Now",             // 0
            "Skip - Continue Without",       // 1
            "Go Back");                      // 2

        // 4) User said "Skip" - allow them to continue anyway.
        if (reply == 1)
            return true;

        // 5) User cancelled - abort the current step.
        if (reply == 2)
            return false;

        // 6) User chose "Add Colliders Now" - open the helper window.
        List<Transform> allWheelTransforms = new List<Transform>();

        for (int i = 0; i < Data.frontWheels.Count; i++) {

            if (Data.frontWheels[i] != null)
                allWheelTransforms.Add(Data.frontWheels[i].transform);

        }

        for (int i = 0; i < Data.rearWheels.Count; i++) {

            if (Data.rearWheels[i] != null)
                allWheelTransforms.Add(Data.rearWheels[i].transform);

        }

        RCCP_BodyCollidersWizard.ShowWindow(SelectedVehicle, allWheelTransforms);
        // We return true so the wizard may continue; the window runs independently.
        return true;

    }

    /// <summary>
    /// Ensures that every mesh used by the vehicle can be edited at runtime.
    /// Damage deformation needs Mesh.isReadable == true.
    /// </summary>
    public bool ValidateMeshesReadable() {

        if (SelectedVehicle == null)
            return true;

        // Collect every non-readable mesh under the vehicle hierarchy.
        List<Mesh> unreadableMeshes = new List<Mesh>();

        foreach (MeshFilter mf in SelectedVehicle.GetComponentsInChildren<MeshFilter>(true)) {

            Mesh mesh = mf.sharedMesh;
            if (!mesh) continue;

            if (!mesh.isReadable) unreadableMeshes.Add(mesh);

        }

        foreach (SkinnedMeshRenderer smr in SelectedVehicle.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {

            Mesh mesh = smr.sharedMesh;
            if (!mesh) continue;

            if (!mesh.isReadable) unreadableMeshes.Add(mesh);

        }

        // Everything OK?
        if (unreadableMeshes.Count == 0)
            return true;

        // Ask the developer how to proceed.
        int answer = EditorUtility.DisplayDialogComplex(
            "Meshes not Readable",
            $"Damage deformation requires Read/Write enabled meshes, " +
            $"but {unreadableMeshes.Count} mesh asset(s) are currently non-readable.\n\n" +
            "Would you like the wizard to enable the flag and re-import them now? " +
            "(This will slightly increase memory usage.)",
            "Enable Automatically",        // 0
            "Continue Without Fix",        // 1
            "Cancel Setup");               // 2

        // Cancel pressed - abort the step.
        if (answer == 2)
            return false;

        // Fix requested - flip the flag and re-import.
        if (answer == 0)
            EnableReadWriteOnMeshes(unreadableMeshes);

        // Either fixed or dev chose to ignore - carry on.
        return true;

    }

    /// <summary>
    /// Sets Read/Write Enabled = true on each supplied mesh asset and re-imports it.
    /// </summary>
    public void EnableReadWriteOnMeshes(List<Mesh> meshes) {

        HashSet<string> processedPaths = new HashSet<string>();

        foreach (Mesh mesh in meshes) {

            string path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path) || processedPaths.Contains(path))
                continue;

            processedPaths.Add(path);

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || importer.isReadable)
                continue;

            Undo.RecordObject(importer, "Enable Read/Write");
            importer.isReadable = true;
            importer.SaveAndReimport();

        }

        Debug.Log($"[RCCP Setup Wizard] Enabled Read/Write on {processedPaths.Count} mesh asset(s).");

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Scene Readiness
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Checks if the scene contains a static collider that could serve as ground.</summary>
    public bool SceneHasGroundCollider() {

        Collider[] colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Collider col in colliders) {

            if (col is WheelCollider)
                continue;

            // A ground collider is typically on a static (non-rigidbody) GameObject.
            if (col.attachedRigidbody == null && !col.isTrigger)
                return true;

        }

        return false;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Addon Toggles
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>Sets all 10 addon flags to the given state.</summary>
    public void ToggleAllAddons(bool state) {

        Data.addInputs = Data.addDynamics = Data.addStability = Data.addAudio =
        Data.addCustomizer = Data.addLights = Data.addDamage = Data.addParticles =
        Data.addLOD = Data.addOtherAddons = state;

    }

    #endregion

    //───────────────────────────────────────────────────────────────────────//
    #region Finish Setup
    //───────────────────────────────────────────────────────────────────────//

    /// <summary>
    /// Executes the full setup pipeline: validates all steps, warns about
    /// scene readiness, applies body colliders, creates the vehicle via
    /// RCCP_CreateNewVehicle, optionally spawns the UI canvas, and shows
    /// a completion dialog.
    /// </summary>
    /// <returns>True if setup completed successfully, false if aborted.</returns>
    public bool FinishSetup() {

        if (!ValidateAll())
            return false;

        if (SelectedVehicle == null) {
            EditorUtility.DisplayDialog("No Vehicle Selected",
                "Please select your vehicle GameObject in the hierarchy first.", "OK");
            return false;
        }

        // Scene readiness warning (non-blocking)
        bool missingCamera = UnityEngine.Object.FindAnyObjectByType<RCCP_Camera>() == null;
        bool missingGround = !SceneHasGroundCollider();

        if (missingCamera || missingGround) {

            string warning = "Your scene is missing:\n";
            if (missingCamera) warning += "- RCCP Camera (vehicle won't be followed)\n";
            if (missingGround) warning += "- Ground collider (vehicle will fall through)\n";
            warning += "\nContinue anyway?";

            if (!EditorUtility.DisplayDialog("Scene Not Ready", warning, "Continue", "Go Back"))
                return false;

        }

        // Apply body colliders from the inline step
        ApplyBodyColliders();

        RCCP_CreateNewVehicle.NewVehicle(SelectedVehicle, Data);

        if (Data.addUICanvas && UnityEngine.Object.FindAnyObjectByType<RCCP_UIManager>() == null) {

            if (RCCP_Settings.Instance.RCCPCanvas != null) {

                GameObject canvasInstance = (GameObject)PrefabUtility.InstantiatePrefab(RCCP_Settings.Instance.RCCPCanvas.gameObject);
                canvasInstance.name = RCCP_Settings.Instance.RCCPCanvas.name;
                Undo.RegisterCreatedObjectUndo(canvasInstance, "Add RCCP Canvas");

            }

        }

        EditorUtility.DisplayDialog("Setup Completed", "Vehicle setup successfully completed!", "OK");
        return true;

    }

    #endregion

}
#endif

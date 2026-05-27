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
/// Manages a wheel's physics, alignment, slip calculations, friction, and special states (deflation, drift, etc.) 
/// using Unity WheelCollider. This component is designed to work in conjunction with the RCCP vehicle system.
/// </summary>
[DefaultExecutionOrder(0)]
[RequireComponent(typeof(WheelCollider))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP WheelCollider")]
public class RCCP_WheelCollider : RCCP_Component {

    #region Constants

    // Magic numbers converted to constants for better maintainability
    private const float WHEEL_TEMPERATURE_INCREASE_RATE = 10f;
    private const float WHEEL_TEMPERATURE_DECREASE_RATE = 1.5f;
    private const float MIN_WHEEL_TEMPERATURE = 20f;
    private const float MAX_WHEEL_TEMPERATURE = 125f;
    private const float WHEEL_MASS_DIVIDER = 25f;
    private const float WHEEL_FORCE_APP_POINT_DISTANCE = 0.1f;
    private const float WHEEL_SUSPENSION_DISTANCE = 0.2f;
    private const float WHEEL_SPRING_VALUE = 50000f;
    private const float WHEEL_DAMPER_VALUE = 3500f;
    private const float FORWARD_FRICTION_EXTREMUM_SLIP = 0.4f;
    private const float SIDEWAYS_FRICTION_EXTREMUM_SLIP = 0.35f;
    private const float MIN_TORQUE_THRESHOLD = 5f;
    private const float SPEED_THRESHOLD_CHECK = 0.01f;
    private const float CIRCUMFERENCE_MULTIPLIER = 60f / 1000f;
    private const float RPM_TO_DEGREES_MULTIPLIER = 360f / 60f;
    private const float FRICTION_COMPARISON_EPSILON = 0.0001f;
    private const float HANDBRAKE_DIVISOR = 5f;
    private const float GEAR_SPEED_TOLERANCE = 1.02f;
    private const float AUDIO_SOURCE_CREATION_DELAY = 0.02f;
    private const float SKID_VOLUME_THRESHOLD = 0.02f;
    private const float MIN_AUDIO_VOLUME_TO_STOP = 0.05f;
    private const float BUMP_FORCE_THRESHOLD = 5000f;

    #endregion

    #region Core Fields

    /// <summary>
    /// Backing field for WheelCollider component reference.
    /// </summary>
    private WheelCollider _wheelCollider;

    /// <summary>
    /// Actual wheelcollider component. Lazy-loaded on first access.
    /// </summary>
    public WheelCollider WheelCollider {

        get {

            if (_wheelCollider == null)
                TryGetComponent(out _wheelCollider);

            return _wheelCollider;

        }

    }

    /// <summary>
    /// This wheel is connected to this axle. Defines axle grouping (front/rear or other).
    /// </summary>
    [Tooltip("The axle this wheel is connected to (front/rear).")]
    public RCCP_Axle connectedAxle;

    /// <summary>
    /// Information about what this wheel is currently hitting (if anything).
    /// </summary>
    [Tooltip("Current ground contact information from WheelCollider.")]
    public WheelHit wheelHit;

    /// <summary>
    /// If true, wheel models are aligned to WheelCollider orientation and position each frame.
    /// </summary>
    [Tooltip("Align wheel model to WheelCollider position and rotation each frame.")]
    public bool alignWheels = true;

    /// <summary>
    /// Vehicle-authored base suspension spring rate captured at Awake, before any behavior preset or runtime setter runs.
    /// BehaviorType multipliers apply relative to this value so the same preset scales correctly across a sedan and a heavy truck.
    /// </summary>
    [System.NonSerialized]
    public float BaseSuspensionSpring;

    /// <summary>
    /// Vehicle-authored base suspension damper rate captured at Awake, before any behavior preset or runtime setter runs.
    /// BehaviorType multipliers apply relative to this value.
    /// </summary>
    [System.NonSerialized]
    public float BaseSuspensionDamper;

    #endregion

    #region Status Fields

    /// <summary>
    /// Indicates whether this wheel is in contact with a surface.
    /// </summary>
    [Space(), Tooltip("True if this wheel is in contact with a surface.")]
    public bool isGrounded = false;

    /// <summary>
    /// Indicates whether this wheel is currently slipping above a threshold (skidding).
    /// </summary>
    [Tooltip("True if this wheel is slipping above the skid threshold.")]
    public bool isSkidding = false;

    /// <summary>
    /// Index of the ground material this wheel is on, used for slip thresholds, audio, etc.
    /// </summary>
    [Min(0), Tooltip("Index of the current ground material (from RCCP_GroundMaterials).")]
    public int groundIndex = 0;

    #endregion

    #region Input Fields

    /// <summary>
    /// Motor torque applied to this wheel in Nm.
    /// </summary>
    [Space(), Tooltip("Motor torque applied to this wheel in Nm.")]
    public float motorTorque = 0f;

    /// <summary>
    /// Brake torque applied to this wheel in Nm.
    /// </summary>
    [Min(0f), Tooltip("Brake torque applied to this wheel in Nm.")]
    public float brakeTorque = 0f;

    /// <summary>
    /// Steer input angle in degrees (before Ackermann corrections, if any).
    /// </summary>
    [Tooltip("Steer input angle in degrees (before Ackermann corrections).")]
    public float steerInput = 0f;

    /// <summary>
    /// Handbrake input in range [0..1].
    /// </summary>
    [Min(0f), Tooltip("Handbrake input (0-1).")]
    public float handbrakeInput = 0f;

    [Min(0f), Tooltip("Per-frame fake engine-brake torque at this wheel in Nm. Engine writes to it via AddEngineBrakeTorque(); applied as negative motor torque opposing wheel rotation, then reset every fixed frame.")]
    public float engineBrakeTorqueNm = 0f;

    private const float LegacyFeedbackTorqueNm = 500f;

    /// <summary>
    /// Legacy normalized feedback value kept for backwards compatibility. Use engineBrakeTorqueNm for new code.
    /// </summary>
    [System.Obsolete("Use engineBrakeTorqueNm and normalize it for your haptic SDK.")]
    public float negativeFeedbackIntensity {
        get {
            return Mathf.Clamp01(engineBrakeTorqueNm / LegacyFeedbackTorqueNm);
        }
        set {
            engineBrakeTorqueNm = Mathf.Max(0f, value * LegacyFeedbackTorqueNm);
        }
    }

    #endregion

    #region Visual Fields

    /// <summary>
    /// Transform reference for the visual representation (wheel model).
    /// </summary>
    [Space(), Tooltip("Visual wheel model Transform.")]
    public Transform wheelModel;

    /// <summary>
    /// Approximate speed of the wheel derived from RPM, in km/h.
    /// </summary>
    [Tooltip("Speed derived from wheel RPM in km/h.")]
    public float wheelRPM2Speed = 0f;

    /// <summary>
    /// Filtered RPM of the wheel, smoothed with a low-pass filter. Only updates while grounded.
    /// </summary>
    public float WheelRPM {

        get {

            if (WheelCollider == null)
                return 0f;

            float rawRpm = WheelCollider.rpm;

            // Only update when grounded
            if (WheelCollider.isGrounded) {
                // Frame-rate independent low-pass filter (targets ~0.15 blend at 50 Hz)
                float smoothFactor = 1f - Mathf.Pow(1f - 0.15f, Time.deltaTime / 0.02f);
                _filteredRpm = Mathf.Lerp(_filteredRpm, rawRpm, smoothFactor);
            }

            return _filteredRpm;

        }

    }
    private float _filteredRpm = 0f;

    /// <summary>
    /// Width of the wheel used for skidmarks.
    /// </summary>
    [Space(), Min(.1f), Tooltip("Wheel width for skidmark rendering.")]
    public float width = .25f;

    /// <summary>
    /// Total rotation of the wheel (for spinning animation).
    /// </summary>
    private float wheelRotation = 0f;

    /// <summary>
    /// Camber angle, caster angle, and X offset to adjust wheel tilt and position.
    /// </summary>
    [Range(-10f, 10f), Tooltip("Camber (tilt), caster (lean), and offset (position) for wheel alignment.")]
    public float camber, caster, offset = 0f;

    /// <summary>
    /// Grip multiplier applied to ground material stiffness. 1.0 = default, 0.5 = half grip, 2.0 = double grip.
    /// </summary>
    [Tooltip("Grip multiplier (0-2). Multiplies ground material stiffness. 1.0 = normal, 0.5 = low grip, 2.0 = max grip.")]
    [Range(0f, 2f)]
    public float grip = 1f;

    #endregion

    #region Physics Fields

    /// <summary>
    /// Represents the 'temperature' or usage stress of this wheel. Increases with slip.
    /// </summary>
    [Space(), Min(0f), Tooltip("Wheel temperature (increases with slip, affects grip).")]
    public float totalWheelTemp = MIN_WHEEL_TEMPERATURE;

    /// <summary>
    /// Combined magnitude of forward and sideways slip, used for skids and audio.
    /// </summary>
    [System.Obsolete("Use TotalSlip instead totalSlip.")]
    public float totalSlip {

        get {

            return TotalSlip;

        }

    }

    /// <summary>
    /// Deprecated. Use <see cref="SidewaysSlip"/> instead.
    /// </summary>
    [System.Obsolete("Use SidewaysSlip instead wheelSlipAmountSideways.")]
    public float wheelSlipAmountSideways {

        get {

            return SidewaysSlip;

        }

    }

    /// <summary>
    /// Deprecated. Use <see cref="ForwardSlip"/> instead.
    /// </summary>
    [System.Obsolete("Use ForwardSlip instead wheelSlipAmountForward.")]
    public float wheelSlipAmountForward {

        get {

            return ForwardSlip;

        }

    }

    /// <summary>
    /// Filtered forward (longitudinal) slip of the wheel. Returns 0 when not grounded.
    /// Cached per frame so multiple reads don't advance the filter more than once.
    /// </summary>
    public float ForwardSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            if (Time.frameCount != _forwardSlipLastFrame) {

                _forwardSlipLastFrame = Time.frameCount;

                float rawSlip = wheelHit.forwardSlip;

                if (!isGrounded)
                    rawSlip = 0f;

                // Frame-rate independent low-pass filter (targets ~0.15 blend at 50 Hz)
                float fwdSmooth = 1f - Mathf.Pow(1f - 0.15f, Time.deltaTime / 0.02f);
                _filteredForwardSlip = Mathf.Lerp(_filteredForwardSlip, rawSlip, fwdSmooth);

            }

            return _filteredForwardSlip;

        }

    }
    private float _filteredForwardSlip = 0f;
    private int _forwardSlipLastFrame = -1;

    /// <summary>
    /// Filtered sideways (lateral) slip of the wheel. Returns 0 when not grounded.
    /// Cached per frame so multiple reads don't advance the filter more than once.
    /// </summary>
    public float SidewaysSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            if (Time.frameCount != _sidewaysSlipLastFrame) {

                _sidewaysSlipLastFrame = Time.frameCount;

                float rawSlip = wheelHit.sidewaysSlip;

                if (!isGrounded)
                    rawSlip = 0f;

                // Frame-rate independent low-pass filter (targets ~0.15 blend at 50 Hz)
                float sideSmooth = 1f - Mathf.Pow(1f - 0.15f, Time.deltaTime / 0.02f);
                _filteredSidewaysSlip = Mathf.Lerp(_filteredSidewaysSlip, rawSlip, sideSmooth);

            }

            return _filteredSidewaysSlip;

        }

    }
    private float _filteredSidewaysSlip = 0f;
    private int _sidewaysSlipLastFrame = -1;

    /// <summary>
    /// Combined magnitude of forward and sideways slip (sum of absolute values).
    /// </summary>
    public float TotalSlip {

        get {

            if (WheelCollider == null)
                return 0f;

            return Mathf.Abs(ForwardSlip) + Mathf.Abs(SidewaysSlip);

        }

    }

    /// <summary>
    /// Current bump force used in collision/bump sound calculations.
    /// </summary>
    [HideInInspector]
    public float bumpForce, oldForce = 0f;

    #endregion

    #region Skidmarks Fields

    /// <summary>
    /// Whether this wheel can generate skidmarks or not.
    /// </summary>
    [Space(), Tooltip("Enable skidmark generation for this wheel.")]
    public bool drawSkid = true;

    /// <summary>
    /// Index of the last skidmark created by this wheel in the global SkidmarksManager.
    /// </summary>
    private int lastSkidmark = -1;

    #endregion

    #region Traction Control Fields

    /// <summary>
    /// ESP traction cut factor applied to reduce motor torque during slip.
    /// </summary>
    [Space(), Range(0f, 1f), Tooltip("ESP traction cut factor (0-1) to reduce motor torque during slip.")]
    public float cutTractionESP = 0f;

    /// <summary>
    /// TCS traction cut factor applied to reduce motor torque during forward slip.
    /// </summary>
    [Range(0f, 1f), Tooltip("TCS traction cut factor (0-1) to reduce motor torque during forward slip.")]
    public float cutTractionTCS = 0f;

    /// <summary>
    /// ABS brake cut factor applied to reduce brake torque during wheel lock.
    /// </summary>
    [Range(0f, 1f), Tooltip("ABS brake cut factor (0-1) to reduce brake torque during wheel lock.")]
    public float cutBrakeABS = 0f;

    #endregion

    #region Audio Fields

    /// <summary>
    /// AudioSource component for skid sound effects.
    /// </summary>
    private AudioSource skidAudioSource;

    /// <summary>
    /// Audio clip for skid sounds, determined by ground material.
    /// </summary>
    private AudioClip skidClip;

    /// <summary>
    /// Volume level for skid audio, determined by ground material and slip amount.
    /// </summary>
    private float skidVolume = 0f;

    #endregion

    #region Special State Fields

    /// <summary>
    /// Whether this wheel is currently deflated (flat tire).
    /// </summary>
    [Space(), Tooltip("True if this wheel has a flat tire.")]
    public bool deflated = false;

    /// <summary>
    /// Scaling factor to reduce wheel radius when deflated.
    /// </summary>
    [Range(0f, 1f), Tooltip("Wheel radius multiplier when deflated (e.g., 0.8 = 80% of normal).")]
    public float deflatedRadiusMultiplier = .8f;

    /// <summary>
    /// Stiffness multiplier applied when wheel is deflated.
    /// </summary>
    [Range(0f, 1f), Tooltip("Friction stiffness multiplier when deflated (e.g., 0.25 = 25% grip).")]
    public float deflatedStiffnessMultiplier = .25f;

    /// <summary>
    /// Cached un-deflated radius. Used to restore radius on inflation.
    /// </summary>
    [Min(0f)]
    private float defRadius = -1f;

    /// <summary>
    /// Whether drift mode is active for this wheel.
    /// </summary>
    [Space(), Tooltip("Enable drift mode friction adjustments.")]
    public bool driftMode = false;

    /// <summary>
    /// Forward stiffness multiplier set by RCCP_Stability drift system. 1.0 = normal grip.
    /// </summary>
    [HideInInspector] public float driftForwardStiffnessMultiplier = 1f;

    /// <summary>
    /// Sideways stiffness multiplier set by RCCP_Stability drift system. 1.0 = normal grip.
    /// </summary>
    [HideInInspector] public float driftSidewaysStiffnessMultiplier = 1f;

    #endregion

    #region Ackermann Steering Fields

    /// <summary>
    /// Distance between the front and rear axles, used in steering calculations (Ackermann).
    /// </summary>
    [Space(), Tooltip("Distance between front and rear axles for Ackermann steering.")]
    [Min(0.01f)] public float wheelbase = 2.55f;

    /// <summary>
    /// Distance between the left and right wheels on an axle.
    /// </summary>
    [Tooltip("Distance between left and right wheels on this axle.")]
    [Min(0.01f)] public float trackWidth = 1.5f;

    #endregion

    #region Performance Cache Fields

    // Performance optimization: cache frequently used references
    private RCCP_GroundMaterials cachedGroundMaterials;
    private RCCP_SkidmarksManager cachedSkidmarksManager;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Unity Awake method. Ensures the wheel model is assigned. Disables if missing.
    /// </summary>
    public override void Awake() {

        base.Awake();

        if (wheelModel == null) {

            Debug.LogError("Wheel model is not selected for " + transform.name + ". Disabling this wheelcollider.");
            enabled = false;
            return;

        }

        gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        // Capture the vehicle-authored suspension values BEFORE any behavior preset or customization runs.
        // Behavior presets multiply these base values, so every future write is relative to the prefab's design.
        if (WheelCollider != null) {

            BaseSuspensionSpring = WheelCollider.suspensionSpring.spring > 0f ? WheelCollider.suspensionSpring.spring : WHEEL_SPRING_VALUE;
            BaseSuspensionDamper = WheelCollider.suspensionSpring.damper > 0f ? WheelCollider.suspensionSpring.damper : WHEEL_DAMPER_VALUE;

        } else {

            BaseSuspensionSpring = WHEEL_SPRING_VALUE;
            BaseSuspensionDamper = WHEEL_DAMPER_VALUE;

        }

        // Cache references for performance
        CacheReferences();

    }

    /// <summary>
    /// Unity Start method. Applies fixed wheel mass, creates the audio source, and sets up a pivot transform for the wheel model.
    /// Vehicle-wide sub-stepping is configured once by RCCP_CarController.ConfigureWheelSubsteps().
    /// </summary>
    public override void Start() {

        base.Start();

        // Increasing mass of the wheel for more stable handling.
        // In RCCPSettings, if useFixedWheelColliders is true, it sets mass based on the vehicle mass.
        if (RCCPSettings.useFixedWheelColliders)
            WheelCollider.mass = CarController.Rigid.mass / WHEEL_MASS_DIVIDER;

        // Creating a pivot at the correct position and rotation for the wheel model.
        GameObject newPivot = new GameObject("Pivot_" + wheelModel.transform.name);

        newPivot.transform.SetPositionAndRotation(RCCP_GetBounds.GetBoundsCenter(wheelModel.transform), transform.rotation);
        newPivot.transform.SetParent(wheelModel.transform.parent, true);

        // Assigning the actual wheel model to the new pivot.
        wheelModel.SetParent(newPivot.transform, true);
        wheelModel = newPivot.transform;

        Invoke(nameof(CreateAudioSource), AUDIO_SOURCE_CREATION_DELAY);

    }

    /// <summary>
    /// Unity OnEnable method.
    /// </summary>
    public override void OnEnable() {

        base.OnEnable();

    }

    /// <summary>
    /// Unity Update method. Optionally aligns the visual wheel model.
    /// </summary>
    private void Update() {

        if (alignWheels)
            WheelAlign();

    }

    /// <summary>
    /// Unity FixedUpdate method. Calculates speed from RPM, applies motor/brake torque, handles friction, skidmarks, etc.
    /// </summary>
    private void FixedUpdate() {

        // If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        // Convert RPM to approximate speed (km/h).
        CalculateWheelSpeed();

        // Execute all wheel physics calculations
        MotorTorque();
        BrakeTorque();
        GroundMaterial();
        Frictions();
        SkidMarks();
        WheelTemp();
        Audio();

        engineBrakeTorqueNm = 0f;

    }

    #endregion

    #region Performance Optimization Methods

    /// <summary>
    /// Caches frequently used component references for performance.
    /// </summary>
    private void CacheReferences() {

        cachedGroundMaterials = RCCP_GroundMaterials.Instance;
        cachedSkidmarksManager = RCCP_SkidmarksManager.Instance;

    }

    /// <summary>
    /// Calculates wheel speed from RPM with null checking and optimization.
    /// </summary>
    private void CalculateWheelSpeed() {

        if (WheelCollider == null)
            return;

        float circumference = 2.0f * Mathf.PI * WheelCollider.radius;

        if (Mathf.Abs(WheelRPM) > SPEED_THRESHOLD_CHECK)
            wheelRPM2Speed = (circumference * WheelRPM) * CIRCUMFERENCE_MULTIPLIER;
        else
            wheelRPM2Speed = 0f;

    }

    #endregion

    #region Visual Alignment Methods

    /// <summary>
    /// Aligning wheel model position and rotation to match the WheelCollider, accounting for camber/caster.
    /// </summary>
    private void WheelAlign() {

        // Return if no wheel model selected.
        if (wheelModel == null)
            return;

        // If wheelcollider is not enabled, hide or disable model. Otherwise show it.
        wheelModel.gameObject.SetActive(WheelCollider.enabled);

        // If wheelcollider is not enabled yet, return.
        if (!WheelCollider.enabled)
            return;

        // Positions and rotations of the wheel.
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        // Getting position and rotation from WheelCollider.
        WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);

        // Increase the rotation value based on RPM.
        this.wheelRotation += WheelRPM * RPM_TO_DEGREES_MULTIPLIER * Time.deltaTime;

        // Assigning position and rotation to the wheel model.
        wheelModel.transform.SetPositionAndRotation(wheelPosition, transform.rotation * Quaternion.Euler(this.wheelRotation, WheelCollider.steerAngle, 0f));

        // Adjust offset by X axis to simulate different rim offsets.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.position += (transform.right * offset);
        else
            wheelModel.transform.position -= (transform.right * offset);

        // Adjusting camber angle by Z axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, -camber);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.forward, camber);

        // Adjusting caster angle by X axis.
        if (transform.localPosition.x < 0f)
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, -caster);
        else
            wheelModel.transform.RotateAround(wheelModel.transform.position, transform.right, caster);

    }

    #endregion

    #region Torque Application Methods

    /// <summary>
    /// Applies the accumulated motorTorque to the WheelCollider's motorTorque, factoring in traction cuts (ESP/TCS) and overtorque checks.
    /// FIXED: Now properly handles negative torque for engine braking and over-rev protection.
    /// </summary>
    private void MotorTorque() {

        float torque = motorTorque;
        bool positiveTorque = torque >= 0f;

        // IMPORTANT: Only apply traction control cuts to positive (driving) torque
        // Negative torque (engine braking) should NOT be limited by ESP/TCS systems

        if (positiveTorque) {

            // Cut traction for ESP only on positive torque.
            if (cutTractionESP != 0f) {

                torque -= Mathf.Clamp(torque * (Mathf.Abs(SidewaysSlip) * cutTractionESP), 0f, Mathf.Infinity);
                torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

            }

            // Cut traction for TCS if there's forward slip, only on positive torque.
            if (cutTractionTCS != 0f && Mathf.Abs(ForwardSlip) > .05f) {

                if (Mathf.Sign(WheelRPM) >= 0) {

                    torque -= Mathf.Clamp(torque * (Mathf.Abs(ForwardSlip) * cutTractionTCS), 0f, Mathf.Infinity);
                    torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

                }

            }

        } else {

            // For negative torque (engine braking), only apply TCS if wheel is spinning backwards
            if (cutTractionTCS != 0f && Mathf.Abs(ForwardSlip) > .05f) {

                if (Mathf.Sign(WheelRPM) < 0) {

                    torque += Mathf.Clamp(-torque * (Mathf.Abs(ForwardSlip) * cutTractionTCS), 0f, Mathf.Infinity);
                    torque = Mathf.Clamp(torque, -Mathf.Infinity, 0f);

                }

            }

        }

        if (Mathf.Abs(torque) < 1f)
            torque = 0f;

        // Only zero out positive (driving) torque when overtorque conditions are met.
        // Engine braking (negative torque) must still work to allow deceleration.
        if (positiveTorque && CheckOvertorque())
            torque = 0f;

        // Prevent positive torque when wheels are spinning backwards fast
        // (e.g. applying forward power while rolling in reverse above 50 km/h).
        if (positiveTorque && Mathf.Abs(wheelRPM2Speed) > 50f && wheelRPM2Speed < -50f)
            torque = 0f;

        // Fake engine brake: subtract a real Nm magnitude opposing wheel rotation.
        // Sign-aware so reversing also produces a decelerating force; deadbanded near 0
        // to avoid ping-pong at standstill.
        if (engineBrakeTorqueNm > 0f) {

            float dirSpeed = wheelRPM2Speed;

            if (Mathf.Abs(dirSpeed) > 0.5f)
                torque -= engineBrakeTorqueNm * Mathf.Sign(dirSpeed);

        }

        // Apply final torque to wheel collider
        WheelCollider.motorTorque = torque;

        // Reset values for next frame
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        motorTorque = 0f;

    }

    /// <summary>
    /// Applies the accumulated brakeTorque to the WheelCollider's brakeTorque, factoring in ABS cuts.
    /// </summary>
    private void BrakeTorque() {

        float torque = brakeTorque;

        // ABS brake cut.
        if (cutBrakeABS != 0f) {

            torque -= Mathf.Clamp(torque * cutBrakeABS, 0f, Mathf.Infinity);
            torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        }

        torque = Mathf.Clamp(torque, 0f, Mathf.Infinity);

        if (torque < MIN_TORQUE_THRESHOLD)
            torque = 0f;

        WheelCollider.brakeTorque = torque;

        cutBrakeABS = 0f;
        brakeTorque = 0f;

    }

    #endregion

    #region Physics Calculation Methods

    /// <summary>
    /// Manages friction curves and updates slip values. Also applies drift mode changes if enabled.
    /// </summary>
    private void Frictions() {

        // Null check for cached ground materials
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        // Null check before accessing ground materials array
        if (groundIndex >= 0 && groundIndex < cachedGroundMaterials.frictions.Length) {

            if (TotalSlip >= cachedGroundMaterials.frictions[groundIndex].slip)
                isSkidding = true;
            else
                isSkidding = false;

            // Setting stiffness of the forward and sideways friction curves (multiplied by grip).
            // Must copy struct, modify, then assign back (WheelFrictionCurve is a value type)
            var forwardFriction = WheelCollider.forwardFriction;
            var sidewaysFriction = WheelCollider.sidewaysFriction;
            forwardFriction.stiffness = cachedGroundMaterials.frictions[groundIndex].forwardStiffness * grip;
            sidewaysFriction.stiffness = ((cachedGroundMaterials.frictions[groundIndex].sidewaysStiffness * grip * (1f - (handbrakeInput / HANDBRAKE_DIVISOR))) * connectedAxle.tractionHelpedSidewaysStiffness);

            // If wheel is deflated, multiply the stiffness by the deflatedStiffnessMultiplier.
            if (deflated) {

                forwardFriction.stiffness *= deflatedStiffnessMultiplier;
                sidewaysFriction.stiffness *= deflatedStiffnessMultiplier;

            }

            // Apply drift friction multipliers set by RCCP_Stability.
            if (driftMode) {

                forwardFriction.stiffness *= driftForwardStiffnessMultiplier;
                sidewaysFriction.stiffness *= driftSidewaysStiffnessMultiplier;

            }

            WheelCollider.forwardFriction = forwardFriction;
            WheelCollider.sidewaysFriction = sidewaysFriction;

        }

        handbrakeInput = 0f;

        // Control wheel damping based on motor torque. Previously this lerped the damping rate down
        // to zero at high torque, which removed the single best tool Unity gives us for damping the
        // Euler-integration instability of the wheel spin ODE — the exact moment we need damping most
        // is when motor torque is being applied. The blend still allows a modest reduction under torque
        // (so acceleration feel is preserved), but enforces a 50% floor of the ground material's damp.
        if (groundIndex >= 0 && groundIndex < cachedGroundMaterials.frictions.Length) {

            float groundDamp = cachedGroundMaterials.frictions[groundIndex].damp;
            float dampBlend = Mathf.Clamp01(Mathf.Abs(WheelCollider.motorTorque) / 100f);
            WheelCollider.wheelDampingRate = Mathf.Lerp(groundDamp, groundDamp * 0.5f, dampBlend);

        }

    }

    /// <summary>
    /// Updates wheel temperature based on slip and cools it over time.
    /// </summary>
    private void WheelTemp() {

        if (isSkidding)
            totalWheelTemp += Time.fixedDeltaTime * WHEEL_TEMPERATURE_INCREASE_RATE * TotalSlip;

        totalWheelTemp -= Time.fixedDeltaTime * WHEEL_TEMPERATURE_DECREASE_RATE;
        totalWheelTemp = Mathf.Clamp(totalWheelTemp, MIN_WHEEL_TEMPERATURE, MAX_WHEEL_TEMPERATURE);

    }

    #endregion

    #region Ground Material Detection Methods

    /// <summary>
    /// Determines the appropriate ground material index by checking contact's PhysicMaterial or terrain texture.
    /// </summary>
    private void GroundMaterial() {

        isGrounded = WheelCollider.GetGroundHit(out wheelHit);

        // If there are no contact points, set default index to 0.
        if (!isGrounded || wheelHit.point == Vector3.zero || wheelHit.collider == null) {

            groundIndex = 0;
            return;

        }

        // Null check for cached ground materials
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null) {

            groundIndex = 0;
            return;

        }

        // Contacted any physic material in Configurable Ground Materials yet?
        bool contactedWithAnyMaterialYet = false;

        // Checking the material of the contact point in the RCCP_GroundMaterials ground frictions.
        for (int i = 0; i < cachedGroundMaterials.frictions.Length; i++) {

            // If there is one, assign the index of the material. 
            if (wheelHit.collider.sharedMaterial == cachedGroundMaterials.frictions[i].groundMaterial) {

                contactedWithAnyMaterialYet = true;
                groundIndex = i;
                break; // Performance optimization: break when found

            }

        }

        // If ground PhysicMaterial is not found among configured ground materials, check if we are on a terrain collider.
        if (!contactedWithAnyMaterialYet) {

            // If terrains are not initialized yet, return.
            if (!RCCPSceneManager.terrainsInitialized) {

                groundIndex = 0;
                return;

            }

            // Null check for terrain ground materials
            if (cachedGroundMaterials.terrainFrictions == null) {

                groundIndex = 0;
                return;

            }

            // Checking the material of the contact point in the RCCP_GroundMaterials terrain frictions.
            for (int i = 0; i < cachedGroundMaterials.terrainFrictions.Length; i++) {

                if (wheelHit.collider.sharedMaterial == cachedGroundMaterials.terrainFrictions[i].groundMaterial) {

                    RCCP_SceneManager.Terrains currentTerrain = null;

                    // Null check for terrains array
                    if (RCCPSceneManager.terrains != null) {

                        for (int l = 0; l < RCCPSceneManager.terrains.Length; l++) {

                            if (RCCPSceneManager.terrains[l] != null && RCCPSceneManager.terrains[l].terrainCollider == cachedGroundMaterials.terrainFrictions[i].groundMaterial) {
                                currentTerrain = RCCPSceneManager.terrains[l];
                                break;
                            }

                        }

                    }

                    // Once we have that terrain, get exact position in the terrain map coordinate.
                    if (currentTerrain != null && currentTerrain.terrain != null) {

                        Vector3 playerPos = transform.position;
                        Vector3 TerrainCord = ConvertToSplatMapCoordinate(currentTerrain.terrain, playerPos);
                        float comp = 0f;

                        // Null check for splatmap data
                        if (currentTerrain.mSplatmapData != null && TerrainCord.x >= 0 && TerrainCord.z >= 0 &&
                            TerrainCord.x < currentTerrain.alphamapWidth && TerrainCord.z < currentTerrain.alphamapHeight) {

                            // Finding the right terrain texture around the hit position.
                            for (int k = 0; k < currentTerrain.mNumTextures; k++) {

                                if (comp < currentTerrain.mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, k])
                                    groundIndex = k;

                            }

                            // Null check for splatmap indexes before assignment
                            if (cachedGroundMaterials.terrainFrictions[i].splatmapIndexes != null &&
                                groundIndex >= 0 && groundIndex < cachedGroundMaterials.terrainFrictions[i].splatmapIndexes.Length) {

                                // Assign the index of the material based on splatmap indexes.
                                groundIndex = cachedGroundMaterials.terrainFrictions[i].splatmapIndexes[groundIndex].index;

                            }

                        }

                    }

                    break; // Performance optimization: break when terrain found

                }

            }

        }

    }

    #endregion

    #region Skidmarks Methods

    /// <summary>
    /// Handles skidmark generation based on slip threshold and wheel contact.
    /// </summary>
    private void SkidMarks() {

        // If drawing skids are not enabled, return.
        if (!drawSkid)
            return;

        // Null checks for safety
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        if (groundIndex < 0 || groundIndex >= cachedGroundMaterials.frictions.Length)
            return;

        // If slip is above the ground friction slip threshold...
        if (TotalSlip > cachedGroundMaterials.frictions[groundIndex].slip) {

            Vector3 skidPoint = wheelHit.point + (CarController.Rigid.linearVelocity * Time.deltaTime);

            // If velocity is nonzero and the wheel is grounded, record a new skidmark.
            if (CarController.Rigid.linearVelocity.magnitude > .1f && isGrounded && wheelHit.normal != Vector3.zero && wheelHit.point != Vector3.zero && skidPoint != Vector3.zero && Mathf.Abs(skidPoint.magnitude) >= .1f) {

                if (cachedSkidmarksManager != null)
                    lastSkidmark = cachedSkidmarksManager.AddSkidMark(skidPoint, wheelHit.normal, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip, width, lastSkidmark, groundIndex);

            } else {

                lastSkidmark = -1;

            }

        } else {

            // Slip is not above threshold, reset last skidmark index.
            lastSkidmark = -1;

        }

    }

    #endregion

    #region Audio Methods

    /// <summary>
    /// Creating audiosource for skid SFX.
    /// </summary>
    private void CreateAudioSource() {

        if (skidAudioSource != null)
            return;

        // Null check for CarController and Audio components
        if (CarController == null)
            return;

        if (CarController.Audio != null && CarController.Audio.audioMixer != null)
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.Audio.audioMixer, CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);
        else
            skidAudioSource = RCCP_AudioSource.NewAudioSource(CarController.gameObject, "Skid Sound AudioSource", 3f, 50f, 0f, null, true, true, false);

        if (CarController.Audio != null && skidAudioSource != null) {

            if (CarController.Audio.transform.childCount > 0)
                skidAudioSource.transform.SetParent(CarController.Audio.transform.GetChild(0), true);
            else
                skidAudioSource.transform.SetParent(CarController.Audio.transform, true);

        }

    }

    /// <summary>
    /// Manages the skid audio playback by monitoring total slip and applying volumes/pitches.
    /// Also calculates a bump force when the wheel hits large forces.
    /// </summary>
    private void Audio() {

        // Null checks for safety
        if (cachedGroundMaterials == null || cachedGroundMaterials.frictions == null)
            return;

        if (groundIndex < 0 || groundIndex >= cachedGroundMaterials.frictions.Length)
            return;

        if (skidAudioSource != null) {

            // If total slip is high enough, play skid SFX.
            if (TotalSlip > cachedGroundMaterials.frictions[groundIndex].slip) {

                skidClip = cachedGroundMaterials.frictions[groundIndex].groundSound;
                skidVolume = cachedGroundMaterials.frictions[groundIndex].volume;

                if (skidAudioSource.clip != skidClip)
                    skidAudioSource.clip = skidClip;

                if (!skidAudioSource.isPlaying)
                    skidAudioSource.Play();

                if (CarController.Rigid.linearVelocity.magnitude > .1f) {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, Mathf.Lerp(0f, skidVolume, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 10f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, Mathf.Lerp(.7f, 1f, TotalSlip - cachedGroundMaterials.frictions[groundIndex].slip), Time.fixedDeltaTime * 10f);

                } else {

                    skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 10f);
                    skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 10f);

                }

            } else {

                skidAudioSource.volume = Mathf.Lerp(skidAudioSource.volume, 0f, Time.fixedDeltaTime * 10f);
                skidAudioSource.pitch = Mathf.Lerp(skidAudioSource.pitch, 1f, Time.fixedDeltaTime * 10f);

                if (skidAudioSource.volume <= MIN_AUDIO_VOLUME_TO_STOP && skidAudioSource.isPlaying)
                    skidAudioSource.Stop();

            }

            if (skidAudioSource.volume < SKID_VOLUME_THRESHOLD)
                skidAudioSource.volume = 0f;

        }

        // Calculate bump force based on difference in hit force.
        bumpForce = wheelHit.force - oldForce;

        // If bump force is high enough, you could play a bump SFX here.
        if ((bumpForce) >= BUMP_FORCE_THRESHOLD) {
            // Example: Trigger bump sounds, apply random pitch, etc.
        }

        oldForce = wheelHit.force;

    }

    #endregion

    #region Input Methods

    /// <summary>
    /// Applies Ackermann steering geometry to this wheel based on the given steering angle.
    /// </summary>
    /// <param name="steeringAngle">Input steering angle in degrees</param>
    public void ApplySteering(float steeringAngle) {

        if (!WheelCollider.enabled)
            return;

        float avgAngleDeg = steeringAngle;
        float avgAngleRad = avgAngleDeg * Mathf.Deg2Rad;

        float radiusInside = wheelbase / Mathf.Tan(Mathf.Abs(avgAngleRad));
        float finalAngleDeg;

        bool turningRight = steeringAngle > 0f;
        bool turningLeft = steeringAngle < 0f;
        bool thisIsLeftWheel = transform.localPosition.x < 0f;

        if (turningRight) {

            if (thisIsLeftWheel) {

                // Outside wheel (left) during right turn - larger turning radius
                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            } else {

                // Inside wheel (right) during right turn - smaller turning radius
                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            }

        } else if (turningLeft) {

            if (thisIsLeftWheel) {

                // Inside wheel (left) during left turn - smaller turning radius
                float insideAngleRad = Mathf.Atan(wheelbase / (radiusInside - trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * insideAngleRad;

            } else {

                // Outside wheel (right) during left turn - larger turning radius
                float outsideAngleRad = Mathf.Atan(wheelbase / (radiusInside + trackWidth * 0.5f));
                finalAngleDeg = Mathf.Rad2Deg * outsideAngleRad;

            }

            finalAngleDeg *= -1f;

        } else {

            finalAngleDeg = 0f;

        }

        WheelCollider.steerAngle = finalAngleDeg;

    }

    /// <summary>
    /// Adds motor torque (Nm) to be applied in the next FixedUpdate. Positive for forward, negative for reverse.
    /// </summary>
    /// <param name="torque">Motor torque in Newton-meters</param>
    public void AddMotorTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        motorTorque += torque;

    }

    /// <summary>
    /// Adds brake torque (Nm) to be applied in the next FixedUpdate.
    /// </summary>
    /// <param name="torque">Brake torque in Newton-meters</param>
    public void AddBrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;

    }

    /// <summary>
    /// Adds handbrake torque (Nm) to be applied in the next FixedUpdate, also sets the handbrake input factor.
    /// </summary>
    /// <param name="torque">Handbrake torque in Newton-meters</param>
    public void AddHandbrakeTorque(float torque) {

        if (!WheelCollider.enabled)
            return;

        brakeTorque += torque;
        handbrakeInput += Mathf.Clamp01(torque / 1000f);

    }

    /// <summary>
    /// Accumulates fake engine-brake torque (Nm at this wheel) for the current physics frame.
    /// Applied as negative motor torque opposing wheel rotation in MotorTorque(), then reset.
    /// </summary>
    /// <param name="nmAtWheel">Engine-brake torque at this wheel, in Nm. Negative values are ignored.</param>
    public void AddEngineBrakeTorque(float nmAtWheel) {

        if (!WheelCollider.enabled)
            return;

        if (nmAtWheel <= 0f)
            return;

        engineBrakeTorqueNm += nmAtWheel;

    }

    /// <summary>
    /// Legacy normalized feedback input kept for backwards compatibility. Use AddEngineBrakeTorque for new code.
    /// </summary>
    /// <param name="intensity">Normalized feedback intensity in the old 0-1 range.</param>
    [System.Obsolete("Use AddEngineBrakeTorque(float nmAtWheel) instead.")]
    public void AddNegativeFeedback(float intensity) {

        AddEngineBrakeTorque(Mathf.Clamp01(intensity) * LegacyFeedbackTorqueNm);

    }

    /// <summary>
    /// Cuts traction torque (ESP) to control slip. Larger values reduce more motor torque.
    /// </summary>
    /// <param name="_cutTraction">ESP traction cut factor (0-1)</param>
    public void CutTractionESP(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionESP = _cutTraction;

    }

    /// <summary>
    /// Cuts traction torque (TCS) for forward slip. Larger values reduce more motor torque.
    /// </summary>
    /// <param name="_cutTraction">TCS traction cut factor (0-1)</param>
    public void CutTractionTCS(float _cutTraction) {

        if (!WheelCollider.enabled)
            return;

        cutTractionTCS = _cutTraction;

    }

    /// <summary>
    /// Cuts brake torque (ABS) to prevent wheel lock. Larger values reduce more brake torque.
    /// </summary>
    /// <param name="_cutBrake">ABS brake cut factor (0-1)</param>
    public void CutBrakeABS(float _cutBrake) {

        if (!WheelCollider.enabled)
            return;

        cutBrakeABS = _cutBrake;

    }

    #endregion

    #region Special State Methods

    /// <summary>
    /// Deflates the wheel, reducing radius and friction stiffness. Triggers events in CarController.
    /// </summary>
    public void Deflate() {

        if (!WheelCollider.enabled)
            return;

        if (deflated)
            return;

        deflated = true;

        if (defRadius == -1)
            defRadius = WheelCollider.radius;

        WheelCollider.radius = defRadius * deflatedRadiusMultiplier;

        if (CarController != null && CarController.Rigid != null) {
            CarController.Rigid.AddForceAtPosition(transform.right * UnityEngine.Random.Range(-1f, 1f) * 25f, transform.position, ForceMode.Acceleration);
            CarController.OnWheelDeflated();
        }

        //  Mark vehicle as needing repair so repair zones can fix deflated wheels.
        if (CarController != null && CarController.Damage != null)
            CarController.Damage.repaired = false;

    }

    /// <summary>
    /// Inflates the wheel, restoring normal friction stiffness and handling.
    /// </summary>
    public void Inflate() {

        if (!WheelCollider.enabled)
            return;

        if (!deflated)
            return;

        deflated = false;

        if (defRadius != -1)
            WheelCollider.radius = defRadius;

        if (CarController != null)
            CarController.OnWheelInflated();

    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if the wheel should stop receiving motor torque due to engine state or speed limits.
    /// </summary>
    /// <returns>True if motor torque should be cut</returns>
    private bool CheckOvertorque() {

        // Null checks for safety
        if (CarController == null)
            return true;

        if (!CarController.engineRunning)
            return true;

        if (CarController.absoluteSpeed > CarController.maximumSpeed)
            return true;

        // Null check for Gearbox before accessing
        if (CarController.Gearbox != null && CarController.Gearbox.TargetSpeeds != null && CarController.Gearbox.currentGear >= 0 && CarController.Gearbox.currentGear < CarController.Gearbox.TargetSpeeds.Length) {

            if (Mathf.Abs(wheelRPM2Speed) > (CarController.Gearbox.TargetSpeeds[CarController.Gearbox.currentGear] * GEAR_SPEED_TOLERANCE))
                return true;

        }

        return false;

    }

    #endregion

    #region Friction Configuration Methods

    /// <summary>
    /// Sets the forward friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    /// <param name="extremumSlip">Slip value at peak grip</param>
    /// <param name="extremumValue">Peak grip value</param>
    /// <param name="asymptoteSlip">Slip value at sliding friction</param>
    /// <param name="asymptoteValue">Sliding friction value</param>
    public void SetFrictionCurvesForward(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        WheelCollider.forwardFriction = newCurve;

    }

    /// <summary>
    /// Sets the sideways friction curves of the wheel. Allows customizing slip and grip levels.
    /// </summary>
    /// <param name="extremumSlip">Slip value at peak grip</param>
    /// <param name="extremumValue">Peak grip value</param>
    /// <param name="asymptoteSlip">Slip value at sliding friction</param>
    /// <param name="asymptoteValue">Sliding friction value</param>
    public void SetFrictionCurvesSideways(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue) {

        WheelFrictionCurve newCurve = new WheelFrictionCurve();
        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;
        WheelCollider.sidewaysFriction = newCurve;

    }

    /// <summary>
    /// Sets the suspension spring, damper and travel distance on this WheelCollider.
    /// </summary>
    /// <param name="spring">Spring force in N/m.</param>
    /// <param name="damper">Damper force in N·s/m.</param>
    /// <param name="distance">Suspension travel distance in meters.</param>
    public void SetSuspension(float spring, float damper, float distance) {

        JointSpring js = WheelCollider.suspensionSpring;
        js.spring = spring;
        js.damper = damper;
        WheelCollider.suspensionSpring = js;
        WheelCollider.suspensionDistance = distance;

    }

    /// <summary>
    /// Sets only the suspension spring and damper force on this WheelCollider, leaving travel distance and target position untouched.
    /// Used by behavior-preset multipliers which should not overwrite vehicle-specific ride height.
    /// </summary>
    /// <param name="spring">Spring force in N/m.</param>
    /// <param name="damper">Damper force in N·s/m.</param>
    public void SetSuspensionForces(float spring, float damper) {

        JointSpring js = WheelCollider.suspensionSpring;
        js.spring = spring;
        js.damper = damper;
        WheelCollider.suspensionSpring = js;

    }

    #endregion

    #region Terrain Support Methods

    /// <summary>
    /// Converts world position to terrain splat map coordinates for checking terrain texture indexes.
    /// </summary>
    /// <param name="terrain">Target terrain</param>
    /// <param name="playerPos">World position to convert</param>
    /// <returns>Terrain coordinates in splat map space</returns>
    private Vector3 ConvertToSplatMapCoordinate(Terrain terrain, Vector3 playerPos) {

        if (terrain == null || terrain.terrainData == null)
            return Vector3.zero;

        Vector3 vecRet = new Vector3();
        Vector3 terPosition = terrain.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / terrain.terrainData.size.x) * terrain.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / terrain.terrainData.size.z) * terrain.terrainData.alphamapHeight;
        return vecRet;

    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Aligns wheel model with wheel collider in the Editor. Adjusts WheelCollider radius and position based on the model mesh bounds.
    /// Position is intentionally independent of suspensionDistance / targetPosition so that tuning those values in the inspector does not drift the WheelCollider transform.
    /// Writes are gated on a sub-millimeter epsilon so repeat calls don't dirty prefabs with floating-point noise from world-space bounds computations.
    /// Radius uses absolute lossy scale so nested-scale parents and mirrored (negative-scale) wheels can't produce a bad radius.
    /// </summary>
    public void AlignWheel() {

        if (!WheelCollider.enabled)
            return;

        if (wheelModel == null)
            return;

        const float kPositionEpsilon = 0.001f;
        const float kRadiusEpsilon = 0.0005f;

        Vector3 targetPos = RCCP_GetBounds.GetBoundsCenter(wheelModel);
        if ((transform.position - targetPos).sqrMagnitude >= kPositionEpsilon * kPositionEpsilon)
            transform.position = targetPos;

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY > 0f) {
            float targetRadius = RCCP_GetBounds.MaxBoundsExtent(wheelModel) / scaleY;
            if (Mathf.Abs(WheelCollider.radius - targetRadius) >= kRadiusEpsilon)
                WheelCollider.radius = targetRadius;
        }

        WheelCollider.transform.position += WheelCollider.transform.up * (Mathf.Clamp(WheelCollider.suspensionDistance, kPositionEpsilon, 1f) / 2f);

    }

    /// <summary>
    /// Detaches the wheel by creating a physics-enabled copy and disabling the original.
    /// </summary>
    public void DetachWheel() {

        if (!WheelCollider.enabled)
            return;

        if (wheelModel != null && !wheelModel.gameObject.activeSelf)
            return;

        // Create detached wheel copy
        GameObject clonedWheel = Instantiate(wheelModel.gameObject, wheelModel.transform.position, wheelModel.transform.rotation, null);
        clonedWheel.SetActive(true);
        clonedWheel.AddComponent<Rigidbody>();

        // Add mesh collider for physics
        GameObject clonedMeshCollider = new GameObject("MeshCollider");
        clonedMeshCollider.transform.SetParent(clonedWheel.transform, false);
        clonedMeshCollider.transform.position = RCCP_GetBounds.GetBoundsCenter(clonedWheel.transform);
        MeshCollider mc = clonedMeshCollider.AddComponent<MeshCollider>();
        MeshFilter biggestMesh = RCCP_GetBounds.GetBiggestMesh(clonedWheel.transform);

        if (biggestMesh != null && biggestMesh.mesh != null) {
            mc.sharedMesh = biggestMesh.mesh;
            mc.convex = true;
        }

        // Set appropriate layer
        clonedMeshCollider.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);

        foreach (Transform item in clonedMeshCollider.GetComponentsInChildren<Transform>(true)) {
            item.gameObject.layer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);
        }

        // Disable original wheel
        WheelCollider.enabled = false;

    }

    /// <summary>
    /// Repairs the wheel by re-enabling it and restoring visual model.
    /// </summary>
    public void OnRepair() {

        if (WheelCollider.enabled)
            return;

        WheelCollider.enabled = true;

        if (wheelModel != null)
            wheelModel.gameObject.SetActive(true);

        Inflate();

    }

    /// <summary>
    /// Resets all runtime fields (torques, slip, audio) for this wheel. Useful when toggling the wheel on/off.
    /// </summary>
    public void Reload() {

        engineBrakeTorqueNm = 0f;
        motorTorque = 0f;
        brakeTorque = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        wheelRotation = 0f; // Fixed: removed duplicate assignment
        cutTractionESP = 0f;
        cutTractionTCS = 0f;
        cutBrakeABS = 0f;
        bumpForce = 0f;
        oldForce = 0f;
        lastSkidmark = -1;
        skidVolume = 0f;
        defRadius = -1f; // Reset cached radius for deflation system

        if (skidAudioSource != null) {

            skidAudioSource.volume = 0f;
            skidAudioSource.pitch = 1f;

        }

    }

    #endregion

    #region Unity Editor Methods

    /// <summary>
    /// Unity Reset method. Called when script is first added or component is reset in the Editor. 
    /// Initializes some default values for WheelCollider.
    /// </summary>
    private void Reset() {

        if (!TryGetComponent<WheelCollider>(out var wc))
            return;

        // Increasing mass of the wheel for more stable handling.
        if (RCCP_Settings.Instance.useFixedWheelColliders) {

            RCCP_CarController carController = GetComponentInParent<RCCP_CarController>(true);
            if (carController != null && carController.Rigid != null)
                wc.mass = carController.Rigid.mass / WHEEL_MASS_DIVIDER;

        }

        wc.forceAppPointDistance = WHEEL_FORCE_APP_POINT_DISTANCE;
        wc.suspensionDistance = WHEEL_SUSPENSION_DISTANCE;

        JointSpring js = wc.suspensionSpring;
        js.spring = WHEEL_SPRING_VALUE;
        js.damper = WHEEL_DAMPER_VALUE;
        js.targetPosition = .6f;
        wc.suspensionSpring = js;

        WheelFrictionCurve frictionCurveFwd = wc.forwardFriction;
        frictionCurveFwd.extremumSlip = FORWARD_FRICTION_EXTREMUM_SLIP;
        wc.forwardFriction = frictionCurveFwd;

        WheelFrictionCurve frictionCurveSide = wc.sidewaysFriction;
        frictionCurveSide.extremumSlip = SIDEWAYS_FRICTION_EXTREMUM_SLIP;
        wc.sidewaysFriction = frictionCurveSide;

    }

    #endregion

}

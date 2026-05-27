//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

/// <summary>
/// PhysX vehicle sub-stepping profile selected per <see cref="RCCP_Settings.BehaviorType"/>.
/// Each profile picks a (speedThreshold, stepsBelowThreshold, stepsAboveThreshold) triple
/// for <see cref="UnityEngine.WheelCollider.ConfigureVehicleSubsteps"/>. Configured centrally
/// in <c>RCCP_CarController.ConfigureWheelSubsteps()</c>.
/// </summary>
public enum RCCP_WheelSubstepProfile {

    /// <summary>Realistic road cars at moderate speeds — 10 m/s, 12 below, 8 above. (default)</summary>
    Realistic = 0,
    /// <summary>Arcade racing — 20 m/s, 10 below, 6 above. Arcade-feel handling.</summary>
    Arcade = 1,
    /// <summary>Off-road / bumpy terrain — 10 m/s, 14 below, 10 above. Extra contact accuracy on uneven ground.</summary>
    OffRoad = 2,
    /// <summary>High-speed racing — 30 m/s, 22 below, 16 above. Heavy substepping for stability under high-force racing.</summary>
    HighSpeed = 3

}

/// <summary>
/// Stored all general shared RCCP settings here.
/// </summary>
[System.Serializable]
public class RCCP_Settings : ScriptableObject {

    #region singleton
    private static RCCP_Settings instance;
    /// <summary>
    /// Singleton instance of the RCCP settings, loaded from Resources.
    /// </summary>
    public static RCCP_Settings Instance { get { if (instance == null) instance = Resources.Load("RCCP_Settings") as RCCP_Settings; return instance; } }
    #endregion

    /// <summary>
    /// Current behavior.
    /// </summary>
    public BehaviorType SelectedBehaviorType {

        get {

            if (!overrideBehavior)
                return null;

            if (behaviorTypes == null || behaviorTypes.Length == 0)
                return null;

            if (behaviorSelectedIndex < 0 || behaviorSelectedIndex >= behaviorTypes.Length)
                return null;

            return behaviorTypes[behaviorSelectedIndex];

        }

    }

    [Header("General")]
    /// <summary>
    /// Use multithreading if current platform is supported. Fallback to false if platform is not supported.
    /// </summary>
    [Tooltip("Enables multithreading on supported platforms for better physics performance.")]
    public bool multithreading = true;

    /// <summary>
    /// Current selected behavior index.
    /// </summary>
    [Tooltip("Index of the currently active behavior preset in the behaviorTypes array.")]
    [Min(0)] public int behaviorSelectedIndex = 0;

    /// <summary>
    /// Override FPS?
    /// </summary>
    [Tooltip("Overrides Application.targetFrameRate with the maxFPS value below.")]
    public bool overrideFPS = true;

    /// <summary>
    /// Override fixed timestep?
    /// </summary>
    [Tooltip("Overrides Time.fixedDeltaTime with the fixedTimeStep value below.")]
    public bool overrideFixedTimeStep = true;

    /// <summary>
    /// Overriden fixed timestep value.
    /// </summary>
    [Tooltip("Physics fixed timestep in seconds; lower values improve accuracy but cost more CPU.")]
    [Range(.005f, .06f)] public float fixedTimeStep = .02f;

    /// <summary>
    /// Maximum angular velocity.
    /// </summary>
    [Tooltip("Maximum angular velocity for vehicle rigidbodies; prevents unrealistic spinning.")]
    [Range(.5f, 20f)] public float maxAngularVelocity = 6;

    /// <summary>
    /// Maximum FPS.
    /// </summary>
    [Tooltip("Target frame rate when overrideFPS is enabled.")]
    [Min(1)] public int maxFPS = 120;

    /// <summary>
    /// Project-wide default steering curve copied into <see cref="RCCP_Input.steeringCurve"/> when a new
    /// RCCP_Input component is added or reset. Hidden from the normal inspector — edit only via Debug mode.
    /// If null or empty, RCCP_Input falls back to its hard-coded 3-keyframe default.
    /// X = speed (km/h), Y = max steer multiplier (0-1).
    /// </summary>
    public AnimationCurve defaultSteeringCurve;

    [Header("Behavior")]
    /// <summary>
    /// Override the behavior?
    /// </summary>
    [Tooltip("Applies the selected behavior preset to all vehicles, overriding their individual settings.")]
    public bool overrideBehavior = true;

    /// <summary>
    /// Defines a driving behavior preset that controls stability aids, steering, differential, drift, and wheel friction parameters.
    /// </summary>
    [System.Serializable]
    public class BehaviorType {

        /// <summary>
        /// Behavior name.
        /// </summary>
        [Tooltip("Display name for this behavior preset shown in the inspector and UI.")]
        public string behaviorName = "New Behavior";

        //  Driving helpers.
        [Header("Stability")]
        /// <summary>
        /// Enables Anti-lock Braking System to prevent wheel lockup under braking.
        /// </summary>
        [Tooltip("Enables Anti-lock Braking System to prevent wheel lockup under heavy braking.")]
        public bool ABS = true;
        /// <summary>
        /// Enables Electronic Stability Program to prevent oversteer and understeer.
        /// </summary>
        [Tooltip("Enables Electronic Stability Program to reduce oversteer and understeer.")]
        public bool ESP = true;
        /// <summary>
        /// Enables Traction Control System to prevent wheel spin during acceleration.
        /// </summary>
        [Tooltip("Enables Traction Control System to limit wheel spin during acceleration.")]
        public bool TCS = true;
        /// <summary>
        /// Enables steering correction that helps align the vehicle with its velocity direction.
        /// </summary>
        [Tooltip("Applies steering correction that aligns the vehicle with its velocity direction.")]
        public bool steeringHelper = true;
        /// <summary>
        /// Enables traction assistance that limits wheel spin based on surface grip.
        /// </summary>
        [Tooltip("Limits wheel spin based on surface grip to improve traction.")]
        public bool tractionHelper = true;
        /// <summary>
        /// Enables angular drag assistance to dampen rotational velocity at higher speeds.
        /// </summary>
        [Tooltip("Dampens rotational velocity at higher speeds using additional angular drag.")]
        public bool angularDragHelper = false;

        [Tooltip("Enables drift mode. Applies force-based drift assistance and reduces rear tire grip for controlled sliding.")]
        public bool driftMode = false;
        [Tooltip("Limits the maximum drift angle. Dampens angular velocity when drift angle exceeds the limit to prevent uncontrollable spins.")]
        public bool driftAngleLimiter = false;
        [Tooltip("Maximum allowed drift angle in degrees before correction forces are applied.")]
        [Range(0f, 90f)] public float driftAngleLimit = 30f;
        [Tooltip("How aggressively the drift angle is corrected when exceeding the limit. Higher values mean faster correction.")]
        [Range(0f, 10f)] public float driftAngleCorrectionFactor = 3f;

        //  Drift forces.
        [Header("Drift Forces")]
        [Tooltip("Multiplier for yaw torque applied during drift. Higher values make the car rotate faster when drifting.")]
        [Range(0f, 3f)] public float driftYawTorqueMultiplier = 0.7f;
        [Tooltip("Forward push force during drift to maintain speed. Higher values reduce speed loss while sliding.")]
        [Range(0f, 5000f)] public float driftForwardForceMultiplier = 2000f;
        [Tooltip("Lateral push force during drift. Higher values push the car further sideways for wider drifts.")]
        [Range(0f, 4000f)] public float driftSidewaysForceMultiplier = 1500f;
        [Tooltip("Minimum speed (km/h) required for drift forces to activate. Below this speed, no drift assistance is applied.")]
        [Range(0f, 60f)] public float driftMinSpeed = 20f;
        [Tooltip("Speed (km/h) at which drift forces reach full strength. Forces scale linearly between min speed and this value.")]
        [Range(20f, 150f)] public float driftFullForceSpeed = 80f;
        [Tooltip("How much throttle input alone contributes to yaw rotation. Higher values allow initiating drift with throttle without steering.")]
        [Range(0f, 1f)] public float driftThrottleYawFactor = 0.3f;

        //  Drift friction.
        [Header("Drift Friction")]
        [Tooltip("Minimum rear tire sideways grip during full drift. Lower values allow more lateral sliding. 1.0 = no grip reduction.")]
        [Range(0.1f, 1f)] public float driftRearSidewaysStiffnessMin = 0.45f;
        [Tooltip("Minimum rear tire forward grip during full drift. Lower values cause more speed loss. 1.0 = no grip reduction.")]
        [Range(0.5f, 1f)] public float driftRearForwardStiffnessMin = 0.8f;
        [Tooltip("Minimum front tire sideways grip during drift. Higher values keep front-end responsive for steering control.")]
        [Range(0.5f, 1.2f)] public float driftFrontSidewaysStiffnessMin = 0.9f;
        [Tooltip("How quickly tire grip reduces when entering a drift. Higher values make grip loss more immediate.")]
        [Range(1f, 20f)] public float driftFrictionResponseSpeed = 8f;
        [Tooltip("How quickly tire grip recovers when exiting a drift. Higher values make grip recovery faster.")]
        [Range(1f, 20f)] public float driftFrictionRecoverySpeed = 4f;

        //  Drift recovery.
        [Header("Drift Recovery")]
        [Tooltip("Maximum angular velocity (deg/s) allowed during drift. Prevents uncontrollable spins. 0 = no limit.")]
        [Range(0f, 360f)] public float driftMaxAngularVelocity = 120f;
        [Tooltip("Multiplier for recovery force when counter-steering during drift. Higher values make recovery from drifts easier.")]
        [Range(1f, 5f)] public float driftCounterSteerRecoveryBoost = 2f;
        [Tooltip("Constant forward force applied during drift to maintain momentum. Higher values prevent speed loss while drifting.")]
        [Range(0f, 3000f)] public float driftMomentumMaintenanceForce = 800f;
        [Tooltip("Smoothing speed for drift force transitions. Higher values mean faster response, lower values mean smoother transitions.")]
        [Range(1f, 20f)] public float driftForceSmoothing = 8f;

        //  Steering.
        [Header("Steering")]
        /// <summary>
        /// Steering angle curve where X-axis is speed (km/h) and Y-axis is maximum steering angle (degrees).
        /// </summary>
        [Tooltip("Maps vehicle speed (km/h) on X-axis to maximum steering angle (degrees) on Y-axis.")]
        public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0f, 40f), new Keyframe(50f, 20f), new Keyframe(100f, 11f), new Keyframe(150f, 6f), new Keyframe(200f, 5f));
        /// <summary>
        /// Multiplier for steering input responsiveness.
        /// </summary>
        [Tooltip("Multiplier for how quickly steering input reaches full lock.")]
        [Min(0f)] public float steeringSensitivity = 1f;
        /// <summary>
        /// Enables automatic counter-steering to assist with oversteer recovery.
        /// </summary>
        [Tooltip("Automatically applies counter-steer input to help recover from oversteer.")]
        public bool counterSteering = true;
        /// <summary>
        /// Limits maximum steering angle based on vehicle speed using the steering curve.
        /// </summary>
        [Tooltip("Reduces maximum steering angle at higher speeds using the steering curve.")]
        public bool limitSteering = true;

        [Header("Differential")]
        /// <summary>
        /// Differential type used by the behavior (Open, Limited, FullLocked, or Direct).
        /// </summary>
        [Tooltip("Differential type (Open, Limited, FullLocked, or Direct) applied by this behavior.")]
        public RCCP_Differential.DifferentialType differentialType = RCCP_Differential.DifferentialType.Open;

        //  Counter steering limitations.
        [Space()]
        /// <summary>
        /// Minimum counter-steering strength multiplier.
        /// </summary>
        [Tooltip("Minimum counter-steer strength multiplier at low speed.")]
        [Min(0f)] public float counterSteeringMinimum = .5f;
        /// <summary>
        /// Maximum counter-steering strength multiplier.
        /// </summary>
        [Tooltip("Maximum counter-steer strength multiplier at high speed.")]
        [Min(0f)] public float counterSteeringMaximum = 1f;

        //  Steering sensitivity limitations.
        [Space()]
        /// <summary>
        /// Minimum steering speed sensitivity multiplier.
        /// </summary>
        [Tooltip("Lower bound for speed-dependent steering sensitivity scaling.")]
        [Min(0f)] public float steeringSpeedMinimum = .5f;
        /// <summary>
        /// Maximum steering speed sensitivity multiplier.
        /// </summary>
        [Tooltip("Upper bound for speed-dependent steering sensitivity scaling.")]
        [Min(0f)] public float steeringSpeedMaximum = 1f;

        //  Steering helper linear velocity limitations.
        /// <summary>
        /// Minimum steering helper linear velocity correction strength.
        /// </summary>
        [Tooltip("Lower bound for steering helper velocity correction strength.")]
        [Range(0f, 1f)] public float steeringHelperStrengthMinimum = .1f;
        /// <summary>
        /// Maximum steering helper linear velocity correction strength.
        /// </summary>
        [Tooltip("Upper bound for steering helper velocity correction strength.")]
        [Range(0f, 1f)] public float steeringHelperStrengthMaximum = 1f;

        //  Traction helper strength limitations.
        /// <summary>
        /// Minimum traction helper strength for limiting wheel spin.
        /// </summary>
        [Tooltip("Lower bound for traction helper wheel-spin limiting strength.")]
        [Range(0f, 1f)] public float tractionHelperStrengthMinimum = .1f;
        /// <summary>
        /// Maximum traction helper strength for limiting wheel spin.
        /// </summary>
        [Tooltip("Upper bound for traction helper wheel-spin limiting strength.")]
        [Range(0f, 1f)] public float tractionHelperStrengthMaximum = 1f;

        //  Angular drag limitations.
        /// <summary>
        /// Base angular drag value applied to the vehicle rigidbody.
        /// </summary>
        [Tooltip("Base angular drag applied to the vehicle rigidbody to resist rotation.")]
        [Range(0f, 10f)] public float angularDrag = .1f;
        /// <summary>
        /// Minimum angular drag helper strength multiplier.
        /// </summary>
        [Tooltip("Lower bound for speed-dependent angular drag assistance.")]
        [Range(0f, 1f)] public float angularDragHelperMinimum = .1f;
        /// <summary>
        /// Maximum angular drag helper strength multiplier.
        /// </summary>
        [Tooltip("Upper bound for speed-dependent angular drag assistance.")]
        [Range(0f, 1f)] public float angularDragHelperMaximum = 1f;

        //  Suspension.
        [Header("Suspension")]
        /// <summary>
        /// Multiplier applied to each wheel's vehicle-authored base spring rate (captured at Awake on RCCP_WheelCollider).
        /// 1 = vehicle default, greater than 1 stiffer (sport/racing), less than 1 softer (comfort). Scales correctly across vehicle masses.
        /// </summary>
        [Tooltip("Multiplier on each wheel's authored base spring rate. 1 = vehicle default, >1 stiffer, <1 softer.")]
        [Range(0.25f, 3f)] public float suspensionSpringMultiplier = 1f;
        /// <summary>
        /// Multiplier applied to each wheel's vehicle-authored base damper rate (captured at Awake on RCCP_WheelCollider).
        /// 1 = vehicle default. Usually tracks the spring multiplier but can be tuned independently for feel.
        /// </summary>
        [Tooltip("Multiplier on each wheel's authored base damper rate. 1 = vehicle default.")]
        [Range(0.25f, 3f)] public float suspensionDamperMultiplier = 1f;

        //  Wheel physics — PhysX vehicle sub-stepping profile.
        [Header("Wheel Physics")]
        /// <summary>
        /// Profile that selects the PhysX vehicle sub-stepping triple
        /// (speedThreshold, stepsBelowThreshold, stepsAboveThreshold) for this behavior.
        /// Applied centrally by <c>RCCP_CarController.ConfigureWheelSubsteps()</c> on Start
        /// and re-applied on every <c>OnBehaviorChanged</c> event.
        /// </summary>
        [Tooltip("PhysX vehicle sub-stepping profile applied to all wheels: Realistic (10/12/8, default), Arcade (20/10/6), OffRoad (10/14/10), HighSpeed (30/22/16). Higher counts trade CPU for stability.")]
        public RCCP_WheelSubstepProfile wheelSubstepProfile = RCCP_WheelSubstepProfile.Realistic;

        //  Anti roll limitations.
        [Space()]
        /// <summary>
        /// Minimum anti-roll bar force to reduce body roll in corners.
        /// </summary>
        [Tooltip("Minimum anti-roll bar force to reduce body roll in corners.")]
        [Min(0f)] public float antiRollMinimum = 500f;

        //  Gear shifting delay limitation.
        [Space()]
        /// <summary>
        /// RPM threshold (0-1 normalized) at which automatic gear shifts occur.
        /// </summary>
        [Tooltip("Normalized RPM threshold (0-1) at which the automatic gearbox shifts up.")]
        [Range(.1f, .9f)] public float gearShiftingThreshold = .8f;
        /// <summary>
        /// Minimum delay in seconds between automatic gear shifts.
        /// </summary>
        [Tooltip("Shortest allowed pause in seconds between consecutive gear shifts.")]
        [Range(0f, 1f)] public float gearShiftingDelayMinimum = .15f;
        /// <summary>
        /// Maximum delay in seconds between automatic gear shifts.
        /// </summary>
        [Tooltip("Longest allowed pause in seconds between consecutive gear shifts.")]
        [Range(0f, 1f)] public float gearShiftingDelayMaximum = .5f;

        //  Wheel frictions.
        [Header("Wheel Frictions Forward Front Side")]
        /// <summary>
        /// Front wheels forward friction extremum slip value.
        /// </summary>
        [Tooltip("Slip value at peak forward grip for front wheels.")]
        [Min(0f)] public float forwardExtremumSlip_F = .4f;
        /// <summary>
        /// Front wheels forward friction extremum force value.
        /// </summary>
        [Tooltip("Peak forward friction force for front wheels (normalized).")]
        [Min(0f)] public float forwardExtremumValue_F = 1f;
        /// <summary>
        /// Front wheels forward friction asymptote slip value.
        /// </summary>
        [Tooltip("Slip value at which forward friction reaches its asymptote for front wheels.")]
        [Min(0f)] public float forwardAsymptoteSlip_F = .8f;
        /// <summary>
        /// Front wheels forward friction asymptote force value.
        /// </summary>
        [Tooltip("Asymptotic forward friction force for front wheels (normalized).")]
        [Min(0f)] public float forwardAsymptoteValue_F = .5f;

        [Header("Wheel Frictions Forward Rear Side")]
        /// <summary>
        /// Rear wheels forward friction extremum slip value.
        /// </summary>
        [Tooltip("Slip value at peak forward grip for rear wheels.")]
        [Min(0f)] public float forwardExtremumSlip_R = .4f;
        /// <summary>
        /// Rear wheels forward friction extremum force value.
        /// </summary>
        [Tooltip("Peak forward friction force for rear wheels (normalized).")]
        [Min(0f)] public float forwardExtremumValue_R = .95f;
        /// <summary>
        /// Rear wheels forward friction asymptote slip value.
        /// </summary>
        [Tooltip("Slip value at which forward friction reaches its asymptote for rear wheels.")]
        [Min(0f)] public float forwardAsymptoteSlip_R = .75f;
        /// <summary>
        /// Rear wheels forward friction asymptote force value.
        /// </summary>
        [Tooltip("Asymptotic forward friction force for rear wheels (normalized).")]
        [Min(0f)] public float forwardAsymptoteValue_R = .5f;

        [Header("Wheel Frictions Sideways Front Side")]
        /// <summary>
        /// Front wheels sideways friction extremum slip value.
        /// </summary>
        [Tooltip("Slip value at peak lateral grip for front wheels.")]
        [Min(0f)] public float sidewaysExtremumSlip_F = .4f;
        /// <summary>
        /// Front wheels sideways friction extremum force value.
        /// </summary>
        [Tooltip("Peak lateral friction force for front wheels (normalized).")]
        [Min(0f)] public float sidewaysExtremumValue_F = 1f;
        /// <summary>
        /// Front wheels sideways friction asymptote slip value.
        /// </summary>
        [Tooltip("Slip value at which lateral friction reaches its asymptote for front wheels.")]
        [Min(0f)] public float sidewaysAsymptoteSlip_F = .5f;
        /// <summary>
        /// Front wheels sideways friction asymptote force value.
        /// </summary>
        [Tooltip("Asymptotic lateral friction force for front wheels (normalized).")]
        [Min(0f)] public float sidewaysAsymptoteValue_F = .75f;

        [Header("Wheel Frictions Sideways Rear Side")]
        /// <summary>
        /// Rear wheels sideways friction extremum slip value.
        /// </summary>
        [Tooltip("Slip value at peak lateral grip for rear wheels.")]
        [Min(0f)] public float sidewaysExtremumSlip_R = .4f;
        /// <summary>
        /// Rear wheels sideways friction extremum force value.
        /// </summary>
        [Tooltip("Peak lateral friction force for rear wheels (normalized).")]
        [Min(0f)] public float sidewaysExtremumValue_R = 1.05f;
        /// <summary>
        /// Rear wheels sideways friction asymptote slip value.
        /// </summary>
        [Tooltip("Slip value at which lateral friction reaches its asymptote for rear wheels.")]
        [Min(0f)] public float sidewaysAsymptoteSlip_R = .5f;
        /// <summary>
        /// Rear wheels sideways friction asymptote force value.
        /// </summary>
        [Tooltip("Asymptotic lateral friction force for rear wheels (normalized).")]
        [Min(0f)] public float sidewaysAsymptoteValue_R = .8f;

    }

    /// <summary>
    /// Behavior Types
    /// </summary>
    [Tooltip("Array of driving behavior presets; the selected index determines active behavior.")]
    public BehaviorType[] behaviorTypes;

    [Header("Wheel Physics")]
    /// <summary>
    /// Fixed wheelcolliders with higher mass will be used.
    /// </summary>
    [Tooltip("Uses heavier-mass fixed wheel colliders for more stable physics simulation.")]
    public bool useFixedWheelColliders = true;      //

    /// <summary>
    /// All vehicles can be resetted if upside down.
    /// </summary>
    [Tooltip("Automatically resets vehicles that flip upside down back onto their wheels.")]
    public bool autoReset = true;       //

    [Header("Units")]
    /// <summary>
    /// Information telemetry about current vehicle
    /// </summary>
    [Tooltip("Shows a real-time telemetry overlay with speed, RPM, gear, and other vehicle data.")]
    public bool useTelemetry = false;
    /// <summary>
    /// Displays input debugger overlay showing current control inputs.
    /// </summary>
    [Tooltip("Displays a debug overlay showing current throttle, brake, and steering inputs.")]
    public bool useInputDebugger = false;
    /// <summary>
    /// Uses miles per hour (MPH) instead of kilometers per hour (KMH) for speed display.
    /// </summary>
    [Tooltip("Switches speed display units from km/h to mph throughout the UI.")]
    public bool useMPH = false;

    /// <summary>
    /// Auto saves and loads the rebind map.
    /// </summary>
    [Tooltip("Automatically persists and restores custom input rebindings between sessions.")]
    public bool autoSaveLoadInputRebind = true;

    /// <summary>
    /// For mobile inputs
    /// </summary>
    public enum MobileController { TouchScreen, Gyro, SteeringWheel, Joystick }

    /// <summary>
    /// For mobile inputs
    /// </summary>
    [Header("Mobile Input")]
    [Tooltip("Active mobile input method: touch buttons, gyroscope, steering wheel, or joystick.")]
    public MobileController mobileController;

    /// <summary>
    /// Enable / disable the mobile controllers.
    /// </summary>
    [Tooltip("Enables or disables on-screen mobile controller UI elements.")]
    public bool mobileControllerEnabled = false;

    /// <summary>
    /// Accelerometer sensitivity
    /// </summary>
    [Tooltip("Sensitivity multiplier for gyroscope/accelerometer steering input on mobile.")]
    [Min(0f)] public float gyroSensitivity = 2.5f;

    [Header("Layers")]
    /// <summary>
    /// Setting layers.
    /// </summary>
    [Tooltip("Automatically assigns RCCP-specific layers to vehicles and their parts on spawn.")]
    public bool setLayers = true;

    /// <summary>
    /// Layer of the vehicle.
    /// </summary>
    [Tooltip("Physics layer name assigned to the main vehicle body collider.")]
    public string RCCPLayer = "RCCP_Vehicle";

    /// <summary>
    /// Wheelcollider layer.
    /// </summary>
    [Tooltip("Physics layer name assigned to wheel collider GameObjects.")]
    public string RCCPWheelColliderLayer = "RCCP_WheelCollider";

    /// <summary>
    /// Detachable part's layer.
    /// </summary>
    [Tooltip("Physics layer name assigned to detachable body parts (doors, bumpers, etc.).")]
    public string RCCPDetachablePartLayer = "RCCP_DetachablePart";

    /// <summary>
    /// Props layer.
    /// </summary>
    [Tooltip("Physics layer name assigned to interactive prop objects in the scene.")]
    public string RCCPPropLayer = "RCCP_Prop";

    /// <summary>
    /// Props layer.
    /// </summary>
    [Tooltip("Physics layer name assigned to obstacle objects that vehicles can collide with.")]
    public string RCCPObstacleLayer = "RCCP_Obstacle";

    [Header("Lights")]
    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    [Tooltip("Renders headlights as cheap vertex lights instead of per-pixel for better performance.")]
    public bool useHeadLightsAsVertexLights = false;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    [Tooltip("Renders brake lights as cheap vertex lights instead of per-pixel for better performance.")]
    public bool useBrakeLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    [Tooltip("Renders reverse lights as cheap vertex lights instead of per-pixel for better performance.")]
    public bool useReverseLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    [Tooltip("Renders indicator lights as cheap vertex lights instead of per-pixel for better performance.")]
    public bool useIndicatorLightsAsVertexLights = true;

    /// <summary>
    /// Used for using the lights more efficent and realistic. Vertex is not important, pixel is important.
    /// </summary>
    [Tooltip("Renders miscellaneous lights as vertex lights instead of per-pixel for better performance.")]
    public bool useOtherLightsAsVertexLights = true;

    #region Setup Prefabs

    // Light prefabs.
    [Header("Light Prefabs")]
    /// <summary>
    /// Default light configuration data for vehicle headlights, brake lights, and indicators.
    /// </summary>
    [Tooltip("Default light configuration data for headlights, brake lights, and indicators.")]
    public RCCP_LightSetupData lightsSetupData = new RCCP_LightSetupData();
    /// <summary>
    /// Prefab for the light-emitting box visualization used in vehicle lighting.
    /// </summary>
    [Tooltip("Prefab for the emissive box mesh used to visualize vehicle lights.")]
    public GameObject lightBox;

    //  Camera prefabs.
    [Header("Camera Prefabs")]
    /// <summary>
    /// Main RCCP camera prefab that follows the player vehicle.
    /// </summary>
    [Tooltip("Main chase camera prefab instantiated to follow the player vehicle.")]
    public RCCP_Camera RCCPMainCamera;
    /// <summary>
    /// Hood camera prefab mounted on the vehicle hood.
    /// </summary>
    [Tooltip("Camera prefab mounted on the vehicle hood for a driver-perspective view.")]
    public GameObject RCCPHoodCamera;
    /// <summary>
    /// Wheel camera prefab that focuses on a specific wheel.
    /// </summary>
    [Tooltip("Camera prefab focused on a wheel for close-up tire and suspension viewing.")]
    public GameObject RCCPWheelCamera;
    /// <summary>
    /// Cinematic camera prefab for dynamic camera angles.
    /// </summary>
    [Tooltip("Camera prefab that orbits or tracks the vehicle with cinematic angles.")]
    public GameObject RCCPCinematicCamera;
    /// <summary>
    /// Fixed camera prefab placed at static positions in the scene.
    /// </summary>
    [Tooltip("Camera prefab placed at static positions in the scene.")]
    public GameObject RCCPFixedCamera;

    //  UI prefabs.
    [Header("UI Prefabs")]
    /// <summary>
    /// UI Canvas prefab containing all RCCP dashboard and control elements.
    /// </summary>
    [Tooltip("Canvas prefab containing dashboard gauges, buttons, and mobile control elements.")]
    public GameObject RCCPCanvas;
    /// <summary>
    /// Telemetry display prefab showing real-time vehicle data.
    /// </summary>
    [Tooltip("Telemetry display prefab showing real-time speed, RPM, and tire data.")]
    public GameObject RCCPTelemetry;

    // Sound FX.
    [Header("Sound FX")]
    /// <summary>
    /// Audio mixer group used for all vehicle sound effects.
    /// </summary>
    [Tooltip("Audio mixer group that routes all vehicle sound effects for volume control.")]
    public AudioMixerGroup audioMixer;
    /// <summary>
    /// Engine sound clip for low RPM range (throttle on).
    /// </summary>
    [Tooltip("Audio clip played at low RPM when the throttle is applied.")]
    public AudioClip engineLowClipOn;
    /// <summary>
    /// Engine sound clip for low RPM range (throttle off).
    /// </summary>
    [Tooltip("Audio clip played at low RPM when the throttle is released.")]
    public AudioClip engineLowClipOff;
    /// <summary>
    /// Engine sound clip for medium RPM range (throttle on).
    /// </summary>
    [Tooltip("Audio clip played at medium RPM when the throttle is applied.")]
    public AudioClip engineMedClipOn;
    /// <summary>
    /// Engine sound clip for medium RPM range (throttle off).
    /// </summary>
    [Tooltip("Audio clip played at medium RPM when the throttle is released.")]
    public AudioClip engineMedClipOff;
    /// <summary>
    /// Engine sound clip for high RPM range (throttle on).
    /// </summary>
    [Tooltip("Audio clip played at high RPM when the throttle is applied.")]
    public AudioClip engineHighClipOn;
    /// <summary>
    /// Engine sound clip for high RPM range (throttle off).
    /// </summary>
    [Tooltip("Audio clip played at high RPM when the throttle is released.")]
    public AudioClip engineHighClipOff;
    /// <summary>
    /// Engine sound clip for idle RPM (throttle on).
    /// </summary>
    [Tooltip("Audio clip played at idle RPM with slight throttle input.")]
    public AudioClip engineIdleClipOn;
    /// <summary>
    /// Engine sound clip for idle RPM (throttle off).
    /// </summary>
    [Tooltip("Audio clip played at idle RPM with no throttle input.")]
    public AudioClip engineIdleClipOff;
    /// <summary>
    /// Engine start/cranking sound clip.
    /// </summary>
    [Tooltip("Audio clip played during engine cranking on startup.")]
    public AudioClip engineStartClip;
    /// <summary>
    /// Reversing beep or whine sound clip.
    /// </summary>
    [Tooltip("Audio clip for the reversing warning beep or whine sound.")]
    public AudioClip reversingClip;
    /// <summary>
    /// Wind noise sound clip that intensifies with speed.
    /// </summary>
    [Tooltip("Audio clip for wind noise that increases in volume with vehicle speed.")]
    public AudioClip windClip;
    /// <summary>
    /// Brake squeal sound clip played when braking at low speed.
    /// </summary>
    [Tooltip("Audio clip for brake squeal heard when braking at low speed.")]
    public AudioClip brakeClip;
    /// <summary>
    /// Sound clip for tire deflation.
    /// </summary>
    [Tooltip("Audio clip played when a tire loses pressure and deflates.")]
    public AudioClip wheelDeflateClip;
    /// <summary>
    /// Sound clip for tire inflation.
    /// </summary>
    [Tooltip("Audio clip played when a tire is re-inflated to normal pressure.")]
    public AudioClip wheelInflateClip;
    /// <summary>
    /// Sound clip for driving on a flat tire.
    /// </summary>
    [Tooltip("Audio clip for the rhythmic thumping sound of driving on a flat tire.")]
    public AudioClip wheelFlatClip;
    /// <summary>
    /// Turn indicator click sound clip.
    /// </summary>
    [Tooltip("Audio clip for the clicking sound when turn indicators are active.")]
    public AudioClip indicatorClip;
    /// <summary>
    /// Suspension bump sound clip for impacts and road irregularities.
    /// </summary>
    [Tooltip("Audio clip for suspension impacts when hitting bumps or road irregularities.")]
    public AudioClip bumpClip;
    /// <summary>
    /// Nitrous oxide activation sound clip.
    /// </summary>
    [Tooltip("Audio clip played when the nitrous oxide system activates.")]
    public AudioClip NOSClip;
    /// <summary>
    /// Turbocharger spool and blow-off sound clip.
    /// </summary>
    [Tooltip("Audio clip for turbocharger spooling and blow-off valve sounds.")]
    public AudioClip turboClip;
    /// <summary>
    /// Gear shift sound clips, one per gear change.
    /// </summary>
    [Tooltip("Audio clips played on each gear shift, one randomly selected per change.")]
    public AudioClip[] gearClips;
    /// <summary>
    /// Collision impact sound clips, randomly selected on crash.
    /// </summary>
    [Tooltip("Audio clips randomly selected and played on vehicle collision impacts.")]
    public AudioClip[] crashClips;
    /// <summary>
    /// Tire blowout sound clips.
    /// </summary>
    [Tooltip("Audio clips played when a tire blows out from damage or over-pressure.")]
    public AudioClip[] blowoutClip;
    /// <summary>
    /// Exhaust backfire flame sound clips.
    /// </summary>
    [Tooltip("Audio clips for exhaust backfire pops heard during deceleration.")]
    public AudioClip[] exhaustFlameClips;

    //  Particles
    [Header("Particles")]
    /// <summary>
    /// Particle effect prefab for general surface contact.
    /// </summary>
    [Tooltip("Particle prefab spawned at tire contact points on dusty or loose surfaces.")]
    public GameObject contactParticles;
    /// <summary>
    /// Particle effect prefab for metal scratching on collision.
    /// </summary>
    [Tooltip("Particle prefab for metal scratch sparks on vehicle body collision.")]
    public GameObject scratchParticles;
    /// <summary>
    /// Particle effect prefab for wheel sparks on hard surfaces.
    /// </summary>
    [Tooltip("Particle prefab for sparks emitted by wheels scraping hard surfaces.")]
    public GameObject wheelSparkleParticles;

    //  Other prefabs.
    [Header("Other Prefabs")]
    /// <summary>
    /// Exhaust gas particle effect prefab.
    /// </summary>
    [Tooltip("Particle prefab for exhaust pipe smoke and gas emission.")]
    public GameObject exhaustGas;
    /// <summary>
    /// Skidmarks manager prefab handling tire mark rendering on surfaces.
    /// </summary>
    [Tooltip("Prefab that manages shared skidmark mesh rendering for all vehicles.")]
    public RCCP_SkidmarksManager skidmarksManager;
    /// <summary>
    /// Wheel blur visual effect prefab for high-speed wheel rotation.
    /// </summary>
    [Tooltip("Visual effect prefab that blurs wheel spokes at high rotation speeds.")]
    public GameObject wheelBlur;

    [Header("Rendering")]
    /// <summary>
    /// Lens flare data asset used for light flare effects (SRP).
    /// </summary>
    [Tooltip("SRP lens flare data asset applied to vehicle light flare effects.")]
    public Object lensFlareData;
    /// <summary>
    /// Legacy lens flare asset used for built-in render pipeline.
    /// </summary>
    [Tooltip("Legacy built-in render pipeline lens flare asset for vehicle lights.")]
    public Flare flare;
    /// <summary>
    /// Lens flare prefab instantiated on vehicle lights.
    /// </summary>
    [Tooltip("Prefab instantiated on vehicle lights to display lens flare effects.")]
    public GameObject flarePrefab;

    /// <summary>
    /// HDRP Volume Profile prefab for high-definition rendering settings.
    /// </summary>
    [Tooltip("HDRP Volume Profile prefab for post-processing and rendering overrides.")]
    public GameObject hdrpVolumeProfilePrefab;
    /// <summary>
    /// Default material applied to vehicle decal projectors.
    /// </summary>
    [Tooltip("Default material assigned to decal projectors on vehicle surfaces.")]
    public Material defaultDecalMaterial;
    /// <summary>
    /// Default material applied to vehicle neon underglow lights.
    /// </summary>
    [Tooltip("Default material assigned to neon underglow light meshes.")]
    public Material defaultNeonMaterial;

    /// <summary>
    /// Physics material applied to vehicle body colliders for friction and bounce.
    /// </summary>
    [Tooltip("Physics material controlling friction and bounciness of the vehicle body collider.")]
    public Object vehicleColliderMaterial;

    #endregion

    // Used for folding sections of RCCP Settings.
    /// <summary>
    /// Editor fold state for the General Settings section.
    /// </summary>
    [Tooltip("Expands or collapses the General Settings section in the inspector.")]
    public bool foldGeneralSettings = false;
    /// <summary>
    /// Editor fold state for the Behavior Settings section.
    /// </summary>
    [Tooltip("Expands or collapses the Behavior Settings section in the inspector.")]
    public bool foldBehaviorSettings = false;
    /// <summary>
    /// Editor fold state for the Controller Settings section.
    /// </summary>
    [Tooltip("Expands or collapses the Controller Settings section in the inspector.")]
    public bool foldControllerSettings = false;
    /// <summary>
    /// Editor fold state for the UI Settings section.
    /// </summary>
    [Tooltip("Expands or collapses the UI Settings section in the inspector.")]
    public bool foldUISettings = false;
    /// <summary>
    /// Editor fold state for the Wheel Physics section.
    /// </summary>
    [Tooltip("Expands or collapses the Wheel Physics section in the inspector.")]
    public bool foldWheelPhysics = false;
    /// <summary>
    /// Editor fold state for the Optimization section.
    /// </summary>
    [Tooltip("Expands or collapses the Optimization section in the inspector.")]
    public bool foldOptimization = false;
    /// <summary>
    /// Editor fold state for the Tags and Layers section.
    /// </summary>
    [Tooltip("Expands or collapses the Tags and Layers section in the inspector.")]
    public bool foldTagsAndLayers = false;
    /// <summary>
    /// Editor fold state for the Extensions section.
    /// </summary>
    [Tooltip("Expands or collapses the Extensions section in the inspector.")]
    public bool foldExtensions = false;
    /// <summary>
    /// Editor fold state for the Resources Settings section.
    /// </summary>
    [Tooltip("Expands or collapses the Resources Settings section in the inspector.")]
    [FormerlySerializedAs("resourcesSettings")]
    public bool foldResourcesSettings = false;

    /// <summary>
    /// Editor fold state for the Resources - Light Prefabs sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Light Prefabs sub-section under Resources.")]
    public bool foldResourcesLightPrefabs = false;
    /// <summary>
    /// Editor fold state for the Resources - Camera Prefabs sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Camera Prefabs sub-section under Resources.")]
    public bool foldResourcesCameraPrefabs = false;
    /// <summary>
    /// Editor fold state for the Resources - UI Prefabs sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the UI Prefabs sub-section under Resources.")]
    public bool foldResourcesUIPrefabs = false;
    /// <summary>
    /// Editor fold state for the Resources - Particles sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Particles sub-section under Resources.")]
    public bool foldResourcesParticles = false;
    /// <summary>
    /// Editor fold state for the Resources - Sound FX sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Sound FX sub-section under Resources.")]
    public bool foldResourcesSoundFX = false;
    /// <summary>
    /// Editor fold state for the Resources - Other Prefabs sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Other Prefabs sub-section under Resources.")]
    public bool foldResourcesOtherPrefabs = false;
    /// <summary>
    /// Editor fold state for the Resources - Rendering sub-section.
    /// </summary>
    [Tooltip("Expands or collapses the Rendering sub-section under Resources.")]
    public bool foldResourcesRendering = false;

}

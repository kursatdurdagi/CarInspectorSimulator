//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright (c) 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages various stability systems for the vehicle:
/// - ABS (Anti-skid Braking System),
/// - ESP (Electronic Stability Program),
/// - TCS (Traction Control System),
/// plus steering, traction, and angular-drag helpers.
///
/// Steering reference for ESP:
/// ESP V2 uses <c>FrontAxle.steerAngle</c> as its reference steer angle — the vehicle-commanded
/// wheel angle after RCCP_Input processing (steering curve, counter-steer, steering limiter).
/// This is the actual state the wheels are in, so the bicycle-model reference yaw reflects what
/// the vehicle is commanding rather than what the player requested. Using player-side intent here
/// would over-estimate target yaw at high speeds where the steering curve attenuates, causing
/// false understeer classification.
///
/// Player vs vehicle input split:
/// CarController exposes both <c>steerInput_P</c> (player-processed) and <c>steerInput_V</c>
/// (vehicle-realized, averaged from axle.steerAngle / maxSteerAngle) as semantically distinct
/// signals after the VehicleInputs() rewire. ESP reads the authoritative per-axle state directly.
/// The AngularDragHelper branch that compares |steerInput_V| against |steerInput_P| is
/// load-bearing — it detects wheel-vs-request slew lag and counter-steer contributions.
/// </summary>
[DefaultExecutionOrder(-1)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Stability")]
public class RCCP_Stability : RCCP_Component {

    /// <summary>
    /// ESP control mode.
    /// - Normal: standard activation thresholds, full engine torque reduction, full correction.
    /// - Sport: dead bands widened ~2x, engine torque reduction disabled. Brake differential still active.
    /// </summary>
    public enum ESPMode { Normal, Sport }

    [Header("Axle References")]
    /// <summary>
    /// Reference to the axle manager component.
    /// </summary>
    [Tooltip("Reference to the axle manager component.")]
    public RCCP_Axles AxleManager;

    /// <summary>
    /// Reference to the front axle.
    /// </summary>
    [Tooltip("Reference to the front axle for ESP calculations.")]
    public RCCP_Axle frontAxle;

    /// <summary>
    /// Reference to the rear axle.
    /// </summary>
    [Tooltip("Reference to the rear axle for ESP calculations.")]
    public RCCP_Axle rearAxle;

    /// <summary>
    /// Collection of axles that provide power to wheels.
    /// </summary>
    [Tooltip("Collection of powered axles for TCS calculations.")]
    public List<RCCP_Axle> poweredAxles = new List<RCCP_Axle>();

    /// <summary>
    /// Collection of axles used for steering.
    /// </summary>
    [Tooltip("Collection of steering axles.")]
    public List<RCCP_Axle> steeringAxles = new List<RCCP_Axle>();

    /// <summary>
    /// Collection of axles used for braking.
    /// </summary>
    [Tooltip("Collection of braked axles for ABS calculations.")]
    public List<RCCP_Axle> brakedAxles = new List<RCCP_Axle>();

    [Header("ABS")]
    /// <summary>
    /// Enable / disable ABS.
    /// </summary>
    [Tooltip("Enable or disable the Anti-lock Braking System (ABS). When enabled, prevents wheel lockup during heavy braking by modulating brake pressure.")]
    public bool ABS = true;

    [Header("ESP")]
    /// <summary>
    /// Enable / disable ESP.
    /// </summary>
    [Tooltip("Enable or disable the Electronic Stability Program (ESP). When enabled, applies selective braking to individual wheels to correct oversteer and understeer.")]
    public bool ESP = true;

    [Header("TCS")]
    /// <summary>
    /// Enable / disable TCS.
    /// </summary>
    [Tooltip("Enable or disable the Traction Control System (TCS). When enabled, reduces engine torque when driven wheels lose traction to prevent wheelspin.")]
    public bool TCS = true;

    /// <summary>
    /// ABS threshold. If slip * brakeInput exceeds this, ABS will engage to reduce brake torque.
    /// </summary>
    [Tooltip("ABS engagement threshold. If wheel slip multiplied by brake input exceeds this value, ABS activates to reduce brake torque. Lower values make ABS more sensitive.")]
    [Range(.01f, .5f)] public float engageABSThreshold = .5f;

    /// <summary>
    /// ESP activation dead band in degrees per second. ESP engages when yaw rate error exceeds this threshold.
    /// Paired with espDeactivationDeadband for hysteresis. Production ESP typical: 4 deg/s activation, 2 deg/s deactivation.
    /// </summary>
    [Tooltip("ESP activation threshold (deg/s). ESP engages when yaw rate error exceeds this value. Lower = more aggressive, Higher = more driver freedom. Production typical: 4 deg/s.")]
    [Range(0.5f, 10f)] public float espDeadband = 6f;

    [Header("ESP V2")]
    /// <summary>
    /// ESP control mode. Sport widens dead bands ~2x and disables engine torque reduction, leaving only brake differential.
    /// </summary>
    [Tooltip("ESP control mode. Normal: standard thresholds, full correction. Sport: widened thresholds, no engine cut — for experienced drivers.")]
    public ESPMode espMode = ESPMode.Sport;

    /// <summary>
    /// Arcade option. Strength of the compensating forward force applied during ESP brake intervention,
    /// offsetting the longitudinal deceleration ESP brakes produce. 0 = disabled (realistic: ESP bleeds
    /// speed through the corner). 1 = full cancellation (arcade: corrective yaw moment still works,
    /// but the car does not slow down). Intermediate values give a partial preservation. Does not
    /// counteract engine torque reduction — combine with espMode=Sport to disable that separately.
    /// </summary>
    [Tooltip("Arcade speed preservation. 0 = off (realistic, ESP bleeds speed). 1 = full cancellation of ESP brake deceleration. Intermediate values partially preserve speed. Does not affect engine torque cut.")]
    [Range(0f, 1f)] public float preserveSpeedFactor = 1f;

    /// <summary>
    /// ESP deactivation dead band in degrees per second. Once engaged, ESP releases only when yaw rate error
    /// drops below this lower threshold. Creates a hysteresis band to prevent rapid on/off cycling near the
    /// engagement boundary. Automatically clamped to be less than or equal to espDeadband.
    /// </summary>
    [Tooltip("ESP deactivation threshold (deg/s). Once engaged, ESP releases only when yaw error drops below this. Should be 1-3 deg/s lower than activation. Production typical: 2 deg/s.")]
    [Range(0.1f, 5f)] public float espDeactivationDeadband = 2.5f;

    /// <summary>
    /// Minimum time in seconds that ESP remains engaged after activation. Once triggered, ESP stays engaged
    /// at least this long even if the error dips below the deactivation threshold. Prevents flutter during
    /// transient events. Production ESP typical: 100-200 ms.
    /// </summary>
    [Tooltip("Minimum ESP intervention time (seconds). Once engaged, ESP stays engaged for at least this duration. Prevents flutter. Production typical: 0.1-0.2s.")]
    [Range(0f, 0.5f)] public float espMinInterventionTime = 0.055f;

    /// <summary>
    /// Minimum max-wheel brake torque (Nm) before the dashboard-facing ESPIndicatorEngaged flag turns true.
    /// The real ESP engagement state is still exposed through ESPEngaged the moment ESP decides to
    /// intervene — this threshold only gates the user-visible indicator so dashboard lights do not flicker
    /// during micro-corrections the driver cannot feel. Set to 0 to make ESPIndicatorEngaged mirror
    /// ESPEngaged directly. Typical production ESP intervention is
    /// 100-500 Nm; 150-250 Nm works well as a dashboard-light threshold.
    /// </summary>
    [Tooltip("UI gate: ESPIndicatorEngaged flips true only when max smoothed brake torque exceeds this value (Nm). Prevents dashboard light flicker during micro-corrections. 0 = disabled.")]
    [Range(0f, 500f)] public float espMinNoticeableBrakeTorque = 75f;

    /// <summary>
    /// Minimum time (seconds) the dashboard-facing ESPIndicatorEngaged flag remains true after the brake-torque
    /// threshold is last crossed upward. Debounces the UI signal against brake-torque Lerp
    /// oscillation near espMinNoticeableBrakeTorque. The internal _espPersistentlyEngaged state
    /// is not affected — this only applies to the user-visible dashboard indicator. Fast turn-on,
    /// slow turn-off. Set to 0 to disable (raw threshold gate).
    /// </summary>
    [Tooltip("UI debounce: how long ESPIndicatorEngaged stays true after brake dips below the UI threshold. Prevents dashboard flicker when brake torque oscillates near the threshold. 0 = disabled.")]
    [Range(0f, 1f)] public float espUIMinHoldTime = 0.1f;

    /// <summary>
    /// Understeer gradient K_us in rad·s²/m. Used in the bicycle model denominator:
    /// ψ̇_ref = V · δ / (L + K_us · V²). Positive values reduce demanded yaw at high speed to match real vehicle
    /// understeer (all production passenger cars). Typical: 0.0035. Sporty: 0.002. Comfort/SUV: 0.005+.
    /// </summary>
    [Tooltip("Understeer gradient K_us (rad·s²/m). Shapes ESP's reference yaw rate at high speed to match natural vehicle understeer. Typical passenger car: 0.0035. Sportier: 0.002. Comfort/SUV: 0.005.")]
    [Range(0f, 0.01f)] public float understeerGradient = 0.01f;

    /// <summary>
    /// Estimated road friction coefficient μ. Clamps ESP's reference yaw rate to the friction limit:
    /// ψ̇_max = μ · g / V. Without runtime μ estimation, set this to match typical gameplay surface.
    /// 0.9-1.0 = dry asphalt, 0.5-0.7 = wet, 0.2-0.4 = snow/ice.
    /// </summary>
    [Tooltip("Estimated road friction coefficient μ. Clamps ESP reference yaw to physical limit. 0.9-1.0 = dry, 0.5-0.7 = wet, 0.2-0.4 = snow/ice.")]
    [Range(0.1f, 1.2f)] public float estimatedMu = 0.85f;

    /// <summary>
    /// ESP proportional gain base (multiplied by vehicle mass to form final P gain).
    /// Higher = stronger correction per unit yaw error. Default 2.5 matches typical production ESP calibration.
    /// </summary>
    [Tooltip("ESP proportional gain base (multiplied by vehicle mass). Higher = stronger correction per unit yaw error.")]
    [Range(0.5f, 10f)] public float espPGain = 4f;

    /// <summary>
    /// ESP derivative gain base (multiplied by vehicle mass). Dampens rapid yaw changes and accelerates
    /// response to transient events (e.g., lift-off oversteer which develops in 200-500 ms).
    /// </summary>
    [Tooltip("ESP derivative gain base (multiplied by vehicle mass). Dampens rapid yaw changes and speeds response to lift-off oversteer transients.")]
    [Range(0f, 2f)] public float espDGain = 0.3f;

    /// <summary>
    /// Time constant (seconds) of the first-order lag filter applied to the reference yaw rate.
    /// Prevents commanding instantaneous yaw rate changes from rapid steering inputs. Production typical: 0.1-0.3 s.
    /// </summary>
    [Tooltip("Reference yaw rate lag time constant (seconds). Smooths response to rapid steering inputs. Production typical: 0.1-0.3s.")]
    [Range(0.05f, 0.3f)] public float yawRateTimeConstant = 0.25f;

    /// <summary>
    /// Minimum time (seconds) ESP holds the current oversteer/understeer classification before allowing a mode switch.
    /// Prevents wrong-wheel braking during rapid classification flips at transition boundaries. Sideslip spin risk overrides.
    /// </summary>
    [Tooltip("Minimum time (seconds) ESP holds the oversteer/understeer classification before allowing a flip. Prevents wrong-wheel braking during transients.")]
    [Range(0f, 0.5f)] public float espModeCommitTime = 0.2f;

    /// <summary>
    /// Sideslip angle limit (degrees). When |β| exceeds this, ESP treats the condition as developing spin
    /// (forces oversteer classification) even if yaw rate error alone would suggest understeer.
    /// Defines the outer boundary of the controllable envelope. Production typical: 6-12 deg on dry asphalt.
    /// </summary>
    [Tooltip("Sideslip angle limit (deg). When |β| exceeds this, ESP treats the condition as developing spin. Typical: 6-12 deg on dry asphalt.")]
    [Range(3f, 20f)] public float sideslipMaxAngle = 10f;

    /// <summary>
    /// Sideslip angle rate limit (deg/s). When |dβ/dt| exceeds this, ESP promotes classification to oversteer.
    /// Provides earlier detection of developing spin than yaw rate error alone.
    /// </summary>
    [Tooltip("Sideslip rate limit (deg/s). When |dβ/dt| exceeds this, ESP promotes to oversteer. Earlier detection than yaw error alone.")]
    [Range(5f, 30f)] public float sideslipMaxRate = 15f;

    /// <summary>
    /// TCS threshold. If forward slip on powered wheels exceeds this, TCS will engage to reduce motor torque.
    /// </summary>
    [Tooltip("TCS engagement threshold. If forward wheel slip on powered wheels exceeds this value, TCS activates to reduce engine torque. Lower values make TCS more sensitive.")]
    [Range(.01f, .5f)] public float engageTCSThreshold = .35f;

    /// <summary>
    /// How strongly ABS reduces brake torque.
    /// </summary>
    [Tooltip("ABS intensity multiplier. Higher values result in more aggressive brake torque reduction when ABS is engaged.")]
    [Range(0f, 1f)] public float ABSIntensity = 1f;

    /// <summary>
    /// How strongly ESP brakes wheels to correct over/under steering.
    /// </summary>
    [Tooltip("ESP intensity multiplier. Higher values result in more aggressive corrective braking to stabilize the vehicle.")]
    [Range(0f, 1f)] public float ESPIntensity = .5f;

    /// <summary>
    /// How strongly TCS reduces torque to wheels if slipping.
    /// </summary>
    [Tooltip("TCS intensity multiplier. Higher values result in more aggressive torque reduction when wheels are spinning.")]
    [Range(0f, 1f)] public float TCSIntensity = .25f;

    /// <summary>
    /// True if ABS is currently engaged on at least one wheel.
    /// </summary>
    [Tooltip("True if ABS is currently active on at least one wheel.")]
    public bool ABSEngaged = false;

    /// <summary>
    /// True if ESP control logic is currently intervening to stabilize the vehicle.
    /// </summary>
    [Tooltip("Raw ESP runtime state. True whenever ESP is actively correcting oversteer or understeer, even for micro-corrections.")]
    public bool ESPEngaged = false;

    /// <summary>
    /// True if ESP intervention is strong enough to light the dashboard indicator.
    /// </summary>
    [Tooltip("Dashboard-facing ESP state. True only when ESP intervention exceeds the UI brake threshold and hold settings.")]
    public bool ESPIndicatorEngaged = false;

    /// <summary>
    /// True if TCS is currently engaged to reduce excessive wheel slip under power.
    /// </summary>
    [Tooltip("True if TCS is currently reducing torque due to wheel slip.")]
    public bool TCSEngaged = false;

    [Header("ESP V2 Telemetry (read-only)")]
    /// <summary>
    /// Current actual yaw rate in degrees per second. Positive = turning right (clockwise from above, Unity left-handed).
    /// </summary>
    [Tooltip("Current actual yaw rate (deg/s). Positive = turning right (clockwise from above).")]
    public float debugYawActualDegS;

    /// <summary>
    /// Reference (desired) yaw rate from bicycle model in degrees per second, after friction cap and first-order lag filter.
    /// </summary>
    [Tooltip("Reference yaw rate from bicycle model (deg/s), after friction cap and lag filter.")]
    public float debugYawRefDegS;

    /// <summary>
    /// Yaw rate error in degrees per second. Actual minus reference. Sign indicates oversteer/understeer direction.
    /// </summary>
    [Tooltip("Yaw rate error (deg/s) = actual - reference. Sign indicates oversteer/understeer direction.")]
    public float debugYawErrorDegS;

    /// <summary>
    /// Estimated sideslip angle β in degrees. Angle between vehicle forward axis and velocity vector.
    /// Positive = vehicle pointed left of travel direction.
    /// </summary>
    [Tooltip("Estimated sideslip angle β (degrees). Angle between vehicle forward and velocity vector.")]
    public float debugSideslipAngleDeg;

    /// <summary>
    /// Sideslip angle rate dβ/dt in degrees per second. Rapid growth indicates developing spin.
    /// </summary>
    [Tooltip("Sideslip rate dβ/dt (deg/s). Rapid growth indicates developing spin.")]
    public float debugSideslipRateDegS;

    /// <summary>
    /// Currently active ESP brake wheel. -1 = none, 0 = FL, 1 = FR, 2 = RL, 3 = RR.
    /// </summary>
    [Tooltip("Currently active ESP brake wheel: -1 = none, 0 = FL, 1 = FR, 2 = RL, 3 = RR.")]
    public int debugActiveWheelIndex = -1;

    /// <summary>
    /// True if ESP is currently classifying the condition as oversteer (rotating more than commanded).
    /// </summary>
    [Tooltip("True if ESP is currently classifying the condition as oversteer.")]
    public bool debugIsOversteer;

    [Header("Steering Helper")]
    /// <summary>
    /// If true, adds a small force to reduce oversteer or understeer (steering helper).
    /// </summary>
    [Tooltip("Enable steering helper. Applies corrective forces at wheel contact points to reduce oversteer and understeer based on wheel load distribution.")]
    public bool steeringHelper = true;

    [Header("Traction Helper")]
    /// <summary>
    /// If true, reduces front tire stiffness if the vehicle is skidding significantly (traction helper).
    /// </summary>
    [Tooltip("Enable traction helper. Reduces front axle lateral stiffness when the vehicle is sliding to prevent spins and improve control recovery.")]
    public bool tractionHelper = true;

    [Header("Angular Drag Helper")]
    /// <summary>
    /// If true, increases angular drag as speed increases (angular drag helper).
    /// </summary>
    [Tooltip("Enable angular drag helper. Increases rotational damping at higher speeds for improved high-speed stability.")]
    public bool angularDragHelper = false;

    [Header("Drift Limiter")]
    /// <summary>
    /// If true, limits maximum drift angle. On extreme angles, angular velocity is damped.
    /// </summary>
    [Tooltip("Enable drift angle limiter. Dampens angular velocity when the drift angle exceeds the maximum allowed value to prevent uncontrollable spins.")]
    public bool driftAngleLimiter = false;

    /// <summary>
    /// Max allowed drift angle in degrees before it's partially corrected.
    /// </summary>
    [Tooltip("Maximum allowed drift angle in degrees. Beyond this angle, the drift angle limiter will dampen angular velocity.")]
    [Range(0f, 90f)] public float maxDriftAngle = 30f;

    /// <summary>
    /// How quickly the angular velocity is damped if the drift angle exceeds maxDriftAngle.
    /// </summary>
    [Tooltip("Drift angle correction factor. Higher values result in faster damping of angular velocity when drift angle exceeds the maximum.")]
    [Range(0f, 10f)] public float driftAngleCorrectionFactor = 2f;

    [Header("Helper Strengths")]
    /// <summary>
    /// Strength factor for steeringHelper.
    /// </summary>
    [Tooltip("Steering helper strength. Controls the intensity of corrective forces applied to reduce lateral slip and improve handling.")]
    [Range(0f, 1f)] public float steerHelperStrength = .025f;

    /// <summary>
    /// Strength factor for tractionHelper.
    /// </summary>
    [Tooltip("Traction helper strength. Controls how much front axle stiffness is reduced when the vehicle is sliding.")]
    [Range(0f, 1f)] public float tractionHelperStrength = .05f;

    /// <summary>
    /// Strength factor for angularDragHelper.
    /// </summary>
    [Tooltip("Angular drag helper strength. Controls how much additional rotational damping is applied at higher speeds.")]
    [Range(0f, 1f)] public float angularDragHelperStrength = .075f;

    [Header("Speed Scaling")]
    /// <summary>
    /// Minimum helper strength at low speeds (0 km/h). Lower values = more nimble at low speed.
    /// </summary>
    [Tooltip("Minimum helper strength at low speeds. Lower values make the vehicle more nimble and responsive at low speeds.")]
    [Range(0f, 1f)] public float minSpeedHelperStrength = .2f;

    /// <summary>
    /// Maximum helper strength at high speeds. Higher values = more stable at high speed.
    /// </summary>
    [Tooltip("Maximum helper strength at high speeds. Higher values provide more stability assistance at highway speeds.")]
    [Range(0f, 1f)] public float maxSpeedHelperStrength = 1f;

    /// <summary>
    /// Speed (km/h) at which helper reaches maximum strength. Lower = earlier stabilization.
    /// </summary>
    [Tooltip("Speed at which stability helpers reach maximum strength. Lower values mean full stabilization kicks in at lower speeds.")]
    [Range(0f, 200f)] public float fullHelperSpeed = 80f;

    /// <summary>
    /// Minimum steering angle to trigger steering assist (degrees). Prevents unnecessary calculations at near-zero steering.
    /// </summary>
    [Tooltip("Minimum steering angle to activate steering assist. Prevents unnecessary force calculations when steering is nearly centered.")]
    [Range(0.1f, 5f)] public float minSteerAngleForAssist = 1f;

    /// <summary>
    /// Minimum speed for steering assist to activate (km/h). More responsive at low speeds.
    /// </summary>
    [Tooltip("Minimum speed for steering assist activation. Below this speed, steering assist is reduced.")]
    [Range(5f, 30f)] public float steerAssistMinSpeed = 10f;

    /// <summary>
    /// Maximum speed for full steering assist (km/h). Assist scales between min and max speed.
    /// </summary>
    [Tooltip("Maximum speed for full steering assist effect. Assist strength scales between min and max speed values.")]
    [Range(30f, 120f)] public float steerAssistMaxSpeed = 60f;

    /// <summary>
    /// High speed threshold (km/h) for safety adjustments. Above this speed, stability is prioritized.
    /// </summary>
    [Tooltip("High speed threshold for safety adjustments. Above this speed, stability helpers are reduced when wheels lose contact to prevent flips.")]
    [Range(40f, 100f)] public float highSpeedThreshold = 60f;

    /// <summary>
    /// Speed range for high-speed safety scaling (km/h). Used to calculate safety reduction at very high speeds.
    /// </summary>
    [Tooltip("Speed range over which high-speed safety scaling is applied. Larger values create a more gradual safety reduction.")]
    [Range(50f, 150f)] public float highSpeedSafetyRange = 80f;

    [Header("Smoothing")]
    /// <summary>
    /// Smoothing speed for drift forces and torques. Higher values = faster response, lower values = smoother transitions.
    /// </summary>
    [Tooltip("Smoothing speed for drift mode forces. Higher values mean faster response, lower values mean smoother transitions.")]
    [Range(1f, 20f)] public float driftForceSmoothing = 8f;

    /// <summary>
    /// Smoothing speed for ESP brake torques. Higher values = faster ESP response, lower values = gentler corrections.
    /// </summary>
    [Tooltip("Smoothing speed for ESP brake corrections. Higher values mean faster ESP response, lower values mean gentler corrections.")]
    [Range(1f, 20f)] public float espBrakeSmoothing = 2f;

    /// <summary>
    /// Smoothing speed for steering helper forces. Higher values = more responsive, lower values = smoother handling.
    /// </summary>
    [Tooltip("Smoothing speed for steering helper forces. Higher values mean more responsive handling, lower values mean smoother steering.")]
    [Range(1f, 20f)] public float steerHelperForceSmoothing = 15f;

    /// <summary>
    /// Current grounded factor based on wheel contact time.
    /// </summary>
    [Tooltip("Grounded factor (0-1) based on wheel contact time. Used to scale stability forces.")]
    [Range(0f, 1f)] public float groundedFactor = 0f;

    /// <summary>
    /// Current stability factor based on handbrake input.
    /// 1.0 = full stability assistance, 0.0 = no stability assistance.
    /// Scales down as handbrake is applied.
    /// </summary>
    private float currentStabilityFactor = 1f;

    private float previousGroundedScaling = 1f;
    private int previousGroundedWheelCount = 4;

    // Drift state
    [HideInInspector, Range(0f, 1f), Tooltip("Current drift intensity (0-1). Computed from rear wheel slip magnitude.")]
    public float driftIntensity = 0f;
    private float smoothedDriftIntensity = 0f;

    // Drift force parameters (set via CheckBehaviorDelayed from BehaviorType)
    [HideInInspector, Tooltip("Multiplier for yaw torque applied during drift. Higher values make the car rotate faster when drifting.")]
    public float driftYawTorqueMultiplier = 0.7f;
    [HideInInspector, Tooltip("Forward push force during drift to maintain speed. Higher values reduce speed loss while sliding.")]
    public float driftForwardForceMultiplier = 1500f;
    [HideInInspector, Tooltip("Lateral push force during drift. Higher values push the car further sideways for wider drifts.")]
    public float driftSidewaysForceMultiplier = 1750f;
    [HideInInspector, Tooltip("Minimum speed (km/h) required for drift forces to activate. Below this speed, no drift assistance is applied.")]
    public float driftMinSpeed = 20f;
    [HideInInspector, Tooltip("Speed (km/h) at which drift forces reach full strength. Forces scale linearly between min speed and this value.")]
    public float driftFullForceSpeed = 80f;
    [HideInInspector, Range(0f, 1f), Tooltip("How much throttle input alone contributes to yaw rotation. Higher values allow initiating drift with throttle without steering.")]
    public float driftThrottleYawFactor = 0.55f;

    // Drift friction parameters
    [HideInInspector, Range(0.1f, 1f), Tooltip("Minimum rear tire sideways grip during full drift. Lower values allow more lateral sliding. 1.0 = no grip reduction.")]
    public float driftRearSidewaysStiffnessMin = 0.5f;
    [HideInInspector, Range(0.5f, 1f), Tooltip("Minimum rear tire forward grip during full drift. Lower values cause more speed loss. 1.0 = no grip reduction.")]
    public float driftRearForwardStiffnessMin = 0.9f;
    [HideInInspector, Range(0.5f, 1.2f), Tooltip("Minimum front tire sideways grip during drift. Higher values keep front-end responsive for steering control.")]
    public float driftFrontSidewaysStiffnessMin = 0.7f;
    [HideInInspector, Tooltip("How quickly tire grip reduces when entering a drift. Higher values make grip loss more immediate.")]
    public float driftFrictionResponseSpeed = 12f;
    [HideInInspector, Tooltip("How quickly tire grip recovers when exiting a drift. Higher values make grip recovery faster.")]
    public float driftFrictionRecoverySpeed = 10f;

    // Drift recovery parameters
    [HideInInspector, Tooltip("Maximum angular velocity (deg/s) allowed during drift. Prevents uncontrollable spins. 0 = no limit.")]
    public float driftMaxAngularVelocity = 65f;
    [HideInInspector, Tooltip("Multiplier for recovery force when counter-steering during drift. Higher values make recovery from drifts easier.")]
    public float driftCounterSteerRecoveryBoost = 2f;
    [HideInInspector, Tooltip("Constant forward force applied during drift to maintain momentum. Higher values prevent speed loss while drifting.")]
    public float driftMomentumMaintenanceForce = 800f;

    // Smoothed drift force/torque tracking
    private Vector3 smoothedDriftTorque = Vector3.zero;
    private Vector3 smoothedDriftForwardForce = Vector3.zero;
    private Vector3 smoothedDriftSidewaysForce = Vector3.zero;

    // Smoothed ESP brake torque tracking
    private float smoothedESPFrontLeftBrake = 0f;
    private float smoothedESPFrontRightBrake = 0f;
    private float smoothedESPRearLeftBrake = 0f;
    private float smoothedESPRearRightBrake = 0f;

    // ESP V2 state — persists across frames for hysteresis, PD derivative, mode-commit, and filtering.
    // _espPersistentlyEngaged is the private backing state used by the control logic. The public
    // ESPEngaged mirrors the raw runtime intervention state, while ESPIndicatorEngaged is the
    // dashboard-gated version with brake-threshold filtering and hold-time debounce.
    private bool _espPersistentlyEngaged = false;
    private float _espEngagementTimer = 0f;     // counts down the minimum-intervention window
    private float _espUIHoldTimer = 0f;         // debounces ESPIndicatorEngaged against brake-torque jitter
    private float _prevYawError = 0f;           // for PD derivative term
    private float _filteredYawRef = 0f;         // first-order lag on bicycle-model reference
    private float _sideslipAngle = 0f;          // smoothed sideslip β (radians)
    private float _prevSideslipAngle = 0f;
    private float _sideslipRate = 0f;           // dβ/dt (rad/s)
    private bool _prevIsOversteer = false;      // for mode-switch commit timer
    private float _modeCommitTimer = 0f;

    // Cached vehicle geometry for ESP bicycle model
    private float espWheelbase;
    private float espFrontTrackWidth;
    private float espRearTrackWidth;
    private float espFrontWheelRadius;
    private float espRearWheelRadius;
    private bool espGeometryCached;

    // Smoothed steer helper force tracking (per wheel)
    private Dictionary<RCCP_WheelCollider, Vector3> smoothedLateralForces = new Dictionary<RCCP_WheelCollider, Vector3>();
    private Dictionary<RCCP_WheelCollider, Vector3> smoothedSteerForces = new Dictionary<RCCP_WheelCollider, Vector3>();

    private float wheelForceFactor;
    private float smoothedTractionStiffness = 1f;

    public override void Start() {

        base.Start();

        AxleManager = CarController.AxleManager;
        frontAxle = CarController.FrontAxle;
        rearAxle = CarController.RearAxle;
        poweredAxles = CarController.PoweredAxles;
        steeringAxles = CarController.SteeredAxles;
        brakedAxles = CarController.BrakedAxles;

        CacheESPGeometry();

    }

    /// <summary>
    /// Caches vehicle geometry (wheelbase, track width, wheel radius) from axle positions
    /// for the ESP bicycle model reference yaw rate calculation.
    /// </summary>
    private void CacheESPGeometry() {

        espGeometryCached = false;

        if (frontAxle == null || rearAxle == null)
            return;

        // Wheelbase from front wheel collider (already computed by RCCP_WheelCollider)
        if (frontAxle.leftWheelCollider != null)
            espWheelbase = frontAxle.leftWheelCollider.wheelbase;
        else if (frontAxle.rightWheelCollider != null)
            espWheelbase = frontAxle.rightWheelCollider.wheelbase;

        if (espWheelbase < 0.1f)
            espWheelbase = 2.5f;

        // Front track width and wheel radius
        if (frontAxle.leftWheelCollider != null) {

            espFrontTrackWidth = frontAxle.leftWheelCollider.trackWidth;
            espFrontWheelRadius = frontAxle.leftWheelCollider.WheelCollider != null
                ? frontAxle.leftWheelCollider.WheelCollider.radius : 0.3f;

        }

        if (espFrontTrackWidth < 0.1f)
            espFrontTrackWidth = 1.5f;

        if (espFrontWheelRadius < 0.01f)
            espFrontWheelRadius = 0.3f;

        // Rear track width and wheel radius
        if (rearAxle.leftWheelCollider != null) {

            espRearTrackWidth = rearAxle.leftWheelCollider.trackWidth;
            espRearWheelRadius = rearAxle.leftWheelCollider.WheelCollider != null
                ? rearAxle.leftWheelCollider.WheelCollider.radius : 0.3f;

        }

        if (espRearTrackWidth < 0.1f)
            espRearTrackWidth = 1.5f;

        if (espRearWheelRadius < 0.01f)
            espRearWheelRadius = 0.3f;

        espGeometryCached = true;

    }

    /// <summary>
    /// Returns the vehicle-resolved front steer angle for ESP.
    /// This must follow the actual axle steer state after counter-steer, steering limiter,
    /// speed curve, and any other vehicle-side overrides have modified the player's request.
    /// Using player-side steer intent here can overestimate the target yaw rate and make ESP
    /// classify normal correction as understeer / oversteer too early.
    /// </summary>
    private float GetESPReferenceSteerAngle() {

        if (frontAxle == null)
            return CarController.steerAngle;

        return frontAxle.steerAngle;

    }

    private void FixedUpdate() {

        if (!CarController)
            return;

        if (CarController.IsGrounded)
            groundedFactor += Time.deltaTime * .5f;
        else
            groundedFactor = 0f;

        if (CarController.handbrakeInput_V > .5f)
            groundedFactor = .35f;

        groundedFactor = Mathf.Clamp01(groundedFactor);

        // Calculate stability factor based on handbrake input
        // 1.0 = no handbrake (full stability), 0.0 = full handbrake (no stability)
        // This allows gradual reduction of stability systems as handbrake is applied
        currentStabilityFactor = Mathf.Clamp01(1f - CarController.handbrakeInput_V);

        if (ESP)
            UpdateESP();

        if (TCS)
            UpdateTCS();

        if (ABS)
            UpdateABS();

        if (steeringHelper)
            SteerHelper();

        if (tractionHelper)
            TractionHelper();

        if (angularDragHelper)
            AngularDragHelper();

        if (driftAngleLimiter)
            LimitDriftAngle();

        RCCP_Settings.BehaviorType currentBehaviorType = CarController != null ? CarController.GetVehicleBehaviorType() : RCCPSettings.SelectedBehaviorType;

        if (currentBehaviorType != null && currentBehaviorType.driftMode)
            Drift();
        else
            ResetDriftFriction();

    }

    /// <summary>
    /// Two-layer cooperative drift system. Computes drift intensity from rear wheel slip,
    /// applies force-based drift (yaw torque, forward/sideways forces) with speed scaling
    /// and spinout prevention, then communicates friction multipliers to wheel colliders.
    /// </summary>
    private void Drift() {

        // Early exit if not properly grounded
        if (groundedFactor < 0.1f)
            return;

        float rearWheelSlipAmountForward = 0f;
        float rearWheelSlipAmountSideways = 0f;

        // Check if rear wheels are actually grounded before calculating slip
        bool rearLeftGrounded = false;
        bool rearRightGrounded = false;

        if (rearAxle != null) {

            WheelHit hit;

            if (rearAxle.leftWheelCollider != null && rearAxle.leftWheelCollider.WheelCollider != null)
                rearLeftGrounded = rearAxle.leftWheelCollider.WheelCollider.GetGroundHit(out hit) && hit.force > 10f;

            if (rearAxle.rightWheelCollider != null && rearAxle.rightWheelCollider.WheelCollider != null)
                rearRightGrounded = rearAxle.rightWheelCollider.WheelCollider.GetGroundHit(out hit) && hit.force > 10f;

        }

        // Only calculate slip if at least one rear wheel is properly grounded
        if (!rearLeftGrounded && !rearRightGrounded) {

            ResetDriftFriction();
            return;

        }

        // 1. Get average slip on rear wheels (forward & sideways).
        if (rearAxle != null) {

            float leftForwardSlip = rearLeftGrounded ? rearAxle.leftWheelCollider.ForwardSlip : 0f;
            float rightForwardSlip = rearRightGrounded ? rearAxle.rightWheelCollider.ForwardSlip : 0f;
            float leftSidewaysSlip = rearLeftGrounded ? rearAxle.leftWheelCollider.SidewaysSlip : 0f;
            float rightSidewaysSlip = rearRightGrounded ? rearAxle.rightWheelCollider.SidewaysSlip : 0f;

            rearWheelSlipAmountForward = (leftForwardSlip + rightForwardSlip) * 0.5f;
            rearWheelSlipAmountSideways = (leftSidewaysSlip + rightSidewaysSlip) * 0.5f;

        }

        // 2. Compute drift intensity using sqrt scaling — makes small slips significant for easier initiation
        float sidewaysSlipAbs = Mathf.Abs(rearWheelSlipAmountSideways);
        float rawDriftIntensity = Mathf.Clamp01(Mathf.Sqrt(sidewaysSlipAbs) * 1.5f);
        smoothedDriftIntensity = Mathf.Lerp(smoothedDriftIntensity, rawDriftIntensity, Time.fixedDeltaTime * driftForceSmoothing);
        driftIntensity = smoothedDriftIntensity;

        // 3. Speed-dependent scaling — no drift forces at low speeds
        float speed = CarController.absoluteSpeed;
        float speedScale = Mathf.Clamp01((speed - driftMinSpeed) / Mathf.Max(driftFullForceSpeed - driftMinSpeed, 1f));

        // 4. Use linear slip with sign preservation instead of squared
        float linearForwardSlip = rearWheelSlipAmountForward;
        float linearSidewaysSlip = rearWheelSlipAmountSideways;

        // 5. Determine force application point (COM if available)
        Transform comTransform = transform;
        RCCP_AeroDynamics aeroDynamics = CarController.AeroDynamics;

        if (aeroDynamics != null && aeroDynamics.COM != null)
            comTransform = aeroDynamics.COM;

        // 6. Normalized steering input
        float normalizedSteerInput = Mathf.Clamp(CarController.steerInput_P, -1f, 1f);

        // 7. Counter-steer detection: player steers against drift direction
        float driftDirection = Mathf.Sign(rearWheelSlipAmountSideways);
        bool isCounterSteering = (normalizedSteerInput != 0f) && (Mathf.Sign(normalizedSteerInput) != driftDirection) && (sidewaysSlipAbs > 0.1f);
        float counterSteerFactor = isCounterSteering ? driftCounterSteerRecoveryBoost : 1f;

        // 8. Yaw torque — steering always contributes; throttle adds extra yaw via driftThrottleYawFactor
        float steeringYaw = normalizedSteerInput * CarController.direction;
        float throttleYaw = driftThrottleYawFactor * driftIntensity * Mathf.Abs(CarController.throttleInput_P) * CarController.direction * driftDirection;

        // Counter-steer boosts recovery yaw
        float yawInput = steeringYaw * counterSteerFactor + throttleYaw;

        Vector3 targetDriftTorque = Vector3.up
            * yawInput
            * driftYawTorqueMultiplier
            * speedScale
            * groundedFactor;

        smoothedDriftTorque = Vector3.Lerp(
            smoothedDriftTorque,
            targetDriftTorque,
            Time.fixedDeltaTime * driftForceSmoothing
        );

        CarController.Rigid.AddRelativeTorque(smoothedDriftTorque, ForceMode.Acceleration);

        // 9. Angular velocity clamping — prevents spinouts
        if (driftMaxAngularVelocity > 0f) {

            Vector3 angVel = CarController.Rigid.angularVelocity;
            float maxAngVelRad = driftMaxAngularVelocity * Mathf.Deg2Rad;

            if (Mathf.Abs(angVel.y) > maxAngVelRad) {

                angVel.y = Mathf.Lerp(angVel.y, Mathf.Sign(angVel.y) * maxAngVelRad, Time.fixedDeltaTime * 5f);
                CarController.Rigid.angularVelocity = angVel;

            }

        }

        // Grounded ratio for force scaling
        float groundedRatio = 0f;
        if (rearLeftGrounded && rearRightGrounded)
            groundedRatio = 1f;
        else if (rearLeftGrounded || rearRightGrounded)
            groundedRatio = 0.5f;

        // 10. Forward force — two parts: reactive (slip-based) + proactive (momentum maintenance)
        float reactiveForward = driftForwardForceMultiplier
            * Mathf.Abs(linearSidewaysSlip)
            * Mathf.Clamp01(Mathf.Abs(linearForwardSlip) * 4f);

        float proactiveForward = driftMomentumMaintenanceForce
            * driftIntensity
            * Mathf.Abs(CarController.throttleInput_P);

        Vector3 targetForwardForce = transform.forward
            * (reactiveForward + proactiveForward)
            * speedScale
            * CarController.direction
            * groundedFactor
            * groundedRatio;

        smoothedDriftForwardForce = Vector3.Lerp(
            smoothedDriftForwardForce,
            targetForwardForce,
            Time.fixedDeltaTime * driftForceSmoothing
        );

        CarController.Rigid.AddForceAtPosition(
            smoothedDriftForwardForce,
            comTransform.position,
            ForceMode.Force
        );

        // 11. Sideways force — reduced during counter-steer for easier recovery
        float sidewaysReduction = isCounterSteering ? 0.4f : 1f;

        Vector3 targetSidewaysForce = transform.right
            * driftSidewaysForceMultiplier
            * linearSidewaysSlip
            * Mathf.Clamp01(Mathf.Abs(linearForwardSlip) * 4f)
            * sidewaysReduction
            * speedScale
            * CarController.direction
            * groundedFactor
            * groundedRatio;

        smoothedDriftSidewaysForce = Vector3.Lerp(
            smoothedDriftSidewaysForce,
            targetSidewaysForce,
            Time.fixedDeltaTime * driftForceSmoothing
        );

        CarController.Rigid.AddForceAtPosition(
            smoothedDriftSidewaysForce,
            comTransform.position,
            ForceMode.Force
        );

        // 12. Bridge to friction layer
        ApplyDriftFriction();

    }

    /// <summary>
    /// Communicates drift friction multipliers to each wheel collider based on drift intensity.
    /// Rear wheels get full intensity effect, front wheels get 50% reduced effect.
    /// </summary>
    private void ApplyDriftFriction() {

        if (CarController.AxleManager == null || CarController.AxleManager.Axles == null)
            return;

        float lerpSpeed = driftIntensity > 0.1f ? driftFrictionResponseSpeed : driftFrictionRecoverySpeed;

        for (int i = 0; i < CarController.AxleManager.Axles.Count; i++) {

            RCCP_Axle axle = CarController.AxleManager.Axles[i];

            if (axle == null)
                continue;

            // Determine if this is a rear axle (z < 0) or front axle
            bool isRearAxle = false;

            if (axle.leftWheelCollider != null)
                isRearAxle = axle.leftWheelCollider.transform.localPosition.z < 0f;
            else if (axle.rightWheelCollider != null)
                isRearAxle = axle.rightWheelCollider.transform.localPosition.z < 0f;

            // Front wheels get 50% reduced drift intensity effect
            float effectiveIntensity = isRearAxle ? driftIntensity : driftIntensity * 0.5f;

            float targetForwardMul;
            float targetSidewaysMul;

            if (isRearAxle) {

                targetForwardMul = Mathf.Lerp(1f, driftRearForwardStiffnessMin, effectiveIntensity);
                targetSidewaysMul = Mathf.Lerp(1f, driftRearSidewaysStiffnessMin, effectiveIntensity);

            } else {

                targetForwardMul = 1f; // Front forward grip stays at 100%
                targetSidewaysMul = Mathf.Lerp(1f, driftFrontSidewaysStiffnessMin, effectiveIntensity);

            }

            if (axle.leftWheelCollider != null) {

                axle.leftWheelCollider.driftForwardStiffnessMultiplier = Mathf.Lerp(
                    axle.leftWheelCollider.driftForwardStiffnessMultiplier,
                    targetForwardMul,
                    Time.fixedDeltaTime * lerpSpeed
                );

                axle.leftWheelCollider.driftSidewaysStiffnessMultiplier = Mathf.Lerp(
                    axle.leftWheelCollider.driftSidewaysStiffnessMultiplier,
                    targetSidewaysMul,
                    Time.fixedDeltaTime * lerpSpeed
                );

            }

            if (axle.rightWheelCollider != null) {

                axle.rightWheelCollider.driftForwardStiffnessMultiplier = Mathf.Lerp(
                    axle.rightWheelCollider.driftForwardStiffnessMultiplier,
                    targetForwardMul,
                    Time.fixedDeltaTime * lerpSpeed
                );

                axle.rightWheelCollider.driftSidewaysStiffnessMultiplier = Mathf.Lerp(
                    axle.rightWheelCollider.driftSidewaysStiffnessMultiplier,
                    targetSidewaysMul,
                    Time.fixedDeltaTime * lerpSpeed
                );

            }

        }

    }

    /// <summary>
    /// Resets all drift friction multipliers to 1.0 when drift mode is off.
    /// </summary>
    private void ResetDriftFriction() {

        driftIntensity = 0f;
        smoothedDriftIntensity = 0f;

        if (CarController.AxleManager == null || CarController.AxleManager.Axles == null)
            return;

        for (int i = 0; i < CarController.AxleManager.Axles.Count; i++) {

            RCCP_Axle axle = CarController.AxleManager.Axles[i];

            if (axle == null)
                continue;

            if (axle.leftWheelCollider != null) {

                axle.leftWheelCollider.driftForwardStiffnessMultiplier = 1f;
                axle.leftWheelCollider.driftSidewaysStiffnessMultiplier = 1f;

            }

            if (axle.rightWheelCollider != null) {

                axle.rightWheelCollider.driftForwardStiffnessMultiplier = 1f;
                axle.rightWheelCollider.driftSidewaysStiffnessMultiplier = 1f;

            }

        }

    }

    /// <summary>
    /// Manages the ABS logic, reducing brake torque if a braked wheel is slipping above engageABSThreshold.
    /// </summary>
    private void UpdateABS() {

        ABSEngaged = false;

        if (AxleManager == null)
            return;

        if (brakedAxles == null || brakedAxles.Count < 1)
            return;

        // Scale ABS intensity by stability factor (reduced when handbrake is applied)
        float scaledABSIntensity = ABSIntensity * currentStabilityFactor;

        for (int i = 0; i < brakedAxles.Count; i++) {

            if (brakedAxles[i] == null)
                continue;

            // Skip ABS on airborne wheels — they can "slip" freely without ground contact,
            // triggering false engagement that wastes cycles and pollutes ABSEngaged telemetry.
            if (brakedAxles[i].leftWheelCollider != null && brakedAxles[i].leftWheelCollider.isGrounded) {

                if ((Mathf.Abs(brakedAxles[i].leftWheelCollider.ForwardSlip) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                    brakedAxles[i].leftWheelCollider.CutBrakeABS(scaledABSIntensity);
                    ABSEngaged = true;

                }

            }

            if (brakedAxles[i].rightWheelCollider != null && brakedAxles[i].rightWheelCollider.isGrounded) {

                if ((Mathf.Abs(brakedAxles[i].rightWheelCollider.ForwardSlip) * brakedAxles[i].brakeInput) >= engageABSThreshold) {

                    brakedAxles[i].rightWheelCollider.CutBrakeABS(scaledABSIntensity);
                    ABSEngaged = true;

                }

            }

        }

    }

    /// <summary>
    /// ESP V2 — yaw-rate-based electronic stability program with bicycle-model reference, hysteresis,
    /// sideslip-angle β estimation, PD control, mode-switch commit timer, first-order reference lag,
    /// and Normal/Sport modes.
    ///
    /// Reference: ψ̇_ref = V · δ / (L + K_us · V²), clamped by friction limit μ · g / V, then passed
    /// through a first-order lag of time constant yawRateTimeConstant.
    ///
    /// Classification:
    /// - Oversteer (actual yaw > reference, or large β, or yaw opposes commanded wheel direction): brake OUTER FRONT wheel
    /// - Understeer (actual yaw less than reference): brake INNER REAR wheel
    ///
    /// Engine torque reduction is applied asymmetrically: aggressive for understeer (frees front
    /// friction circle), mild for oversteer, minimal for FWD oversteer (where front drive stabilizes).
    /// Sport mode disables engine torque reduction and widens dead bands ~2x.
    /// </summary>
    private void UpdateESP() {

        ESPEngaged = false;
        ESPIndicatorEngaged = false;

        if (frontAxle == null || rearAxle == null) {
            ResetESPTelemetry();
            return;
        }

        if (frontAxle.leftWheelCollider == null || frontAxle.rightWheelCollider == null) {
            ResetESPTelemetry();
            return;
        }

        if (rearAxle.leftWheelCollider == null || rearAxle.rightWheelCollider == null) {
            ResetESPTelemetry();
            return;
        }

        if (!espGeometryCached)
            CacheESPGeometry();

        // ---- 1. Sensor readings ----
        float speed = CarController.absoluteSpeed / 3.6f;  // m/s
        float referenceSteerAngle = GetESPReferenceSteerAngle();
        float steerRad = referenceSteerAngle * Mathf.Deg2Rad;
        float yawActual = CarController.Rigid.angularVelocity.y;  // rad/s (+ = clockwise/right in Unity LH)

        // ---- 2. Sideslip β estimation (GAP 2) ----
        // Kinematic β = atan2(v_x_local, |v_z_local|). Only meaningful when we have forward motion.
        // A smoothed running value rejects per-frame jitter; derivative then gives dβ/dt.
        Vector3 localVel = transform.InverseTransformDirection(CarController.Rigid.linearVelocity);
        float newSideslip = (Mathf.Abs(localVel.z) > 0.5f)
            ? Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z))
            : 0f;
        _sideslipAngle = Mathf.Lerp(_sideslipAngle, newSideslip, Time.fixedDeltaTime * 12f);
        _sideslipRate = (_sideslipAngle - _prevSideslipAngle) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        _prevSideslipAngle = _sideslipAngle;

        float sideslipDeg = _sideslipAngle * Mathf.Rad2Deg;
        float sideslipRateDeg = _sideslipRate * Mathf.Rad2Deg;
        debugSideslipAngleDeg = sideslipDeg;
        debugSideslipRateDegS = sideslipRateDeg;
        debugYawActualDegS = yawActual * Mathf.Rad2Deg;

        // ---- 3. Speed gate ----
        // ESP deactivates below ~15 km/h (doc recommends 5-20 km/h range). Parking maneuvers at
        // low speed can generate yaw rates that would look like massive errors.
        if (speed < 4f) {
            FadeESPBrakes();
            _espPersistentlyEngaged = false;
            _espEngagementTimer = 0f;
            _espUIHoldTimer = 0f;
            _modeCommitTimer = 0f;
            _prevYawError = 0f;
            _filteredYawRef = Mathf.Lerp(_filteredYawRef, 0f, Time.fixedDeltaTime * 5f);
            debugYawRefDegS = 0f;
            debugYawErrorDegS = 0f;
            debugActiveWheelIndex = -1;
            debugIsOversteer = false;
            return;
        }

        // ---- 4. Bicycle model reference yaw rate (GAPs 1, 5) ----
        // ψ̇_ref = V · δ / (L + K_us · V²)  then clamped by friction limit μ · g / V
        float yawRefSS = speed * steerRad / (espWheelbase + understeerGradient * speed * speed);
        float yawMax = (estimatedMu * 9.81f) / speed;
        yawRefSS = Mathf.Clamp(yawRefSS, -yawMax, yawMax);

        // ---- 5. First-order lag filter on reference (GAP 8) ----
        // Discrete approximation: y[n+1] = y[n] + dt/(τ+dt) · (u - y[n])
        float lagAlpha = Time.fixedDeltaTime / Mathf.Max(yawRateTimeConstant + Time.fixedDeltaTime, 0.0001f);
        _filteredYawRef = Mathf.Lerp(_filteredYawRef, yawRefSS, lagAlpha);
        float yawRef = _filteredYawRef;

        // ---- 6. Yaw error ----
        float yawError = yawActual - yawRef;
        float yawErrorDeg = Mathf.Abs(yawError) * Mathf.Rad2Deg;

        debugYawRefDegS = yawRef * Mathf.Rad2Deg;
        debugYawErrorDegS = yawError * Mathf.Rad2Deg;

        // ---- 7. Sport-mode threshold widening (GAP 9) + hysteresis + min intervention (GAP 3) ----
        float modeThresholdScale = (espMode == ESPMode.Sport) ? 2f : 2f;
        float activationThresholdDeg = espDeadband * modeThresholdScale;
        float deactivationThresholdDeg = Mathf.Min(espDeactivationDeadband * modeThresholdScale, activationThresholdDeg);

        // Sideslip-based spin risk forces ESP engagement regardless of yaw-error threshold.
        // This catches developing spins earlier than yaw rate error alone (GAP 2).
        bool sideslipSpinRisk = (Mathf.Abs(sideslipDeg) > sideslipMaxAngle)
                             || (Mathf.Abs(sideslipRateDeg) > sideslipMaxRate);

        // Threshold depends on whether we're already engaged (hysteresis band).
        float currentThresholdDeg = _espPersistentlyEngaged ? deactivationThresholdDeg : activationThresholdDeg;
        bool aboveThreshold = yawErrorDeg >= currentThresholdDeg;

        if (!aboveThreshold && !sideslipSpinRisk) {
            // Below threshold — honor min intervention time before disengaging.
            if (_espPersistentlyEngaged && _espEngagementTimer > 0f) {
                _espEngagementTimer -= Time.fixedDeltaTime;
                // Fall through: effectiveError clamps to 0 below, so brakes naturally taper via smoothing.
            } else {
                FadeESPBrakes();
                _espPersistentlyEngaged = false;
                _prevYawError = 0f;
                _modeCommitTimer = 0f;
                debugActiveWheelIndex = -1;
                debugIsOversteer = false;
                return;
            }
        } else {
            if (!_espPersistentlyEngaged)
                _espEngagementTimer = espMinInterventionTime;
            _espPersistentlyEngaged = true;
        }

        // ESPEngaged exposes the real intervention state immediately. The dashboard-facing
        // ESPIndicatorEngaged flag is written later in this method, after the UI gate/debounce.
        ESPEngaged = _espPersistentlyEngaged;

        // Scale by user intensity and handbrake factor.
        float scaledIntensity = ESPIntensity * currentStabilityFactor;

        // ---- 8. Classify oversteer vs understeer (with sideslip promotion — GAP 2) ----
        bool yawOpposesVehicleCommand =
            Mathf.Abs(referenceSteerAngle) > 0.5f &&
            Mathf.Abs(yawActual) > 0.05f &&
            (yawActual * referenceSteerAngle) < 0f;

        bool isOversteer;
        if (sideslipSpinRisk)
            isOversteer = true;                                 // β says spin is developing
        else if (Mathf.Abs(yawRef) < 0.02f)
            isOversteer = true;                                 // spinning while going straight
        else if (yawOpposesVehicleCommand)
            isOversteer = true;                                 // body yawing opposite to the commanded wheel direction
        else
            isOversteer = (yawError * yawRef) > 0f;

        // ---- 9. Mode-switch commit timer (GAP 7) ----
        // Once classified as oversteer or understeer, hold that classification for at least
        // espModeCommitTime seconds before allowing a flip. This prevents wrong-wheel braking
        // during rapid transients (e.g., mid-transition between understeer and oversteer).
        // Sideslip spin risk always wins — real emergencies bypass the commit timer.
        if (!sideslipSpinRisk && _prevIsOversteer != isOversteer && _modeCommitTimer > 0f) {
            isOversteer = _prevIsOversteer;                     // hold previous classification
        } else if (_prevIsOversteer != isOversteer) {
            _modeCommitTimer = espModeCommitTime;               // start commit timer on change
        } else {
            _modeCommitTimer = Mathf.Max(0f, _modeCommitTimer - Time.fixedDeltaTime);
        }
        _prevIsOversteer = isOversteer;
        debugIsOversteer = isOversteer;

        // ---- 10. Engine torque reduction (GAPs 6, 9) ----
        // Production ESP reduces engine torque asymmetrically:
        // - Understeer: aggressive cut (frees front friction circle for lateral grip)
        // - Oversteer: softer cut; for FWD, very soft because front drive stabilizes the car
        // - Sport mode: engine cut disabled entirely
        bool allowEngineCut = (espMode != ESPMode.Sport);
        float yawErrorSeverity = Mathf.Clamp01((yawErrorDeg - currentThresholdDeg) / Mathf.Max(currentThresholdDeg, 1f));
        float motorCutIntensity = scaledIntensity * yawErrorSeverity * 4f;

        if (isOversteer) {
            motorCutIntensity *= 0.5f;                          // softer cut for oversteer

            bool isFWD = frontAxle != null && frontAxle.isPower
                      && rearAxle != null && !rearAxle.isPower;
            if (isFWD)
                motorCutIntensity *= 0.4f;                      // FWD front drive stabilizes — barely cut
        }

        if (!allowEngineCut)
            motorCutIntensity = 0f;

        if (motorCutIntensity > 0.001f && poweredAxles != null) {

            for (int p = 0; p < poweredAxles.Count; p++) {

                RCCP_Axle poweredAxle = poweredAxles[p];

                if (poweredAxle == null)
                    continue;

                if (poweredAxle.leftWheelCollider != null)
                    poweredAxle.leftWheelCollider.CutTractionESP(motorCutIntensity);

                if (poweredAxle.rightWheelCollider != null)
                    poweredAxle.rightWheelCollider.CutTractionESP(motorCutIntensity);

            }

        }

        // ---- 11. Corrective yaw moment via PD controller (GAP 4) ----
        // M_z = (K_P · e + K_D · de/dt) · scaledIntensity   (magnitudes; sign handled by wheel selection)
        // Dead band is subtracted so correction starts from zero at the threshold edge.
        float mass = CarController.Rigid.mass;
        float yawErrorDeriv = (yawError - _prevYawError) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        _prevYawError = yawError;

        float deadbandRad = currentThresholdDeg * Mathf.Deg2Rad;
        float effectiveError = Mathf.Max(Mathf.Abs(yawError) - deadbandRad, 0f);
        float pComponent = espPGain * mass * effectiveError;
        float dComponent = espDGain * mass * Mathf.Abs(yawErrorDeriv);
        float correctiveMoment = (pComponent + dComponent) * scaledIntensity;

        // ---- 12. Turn direction for inner/outer wheel selection ----
        bool turningRight;
        if (Mathf.Abs(referenceSteerAngle) > 0.5f)
            turningRight = referenceSteerAngle > 0f;
        else if (Mathf.Abs(CarController.steerAngle) > 0.5f)
            turningRight = CarController.steerAngle > 0f;
        else
            turningRight = yawActual > 0f;

        // Spin recovery: when the body is yawing opposite to the driver's intent OR we have a
        // sideslip spin risk, base wheel selection on the ACTUAL spin direction so the brake
        // moment opposes the spin rather than reinforcing it.
        if (yawOpposesVehicleCommand || sideslipSpinRisk)
            turningRight = yawActual > 0f;

        // ---- 13. Apply brake to selected wheel ----
        // Skip brake if the target wheel is airborne — it cannot generate a yaw moment, and
        // commanding torque risks a lockup shock on touchdown. The Lerp smoothing naturally
        // fades the command toward 0 until the wheel lands again.
        if (isOversteer && frontAxle.isBrake) {

            // Oversteer: brake OUTSIDE FRONT wheel
            float brakeTorque = correctiveMoment * 2f * espFrontWheelRadius / espFrontTrackWidth;
            brakeTorque = Mathf.Min(brakeTorque, frontAxle.maxBrakeTorque);

            float targetFL, targetFR;

            if (turningRight) {
                // Turning right → outer = left front
                bool grounded = frontAxle.leftWheelCollider.isGrounded;
                targetFL = grounded ? brakeTorque : 0f;
                targetFR = 0f;
                debugActiveWheelIndex = grounded ? 0 : -1;
            } else {
                // Turning left → outer = right front
                bool grounded = frontAxle.rightWheelCollider.isGrounded;
                targetFL = 0f;
                targetFR = grounded ? brakeTorque : 0f;
                debugActiveWheelIndex = grounded ? 1 : -1;
            }

            smoothedESPFrontLeftBrake = Mathf.Lerp(smoothedESPFrontLeftBrake, targetFL, Time.fixedDeltaTime * espBrakeSmoothing);
            smoothedESPFrontRightBrake = Mathf.Lerp(smoothedESPFrontRightBrake, targetFR, Time.fixedDeltaTime * espBrakeSmoothing);

            frontAxle.leftWheelCollider.AddBrakeTorque(smoothedESPFrontLeftBrake);
            frontAxle.rightWheelCollider.AddBrakeTorque(smoothedESPFrontRightBrake);

            // If the front axle is also powered (FWD/AWD), zero the accumulated motor torque on
            // the specifically-braked wheel. Direct assignment beats CutTractionESP here because
            // the latter's sideways-slip scaling can leave residual motor torque. Relies on
            // execution order: Axle(-2) → Stability(-1) → WheelCollider(0).
            if (frontAxle.isPower) {

                if (smoothedESPFrontLeftBrake > 1f)
                    frontAxle.leftWheelCollider.motorTorque = 0f;

                if (smoothedESPFrontRightBrake > 1f)
                    frontAxle.rightWheelCollider.motorTorque = 0f;

            }

            // Fade rear (not used for oversteer).
            smoothedESPRearLeftBrake = Mathf.Lerp(smoothedESPRearLeftBrake, 0f, Time.fixedDeltaTime * espBrakeSmoothing * 2f);
            smoothedESPRearRightBrake = Mathf.Lerp(smoothedESPRearRightBrake, 0f, Time.fixedDeltaTime * espBrakeSmoothing * 2f);

        } else if (!isOversteer && rearAxle.isBrake) {

            // Understeer: brake INSIDE REAR wheel. Same grounding rule as the oversteer branch above.
            float brakeTorque = correctiveMoment * 2f * espRearWheelRadius / espRearTrackWidth;
            brakeTorque = Mathf.Min(brakeTorque, rearAxle.maxBrakeTorque);

            float targetRL, targetRR;

            if (turningRight) {
                // Turning right → inner = right rear
                bool grounded = rearAxle.rightWheelCollider.isGrounded;
                targetRL = 0f;
                targetRR = grounded ? brakeTorque : 0f;
                debugActiveWheelIndex = grounded ? 3 : -1;
            } else {
                // Turning left → inner = left rear
                bool grounded = rearAxle.leftWheelCollider.isGrounded;
                targetRL = grounded ? brakeTorque : 0f;
                targetRR = 0f;
                debugActiveWheelIndex = grounded ? 2 : -1;
            }

            smoothedESPRearLeftBrake = Mathf.Lerp(smoothedESPRearLeftBrake, targetRL, Time.fixedDeltaTime * espBrakeSmoothing);
            smoothedESPRearRightBrake = Mathf.Lerp(smoothedESPRearRightBrake, targetRR, Time.fixedDeltaTime * espBrakeSmoothing);

            rearAxle.leftWheelCollider.AddBrakeTorque(smoothedESPRearLeftBrake);
            rearAxle.rightWheelCollider.AddBrakeTorque(smoothedESPRearRightBrake);

            // If the rear axle is also powered (RWD/AWD), zero the accumulated motor torque on
            // the specifically-braked wheel. Same reasoning as FWD/oversteer case above.
            if (rearAxle.isPower) {

                if (smoothedESPRearLeftBrake > 1f)
                    rearAxle.leftWheelCollider.motorTorque = 0f;

                if (smoothedESPRearRightBrake > 1f)
                    rearAxle.rightWheelCollider.motorTorque = 0f;

            }

            // Fade front (not used for understeer).
            smoothedESPFrontLeftBrake = Mathf.Lerp(smoothedESPFrontLeftBrake, 0f, Time.fixedDeltaTime * espBrakeSmoothing * 2f);
            smoothedESPFrontRightBrake = Mathf.Lerp(smoothedESPFrontRightBrake, 0f, Time.fixedDeltaTime * espBrakeSmoothing * 2f);

        } else {

            FadeESPBrakes();
            debugActiveWheelIndex = -1;

        }

        // ---- 14. ESPIndicatorEngaged UI gate (threshold + asymmetric debounce) ----
        // Internal state (_espPersistentlyEngaged) is used for hysteresis & min-intervention logic above.
        // The dashboard-facing ESPIndicatorEngaged flag is gated on actual brake torque magnitude AND debounced with a UI
        // hold timer so brake-torque Lerp oscillation near the threshold cannot flicker the dashboard.
        // - Fast turn-on: one frame after threshold crossed upward.
        // - Slow turn-off: UI stays lit for espUIMinHoldTime seconds after brake last crossed threshold.
        // - Snap-off on full disengagement: if _espPersistentlyEngaged drops, UI drops in the same frame.
        // espMinNoticeableBrakeTorque = 0 recovers pre-gate behavior; espUIMinHoldTime = 0 disables debounce.
        float maxESPBrake = Mathf.Max(
            Mathf.Max(smoothedESPFrontLeftBrake, smoothedESPFrontRightBrake),
            Mathf.Max(smoothedESPRearLeftBrake, smoothedESPRearRightBrake));

        bool thresholdMet = _espPersistentlyEngaged && (maxESPBrake >= espMinNoticeableBrakeTorque);

        if (thresholdMet)
            _espUIHoldTimer = espUIMinHoldTime;
        else if (_espPersistentlyEngaged)
            _espUIHoldTimer = Mathf.Max(0f, _espUIHoldTimer - Time.fixedDeltaTime);
        else
            _espUIHoldTimer = 0f;

        ESPIndicatorEngaged = thresholdMet || (_espPersistentlyEngaged && _espUIHoldTimer > 0f);

        // ---- 15. Speed-preservation arcade option ----
        // Offsets the longitudinal deceleration ESP brake intervention produces by applying
        // a scaled forward force at the rigidbody. preserveSpeedFactor=0 is off (realistic),
        // 1 is full cancellation (arcade), intermediate values give partial preservation.
        // Approximation: longitudinal force at each wheel ≈ brakeTorque / wheelRadius when gripped.
        // Over-estimates during wheel lockup (tire delivers less than brakeTorque/R when slipping),
        // which can cause mild forward push at high factors on low-μ — acceptable for arcade mode.
        if (preserveSpeedFactor > 0.001f && maxESPBrake > 0f) {

            float totalESPBrakeForce =
                (smoothedESPFrontLeftBrake + smoothedESPFrontRightBrake) / Mathf.Max(espFrontWheelRadius, 0.01f) +
                (smoothedESPRearLeftBrake + smoothedESPRearRightBrake) / Mathf.Max(espRearWheelRadius, 0.01f);

            // Use the vehicle's travel direction sign so we compensate along actual motion axis.
            // direction == 0 happens at low speed; bail rather than pick a default that could nudge.
            int forwardSign = CarController.direction;

            if (forwardSign != 0 && totalESPBrakeForce > 0f) {

                CarController.Rigid.AddForce(
                    transform.forward * totalESPBrakeForce * forwardSign * preserveSpeedFactor,
                    ForceMode.Force
                );

            }

        }

    }

    /// <summary>
    /// Resets ESP V2 telemetry debug fields to zero. Called on early returns when ESP cannot compute values.
    /// </summary>
    private void ResetESPTelemetry() {

        debugYawActualDegS = 0f;
        debugYawRefDegS = 0f;
        debugYawErrorDegS = 0f;
        debugSideslipAngleDeg = 0f;
        debugSideslipRateDegS = 0f;
        debugActiveWheelIndex = -1;
        debugIsOversteer = false;

    }

    /// <summary>
    /// Smoothly fades all ESP brake torque smoothing values to zero.
    /// Called when ESP is not intervening.
    /// </summary>
    private void FadeESPBrakes() {

        float fadeRate = Time.fixedDeltaTime * espBrakeSmoothing * 2f;
        smoothedESPFrontLeftBrake = Mathf.Lerp(smoothedESPFrontLeftBrake, 0f, fadeRate);
        smoothedESPFrontRightBrake = Mathf.Lerp(smoothedESPFrontRightBrake, 0f, fadeRate);
        smoothedESPRearLeftBrake = Mathf.Lerp(smoothedESPRearLeftBrake, 0f, fadeRate);
        smoothedESPRearRightBrake = Mathf.Lerp(smoothedESPRearRightBrake, 0f, fadeRate);

    }

    /// <summary>
    /// Manages TCS logic, reducing motor torque if the powered wheels
    /// are slipping beyond engageTCSThreshold (in forward or reverse).
    /// </summary>
    private void UpdateTCS() {

        TCSEngaged = false;

        if (poweredAxles == null || poweredAxles.Count < 1)
            return;

        // If the vehicle isn't moving forward or backward (direction == 0),
        // we can skip TCS. Or handle differently if desired.
        if (CarController.direction == 0)
            return;

        // Scale TCS intensity by stability factor (reduced when handbrake is applied)
        float scaledTCSIntensity = TCSIntensity * currentStabilityFactor;

        // For each powered axle, check forward slip. If it exceeds threshold,
        // and the sign of slip matches the car's direction, reduce torque.
        for (int i = 0; i < poweredAxles.Count; i++) {

            if (poweredAxles[i] == null)
                continue;

            // Left wheel — only engage when grounded; airborne driven wheels spin freely and
            // report huge slip, which would trigger phantom engine cut on every bump.
            if (poweredAxles[i].leftWheelCollider != null && poweredAxles[i].leftWheelCollider.isGrounded) {

                float leftSlip = poweredAxles[i].leftWheelCollider.ForwardSlip;

                if (Mathf.Abs(leftSlip) >= engageTCSThreshold && Mathf.Sign(leftSlip) == CarController.direction) {

                    poweredAxles[i].leftWheelCollider.CutTractionTCS(scaledTCSIntensity);
                    TCSEngaged = true;

                }

            }

            // Right wheel — same grounding guard as left.
            if (poweredAxles[i].rightWheelCollider != null && poweredAxles[i].rightWheelCollider.isGrounded) {

                float rightSlip = poweredAxles[i].rightWheelCollider.ForwardSlip;

                if (Mathf.Abs(rightSlip) >= engageTCSThreshold && Mathf.Sign(rightSlip) == CarController.direction) {

                    poweredAxles[i].rightWheelCollider.CutTractionTCS(scaledTCSIntensity);
                    TCSEngaged = true;

                }

            }

        }

    }


    /// <summary>
    /// Returns a normalized [0-1] factor based on how much suspension force
    /// the wheels are applying right now. 0 = all wheels off-ground, 1 = ~static weight.
    /// </summary>
    private float GetWheelForceFactor() {

        if (CarController == null || CarController.Rigid == null)
            return 0f;

        if (CarController.AxleManager == null || CarController.AxleManager.Axles == null)
            return 0f;

        var rb = CarController.Rigid;
        float g = Physics.gravity.magnitude;
        int wheelCount = 0;
        float totalForce = 0f;

        // assume your AxleManager has a list of all axles
        foreach (var axle in CarController.AxleManager.Axles) {

            if (axle == null)
                continue;

            foreach (var wc in new[] { axle.leftWheelCollider, axle.rightWheelCollider }) {

                if (wc == null || wc.WheelCollider == null)
                    continue;

                WheelHit hit;
                if (wc.WheelCollider.GetGroundHit(out hit)) {
                    totalForce += hit.force;  // suspension force this wheel applies
                    wheelCount++;
                }

            }

        }

        if (wheelCount == 0)
            return 0f;

        // static weight per wheel = mass * g / wheelCount
        float referenceForcePerWheel = (rb.mass * g) / wheelCount;
        // average actual force per wheel
        float avgForcePerWheel = totalForce / wheelCount;

        // normalize and clamp
        return Mathf.Clamp01(avgForcePerWheel / referenceForcePerWheel);

    }

    /// <summary>
    /// SteerHelper uses wheel contact positions for geometry-aware physics.
    /// Applies corrective forces at individual wheel contact points based on load.
    /// </summary>
    private void SteerHelper() {

        if (CarController == null || CarController.Rigid == null)
            return;

        if (CarController.Rigid.isKinematic)
            return;

        if (CarController.AxleManager == null || CarController.AxleManager.Axles == null)
            return;

        // get our new physical grounding factor - increased response speed from 3.5f to 5.5f
        wheelForceFactor = Mathf.Lerp(wheelForceFactor, GetWheelForceFactor(), Time.fixedDeltaTime * 5.5f);

        if (!CarController.IsGrounded)
            wheelForceFactor -= Time.fixedDeltaTime * 12f;

        if (wheelForceFactor < 0)
            wheelForceFactor = 0f;

        if (wheelForceFactor < 0.1f)
            return;

        // Get total wheel load and count grounded wheels
        float totalWheelForce = 0f;
        int groundedWheelCount = 0;
        int totalWheelCount = 0;

        foreach (var axle in CarController.AxleManager.Axles) {

            if (axle == null)
                continue;

            WheelHit hit;
            totalWheelCount += 2;

            if (axle.leftWheelCollider != null && axle.leftWheelCollider.WheelCollider != null) {

                if (axle.leftWheelCollider.WheelCollider.GetGroundHit(out hit)) {

                    totalWheelForce += hit.force;
                    groundedWheelCount++;

                }

            }

            if (axle.rightWheelCollider != null && axle.rightWheelCollider.WheelCollider != null) {

                if (axle.rightWheelCollider.WheelCollider.GetGroundHit(out hit)) {

                    totalWheelForce += hit.force;
                    groundedWheelCount++;

                }

            }

        }

        if (totalWheelForce < 0.1f || groundedWheelCount == 0)
            return;

        // Calculate groundedness ratio (1.0 = all wheels grounded, 0.5 = half grounded, etc.)
        float groundednessRatio = (float)groundedWheelCount / (float)totalWheelCount;

        // Scale helper strength based on how many wheels are grounded
        // Full strength at 4 wheels, reduced strength at 2-3 wheels, minimal at 1 wheel
        float groundedScaling = Mathf.Pow(groundednessRatio, 1.5f);

        // Calculate speed-based scaling for steering helper
        // Low speeds: minSpeedHelperStrength (default 20% - nimble, responsive)
        // High speeds: maxSpeedHelperStrength (default 100% - stable)
        // Transition point: fullHelperSpeed (default 80 km/h)
        float speedFactor = Mathf.InverseLerp(0f, fullHelperSpeed, CarController.absoluteSpeed);
        speedFactor = Mathf.Lerp(minSpeedHelperStrength, maxSpeedHelperStrength, speedFactor);

        // High-speed safety: reduce helper strength at high speeds with partial ground contact
        // Prevents flips during bumps/jumps at highway speeds
        if (CarController.absoluteSpeed > highSpeedThreshold && groundedWheelCount < totalWheelCount) {

            float highSpeedSafety = Mathf.Lerp(1f, 0.4f, (CarController.absoluteSpeed - highSpeedThreshold) / highSpeedSafetyRange);
            speedFactor *= highSpeedSafety;

        }

        // Apply speed factor to grounded scaling
        groundedScaling *= speedFactor;

        // Detect sudden wheel lift-off (bump/jump detection)
        int wheelCountChange = Mathf.Abs(groundedWheelCount - previousGroundedWheelCount);
        bool suddenGroundChange = wheelCountChange >= 1;

        // Smooth the grounded scaling to prevent sudden force spikes
        // Slower smoothing during transitions (bumps/jumps), faster during stable driving
        // Increased stable driving speed from 8f to 12f for better responsiveness
        float smoothingSpeed = suddenGroundChange ? 4f : 12f;
        groundedScaling = Mathf.Lerp(previousGroundedScaling, groundedScaling, Time.fixedDeltaTime * smoothingSpeed);

        // Limit maximum change per frame to prevent force spikes
        // Max 15% change per frame = safe, gradual transitions
        float maxChangePerFrame = 0.15f;
        float scalingDelta = groundedScaling - previousGroundedScaling;
        scalingDelta = Mathf.Clamp(scalingDelta, -maxChangePerFrame, maxChangePerFrame);
        groundedScaling = previousGroundedScaling + scalingDelta;

        // Store for next frame
        previousGroundedScaling = groundedScaling;
        previousGroundedWheelCount = groundedWheelCount;

        // Transform current velocity into local space for lateral calculations
        Vector3 localVelocity = transform.InverseTransformDirection(CarController.Rigid.linearVelocity);

        // Apply corrective forces at each wheel contact point
        foreach (var axle in CarController.AxleManager.Axles) {

            if (axle == null)
                continue;

            if (axle.leftWheelCollider != null)
                ApplyWheelSteerHelper(axle.leftWheelCollider, localVelocity, totalWheelForce, groundedScaling);

            if (axle.rightWheelCollider != null)
                ApplyWheelSteerHelper(axle.rightWheelCollider, localVelocity, totalWheelForce, groundedScaling);

        }

        // Global angular velocity damping for stability
        // Scale damping based on groundedness to avoid abnormal behavior when wheels are off ground
        // Also scale by stability factor to reduce damping when handbrake is applied
        float correctedSteerHelper = steerHelperStrength * 1f * wheelForceFactor * groundedScaling * currentStabilityFactor;

        Vector3 angVel = CarController.Rigid.angularVelocity;
        angVel.y *= (1f - (correctedSteerHelper * 0.1f));

        CarController.Rigid.angularVelocity = Vector3.Lerp(
            CarController.Rigid.angularVelocity,
            angVel,
            wheelForceFactor * 0.5f * groundedScaling * currentStabilityFactor
        );

    }

    /// <summary>
    /// Applies steering helper forces at individual wheel contact points.
    /// Uses wheel load to scale force intensity, creating geometry-aware physics.
    /// Scales forces based on how many wheels are grounded to prevent abnormal behavior.
    /// </summary>
    private void ApplyWheelSteerHelper(RCCP_WheelCollider wc, Vector3 localVel, float totalForce, float groundedScaling) {

        if (wc == null || wc.WheelCollider == null)
            return;

        WheelHit hit;

        if (!wc.WheelCollider.GetGroundHit(out hit))
            return;

        // Check if wheel is actually making meaningful contact
        // Adaptive threshold based on speed - lower at low speeds for better response
        float minForceThreshold = Mathf.Lerp(5f, 15f, CarController.absoluteSpeed / 100f);
        if (hit.force < minForceThreshold)
            return;

        // Calculate per-wheel strength based on load distribution
        float wheelLoadFactor = hit.force / totalForce;

        // Add damping when wheel force is low (transitioning state)
        // This prevents sudden force spikes when wheels are lifting off
        float contactStability = Mathf.Clamp01(hit.force / (CarController.Rigid.mass * Physics.gravity.magnitude * 0.25f));

        // Scale by stability factor to reduce steering helper when handbrake is applied
        float helperStrength = steerHelperStrength * 1f * wheelLoadFactor * groundedScaling * contactStability * currentStabilityFactor;

        // Adaptive lateral force scaling based on speed
        // More aggressive at low speeds for better maneuverability, gentler at high speeds for stability
        float lateralForceScale = Mathf.Lerp(2.5f, 1.8f, CarController.absoluteSpeed / fullHelperSpeed);

        // Lateral correction force at contact point
        // Reduces sideways slip by applying force opposite to lateral velocity
        Vector3 targetLateralForce = -transform.right * localVel.x * helperStrength * lateralForceScale;

        // Initialize smoothed force tracking for this wheel if not exists
        if (!smoothedLateralForces.ContainsKey(wc))
            smoothedLateralForces[wc] = Vector3.zero;

        // Smooth the lateral force transition
        smoothedLateralForces[wc] = Vector3.Lerp(
            smoothedLateralForces[wc],
            targetLateralForce,
            Time.fixedDeltaTime * steerHelperForceSmoothing
        );

        // Use Force instead of VelocityChange to prevent instant velocity changes
        // This makes transitions smoother when wheels lose contact
        CarController.Rigid.AddForceAtPosition(smoothedLateralForces[wc], hit.point, ForceMode.Force);

        // Steering assist force at contact point
        // Creates natural yaw torque based on wheelbase geometry
        if (Mathf.Abs(CarController.steerAngle) > minSteerAngleForAssist) {

            // Get the theoretical max steering angle from front axle
            float theoreticalMaxSteerAngle = frontAxle != null ? frontAxle.maxSteerAngle : 40f;

            // Get the effective max steering angle at current speed
            // This accounts for RCCP_Input's steering curve reduction
            // steerInput_P is already processed by the curve, so we can derive the effective max
            float effectiveMaxSteerAngle = theoreticalMaxSteerAngle;

            // If we have a valid steer input, calculate the effective max based on actual angle vs input
            // This gives us the real maximum considering steering curve and limiters
            if (Mathf.Abs(CarController.steerInput_P) > 0.01f) {
                // The actual angle divided by the input gives us the effective max at this speed
                effectiveMaxSteerAngle = Mathf.Abs(CarController.steerAngle / CarController.steerInput_P);
            }

            // Calculate normalized steering factor based on effective maximum
            // This properly accounts for RCCP_Input's modifications
            float normalizedSteer = Mathf.Clamp01(Mathf.Abs(CarController.steerAngle) / effectiveMaxSteerAngle);
            normalizedSteer *= Mathf.Sign(CarController.steerAngle);

            // Scale assist based on speed (more assist at lower speeds, less at high speeds)
            float speedAssistFactor = Mathf.InverseLerp(steerAssistMinSpeed, steerAssistMaxSpeed, CarController.absoluteSpeed);

            // Apply steering assist force
            float steerFactor = normalizedSteer * speedAssistFactor;
            Vector3 targetSteerForce = transform.right * helperStrength * steerFactor * 1.5f;

            // Use Force mode and scale by mass for consistent behavior
            // This prevents sudden torque when wheels lose contact
            targetSteerForce *= CarController.Rigid.mass;

            // Initialize smoothed force tracking for this wheel if not exists
            if (!smoothedSteerForces.ContainsKey(wc))
                smoothedSteerForces[wc] = Vector3.zero;

            // Smooth the steering force transition
            smoothedSteerForces[wc] = Vector3.Lerp(
                smoothedSteerForces[wc],
                targetSteerForce,
                Time.fixedDeltaTime * steerHelperForceSmoothing
            );

            // Apply smoothed steering force
            CarController.Rigid.AddForceAtPosition(smoothedSteerForces[wc], hit.point, ForceMode.Force);

        } else {

            // Reset smoothed steering force when not steering
            if (smoothedSteerForces.ContainsKey(wc))
                smoothedSteerForces[wc] = Vector3.Lerp(smoothedSteerForces[wc], Vector3.zero, Time.fixedDeltaTime * steerHelperForceSmoothing * 2f);

        }

    }


    /// <summary>
    /// Traction helper. Reduces front axle lateral stiffness if the vehicle's
    /// lateral slip or angular velocity is high, preventing spins.
    /// Enhanced with adaptive response based on speed and improved smoothing.
    /// </summary>
    private void TractionHelper() {

        // 1. Basic checks
        if (!CarController.IsGrounded)
            return;                 // Don't apply traction help if car is airborne.

        if (CarController.Rigid == null || CarController.Rigid.isKinematic)
            return;                 // Skip if the Rigidbody is not being simulated normally.

        if (frontAxle == null)
            return;

        // 2. Grab the car's velocity and remove any vertical component
        Vector3 velocity = CarController.Rigid.linearVelocity;
        velocity -= transform.up * Vector3.Dot(velocity, transform.up);

        // Optional: Early out if velocity is nearly zero to avoid undefined directions
        if (velocity.sqrMagnitude < 0.0001f) {

            // Smooth return to full grip
            smoothedTractionStiffness = Mathf.Lerp(smoothedTractionStiffness, 1f, Time.fixedDeltaTime * 8f);
            frontAxle.tractionHelpedSidewaysStiffness = smoothedTractionStiffness;
            return;

        }

        // Normalize to keep only direction
        velocity.Normalize();

        // 3. Calculate the angle between the car's forward vector and the velocity direction
        float crossDot = Vector3.Dot(Vector3.Cross(transform.forward, velocity), transform.up);
        // Clamp to avoid domain errors in Asin if crossDot slightly exceeds [-1..1]
        crossDot = Mathf.Clamp(crossDot, -1f, 1f);

        float angle = -Mathf.Asin(crossDot);

        // 4. Get the yaw (angular velocity around Y axis)
        float angularVelo = CarController.Rigid.angularVelocity.y;

        // 5. Decide whether to reduce front-axle grip
        //    Check if the angle sign is "opposite" the steerAngle sign (angle * steerAngle < 0).
        //    This indicates counter-steering or loss of control
        if (angle * frontAxle.steerAngle < 0) {

            // Adaptive minimum grip based on speed
            // Higher minimum at low speeds (better control), lower at high speeds (easier drift initiation)
            float minGrip = Mathf.Lerp(0.3f, 0.15f, CarController.absoluteSpeed / fullHelperSpeed);

            // Get the theoretical max steering angle
            float theoreticalMaxSteerAngle = frontAxle != null ? frontAxle.maxSteerAngle : 40f;

            // Calculate effective max based on current steering input
            // This accounts for RCCP_Input's steering curve and limiters
            float effectiveMaxSteerAngle = theoreticalMaxSteerAngle;

            if (Mathf.Abs(CarController.steerInput_P) > 0.01f) {
                // Derive effective max from actual angle vs processed input
                effectiveMaxSteerAngle = Mathf.Abs(CarController.steerAngle / CarController.steerInput_P);
            }

            // Calculate normalized steering using the effective maximum
            float normalizedSteerInput = Mathf.Clamp01(Mathf.Abs(frontAxle.steerAngle) / effectiveMaxSteerAngle);

            // Speed-based response scaling
            // More aggressive at high speeds, gentler at low speeds
            float speedResponseScale = Mathf.Lerp(0.7f, 1.2f, CarController.absoluteSpeed / fullHelperSpeed);

            // The higher the angular velocity and steering input, the more we reduce stiffness
            // Enhanced with speed-based scaling for better response across speed ranges
            // Scale by stability factor to reduce traction helper when handbrake is applied
            float clampFactor = Mathf.Clamp01(
                tractionHelperStrength
                * Mathf.Abs(angularVelo)
                * (0.4f + normalizedSteerInput * 0.6f)
                * speedResponseScale
                * currentStabilityFactor
            );

            // Calculate target stiffness
            float targetStiffness = Mathf.Lerp(1f, minGrip, clampFactor);

            // Smooth the stiffness transition for better feel
            // Faster response when reducing grip (entering slide), slower when restoring (exiting slide)
            float smoothSpeed = targetStiffness < smoothedTractionStiffness ? 10f : 6f;
            smoothedTractionStiffness = Mathf.Lerp(smoothedTractionStiffness, targetStiffness, Time.fixedDeltaTime * smoothSpeed);

            frontAxle.tractionHelpedSidewaysStiffness = smoothedTractionStiffness;

        } else {

            // If angles aren't conflicting, restore full grip smoothly
            smoothedTractionStiffness = Mathf.Lerp(smoothedTractionStiffness, 1f, Time.fixedDeltaTime * 8f);
            frontAxle.tractionHelpedSidewaysStiffness = smoothedTractionStiffness;

        }

    }

    /// <summary>
    /// Angular drag helper. Gradually increases Rigidbody's angularDrag
    /// at higher speeds for more stability, but scales it down while
    /// the player is actually steering in the same direction of the
    /// car's turn. Uses a calculated steering scale instead of a fixed factor.
    /// </summary>
    private void AngularDragHelper() {

        if (CarController.Rigid == null || CarController.Rigid.isKinematic)
            return;

        float baseDrag = 0f;
        float maxDrag = 10f;

        float speedFactor = (CarController.absoluteSpeed * angularDragHelperStrength) / 1000f;

        if (!CarController.IsGrounded)
            speedFactor *= 4f;

        speedFactor = Mathf.Clamp01(speedFactor);

        float targetAngularDrag = Mathf.Lerp(baseDrag, maxDrag, speedFactor);

        float steerDifference = Mathf.Abs(CarController.steerInput_V) - Mathf.Abs(CarController.steerInput_P);
        steerDifference *= 100f;
        steerDifference = Mathf.Clamp01(steerDifference);

        if (steerDifference > 0.05f) {

            float steerAmount = Mathf.Clamp01((steerDifference - 0.1f) / (1f - 0.1f));
            float maxSteerDragReduction = 0.6f;
            float steeringDragScale = 1f - (maxSteerDragReduction * steerAmount * 1f);

            targetAngularDrag *= steeringDragScale;

        }

        // Scale by stability factor to reduce angular drag helper when handbrake is applied
        targetAngularDrag *= currentStabilityFactor;

        // Finally apply the computed drag
        CarController.Rigid.angularDamping = Mathf.Lerp(CarController.Rigid.angularDamping, targetAngularDrag, Time.fixedDeltaTime * 2f);

    }


    /// <summary>
    /// Limits the maximum drift angle if it exceeds maxDriftAngle
    /// by damping angular velocity.
    /// </summary>
    private void LimitDriftAngle() {

        if (CarController.Rigid == null)
            return;

        // Skip when airborne — damping angular velocity mid-jump/ramp feels unnatural
        // (the car auto-corrects its rotation in the air). Drift limiting only makes sense
        // on the ground where tire slip can actually cause an uncontrollable drift angle.
        if (!CarController.IsGrounded)
            return;

        // 1. Acquire current velocity and forward direction
        Vector3 velocity = CarController.Rigid.linearVelocity;
        Vector3 forward = transform.forward;

        // 2. Compute the signed angle (in degrees) between 'forward' and 'velocity',
        //    using Vector3.up as the axis. This effectively measures the yaw angle
        //    relative to the car's forward direction.
        float angle = Vector3.SignedAngle(forward, velocity, Vector3.up);

        // 3. If the absolute drift angle is beyond the desired maxDriftAngle,
        //    we damp the car's angular velocity, pulling it back toward zero rotation.
        //    Scale by stability factor to reduce correction when handbrake is applied
        if (Mathf.Abs(angle) > maxDriftAngle) {

            CarController.Rigid.angularVelocity = Vector3.Lerp(
                CarController.Rigid.angularVelocity,
                Vector3.zero,
                Time.fixedDeltaTime * driftAngleCorrectionFactor * groundedFactor * currentStabilityFactor
            );

        }

    }

    /// <summary>
    /// Resets all stability system states and runtime variables to defaults.
    /// </summary>
    public void Reload() {

        // Main stability system engaged states
        ABSEngaged = false;
        ESPEngaged = false;
        ESPIndicatorEngaged = false;
        TCSEngaged = false;

        groundedFactor = 0f;
        currentStabilityFactor = 1f;

        // Reset force smoothing values
        previousGroundedScaling = 1f;
        previousGroundedWheelCount = 4;

        // Reset drift state
        driftIntensity = 0f;
        smoothedDriftIntensity = 0f;

        // Reset drift force/torque smoothing
        smoothedDriftTorque = Vector3.zero;
        smoothedDriftForwardForce = Vector3.zero;
        smoothedDriftSidewaysForce = Vector3.zero;

        // Reset ESP brake torque smoothing
        smoothedESPFrontLeftBrake = 0f;
        smoothedESPFrontRightBrake = 0f;
        smoothedESPRearLeftBrake = 0f;
        smoothedESPRearRightBrake = 0f;

        // Reset ESP V2 state (hysteresis, PD derivative, mode-commit, lag filter, sideslip, UI debounce)
        _espPersistentlyEngaged = false;
        _espEngagementTimer = 0f;
        _espUIHoldTimer = 0f;
        _prevYawError = 0f;
        _filteredYawRef = 0f;
        _sideslipAngle = 0f;
        _prevSideslipAngle = 0f;
        _sideslipRate = 0f;
        _prevIsOversteer = false;
        _modeCommitTimer = 0f;

        // Reset ESP V2 telemetry
        debugYawActualDegS = 0f;
        debugYawRefDegS = 0f;
        debugYawErrorDegS = 0f;
        debugSideslipAngleDeg = 0f;
        debugSideslipRateDegS = 0f;
        debugActiveWheelIndex = -1;
        debugIsOversteer = false;

        // Reset ESP geometry cache (will be recalculated on next Start)
        espGeometryCached = false;

        // Reset steer helper force smoothing
        smoothedLateralForces.Clear();
        smoothedSteerForces.Clear();

        // Reset traction helper smoothing
        smoothedTractionStiffness = 1f;
        wheelForceFactor = 0f;

    }

}
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
using System.Linq;

/// <summary>
/// Main power generator of the vehicle. Produces and transmits the generated power to the clutch.
/// Enhanced version with improved calculations for more realistic engine behavior.
/// </summary>
[DefaultExecutionOrder(-7)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Drivetrain/RCCP Engine")]
public class RCCP_Engine : RCCP_Component {

    /// <summary>
    /// If true, overrides the engine RPM with an externally provided value. All internal calculations will be skipped.
    /// </summary>
    [Header("Engine State")]
    [Tooltip("If true, overrides the engine RPM with an externally provided value. All internal calculations will be skipped.")]
    public bool overrideEngineRPM = false;

    /// <summary>
    /// Indicates whether the engine is currently running.
    /// </summary>
    [Tooltip("Indicates whether the engine is currently running.")]
    public bool engineRunning = true;

    /// <summary>
    /// Indicates whether the engine is in the process of starting (a brief delay).
    /// </summary>
    [Tooltip("Indicates whether the engine is in the process of starting (a brief delay).")]
    public bool engineStarting = false;

    /// <summary>
    /// Current engine RPM (revolutions per minute).
    /// </summary>
    [Header("RPM")]
    [Min(0f), Tooltip("Current engine RPM (revolutions per minute).")]
    public float engineRPM = 0f;

    /// <summary>
    /// Minimum engine RPM (typical idle speed).
    /// </summary>
    [Min(0f), Tooltip("Minimum engine RPM (idle speed).")]
    public float minEngineRPM = 750f;

    /// <summary>
    /// Maximum engine RPM (redline).
    /// </summary>
    [Min(0f), Tooltip("Maximum engine RPM (redline).")]
    public float maxEngineRPM = 7000f;

    /// <summary>
    /// Rate at which the engine freely accelerates when the drivetrain is disengaged (e.g., clutch in, neutral).
    /// </summary>
    [Header("Acceleration / Deceleration")]
    [Tooltip("Rate at which the engine freely accelerates when drivetrain is disengaged.")]
    [Min(0f)] public float engineAccelerationRate = .75f;

    /// <summary>
    /// If true, enables dynamic adjustment of acceleration rate based on engine load.
    /// </summary>
    [Tooltip("Enables dynamic adjustment of acceleration rate based on engine load.")]
    public bool enableDynamicAcceleration = false;

    /// <summary>
    /// Base acceleration rate stored for dynamic calculations when enableDynamicAcceleration is true.
    /// </summary>
    private float baseEngineAccelerationRate = .75f;

    /// <summary>
    /// How strongly the engine couples to the wheels when the clutch is engaged.
    /// </summary>
    [Tooltip("How strongly the engine couples to the wheels when the clutch is engaged.")]
    [Min(0f)] public float engineCouplingToWheelsRate = 1.5f;

    /// <summary>
    /// Rate at which the engine RPM drops due to friction or no throttle input.
    /// </summary>
    [Tooltip("Rate at which the engine RPM drops due to friction or no throttle input.")]
    [Min(0f)] public float engineDecelerationRate = .35f;

    /// <summary>
    /// Raw target RPM used internally for smoothing.
    /// </summary>
    [Min(0f)] internal float wantedEngineRPMRaw = 0f;

    /// <summary>
    /// Internal velocity for SmoothDamp usage on engine RPM.
    /// </summary>
    private float engineVelocity;

    /// <summary>
    /// Torque curve (normalized 0-1) for the engine. X axis is RPM, Y axis is normalized torque (0-1).
    /// </summary>
    [Header("Torque")]
    [Tooltip("Torque curve for the engine. X = RPM, Y = normalized torque (0-1).")]
    public AnimationCurve NMCurve = new AnimationCurve(new Keyframe(800f, .38f), new Keyframe(2560f, .82f), new Keyframe(4000f, 1f), new Keyframe(5800f, .82f), new Keyframe(8000f, .5f));

    /// <summary>
    /// If true, automatically generates the NMCurve based on minEngineRPM, maxTorqueAtRPM, and maxEngineRPM when the script is reset.
    /// </summary>
    [Tooltip("Automatically generates the torque curve based on min/max RPM values.")]
    public bool autoCreateNMCurve = true;

    /// <summary>
    /// Desired RPM at which the engine produces peak torque (used if autoCreateNMCurve is true).
    /// </summary>
    [Min(0f), Tooltip("Maximum torque output in Newton-meters.")]
    public float maximumTorqueAsNM = 200f;

    [Min(0f), Tooltip("RPM at which the engine produces peak torque.")]
    public float peakRPM = 4000f;

    /// <summary>
    /// If true, cuts fuel input once RPM approaches maxEngineRPM to act as a rev limiter.
    /// </summary>
    [Header("Rev Limiter")]
    [Tooltip("Cuts fuel input once RPM approaches max to act as a rev limiter.")]
    public bool engineRevLimiter = true;

    /// <summary>
    /// Becomes true when the rev limiter is actively cutting fuel.
    /// </summary>
    [Tooltip("True when the rev limiter is actively cutting fuel.")]
    public bool cutFuel = false;

    /// <summary>
    /// Frequency of rev limiter cuts per second when active.
    /// </summary>
    [Range(5f, 30f), Tooltip("Frequency of rev limiter cuts per second when active.")]
    public float revLimiterCutFrequency = 15f;

    /// <summary>
    /// Internal timer for rev limiter cut cycles.
    /// </summary>
    private float revLimiterTimer = 0f;

    /// <summary>
    /// Enables forced induction simulation (turbo). If true, turboChargePsi is calculated each frame.
    /// </summary>
    [Header("Turbo")]
    [Tooltip("Enables turbocharger simulation.")]
    public bool turboCharged = false;

    /// <summary>
    /// Current turbo pressure in PSI.
    /// </summary>
    [Min(0f), Tooltip("Current turbo boost pressure in PSI.")]
    public float turboChargePsi = 0f;

    /// <summary>
    /// Last frame's PSI, used for blow-off detection.
    /// </summary>
    [Min(0f)]
    internal float turboChargePsi_Old = 0f;

    /// <summary>
    /// Max turbo boost (PSI) that can be reached at full throttle and high RPM.
    /// </summary>
    [Min(0f), Tooltip("Maximum turbo boost pressure in PSI.")]
    public float maxTurboChargePsi = 12f;

    /// <summary>
    /// Maximum torque multiplier from the turbo at max boost.
    /// </summary>
    [Range(1f, 2f), Tooltip("Torque multiplier from turbo at max boost.")]
    public float turboChargerCoEfficient = 1.25f;

    /// <summary>
    /// True if the turbo is venting/blowing off due to sudden throttle closure.
    /// </summary>
    [HideInInspector] public bool turboBlowOut = false;

    /// <summary>
    /// Additional multiplier applied to the engine torque (e.g., from nitrous).
    /// </summary>
    private float multiplier = 1f;

    /// <summary>
    /// Engine friction factor. Higher values cause RPM to drop faster when throttle is released.
    /// </summary>
    [Header("Friction / Inertia")]
    [Range(0f, 1f), Tooltip("Engine friction. Higher values cause RPM to drop faster.")]
    public float engineFriction = .2f;

    /// <summary>
    /// Engine inertia factor. Lower values let the engine rev up/down more quickly.
    /// </summary>
    [Range(.01f, 1f), Tooltip("Engine inertia. Lower values = faster RPM changes.")]
    public float engineInertia = .2f;

    /// <summary>
    /// Dynamically calculated engine inertia based on RPM and engine load.
    /// </summary>
    private float sensitiveEngineInertia = 1f;

    /// <summary>
    /// Current engine load factor (0-1). Calculated based on throttle input and resistance.
    /// </summary>
    [Range(0f, 1f), Tooltip("Current engine load factor (0-1).")]
    public float engineLoad = 0f;

    /// <summary>
    /// Current engine torque output (Newton-meters).
    /// </summary>
    [Min(0f), Tooltip("Current torque output in Newton-meters.")]
    public float producedTorqueAsNM = 0f;

    /// <summary>
    /// Current fuel input to the engine (0-1). Combines throttle input with idle adjustment.
    /// </summary>
    [Range(0f, 1f), Tooltip("Current fuel input (0-1). Combines throttle and idle.")]
    public float fuelInput = 0f;

    /// <summary>
    /// Idle compensation input to prevent engine stalling at low RPM.
    /// </summary>
    [Range(0f, 1f), Tooltip("Idle compensation to prevent stalling at low RPM.")]
    public float idleInput = 0f;

    /// <summary>
    /// If true, simulates realistic engine temperature effects on performance.
    /// </summary>
    [Header("Temperature")]
    [Tooltip("Simulates engine temperature effects on performance.")]
    public bool simulateEngineTemperature = false;

    /// <summary>
    /// Current engine operating temperature in Celsius.
    /// </summary>
    [Range(20f, 150f), Tooltip("Current engine temperature in Celsius.")]
    public float engineTemperature = 85f;

    /// <summary>
    /// Optimal engine operating temperature for peak performance.
    /// </summary>
    [Range(70f, 100f), Tooltip("Optimal temperature for peak performance.")]
    public float optimalTemperature = 85f;

    /// <summary>
    /// Ambient temperature affecting engine cooling rate.
    /// </summary>
    [Range(-20f, 50f), Tooltip("Ambient temperature affecting engine cooling.")]
    public float ambientTemperature = 20f;

    /// <summary>
    /// If true, enables Variable Valve Timing simulation for enhanced torque at specific RPM ranges.
    /// </summary>
    [Header("VVT")]
    [Tooltip("Enables Variable Valve Timing simulation for enhanced torque.")]
    public bool enableVVT = false;

    /// <summary>
    /// RPM range where VVT provides optimal performance boost.
    /// </summary>
    [Tooltip("RPM range where VVT provides optimal performance boost.")]
    public Vector2 vvtOptimalRange = new Vector2(3000f, 6000f);

    /// <summary>
    /// Torque multiplier applied when engine is in VVT optimal range.
    /// </summary>
    [Range(1f, 1.3f), Tooltip("Torque multiplier when in VVT optimal range.")]
    public float vvtTorqueMultiplier = 1.1f;

    /// <summary>
    /// If true, enables knock detection and protection.
    /// </summary>
    [Header("Knock Detection")]
    [Tooltip("Enables engine knock detection and protection.")]
    public bool enableKnockDetection = false;

    /// <summary>
    /// Current knock factor (0-1). Higher values reduce engine performance.
    /// </summary>
    [Range(0f, 1f), Tooltip("Current knock factor. Higher values reduce performance.")]
    public float knockFactor = 0f;

    [Header("Engine Braking")]
    [Tooltip("Enables fake engine braking — applies negative torque to driven wheels when off-throttle and clutch is engaged, simulating engine pumping/friction losses.")]
    public bool engineBraking = true;

    [Range(0f, 0.4f), Tooltip("Engine-brake torque as a fraction of peak engine torque at redline. Real naturally-aspirated engines: ~0.10-0.20.")]
    public float engineBrakingCoefficient = 0.15f;

    [Range(0f, 1f), Tooltip("RPM fraction at which engine braking starts to fade in. 0 = active from idle, 1 = active only at redline. Below this point engine brake is 0.")]
    public float engineBrakingMinRPMFactor = 0.15f;

    [Range(0f, 0.5f), Tooltip("Throttle input above which engine braking fades to 0. Below this it scales linearly to full strength at zero throttle.")]
    public float engineBrakingThrottleCutoff = 0.05f;

    [Range(1f, 4f), Tooltip("Maximum extra engine-brake multiplier when a downshift would force driven-wheel equivalent engine RPM past redline.")]
    public float overRevEngineBrakingMultiplier = 2.5f;

    [Header("Speed")]
    [Tooltip("If enabled, editing Maximum Speed automatically tunes each Differential's finalDriveRatio so the vehicle tops out at that value. Disable to author finalDriveRatio manually on the differentials; Maximum Speed then becomes informational only.")]
    public bool autoCalculateDifferentialRatio = true;
    [Min(0f), Tooltip("Target maximum speed in km/h. When Auto Calculate Differential Ratio is enabled, editing this value overwrites every differential's finalDriveRatio. When disabled, this field has no effect at runtime.")]
    public float maximumSpeed = 240f;
    [Min(0f), Tooltip("Previous frame's maximum speed value, used to detect changes and trigger recalculation.")]
    public float maximumSpeed_Old = 240f;

    /// <summary>
    /// Events for torque output, using a custom class.
    /// </summary>
    [Tooltip("Event invoked each physics frame to transmit produced torque downstream to the clutch.")]
    public RCCP_Event_Output outputEvent = new RCCP_Event_Output();
    [Tooltip("Cached output data structure carrying the current torque value in Newton-meters.")]
    public RCCP_Output output = new RCCP_Output();

    public override void Awake() {

        base.Awake();

        maximumSpeed_Old = maximumSpeed;
        if (autoCalculateDifferentialRatio)
            UpdateMaximumSpeed();

    }

    public override void Start() {

        base.Start();

        // Store base acceleration rate for dynamic calculations only if enabled
        if (enableDynamicAcceleration)
            baseEngineAccelerationRate = engineAccelerationRate;

        if (autoCreateNMCurve)
            CheckAndCreateNMCurve();

    }

    private void Update() {

        // Inspector-driven max-speed change detection runs at render rate; physics state lives in FixedUpdate.
        if (autoCalculateDifferentialRatio && maximumSpeed != maximumSpeed_Old)
            UpdateMaximumSpeed();

        maximumSpeed_Old = maximumSpeed;

    }

    private void FixedUpdate() {

        // Inputs() runs in the fixed timestep so the rev limiter and idle math
        // remain frame-rate independent (they integrate via Time.fixedDeltaTime).
        Inputs();
        RPM();
        TurboCharger();
        EngineTemperature();
        GenerateKW();
        NegativeFeedback();
        Output();

    }

    /// <summary>
    /// Starts the engine if it's currently off. Plays a delay, then sets engineRunning to true.
    /// </summary>
    public void StartEngine() {

        if (engineRunning || engineStarting)
            return;

        StartCoroutine(StartEngineDelayed());

    }

    /// <summary>
    /// Immediately stops the engine, setting engineRunning to false.
    /// </summary>
    public void StopEngine() {

        engineRunning = false;

    }

    /// <summary>
    /// Coroutine for engine start delay, simulating a brief ignition sequence.
    /// </summary>
    private IEnumerator StartEngineDelayed() {

        engineRunning = false;
        engineStarting = true;
        yield return new WaitForSeconds(1);
        engineStarting = false;
        engineRunning = true;

    }

    /// <summary>
    /// Calculates idleInput, fuelInput, and applies rev-limiter logic.
    /// </summary>
    private void Inputs() {

        if (overrideEngineRPM)
            return;

        // Raise idleInput if RPM is below (minEngineRPM + ~10% buffer).
        // Both the condition and the amount read engineRPM (smoothed) — reading
        // wantedEngineRPMRaw here caused small idle oscillations because the
        // controller was sampling two different RPM states.
        if (engineRPM <= minEngineRPM + (minEngineRPM / 10f))
            idleInput = 1f - Mathf.InverseLerp(minEngineRPM - (minEngineRPM / 10f), minEngineRPM + (minEngineRPM / 10f), engineRPM);
        else
            idleInput = 0f;

        // Combine throttle with idle compensation.
        fuelInput = CarController.throttleInput_P + idleInput;
        fuelInput = Mathf.Clamp01(fuelInput);

        // Enhanced rev limiter with realistic hard cuts
        RevLimiter();

        // If the engine is turned off, no fuel and no idle input.
        if (!engineRunning) {

            fuelInput = 0f;
            idleInput = 0f;

        }

    }

    /// <summary>
    /// Enhanced rev limiter with realistic hard cut behavior for more noticeable effect.
    /// </summary>
    private void RevLimiter() {

        if (!engineRevLimiter) {

            cutFuel = false;
            revLimiterTimer = 0f;
            return;

        }

        float cutThreshold = maxEngineRPM - 100f;

        // Mechanical over-rev: wheel-derived RPM is above redline. The engine RPM
        // itself is clamped to maxEngineRPM + 100 (see RPM()), so the duty-cycled
        // cut below fires off engineRPM and leaves 70 % of frames producing full
        // torque — fine for forward redline-bounce where aero drag balances it,
        // but in reverse (no upshift available) the wheels keep climbing and the
        // vehicle runs away. Hard-cut fuel any time the wheels are spinning
        // faster than redline supports.
        bool mechanicalOverRev = false;

        if (CarController.Gearbox != null && CarController.Gearbox.gearInput > 0f) {

            float gearRatio = Mathf.Abs(CarController.Gearbox.CurrentGearRatio);

            if (gearRatio > 0f) {

                float equivalentEngineRPM = CarController.GetAverageDrivenWheelEngineRPM(gearRatio);

                if (equivalentEngineRPM >= maxEngineRPM)
                    mechanicalOverRev = true;

            }

        }

        if (mechanicalOverRev) {

            // Hard cut every frame; let wheel coupling continue to drive
            // engineRPM via the clamped wantedRaw (so auto-shift logic still
            // sees a high engineRPM and can upshift in forward gears).
            cutFuel = true;
            fuelInput = 0f;
            revLimiterTimer = 0f;
            return;

        }

        if (engineRPM >= cutThreshold) {

            // Update rev limiter timer
            revLimiterTimer += Time.fixedDeltaTime;

            // Calculate cut cycle based on frequency
            float cutCycleDuration = 1f / revLimiterCutFrequency;
            float cutOnDuration = cutCycleDuration * 0.3f; // 30% of cycle is cut
            float cutOffDuration = cutCycleDuration * 0.7f; // 70% of cycle is normal

            // Determine if we should cut fuel based on timer position in cycle
            float cyclePosition = revLimiterTimer % cutCycleDuration;

            if (cyclePosition < cutOnDuration) {

                if (fuelInput >= .5f)
                    wantedEngineRPMRaw -= 20000f * Time.fixedDeltaTime;

                cutFuel = true;
                // Complete fuel cut for more noticeable effect
                fuelInput = 0f;

            } else {

                cutFuel = false;

            }

        } else {

            cutFuel = false;
            revLimiterTimer = 0f;

        }

    }

    /// <summary>
    /// Calculates dynamic engine inertia based on RPM and load for more realistic behavior.
    /// </summary>
    private void CalculateDynamicInertia() {

        // Lower inertia at higher RPMs for more responsive behavior
        float rpmFactor = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM);
        float loadFactor = Mathf.Clamp01(engineLoad);

        // Combine RPM and load factors
        float dynamicFactor = (rpmFactor * 0.7f) + (loadFactor * 0.3f);
        sensitiveEngineInertia = Mathf.Lerp(engineInertia, engineInertia * 0.75f, dynamicFactor);

    }

    /// <summary>
    /// Calculates realistic engine friction that varies with RPM and temperature.
    /// </summary>
    private float CalculateEngineFriction() {

        float baseFriction = engineFriction;

        // Higher friction at higher RPMs
        float rpmFriction = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM) * 0.3f;

        // Temperature affects friction
        float tempFactor = 1f;
        if (simulateEngineTemperature) {

            if (engineTemperature < optimalTemperature) {

                // Cold engine has more friction
                tempFactor = Mathf.Lerp(1.5f, 1f, Mathf.InverseLerp(ambientTemperature, optimalTemperature, engineTemperature));

            } else if (engineTemperature > optimalTemperature + 20f) {

                // Overheated engine has more friction
                tempFactor = Mathf.Lerp(1f, 1.3f, Mathf.InverseLerp(optimalTemperature + 20f, 150f, engineTemperature));

            }

        }

        return (baseFriction + rpmFriction) * tempFactor;

    }

    /// <summary>
    /// Calculates current engine load based on throttle input and resistance from drivetrain.
    /// </summary>
    private void CalculateEngineLoad() {

        // Base load from throttle input
        float throttleLoad = CarController ? CarController.throttleInput_P : 0f;

        // Additional load from drivetrain resistance
        float drivetrainLoad = 0f;

        if (CarController.Clutch) {

            float clutchInput = GetEffectiveClutchInput();

            // More load when clutch is engaged and resisting
            if (clutchInput < 0.5f) {

                drivetrainLoad = (1f - clutchInput) * 0.3f;

            }

        }

        engineLoad = Mathf.Clamp01(throttleLoad + drivetrainLoad);

        // Adjust acceleration rate based on load if dynamic acceleration is enabled
        if (enableDynamicAcceleration) {

            float loadMultiplier = Mathf.Lerp(1.2f, 0.7f, engineLoad);
            engineAccelerationRate = baseEngineAccelerationRate * loadMultiplier;

        }

    }

    /// <summary>
    /// Simulates realistic engine temperature changes based on load and ambient conditions.
    /// </summary>
    private void EngineTemperature() {

        if (!simulateEngineTemperature)
            return;

        // Target temperature based on engine load and ambient temperature
        float baseTargetTemp = ambientTemperature + 65f; // Base operating temp
        float loadTempIncrease = engineLoad * 30f; // Load increases temperature
        float targetTemp = baseTargetTemp + loadTempIncrease;

        // Temperature change rate varies based on conditions
        float tempChangeRate = engineRunning ? 2f : 5f; // Cool down faster when engine is off

        if (engineRunning) {

            engineTemperature = Mathf.MoveTowards(engineTemperature, targetTemp, Time.fixedDeltaTime * tempChangeRate);

        } else {

            engineTemperature = Mathf.MoveTowards(engineTemperature, ambientTemperature, Time.fixedDeltaTime * tempChangeRate);

        }

        engineTemperature = Mathf.Clamp(engineTemperature, ambientTemperature, 150f);

    }

    /// <summary>
    /// Calculates knock factor based on engine load and RPM conditions.
    /// </summary>
    private void CalculateKnockDetection() {

        if (!enableKnockDetection) {

            knockFactor = 0f;
            return;

        }

        // High load at low RPM increases knock risk
        float rpmFactor = Mathf.InverseLerp(maxEngineRPM * 0.3f, maxEngineRPM * 0.6f, engineRPM);
        rpmFactor = 1f - rpmFactor; // Invert so low RPM = high risk

        float loadFactor = engineLoad;

        // Combine factors
        knockFactor = Mathf.Clamp01(rpmFactor * loadFactor * 0.8f);

        // Temperature also affects knock
        if (simulateEngineTemperature && engineTemperature > optimalTemperature + 10f) {

            float tempKnockFactor = (engineTemperature - optimalTemperature - 10f) / 40f;
            knockFactor = Mathf.Clamp01(knockFactor + tempKnockFactor);

        }

    }

    /// <summary>
    /// Handles all RPM-related calculations including dynamic inertia and load.
    /// </summary>
    private void RPM() {

        if (overrideEngineRPM)
            return;

        // Calculate dynamic factors
        CalculateDynamicInertia();
        CalculateEngineLoad();
        CalculateKnockDetection();

        // Get dynamic friction
        float dynamicFriction = CalculateEngineFriction();

        // Read clutch input once.
        float clutchInput = GetEffectiveClutchInput();

        if (!engineRunning) {

            wantedEngineRPMRaw -= 5000f * Time.fixedDeltaTime;

        } else {

            // Smooth-stepped blend between fully engaged (1x) and free-rev (8.5x / 7.2x).
            // Below 0.85: pure engaged behavior. Above 0.95: pure free-rev.
            // The narrow band removes the felt "snap" that the previous binary threshold
            // (clutchInput >= 0.9f) caused — torque transfer through the clutch already
            // changes continuously as receivedTorque * (1 - clutchInput) in RCCP_Clutch.
            float disengageBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 0.95f, clutchInput));
            float accelMultiplier = Mathf.Lerp(1f, 8.5f, disengageBlend);
            float decelMultiplier = Mathf.Lerp(1f, 7.2f, disengageBlend);

            wantedEngineRPMRaw += fuelInput * (engineAccelerationRate * accelMultiplier) * 1000f * Time.fixedDeltaTime;
            wantedEngineRPMRaw -= dynamicFriction * (engineDecelerationRate * decelMultiplier) * 1000f * Time.fixedDeltaTime;

            // Free-rev engine braking ramp scales with how disengaged the clutch is —
            // at engaged, the wheel coupling and base friction already provide deceleration.
            if (fuelInput <= 0.1f) {

                float engineBraking = Mathf.InverseLerp(minEngineRPM, maxEngineRPM, engineRPM) * 200f;
                wantedEngineRPMRaw -= engineBraking * disengageBlend * Time.fixedDeltaTime;

            }

            // Wheel coupling runs every frame; its internal coupling strength
            // already scales by (1 - clutchInput) and gates on Gearbox.gearInput.
            CheckEngineRPMForWheelFeedback();

        }

        // Clamp final raw target RPM.
        wantedEngineRPMRaw = Mathf.Clamp(wantedEngineRPMRaw, 0f, maxEngineRPM + 100f);

        // SmoothDamp final engine RPM for stability.
        engineRPM = Mathf.SmoothDamp(engineRPM, wantedEngineRPMRaw, ref engineVelocity, sensitiveEngineInertia * .35f);

    }

    /// <summary>
    /// Returns the clutch state as the engine should see it during this fixed step.
    /// The clutch component updates after the engine, so forced clutch conditions
    /// such as handbrake and shift presses need to be mirrored here for RPM logic.
    /// </summary>
    private float GetEffectiveClutchInput() {

        if (!CarController || !CarController.Clutch)
            return 0f;

        RCCP_Clutch clutch = CarController.Clutch;
        float clutchInput = clutch.clutchInput;

        if (clutch.forceToPressClutch)
            return 1f;

        bool pressForShift = clutch.pressClutchWhileShiftingGears && CarController.shiftingNow;
        bool pressForHandbrake = clutch.pressClutchWhileHandbraking &&
            (CarController.handbrakeInput_P >= .75f || CarController.handbrakeInput_V >= .75f);

        if (pressForShift || pressForHandbrake)
            clutchInput = 1f;

        return Mathf.Clamp01(clutchInput);

    }

    /// <summary>
    /// Minimum turbo PSI required to trigger a blow-off when throttle is released.
    /// </summary>
    private float turboBlowOffMinPsi = 5f;

    /// <summary>
    /// Threshold under which we consider the throttle "released."
    /// </summary>
    private float throttleLiftThreshold = 0.1f;

    /// <summary>
    /// Tracks throttle input from the previous FixedUpdate.
    /// </summary>
    private float lastFuelInput;

    /// <summary>
    /// Enhanced turbocharging with improved lag and spool-up behavior.
    /// </summary>
    private void TurboCharger() {

        // If engine or turbo is off, gradually reduce PSI.
        if (!engineRunning || !turboCharged) {

            turboChargePsi = Mathf.MoveTowards(turboChargePsi, 0f, Time.fixedDeltaTime * 10f);
            turboBlowOut = false;
            lastFuelInput = fuelInput;
            return;

        }

        // Calculate spool-up curve
        float rpmFactor = Mathf.InverseLerp(minEngineRPM * 1.5f, maxEngineRPM * 0.8f, engineRPM);
        rpmFactor = Mathf.Pow(rpmFactor, 1.5f);

        float targetPsi = maxTurboChargePsi * fuelInput * rpmFactor;
        float spoolRate = Mathf.Lerp(10f, 20f, fuelInput);
        turboChargePsi = Mathf.MoveTowards(turboChargePsi, targetPsi, Time.fixedDeltaTime * spoolRate);

        // Blow-off event detection: when throttle goes from "pressed" to "released"
        bool justLifted = lastFuelInput >= throttleLiftThreshold && fuelInput < throttleLiftThreshold;
        if (justLifted && turboChargePsi > turboBlowOffMinPsi) {

            turboBlowOut = true;

        } else {

            turboBlowOut = false;

        }

        // Store state for next update
        lastFuelInput = fuelInput;

    }

    /// <summary>
    /// Overrides the engine RPM to a specified value, bypassing internal calculations.
    /// </summary>
    /// <param name="targetRPM">Engine RPM to set.</param>
    public void OverrideRPM(float targetRPM) {

        overrideEngineRPM = true;
        engineRPM = targetRPM;
        wantedEngineRPMRaw = targetRPM;

    }

    /// <summary>Disables the engine torque override, returning control to the throttle-based system.</summary>
    public void DisableOverride() {

        overrideEngineRPM = false;

    }

    /// <summary>
    /// Enhanced torque generation with VVT, temperature, and knock compensation.
    /// </summary>
    private void GenerateKW() {

        if (!engineRunning) {

            producedTorqueAsNM = 0f;
            return;

        }

        // Base torque from curve
        float torqueCurveMultiplier = NMCurve.Evaluate(engineRPM);
        float baseTorque = maximumTorqueAsNM * torqueCurveMultiplier * fuelInput;

        // Temperature compensation
        float temperatureMultiplier = 1f;
        if (simulateEngineTemperature) {

            if (engineTemperature < optimalTemperature) {

                // Cold engine produces less power
                temperatureMultiplier = Mathf.Lerp(0.85f, 1f, Mathf.InverseLerp(ambientTemperature, optimalTemperature, engineTemperature));

            } else if (engineTemperature > optimalTemperature + 15f) {

                // Overheated engine loses power
                temperatureMultiplier = Mathf.Lerp(1f, 0.7f, Mathf.InverseLerp(optimalTemperature + 15f, 150f, engineTemperature));

            }

        }

        // VVT bonus
        float vvtMultiplier = 1f;
        if (enableVVT && engineRPM >= vvtOptimalRange.x && engineRPM <= vvtOptimalRange.y) {

            vvtMultiplier = vvtTorqueMultiplier;

        }

        // Knock penalty
        float knockMultiplier = 1f - (knockFactor * 0.3f);

        // Turbo boost
        float turboMultiplier = 1f;
        if (turboCharged && turboChargePsi > 0f) {

            float boostRatio = turboChargePsi / maxTurboChargePsi;
            turboMultiplier = 1f + (boostRatio * (turboChargerCoEfficient - 1f));

        }

        // Wrong-direction + over-rev cap as a [0,1] scale, applied here so it survives the assignment.
        float negativeFeedbackScale = GetNegativeFeedbackTorqueScale();

        // Apply all multipliers
        producedTorqueAsNM = baseTorque * temperatureMultiplier * vvtMultiplier * knockMultiplier * turboMultiplier * negativeFeedbackScale * multiplier;

        // Clamp torque so we never exceed our realistic limit
        producedTorqueAsNM = Mathf.Clamp(producedTorqueAsNM, -maximumTorqueAsNM * 1.8f, maximumTorqueAsNM * 1.8f);

        // Reset frame multiplier
        multiplier = 1f;

    }

    /// <summary>
    /// Returns a torque-scale multiplier in [0, 1] that captures wrong-direction
    /// braking and over-rev protection. Applied inside <see cref="GenerateKW"/> as
    /// part of the multiplier chain, so the cap survives the producedTorqueAsNM
    /// reassignment in the same FixedUpdate frame.
    /// </summary>
    private float GetNegativeFeedbackTorqueScale() {

        if (CarController.PoweredAxles == null ||
            CarController.Gearbox == null ||
            CarController.Differentials == null)
            return 1f;

        float currentGearRatio = Mathf.Abs(CarController.Gearbox.CurrentGearRatio);

        // Gearbox not engaging drive (neutral / park) -> no engine-wheel feedback.
        if (CarController.Gearbox.gearInput <= 0f || Mathf.Approximately(currentGearRatio, 0f))
            return 1f;

        // Compute average wheel RPM (with sign preserved for direction)
        float avgWheelRPM = CarController.GetAverageDrivenWheelRPMSigned();

        if (Mathf.Approximately(avgWheelRPM, 0f))
            return 1f;

        float scale = 1f;

        // Calculate expected wheel rotation direction based on gear
        bool isReverseGear = CarController.direction == -1;
        float expectedWheelDirection = isReverseGear ? -1f : 1f;

        // Check if wheels are spinning in the wrong direction relative to gear
        bool wheelsSpinningWrongDirection = (avgWheelRPM * expectedWheelDirection) < -50f;

        // Apply negative feedback if wheels spinning wrong direction
        if (wheelsSpinningWrongDirection) {

            float wrongDirectionSpeed = Mathf.Abs(avgWheelRPM);
            float brakeFactor = Mathf.Clamp01(wrongDirectionSpeed / 500f);
            scale *= (1f - brakeFactor);

        }

        // Calculate equivalent engine RPM from wheel speed
        float equivalentEngineRPM = CarController.GetAverageDrivenWheelEngineRPM(currentGearRatio);

        // Apply over-rev protection
        if (engineRPM > equivalentEngineRPM * 1.2f && equivalentEngineRPM > 100f) {

            float rpmDifference = engineRPM - equivalentEngineRPM;
            float overRevFactor = Mathf.Clamp01(rpmDifference / 2000f);
            scale *= (1f - overRevFactor * 0.5f);

        }

        return Mathf.Clamp01(scale);

    }

    /// <summary>
    /// Handles the coupling between engine RPM and wheel RPM for realistic behavior.
    /// Fixed to properly handle reverse gear calculations.
    /// </summary>
    private void CheckEngineRPMForWheelFeedback() {

        float clutchInput = GetEffectiveClutchInput();

        // If clutch is fully pressed, no wheel coupling.
        if (clutchInput >= .95f)
            return;

        // Gearbox not engaging drive (neutral / park) → no engine-wheel coupling.
        // CurrentGearRatio still returns the active gear's ratio when gearInput is 0,
        // so without this guard the engine RPM would be dragged toward the wheel-equivalent
        // RPM even though the drivetrain is logically disconnected.
        if (CarController.Gearbox && CarController.Gearbox.gearInput <= 0f)
            return;

        float currentGearRatio = 0f;

        if (CarController.Gearbox)
            currentGearRatio = Mathf.Abs(CarController.Gearbox.CurrentGearRatio);

        // Calculate equivalent engine RPM - use absolute value for engine RPM calculation
        float equivalentEngineRPM = CarController.GetAverageDrivenWheelEngineRPM(currentGearRatio);

        float totalSlip = CarController.GetAverageWheelSlip(CarController.PoweredAxles) * CarController.throttleInput_V;
        totalSlip = Mathf.Abs(totalSlip);
        totalSlip = 2f * Mathf.Pow(totalSlip, 2f);
        totalSlip = Mathf.Clamp01(totalSlip);

        float slipFactor = Mathf.Lerp(2.7f, 1f, totalSlip);

        if (equivalentEngineRPM > 0f) {

            // Launch gate: don't drag engine RPM down when the player is throttling and the
            // gear-tied wheel-RPM is below idle. At standstill in 1st gear the equivalent
            // engine RPM is well below minEngineRPM, so the Lerp would pull wantedRPM toward
            // 0 against the player's throttle input — suppressing the very torque they're
            // asking for and producing the "vehicle bogs from a stop" feel. Skipping the
            // pull only in this band keeps engine braking, shifts, and high-speed coupling
            // unchanged (the existing line-903 clutch>=0.95 check still gates shifts).
            bool launchGate = equivalentEngineRPM <= minEngineRPM
                && CarController.throttleInput_P > 0.5f;

            if (!launchGate) {

                float couplingStrength = (1f - clutchInput) * engineCouplingToWheelsRate * slipFactor;
                wantedEngineRPMRaw = Mathf.Lerp(wantedEngineRPMRaw, equivalentEngineRPM, couplingStrength * Time.fixedDeltaTime);

            }

        }

    }

    /// <summary>
    /// Sets engine torque multiplier (e.g., for nitrous systems).
    /// </summary>
    /// <param name="targetMultiplier">Torque multiplier to apply this frame.</param>
    public void SetTorqueMultiplier(float targetMultiplier) {

        multiplier = targetMultiplier;

    }

    /// <summary>
    /// Multiplies the current engine torque multiplier by the specified value.
    /// Useful for stacking multiple torque modifications (e.g., NOS + tuning + temperature effects).
    /// </summary>
    /// <param name="targetMultiplier">Multiplier value to apply to the current multiplier.</param>
    public void Multiply(float targetMultiplier) {

        multiplier *= targetMultiplier;

    }

    /// <summary>
    /// Outputs the calculated torque to connected components and invokes events.
    /// </summary>
    private void Output() {

        if (output == null)
            output = new RCCP_Output();

        output.NM = producedTorqueAsNM;
        outputEvent.Invoke(output);

    }

    /// <summary>
    /// Reloads and resets engine parameters when script is enabled or disabled.
    /// </summary>
    public void Reload() {

        engineStarting = false;
        engineRPM = 0f;
        wantedEngineRPMRaw = 0f;

        if (engineRunning) {

            wantedEngineRPMRaw = minEngineRPM;
            engineRPM = wantedEngineRPMRaw;

        }


        engineVelocity = 0f;
        fuelInput = 0f;
        idleInput = 0f;
        producedTorqueAsNM = 0f;
        multiplier = 1f;
        cutFuel = false;
        revLimiterTimer = 0f;

        if (turboCharged) {

            turboChargePsi = 0f;
            turboChargePsi_Old = 0f;
            turboBlowOut = false;

        }

        if (simulateEngineTemperature) {

            engineTemperature = ambientTemperature + 20f;

        }

        engineLoad = 0f;
        knockFactor = 0f;
        sensitiveEngineInertia = engineInertia;

    }

    /// <summary>
    /// Checks if autoCreateNMCurve is enabled and, if so, regenerates the engine torque curve (NMCurve)
    /// using minEngineRPM, peak-torque RPM (maximumTorqueAsNM), and maxEngineRPM.
    /// </summary>
    public void CheckAndCreateNMCurve() {

        // only proceed when auto-creation is requested
        if (!autoCreateNMCurve)
            return;

        // normalized torque at idle and redline — realistic engines produce
        // much less torque at the RPM extremes than at their peak
        float idleTorqueNormalized = 0.38f;
        float redlineTorqueNormalized = 0.5f;

        // intermediate keyframes for a realistic bell-curve shape
        float prePeakRPM = Mathf.Lerp(minEngineRPM, peakRPM, 0.55f);
        float postPeakRPM = Mathf.Lerp(peakRPM, maxEngineRPM, 0.45f);

        // build a 5-point curve: idle → rise → peak → fall → redline
        NMCurve = new AnimationCurve(
            new Keyframe(minEngineRPM, idleTorqueNormalized),
            new Keyframe(prePeakRPM, 0.82f),
            new Keyframe(peakRPM, 1f),
            new Keyframe(postPeakRPM, 0.82f),
            new Keyframe(maxEngineRPM, redlineTorqueNormalized)
        );

        // smooth tangents so the curve flows through intermediate keys instead of
        // pivoting on them (avoids the small shoulders at prePeakRPM / postPeakRPM)
        for (int i = 0; i < NMCurve.length; i++)
            NMCurve.SmoothTangents(i, 0f);

    }

    /// <summary>
    /// Fake engine braking: applies a physics-sized negative torque to driven wheels
    /// when the player is off-throttle and the clutch couples engine to wheels.
    ///
    /// Engine-side magnitude: T_e = coefficient * peakTorque * rpmFactor * (1 - throttle) * (1 - clutch).
    /// rpmFactor ramps linearly from 0 at engineBrakingMinRPMFactor*maxRPM to 1 at maxRPM.
    /// Wheel-side magnitude per driven wheel: T_w = T_e * gearRatio * finalDriveRatio / drivenWheelCount.
    /// </summary>
    private void NegativeFeedback() {

        if (!engineBraking || !engineRunning)
            return;

        if (CarController.Gearbox == null ||
            CarController.Differentials == null ||
            CarController.Clutch == null ||
            CarController.PoweredAxles == null) {
            return;
        }

        float clutchInput = GetEffectiveClutchInput();

        // Clutch fully open → drivetrain decoupled, no engine brake reaches the wheels.
        if (clutchInput >= 0.9f)
            return;

        // Gearbox not engaging drive (neutral / park) → no path from engine to wheels.
        if (CarController.Gearbox.gearInput <= 0f)
            return;

        float gearRatio = Mathf.Abs(CarController.Gearbox.CurrentGearRatio);

        if (gearRatio <= 0f)
            return;

        // Throttle fade: full strength at 0% throttle, fades out by engineBrakingThrottleCutoff.
        float throttle = CarController.throttleInput_P;
        float throttleFade = engineBrakingThrottleCutoff > 0f
            ? Mathf.Clamp01(1f - (throttle / engineBrakingThrottleCutoff))
            : (throttle <= 0f ? 1f : 0f);

        if (throttleFade <= 0f)
            return;

        // RPM fade: linear from minFactor*maxRPM up to maxRPM.
        float minRPM = engineBrakingMinRPMFactor * maxEngineRPM;
        float rpmFactor = maxEngineRPM > minRPM
            ? Mathf.Clamp01((engineRPM - minRPM) / (maxEngineRPM - minRPM))
            : 0f;

        if (rpmFactor <= 0f)
            return;

        // Engine-side brake torque (Nm at the crank).
        float engineBrakeNm = engineBrakingCoefficient * maximumTorqueAsNM * rpmFactor * throttleFade * (1f - clutchInput);

        // Aggressive downshifts can mechanically force the engine beyond redline.
        // engineRPM itself is clamped, so use wheel-equivalent engine RPM to detect that case.
        if (maxEngineRPM > 0f && overRevEngineBrakingMultiplier > 1f) {

            float equivalentEngineRPM = CarController.GetAverageDrivenWheelEngineRPM(gearRatio);
            float overRevFactor = Mathf.InverseLerp(maxEngineRPM, maxEngineRPM * 1.8f, equivalentEngineRPM);
            float overRevMultiplier = Mathf.Lerp(1f, overRevEngineBrakingMultiplier, overRevFactor);
            engineBrakeNm *= overRevMultiplier;

        }

        if (engineBrakeNm <= 0f)
            return;

        // Distribute across driven wheels weighted by each wheel's own differential ratio.
        // Total wheel-side torque = engineBrakeNm * gearRatio * finalDriveRatio summed,
        // but we want the engine-side torque accounted for once total — split by wheel count
        // and let each wheel see its own diff ratio applied.
        int drivenWheelCount = 0;

        for (int i = 0; i < CarController.PoweredAxles.Count; i++) {

            RCCP_Axle axle = CarController.PoweredAxles[i];

            if (axle == null)
                continue;

            if (axle.leftWheelCollider != null && axle.leftWheelCollider.isActiveAndEnabled)
                drivenWheelCount++;

            if (axle.rightWheelCollider != null && axle.rightWheelCollider.isActiveAndEnabled)
                drivenWheelCount++;

        }

        if (drivenWheelCount <= 0)
            return;

        float perWheelEngineNm = engineBrakeNm / drivenWheelCount;

        for (int i = 0; i < CarController.PoweredAxles.Count; i++) {

            RCCP_Axle axle = CarController.PoweredAxles[i];

            if (axle == null)
                continue;

            float diffRatio = 1f;

            for (int d = 0; d < CarController.Differentials.Length; d++) {

                RCCP_Differential diff = CarController.Differentials[d];

                if (diff != null && diff.connectedAxle == axle && diff.finalDriveRatio > 0f) {

                    diffRatio = diff.finalDriveRatio;
                    break;

                }

            }

            float wheelBrakeNm = perWheelEngineNm * gearRatio * diffRatio;

            if (axle.leftWheelCollider != null)
                axle.leftWheelCollider.AddEngineBrakeTorque(wheelBrakeNm);

            if (axle.rightWheelCollider != null)
                axle.rightWheelCollider.AddEngineBrakeTorque(wheelBrakeNm);

        }

    }

    /// <summary>
    /// Recomputes and applies per-differential finalDriveRatio
    /// such that the vehicle’s top speed matches maximumSpeed.
    /// </summary>
    public void UpdateMaximumSpeed() {

        // find the car controller
        RCCP_CarController controller = GetComponentInParent<RCCP_CarController>(true);
        if (controller == null)
            return;

        // find the gearbox
        RCCP_Gearbox gearbox = controller.GetComponentInChildren<RCCP_Gearbox>(true);
        if (gearbox == null || gearbox.gearRatios == null || gearbox.gearRatios.Length == 0)
            return;

        // gather all differentials that actually drive wheels
        List<RCCP_Differential> validDiffs = new List<RCCP_Differential>();
        float totalCircumference = 0f;

        foreach (RCCP_Differential diff in controller.GetComponentsInChildren<RCCP_Differential>(true)) {

            if (!diff.gameObject.activeSelf)
                continue;

            RCCP_Axle axle = diff.connectedAxle;
            if (axle == null)
                continue;

            float radius = controller.GetAverageWheelRadius(axle);
            totalCircumference += 2f * Mathf.PI * radius;
            validDiffs.Add(diff);
        }

        if (validDiffs.Count == 0)
            return;

        // average wheel circumference in meters
        float averageCircumference = totalCircumference / validDiffs.Count;

        float lastGearRatio = gearbox.gearRatios[^1];
        if (maximumSpeed > 0f && lastGearRatio > 0f) {
            // engineRPM → wheel rpm (at diff ratio=1) → km/h
            float k = maxEngineRPM / lastGearRatio
                      * averageCircumference
                      * 60f    // minutes per hour
                      / 1000f; // meters → kilometers

            // total diff ratio needed
            float requiredTotalRatio = k / maximumSpeed;

            foreach (RCCP_Differential diff in validDiffs) {

                if (!diff.gameObject.activeSelf)
                    continue;

                diff.finalDriveRatio = requiredTotalRatio;
            }
        }

        // propagate the first diff’s ratio into any speed-upgrader UI
        RCCP_VehicleUpgrade_Speed speedUpgrader = controller.GetComponentInChildren<RCCP_VehicleUpgrade_Speed>(true);

        if (speedUpgrader != null && speedUpgrader.defMaxSpeed <= 0f)
            speedUpgrader.defMaxSpeed = maximumSpeed;

    }

    /// <summary>
    /// Reads back the current finalDriveRatios and computes
    /// what the resulting top speed would be, storing in maximumSpeed.
    /// </summary>
    public void RetrieveMaximumSpeed() {

        // find the car controller
        RCCP_CarController controller = GetComponentInParent<RCCP_CarController>(true);
        if (controller == null)
            return;

        // find the gearbox
        RCCP_Gearbox gearbox = controller.GetComponentInChildren<RCCP_Gearbox>(true);
        if (gearbox == null || gearbox.gearRatios == null || gearbox.gearRatios.Length == 0)
            return;

        // gather all differentials that actually drive wheels
        List<RCCP_Differential> validDiffs = new List<RCCP_Differential>();
        float totalCircumference = 0f;

        foreach (RCCP_Differential diff in controller.GetComponentsInChildren<RCCP_Differential>(true)) {

            if (!diff.gameObject.activeSelf)
                continue;

            RCCP_Axle axle = diff.connectedAxle;
            if (axle == null)
                continue;

            float radius = controller.GetAverageWheelRadius(axle);
            totalCircumference += 2f * Mathf.PI * radius;
            validDiffs.Add(diff);
        }

        if (validDiffs.Count == 0)
            return;

        // average wheel circumference in meters
        float averageCircumference = totalCircumference / validDiffs.Count;

        // Average the final drive ratios across active differentials.
        // Correct for parallel-driven axles with similar finalDriveRatio (the common case
        // for shipped vehicles). For setups with mismatched per-axle ratios this returns
        // an approximation rather than the per-axle exact result.
        float totalFinalDriveRatio = 0f;
        foreach (RCCP_Differential diff in validDiffs) {

            if (!diff.gameObject.activeSelf)
                continue;

            totalFinalDriveRatio += diff.finalDriveRatio;
        }

        totalFinalDriveRatio /= (float)validDiffs.Count;

        // speed formula: (engineRPM / lastGearRatio / totalDiffRatio) * circumference * 60 / 1000
        float lastGearRatio = gearbox.gearRatios[^1];
        maximumSpeed = (maxEngineRPM / lastGearRatio / totalFinalDriveRatio)
                       * averageCircumference
                       * 60f   // minutes per hour
                       / 1000f;

        // store for legacy compatibility if needed
        maximumSpeed_Old = maximumSpeed;
    }


    private void Reset() {

        RetrieveMaximumSpeed();

    }

}

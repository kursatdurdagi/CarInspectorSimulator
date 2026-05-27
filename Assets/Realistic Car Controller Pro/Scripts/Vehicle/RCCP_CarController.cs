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
using System.Threading.Tasks;

/// <summary>
/// Main car controller of the vehicle. Manages and observes every component attached to the vehicle.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Main/RCCP Car Controller")]
[DefaultExecutionOrder(-10)]
public class RCCP_CarController : RCCP_MainComponent {

    /// <summary>
    /// When TRUE, the vehicle accepts player input from RCCP_InputManager (keyboard, mobile, gamepad).
    /// When FALSE, all player inputs are zeroed and the vehicle applies brakes automatically.
    /// Use this to enable/disable player control of the vehicle.
    /// </summary>
    [Tooltip("When TRUE, the vehicle accepts player input from RCCP_InputManager (keyboard, mobile, gamepad). When FALSE, all player inputs are zeroed and the vehicle applies brakes automatically. Use this to enable/disable player control of the vehicle.")]
    public bool canControl = true;

    /// <summary>
    /// When TRUE, this vehicle is being controlled externally (AI, scripted paths, replay system).
    /// The RCCP_Input component will use overridePlayerInputs mode and ignore RCCP_InputManager.
    /// AI vehicles set this to TRUE. Player vehicles should keep this FALSE.
    /// </summary>
    [Tooltip("When TRUE, this vehicle is being controlled externally (AI, scripted paths, replay system). The RCCP_Input component will use overridePlayerInputs mode and ignore RCCP_InputManager. AI vehicles set this to TRUE. Player vehicles should keep this FALSE.")]
    public bool externalControl = false;

    /// <summary>
    /// When TRUE, the selected behavior preset in RCCP_Settings will NOT be applied to this vehicle.
    /// Use this to exclude specific vehicles from global behavior changes (e.g., AI vehicles with custom tuning).
    /// </summary>
    [Tooltip("When TRUE, the selected behavior preset in RCCP_Settings will NOT be applied to this vehicle. Use this to exclude specific vehicles from global behavior changes.")]
    public bool ineffectiveBehavior = false;

    /// <summary>
    /// When TRUE, this vehicle uses its own behavior preset instead of the global preset from RCCP_Settings.
    /// This allows per-vehicle behavior configuration (e.g., one car can drift while others race).
    /// </summary>
    [Header("Per-Vehicle Behavior")]
    [Tooltip("When TRUE, this vehicle uses its own behavior preset instead of the global preset from RCCP_Settings. This allows per-vehicle behavior configuration.")]
    public bool useCustomBehavior = false;

    /// <summary>
    /// The behavior preset index to use for this vehicle. Only applies when useCustomBehavior is true.
    /// </summary>
    [Tooltip("The behavior preset index to use for this vehicle. Only applies when useCustomBehavior is true. Use RCCP.GetBehaviorIndexByName() to get index by name.")]
    [Range(-1, 20)]
    public int customBehaviorIndex = -1;

    #region STATS

    /// <summary>
    /// Current engine rpm.
    /// </summary>
    [Tooltip("Current engine RPM reported by the RCCP_Engine component.")]
    public float engineRPM = 0f;

    /// <summary>
    /// Minimum engine rpm.
    /// </summary>
    [Tooltip("Minimum engine RPM (idle speed) from the RCCP_Engine component.")]
    public float minEngineRPM = 800f;

    /// <summary>
    /// Maximum engine rpm.
    /// </summary>
    [Tooltip("Maximum engine RPM (redline) from the RCCP_Engine component.")]
    public float maxEngineRPM = 8000f;

    /// <summary>
    /// Current gear.
    /// </summary>
    [Tooltip("Current gear index from the gearbox (0 = first gear).")]
    public int currentGear = 0;

    /// <summary>
    /// Current gear ratio.
    /// </summary>
    [Tooltip("Gear ratio of the currently selected gear from the gearbox.")]
    public float currentGearRatio = 1f;

    /// <summary>
    /// Last gear ratio.
    /// </summary>
    [Tooltip("Gear ratio of the highest forward gear, used to calculate theoretical top speed.")]
    public float lastGearRatio = 1f;

    /// <summary>
    /// Differential ratio.
    /// </summary>
    [Tooltip("Average final drive ratio across all active differentials.")]
    public float differentialRatio = 1f;

    /// <summary>
    /// Speed of the vehicle.
    /// </summary>
    [Tooltip("Current vehicle speed in km/h (signed: positive = forward, negative = reverse).")]
    public float speed = 0f;

    /// <summary>
    /// Deprecated. Use <see cref="speed"/> or <see cref="absoluteSpeed"/> instead.
    /// </summary>
    [System.Obsolete("You can use ''speed'' or ''absoluteSpeed'' instead of 'physicalSpeed'.")]
    public float physicalSpeed {

        get {

            return speed;

        }

    }

    /// <summary>
    /// Absolute (unsigned) speed of the vehicle in km/h. Always returns a positive value regardless of direction.
    /// </summary>
    public float absoluteSpeed {

        get {

            return Mathf.Abs(speed);

        }

    }

    /// <summary>
    /// Wheel speed of the vehicle.
    /// </summary>
    [Tooltip("Vehicle speed in km/h derived from driven wheel RPM and radius.")]
    public float wheelRPM2Speed = 0f;

    /// <summary>
    /// Maximum speed of the vehicle related to engine rpm, gear ratio, differential ratio, and wheel diameter.
    /// </summary>
    [Tooltip("Theoretical top speed in km/h based on max RPM, top gear ratio, differential ratio, and wheel radius.")]
    public float maximumSpeed = 0f;

    /// <summary>
    /// RPM of the traction wheels.
    /// </summary>
    [Tooltip("Equivalent engine RPM calculated from driven wheel RPM, gear ratio, and differential ratio.")]
    public float tractionWheelRPM2EngineRPM = 0f;

    /// <summary>
    /// Target wheel speed for current gear.
    /// </summary>
    [Tooltip("Theoretical wheel speed in km/h for the current gear at the current engine RPM.")]
    public float targetWheelSpeedForCurrentGear = 0f;

    /// <summary>
    /// Produced engine torque.
    /// </summary>
    [Tooltip("Current engine output torque in Nm from the torque curve.")]
    public float producedEngineTorque = 0f;

    /// <summary>
    /// Produced gearbox torque.
    /// </summary>
    [Tooltip("Current gearbox output torque in Nm (engine torque multiplied by gear ratio).")]
    public float producedGearboxTorque = 0f;

    /// <summary>
    /// Produced differential torque.
    /// </summary>
    [Tooltip("Average output torque in Nm across all active differentials.")]
    public float producedDifferentialTorque = 0f;

    /// <summary>
    /// 1 = Forward, -1 = Reverse.
    /// </summary>
    [Tooltip("Drive direction: 1 = forward gear, -1 = reverse gear.")]
    public int direction = 1;

    /// <summary>
    /// Is engine starting now?
    /// </summary>
    [Tooltip("True while the engine starter motor is cranking.")]
    public bool engineStarting = false;

    /// <summary>
    /// Is engine running now?
    /// </summary>
    [Tooltip("True when the engine is running and producing torque.")]
    public bool engineRunning = false;

    /// <summary>
    /// Is gearbox shifting now?
    /// </summary>
    [Tooltip("True during a gear shift transition (throttle may be cut).")]
    public bool shiftingNow = false;

    /// <summary>
    /// Is gearbox at N gear now?
    /// </summary>
    [Tooltip("True when the gearbox is in neutral (no gear engaged).")]
    public bool NGearNow = false;

    /// <summary>
    /// Is reversing now?
    /// </summary>
    [Tooltip("True when the gearbox is in a reverse gear.")]
    public bool reversingNow = false;

    /// <summary>
    /// Current steer angle.
    /// </summary>
    [Tooltip("Current front axle steering angle in degrees (positive = right).")]
    public float steerAngle = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    [Tooltip("Fuel input from the engine component (0 = no fuel, 1 = full fuel).")]
    public float fuelInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    [Tooltip("Throttle input averaged from powered axles (component-side, not player input).")]
    public float throttleInput_V = 0f;

    /// <summary>
    /// Realized brake input averaged from braked axles. Derived from each axle's produced brake
    /// torque divided by maxBrakeTorque, so it reflects brake multipliers and Park-brake overrides
    /// (not just the player request).
    /// </summary>
    [Tooltip("Realized brake input averaged from braked axles (producedBrakeTorqueNM / maxBrakeTorque). Includes brake multiplier and Park-brake overrides.")]
    public float brakeInput_V = 0f;

    /// <summary>
    /// Realized steering input averaged from steered axles. Derived from each axle's current
    /// steerAngle divided by maxSteerAngle, so it reflects the slewed physical wheel angle rather
    /// than the player's target request.
    /// </summary>
    [Tooltip("Realized steering input averaged from steered axles (steerAngle / maxSteerAngle). Reflects slewed wheel angle, not target.")]
    public float steerInput_V = 0f;

    /// <summary>
    /// Realized handbrake input averaged from handbraked axles. Calculated from the current
    /// handbrake request, axle handbrake multipliers, and Park-brake overrides.
    /// </summary>
    [Tooltip("Realized handbrake input averaged from handbraked axles. Includes handbrake multiplier and Park-brake overrides.")]
    public float handbrakeInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    [Tooltip("Clutch input from the clutch component (0 = engaged, 1 = fully disengaged).")]
    public float clutchInput_V = 0f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    [Tooltip("Gear engagement input from the gearbox (0 = neutral, 1 = gear fully engaged).")]
    public float gearInput_V = 1f;

    /// <summary>
    /// Inputs of the vehicle. These values taken from the components, not player inputs.
    /// </summary>
    [Tooltip("Nitrous input from the NOS addon (0 = off, 1 = active).")]
    public float nosInput_V = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player throttle input after processing by RCCP_Input (0-1).")]
    public float throttleInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player brake input after processing by RCCP_Input (0-1).")]
    public float brakeInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player steering input after processing by RCCP_Input (-1 to 1).")]
    public float steerInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player handbrake input after processing by RCCP_Input (0-1).")]
    public float handbrakeInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player clutch input after processing by RCCP_Input (0-1).")]
    public float clutchInput_P = 0f;

    /// <summary>
    /// Inputs of the player. These values taken from the player inputs, not components.
    /// </summary>
    [Tooltip("Player nitrous input after processing by RCCP_Input (0-1).")]
    public float nosInput_P = 0f;

    /// <summary>
    /// Low beam headlights.
    /// </summary>
    [Tooltip("True when low beam headlights are on.")]
    public bool lowBeamLights = false;

    /// <summary>
    /// High beam headlights.
    /// </summary>
    [Tooltip("True when high beam headlights are on.")]
    public bool highBeamLights = false;

    /// <summary>
    /// Indicator lights to left side.
    /// </summary>
    [Tooltip("True when the left turn indicator is active.")]
    public bool indicatorsLeftLights = false;

    /// <summary>
    /// Indicator lights to right side.
    /// </summary>
    [Tooltip("True when the right turn indicator is active.")]
    public bool indicatorsRightLights = false;

    /// <summary>
    /// Indicator lights to all sides.
    /// </summary>
    [Tooltip("True when hazard lights (all indicators) are active.")]
    public bool indicatorsAllLights = false;

    #endregion

    /// <summary>
    /// Checks if at least one wheel from the AxleManager is grounded.
    /// </summary>
    public bool IsGrounded {

        get {

            bool grounded = false;

            if (AxleManager != null && AxleManager.Axles.Count >= 1) {

                for (int i = 0; i < AxleManager.Axles.Count; i++) {

                    if (AxleManager.Axles[i].isGrounded)
                        grounded = true;

                }

            }

            return grounded;

        }

    }

    /// <summary>
    /// Currently attached and connected trailer, or null if no trailer is connected.
    /// </summary>
    public RCCP_TrailerController ConnectedTrailer {

        get {

            if (!OtherAddonsManager)
                return null;

            if (!OtherAddonsManager.TrailAttacher)
                return null;

            if (!OtherAddonsManager.TrailAttacher.attachedTrailer)
                return null;

            if (!OtherAddonsManager.TrailAttacher.attachedTrailer.attached)
                return null;

            return OtherAddonsManager.TrailAttacher.attachedTrailer;

        }

    }

    /// <summary>
    /// Configures PhysX vehicle sub-stepping for this vehicle. Sub-stepping decouples
    /// wheel simulation from the fixed timestep so wheels use smaller internal steps
    /// for contact accuracy. ConfigureVehicleSubsteps is per-vehicle (per-Rigidbody),
    /// so one call on any WheelCollider attached to this vehicle propagates to all of
    /// its wheels — we just need a single valid wheel to invoke it on.
    ///
    /// The (speedThreshold, stepsBelow, stepsAbove) triple is selected by the active
    /// BehaviorType's <c>wheelSubstepProfile</c>; falls back to <c>Realistic</c> when no
    /// behavior is resolved (matches the V2.41.1 default). Re-applied on
    /// <c>RCCP_Events.OnBehaviorChanged</c> via <c>CheckBehaviorDelayed</c>.
    ///
    /// Profile values (threshold m/s | steps below | steps above) — biased toward
    /// stability over CPU savings:
    ///   Realistic   : 10 | 12 |  8   (default — road cars at moderate speeds)
    ///   Arcade      : 20 | 10 |  6   (arcade-feel handling)
    ///   OffRoad     : 10 | 14 | 10   (extra accuracy for bumpy / uneven terrain)
    ///   HighSpeed   : 30 | 22 | 16   (heavy substepping for high-force racing)
    /// </summary>
    private void ConfigureWheelSubsteps() {

        RCCP_WheelCollider[] wheels = AllWheelColliders;

        if (wheels == null)
            return;

        RCCP_Settings.BehaviorType behavior = GetVehicleBehaviorType();
        RCCP_WheelSubstepProfile profile = behavior != null ? behavior.wheelSubstepProfile : RCCP_WheelSubstepProfile.Realistic;

        GetWheelSubstepValues(profile, out float speedThreshold, out int stepsBelow, out int stepsAbove);

        for (int i = 0; i < wheels.Length; i++) {

            if (wheels[i] == null || wheels[i].WheelCollider == null)
                continue;

            wheels[i].WheelCollider.ConfigureVehicleSubsteps(speedThreshold, stepsBelow, stepsAbove);

            return;

        }

    }

    /// <summary>
    /// Maps a <see cref="RCCP_WheelSubstepProfile"/> to its
    /// (speedThreshold, stepsBelowThreshold, stepsAboveThreshold) triple.
    /// Unknown values fall back to <c>Realistic</c>.
    /// </summary>
    private static void GetWheelSubstepValues(
        RCCP_WheelSubstepProfile profile,
        out float speedThreshold,
        out int stepsBelowThreshold,
        out int stepsAboveThreshold) {

        switch (profile) {

            case RCCP_WheelSubstepProfile.Arcade:
                speedThreshold = 20f; stepsBelowThreshold = 10; stepsAboveThreshold = 6;
                break;

            case RCCP_WheelSubstepProfile.OffRoad:
                speedThreshold = 10f; stepsBelowThreshold = 14; stepsAboveThreshold = 10;
                break;

            case RCCP_WheelSubstepProfile.HighSpeed:
                speedThreshold = 30f; stepsBelowThreshold = 22; stepsAboveThreshold = 16;
                break;

            case RCCP_WheelSubstepProfile.Realistic:
            default:
                speedThreshold = 10f; stepsBelowThreshold = 12; stepsAboveThreshold = 8;
                break;

        }

    }

    /// <summary>
    /// Unity Start. Runs vehicle-wide physics setup that requires all wheels to be discoverable.
    /// Executes before wheel Start() thanks to DefaultExecutionOrder(-10).
    /// </summary>
    private void Start() {

        if (RCCPSettings.useFixedWheelColliders)
            ConfigureWheelSubsteps();

    }

    /// <summary>
    /// Called by Unity when the object becomes enabled and active.
    /// </summary>
    private void OnEnable() {

        //  Firing an event when a vehicle spawned.
        //  AI vehicles fire OnRCCPAISpawned, player vehicles fire OnRCCPSpawned.
        if (OtherAddonsManager != null) {

            if (OtherAddonsManager.AI == null)
                RCCP_Events.Event_OnRCCPSpawned(this);
            else
                RCCP_Events.Event_OnRCCPAISpawned(this);

        } else {

            RCCP_Events.Event_OnRCCPSpawned(this);

        }

        //  Listening for changes in behavior settings.
        RCCP_Events.OnBehaviorChanged += CheckBehavior;

        //  Checking if a global behavior should be applied to this vehicle.
        CheckBehavior();

        //  Making sure certain parameters are reset before usage.
        ResetVehicle();

    }

    /// <summary>
    /// Called by Unity once every physics step.
    /// Handles player and vehicle inputs, and then updates drivetrain logic.
    /// </summary>
    private void FixedUpdate() {

        //  Receiving player inputs from RCCP_InputManager.
        PlayerInputs();

        //  Receiving vehicle inputs from the attached components (e.g., axles).
        VehicleInputs();

        //  Updating drivetrain calculations based on input values.
        Drivetrain();

    }

    /// <summary>
    /// Main function that collects data from Engine, Gearbox, Differential, and Axles to calculate speed, torque, etc.
    /// </summary>
    private void Drivetrain() {

        //  Getting important variables from the engine.
        if (Engine) {

            engineStarting = Engine.engineStarting;
            engineRunning = Engine.engineRunning;
            engineRPM = Engine.engineRPM;
            minEngineRPM = Engine.minEngineRPM;
            maxEngineRPM = Engine.maxEngineRPM;

        }

        //  Getting important variables from the gearbox.
        if (Gearbox) {

            currentGear = Gearbox.currentGear;
            currentGearRatio = Gearbox.CurrentGearRatio;
            lastGearRatio = Gearbox.LastForwardGearRatio;

            if (Gearbox.IsInReverseGear)
                direction = -1;
            else
                direction = 1;

            shiftingNow = Gearbox.shiftingNow;
            reversingNow = Gearbox.IsInReverseGear ? true : false;
            NGearNow = Gearbox.currentGearState.gearState == RCCP_Gearbox.CurrentGearState.GearState.Neutral ? true : false;

        }

        differentialRatio = GetFinalDriveRatio();

        List<RCCP_Axle> poweredAxles = PoweredAxles;

        float slip = GetAverageWheelSlip(poweredAxles);
        float currentGearRatioMagnitude = Mathf.Abs(currentGearRatio);

        //  Calculating speed as km/h unit.
        speed = transform.InverseTransformDirection(Rigid.linearVelocity).z * 3.6f;

        //  Converting traction wheel rpm to engine rpm.
        tractionWheelRPM2EngineRPM = GetAverageDrivenWheelEngineRPM(currentGearRatioMagnitude) * (1f - clutchInput_V) * gearInput_V;

        //  Converting wheel rpm to speed as km/h unit.
        wheelRPM2Speed = GetAverageDrivenWheelSpeed();

        // If slip is moderate, reduce difference between wheel RPM speed and actual speed. 
        // If slip is very high, let the wheels spin freely.
        if (Mathf.Abs(slip) > 0f && Mathf.Abs(slip) < .15f) {

            float diff = wheelRPM2Speed - speed;
            wheelRPM2Speed -= Mathf.Lerp(0f, diff, Mathf.Abs(slip) * 10f);

        }

        //  Calculating target max speed for the current gear.
        targetWheelSpeedForCurrentGear = GetAverageDrivenWheelTargetSpeed(engineRPM, currentGearRatioMagnitude);

        //  Calculating max speed at last gear as km/h unit.
        maximumSpeed = GetAverageDrivenWheelTargetSpeed(maxEngineRPM, lastGearRatio);

        //  Produced torques.
        if (Engine)
            producedEngineTorque = Engine.producedTorqueAsNM;

        if (Gearbox)
            producedGearboxTorque = Gearbox.producedTorqueAsNM;

        producedDifferentialTorque = 0f;

        RCCP_Differential[] activeDifferentials = Differentials;

        if (activeDifferentials != null && activeDifferentials.Length > 0) {

            for (int i = 0; i < activeDifferentials.Length; i++) {

                if (!activeDifferentials[i].gameObject.activeSelf)
                    continue;

                producedDifferentialTorque += activeDifferentials[i].producedTorqueAsNM;

            }

        }

        if (activeDifferentials != null && activeDifferentials.Length > 0)
            producedDifferentialTorque /= (float)ActiveDifferentials;

    }

    /// <summary>
    /// Calculates the average final drive ratio across all active differentials.
    /// </summary>
    /// <returns>Average final drive ratio, or 0 if no active differentials exist.</returns>
    public float GetFinalDriveRatio() {

        float finalDriveRatio = 0f;
        int activeCount = 0;

        //  Getting important variables from the differential.
        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                if (Differentials[i] == null)
                    continue;

                if (Differentials[i].isActiveAndEnabled && Differentials[i].gameObject.activeSelf && Differentials[i].connectedAxle != null) {
                    finalDriveRatio += Differentials[i].finalDriveRatio;
                    activeCount++;
                }

            }

        }

        if (activeCount > 0)
            finalDriveRatio /= (float)activeCount;

        return finalDriveRatio;

    }

    /// <summary>
    /// Calculates the average final drive ratio connected to a single axle.
    /// </summary>
    /// <param name="axle">Axle to query.</param>
    /// <returns>Average final drive ratio for that axle, or 0 if none exists.</returns>
    public float GetFinalDriveRatio(RCCP_Axle axle) {

        if (axle == null)
            return 0f;

        float finalDriveRatio = 0f;
        int activeCount = 0;

        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                if (Differentials[i] == null)
                    continue;

                if (!Differentials[i].isActiveAndEnabled || !Differentials[i].gameObject.activeSelf || Differentials[i].connectedAxle != axle)
                    continue;

                finalDriveRatio += Differentials[i].finalDriveRatio;
                activeCount++;

            }

        }

        if (activeCount > 0)
            finalDriveRatio /= activeCount;

        return finalDriveRatio;

    }

    /// <summary>
    /// Returns the signed average RPM of all wheels connected to active differentials.
    /// </summary>
    /// <returns>Signed average driven-wheel RPM, or 0 if no valid wheels exist.</returns>
    public float GetAverageDrivenWheelRPMSigned() {

        float totalWheelRPM = 0f;
        int wheelCount = 0;

        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                RCCP_Differential differential = Differentials[i];

                if (differential == null || !differential.isActiveAndEnabled || !differential.gameObject.activeSelf || differential.connectedAxle == null)
                    continue;

                if (differential.connectedAxle.leftWheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider.enabled) {
                    totalWheelRPM += differential.connectedAxle.leftWheelCollider.WheelCollider.rpm;
                    wheelCount++;
                }

                if (differential.connectedAxle.rightWheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider.enabled) {
                    totalWheelRPM += differential.connectedAxle.rightWheelCollider.WheelCollider.rpm;
                    wheelCount++;
                }

            }

        }

        if (wheelCount <= 0)
            return 0f;

        return totalWheelRPM / wheelCount;

    }

    /// <summary>
    /// Converts driven wheel RPM back into equivalent engine RPM using each wheel's own differential ratio.
    /// </summary>
    /// <param name="gearRatioMagnitude">Absolute current gear ratio.</param>
    /// <returns>Average equivalent engine RPM across all driven wheels, or 0 if unavailable.</returns>
    public float GetAverageDrivenWheelEngineRPM(float gearRatioMagnitude) {

        if (gearRatioMagnitude <= 0f)
            return 0f;

        float totalEquivalentEngineRPM = 0f;
        int wheelCount = 0;

        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                RCCP_Differential differential = Differentials[i];

                if (differential == null || !differential.isActiveAndEnabled || !differential.gameObject.activeSelf || differential.connectedAxle == null || differential.finalDriveRatio <= 0f)
                    continue;

                if (differential.connectedAxle.leftWheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider.enabled) {
                    totalEquivalentEngineRPM += Mathf.Abs(differential.connectedAxle.leftWheelCollider.WheelCollider.rpm) * differential.finalDriveRatio * gearRatioMagnitude;
                    wheelCount++;
                }

                if (differential.connectedAxle.rightWheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider.enabled) {
                    totalEquivalentEngineRPM += Mathf.Abs(differential.connectedAxle.rightWheelCollider.WheelCollider.rpm) * differential.finalDriveRatio * gearRatioMagnitude;
                    wheelCount++;
                }

            }

        }

        if (wheelCount <= 0)
            return 0f;

        return totalEquivalentEngineRPM / wheelCount;

    }

    /// <summary>
    /// Calculates the average road speed of all wheels connected to active differentials.
    /// </summary>
    /// <returns>Average driven-wheel road speed in km/h, or 0 if no valid wheels exist.</returns>
    private float GetAverageDrivenWheelSpeed() {

        float totalWheelSpeed = 0f;
        int wheelCount = 0;

        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                RCCP_Differential differential = Differentials[i];

                if (differential == null || !differential.isActiveAndEnabled || !differential.gameObject.activeSelf || differential.connectedAxle == null)
                    continue;

                if (differential.connectedAxle.leftWheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider.enabled) {
                    totalWheelSpeed += (Mathf.Abs(differential.connectedAxle.leftWheelCollider.WheelCollider.rpm) * differential.connectedAxle.leftWheelCollider.WheelCollider.radius * Mathf.PI * 2f) * 60f / 1000f;
                    wheelCount++;
                }

                if (differential.connectedAxle.rightWheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider.enabled) {
                    totalWheelSpeed += (Mathf.Abs(differential.connectedAxle.rightWheelCollider.WheelCollider.rpm) * differential.connectedAxle.rightWheelCollider.WheelCollider.radius * Mathf.PI * 2f) * 60f / 1000f;
                    wheelCount++;
                }

            }

        }

        if (wheelCount <= 0)
            return 0f;

        return totalWheelSpeed / wheelCount;

    }

    /// <summary>
    /// Calculates the average theoretical road speed at a given engine RPM using each driven wheel's own radius and differential ratio.
    /// </summary>
    /// <param name="targetEngineRPM">Engine RPM to convert into wheel speed.</param>
    /// <param name="gearRatioMagnitude">Absolute gear ratio.</param>
    /// <returns>Average driven-wheel target speed in km/h, or 0 if unavailable.</returns>
    private float GetAverageDrivenWheelTargetSpeed(float targetEngineRPM, float gearRatioMagnitude) {

        if (targetEngineRPM <= 0f || gearRatioMagnitude <= 0f)
            return 0f;

        float totalWheelSpeed = 0f;
        int wheelCount = 0;

        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                RCCP_Differential differential = Differentials[i];

                if (differential == null || !differential.isActiveAndEnabled || !differential.gameObject.activeSelf || differential.connectedAxle == null || differential.finalDriveRatio <= 0f)
                    continue;

                float targetWheelRPM = targetEngineRPM / gearRatioMagnitude / differential.finalDriveRatio;

                if (differential.connectedAxle.leftWheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider && differential.connectedAxle.leftWheelCollider.WheelCollider.enabled) {
                    totalWheelSpeed += (targetWheelRPM * differential.connectedAxle.leftWheelCollider.WheelCollider.radius * Mathf.PI * 2f) * 60f / 1000f;
                    wheelCount++;
                }

                if (differential.connectedAxle.rightWheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider && differential.connectedAxle.rightWheelCollider.WheelCollider.enabled) {
                    totalWheelSpeed += (targetWheelRPM * differential.connectedAxle.rightWheelCollider.WheelCollider.radius * Mathf.PI * 2f) * 60f / 1000f;
                    wheelCount++;
                }

            }

        }

        if (wheelCount <= 0)
            return 0f;

        return totalWheelSpeed / wheelCount;

    }

    /// <summary>
    /// Gathers input values from vehicle components such as engine, axles, gearbox, etc.
    /// </summary>
    private void VehicleInputs() {

        //  Resetting all inputs to 0 before assigning them.
        fuelInput_V = 0f;
        throttleInput_V = 0f;
        brakeInput_V = 0f;
        steerInput_V = 0f;
        handbrakeInput_V = 0f;
        clutchInput_V = 0f;
        gearInput_V = 0f;
        nosInput_V = 0f;

        //  Fuel input of the engine.
        if (Engine)
            fuelInput_V = Engine.fuelInput;

        List<RCCP_Axle> poweredAxles = PoweredAxles;

        //  Throttle input.
        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++)
                throttleInput_V += poweredAxles[i].throttleInput;

            throttleInput_V /= (float)Mathf.Clamp(poweredAxles.Count, 1, 20);

        }

        List<RCCP_Axle> brakedAxles = BrakedAxles;

        //  Brake input — realized torque / axle max, so multipliers and Park-brake override show up.
        if (brakedAxles != null && brakedAxles.Count > 0) {

            int brakeSamples = 0;

            for (int i = 0; i < brakedAxles.Count; i++) {

                RCCP_Axle axle = brakedAxles[i];
                if (axle == null || axle.maxBrakeTorque <= Mathf.Epsilon)
                    continue;

                brakeInput_V += axle.producedBrakeTorqueNM / axle.maxBrakeTorque;
                brakeSamples++;

            }

            if (brakeSamples > 0)
                brakeInput_V /= brakeSamples;

        }

        List<RCCP_Axle> steeringAxles = SteeredAxles;

        //  Steer input — slewed physical wheel angle normalized, not the player target.
        if (steeringAxles != null && steeringAxles.Count > 0) {

            int steerSamples = 0;

            for (int i = 0; i < steeringAxles.Count; i++) {

                RCCP_Axle axle = steeringAxles[i];
                if (axle == null || axle.maxSteerAngle <= Mathf.Epsilon)
                    continue;

                steerInput_V += axle.steerAngle / axle.maxSteerAngle;
                steerSamples++;

            }

            if (steerSamples > 0)
                steerInput_V /= steerSamples;

        }

        List<RCCP_Axle> handbrakedAxles = HandbrakedAxles;

        //  Handbrake input — use the current player/control request instead of
        //  producedHandbrakeTorqueNM, because axles recalculate their output later
        //  in the fixed step. This keeps _V current for clutch and stability.
        if (handbrakedAxles != null && handbrakedAxles.Count > 0) {

            int handbrakeSamples = 0;
            bool parkBrakeActive = Gearbox &&
                Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP &&
                Gearbox.automaticGearSelector == RCCP_Gearbox.SemiAutomaticDNRPGear.P;

            for (int i = 0; i < handbrakedAxles.Count; i++) {

                RCCP_Axle axle = handbrakedAxles[i];
                if (axle == null || axle.maxBrakeTorque <= Mathf.Epsilon)
                    continue;

                float axleHandbrakeInput = parkBrakeActive ? 1f : Mathf.Clamp01(handbrakeInput_P);

                if (!parkBrakeActive && axleHandbrakeInput < .2f)
                    axleHandbrakeInput = 0f;

                axleHandbrakeInput *= Mathf.Max(0f, axle.handbrakeMultiplier);
                handbrakeInput_V += Mathf.Clamp01(axleHandbrakeInput);
                handbrakeSamples++;

            }

            if (handbrakeSamples > 0)
                handbrakeInput_V = Mathf.Clamp01(handbrakeInput_V / handbrakeSamples);

        }

        //  Clutch input.
        if (Clutch)
            clutchInput_V = Clutch.clutchInput;

        //  Gearbox input.
        if (Gearbox)
            gearInput_V = Gearbox.gearInput;

        //  Nos input.
        if (OtherAddonsManager && OtherAddonsManager.Nos)
            nosInput_V = OtherAddonsManager.Nos.nosInUse ? 1f : 0f;

        //  Lights input.
        if (Lights) {

            lowBeamLights = Lights.lowBeamHeadlights;
            highBeamLights = Lights.highBeamHeadlights;
            indicatorsLeftLights = Lights.indicatorsLeft;
            indicatorsRightLights = Lights.indicatorsRight;
            indicatorsAllLights = Lights.indicatorsAll;

        }

        if (FrontAxle)
            steerAngle = FrontAxle.steerAngle;

    }

    /// <summary>
    /// Gathers actual player inputs from RCCP_InputManager, unless the vehicle is not controllable.
    /// </summary>
    private void PlayerInputs() {

        //  Early out if vehicle has no input component.
        if (!Inputs)
            return;

        //  If canControl is false, force all inputs except handbrake to 0. 
        //  Optionally apply brake or handbrake if script settings require so.
        if (!canControl) {

            throttleInput_P = 0f;
            brakeInput_P = 1f;
            steerInput_P = 0f;
            handbrakeInput_P = 1f;
            clutchInput_P = 0f;
            nosInput_P = 0f;

            if (!Inputs.applyBrakeOnDisable)
                brakeInput_P = 0f;

            if (!Inputs.applyHandBrakeOnDisable)
                handbrakeInput_P = 0f;

            return;

        }

        //  Getting player inputs.
        throttleInput_P = Inputs.throttleInput;
        brakeInput_P = Inputs.brakeInput;
        steerInput_P = Inputs.steerInput;
        handbrakeInput_P = Inputs.handbrakeInput;
        clutchInput_P = Inputs.clutchInput;
        nosInput_P = Inputs.nosInput;

    }

    /// <summary>
    /// Sets controllable state of the vehicle.
    /// </summary>
    /// <param name="state">If true, the player can control this vehicle; if false, inputs are zeroed.</param>
    public void SetCanControl(bool state) {

        canControl = state;

    }

    /// <summary>
    /// Starts the engine if an RCCP_Engine component is attached.
    /// </summary>
    public void StartEngine() {

        if (Engine)
            Engine.engineRunning = true;

    }

    /// <summary>
    /// Kills the engine if an RCCP_Engine component is attached.
    /// </summary>
    public void KillEngine() {

        if (Engine)
            Engine.engineRunning = false;

    }

    /// <summary>
    /// Sets the engine on or off.
    /// </summary>
    /// <param name="state">If true, starts the engine; if false, kills it.</param>
    public void SetEngine(bool state) {

        if (Engine)
            Engine.engineRunning = state;

    }

    /// <summary>
    /// Triggers collision events for damage, particles, and audio components.
    /// </summary>
    /// <param name="collision">Collision data provided by Unity's physics engine.</param>
    public void OnCollisionEnter(Collision collision) {

        RCCP_Events.Event_OnRCCPCollision(this, collision);

        if (Damage)
            Damage.OnCollision(collision);

        if (Particles)
            Particles.OnCollision(collision);

        if (Audio)
            Audio.OnCollision(collision);

    }

    /// <summary>
    /// Triggers OnCollisionStay event for particles, if available.
    /// </summary>
    /// <param name="collision">Collision data provided by Unity's physics engine.</param>
    public void OnCollisionStay(Collision collision) {

        RCCP_Events.Event_OnRCCPCollision(this, collision);

        if (Particles)
            Particles.OnCollisionStay(collision);

    }

    /// <summary>
    /// Triggers OnCollisionExit event for particles, if available.
    /// </summary>
    /// <param name="collision">Collision data provided by Unity's physics engine.</param>
    public void OnCollisionExit(Collision collision) {

        if (Particles)
            Particles.OnCollisionExit(collision);

    }

    /// <summary>
    /// Called when a wheel is deflated, triggering a deflation sound if available.
    /// </summary>
    public void OnWheelDeflated() {

        if (Audio)
            Audio.DeflateWheel();

    }

    /// <summary>
    /// Called when a wheel is inflated, triggering an inflation sound if available.
    /// </summary>
    public void OnWheelInflated() {

        if (Audio)
            Audio.InflateWheel();

    }

    /// <summary>
    /// Gets the behavior type for this vehicle.
    /// Returns custom behavior if useCustomBehavior is true, otherwise returns global behavior from RCCP_Settings.
    /// </summary>
    public RCCP_Settings.BehaviorType GetVehicleBehaviorType() {

        // If this vehicle has a custom behavior selected, use that
        if (useCustomBehavior && customBehaviorIndex >= 0 &&
            RCCPSettings.behaviorTypes != null &&
            customBehaviorIndex < RCCPSettings.behaviorTypes.Length) {
            return RCCPSettings.behaviorTypes[customBehaviorIndex];
        }

        // Otherwise use the global behavior from RCCP_Settings
        return RCCPSettings.SelectedBehaviorType;

    }

    /// <summary>
    /// Checks and applies the selected behavior from RCCP_Settings if overrideBehavior is enabled,
    /// and if 'ineffectiveBehavior' is not set on this vehicle.
    /// Also applies custom per-vehicle behavior if useCustomBehavior is enabled.
    /// </summary>
    private void CheckBehavior() {

        if (ineffectiveBehavior)
            return;

        // If this vehicle has custom behavior, apply it regardless of global override setting
        if (useCustomBehavior && customBehaviorIndex >= 0) {

            RCCP_Settings.BehaviorType vehicleBehavior = GetVehicleBehaviorType();

            if (vehicleBehavior != null) {
                StartCoroutine(CheckBehaviorDelayed());
                return;
            }

        }

        // Otherwise, check if global behavior should be applied
        if (!RCCPSettings.overrideBehavior)
            return;

        if (RCCPSettings.SelectedBehaviorType == null)
            return;

        StartCoroutine(CheckBehaviorDelayed());

    }

    /// <summary>
    /// Waits a fixed frame before applying the selected behavior values to this vehicle's components.
    /// Uses GetVehicleBehaviorType() to get either custom or global behavior.
    /// </summary>
    private IEnumerator CheckBehaviorDelayed() {

        yield return new WaitForFixedUpdate();

        // Use vehicle-specific or global behavior
        RCCP_Settings.BehaviorType currentBehaviorType = GetVehicleBehaviorType();

        // Safety check
        if (currentBehaviorType == null)
            yield break;

        // Re-apply PhysX wheel sub-stepping so the new behavior's profile takes effect immediately.
        // Gated on useFixedWheelColliders (same gate as Start()) — when fixed wheel colliders are
        // disabled, RCCP doesn't author substep counts at all.
        if (RCCPSettings.useFixedWheelColliders)
            ConfigureWheelSubsteps();

        // Rigid settings
        Rigid.angularDamping = currentBehaviorType.angularDrag;

        // Stability settings
        if (Stability) {

            Stability.ABS = currentBehaviorType.ABS;
            Stability.ESP = currentBehaviorType.ESP;
            Stability.TCS = currentBehaviorType.TCS;

            Stability.steeringHelper = currentBehaviorType.steeringHelper;
            Stability.tractionHelper = currentBehaviorType.tractionHelper;
            Stability.angularDragHelper = currentBehaviorType.angularDragHelper;

            Stability.steerHelperStrength = Mathf.Clamp(Stability.steerHelperStrength, currentBehaviorType.steeringHelperStrengthMinimum, currentBehaviorType.steeringHelperStrengthMaximum);
            Stability.tractionHelperStrength = Mathf.Clamp(Stability.tractionHelperStrength, currentBehaviorType.tractionHelperStrengthMinimum, currentBehaviorType.tractionHelperStrengthMaximum);
            Stability.angularDragHelperStrength = Mathf.Clamp(Stability.angularDragHelperStrength, currentBehaviorType.angularDragHelperMinimum, currentBehaviorType.angularDragHelperMaximum);

            // Bug fix: propagate drift angle limiter settings that were previously missing
            Stability.driftAngleLimiter = currentBehaviorType.driftAngleLimiter;
            Stability.maxDriftAngle = currentBehaviorType.driftAngleLimit;
            Stability.driftAngleCorrectionFactor = currentBehaviorType.driftAngleCorrectionFactor;

            // Drift force parameters
            Stability.driftYawTorqueMultiplier = currentBehaviorType.driftYawTorqueMultiplier;
            Stability.driftForwardForceMultiplier = currentBehaviorType.driftForwardForceMultiplier;
            Stability.driftSidewaysForceMultiplier = currentBehaviorType.driftSidewaysForceMultiplier;
            Stability.driftMinSpeed = currentBehaviorType.driftMinSpeed;
            Stability.driftFullForceSpeed = currentBehaviorType.driftFullForceSpeed;
            Stability.driftThrottleYawFactor = currentBehaviorType.driftThrottleYawFactor;

            // Drift friction parameters
            Stability.driftRearSidewaysStiffnessMin = currentBehaviorType.driftRearSidewaysStiffnessMin;
            Stability.driftRearForwardStiffnessMin = currentBehaviorType.driftRearForwardStiffnessMin;
            Stability.driftFrontSidewaysStiffnessMin = currentBehaviorType.driftFrontSidewaysStiffnessMin;
            Stability.driftFrictionResponseSpeed = currentBehaviorType.driftFrictionResponseSpeed;
            Stability.driftFrictionRecoverySpeed = currentBehaviorType.driftFrictionRecoverySpeed;

            // Drift recovery parameters
            Stability.driftMaxAngularVelocity = currentBehaviorType.driftMaxAngularVelocity;
            Stability.driftCounterSteerRecoveryBoost = currentBehaviorType.driftCounterSteerRecoveryBoost;
            Stability.driftMomentumMaintenanceForce = currentBehaviorType.driftMomentumMaintenanceForce;
            Stability.driftForceSmoothing = currentBehaviorType.driftForceSmoothing;

        }

        // Input settings
        if (Inputs) {

            Inputs.steeringCurve = currentBehaviorType.steeringCurve;
            Inputs.steeringLimiter = currentBehaviorType.limitSteering;
            Inputs.counterSteering = currentBehaviorType.counterSteering;
            Inputs.counterSteerFactor = Mathf.Clamp(Inputs.counterSteerFactor, currentBehaviorType.counterSteeringMinimum, currentBehaviorType.counterSteeringMaximum);

            Inputs.ResetInputs();

        }

        // Axle settings
        if (AxleManager != null && AxleManager.Axles.Count > 1) {

            for (int i = 0; i < AxleManager.Axles.Count; i++) {

                RCCP_Axle axle = AxleManager.Axles[i];

                if (axle == null)
                    continue;

                axle.antirollForce = Mathf.Clamp(axle.antirollForce, currentBehaviorType.antiRollMinimum, Mathf.Infinity);
                axle.steerSpeed = Mathf.Clamp(axle.steerSpeed, currentBehaviorType.steeringSpeedMinimum, currentBehaviorType.steeringSpeedMaximum);

                if (axle.leftWheelCollider) {

                    axle.leftWheelCollider.driftMode = currentBehaviorType.driftMode;

                    if (axle.leftWheelCollider.transform.localPosition.z > 0) {

                        axle.leftWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_F, currentBehaviorType.forwardExtremumValue_F, currentBehaviorType.forwardAsymptoteSlip_F, currentBehaviorType.forwardAsymptoteValue_F);
                        axle.leftWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_F, currentBehaviorType.sidewaysExtremumValue_F, currentBehaviorType.sidewaysAsymptoteSlip_F, currentBehaviorType.sidewaysAsymptoteValue_F);

                    } else {

                        axle.leftWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_R, currentBehaviorType.forwardExtremumValue_R, currentBehaviorType.forwardAsymptoteSlip_R, currentBehaviorType.forwardAsymptoteValue_R);
                        axle.leftWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_R, currentBehaviorType.sidewaysExtremumValue_R, currentBehaviorType.sidewaysAsymptoteSlip_R, currentBehaviorType.sidewaysAsymptoteValue_R);

                    }

                }

                if (axle.rightWheelCollider) {

                    axle.rightWheelCollider.driftMode = currentBehaviorType.driftMode;

                    if (axle.rightWheelCollider.transform.localPosition.z > 0) {

                        axle.rightWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_F, currentBehaviorType.forwardExtremumValue_F, currentBehaviorType.forwardAsymptoteSlip_F, currentBehaviorType.forwardAsymptoteValue_F);
                        axle.rightWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_F, currentBehaviorType.sidewaysExtremumValue_F, currentBehaviorType.sidewaysAsymptoteSlip_F, currentBehaviorType.sidewaysAsymptoteValue_F);

                    } else {

                        axle.rightWheelCollider.SetFrictionCurvesForward(currentBehaviorType.forwardExtremumSlip_R, currentBehaviorType.forwardExtremumValue_R, currentBehaviorType.forwardAsymptoteSlip_R, currentBehaviorType.forwardAsymptoteValue_R);
                        axle.rightWheelCollider.SetFrictionCurvesSideways(currentBehaviorType.sidewaysExtremumSlip_R, currentBehaviorType.sidewaysExtremumValue_R, currentBehaviorType.sidewaysAsymptoteSlip_R, currentBehaviorType.sidewaysAsymptoteValue_R);

                    }

                }

            }

        }

        // Suspension settings.
        // Behavior presets no longer hold absolute spring/damper values — they multiply each wheel's authored base values.
        // This preserves per-vehicle tuning (a sedan's 40k spring and a truck's 180k spring stay correct) while letting a single
        // preset scale the feel — "Racing 1.4x" means stiffer-than-stock regardless of vehicle mass.
        if (AxleManager != null) {

            float springMul = currentBehaviorType.suspensionSpringMultiplier;
            float damperMul = currentBehaviorType.suspensionDamperMultiplier;

            for (int i = 0; i < AxleManager.Axles.Count; i++) {

                RCCP_Axle axle = AxleManager.Axles[i];

                if (axle == null)
                    continue;

                if (axle.leftWheelCollider)
                    axle.leftWheelCollider.SetSuspensionForces(axle.leftWheelCollider.BaseSuspensionSpring * springMul, axle.leftWheelCollider.BaseSuspensionDamper * damperMul);

                if (axle.rightWheelCollider)
                    axle.rightWheelCollider.SetSuspensionForces(axle.rightWheelCollider.BaseSuspensionSpring * springMul, axle.rightWheelCollider.BaseSuspensionDamper * damperMul);

            }

        }

        // Gearbox settings
        if (Gearbox) {

            Gearbox.shiftThreshold = currentBehaviorType.gearShiftingThreshold;
            Gearbox.shiftingTime = Mathf.Clamp(Gearbox.shiftingTime, currentBehaviorType.gearShiftingDelayMinimum, currentBehaviorType.gearShiftingDelayMaximum);

        }

        // Differential settings
        if (Differentials != null && Differentials.Length > 0) {

            for (int i = 0; i < Differentials.Length; i++) {

                if (Differentials[i] == null)
                    continue;

                Differentials[i].differentialType = currentBehaviorType.differentialType;

            }

        }

    }

    /// <summary>
    /// Checks if the vehicle is currently drivable by the player (not externally controlled, and 'canControl' is true).
    /// </summary>
    /// <returns>True if canControl is true and externalControl is false.</returns>
    public bool IsControllableByPlayer() {

        if (!canControl || externalControl)
            return false;

        return true;

    }

    /// <summary>
    /// Calculates the average wheel radius across all wheels on the given powered axles.
    /// </summary>
    /// <param name="poweredAxles">List of powered axles to sample wheel radii from.</param>
    /// <returns>Average wheel radius in meters, or 0 if no valid wheels exist.</returns>
    public float GetAverageWheelRadius(List<RCCP_Axle> poweredAxles) {

        //  Calculating average traction wheel radius.
        float averagePowerWheelRadius = 0f;
        int wheelCount = 0;

        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++) {

                if (poweredAxles[i].leftWheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelRadius += poweredAxles[i].leftWheelCollider.WheelCollider.radius;
                    wheelCount++;
                }

                if (poweredAxles[i].rightWheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelRadius += poweredAxles[i].rightWheelCollider.WheelCollider.radius;
                    wheelCount++;
                }

            }

            if (wheelCount > 0)
                averagePowerWheelRadius /= wheelCount;

        }

        return averagePowerWheelRadius;

    }

    /// <summary>
    /// Calculates the average wheel radius for a single axle.
    /// </summary>
    /// <param name="poweredAxle">Axle to sample wheel radii from.</param>
    /// <returns>Average wheel radius in meters, or 0 if no valid wheels exist.</returns>
    public float GetAverageWheelRadius(RCCP_Axle poweredAxle) {

        //  Calculating average traction wheel radius.
        float averagePowerWheelRadius = 0f;
        int wheelCount = 0;

        if (poweredAxle.leftWheelCollider && poweredAxle.leftWheelCollider.WheelCollider && poweredAxle.leftWheelCollider.WheelCollider.enabled) {
            averagePowerWheelRadius += poweredAxle.leftWheelCollider.WheelCollider.radius;
            wheelCount++;
        }

        if (poweredAxle.rightWheelCollider && poweredAxle.rightWheelCollider.WheelCollider && poweredAxle.rightWheelCollider.WheelCollider.enabled) {
            averagePowerWheelRadius += poweredAxle.rightWheelCollider.WheelCollider.radius;
            wheelCount++;
        }

        if (wheelCount <= 0)
            return 0f;

        return averagePowerWheelRadius / wheelCount;

    }

    /// <summary>
    /// Calculates the average absolute wheel RPM across all wheels on the given powered axles.
    /// </summary>
    /// <param name="poweredAxles">List of powered axles to sample wheel RPMs from.</param>
    /// <returns>Average absolute wheel RPM, or 0 if no valid wheels exist.</returns>
    public float GetAverageWheelRPM(List<RCCP_Axle> poweredAxles) {

        //  Calculating average traction wheel rpm.
        float averagePowerWheelRPM = 0f;
        int wheelCount = 0;

        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++) {

                if (poweredAxles[i].leftWheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelRPM += Mathf.Abs(poweredAxles[i].leftWheelCollider.WheelCollider.rpm);
                    wheelCount++;
                }

                if (poweredAxles[i].rightWheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelRPM += Mathf.Abs(poweredAxles[i].rightWheelCollider.WheelCollider.rpm);
                    wheelCount++;
                }

            }

            if (wheelCount > 0)
                averagePowerWheelRPM /= wheelCount;

        }

        return averagePowerWheelRPM;

    }

    /// <summary>
    /// Calculates the average forward wheel slip across all wheels on the given powered axles.
    /// </summary>
    /// <param name="poweredAxles">List of powered axles to sample wheel slip from.</param>
    /// <returns>Average forward wheel slip value, or 0 if slip is negligible.</returns>
    public float GetAverageWheelSlip(List<RCCP_Axle> poweredAxles) {

        //  Calculating average slip of the traction wheels.
        float averagePowerWheelSlip = 0f;
        int wheelCount = 0;

        if (poweredAxles != null && poweredAxles.Count > 0) {

            for (int i = 0; i < poweredAxles.Count; i++) {

                if (poweredAxles[i].leftWheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider && poweredAxles[i].leftWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelSlip += poweredAxles[i].leftWheelCollider.ForwardSlip;
                    wheelCount++;
                }

                if (poweredAxles[i].rightWheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider && poweredAxles[i].rightWheelCollider.WheelCollider.enabled) {
                    averagePowerWheelSlip += poweredAxles[i].rightWheelCollider.ForwardSlip;
                    wheelCount++;
                }

            }

            if (wheelCount > 0)
                averagePowerWheelSlip /= wheelCount;

            if (Mathf.Abs(averagePowerWheelSlip) < .1f)
                averagePowerWheelSlip = 0f;

        }

        return averagePowerWheelSlip;

    }

    /// <summary>
    /// Called by Unity when the object is disabled or destroyed.
    /// Fires an event for vehicle destruction, and unsubscribes from the behavior-changed event.
    /// </summary>
    private void OnDisable() {

        //  Firing an event when disabling / destroying the vehicle.
        //  AI vehicles fire OnRCCPAIDestroyed, player vehicles fire OnRCCPDestroyed.
        if (OtherAddonsManager != null) {

            if (OtherAddonsManager.AI == null)
                RCCP_Events.Event_OnRCCPDestroyed(this);
            else
                RCCP_Events.Event_OnRCCPAIDestroyed(this);

        } else {

            RCCP_Events.Event_OnRCCPDestroyed(this);

        }

        RCCP_Events.OnBehaviorChanged -= CheckBehavior;

    }

}

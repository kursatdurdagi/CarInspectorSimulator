//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI telemetry for info.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP Telemetry")]
public class RCCP_Telemetry : RCCP_GenericComponent {

    /// <summary>
    /// Main car controller.
    /// </summary>
    private RCCP_CarController carController;

    /// <summary>
    /// Holds UI Text references for displaying telemetry data of a single wheel.
    /// </summary>
    [System.Serializable]
    public class WheelInfo {

        /// <summary>
        /// Text displaying the wheel collider GameObject name.
        /// </summary>
        [Tooltip("Text displaying the wheel collider GameObject name.")]
        public Text wheelName;

        /// <summary>
        /// Text displaying the wheel's current RPM.
        /// </summary>
        [Tooltip("Text displaying the wheel's current RPM.")]
        public Text RPM_Wheel;

        /// <summary>
        /// Text displaying the motor torque applied to the wheel.
        /// </summary>
        [Tooltip("Text displaying the motor torque applied to the wheel.")]
        public Text Torque_Wheel;

        /// <summary>
        /// Text displaying the brake torque applied to the wheel.
        /// </summary>
        [Tooltip("Text displaying the brake torque applied to the wheel.")]
        public Text Brake_Wheel;

        /// <summary>
        /// Text displaying the suspension bump force on the wheel.
        /// </summary>
        [Tooltip("Text displaying the suspension bump force on the wheel.")]
        public Text Force_Wheel;

        /// <summary>
        /// Text displaying the wheel's current steering angle.
        /// </summary>
        [Tooltip("Text displaying the wheel's current steering angle.")]
        public Text Angle_Wheel;

        /// <summary>
        /// Text displaying the wheel's sideways slip value.
        /// </summary>
        [Tooltip("Text displaying the wheel's sideways slip value.")]
        public Text Sideways_Wheel;

        /// <summary>
        /// Text displaying the wheel's forward slip value.
        /// </summary>
        [Tooltip("Text displaying the wheel's forward slip value.")]
        public Text Forward_Wheel;

        /// <summary>
        /// Text displaying the name of the collider the wheel is contacting.
        /// </summary>
        [Tooltip("Text displaying the ground collider the wheel is contacting.")]
        public Text GroundHit_Wheel;

    }

    /// <summary>
    /// All wheel info with custom class.
    /// </summary>
    [Tooltip("Per-wheel telemetry UI panels (expects at least 4 entries).")]
    [Space()]
    public WheelInfo[] wheelInfos;

    /// <summary>
    /// Text displaying Anti-lock Braking System engagement status.
    /// </summary>
    [Tooltip("Text showing ABS engagement status.")]
    [Space()]
    public Text ABS;

    /// <summary>
    /// Text displaying Electronic Stability Program engagement status.
    /// </summary>
    [Tooltip("Text showing ESP engagement status.")]
    public Text ESP;

    /// <summary>
    /// Text displaying Traction Control System engagement status.
    /// </summary>
    [Tooltip("Text showing TCS engagement status.")]
    public Text TCS;

    /// <summary>
    /// Wheel speec text.
    /// </summary>
    [Tooltip("Text showing average wheel speed in km/h.")]
    [Space()]
    public Text wheelSpeed;

    /// <summary>
    /// Vehicle speed text.
    /// </summary>
    [Tooltip("Text showing the vehicle's physical speed.")]
    public Text physicalSpeed;

    /// <summary>
    /// Engine rpm text.
    /// </summary>
    [Tooltip("Text showing current engine RPM.")]
    public Text engineRPM;

    /// <summary>
    /// Current gear text.
    /// </summary>
    [Tooltip("Text showing current gear (number, N, or R).")]
    public Text gear;

    /// <summary>
    /// Differential final drive torque text.
    /// </summary>
    [Tooltip("Text showing differential final drive torque output.")]
    public Text finalTorque;

    /// <summary>
    /// Can Control state of the car controller.
    /// </summary>
    [Tooltip("Text showing whether the vehicle is controllable by the player.")]
    public Text controllable;

    /// <summary>
    /// Player throttle input.
    /// </summary>
    [Tooltip("Text showing player throttle input value.")]
    [Space()]
    public Text throttle_P;

    /// <summary>
    /// Player steer input.
    /// </summary>
    [Tooltip("Text showing player steering input value.")]
    public Text steer_P;

    /// <summary>
    /// Player brake input.
    /// </summary>
    [Tooltip("Text showing player brake input value.")]
    public Text brake_P;

    /// <summary>
    /// Player handbrake input.
    /// </summary>
    [Tooltip("Text showing player handbrake input value.")]
    public Text handbrake_P;

    /// <summary>
    /// Player clutch input.
    /// </summary>
    [Tooltip("Text showing player clutch input value.")]
    public Text clutch_P;

    /// <summary>
    /// Vehicle throttle input.
    /// </summary>
    [Tooltip("Text showing final vehicle throttle input value.")]
    [Space()]
    public Text throttle_V;

    /// <summary>
    /// Vehicle steer input.
    /// </summary>
    [Tooltip("Text showing final vehicle steering input value.")]
    public Text steer_V;

    /// <summary>
    /// Vehicle brake input.
    /// </summary>
    [Tooltip("Text showing final vehicle brake input value.")]
    public Text brake_V;

    /// <summary>
    /// Vehicle handbrake input.
    /// </summary>
    [Tooltip("Text showing final vehicle handbrake input value.")]
    public Text handbrake_V;

    /// <summary>
    /// Vehicle clutch input.
    /// </summary>
    [Tooltip("Text showing final vehicle clutch input value.")]
    public Text clutch_V;

    private void Update() {

        //  Getting active player car controller on the scene.
        carController = RCCPSceneManager.activePlayerVehicle;

        //  If no active player car found, return.
        if (!carController)
            return;

        //  If vehicle has wheelcolliders, assign wheel texts.
        if (carController.AllWheelColliders != null && carController.AllWheelColliders.Length >= 1) {

            //  Telemetry has four panels. Even if vehicle has over four wheels, only four panel will be visible.
            for (int i = 0; i < 4; i++) {

                RCCP_WheelCollider wheelCollider_RCCP = carController.AllWheelColliders[i];

                wheelInfos[i].wheelName.text = "Name: " + wheelCollider_RCCP.WheelCollider.name;
                wheelInfos[i].RPM_Wheel.text = "RPM: " + wheelCollider_RCCP.WheelCollider.rpm.ToString("F0");
                wheelInfos[i].Torque_Wheel.text = "Torque: " + wheelCollider_RCCP.WheelCollider.motorTorque.ToString("F0");
                wheelInfos[i].Brake_Wheel.text = "Brake: " + wheelCollider_RCCP.WheelCollider.brakeTorque.ToString("F0");
                wheelInfos[i].Force_Wheel.text = "Force: " + wheelCollider_RCCP.bumpForce.ToString("F0");
                wheelInfos[i].Angle_Wheel.text = "Angle: " + wheelCollider_RCCP.WheelCollider.steerAngle.ToString("F0");
                wheelInfos[i].Sideways_Wheel.text = "Slip_Sd: " + wheelCollider_RCCP.SidewaysSlip.ToString("F");
                wheelInfos[i].Forward_Wheel.text = "Slip_Fwd: " + wheelCollider_RCCP.ForwardSlip.ToString("F");

                if (wheelCollider_RCCP.wheelHit.collider != null)
                    wheelInfos[i].GroundHit_Wheel.text = "Hit: " + (wheelCollider_RCCP.WheelCollider.isGrounded ? wheelCollider_RCCP.wheelHit.collider.name : "");

            }

        }

        if (carController.Stability) {

            ABS.text = "ABS: " + (carController.Stability.ABSEngaged ? "Engaged" : "Not Engaged");
            ESP.text = "ESP: " + (carController.Stability.ESPEngaged ? "Engaged" : "Not Engaged");
            TCS.text = "TCS: " + (carController.Stability.TCSEngaged ? "Engaged" : "Not Engaged");

        } else {

            ABS.text = "ABS: Not Equipped";
            ESP.text = "ESP: Not Equipped";
            TCS.text = "TCS: Not Equipped";

        }

        wheelSpeed.text = "Wheel Speed Average: " + carController.wheelRPM2Speed.ToString("F0");
        physicalSpeed.text = "Speed: " + carController.speed.ToString("F0");
        engineRPM.text = "Engine RPM: " + carController.engineRPM.ToString("F0");
        finalTorque.text = "Final Torque: " + carController.producedDifferentialTorque.ToString("F0");

        if (carController.direction == 1) {

            if (!carController.NGearNow)
                gear.text = "Gear: " + (carController.currentGear + 1).ToString("F0");
            else
                gear.text = "Gear: " + "N";

        } else {

            gear.text = "Gear: " + "R";

        }

        controllable.text = "Controllable: " + (carController.IsControllableByPlayer() ? "True" : "False");

        throttle_P.text = "Player Throttle: " + carController.throttleInput_P.ToString("F2");
        steer_P.text = "Player Steer: " + carController.steerInput_P.ToString("F2");
        brake_P.text = "Player Brake: " + carController.brakeInput_P.ToString("F2");
        handbrake_P.text = "Player Handbrake: " + carController.handbrakeInput_P.ToString("F2");
        clutch_P.text = "Player Clutch: " + carController.clutchInput_P.ToString("F2");

        throttle_V.text = "Vehicle Throttle: " + carController.throttleInput_V.ToString("F2");
        steer_V.text = "Vehicle Steer: " + carController.steerInput_V.ToString("F2");
        brake_V.text = "Vehicle Brake: " + carController.brakeInput_V.ToString("F2");
        handbrake_V.text = "Vehicle Handbrake: " + carController.handbrakeInput_V.ToString("F2");
        clutch_V.text = "Vehicle Clutch: " + carController.clutchInput_V.ToString("F2");

    }

}

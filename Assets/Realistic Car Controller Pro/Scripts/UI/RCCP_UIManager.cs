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
using TMPro;

/// <summary>
/// RCCP UI Canvas that manages the event systems, panels, gauges, images and texts related to the vehicle and player.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Manager")]
public class RCCP_UIManager : RCCP_UIComponent {

    private static RCCP_UIManager instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static RCCP_UIManager Instance {

        get {

#if !UNITY_2022_1_OR_NEWER
            if (instance == null)
                instance = FindObjectOfType<RCCP_UIManager>();
#else
            if (instance == null)
                instance = FindAnyObjectByType<RCCP_UIManager>();
#endif

            return instance;

        }

    }

    /// <summary>
    /// Main car controller.
    /// </summary>
    [Tooltip("Currently active car controller providing telemetry data to the UI.")]
    public RCCP_CarController carController;

    [Header("Panels")]
    /// <summary>
    /// Dashboard panel displaying speed, RPM, gear, and vehicle indicator icons.
    /// </summary>
    [Tooltip("Dashboard panel")] public GameObject dashboard;
    /// <summary>
    /// Panel containing mobile touch control elements (throttle, brake, steering).
    /// </summary>
    [Tooltip("Mobile controllers panel")] public GameObject mobileControllers;
    /// <summary>
    /// Panel for vehicle customization options (paint, wheels, upgrades).
    /// </summary>
    [Tooltip("Customization panel")] public GameObject customization;
    /// <summary>
    /// Settings panel for quality, audio, and control configuration.
    /// </summary>
    [Tooltip("Settings panel")] public GameObject settings;
    /// <summary>
    /// Panel for rebinding keyboard and gamepad input mappings.
    /// </summary>
    [Tooltip("Rebind inputs panel")] public GameObject rebindInputs;

    /// <summary>
    /// Vehicle spawn panel showing available demo vehicles.
    /// </summary>
    [Tooltip("Spawn vehicles panel with demo vehicles")] public GameObject spawnVehicles;
    /// <summary>
    /// Vehicle spawn panel showing the prototype vehicle.
    /// </summary>
    [Tooltip("Spawn vehicles panel with prototype vehicle")] public GameObject spawnPrototypeVehicles;

    [Header("Gear Selectors")]
    /// <summary>
    /// Automatic DNRP (Drive/Neutral/Reverse/Park) gear selector UI element.
    /// </summary>
    [Tooltip("Automatic DNRP Gear Selector")] public GameObject dnrp;
    /// <summary>
    /// Manual gear shift up button UI element.
    /// </summary>
    [Tooltip("Manual Gear Up Selector")] public GameObject gearUp;
    /// <summary>
    /// Manual gear shift down button UI element.
    /// </summary>
    [Tooltip("Manual Gear Down Selector")] public GameObject gearDown;

    [System.Serializable]
    /// <summary>
    /// Configurable analog gauge needle that rotates based on a vehicle value such as RPM or speed.
    /// </summary>
    public class SpeedOMeter {

        /// <summary>
        /// Needle gameobject.
        /// </summary>
        [Tooltip("GameObject representing the physical gauge needle that rotates.")]
        public GameObject needle;

        /// <summary>
        /// Axis around which the gauge needle rotates.
        /// </summary>
        public enum TurnAxis {

            /// <summary>
            /// Rotate around the local X axis.
            /// </summary>
            X,

            /// <summary>
            /// Rotate around the local Y axis.
            /// </summary>
            Y,

            /// <summary>
            /// Rotate around the local Z axis.
            /// </summary>
            Z

        }
        [Tooltip("Local axis around which the needle rotates.")]
        public TurnAxis turnAxis = TurnAxis.Z;

        /// <summary>
        /// Turn multiplier.
        /// </summary>
        [Tooltip("Degrees of rotation applied per unit of input value.")]
        public float multiplierRotation = -0.0245f;

        /// <summary>
        /// Default rotation of the needle.
        /// </summary>
        private float defRotation = -1f;

        /// <summary>
        /// Current rotation of the needle.
        /// </summary>
        private float currentRotation = 0f;

        /// <summary>
        /// Input value between 0f and 1f.
        /// </summary>
        private float input = 0f;

        /// <summary>
        /// Operates the needle with given input.
        /// </summary>
        /// <param name="_input">Normalized input value (e.g., engine RPM) that drives the needle rotation.</param>
        public void Operate(float _input) {

            //  Taking default rotation of the needle.
            if (defRotation == -1f) {

                switch (turnAxis) {

                    case TurnAxis.X:
                        defRotation = needle.transform.localEulerAngles.x;
                        break;

                    case TurnAxis.Y:
                        defRotation = needle.transform.localEulerAngles.y;
                        break;

                    case TurnAxis.Z:
                        defRotation = needle.transform.localEulerAngles.z;
                        break;

                }

            }

            //  Input.
            input = _input;

            //  Current rotation of the needle.
            currentRotation = defRotation + (input * multiplierRotation);

            //  And turning the needle.
            switch (turnAxis) {

                case TurnAxis.X:
                    needle.transform.localEulerAngles = new Vector3(currentRotation, needle.transform.localEulerAngles.y, needle.transform.localEulerAngles.z);
                    break;

                case TurnAxis.Y:
                    needle.transform.localEulerAngles = new Vector3(needle.transform.localEulerAngles.x, currentRotation, needle.transform.localEulerAngles.z);
                    break;

                case TurnAxis.Z:
                    needle.transform.localEulerAngles = new Vector3(needle.transform.localEulerAngles.x, needle.transform.localEulerAngles.y, currentRotation);
                    break;

            }

        }

    }

    /// <summary>
    /// Analog speedometer gauge configuration controlling the dashboard needle.
    /// </summary>
    [Tooltip("Analog speedometer gauge driving the dashboard needle based on engine RPM.")]
    public SpeedOMeter speedometer;

    [Header("Images")]
    /// <summary>
    /// UI image indicator for the left turn signal.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image left;
    /// <summary>
    /// UI image indicator for the right turn signal.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image right;
    /// <summary>
    /// UI image indicator for headlight status.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image headlights;
    /// <summary>
    /// UI image indicator for Electronic Stability Program engagement.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image ESP;
    /// <summary>
    /// UI image indicator for Anti-lock Braking System engagement.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image ABS;
    /// <summary>
    /// UI image indicator for Traction Control System engagement.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image TCS;
    /// <summary>
    /// UI fill image showing remaining nitrous oxide amount.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image NOS;
    /// <summary>
    /// UI fill image showing remaining fuel level.
    /// </summary>
    [Tooltip("Target image of the following system")] public Image gas;

    [Header("Texts")]
    /// <summary>
    /// Text element displaying the current vehicle speed.
    /// </summary>
    [Tooltip("Target image of the following system")] public TextMeshProUGUI speedText;
    /// <summary>
    /// Text element displaying the speed unit (KMH or MPH).
    /// </summary>
    [Tooltip("Target image of the following system")] public TextMeshProUGUI speedUnitText;
    /// <summary>
    /// Text element displaying the current engine RPM.
    /// </summary>
    [Tooltip("Target image of the following system")] public TextMeshProUGUI RPMText;
    /// <summary>
    /// Text element displaying the current gear (number, N, or R).
    /// </summary>
    [Tooltip("Target image of the following system")] public TextMeshProUGUI gearText;
    /// <summary>
    /// GameObject activated when the vehicle recorder is recording.
    /// </summary>
    [Tooltip("Target image of the following system")] public GameObject recording;
    /// <summary>
    /// GameObject activated when the vehicle recorder is replaying.
    /// </summary>
    [Tooltip("Target image of the following system")] public GameObject replaying;

    [Header("Buttons")]
    /// <summary>
    /// Button that opens the input rebinding panel.
    /// </summary>
    [Tooltip("Button of rebinding inputs")] public Button rebindInputsButton;

    private void Awake() {

        if (spawnVehicles) {

#if RCCP_DEMO
            spawnVehicles.SetActive(true);
            spawnPrototypeVehicles.SetActive(false);
#else
            spawnVehicles.SetActive(false);
            spawnPrototypeVehicles.SetActive(true);
#endif

        }

    }

    private void OnEnable() {

        //  Firing an event when RCCP Canvas spawns.
        RCCP_Events.Event_OnRCCPUISpawned(this);

        RCCP_InputManager.OnOptions += RCCP_InputManager_OnOptions;

    }

    private void RCCP_InputManager_OnOptions() {

        settings.SetActive(!settings.activeSelf);

        if (!settings.activeSelf)
            customization.SetActive(false);

    }

    private void LateUpdate() {

        //  Finding player vehicle on the scene.
        carController = RCCPSceneManager.activePlayerVehicle;

        //  If car controller not found, at disable ui option is enabled, disable panels.
        if (RCCPSceneManager.disableUIWhenNoPlayerVehicle) {

            if (RCCPSettings.mobileControllerEnabled && mobileControllers && mobileControllers.activeSelf != carController)
                mobileControllers.SetActive(carController);

            if (dashboard && dashboard.activeSelf != carController)
                dashboard.SetActive(carController);

        }

        if (rebindInputsButton)
            rebindInputsButton.interactable = !RCCPSettings.mobileControllerEnabled;

        //  If no car controller found, return.
        if (!carController)
            return;

        if (speedometer != null && speedometer.needle)
            speedometer.Operate(carController.engineRPM);

        //  If vehicle has stability component, control the ESP and ABS images.
        if (carController.Stability) {

            if (ESP)
                ESP.color = carController.Stability.ESPIndicatorEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (ABS)
                ABS.color = carController.Stability.ABSEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (TCS)
                TCS.color = carController.Stability.TCSEngaged ? Color.white : new Color(0f, 0f, 0f, .2f);

        }

        //  If vehicle has lights component, control the light images.
        if (carController.Lights) {

            if (headlights)
                headlights.color = carController.Lights.lowBeamHeadlights ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (left)
                left.color = carController.Lights.indicatorsLeft ? Color.white : new Color(0f, 0f, 0f, .2f);

            if (right)
                right.color = carController.Lights.indicatorsRight ? Color.white : new Color(0f, 0f, 0f, .2f);

            //if (hazard)
            //    hazard.color = carController.Lights.indicatorsAll ? Color.white : new Color(0f, 0f, 0f, .2f);

        }

        //  If vehicle has nos component, control the nos sliders.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.Nos) {

            if (NOS)
                NOS.fillAmount = carController.OtherAddonsManager.Nos.amount;

        } else {

            if (NOS)
                NOS.fillAmount = 0f;

        }

        //  If vehicle has fuel tank component, control the gas sliders.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.FuelTank) {

            if (gas)
                gas.fillAmount = carController.OtherAddonsManager.FuelTank.fuelTankFillAmount;

        } else {

            if (gas)
                gas.fillAmount = 0f;

        }

        //  Assigning text of the speed.
        if (speedText)
            speedText.text = !RCCPSettings.useMPH ? carController.absoluteSpeed.ToString("F0") : (carController.absoluteSpeed * .621371f).ToString("F0");

        //  Assigning unit text of the speed.
        if (speedUnitText)
            speedUnitText.text = RCCPSettings.useMPH ? "MPH" : "KMH";

        //  Assigning text of the rpm.
        if (RPMText)
            RPMText.text = carController.engineRPM.ToString("F0");

        //  Assigning text of the gear.
        if (gearText) {

            if (carController.direction == 1) {

                if (!carController.NGearNow)
                    gearText.text = (carController.currentGear + 1).ToString("F0");
                else
                    gearText.text = "N";

            } else {

                gearText.text = "R";

            }

        }

        //  If vehicle has recorder component, control the recording and playing texts.
        if (carController.OtherAddonsManager && carController.OtherAddonsManager.Recorder) {

            switch (carController.OtherAddonsManager.Recorder.mode) {

                case RCCP_Recorder.RecorderMode.Neutral:

                    if (recording)
                        recording.SetActive(false);

                    if (replaying)
                        replaying.SetActive(false);

                    break;

                case RCCP_Recorder.RecorderMode.Record:

                    if (recording)
                        recording.SetActive(true);

                    if (replaying)
                        replaying.SetActive(false);

                    break;

                case RCCP_Recorder.RecorderMode.Play:

                    if (recording)
                        recording.SetActive(false);

                    if (replaying)
                        replaying.SetActive(true);

                    break;

            }

        }

        if (carController.Gearbox) {

            if (gearUp)
                gearUp.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Manual ? true : false);

            if (gearDown)
                gearDown.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Manual ? true : false);

            if (dnrp)
                dnrp.SetActive(carController.Gearbox.transmissionType == RCCP_Gearbox.TransmissionType.Automatic_DNRP ? true : false);

        } else {

            if (gearUp)
                gearUp.SetActive(false);

            if (gearDown)
                gearDown.SetActive(false);

            if (dnrp)
                dnrp.SetActive(false);

        }

    }

    private void OnDisable() {

        RCCP_Events.Event_OnRCCPUIDestroyed(this);

        RCCP_InputManager.OnOptions -= RCCP_InputManager_OnOptions;

    }

}

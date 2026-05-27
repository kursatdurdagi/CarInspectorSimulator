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
/// Receives inputs from the ui controllers. RCCP_InputManager will process these inputs if controller type is mobile.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP Mobile Inputs")]
public class RCCP_MobileInputs : RCCP_GenericComponent {

    private static RCCP_MobileInputs instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static RCCP_MobileInputs Instance {

        get {

#if !UNITY_2022_1_OR_NEWER
            if (instance == null)
                instance = FindObjectOfType<RCCP_MobileInputs>();
#else
            if (instance == null)
                instance = FindAnyObjectByType<RCCP_MobileInputs>();
#endif

            return instance;

        }

    }

    /// <summary>
    /// Root canvas to enable / disable depends on the selected option in the RCCP_Settings ("Use Mobile Controller").
    /// </summary>
    [Tooltip("Root canvas GameObject toggled on/off based on the mobile controller setting.")]
    public GameObject mobileCanvas;

    /// <summary>
    /// Touch UI controller for throttle input.
    /// </summary>
    [Tooltip("Touch button that provides throttle (gas) input when held down.")]
    public RCCP_UIController throttle;

    /// <summary>
    /// Touch UI controller for brake input.
    /// </summary>
    [Tooltip("Touch button that provides brake input when held down.")]
    public RCCP_UIController brake;

    /// <summary>
    /// Touch UI controller for left steering input.
    /// </summary>
    [Tooltip("Touch button that steers the vehicle to the left when held down.")]
    public RCCP_UIController left;

    /// <summary>
    /// Touch UI controller for right steering input.
    /// </summary>
    [Tooltip("Touch button that steers the vehicle to the right when held down.")]
    public RCCP_UIController right;

    /// <summary>
    /// Touch UI controller for handbrake/emergency brake input.
    /// </summary>
    [Tooltip("Touch button that engages the handbrake (emergency brake) when held down.")]
    public RCCP_UIController ebrake;

    /// <summary>
    /// Touch UI controller for nitrous oxide input.
    /// </summary>
    [Tooltip("Touch button that activates the nitrous oxide boost when held down.")]
    public RCCP_UIController nos;

    /// <summary>
    /// Steering wheel.
    /// </summary>
    [Tooltip("On-screen steering wheel UI controller used when the steering wheel mobile mode is selected.")]
    public RCCP_UI_SteeringWheelController steeringWheel;

    /// <summary>
    /// Joystick.
    /// </summary>
    [Tooltip("On-screen joystick UI controller used when the joystick mobile mode is selected.")]
    public RCCP_UI_Joystick joystick;

    /// <summary>
    /// Current processed throttle input value (0 to 1).
    /// </summary>
    [Tooltip("Current processed throttle value from mobile touch controls.")]
    [Range(0f, 1f)] public float throttleInput = 0f;

    /// <summary>
    /// Current processed steering input value (-1 to 1, negative = left).
    /// </summary>
    [Tooltip("Current processed steering value; negative is left, positive is right.")]
    [Range(-1f, 1f)] public float steerInput = 0f;

    /// <summary>
    /// Current processed brake input value (0 to 1).
    /// </summary>
    [Tooltip("Current processed brake value from mobile touch controls.")]
    [Range(0f, 1f)] public float brakeInput = 0f;

    /// <summary>
    /// Current processed handbrake input value (0 to 1).
    /// </summary>
    [Tooltip("Current processed handbrake value from mobile touch controls.")]
    [Range(0f, 1f)] public float ebrakeInput = 0f;

    /// <summary>
    /// Current processed nitrous oxide input value (0 to 1).
    /// </summary>
    [Tooltip("Current processed nitrous oxide value from mobile touch controls.")]
    [Range(0f, 1f)] public float nosInput = 0f;

    private void Update() {

        //  If mobile controller is enabled, set canvas true. Otherwise false.
        if (!RCCPSettings.mobileControllerEnabled) {

            if (mobileCanvas.activeSelf)
                mobileCanvas.SetActive(false);

            return;

        }

        if (!mobileCanvas.activeSelf)
            mobileCanvas.SetActive(true);

        if (RCCPSceneManager && RCCPSceneManager.activePlayerVehicle) {

            if (RCCPSceneManager.activePlayerVehicle.OtherAddonsManager && RCCPSceneManager.activePlayerVehicle.OtherAddonsManager.Nos) {

                if (nos)
                    nos.gameObject.SetActive(true);

            } else {

                if (nos)
                    nos.gameObject.SetActive(false);

            }

        }

        //  Mobile controller types.
        switch (RCCPSettings.mobileController) {

            //  If touch screen, enable and disable corresponding buttons.
            case RCCP_Settings.MobileController.TouchScreen:

                if (steeringWheel && steeringWheel.gameObject.activeSelf)
                    steeringWheel.gameObject.SetActive(false);

                if (joystick && joystick.gameObject.activeSelf)
                    joystick.gameObject.SetActive(false);

                if (left && !left.gameObject.activeSelf)
                    left.gameObject.SetActive(true);

                if (right && !right.gameObject.activeSelf)
                    right.gameObject.SetActive(true);

                break;

            //  If gyro, enable and disable corresponding buttons.
            case RCCP_Settings.MobileController.Gyro:

                if (steeringWheel && steeringWheel.gameObject.activeSelf)
                    steeringWheel.gameObject.SetActive(false);

                if (joystick && joystick.gameObject.activeSelf)
                    joystick.gameObject.SetActive(false);

                if (left && left.gameObject.activeSelf)
                    left.gameObject.SetActive(false);

                if (right && right.gameObject.activeSelf)
                    right.gameObject.SetActive(false);

                if (UnityEngine.InputSystem.Accelerometer.current != null && UnityEngine.InputSystem.Accelerometer.current.device.enabled == false)
                    UnityEngine.InputSystem.InputSystem.EnableDevice(UnityEngine.InputSystem.Accelerometer.current);

                break;

            //  If steering wheel, enable and disable corresponding buttons.
            case RCCP_Settings.MobileController.SteeringWheel:

                if (steeringWheel && !steeringWheel.gameObject.activeSelf)
                    steeringWheel.gameObject.SetActive(true);

                if (joystick && joystick.gameObject.activeSelf)
                    joystick.gameObject.SetActive(false);

                if (left && left.gameObject.activeSelf)
                    left.gameObject.SetActive(false);

                if (right && right.gameObject.activeSelf)
                    right.gameObject.SetActive(false);

                break;

            //  If joystick, enable and disable corresponding buttons.
            case RCCP_Settings.MobileController.Joystick:

                if (steeringWheel && steeringWheel.gameObject.activeSelf)
                    steeringWheel.gameObject.SetActive(false);

                if (joystick && !joystick.gameObject.activeSelf)
                    joystick.gameObject.SetActive(true);

                if (left && left.gameObject.activeSelf)
                    left.gameObject.SetActive(false);

                if (right && right.gameObject.activeSelf)
                    right.gameObject.SetActive(false);

                break;

        }

        //  Inputs.
        if (throttle)
            throttleInput = throttle.input;

        if (left && right)
            steerInput = -left.input + right.input;

        if (steeringWheel)
            steerInput += steeringWheel.input;

        if (joystick)
            steerInput += joystick.inputHorizontal;

        if (brake)
            brakeInput = brake.input;

        if (ebrake)
            ebrakeInput = ebrake.input;

        if (nos)
            nosInput = nos.input;

        throttleInput += nosInput;   //  Increasing throttle input with he nos input. But clamping it to 0 - 1 below.

        if (RCCPSettings.mobileController == RCCP_Settings.MobileController.Gyro) {

            if (UnityEngine.InputSystem.Accelerometer.current != null)
                steerInput += UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue().x * RCCPSettings.gyroSensitivity;

        }

        throttleInput = Mathf.Clamp01(throttleInput);
        steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        brakeInput = Mathf.Clamp01(brakeInput);
        ebrakeInput = Mathf.Clamp01(ebrakeInput);
        nosInput = Mathf.Clamp01(nosInput);

    }

}

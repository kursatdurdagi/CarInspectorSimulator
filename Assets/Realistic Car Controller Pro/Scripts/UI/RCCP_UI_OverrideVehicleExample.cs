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

/// <summary>Example UI component demonstrating how to override vehicle inputs with custom values.</summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Override Vehicle Example")]
public class RCCP_UI_OverrideVehicleExample : RCCP_UIComponent {

    /// <summary>
    /// Target vehicle to override inputs.
    /// </summary>
    [Tooltip("Vehicle whose inputs will be overridden by custom slider values.")]
    public RCCP_CarController targetVehicle;

    /// <summary>
    /// Takes the player vehicle in the scene automatically.
    /// </summary>
    [Tooltip("Automatically assigns the active player vehicle as the override target.")]
    public bool takePlayerVehicle = true;

    /// <summary>
    /// New inputs will be used to override the vehicle.
    /// </summary>
    [Tooltip("Custom input values applied to the vehicle when override is active.")]
    public RCCP_Inputs newInputs = new RCCP_Inputs();

    /// <summary>
    /// Override now?
    /// </summary>
    private bool overrideNow = false;

    /// <summary>
    /// Status text.
    /// </summary>
    [Tooltip("UI text displaying whether the input override is currently enabled or disabled.")]
    public Text statusText;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    [Tooltip("Slider controlling the throttle override value (0 to 1).")]
    public Slider throttle;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    [Tooltip("Slider controlling the brake override value (0 to 1).")]
    public Slider brake;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    [Tooltip("Slider controlling the steering override value (-1 to 1).")]
    public Slider steering;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    [Tooltip("Slider controlling the handbrake override value (0 to 1).")]
    public Slider handbrake;

    /// <summary>
    /// Sliders for inputs.
    /// </summary>
    [Tooltip("Slider controlling the nitrous oxide override value (0 to 1).")]
    public Slider nos;

    private void Update() {

        newInputs.throttleInput = throttle.value;
        newInputs.brakeInput = brake.value;
        newInputs.steerInput = steering.value;
        newInputs.handbrakeInput = handbrake.value;
        newInputs.nosInput = nos.value;

        if (takePlayerVehicle)
            targetVehicle = RCCPSceneManager.activePlayerVehicle;

        if (targetVehicle && overrideNow)
            targetVehicle.Inputs.OverrideInputs(newInputs);

        if (targetVehicle && targetVehicle.Inputs)
            statusText.text = "Status: " + (targetVehicle.Inputs.overridePlayerInputs ? "Enabled" : "False");
        else
            statusText.text = "Status: Disabled";

    }

    /// <summary>Enables input override mode, applying custom throttle, brake, and steering values to the vehicle.</summary>
    public void EnableOverride() {

        overrideNow = true;

        if (targetVehicle)
            targetVehicle.Inputs.OverrideInputs(newInputs);

    }

    /// <summary>Disables input override mode, returning control to the normal input system.</summary>
    public void DisableOverride() {

        overrideNow = false;

        if (targetVehicle)
            targetVehicle.Inputs.DisableOverrideInputs();

    }

}

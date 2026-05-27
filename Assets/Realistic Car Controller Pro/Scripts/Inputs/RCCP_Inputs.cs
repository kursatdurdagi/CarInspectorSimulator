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
/// Main inputs of the player.
/// </summary>
[System.Serializable]
public class RCCP_Inputs {

    /// <summary>
    /// Throttle input value (0 = no throttle, 1 = full throttle).
    /// </summary>
    [Tooltip("Throttle input value (0 = no throttle, 1 = full throttle).")]
    [Range(0f, 1f)] public float throttleInput = 0f;

    /// <summary>
    /// Brake input value (0 = no brake, 1 = full brake).
    /// </summary>
    [Tooltip("Brake input value (0 = no brake, 1 = full brake).")]
    [Range(0f, 1f)] public float brakeInput = 0f;

    /// <summary>
    /// Steering input value (-1 = full left, 0 = center, 1 = full right).
    /// </summary>
    [Tooltip("Steering input value (-1 = full left, 0 = center, 1 = full right).")]
    [Range(-1f, 1f)] public float steerInput = 0f;

    /// <summary>
    /// Handbrake input value (0 = released, 1 = fully engaged).
    /// </summary>
    [Tooltip("Handbrake input value (0 = released, 1 = fully engaged).")]
    [Range(0f, 1f)] public float handbrakeInput = 0f;

    /// <summary>
    /// Clutch input value (0 = engaged, 1 = fully disengaged).
    /// </summary>
    [Tooltip("Clutch input value (0 = engaged, 1 = fully disengaged).")]
    [Range(0f, 1f)] public float clutchInput = 0f;

    /// <summary>
    /// Nitrous oxide input value (0 = off, 1 = full).
    /// </summary>
    [Tooltip("Nitrous oxide input value (0 = off, 1 = full).")]
    [Range(0f, 1f)] public float nosInput = 0f;

    /// <summary>
    /// Mouse or touch delta input used for camera orbit control.
    /// </summary>
    [Tooltip("Mouse or touch delta input used for camera orbit control.")]
    public Vector2 mouseInput = new Vector2(0f, 0f);

    /// <summary>
    /// Creates an empty inputs instance with all values at zero/default.
    /// </summary>
    public RCCP_Inputs() { }

    /// <summary>
    /// Creates an inputs instance with all values specified.
    /// </summary>
    /// <param name="throttleInput">Throttle input (0-1).</param>
    /// <param name="brakeInput">Brake input (0-1).</param>
    /// <param name="steerInput">Steering input (-1 to 1).</param>
    /// <param name="handbrakeInput">Handbrake input (0-1).</param>
    /// <param name="clutchInput">Clutch input (0-1).</param>
    /// <param name="nosInput">Nitrous oxide input (0-1).</param>
    /// <param name="mouseInput">Mouse/touch delta for camera control.</param>
    public RCCP_Inputs(float throttleInput, float brakeInput, float steerInput, float handbrakeInput, float clutchInput, float nosInput, Vector2 mouseInput) {

        this.throttleInput = throttleInput;
        this.brakeInput = brakeInput;
        this.steerInput = steerInput;
        this.handbrakeInput = handbrakeInput;
        this.clutchInput = clutchInput;
        this.nosInput = nosInput;
        this.mouseInput = mouseInput;

    }

    /// <summary>
    /// Resets all input values to zero/default.
    /// </summary>
    public void Clear() {

        throttleInput = 0f;
        brakeInput = 0f;
        steerInput = 0f;
        handbrakeInput = 0f;
        clutchInput = 0f;
        nosInput = 0f;

        mouseInput = Vector2.zero;

    }

}

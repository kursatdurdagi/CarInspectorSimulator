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
/// Animates Driver Sofie (Credits to 3DMaesen). Simply feeds floats and bools of Sofie's animator component.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Character Controller")]
public class RCCP_CharacterController : RCCP_GenericComponent {

    /// <summary>
    /// Reference to the parent car controller providing vehicle state for animations.
    /// </summary>
    private RCCP_CarController carController;

    /// <summary>
    /// Animator.
    /// </summary>
    [Header("References")]
    [Tooltip("Animator component driving the character's animations.")]
    public Animator animator;

    // String parameters of animator.
    [Header("Animator Parameters")]
    /// <summary>
    /// Animator float parameter name for steering input (-1 to 1).
    /// </summary>
    [Tooltip("Animator float parameter name for steering input (-1 to 1).")]
    public string driverSteeringParameter;

    /// <summary>
    /// Animator bool parameter name for gear shifting state.
    /// </summary>
    [Tooltip("Animator bool parameter name for gear shifting state.")]
    public string driverShiftingGearParameter;

    /// <summary>
    /// Animator bool parameter name for collision impact reaction.
    /// </summary>
    [Tooltip("Animator bool parameter name for collision impact reaction.")]
    public string driverDangerParameter;

    /// <summary>
    /// Animator bool parameter name for reversing state.
    /// </summary>
    [Tooltip("Animator bool parameter name for reversing state.")]
    public string driverReversingParameter;

    // Inputs for feeding animator.
    [Header("Runtime Inputs")]
    /// <summary>
    /// Current smoothed steering input value fed to the animator.
    /// </summary>
    [Tooltip("Current smoothed steering input fed to the animator.")]
    [Range(-1f, 1f)]
    public float steerInput = 0f;

    /// <summary>
    /// Local-space forward velocity used to detect reversing.
    /// </summary>
    [Tooltip("Local-space forward velocity used to detect reversing.")]
    public float directionInput = 0f;

    /// <summary>
    /// Whether the vehicle is currently moving backwards.
    /// </summary>
    [Tooltip("Whether the vehicle is currently moving backwards.")]
    public bool reversing = false;

    /// <summary>
    /// Collision impact intensity (0-1) that decays over time, triggers danger animation.
    /// </summary>
    [Tooltip("Collision impact intensity (0-1) that decays over time.")]
    [Range(0f, 1f)]
    public float impactInput = 0f;

    /// <summary>
    /// Gear shift animation intensity (0-1) that decays over time.
    /// </summary>
    [Tooltip("Gear shift animation intensity (0-1) that decays over time.")]
    [Range(0f, 1f)]
    public float gearInput = 0f;

    private void Start() {

        //  Getting components.
        if (!animator)
            animator = GetComponentInChildren<Animator>();

        TryGetComponent(out carController);

    }

    private void Update() {

        //  Getting steer input.
        steerInput = Mathf.Lerp(steerInput, carController.steerInput_V, Time.deltaTime * 5f);
        directionInput = carController.transform.InverseTransformDirection(carController.Rigid.linearVelocity).z;
        impactInput -= Time.deltaTime * 5f;

        //  Clamping impact input.
        if (impactInput < 0)
            impactInput = 0f;
        if (impactInput > 1)
            impactInput = 1f;

        //  If vehicle is going backwards or not.
        if (directionInput <= -2f)
            reversing = true;
        else if (directionInput > -1f)
            reversing = false;

        //  If changing gear.
        if (carController.shiftingNow)
            gearInput = 1f;
        else
            gearInput -= Time.deltaTime * 5f;

        //  Clamping gear input.
        if (gearInput < 0)
            gearInput = 0f;
        if (gearInput > 1)
            gearInput = 1f;

        //  If reversing.
        if (!reversing)
            animator.SetBool(driverReversingParameter, false);
        else
            animator.SetBool(driverReversingParameter, true);

        //  If impact is high enough, animate collision animation by setting bool.
        if (impactInput > .5f)
            animator.SetBool(driverDangerParameter, true);
        else
            animator.SetBool(driverDangerParameter, false);

        //  If changing gear, animate change gear animation by setting bool.
        if (gearInput > .5f)
            animator.SetBool(driverShiftingGearParameter, true);
        else
            animator.SetBool(driverShiftingGearParameter, false);

        //  Setting steer input of the animator by setting float.
        animator.SetFloat(driverSteeringParameter, steerInput);

    }

    private void OnCollisionEnter(Collision col) {

        //  If collision is not high enough, return.
        if (col.relativeVelocity.magnitude < 2.5f)
            return;

        //  Setting impact to 1 on collisions.
        impactInput = 1f;

    }

}

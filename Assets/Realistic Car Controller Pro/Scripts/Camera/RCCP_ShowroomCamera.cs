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
using UnityEngine.EventSystems;

/// <summary>
/// Showroom camera used on main menu.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Showroom Camera")]
public class RCCP_ShowroomCamera : RCCP_GenericComponent {

    /// <summary>
    /// Camera target. Usually our spawn point.
    /// </summary>
    [Tooltip("The transform the camera orbits around, typically the vehicle spawn point.")]
    public Transform target;

    /// <summary>
    /// Z Distance.
    /// </summary>
    [Min(0f), Tooltip("Distance from the target along the camera's forward axis.")]
    public float distance = 8f;

    /// <summary>
    /// Auto orbiting now?
    /// </summary>
    [Space, Tooltip("When enabled, the camera automatically rotates around the target.")]
    public bool orbitingNow = true;

    /// <summary>
    /// Auto orbiting speed.
    /// </summary>
    [Min(0f), Tooltip("Speed of the automatic orbit rotation in degrees per second.")]
    public float orbitSpeed = 5f;

    /// <summary>
    /// Smooth orbiting.
    /// </summary>
    [Space, Tooltip("Smoothly interpolates camera position and rotation instead of snapping.")]
    public bool smooth = true;

    /// <summary>
    /// Smooth orbiting factor.
    /// </summary>
    [Min(0f), Tooltip("How quickly the camera interpolates to its target position and rotation.")]
    public float smoothingFactor = 5f;

    /// <summary>
    /// Minimum Y degree.
    /// </summary>
    [Space, Range(-90f, 90f), Tooltip("Lowest vertical angle the camera can reach when orbiting.")]
    public float minY = 5f;

    /// <summary>
    /// Maximum Y degree.
    /// </summary>
    [Range(-90f, 90f), Tooltip("Highest vertical angle the camera can reach when orbiting.")]
    public float maxY = 35f;

    /// <summary>
    /// Player is rotating the camera now?
    /// </summary>
    [Space]
    private bool draggingNow = false;

    /// <summary>
    /// Drag speed.
    /// </summary>
    [Min(0f), Tooltip("Sensitivity multiplier for manual drag-to-orbit input.")]
    public float dragSpeed = 10f;

    /// <summary>
    /// Orbit X.
    /// </summary>
    [Tooltip("Current horizontal orbit angle in degrees.")]
    public float orbitX = 0f;

    /// <summary>
    /// Orbit Y.
    /// </summary>
    [Tooltip("Current vertical orbit angle in degrees.")]
    public float orbitY = 0f;

    private void Update() {

        // Receiving player inputs for setting orbit X and Y.
        Inputs();

    }

    private void LateUpdate() {

        // If there is no target, return.
        if (!target) {

            Debug.LogWarning("Camera target not found!");
            enabled = false;
            return;

        }

        // If auto orbiting is enabled, increase orbitX slowly with orbitSpeed factor.
        if (orbitingNow)
            orbitX += Time.deltaTime * orbitSpeed;

        //  Clamping orbit Y.
        orbitY = ClampAngle(orbitY, minY, maxY);

        // Calculating rotation and position of the camera.
        Quaternion rotation = Quaternion.Euler(orbitY, orbitX, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + target.transform.position;

        // Setting position and rotation of the camera.
        if (!smooth) {

            transform.rotation = rotation;
            transform.position = position;

        } else {

            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.unscaledDeltaTime * 10f);
            transform.position = Vector3.Lerp(transform.position, position, Time.unscaledDeltaTime * 10f);

        }

    }

    /// <summary>
    /// Receiving inputs for dragging the camera.
    /// </summary>
    private void Inputs() {

        RCCP_Inputs inputs = RCCP_InputManager.Instance.GetInputs();

        if (draggingNow) {

            //orbitX += inputs.orbitX * dragSpeed * Time.deltaTime;
            //orbitY -= inputs.orbitY * dragSpeed * Time.deltaTime;

        }

    }

    /// <summary>
    /// Enables or disables user drag input for manual camera orbiting.
    /// </summary>
    /// <param name="state">True to enable drag input, false to disable.</param>
    public void SetDrag(bool state) {

        draggingNow = state;

    }

    private float ClampAngle(float angle, float min, float max) {

        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);

    }

    /// <summary>
    /// Enables or disables automatic camera orbiting around the target.
    /// </summary>
    /// <param name="state">True to enable auto-rotation, false to disable.</param>
    public void ToggleAutoRotation(bool state) {

        orbitingNow = state;

    }

    /// <summary>
    /// Handles pointer drag events from the UI event system to manually orbit the camera.
    /// </summary>
    /// <param name="pointerData">Pointer event data containing the drag delta.</param>
    public void OnDrag(PointerEventData pointerData) {

        // Receiving drag input from UI.
        orbitX += pointerData.delta.x * dragSpeed * .02f;
        orbitY -= pointerData.delta.y * dragSpeed * .02f;

    }

}

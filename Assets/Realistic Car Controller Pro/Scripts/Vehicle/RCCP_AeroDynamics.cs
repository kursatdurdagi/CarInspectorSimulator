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
#if UNITY_EDITOR        // keep compilation clean in builds
using UnityEditor;
#endif

/// <summary>
/// Manages the dynamics of the vehicle.
/// </summary>
[DefaultExecutionOrder(-3)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Dynamics")]
public class RCCP_AeroDynamics : RCCP_Component {

    /// <summary>
    /// Center of Mass for the vehicle. If not found, automatically creates a "COM" child object.
    /// </summary>
    public Transform COM {

        get {

            if (com == null) {

                if (transform.Find("COM")) {

                    com = transform.Find("COM");
                    return com;

                }

                GameObject newCom = new GameObject("COM");
                newCom.transform.SetParent(transform, false);
                com = newCom.transform;
                RecalculateCOM();

                return com;

            }

            return com;

        }
        set {

            com = value;

        }

    }

    private Transform com;

    [Header("Forces")]
    [Tooltip("Velocity-squared downward force applied at each wheel to improve high-speed grip.")]
    [Min(0f)] public float downForce = 10f;

    /// <summary>
    /// Air resistance applied to the vehicle based on speed. Higher values cause more aerodynamic drag.
    /// </summary>
    [Tooltip("Aerodynamic drag coefficient opposing forward motion at speed.")]
    [Range(0f, 100f)] public float airResistance = 10f;

    /// <summary>
    /// Deceleration applied to the vehicle based on speed. Higher values cause the vehicle to slow down more quickly.
    /// </summary>
    [Tooltip("Rolling resistance that decelerates the vehicle when not accelerating.")]
    [Range(0f, 100f)] public float wheelResistance = 10f;

    /// <summary>
    /// Ignores the rigidbody drag force while accelerating. Used to achieve the maximum speed easily.
    /// </summary>
    [Tooltip("Zeroes rigidbody drag while throttle is applied, making it easier to reach top speed.")]
    public bool ignoreRigidbodyDragOnAccelerate = false;

    /// <summary>
    /// If true, the COM (Center of Mass) will be dynamically updated each physics frame.
    /// </summary>
    [Tooltip("Recalculates the center of mass every physics frame to follow the COM transform.")]
    public bool dynamicCOM = false;

    /// <summary>
    /// If enabled, the vehicle will automatically reset if it flips upside down.
    /// </summary>
    [Header("Auto Reset")]
    [Tooltip("Automatically flips the vehicle upright if it rolls over and stays nearly stationary.")]
    public bool autoReset = true;

    /// <summary>
    /// Time (in seconds) to wait before resetting the vehicle if it's flipped.
    /// </summary>
    [Tooltip("Seconds the vehicle must remain flipped before it is automatically reset.")]
    [Min(0f)] public float autoResetTime = 3f;
    private float autoResetTimer = 0f;

    private float defaultDrag = -1f;

    public override void Start() {

        base.Start();

        // Assigning center of mass position once at the start.
        CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

    }

    private void FixedUpdate() {

        // Dynamically updates COM if enabled.
        if (dynamicCOM)
            CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

        if (defaultDrag < 0)
            defaultDrag = CarController.Rigid.linearDamping;

        if (ignoreRigidbodyDragOnAccelerate)
            CarController.Rigid.linearDamping = defaultDrag * (1f - CarController.throttleInput_V);

        // Local forward speed (z-axis).
        float linearSpeed = transform.InverseTransformDirection(CarController.Rigid.linearVelocity).z;
        float speedMagnitude = Mathf.Abs(linearSpeed);

        if (CarController.IsGrounded) {

            // --------------------------------------------------------------------
            // 1. Downforce (velocity-squared version for more "realistic" feel)
            // --------------------------------------------------------------------
            // If you want to keep it linear, use:
            //   float downforceValue = downForce * speedMagnitude;
            float downforceValue = downForce * (speedMagnitude * speedMagnitude);
            downforceValue *= .15f;

            // Apply downforce in local downward direction.
            RCCP_WheelCollider[] wheelColliders = CarController.AllWheelColliders;

            if (wheelColliders != null && wheelColliders.Length > 0) {

                for (int i = 0; i < wheelColliders.Length; i++) {

                    if (wheelColliders[i] != null)
                        CarController.Rigid.AddForceAtPosition(-transform.up * (downforceValue / (float)wheelColliders.Length), wheelColliders[i].transform.position, ForceMode.Force);

                }

            }

        }

        // --------------------------------------------------------------------
        // 2. Aerodynamic drag (quadratic in speed, monotonic)
        // --------------------------------------------------------------------
        float dragForce = airResistance * 0.025f * speedMagnitude * speedMagnitude;

        Vector3 worldVel = CarController.Rigid.linearVelocity;

        if (worldVel.sqrMagnitude > 0.01f)
            CarController.Rigid.AddForceAtPosition(-worldVel.normalized * dragForce, COM.position, ForceMode.Force);

        // --------------------------------------------------------------------
        // 3. Rolling resistance (constant magnitude, opposes motion)
        // --------------------------------------------------------------------
        if (CarController.IsGrounded && speedMagnitude > 0.1f) {

            float rollingForce = wheelResistance * 20f;
            float rollingSign = Mathf.Sign(linearSpeed);

            CarController.Rigid.AddRelativeForce(-Vector3.forward * rollingForce * rollingSign, ForceMode.Force);

        }

        // --------------------------------------------------------------------
        // 4. Auto-reset if upside down
        // --------------------------------------------------------------------
        if (autoReset)
            CheckUpsideDown();

    }

    /// <summary>
    /// Checks if the vehicle is upside down and resets it after 'autoResetTime' if speed is low.
    /// </summary>
    private void CheckUpsideDown() {

        // If vehicle speed is under 5, not kinematic, and z rotation is between 60 and 300, reset after the timer.
        if (Mathf.Abs(CarController.absoluteSpeed) < 8f && !CarController.Rigid.isKinematic) {

            if (CarController.transform.eulerAngles.z < 300f && CarController.transform.eulerAngles.z > 60f) {

                autoResetTimer += Time.deltaTime;

                if (autoResetTimer > autoResetTime) {

                    CarController.transform.SetPositionAndRotation(

                        new Vector3(CarController.transform.position.x, CarController.transform.position.y + 3f, CarController.transform.position.z),
                        Quaternion.Euler(0f, CarController.transform.eulerAngles.y, 0f)

                    );

                    autoResetTimer = 0f;

                }

            }

        }

    }

    /// <summary>
    /// Resets the timer used for flipping the vehicle.
    /// </summary>
    public void Reload() {

        autoResetTimer = 0f;

    }

    /// <summary>
    /// Sets the COM (Center of Mass) to a specific local position offset.
    /// This overrides any auto-calculated position.
    /// </summary>
    /// <param name="localOffset">Local position offset relative to this component's transform.</param>
    public void SetCOMOffset(Vector3 localOffset) {

        COM.localPosition = localOffset;

        // Apply immediately to rigidbody if available
        if (CarController != null && CarController.Rigid != null)
            CarController.Rigid.centerOfMass = transform.InverseTransformPoint(COM.position);

    }

    /// <summary>
    /// Re-calculates the COM position using renderer bounds plus user-defined
    /// racing biases. Call this after changing the vehicle's meshes.
    /// </summary>
    public void RecalculateCOM() {

        Transform t = COM;

        // --------------------------------------------------------------------
        // 1) Build combined world-space bounds of every visible renderer
        //    (ignores trails / particles so exhaust smoke doesn�t skew the box)
        // --------------------------------------------------------------------
        RCCP_CarController carController = GetComponentInParent<RCCP_CarController>(true);

        // Not part of a vehicle hierarchy yet - use fallback COM
        if (carController == null) {

            if (t) t.localPosition = new Vector3(0f, -.25f, 0f);
            return;

        }

        Renderer[] renderers = carController.GetComponentsInChildren<Renderer>(false);

        Bounds worldBounds = new Bounds();
        bool hasBounds = false;

        foreach (Renderer r in renderers) {

            if (r is TrailRenderer || r is ParticleSystemRenderer)
                continue;

            if (!hasBounds) {

                worldBounds = new Bounds(r.bounds.center, r.bounds.size);
                hasBounds = true;

            } else {

                worldBounds.Encapsulate(r.bounds);

            }

        }

        // Fallback if no renderers were found
        if (!hasBounds) {

            t.localPosition = new Vector3(0f, -.25f, 0f);
            return;

        }

        // --------------------------------------------------------------------
        // 2) Convert to local space so the COM lives neatly under the root
        // --------------------------------------------------------------------
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 ext = worldBounds.extents;             // half-sizes (world)

        // Because we stay in root space (no scaling on car root in RCCP),
        // using world extents for biasing is sufficiently accurate.

        // --------------------------------------------------------------------
        // 3) Apply race-car biases
        // --------------------------------------------------------------------
        // Vertical: bottom + (height * bias)
        float bottomLocalY = localCenter.y - ext.y;
        localCenter.y = bottomLocalY + (worldBounds.size.y * .3f);

        // Longitudinal (Z): centre � fore/aft bias
        localCenter.z = 0f;

        // Lateral (X): centre � left/right bias
        localCenter.x = 0f;

        // --------------------------------------------------------------------
        // 4) Commit
        // --------------------------------------------------------------------
        t.localPosition = localCenter;

    }


    private void Reset() {

        // Runs when the component is first added from the Inspector.
        RecalculateCOM();

    }

}

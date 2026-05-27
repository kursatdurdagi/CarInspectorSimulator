//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Transport On Trigger")]
/// <summary>Trigger zone that transports the player vehicle to a specified position when entered.</summary>
public class RCCP_TransportOnTrigger : RCCP_GenericComponent {

    /// <summary>Target transform position where the vehicle will be transported to.</summary>
    [Tooltip("Destination transform where the vehicle will be teleported upon entering the trigger.")]
    public Transform transportToHere;
    /// <summary>Whether to reset the vehicle's velocity to zero after transport.</summary>
    [Tooltip("Zero out the vehicle's linear and angular velocity after teleporting.")]
    public bool resetVelocity = true;
    /// <summary>Whether to match the vehicle's rotation to the target transform's rotation.</summary>
    [Tooltip("Align the vehicle's rotation to the destination transform's rotation.")]
    public bool resetRotation = true;

    private void OnTriggerEnter(Collider other) {

        if (other.isTrigger)
            return;

        //  Getting car controller.
        RCCP_CarController carController = other.GetComponentInParent<RCCP_CarController>();

        //  If trigger is not a vehicle, return.
        if (!carController)
            return;

        if (resetRotation)
            RCCP.Transport(carController, transportToHere.position, transportToHere.rotation, resetVelocity);
        else
            RCCP.Transport(carController, transportToHere.position, carController.transform.rotation, resetVelocity);

    }

}

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
/// UI change wheel button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Wheel Button")]
public class RCCP_UI_Wheel : RCCP_UIComponent {

    /// <summary>
    /// Index of the target wheel.
    /// </summary>
    [Tooltip("Index of the wheel preset to equip when this button is pressed.")]
    [Min(0)] public int wheelIndex = 0;

    /// <summary>Equips the configured wheel set on the active player vehicle.</summary>
    public void OnClick() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.WheelManager)
            return;

        playerVehicle.Customizer.WheelManager.UpdateWheel(wheelIndex);

    }

}

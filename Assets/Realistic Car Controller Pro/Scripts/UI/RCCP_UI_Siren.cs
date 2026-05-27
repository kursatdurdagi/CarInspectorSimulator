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
/// UI siren button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Siren Button")]
public class RCCP_UI_Siren : RCCP_UIComponent {

    /// <summary>
    /// Index of the target siren.
    /// </summary>
    [Tooltip("Index of the siren preset to equip when this button is pressed.")]
    [Min(0)] public int index = 0;

    /// <summary>Equips the configured siren on the active player vehicle.</summary>
    public void Upgrade() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.SirenManager)
            return;

        playerVehicle.Customizer.SirenManager.Upgrade(index);

    }

}

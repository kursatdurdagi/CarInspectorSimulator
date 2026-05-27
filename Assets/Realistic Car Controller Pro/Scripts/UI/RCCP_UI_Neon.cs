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
/// UI neon button.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Neon Button")]
public class RCCP_UI_Neon : RCCP_UIComponent {

    /// <summary>
    /// Target material.
    /// </summary>
    [Tooltip("Material used for the neon underglow effect on the vehicle.")]
    public Material material;

    /// <summary>Applies the selected neon material to the vehicle's underglow.</summary>
    public void Upgrade() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        //  If player vehicle doesn't have the decal manager component, return.
        if (!playerVehicle.Customizer.NeonManager)
            return;

        //  Set the decal.
        playerVehicle.Customizer.NeonManager.Upgrade(material);

    }

}

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
/// Serializable loadout that persists the player's vehicle customization choices (paint, upgrades, wheels, spoilers, decals, neons, sirens) for save/load.
/// </summary>
[System.Serializable]
public class RCCP_CustomizationLoadout : IRCCP_LoadoutComponent {

    /// <summary>Saved vehicle body paint color (alpha 0 means no paint applied).</summary>
    [Tooltip("Saved vehicle body paint color (alpha 0 means no paint applied).")]
    public Color paint = new Color(1f, 1f, 1f, 0f);

    /// <summary>Index of the equipped spoiler (-1 means none equipped).</summary>
    [Tooltip("Index of the equipped spoiler (-1 means none equipped).")]
    [Min(-1)] public int spoiler = -1;
    /// <summary>Index of the equipped siren/police light (-1 means none equipped).</summary>
    [Tooltip("Index of the equipped siren/police light (-1 means none equipped).")]
    [Min(-1)] public int siren = -1;
    /// <summary>Index of the equipped wheel set (-1 means none equipped).</summary>
    [Tooltip("Index of the equipped wheel set (-1 means none equipped).")]
    [Min(-1)] public int wheel = -1;

    /// <summary>Current engine upgrade level (0 = stock).</summary>
    [Tooltip("Current engine upgrade level (0 = stock).")]
    [Min(0)] public int engineLevel = 0;
    /// <summary>Current handling upgrade level (0 = stock).</summary>
    [Tooltip("Current handling upgrade level (0 = stock).")]
    [Min(0)] public int handlingLevel = 0;
    /// <summary>Current brake upgrade level (0 = stock).</summary>
    [Tooltip("Current brake upgrade level (0 = stock).")]
    [Min(0)] public int brakeLevel = 0;
    /// <summary>Current speed upgrade level (0 = stock).</summary>
    [Tooltip("Current speed upgrade level (0 = stock).")]
    [Min(0)] public int speedLevel = 0;

    /// <summary>Index of the front decal (-1 means none applied).</summary>
    [Tooltip("Index of the front decal (-1 means none applied).")]
    [Min(-1)] public int decalIndexFront = -1;
    /// <summary>Index of the back decal (-1 means none applied).</summary>
    [Tooltip("Index of the back decal (-1 means none applied).")]
    [Min(-1)] public int decalIndexBack = -1;
    /// <summary>Index of the left side decal (-1 means none applied).</summary>
    [Tooltip("Index of the left side decal (-1 means none applied).")]
    [Min(-1)] public int decalIndexLeft = -1;
    /// <summary>Index of the right side decal (-1 means none applied).</summary>
    [Tooltip("Index of the right side decal (-1 means none applied).")]
    [Min(-1)] public int decalIndexRight = -1;

    /// <summary>Index of the equipped neon underglow (-1 means none equipped).</summary>
    [Tooltip("Index of the equipped neon underglow (-1 means none equipped).")]
    [Min(-1)] public int neonIndex = -1;

    /// <summary>Detailed customization data including suspension, steering, and driving aid settings.</summary>
    [Tooltip("Detailed customization data including suspension, steering, and driving aid settings.")]
    public RCCP_CustomizationData customizationData = new RCCP_CustomizationData();

    /// <summary>
    /// Updates the loadout from the given upgrade manager component by reading its current state.
    /// </summary>
    /// <param name="component">The upgrade manager component whose current values will be stored in the loadout.</param>
    public void UpdateLoadout(MonoBehaviour component) {

        switch (component) {

            case RCCP_VehicleUpgrade_WheelManager:

                RCCP_VehicleUpgrade_WheelManager wheelComponent = (RCCP_VehicleUpgrade_WheelManager)component;
                wheel = wheelComponent.wheelIndex;
                break;

            case RCCP_VehicleUpgrade_UpgradeManager:

                RCCP_VehicleUpgrade_UpgradeManager upgradeComponent = (RCCP_VehicleUpgrade_UpgradeManager)component;
                engineLevel = upgradeComponent.EngineLevel;
                brakeLevel = upgradeComponent.BrakeLevel;
                handlingLevel = upgradeComponent.HandlingLevel;
                speedLevel = upgradeComponent.SpeedLevel;
                break;

            case RCCP_VehicleUpgrade_PaintManager:

                RCCP_VehicleUpgrade_PaintManager paintComponent = (RCCP_VehicleUpgrade_PaintManager)component;
                paint = paintComponent.color;
                break;

            case RCCP_VehicleUpgrade_SpoilerManager:

                RCCP_VehicleUpgrade_SpoilerManager spoilerComponent = (RCCP_VehicleUpgrade_SpoilerManager)component;
                spoiler = spoilerComponent.spoilerIndex;
                break;

            case RCCP_VehicleUpgrade_SirenManager:

                RCCP_VehicleUpgrade_SirenManager sirenComponent = (RCCP_VehicleUpgrade_SirenManager)component;
                siren = sirenComponent.sirenIndex;
                break;

            case RCCP_VehicleUpgrade_CustomizationManager:

                RCCP_VehicleUpgrade_CustomizationManager customizationComponent = (RCCP_VehicleUpgrade_CustomizationManager)component;
                customizationData = customizationComponent.customizationData;
                break;

            case RCCP_VehicleUpgrade_DecalManager:

                RCCP_VehicleUpgrade_DecalManager decalManager = (RCCP_VehicleUpgrade_DecalManager)component;
                decalIndexFront = decalManager.index_decalFront;
                decalIndexBack = decalManager.index_decalBack;
                decalIndexLeft = decalManager.index_decalLeft;
                decalIndexRight = decalManager.index_decalRight;
                break;

            case RCCP_VehicleUpgrade_NeonManager:

                RCCP_VehicleUpgrade_NeonManager neonManager = (RCCP_VehicleUpgrade_NeonManager)component;
                neonIndex = neonManager.index;
                break;

        }

    }

}

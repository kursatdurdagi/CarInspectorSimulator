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
/// Manager for upgradable spoilers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Spoiler Manager")]
public class RCCP_VehicleUpgrade_SpoilerManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// All upgradable spoilers.
    /// </summary>
    [Tooltip("All available spoiler upgrade prefabs on this vehicle.")]
    public RCCP_VehicleUpgrade_Spoiler[] spoilers;

    /// <summary>
    /// Last selected spoiler index.
    /// </summary>
    [Tooltip("Currently equipped spoiler index (-1 means no spoiler).")]
    [Min(-1)] public int spoilerIndex = -1;

    /// <summary>
    /// Painting the spoilers?
    /// </summary>
    [Tooltip("Whether spoilers should receive the vehicle body paint color.")]
    public bool paintSpoilers = true;

    /// <summary>
    /// Initializes the spoiler manager by disabling all spoilers, then enabling the saved spoiler from the loadout and applying saved paint color.
    /// </summary>
    public void Initialize() {

        //  If spoilers is null, return.
        if (spoilers == null)
            return;

        //  If spoilers is null, return
        if (spoilers.Length < 1)
            return;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(false);

        }

        //  Getting index of the loadouts spoiler.
        spoilerIndex = Loadout.spoiler;

        //  If spoiler index is -1, return.
        if (spoilerIndex == -1)
            return;

        //  If index is not -1, enable the corresponding spoiler.
        if (spoilers[spoilerIndex] != null)
            spoilers[spoilerIndex].gameObject.SetActive(true);

        //  Getting saved color of the spoiler.
        if (Loadout.paint != new Color(1f, 1f, 1f, 0f))
            Paint(Loadout.paint);

    }

    /// <summary>
    /// Finds and caches all child RCCP_VehicleUpgrade_Spoiler components, including inactive ones.
    /// </summary>
    public void GetAllSpoilers() {

        spoilers = GetComponentsInChildren<RCCP_VehicleUpgrade_Spoiler>(true);

    }

    /// <summary>
    /// Deactivates all spoiler GameObjects on the vehicle.
    /// </summary>
    public void DisableAll() {

        //  If spoilers is null, return.
        if (spoilers == null)
            return;

        //  If spoilers is null, return
        if (spoilers.Length < 1)
            return;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(false);

        }

    }

    /// <summary>
    /// Activates all spoiler GameObjects on the vehicle.
    /// </summary>
    public void EnableAll() {

        //  If spoilers is null, return.
        if (spoilers == null)
            return;

        //  If spoilers is null, return
        if (spoilers.Length < 1)
            return;

        //  Enabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(true);

        }

    }

    /// <summary>
    /// Unlocks target spoiler index and saves it.
    /// </summary>
    /// <param name="index">Index of the spoiler to equip (-1 to remove all spoilers).</param>
    public void Upgrade(int index) {

        //  If sirens is null, return.
        if (spoilers == null)
            return;

        if (spoilers.Length < 1)
            return;

        //  Index of the spoiler.
        spoilerIndex = index;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(false);

        }

        //  If spoiler index is -1, return.
        if (spoilerIndex == -1)
            return;

        //  If index is not -1, enable the corresponding spoiler.
        if (spoilerIndex != -1 && spoilers[spoilerIndex] != null)
            spoilers[spoilerIndex].gameObject.SetActive(true);

        if (spoilerIndex != -1 && Loadout.paint != new Color(1f, 1f, 1f, 0f) && spoilers[spoilerIndex].bodyRenderer != null)
            Paint(Loadout.paint);

        //  Refreshing the loadout.
        Refresh(this);

        //  Saving the loadout.
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Unlocks target spoiler index and saves it.
    /// </summary>
    /// <param name="index">Index of the spoiler to equip (-1 to remove all spoilers).</param>
    public void UpgradeWithoutSave(int index) {

        //  If sirens is null, return.
        if (spoilers == null)
            return;

        if (spoilers.Length < 1)
            return;

        //  Index of the spoiler.
        spoilerIndex = index;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(false);

        }

        //  If spoiler index is -1, return.
        if (spoilerIndex == -1)
            return;

        //  If index is not -1, enable the corresponding spoiler.
        if (spoilers[spoilerIndex] != null)
            spoilers[spoilerIndex].gameObject.SetActive(true);

        if (Loadout.paint != new Color(1f, 1f, 1f, 0f) && spoilers[spoilerIndex].bodyRenderer != null)
            Paint(Loadout.paint);

    }

    /// <summary>
    /// Applies a paint color to all spoiler renderers.
    /// </summary>
    /// <param name="newColor">The color to apply to all spoiler body renderers.</param>
    public void Paint(Color newColor) {

        //  If spoilers is null, return.
        if (spoilers == null)
            return;

        //  If spoilers is null, return.
        if (spoilers.Length < 1)
            return;

        //  If spoiler index is -1, return.
        if (spoilerIndex == -1)
            return;

        //  Painting all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].UpdatePaint(newColor);

        }

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        spoilerIndex = -1;

        //  If sirens is null, return.
        if (spoilers == null)
            return;

        if (spoilers.Length < 1)
            return;

        //  Disabling all spoilers.
        for (int i = 0; i < spoilers.Length; i++) {

            if (spoilers[i] != null)
                spoilers[i].gameObject.SetActive(false);

        }

    }

}

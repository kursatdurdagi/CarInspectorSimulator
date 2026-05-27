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
/// Manager for all upgradable sirens.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Siren Manager")]
public class RCCP_VehicleUpgrade_SirenManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// All sirens.
    /// </summary>
    [Tooltip("All available siren upgrade prefabs on this vehicle.")]
    public RCCP_VehicleUpgrade_Siren[] sirens;

    /// <summary>
    /// Last selected siren index.
    /// </summary>
    [Tooltip("Currently equipped siren index (-1 means no siren).")]
    [Min(-1)] public int sirenIndex = -1;

    /// <summary>
    /// Initializes the siren manager by disabling all sirens, then enabling the saved siren from the loadout.
    /// </summary>
    public void Initialize() {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(false);

        }

        //  Getting index of the selected siren.
        sirenIndex = Loadout.siren;

        //  If siren index is -1, return.
        if (sirenIndex == -1)
            return;

        //  If index is not -1, enable the corresponding siren.
        if (sirens[sirenIndex] != null)
            sirens[sirenIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Finds and caches all child RCCP_VehicleUpgrade_Siren components, including inactive ones.
    /// </summary>
    public void GetAllSirens() {

        sirens = GetComponentsInChildren<RCCP_VehicleUpgrade_Siren>(true);

    }

    /// <summary>
    /// Deactivates all siren GameObjects on the vehicle.
    /// </summary>
    public void DisableAll() {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(false);

        }

    }

    /// <summary>
    /// Activates all siren GameObjects on the vehicle.
    /// </summary>
    public void EnableAll() {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Enabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(true);

        }

    }

    /// <summary>
    /// Unlocks the target index and saves it.
    /// </summary>
    /// <param name="index">Index of the siren to equip (-1 to remove all sirens).</param>
    public void Upgrade(int index) {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Index.
        sirenIndex = index;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(false);

        }

        //  If index is not -1, enable the corresponding siren.
        if (sirenIndex != -1 && sirens[sirenIndex] != null)
            sirens[sirenIndex].gameObject.SetActive(true);

        //  Refreshing the loadout.
        Refresh(this);

        //  Saving the loadout.
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Unlocks the target index and saves it.
    /// </summary>
    /// <param name="index">Index of the siren to equip (-1 to remove all sirens).</param>
    public void UpgradeWithoutSave(int index) {

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Index.
        sirenIndex = index;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(false);

        }

        //  If index is not -1, enable the corresponding siren.
        if (sirenIndex != -1 && sirens[sirenIndex] != null)
            sirens[sirenIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        sirenIndex = -1;

        //  If sirens is null, return.
        if (sirens == null)
            return;

        //  If sirens is null, return.
        if (sirens.Length < 1)
            return;

        //  Disabling all sirens.
        for (int i = 0; i < sirens.Length; i++) {

            if (sirens[i] != null)
                sirens[i].gameObject.SetActive(false);

        }

    }

}

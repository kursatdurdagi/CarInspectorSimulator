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
/// Manager for upgradable decals.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Decal Manager")]
public class RCCP_VehicleUpgrade_DecalManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// Decal painters.
    /// </summary>
    [Header("Decal Renderers")]
    [Tooltip("Decal projector for the front of the vehicle.")]
    public RCCP_VehicleUpgrade_Decal decalFront;

    /// <summary>
    /// Decal painters.
    /// </summary>
    [Tooltip("Decal projector for the back of the vehicle.")]
    public RCCP_VehicleUpgrade_Decal decalBack;

    /// <summary>
    /// Decal painters.
    /// </summary>
    [Tooltip("Decal projector for the left side of the vehicle.")]
    public RCCP_VehicleUpgrade_Decal decalLeft;

    /// <summary>
    /// Decal painters.
    /// </summary>
    [Tooltip("Decal projector for the right side of the vehicle.")]
    public RCCP_VehicleUpgrade_Decal decalRight;

    /// <summary>
    /// Indexes of decals.
    /// </summary>
    [Header("Decal Indexes")]
    [Tooltip("Selected decal material index for the front (-1 means none).")]
    [Min(-1)] public int index_decalFront = -1;

    /// <summary>
    /// Indexes of decals.
    /// </summary>
    [Tooltip("Selected decal material index for the back (-1 means none).")]
    [Min(-1)] public int index_decalBack = -1;

    /// <summary>
    /// Indexes of decals.
    /// </summary>
    [Tooltip("Selected decal material index for the left side (-1 means none).")]
    [Min(-1)] public int index_decalLeft = -1;

    /// <summary>
    /// Indexes of decals.
    /// </summary>
    [Tooltip("Selected decal material index for the right side (-1 means none).")]
    [Min(-1)] public int index_decalRight = -1;

    /// <summary>
    /// Decal materials.
    /// </summary>
    [Header("Materials")]
    [Tooltip("Available decal materials that can be applied to the vehicle.")]
    public Material[] decals;

    /// <summary>
    /// Empty decal material.
    /// </summary>
    [Tooltip("Transparent placeholder material used when no decal is selected.")]
    public Material decal_Null;

    /// <summary>
    /// Initializes all decal projectors by resetting them to the null material, then applies saved decal materials from the loadout.
    /// </summary>
    public void Initialize() {

        //  If empty decal is null, return.
        if (decal_Null != null) {

            decalFront.SetDecal(decal_Null);
            decalBack.SetDecal(decal_Null);
            decalLeft.SetDecal(decal_Null);
            decalRight.SetDecal(decal_Null);

        }

        //  And then getting index values from the loadout. -1 means it's empty.
        index_decalFront = Loadout.decalIndexFront;
        index_decalBack = Loadout.decalIndexBack;
        index_decalLeft = Loadout.decalIndexLeft;
        index_decalRight = Loadout.decalIndexRight;

        decalFront.gameObject.SetActive(false);
        decalBack.gameObject.SetActive(false);
        decalLeft.gameObject.SetActive(false);
        decalRight.gameObject.SetActive(false);

        //  If decals is null, return.
        if (decals == null)
            return;

        //  If decals is null, return
        if (decals.Length < 1)
            return;

        //  If index is not -1, set material of the decal by the loadout.
        if (index_decalFront != -1) {

            decalFront.gameObject.SetActive(true);
            decalFront.SetDecal(decals[index_decalFront]);

        }

        if (index_decalBack != -1) {

            decalBack.gameObject.SetActive(true);
            decalBack.SetDecal(decals[index_decalBack]);

        }

        if (index_decalLeft != -1) {

            decalLeft.gameObject.SetActive(true);
            decalLeft.SetDecal(decals[index_decalLeft]);

        }

        if (index_decalRight != -1) {

            decalRight.gameObject.SetActive(true);
            decalRight.SetDecal(decals[index_decalRight]);

        }

    }

    /// <summary>
    /// Resets all decal projectors to the null material and deactivates their GameObjects.
    /// </summary>
    public void DisableAll() {

        //  If empty decal is null, return.
        if (decal_Null != null) {

            decalFront.SetDecal(decal_Null);
            decalBack.SetDecal(decal_Null);
            decalLeft.SetDecal(decal_Null);
            decalRight.SetDecal(decal_Null);

        }

        decalFront.gameObject.SetActive(false);
        decalBack.gameObject.SetActive(false);
        decalLeft.gameObject.SetActive(false);
        decalRight.gameObject.SetActive(false);

    }

    /// <summary>
    /// Resets all decal projectors to the null material and activates their GameObjects.
    /// </summary>
    public void EnableAll() {

        //  If empty decal is null, return.
        if (decal_Null != null) {

            decalFront.SetDecal(decal_Null);
            decalBack.SetDecal(decal_Null);
            decalLeft.SetDecal(decal_Null);
            decalRight.SetDecal(decal_Null);

        }

        decalFront.gameObject.SetActive(true);
        decalBack.gameObject.SetActive(true);
        decalLeft.gameObject.SetActive(true);
        decalRight.gameObject.SetActive(true);

    }

    /// <summary>
    /// Upgrades target decal index and saves it.
    /// </summary>
    /// <param name="location">Decal placement location (0 = front, 1 = back, 2 = left, 3 = right).</param>
    /// <param name="material">The decal material to apply.</param>
    public void Upgrade(int location, Material material) {

        //  Setting material depending on the location. 0 is front, 1 is back, 2 is left, and 3 is right.
        switch (location) {

            case 0:
                decalFront.gameObject.SetActive(true);
                decalFront.SetDecal(material);
                index_decalFront = FindMaterialIndex(material);
                break;

            case 1:
                decalBack.gameObject.SetActive(true);
                decalBack.SetDecal(material);
                index_decalBack = FindMaterialIndex(material);
                break;

            case 2:
                decalLeft.gameObject.SetActive(true);
                decalLeft.SetDecal(material);
                index_decalLeft = FindMaterialIndex(material);
                break;

            case 3:
                decalRight.gameObject.SetActive(true);
                decalRight.SetDecal(material);
                index_decalRight = FindMaterialIndex(material);
                break;

        }

        //  Refreshing the loadout.
        Refresh(this);

        //  Saving the loadout.
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Upgrades target decal index and saves it.
    /// </summary>
    /// <param name="location">Decal placement location (0 = front, 1 = back, 2 = left, 3 = right).</param>
    /// <param name="material">The decal material to apply.</param>
    public void UpgradeWithoutSave(int location, Material material) {

        //  Setting material depending on the location. 0 is front, 1 is back, 2 is left, and 3 is right.
        switch (location) {

            case 0:
                decalFront.gameObject.SetActive(true);
                decalFront.SetDecal(material);
                index_decalFront = FindMaterialIndex(material);
                break;

            case 1:
                decalBack.gameObject.SetActive(true);
                decalBack.SetDecal(material);
                index_decalBack = FindMaterialIndex(material);
                break;

            case 2:
                decalLeft.gameObject.SetActive(true);
                decalLeft.SetDecal(material);
                index_decalLeft = FindMaterialIndex(material);
                break;

            case 3:
                decalRight.gameObject.SetActive(true);
                decalRight.SetDecal(material);
                index_decalRight = FindMaterialIndex(material);
                break;

        }

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        //  If empty decal is null, return.
        if (decal_Null != null) {

            decalFront.SetDecal(decal_Null);
            decalBack.SetDecal(decal_Null);
            decalLeft.SetDecal(decal_Null);
            decalRight.SetDecal(decal_Null);

        }

        decalFront.gameObject.SetActive(false);
        decalBack.gameObject.SetActive(false);
        decalLeft.gameObject.SetActive(false);
        decalRight.gameObject.SetActive(false);

    }

    /// <summary>
    /// Finds index of the material.
    /// </summary>
    /// <param name="_material">The decal material to search for.</param>
    /// <returns>The index of the material in the decals array, or -1 if not found.</returns>
    private int FindMaterialIndex(Material _material) {

        int index = -1;

        if (decals != null) {

            for (int i = 0; i < decals.Length; i++) {

                if (decals[i] != null && Equals(decals[i], _material))
                    index = i;

            }

        }

        return index;

    }

}

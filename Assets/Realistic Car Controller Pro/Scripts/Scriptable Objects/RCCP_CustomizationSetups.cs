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

/// <summary>
/// ScriptableObject registry that holds prefab references for all customization UI panels (paint, wheels, upgrades, spoilers, sirens, decals, neons).
/// </summary>
public class RCCP_CustomizationSetups : ScriptableObject {

    /// <summary>Prefab for the suspension and handling customization UI panel.</summary>
    [Tooltip("Prefab for the suspension and handling customization UI panel.")]
    public GameObject customization;
    /// <summary>Prefab for the decal selection UI panel.</summary>
    [Tooltip("Prefab for the decal selection UI panel.")]
    public GameObject decals;
    /// <summary>Prefab for the neon underglow UI panel.</summary>
    [Tooltip("Prefab for the neon underglow customization UI panel.")]
    public GameObject neons;
    /// <summary>Prefab for the paint color selection UI panel.</summary>
    [Tooltip("Prefab for the paint color selection UI panel.")]
    public GameObject paints;
    /// <summary>Prefab for the siren/police light UI panel.</summary>
    [Tooltip("Prefab for the siren and police light UI panel.")]
    public GameObject sirens;
    /// <summary>Prefab for the spoiler selection UI panel.</summary>
    [Tooltip("Prefab for the spoiler selection UI panel.")]
    public GameObject spoilers;
    /// <summary>Prefab for the performance upgrade UI panel.</summary>
    [Tooltip("Prefab for the performance upgrade UI panel.")]
    public GameObject upgrades;
    /// <summary>Prefab for the wheel selection UI panel.</summary>
    [Tooltip("Prefab for the wheel selection UI panel.")]
    public GameObject wheels;

    #region singleton
    private static RCCP_CustomizationSetups instance;
    /// <summary>
    /// Singleton instance of the customization setups registry, loaded from Resources.
    /// </summary>
    public static RCCP_CustomizationSetups Instance { get { if (instance == null) instance = Resources.Load("RCCP_CustomizationSetups") as RCCP_CustomizationSetups; return instance; } }
    #endregion

}

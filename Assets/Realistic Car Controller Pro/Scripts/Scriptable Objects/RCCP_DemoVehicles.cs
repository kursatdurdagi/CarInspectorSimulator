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
/// All demo vehicles.
/// </summary>
public class RCCP_DemoVehicles : ScriptableObject {

    /// <summary>Unique instance identifier used for asset tracking.</summary>
    [Min(0), Tooltip("Unique instance identifier used for asset tracking.")]
    public int instanceId = 0;

    /// <summary>
    /// All spawnable vehicles.
    /// </summary>
    [Tooltip("Prefab array of all spawnable demo vehicles.")]
    public RCCP_CarController[] vehicles;

    #region singleton
    private static RCCP_DemoVehicles instance;
    /// <summary>Singleton instance of the demo vehicles registry, loaded from Resources.</summary>
    public static RCCP_DemoVehicles Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoVehicles") as RCCP_DemoVehicles; return instance; } }
    #endregion

}

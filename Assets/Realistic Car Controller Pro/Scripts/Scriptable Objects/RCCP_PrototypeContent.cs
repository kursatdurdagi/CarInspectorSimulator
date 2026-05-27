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
/// All prototype vehicles.
/// </summary>
public class RCCP_PrototypeContent : ScriptableObject {

    /// <summary>Unique instance identifier used for asset tracking.</summary>
    [Min(0), Tooltip("Unique instance identifier used for asset tracking.")]
    public int instanceId = 0;

    /// <summary>
    /// All spawnable vehicles.
    /// </summary>
    [Tooltip("Prefab array of all spawnable prototype vehicles.")]
    public RCCP_CarController[] vehicles;

    #region singleton
    private static RCCP_PrototypeContent instance;
    /// <summary>Singleton instance of the prototype content registry, loaded from Resources.</summary>
    public static RCCP_PrototypeContent Instance { get { if (instance == null) instance = Resources.Load("RCCP_PrototypeContent") as RCCP_PrototypeContent; return instance; } }
    #endregion

}

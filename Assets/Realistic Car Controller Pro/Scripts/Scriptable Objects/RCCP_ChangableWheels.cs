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
/// Changes wheels at runtime. It holds changable wheels as prefab in an array.
/// </summary>
[System.Serializable]
public class RCCP_ChangableWheels : ScriptableObject {

    #region singleton
    private static RCCP_ChangableWheels instance;
    /// <summary>Singleton instance of the changeable wheels configuration, loaded from Resources.</summary>
    public static RCCP_ChangableWheels Instance { get { if (instance == null) instance = Resources.Load("RCCP_ChangableWheels") as RCCP_ChangableWheels; return instance; } }
    #endregion

    /// <summary>Holds a reference to a wheel prefab that can be equipped on the vehicle.</summary>
    [System.Serializable]
    public class ChangableWheels {

        /// <summary>The wheel prefab GameObject to instantiate when this wheel set is equipped.</summary>
        [Tooltip("Wheel prefab to instantiate when this wheel set is equipped.")]
        public GameObject wheel;

    }

    /// <summary>
    /// All changable wheels.
    /// </summary>
    [Tooltip("All available wheel presets that can be equipped at runtime.")]
    public ChangableWheels[] wheels;

}



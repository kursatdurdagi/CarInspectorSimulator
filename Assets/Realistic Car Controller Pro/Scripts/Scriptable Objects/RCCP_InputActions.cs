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
using UnityEngine.InputSystem;


/// <summary>
/// RCCP InputAction.
/// </summary>
public class RCCP_InputActions : ScriptableObject {

    #region singleton
    private static RCCP_InputActions instance;
    /// <summary>Singleton instance of the input actions configuration, loaded from Resources.</summary>
    public static RCCP_InputActions Instance { get { if (instance == null) instance = Resources.Load("RCCP_InputActions") as RCCP_InputActions; return instance; } }
    #endregion

    /// <summary>Reference to the Input System actions asset defining all vehicle control bindings.</summary>
    [Tooltip("Input System actions asset defining all vehicle control bindings.")]
    public InputActionAsset inputActions;

}

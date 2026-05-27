//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Utility class for saving and loading Input System rebinding overrides to PlayerPrefs.
/// </summary>
[System.Serializable]
public class RCCP_RebindSaveLoad {

    /// <summary>
    /// Saves the current input action rebinding overrides to PlayerPrefs.
    /// </summary>
    public static void Save() {

        InputActionAsset actions = RCCP_InputActions.Instance.inputActions;

        var rebinds = actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);

    }

    /// <summary>
    /// Loads previously saved input action rebinding overrides from PlayerPrefs.
    /// </summary>
    public static void Load() {

        InputActionAsset actions = RCCP_InputActions.Instance.inputActions;
        var rebinds = PlayerPrefs.GetString("rebinds");

        if (!string.IsNullOrEmpty(rebinds))
            actions.LoadBindingOverridesFromJson(rebinds);

    }

}

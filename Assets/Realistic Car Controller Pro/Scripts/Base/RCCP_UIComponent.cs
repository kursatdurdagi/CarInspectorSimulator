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
/// Base class for all RCCP UI components. Provides cached access to RCCP settings and scene manager singletons.
/// </summary>
public abstract class RCCP_UIComponent : MonoBehaviour {

    /// <summary>
    /// Cached reference to the runtime RCCP settings singleton.
    /// </summary>
    public RCCP_Settings RCCPSettings {

        get {

            if (_RCCPSettings == null)
                _RCCPSettings = RCCP_RuntimeSettings.RCCPSettingsInstance;

            return _RCCPSettings;

        }

    }
    private RCCP_Settings _RCCPSettings;

    /// <summary>
    /// Cached reference to the RCCP scene manager singleton.
    /// </summary>
    public RCCP_SceneManager RCCPSceneManager {

        get {

            if (_RCCSceneManager == null)
                _RCCSceneManager = RCCP_SceneManager.Instance;

            return _RCCSceneManager;

        }

    }
    private RCCP_SceneManager _RCCSceneManager;

}

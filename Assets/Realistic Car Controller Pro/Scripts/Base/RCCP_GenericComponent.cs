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
/// Base class for non-vehicle components such as managers, cameras, and scene utilities. Provides cached access to RCCP singletons.
/// </summary>
public abstract class RCCP_GenericComponent : MonoBehaviour {

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
    /// Cached reference to the runtime ground materials singleton.
    /// </summary>
    public RCCP_GroundMaterials RCCPGroundMaterials {

        get {

            if (_RCCPGroundMaterials == null)
                _RCCPGroundMaterials = RCCP_RuntimeSettings.RCCPGroundMaterialsInstance;

            return _RCCPGroundMaterials;

        }

    }
    private RCCP_GroundMaterials _RCCPGroundMaterials;

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

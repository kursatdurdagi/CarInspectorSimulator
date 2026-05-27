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
/// All removable demo content.
/// </summary>
public class RCCP_DemoContent : ScriptableObject {

    /// <summary>
    /// Unique instance identifier used for asset tracking.
    /// </summary>
    [Min(0), Tooltip("Unique instance identifier used for asset tracking.")]
    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoContent instance;
    /// <summary>
    /// Singleton instance of the demo content registry, loaded from Resources.
    /// </summary>
    public static RCCP_DemoContent Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoContent") as RCCP_DemoContent; return instance; } }
    #endregion

    /// <summary>
    /// When true, suppresses the prompt asking whether to install or remove demo content.
    /// </summary>
    [Tooltip("When enabled, suppresses the prompt asking whether to install or remove demo content.")]
    public bool dontAskDemoContent = false;
    /// <summary>
    /// Array of removable demo content assets (scenes, prefabs, materials) that can be cleaned from the project.
    /// </summary>
    [Tooltip("Removable demo assets (scenes, prefabs, materials) that can be cleaned from the project.")]
    public Object[] content;

    /// <summary>
    /// Built-in render pipeline shader content package.
    /// </summary>
    [Tooltip("Built-in render pipeline shader content package.")]
    public Object builtinShadersContent;
    /// <summary>
    /// Universal Render Pipeline shader content package.
    /// </summary>
    [Tooltip("Universal Render Pipeline shader content package.")]
    public Object URPShadersContent;
    /// <summary>
    /// High Definition Render Pipeline shader content package.
    /// </summary>
    [Tooltip("High Definition Render Pipeline shader content package.")]
    public Object HDRPShadersContent;

}

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
/// All addon packages.
/// </summary>
public class RCCP_AddonPackages : ScriptableObject {

    #region singleton
    private static RCCP_AddonPackages instance;
    /// <summary>
    /// Singleton instance of the addon packages registry, loaded from Resources.
    /// </summary>
    public static RCCP_AddonPackages Instance { get { if (instance == null) instance = Resources.Load("RCCP_AddonPackages") as RCCP_AddonPackages; return instance; } }
    #endregion

    /// <summary>
    /// Installer package for RCCP demo content scenes and assets.
    /// </summary>
    [Tooltip("Installer package for RCCP demo content scenes and assets.")]
    public Object demoPackage;
    /// <summary>
    /// Installer package for BoneCracker Games shared assets (character controller, UI).
    /// </summary>
    [Tooltip("Installer package for BCG shared assets (character controller, UI).")]
    public Object BCGSharedAssets;
    /// <summary>
    /// Installer package for Photon PUN2 multiplayer integration.
    /// </summary>
    [Tooltip("Installer package for Photon PUN2 multiplayer integration.")]
    public Object PhotonPUN2;
    /// <summary>
    /// Installer package for ProFlare lens flare integration.
    /// </summary>
    [Tooltip("Installer package for ProFlare lens flare integration.")]
    public Object ProFlare;
    /// <summary>
    /// Installer package for Mirror networking integration.
    /// </summary>
    [Tooltip("Installer package for Mirror networking integration.")]
    public Object mirror;
    /// <summary>
    /// Installer package for Realistic Traffic Controller integration.
    /// </summary>
    [Tooltip("Installer package for Realistic Traffic Controller integration.")]
    public Object RTC;

    /// <summary>
    /// Installer package for built-in render pipeline shaders.
    /// </summary>
    [Tooltip("Installer package for built-in render pipeline shaders.")]
    public Object builtinShaders;
    /// <summary>
    /// Installer package for URP shaders compatible with Unity 2021.
    /// </summary>
    [Tooltip("Installer package for URP shaders (Unity 2021).")]
    public Object URPShaders_2021;
    /// <summary>
    /// Installer package for URP shaders compatible with Unity 2022.
    /// </summary>
    [Tooltip("Installer package for URP shaders (Unity 2022).")]
    public Object URPShaders_2022;
    /// <summary>
    /// Installer package for URP shaders compatible with Unity 2023.
    /// </summary>
    [Tooltip("Installer package for URP shaders (Unity 2023).")]
    public Object URPShaders_2023;
    /// <summary>
    /// Installer package for URP shaders compatible with Unity 6.
    /// </summary>
    [Tooltip("Installer package for URP shaders (Unity 6).")]
    public Object URPShaders_6;
    /// <summary>
    /// Installer package for HDRP shaders compatible with Unity 2022.
    /// </summary>
    [Tooltip("Installer package for HDRP shaders (Unity 2022).")]
    public Object HDRPShaders_2022;
    /// <summary>
    /// Installer package for HDRP shaders compatible with Unity 2023.
    /// </summary>
    [Tooltip("Installer package for HDRP shaders (Unity 2023).")]
    public Object HDRPShaders_2023;
    /// <summary>
    /// Installer package for HDRP shaders compatible with Unity 6.
    /// </summary>
    [Tooltip("Installer package for HDRP shaders (Unity 6).")]
    public Object HDRPShaders_6;

    /// <summary>
    /// Installer package for the HDRP Volume Profile prefab.
    /// </summary>
    [Tooltip("Installer package for the HDRP Volume Profile prefab.")]
    public Object HDRPVolumeProfile;

    /// <summary>
    /// Returns the asset database path for the given Unity Object reference.
    /// </summary>
    /// <param name="pathObject">The Unity Object to resolve the asset path for.</param>
    /// <returns>The asset path string relative to the project root.</returns>
    public string GetAssetPath(Object pathObject) {

        string path = UnityEditor.AssetDatabase.GetAssetPath(pathObject);
        return path;

    }

}

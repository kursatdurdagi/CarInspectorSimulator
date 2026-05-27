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
/// All demo scenes.
/// </summary>
public class RCCP_DemoScenes : ScriptableObject {

    /// <summary>
    /// Unique instance identifier used for asset tracking.
    /// </summary>
    [Min(0), Tooltip("Unique instance identifier used for asset tracking.")]
    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoScenes instance;
    /// <summary>
    /// Singleton instance of the demo scenes registry, loaded from Resources.
    /// </summary>
    public static RCCP_DemoScenes Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoScenes") as RCCP_DemoScenes; return instance; } }
    #endregion

    /// <summary>
    /// Scene asset reference for the prototype testing scene.
    /// </summary>
    [Tooltip("Scene asset for the prototype testing scene.")]
    public Object demo_protototype;

    /// <summary>
    /// Scene asset reference for the all-in-one city demo.
    /// </summary>
    [Tooltip("Scene asset for the all-in-one city demo.")]
    public Object city_AIO;

    /// <summary>
    /// Scene asset reference for the city driving demo.
    /// </summary>
    [Tooltip("Scene asset for the city driving demo.")]
    public Object demo_City;

    /// <summary>
    /// Scene asset reference for the vehicle selection demo.
    /// </summary>
    [Tooltip("Scene asset for the vehicle selection demo.")]
    public Object demo_CarSelection;

    /// <summary>
    /// Scene asset reference for the blank API testing scene.
    /// </summary>
    [Tooltip("Scene asset for the blank API testing scene.")]
    public Object demo_APIBlank;

    /// <summary>
    /// Scene asset reference for the blank mobile controls demo.
    /// </summary>
    [Tooltip("Scene asset for the blank mobile controls demo.")]
    public Object demo_BlankMobile;

    /// <summary>
    /// Scene asset reference for the vehicle damage demo.
    /// </summary>
    [Tooltip("Scene asset for the vehicle damage demo.")]
    public Object demo_Damage;

    /// <summary>
    /// Scene asset reference for the vehicle customization demo.
    /// </summary>
    [Tooltip("Scene asset for the vehicle customization demo.")]
    public Object demo_Customization;

    /// <summary>
    /// Scene asset reference for the input override demo.
    /// </summary>
    [Tooltip("Scene asset for the input override demo.")]
    public Object demo_OverrideInputs;

    /// <summary>
    /// Scene asset reference for the vehicle transport demo.
    /// </summary>
    [Tooltip("Scene asset for the vehicle transport demo.")]
    public Object demo_Transport;

    /// <summary>
    /// Scene asset reference for the city AI traffic demo.
    /// </summary>
    [Tooltip("Scene asset for the city AI traffic demo.")]
    public Object demo_City_AI;

    /// <summary>
    /// Scene asset reference for the city with traffic demo.
    /// </summary>
    [Tooltip("Scene asset for the city with traffic demo.")]
    public Object demo_CityWithTraffic;

    /// <summary>
    /// Scene asset reference for the stability test demo.
    /// </summary>
    [Tooltip("Scene asset for the stability test demo.")]
    public Object demo_StabilityTest;

    /// <summary>
    /// Build path for the prototype testing scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the prototype scene.")]
    public string path_demo_protototype;

    /// <summary>
    /// Build path for the all-in-one city demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the all-in-one city scene.")]
    public string path_city_AIO;

    /// <summary>
    /// Build path for the city driving demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the city driving scene.")]
    public string path_demo_City;

    /// <summary>
    /// Build path for the vehicle selection demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the vehicle selection scene.")]
    public string path_demo_CarSelection;

    /// <summary>
    /// Build path for the blank API testing scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the blank API scene.")]
    public string path_demo_APIBlank;

    /// <summary>
    /// Build path for the blank mobile controls demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the blank mobile scene.")]
    public string path_demo_BlankMobile;

    /// <summary>
    /// Build path for the vehicle damage demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the damage demo scene.")]
    public string path_demo_Damage;

    /// <summary>
    /// Build path for the vehicle customization demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the customization scene.")]
    public string path_demo_Customization;

    /// <summary>
    /// Build path for the input override demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the input override scene.")]
    public string path_demo_OverrideInputs;

    /// <summary>
    /// Build path for the vehicle transport demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the transport demo scene.")]
    public string path_demo_Transport;

    /// <summary>
    /// Build path for the city with traffic demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the city with traffic scene.")]
    public string path_demo_CityWithTraffic;

    /// <summary>
    /// Build path for the city AI traffic demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the city AI traffic scene.")]
    public string path_demo_CityWithAI;

    /// <summary>
    /// Build path for the stability test demo scene.
    /// </summary>
    [Tooltip("Cached build-settings path for the stability test scene.")]
    public string path_demo_StabilityTest;

    /// <summary>
    /// Resolves and caches the build-settings path for each assigned demo scene asset.
    /// </summary>
    public void GetPaths() {

        if (demo_protototype != null)
            path_demo_protototype = RCCP_GetAssetPath.GetAssetPath(demo_protototype);
        else
            path_demo_protototype = "";

        if (city_AIO != null)
            path_city_AIO = RCCP_GetAssetPath.GetAssetPath(city_AIO);
        else
            path_city_AIO = "";

        if (demo_City != null)
            path_demo_City = RCCP_GetAssetPath.GetAssetPath(demo_City);
        else
            path_demo_City = "";

        if (demo_CarSelection != null)
            path_demo_CarSelection = RCCP_GetAssetPath.GetAssetPath(demo_CarSelection);
        else
            path_demo_CarSelection = "";

        if (demo_APIBlank != null)
            path_demo_APIBlank = RCCP_GetAssetPath.GetAssetPath(demo_APIBlank);
        else
            path_demo_APIBlank = "";

        if (demo_BlankMobile != null)
            path_demo_BlankMobile = RCCP_GetAssetPath.GetAssetPath(demo_BlankMobile);
        else
            path_demo_BlankMobile = "";

        if (demo_Damage != null)
            path_demo_Damage = RCCP_GetAssetPath.GetAssetPath(demo_Damage);
        else
            path_demo_Damage = "";

        if (demo_Customization != null)
            path_demo_Customization = RCCP_GetAssetPath.GetAssetPath(demo_Customization);
        else
            path_demo_Customization = "";

        if (demo_OverrideInputs != null)
            path_demo_OverrideInputs = RCCP_GetAssetPath.GetAssetPath(demo_OverrideInputs);
        else
            path_demo_OverrideInputs = "";

        if (demo_Transport != null)
            path_demo_Transport = RCCP_GetAssetPath.GetAssetPath(demo_Transport);
        else
            path_demo_Transport = "";

        if (demo_CityWithTraffic != null)
            path_demo_CityWithTraffic = RCCP_GetAssetPath.GetAssetPath(demo_CityWithTraffic);
        else
            path_demo_CityWithTraffic = "";

        if (demo_City_AI != null)
            path_demo_CityWithAI = RCCP_GetAssetPath.GetAssetPath(demo_City_AI);
        else
            path_demo_CityWithAI = "";

        if (demo_StabilityTest != null)
            path_demo_StabilityTest = RCCP_GetAssetPath.GetAssetPath(demo_StabilityTest);
        else
            path_demo_StabilityTest = "";

    }

    /// <summary>
    /// Clears all scene asset references and resets all cached paths to empty strings.
    /// </summary>
    public void Clean() {

        city_AIO = null;
        demo_City = null;
        demo_CarSelection = null;
        demo_APIBlank = null;
        demo_BlankMobile = null;
        demo_Damage = null;
        demo_Customization = null;
        demo_OverrideInputs = null;
        demo_Transport = null;
        demo_CityWithTraffic = null;
        demo_StabilityTest = null;

        path_demo_protototype = "";
        path_city_AIO = "";
        path_demo_City = "";
        path_demo_CarSelection = "";
        path_demo_APIBlank = "";
        path_demo_BlankMobile = "";
        path_demo_Damage = "";
        path_demo_Customization = "";
        path_demo_OverrideInputs = "";
        path_demo_Transport = "";
        path_demo_CityWithTraffic = "";
        path_demo_CityWithAI = "";
        path_demo_StabilityTest = "";

    }

}

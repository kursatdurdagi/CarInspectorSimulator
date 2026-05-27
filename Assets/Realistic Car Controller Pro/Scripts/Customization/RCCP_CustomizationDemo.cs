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
/// Customization demo used in the demo scene. Enables disables cameras and canvases.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Customization Demo")]
public class RCCP_CustomizationDemo : RCCP_GenericComponent {

    private static RCCP_CustomizationDemo instance;

    /// <summary>
    /// Instance of the class.
    /// </summary>
    public static RCCP_CustomizationDemo Instance {

        get {

#if !UNITY_2022_1_OR_NEWER
            if (instance == null)
                instance = FindObjectOfType<RCCP_CustomizationDemo>();
#else
            if (instance == null)
                instance = FindAnyObjectByType<RCCP_CustomizationDemo>();
#endif

            return instance;

        }

    }

    /// <summary>
    /// Current target vehicle to mod.
    /// </summary>
    [Tooltip("Vehicle currently being customized (assigned at runtime).")]
    public RCCP_CarController vehicle;

    /// <summary>
    /// Showroom camera to show the target vehicle while customizing.
    /// </summary>
    [Tooltip("Orbiting showroom camera activated during customization.")]
    public RCCP_ShowroomCamera showroomCamera;

    /// <summary>
    /// RCCP Camera.
    /// </summary>
    [Tooltip("Main RCCP camera disabled during customization mode.")]
    public RCCP_Camera RCCCamera;

    /// <summary>
    /// Modification UI Canvas.
    /// </summary>
    [Tooltip("UI canvas containing the customization controls.")]
    public GameObject modificationCanvas;

    /// <summary>
    /// Modification location that will be used to transport the target vehicle.
    /// </summary>
    [Tooltip("Transform where the vehicle is teleported for customization.")]
    public Transform location;

    /// <summary>Enables the customization UI for the specified vehicle.</summary>
    /// <param name="vehicle">The vehicle to enable customization for.</param>
    public void EnableCustomization(RCCP_CarController carController) {

        vehicle = carController;

        if (RCCCamera)
            RCCCamera.gameObject.SetActive(false);

        if (showroomCamera)
            showroomCamera.gameObject.SetActive(true);

        if (modificationCanvas)
            modificationCanvas.SetActive(true);

        if (location)
            RCCP.Transport(vehicle, location.position, location.rotation);

        RCCP.SetControl(vehicle, false);

    }

    /// <summary>Disables the customization UI and exits customization mode.</summary>
    public void DisableCustomization() {

        if (RCCCamera)
            RCCCamera.gameObject.SetActive(true);

        if (showroomCamera)
            showroomCamera.gameObject.SetActive(false);

        if (modificationCanvas)
            modificationCanvas.SetActive(false);

        if (vehicle)
            RCCP.SetControl(vehicle, true);

        vehicle = null;

    }

}

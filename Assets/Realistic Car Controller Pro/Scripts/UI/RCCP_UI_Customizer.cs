//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// RCCP UI Canvas that manages the event systems, panels, gauges, images and texts related to the vehicle and player.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Customizer")]
public class RCCP_UI_Customizer : RCCP_UIComponent {

    /// <summary>
    /// UI panel containing vehicle paint color options.
    /// </summary>
    [Header("Customization Panels")]
    [Tooltip("Panel containing vehicle paint color options.")]
    public GameObject paints;        //  Painting panel.
    /// <summary>
    /// UI panel containing wheel selection options.
    /// </summary>
    [Tooltip("Panel containing wheel selection options.")]
    public GameObject wheels;        //  Wheels panel.
    /// <summary>
    /// UI panel containing suspension, camber, and other customization sliders.
    /// </summary>
    [Tooltip("Panel containing suspension, camber, and handling sliders.")]
    public GameObject customization;      //  Customization panel.
    /// <summary>
    /// UI panel containing engine, brake, handling, and speed upgrade options.
    /// </summary>
    [Tooltip("Panel containing engine, brake, handling, and speed upgrades.")]
    public GameObject upgrades;      //  Upgrades panel.
    /// <summary>
    /// UI panel containing spoiler selection options.
    /// </summary>
    [Tooltip("Panel containing spoiler selection options.")]
    public GameObject spoilers;       //  Spoilers panel.
    /// <summary>
    /// UI panel containing siren and police light options.
    /// </summary>
    [Tooltip("Panel containing siren and police light options.")]
    public GameObject sirens;     //  Sirens panel.
    /// <summary>
    /// UI panel containing decal selection and placement options.
    /// </summary>
    [Tooltip("Panel containing decal selection and placement options.")]
    public GameObject decals;     //  Decals panel.
    /// <summary>
    /// UI panel containing neon underglow options.
    /// </summary>
    [Tooltip("Panel containing neon underglow lighting options.")]
    public GameObject neons;     //  Neons panel.

    /// <summary>
    /// Button that opens the paint customization panel.
    /// </summary>
    [Header("Customization Buttons")]
    [Tooltip("Button that opens the paint customization panel.")]
    public Button paintsButton;        //  Painting button.
    /// <summary>
    /// Button that opens the wheel customization panel.
    /// </summary>
    [Tooltip("Button that opens the wheel selection panel.")]
    public Button wheelsButton;        //  Wheels button.
    /// <summary>
    /// Button that opens the suspension and handling customization panel.
    /// </summary>
    [Tooltip("Button that opens the suspension and handling panel.")]
    public Button customizationButton;      //  Customization button.
    /// <summary>
    /// Button that opens the performance upgrades panel.
    /// </summary>
    [Tooltip("Button that opens the performance upgrades panel.")]
    public Button upgradesButton;      //  Upgrades button.
    /// <summary>
    /// Button that opens the spoiler selection panel.
    /// </summary>
    [Tooltip("Button that opens the spoiler selection panel.")]
    public Button spoilersButton;       //  Spoilers button.
    /// <summary>
    /// Button that opens the siren selection panel.
    /// </summary>
    [Tooltip("Button that opens the siren selection panel.")]
    public Button sirensButton;     //  Sirens button.
    /// <summary>
    /// Button that opens the decal customization panel.
    /// </summary>
    [Tooltip("Button that opens the decal customization panel.")]
    public Button decalsButton;     //  Decals button.
    /// <summary>
    /// Button that opens the neon underglow panel.
    /// </summary>
    [Tooltip("Button that opens the neon underglow panel.")]
    public Button neonsButton;     //  Neons button.

    /// <summary>
    /// Closes all customization panels, then opens the specified panel.
    /// </summary>
    /// <param name="activeMenu">The customization panel GameObject to activate.</param>
    public void OpenCustomizationPanel(GameObject activeMenu) {

        CloseCustomizationPanels();

        if (activeMenu)
            activeMenu.SetActive(true);

    }

    /// <summary>
    /// Deactivates all customization sub-panels.
    /// </summary>
    public void CloseCustomizationPanels() {

        if (paints)
            paints.SetActive(false);

        if (wheels)
            wheels.SetActive(false);

        if (customization)
            customization.SetActive(false);

        if (upgrades)
            upgrades.SetActive(false);

        if (spoilers)
            spoilers.SetActive(false);

        if (sirens)
            sirens.SetActive(false);

        if (decals)
            decals.SetActive(false);

        if (neons)
            neons.SetActive(false);

    }

    private void Update() {

        if (paintsButton)
            paintsButton.interactable = false;

        if (wheelsButton)
            wheelsButton.interactable = false;

        if (customizationButton)
            customizationButton.interactable = false;

        if (upgradesButton)
            upgradesButton.interactable = false;

        if (spoilersButton)
            spoilersButton.interactable = false;

        if (sirensButton)
            sirensButton.interactable = false;

        if (decalsButton)
            decalsButton.interactable = false;

        if (neonsButton)
            neonsButton.interactable = false;

        if (!RCCPSceneManager)
            return;

        if (!RCCPSceneManager.activePlayerVehicle)
            return;

        if (!RCCPSceneManager.activePlayerVehicle.Customizer)
            return;

        if (paintsButton)
            paintsButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.PaintManager;

        if (wheelsButton)
            wheelsButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.WheelManager;

        if (customizationButton)
            customizationButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.CustomizationManager;

        if (upgradesButton)
            upgradesButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.UpgradeManager;

        if (spoilersButton)
            spoilersButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.SpoilerManager;

        if (sirensButton)
            sirensButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.SirenManager;

        if (decalsButton)
            decalsButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.DecalManager;

        if (neonsButton)
            neonsButton.interactable = RCCPSceneManager.activePlayerVehicle.Customizer.NeonManager;

    }

}

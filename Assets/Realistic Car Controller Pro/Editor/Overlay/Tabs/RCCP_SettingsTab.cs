//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Settings tab content for RCCP Scene View Overlay.
/// Provides quick access to RCCP configuration, scene setup, and useful links.
/// </summary>
public class RCCP_SettingsTab : IRCCP_OverlayContent {

    #region Variables

    private VisualElement rootElement;
    private VisualElement settingsContainer;

    #endregion

    #region Interface Implementation

    public VisualElement CreateContent(string searchQuery) {

        rootElement = new VisualElement();
        rootElement.name = "rccp-settings-tab";
        rootElement.AddToClassList("rccp-scene-tools-settings");
        rootElement.style.flexGrow = 1;
        rootElement.style.flexShrink = 1;
        rootElement.style.height = Length.Percent(100);

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Settings container.
        settingsContainer = new VisualElement();
        settingsContainer.name = "settings-container";
        settingsContainer.AddToClassList("rccp-scene-tools-settings__list");
        settingsContainer.style.paddingBottom = 20 * scale;

        // Create scroll view.
        ScrollView scrollView = RCCP_SceneToolsUI.CreateScrollView("rccp-scene-tools-settings__scroll");
        scrollView.Add(settingsContainer);
        rootElement.Add(scrollView);

        // Refresh settings.
        RefreshSettings(searchQuery);

        return rootElement;

    }

    public void OnUpdate() {

        // No periodic update needed.

    }

    public void OnDestroy() {

        // Cleanup if needed.

    }

    #endregion

    #region Settings Display

    private void RefreshSettings(string searchQuery) {

        settingsContainer.Clear();

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Configuration Section.
        CreateSection("Configuration", settingsContainer);

        // RCCP Settings Asset.
        CreateSettingCard(
            "RCCP Settings",
            RCCP_Settings.Instance != null ? "Select to edit" : "Not found",
            "\ud83d\udcdd",
            RCCP_Settings.Instance != null
                ? SettingCardTone.Success
                : SettingCardTone.Error,
            () => {
                if (RCCP_Settings.Instance != null) {
                    Selection.activeObject = RCCP_Settings.Instance;
                    EditorGUIUtility.PingObject(RCCP_Settings.Instance);
                }
            },
            searchQuery
        );

        // Ground Materials.
        CreateSettingCard(
            "Ground Materials",
            "Surface friction database",
            "\ud83c\udf33",
            SettingCardTone.Green,
            () => {
                var groundMats = RCCP_GroundMaterials.Instance;
                if (groundMats != null) {
                    Selection.activeObject = groundMats;
                    EditorGUIUtility.PingObject(groundMats);
                }
            },
            searchQuery
        );

        // Input Actions.
        CreateSettingCard(
            "Input Actions",
            "Input system configuration",
            "\ud83c\udfae",
            SettingCardTone.Purple,
            () => {
                var inputActions = RCCP_InputActions.Instance;
                if (inputActions != null) {
                    Selection.activeObject = inputActions;
                    EditorGUIUtility.PingObject(inputActions);
                }
            },
            searchQuery
        );

        // Changeable Wheels.
        CreateSettingCard(
            "Changeable Wheels",
            "Wheel presets database",
            "\u2699\ufe0f",
            SettingCardTone.Purple,
            () => {
                var wheels = RCCP_ChangableWheels.Instance;
                if (wheels != null) {
                    Selection.activeObject = wheels;
                    EditorGUIUtility.PingObject(wheels);
                }
            },
            searchQuery
        );

        // Demo Vehicles.
        CreateSettingCard(
            "Demo Vehicles",
            "Vehicle prefabs registry",
            "\ud83d\ude97",
            SettingCardTone.Info,
            () => {
                var demoVehicles = RCCP_DemoVehicles.Instance;
                if (demoVehicles != null) {
                    Selection.activeObject = demoVehicles;
                    EditorGUIUtility.PingObject(demoVehicles);
                }
            },
            searchQuery
        );

        // Scene Setup Section.
        CreateSection("Scene Setup", settingsContainer);

        // Scene Manager.
        bool hasSceneManager = Object.FindAnyObjectByType<RCCP_SceneManager>(FindObjectsInactive.Include) != null;
        CreateSettingCard(
            "Scene Manager",
            hasSceneManager ? "\u2705 In scene (Required)" : "Not in scene (Required)",
            "\ud83c\udfac",
            hasSceneManager ? SettingCardTone.Success : SettingCardTone.Warning,
            () => RCCP_EditorWindows.CreateRCCPSceneManager(),
            searchQuery
        );

        // Skidmarks Manager.
        bool hasSkidmarks = Object.FindAnyObjectByType<RCCP_SkidmarksManager>(FindObjectsInactive.Include) != null;
        CreateSettingCard(
            "Skidmarks Manager",
            hasSkidmarks ? "\u2705 In scene (Optional)" : "Not in scene (Optional)",
            "\ud83d\udee4\ufe0f",
            hasSkidmarks ? SettingCardTone.Success : SettingCardTone.Green,
            () => RCCP_EditorWindows.CreateRCCPSkidmarksManager(),
            searchQuery
        );

        // RCCP Camera.
        bool hasCamera = Object.FindAnyObjectByType<RCCP_Camera>(FindObjectsInactive.Include) != null;
        CreateSettingCard(
            "RCCP Camera",
            hasCamera ? "\u2705 In scene (Required)" : "Not in scene (Required)",
            "\ud83c\udfa5",
            hasCamera ? SettingCardTone.Success : SettingCardTone.Warning,
            () => RCCP_EditorWindows.CreateRCCCamera(),
            searchQuery
        );

        // UI Canvas.
        bool hasCanvas = Object.FindAnyObjectByType<RCCP_UIManager>(FindObjectsInactive.Include) != null;
        CreateSettingCard(
            "UI Canvas",
            hasCanvas ? "\u2705 In scene (Optional)" : "Not in scene (Optional)",
            "\ud83d\udcbb",
            hasCanvas ? SettingCardTone.Success : SettingCardTone.Green,
            () => RCCP_EditorWindows.CreateRCCUICanvas(),
            searchQuery
        );

        // Quick Links Section.
        CreateSection("Quick Links", settingsContainer);

        // Documentation.
        CreateSettingCard(
            "Documentation",
            "Open RCCP documentation",
            "\ud83d\udcd6",
            SettingCardTone.Info,
            () => Application.OpenURL("https://www.bonecrackergames.com/realistic-car-controller-pro/"),
            searchQuery
        );

        // YouTube Tutorials.
        CreateSettingCard(
            "YouTube Tutorials",
            "Watch tutorial videos",
            "\u25b6\ufe0f",
            SettingCardTone.Info,
            () => Application.OpenURL(RCCP_AssetPaths.YTVideos),
            searchQuery
        );

        // Support.
        CreateSettingCard(
            "Support",
            "Get help and support",
            "\ud83d\udcac",
            SettingCardTone.Brown,
            () => Application.OpenURL("https://www.bonecrackergames.com/contact/"),
            searchQuery
        );

        // Asset Store.
        CreateSettingCard(
            "Asset Store",
            "View on Unity Asset Store",
            "\ud83d\udecd\ufe0f",
            SettingCardTone.Brown,
            () => Application.OpenURL(RCCP_AssetPaths.assetStorePath),
            searchQuery
        );

    }

    private void CreateSection(string title, VisualElement parent) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        Label sectionLabel = RCCP_SceneToolsUI.CreateSectionTitle(title, scale);
        parent.Add(sectionLabel);

    }

    private void CreateSettingCard(string title, string description, string icon, SettingCardTone tone, System.Action onClick, string searchQuery) {

        // Filter check.
        if (!string.IsNullOrEmpty(searchQuery)) {
            string lowerSearch = searchQuery.ToLower();
            if (!title.ToLower().Contains(lowerSearch) && !description.ToLower().Contains(lowerSearch)) {
                return;
            }
        }

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-scene-tools-setting-card");
        card.AddToClassList("rccp-scene-tools-setting-card--clickable");
        ApplySettingCardTone(card, tone);
        card.style.paddingTop = 6 * scale;
        card.style.paddingBottom = 6 * scale;
        card.style.paddingLeft = 8 * scale;
        card.style.paddingRight = 8 * scale;
        card.style.marginBottom = 2 * scale;

        card.RegisterCallback<ClickEvent>(evt => onClick());
        RCCP_SceneToolsUI.EnableHoverClass(card, "rccp-scene-tools-setting-card--hover");

        // Icon.
        Label iconLabel = new Label(icon);
        iconLabel.AddToClassList("rccp-scene-tools-setting-card__icon");
        iconLabel.style.fontSize = 16 * scale;
        iconLabel.style.marginRight = 8 * scale;
        iconLabel.style.width = 22 * scale;
        card.Add(iconLabel);

        // Info container.
        VisualElement infoContainer = new VisualElement();
        infoContainer.AddToClassList("rccp-scene-tools-setting-card__info");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-scene-tools-setting-card__title");
        titleLabel.style.fontSize = 9 * scale;
        infoContainer.Add(titleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-scene-tools-setting-card__description");
        descLabel.style.fontSize = 8 * scale;
        infoContainer.Add(descLabel);

        card.Add(infoContainer);

        // Arrow.
        Label arrowLabel = new Label("\u25b6");
        arrowLabel.AddToClassList("rccp-scene-tools-setting-card__arrow");
        arrowLabel.style.fontSize = 10 * scale;
        card.Add(arrowLabel);

        settingsContainer.Add(card);

    }

    private static void ApplySettingCardTone(VisualElement card, SettingCardTone tone) {

        card.RemoveFromClassList("rccp-scene-tools-setting-card--success");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--error");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--warning");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--info");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--purple");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--green");
        card.RemoveFromClassList("rccp-scene-tools-setting-card--brown");

        switch (tone) {
            case SettingCardTone.Success:
                card.AddToClassList("rccp-scene-tools-setting-card--success");
                break;
            case SettingCardTone.Error:
                card.AddToClassList("rccp-scene-tools-setting-card--error");
                break;
            case SettingCardTone.Warning:
                card.AddToClassList("rccp-scene-tools-setting-card--warning");
                break;
            case SettingCardTone.Info:
                card.AddToClassList("rccp-scene-tools-setting-card--info");
                break;
            case SettingCardTone.Purple:
                card.AddToClassList("rccp-scene-tools-setting-card--purple");
                break;
            case SettingCardTone.Green:
                card.AddToClassList("rccp-scene-tools-setting-card--green");
                break;
            case SettingCardTone.Brown:
                card.AddToClassList("rccp-scene-tools-setting-card--brown");
                break;
        }

    }

    #endregion

    private enum SettingCardTone {
        Success,
        Error,
        Warning,
        Info,
        Purple,
        Green,
        Brown
    }

}

#endif

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
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// EditorWindow version of RCCP Scene Tools.
/// Provides the same functionality as the Overlay for Unity versions that don't support Overlays.
/// </summary>
public class RCCP_SceneToolsWindow : EditorWindow {

    #region Variables

    private RCCP_SceneToolsController controller;

    // UI Elements.
    private VisualElement rootContainer;
    private VisualElement contentContainer;
    private VisualElement tabContainer;
    private VisualElement headerButtons;
    private TextField searchField;
    private Button clearSearchButton;

    #endregion

    #region Window Management

    [MenuItem("Tools/BoneCracker Games/Realistic Car Controller Pro/Scene Tools/Open Window", false, 70)]
    public static void ShowWindow() {

        var window = GetWindow<RCCP_SceneToolsWindow>();
        window.titleContent = new GUIContent("RCCP Scene Tools");
        window.minSize = new Vector2(350, 400);
        window.Show();

    }

    #endregion

    #region Lifecycle

    private void OnEnable() {

        // Initialize controller.
        controller = new RCCP_SceneToolsController("RCCP_Window");

        // Build UI.
        BuildUI();

        // Register update callback.
        EditorApplication.update += OnUpdate;

    }

    private void OnDisable() {

        EditorApplication.update -= OnUpdate;

        if (controller != null) {
            controller.Cleanup();
        }

    }

    #endregion

    #region UI Creation

    private void BuildUI() {

        float scale = RCCP_SceneToolsUI.GetScaleFactor();

        // Clear existing content.
        rootVisualElement.Clear();

        // Footer text varies based on AI Assistant availability.
        string footerText = RCCP_SceneToolsController.IsAIAssistantInstalled
            ? "RCCP Scene Tools + AI Assistant"
            : "RCCP Scene Tools";

        // Create root container from shared shell UXML.
        rootContainer = RCCP_SceneToolsUI.CreateShell(
            scale,
            footerText,
            out headerButtons,
            out searchField,
            out clearSearchButton,
            out tabContainer,
            out contentContainer);
        rootContainer.name = "rccp-window-root";

        // Apply default styling.
        RCCP_SceneToolsUI.ApplyDefaultStyling(rootContainer, false);

        RCCP_SceneToolsUI.BindHeaderButtons(headerButtons, null, null, false, false);
        RCCP_SceneToolsUI.BindSearchControls(controller, searchField, clearSearchButton, (query) => {
            LoadTabContent();
        });

        RCCP_SceneToolsUI.PopulateTabBar(tabContainer, controller, (index) => {
            controller.CurrentTabIndex = index;
            RCCP_SceneToolsUI.UpdateTabButtons(tabContainer, index);
            LoadTabContent();
        });

        // Initial Content Load.
        LoadTabContent();

        // Add root to window.
        rootVisualElement.Add(rootContainer);

    }

    private void LoadTabContent() {

        contentContainer.Clear();
        var provider = controller.GetCurrentContentProvider();

        if (provider != null) {
            VisualElement content = provider.CreateContent(controller.CurrentSearchQuery);
            if (content != null) {
                content.style.flexGrow = 1;
                content.style.flexShrink = 1;
                content.style.height = Length.Percent(100);
                contentContainer.Add(content);
            }
        }

    }

    #endregion

    #region Update Loop

    private void OnUpdate() {

        if (controller != null) {
            controller.Update();
        }

    }

    #endregion

}

#endif

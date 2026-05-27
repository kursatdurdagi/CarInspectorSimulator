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
using System.Linq;

/// <summary>
/// Shared UI builder for RCCP Scene Tools.
/// Handles visual construction and styling for both Overlay and Window versions.
/// </summary>
public static class RCCP_SceneToolsUI {

    #region Styling

    // Resolved from RCCP_AssetUtilities.BasePath so the overlay still finds its UXML/USS
    // when users import RCCP under a non-standard folder.
    private static readonly string SHELL_PATH = RCCP_AssetUtilities.BasePath + "Editor/Overlay/UI/rccp_scene_tools_shell.uxml";
    private static readonly string THEME_PATH = RCCP_AssetUtilities.BasePath + "Editor/Overlay/UI/rccp_scene_tools_theme_theme.uss";
    private static readonly string STYLES_PATH = RCCP_AssetUtilities.BasePath + "Editor/Overlay/UI/rccp_scene_tools.uss";

    /// <summary>
    /// Gets the current scale factor for UI elements (dynamic DPI-based).
    /// </summary>
    public static float GetScaleFactor() {
        return Mathf.Clamp(EditorGUIUtility.pixelsPerPoint * 0.8f, 0.75f, 1f);
    }

    public static void AttachStyleSheets(VisualElement element) {

        var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(THEME_PATH);
        if (theme != null)
            element.styleSheets.Add(theme);

        var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLES_PATH);
        if (styles != null)
            element.styleSheets.Add(styles);

    }

    public static void ApplyDefaultStyling(VisualElement element, bool isFloating = false) {

        element.AddToClassList("rccp-scene-tools-root");

        if (isFloating)
            element.AddToClassList("rccp-scene-tools-root--floating");

        // Personal skin override — USS theme is dark-only, so light skin needs inline colors.
        if (!EditorGUIUtility.isProSkin) {
            element.style.backgroundColor = new Color(0.86f, 0.86f, 0.86f);
            element.style.color = Color.black;
            element.style.borderTopColor = new Color(0.6f, 0.6f, 0.6f);
            element.style.borderBottomColor = new Color(0.6f, 0.6f, 0.6f);
            element.style.borderLeftColor = new Color(0.6f, 0.6f, 0.6f);
            element.style.borderRightColor = new Color(0.6f, 0.6f, 0.6f);
        }

    }

    private static void ApplyActiveTabStyle(Button tabButton) {

        tabButton.AddToClassList("rccp-tab-active");

        // Clear any leftover inline styles so USS class takes full control.
        tabButton.style.backgroundColor = StyleKeyword.Null;
        tabButton.style.color = StyleKeyword.Null;
        tabButton.style.borderBottomWidth = StyleKeyword.Null;
        tabButton.style.borderBottomColor = StyleKeyword.Null;

    }

    #endregion

    #region UI Components

    public static VisualElement CreateShell(
        float scale,
        string footerText,
        out VisualElement headerButtons,
        out TextField searchField,
        out Button clearButton,
        out VisualElement tabContainer,
        out VisualElement contentContainer) {

        headerButtons = null;
        searchField = null;
        clearButton = null;
        tabContainer = null;
        contentContainer = null;

        var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SHELL_PATH);

        if (tree == null) {
            Debug.LogError($"[RCCP] Scene Tools shell UXML not found at {SHELL_PATH}");
            return new VisualElement();
        }

        var shell = tree.Instantiate();
        AttachStyleSheets(shell);

        var shellRoot = shell.Q<VisualElement>("shell-root") ?? shell;
        shellRoot.style.flexGrow = 1;
        shellRoot.style.flexShrink = 1;

        var headerRow = shell.Q<VisualElement>("header-row");
        if (headerRow != null) {
            headerRow.style.height = 30 * scale;
            headerRow.style.minHeight = 30 * scale;
            headerRow.style.maxHeight = 30 * scale;
            headerRow.style.paddingRight = 6 * scale;
            headerRow.style.paddingLeft = 6 * scale;
            headerRow.style.paddingTop = 4 * scale;
            headerRow.style.paddingBottom = 4 * scale;
        }

        var titleLabel = shell.Q<Label>("header-title");
        if (titleLabel != null)
            titleLabel.style.fontSize = 12 * scale;

        var searchContainer = shell.Q<VisualElement>("search-container");
        if (searchContainer != null) {
            searchContainer.style.height = 32 * scale;
            searchContainer.style.minHeight = 32 * scale;
            searchContainer.style.maxHeight = 32 * scale;
            searchContainer.style.paddingTop = 5 * scale;
            searchContainer.style.paddingBottom = 5 * scale;
            searchContainer.style.paddingLeft = 6 * scale;
            searchContainer.style.paddingRight = 6 * scale;
        }

        var searchIcon = shell.Q<Label>("search-icon");
        if (searchIcon != null) {
            searchIcon.style.marginRight = 6 * scale;
            searchIcon.style.fontSize = 13 * scale;
            searchIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        searchField = shell.Q<TextField>("search-field");
        if (searchField != null) {
            searchField.style.flexGrow = 1;
            searchField.style.fontSize = 11 * scale;
            searchField.style.borderTopLeftRadius = 4;
            searchField.style.borderTopRightRadius = 4;
            searchField.style.borderBottomLeftRadius = 4;
            searchField.style.borderBottomRightRadius = 4;
        }

        clearButton = shell.Q<Button>("clear-button");
        if (clearButton != null) {
            clearButton.style.width = 20 * scale;
            clearButton.style.height = 20 * scale;
            clearButton.style.fontSize = 12 * scale;
            clearButton.style.marginLeft = 4 * scale;
            clearButton.style.borderTopLeftRadius = 3;
            clearButton.style.borderTopRightRadius = 3;
            clearButton.style.borderBottomLeftRadius = 3;
            clearButton.style.borderBottomRightRadius = 3;
        }

        tabContainer = shell.Q<VisualElement>("tab-container");
        if (tabContainer != null) {
            tabContainer.style.height = 32 * scale;
            tabContainer.style.minHeight = 32 * scale;
            tabContainer.style.maxHeight = 32 * scale;
            tabContainer.style.marginBottom = 4 * scale;
        }

        contentContainer = shell.Q<VisualElement>("content-container");
        if (contentContainer != null) {
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexShrink = 1;
            contentContainer.style.overflow = Overflow.Hidden;
            contentContainer.style.paddingTop = 6 * scale;
            contentContainer.style.paddingBottom = 6 * scale;
            contentContainer.style.paddingLeft = 6 * scale;
            contentContainer.style.paddingRight = 6 * scale;
        }

        var footerLabel = shell.Q<Label>("footer-label");
        if (footerLabel != null) {
            footerLabel.text = footerText;
            footerLabel.style.fontSize = 9 * scale;
        }

        headerButtons = shell.Q<VisualElement>("header-buttons");
        return shell;

    }

    public static void BindHeaderButtons(VisualElement headerButtons, System.Action onClose, System.Action onDockToggle = null, bool isDocked = false, bool showDockButton = true) {

        if (headerButtons == null)
            return;

        headerButtons.Clear();

        if (onClose != null) {
            Button closeButton = RCCP_OverlayDockingHelper.CreateCloseButtonInline(onClose);
            headerButtons.Add(closeButton);
        }

        if (showDockButton && onDockToggle != null) {
            Button dockToggleButton = RCCP_OverlayDockingHelper.CreateDockToggleButtonInline(onDockToggle, isDocked);
            dockToggleButton.name = "dock-toggle-button";
            headerButtons.Add(dockToggleButton);
        }

        Button helpButton = RCCP_OverlayDockingHelper.CreateHelpButtonInline();
        headerButtons.Add(helpButton);

    }

    public static void BindSearchControls(RCCP_SceneToolsController controller, TextField searchField, Button clearButton, System.Action<string> onSearchChanged) {

        if (searchField == null)
            return;

        searchField.value = controller.CurrentSearchQuery;
        searchField.tooltip = "Search RCCP vehicles and components...";
        searchField.textEdition.placeholder = "Search vehicles, tabs, settings";
        searchField.textEdition.hidePlaceholderOnFocus = true;

        if (clearButton != null) {
            clearButton.tooltip = "Clear search";
            clearButton.AddToClassList("rccp-scene-tools-search__clear");
            clearButton.style.display = string.IsNullOrEmpty(controller.CurrentSearchQuery) ? DisplayStyle.None : DisplayStyle.Flex;
            clearButton.clicked += () => {
                controller.ClearSearch();
                searchField.SetValueWithoutNotify("");

                if (clearButton != null)
                    clearButton.style.display = DisplayStyle.None;

                onSearchChanged?.Invoke("");
            };
        }

        searchField.RegisterValueChangedCallback(evt => {
            controller.CurrentSearchQuery = evt.newValue;
            controller.AddToSearchHistory(evt.newValue);

            if (clearButton != null)
                clearButton.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;

            onSearchChanged?.Invoke(evt.newValue);
        });

    }

    public static ScrollView CreateScrollView(string className = null) {

        ScrollView scrollView = new ScrollView();

        if (!string.IsNullOrEmpty(className))
            scrollView.AddToClassList(className);

        scrollView.style.flexGrow = 1;
        scrollView.style.flexShrink = 1;
        scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        return scrollView;

    }

    public static Label CreateSectionTitle(string title, float scale) {

        Label sectionLabel = new Label(title);
        sectionLabel.AddToClassList("rccp-scene-tools-section-title");
        sectionLabel.style.fontSize = 10 * scale;
        sectionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        sectionLabel.style.marginTop = 8 * scale;
        sectionLabel.style.marginBottom = 4 * scale;
        sectionLabel.style.paddingLeft = 4 * scale;

        return sectionLabel;

    }

    public static VisualElement CreateActionBar(float scale, float marginTop, float paddingTop, string className = null) {

        VisualElement buttonsContainer = new VisualElement();
        buttonsContainer.AddToClassList("rccp-scene-tools-action-bar");

        if (!string.IsNullOrEmpty(className))
            buttonsContainer.AddToClassList(className);

        buttonsContainer.style.marginTop = marginTop * scale;
        buttonsContainer.style.paddingTop = paddingTop * scale;

        return buttonsContainer;

    }

    public static void EnableHoverClass(VisualElement element, string hoverClass) {

        element.RegisterCallback<MouseEnterEvent>(evt => element.AddToClassList(hoverClass));
        element.RegisterCallback<MouseLeaveEvent>(evt => element.RemoveFromClassList(hoverClass));

    }

    public static void PopulateTabBar(VisualElement tabContainer, RCCP_SceneToolsController controller, System.Action<int> onTabSelected) {

        if (tabContainer == null)
            return;

        float scale = GetScaleFactor();
        tabContainer.Clear();
        tabContainer.style.flexDirection = FlexDirection.Row;
        tabContainer.style.borderBottomWidth = 1;
        tabContainer.style.borderBottomColor = new Color(0.07f, 0.07f, 0.07f); // Matches --color-separator

        for (int i = 0; i < controller.TabNames.Length; i++) {

            int tabIndex = i;
            Button tabButton = new Button(() => onTabSelected(tabIndex));
            tabButton.text = controller.TabNames[i];
            tabButton.name = $"rccp-tab-{controller.TabNames[i].ToLower().Replace(" ", "-")}";
            tabButton.AddToClassList("rccp-scene-tools-tabbar__button");

            tabButton.style.flexGrow = 1;
            tabButton.style.paddingTop = 6 * scale;
            tabButton.style.paddingBottom = 6 * scale;
            tabButton.style.paddingLeft = 2 * scale;
            tabButton.style.paddingRight = 2 * scale;
            tabButton.style.fontSize = 10 * scale;
            tabButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            tabButton.style.borderTopLeftRadius = 4;
            tabButton.style.borderTopRightRadius = 4;

            if (i == controller.CurrentTabIndex)
                ApplyActiveTabStyle(tabButton);

            tabContainer.Add(tabButton);

        }

    }

    public static void UpdateTabButtons(VisualElement tabContainer, int currentTabIndex) {

        var tabButtons = tabContainer.Query<Button>().ToList();

        for (int i = 0; i < tabButtons.Count; i++) {

            if (i == currentTabIndex) {
                ApplyActiveTabStyle(tabButtons[i]);
            } else {
                tabButtons[i].RemoveFromClassList("rccp-tab-active");

                // Clear inline overrides so USS classes take effect.
                tabButtons[i].style.backgroundColor = StyleKeyword.Null;
                tabButtons[i].style.color = StyleKeyword.Null;
                tabButtons[i].style.borderBottomWidth = StyleKeyword.Null;
                tabButtons[i].style.borderBottomColor = StyleKeyword.Null;
            }

        }

    }
    #endregion

}

#endif

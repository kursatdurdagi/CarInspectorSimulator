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
/// Static UI builder for the RCCP Welcome Window.
/// Provides factory methods for creating styled UI Toolkit components.
/// </summary>
public static class RCCP_WelcomeWindowUI {

    // Resolved from RCCP_AssetUtilities.BasePath so the window still finds its UXML/USS
    // when users import RCCP under a non-standard folder (e.g. Assets/CCDS/Realistic Car Controller Pro/).
    private static readonly string UXML_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/WelcomeWindow/rccp_welcome_window.uxml";
    private static readonly string THEME_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/rccp_orange_theme.uss";
    private static readonly string STYLES_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/WelcomeWindow/rccp_welcome_window.uss";

    #region Shell

    /// <summary>
    /// Creates the main window shell from UXML and attaches stylesheets.
    /// </summary>
    public static VisualElement CreateShell(
        out VisualElement tabContainer,
        out VisualElement contentContainer,
        out VisualElement graceBanner,
        out VisualElement modalOverlay,
        out VisualElement modalContainer) {

        var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML_PATH);

        VisualElement root;

        if (tree != null) {
            root = tree.Instantiate();
        } else {
            root = new VisualElement();
            root.Add(new Label("Welcome Window UXML not found."));
        }

        // tree.Instantiate() returns a TemplateContainer with no flex-grow.
        // Force it to fill the window so the footer pins to the bottom on sparse tabs.
        root.style.flexGrow = 1;

        AttachStyleSheets(root);

        tabContainer = root.Q<VisualElement>("tab-container");
        contentContainer = root.Q<VisualElement>("content-container");
        graceBanner = root.Q<VisualElement>("grace-banner");
        modalOverlay = root.Q<VisualElement>("modal-overlay");
        modalContainer = root.Q<VisualElement>("modal-container");

        // Set banner texture.
        var banner = root.Q<VisualElement>("banner");
        if (banner != null) {
            var tex = Resources.Load<Texture2D>("Editor Icons/RCCP_Banner");
            if (tex != null)
                banner.style.backgroundImage = tex;
        }

        // Set footer version text.
        var footerCenter = root.Q<Label>("footer-center");
        if (footerCenter != null)
            footerCenter.text = $"Realistic Car Controller Pro {RCCP_Version.version}";

        return root;

    }

    /// <summary>
    /// Attaches theme and component stylesheets to a root element.
    /// </summary>
    public static void AttachStyleSheets(VisualElement element) {

        var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(THEME_PATH);
        var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLES_PATH);

        if (theme != null) element.styleSheets.Add(theme);
        if (styles != null) element.styleSheets.Add(styles);

    }

    #endregion

    #region Tab Bar

    /// <summary>
    /// Unity built-in editor icons per tab, index-aligned with RCCP_WelcomeWindowController.tabNames.
    /// </summary>
    private static readonly string[] tabIconNames = {
        "d_BuildSettings.Standalone", // Welcome (avoids play-mode connotation of d_PlayButton)
        "d_SceneAsset Icon",          // Demos
        "d_Package Manager",   // Addons
        "d_Shader Icon",       // Shaders
        "d_CustomTool",        // Keys (scripting symbols)
        "d_Refresh",           // Updates
        "d_Help",              // Docs
    };

    /// <summary>
    /// Populates a tab bar with buttons for each tab name. Each button contains
    /// a Unity built-in editor icon + a label, so the vertical sidebar layout
    /// can theme them separately.
    /// </summary>
    public static void PopulateTabBar(VisualElement tabContainer, string[] tabNames, int activeIndex, System.Action<int> onTabSelected) {

        // Guard against a missing tab-container — happens if UXML failed to load and CreateShell
        // returned a fallback root. Avoids cascading a "stylesheet not found" into a hard NRE.
        if (tabContainer == null)
            return;

        tabContainer.Clear();

        for (int i = 0; i < tabNames.Length; i++) {

            int index = i;
            Button tabButton = new Button(() => onTabSelected(index));
            tabButton.text = string.Empty;
            tabButton.AddToClassList("rccp-welcome-tabbar__button");

            var icon = new VisualElement();
            icon.AddToClassList("rccp-welcome-tabbar__icon");

            if (i < tabIconNames.Length) {

                var content = EditorGUIUtility.IconContent(tabIconNames[i]);

                if (content != null && content.image is Texture2D tex)
                    icon.style.backgroundImage = new StyleBackground(tex);

            }

            tabButton.Add(icon);

            var label = new Label(tabNames[i]);
            label.AddToClassList("rccp-welcome-tabbar__label");
            label.style.flexGrow = 1;
            tabButton.Add(label);

            if (i == activeIndex)
                tabButton.AddToClassList("rccp-welcome-tab-active");

            tabContainer.Add(tabButton);

        }

    }

    /// <summary>
    /// Updates tab bar button active states.
    /// </summary>
    public static void UpdateTabButtons(VisualElement tabContainer, int activeIndex) {

        int i = 0;

        foreach (var child in tabContainer.Children()) {

            if (child is Button btn) {

                btn.RemoveFromClassList("rccp-welcome-tab-active");

                if (i == activeIndex)
                    btn.AddToClassList("rccp-welcome-tab-active");

                i++;

            }

        }

    }

    #endregion

    #region Component Factories

    /// <summary>
    /// Creates a styled section container with optional title and body text.
    /// </summary>
    public static VisualElement CreateSection(string title = null, string body = null) {

        VisualElement section = new VisualElement();
        section.AddToClassList("rccp-welcome-section");

        if (!string.IsNullOrEmpty(title)) {
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList("rccp-welcome-section__title");
            section.Add(titleLabel);
        }

        if (!string.IsNullOrEmpty(body)) {
            Label bodyLabel = new Label(body);
            bodyLabel.AddToClassList("rccp-welcome-section__body");
            section.Add(bodyLabel);
        }

        return section;

    }

    /// <summary>
    /// Creates a subtitle label.
    /// </summary>
    public static Label CreateSubtitle(string text) {

        Label label = new Label(text);
        label.AddToClassList("rccp-welcome-section__subtitle");
        return label;

    }

    /// <summary>
    /// Creates a body text label.
    /// </summary>
    public static Label CreateBody(string text) {

        Label label = new Label(text);
        label.AddToClassList("rccp-welcome-section__body");
        return label;

    }

    /// <summary>
    /// Creates a numbered step component.
    /// </summary>
    public static VisualElement CreateStep(int number, string title, string description) {

        VisualElement step = new VisualElement();
        step.AddToClassList("rccp-welcome-step");

        Label numberLabel = new Label(number.ToString());
        numberLabel.AddToClassList("rccp-welcome-step__number");
        step.Add(numberLabel);

        VisualElement content = new VisualElement();
        content.AddToClassList("rccp-welcome-step__content");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-step__title");
        content.Add(titleLabel);

        if (!string.IsNullOrEmpty(description)) {
            Label descLabel = new Label(description);
            descLabel.AddToClassList("rccp-welcome-step__description");
            content.Add(descLabel);
        }

        step.Add(content);

        return step;

    }

    /// <summary>
    /// Creates a numbered step card (Getting Started grid cell).
    /// Renders as: badge header + title + description + action button, stacked vertically.
    /// Caller adds multiple cards to a row container styled with 'rccp-welcome-step-row'.
    /// </summary>
    public static VisualElement CreateStepCard(int number, string title, string description, string buttonText, System.Action onClick) {

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-welcome-step-card");

        VisualElement header = new VisualElement();
        header.AddToClassList("rccp-welcome-step-card__header");
        card.Add(header);

        Label badge = new Label(number.ToString());
        badge.AddToClassList("rccp-welcome-step-card__badge");
        header.Add(badge);

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-step-card__title");
        card.Add(titleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-welcome-step-card__desc");
        card.Add(descLabel);

        Button actionButton = new Button(onClick);
        actionButton.text = buttonText;
        actionButton.AddToClassList("rccp-welcome-step-card__button");
        card.Add(actionButton);

        return card;

    }

    /// <summary>
    /// Creates a vertical feature card for the Addons tab. Renders as:
    /// title + wrapped description + either a status pill (when installed)
    /// or a primary action button. Multiple cards wrap in a row container
    /// styled with 'rccp-welcome-feature-card-row'.
    /// </summary>
    public static VisualElement CreateFeatureCard(string title, string description, bool isInstalled, string buttonText, System.Action onClick, string buttonVariant = null) {

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-welcome-feature-card");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-feature-card__title");
        card.Add(titleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-welcome-feature-card__desc");
        card.Add(descLabel);

        if (isInstalled) {

            Label status = new Label("Installed");
            status.AddToClassList("rccp-welcome-feature-card__status");
            card.Add(status);

        } else {

            Button btn = new Button(onClick);
            btn.text = buttonText;
            btn.AddToClassList("rccp-welcome-feature-card__button");
            if (!string.IsNullOrEmpty(buttonVariant))
                btn.AddToClassList($"rccp-welcome-feature-card__button--{buttonVariant}");
            card.Add(btn);

        }

        return card;

    }

    /// <summary>
    /// Creates a lightweight scene card (Demos grid cell).
    /// Renders as: title + optional subtitle + "Open Scene" primary button.
    /// Caller adds multiple cards to a row container styled with 'rccp-welcome-scene-card-row'
    /// which wraps to two or three cards per line depending on window width.
    /// </summary>
    public static VisualElement CreateSceneCard(string title, string subtitle, System.Action onClick) {

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-welcome-scene-card");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-scene-card__title");
        card.Add(titleLabel);

        if (!string.IsNullOrEmpty(subtitle)) {
            Label subtitleLabel = new Label(subtitle);
            subtitleLabel.AddToClassList("rccp-welcome-scene-card__subtitle");
            card.Add(subtitleLabel);
        }

        Button btn = new Button(onClick);
        btn.text = "Open Scene";
        btn.AddToClassList("rccp-welcome-scene-card__button");
        card.Add(btn);

        return card;

    }

    /// <summary>
    /// Creates a styled button with an optional variant.
    /// </summary>
    public static Button CreateButton(string text, System.Action onClick, string variant = null) {

        Button button = new Button(onClick);
        button.text = text;
        button.AddToClassList("rccp-welcome-button");

        if (!string.IsNullOrEmpty(variant))
            button.AddToClassList($"rccp-welcome-button--{variant}");

        return button;

    }

    /// <summary>
    /// Creates a horizontal row of buttons.
    /// </summary>
    public static VisualElement CreateButtonRow(params (string text, System.Action onClick, string variant)[] buttons) {

        VisualElement row = new VisualElement();
        row.AddToClassList("rccp-welcome-button-row");

        foreach (var (text, onClick, variant) in buttons) {
            row.Add(CreateButton(text, onClick, variant));
        }

        return row;

    }

    /// <summary>
    /// Creates a help box with a severity level (info, warning, error, success).
    /// </summary>
    public static VisualElement CreateHelpBox(string message, string severity = "info") {

        VisualElement box = new VisualElement();
        box.AddToClassList("rccp-welcome-helpbox");
        box.AddToClassList($"rccp-welcome-helpbox--{severity}");

        Label text = new Label(message);
        text.AddToClassList("rccp-welcome-helpbox__text");
        box.Add(text);

        return box;

    }

    /// <summary>
    /// Creates a visual separator line.
    /// </summary>
    public static VisualElement CreateSeparator() {

        VisualElement sep = new VisualElement();
        sep.AddToClassList("rccp-welcome-separator");
        return sep;

    }

    /// <summary>
    /// Creates an addon card with title, description, status, and an action button.
    /// </summary>
    public static VisualElement CreateAddonCard(string title, string description, bool isInstalled, string buttonText, System.Action onClick) {

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-welcome-addon-card");

        VisualElement info = new VisualElement();
        info.AddToClassList("rccp-welcome-addon-card__info");

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-addon-card__title");
        info.Add(titleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-welcome-addon-card__description");
        info.Add(descLabel);

        card.Add(info);

        if (isInstalled) {

            Label status = new Label("Installed");
            status.AddToClassList("rccp-welcome-addon-card__status");
            status.AddToClassList("rccp-welcome-addon-card__status--installed");
            card.Add(status);

        } else {

            Button actionButton = CreateButton(buttonText, onClick, "primary");
            actionButton.style.flexShrink = 0;
            card.Add(actionButton);

        }

        return card;

    }

    /// <summary>
    /// Creates a version info label.
    /// </summary>
    public static Label CreateVersionLabel(string text) {

        Label label = new Label(text);
        label.AddToClassList("rccp-welcome-version");
        return label;

    }

    #endregion

}

#endif

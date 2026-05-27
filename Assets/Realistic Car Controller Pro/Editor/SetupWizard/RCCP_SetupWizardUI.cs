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
using UnityEditor.UIElements;

/// <summary>
/// Static UI builder for the RCCP Setup Wizard.
/// Provides factory methods for creating styled UI Toolkit components.
/// </summary>
public static class RCCP_SetupWizardUI {

    // Resolved from RCCP_AssetUtilities.BasePath so the wizard still finds its USS
    // when users import RCCP under a non-standard folder.
    private static readonly string THEME_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/rccp_orange_theme.uss";
    private static readonly string WELCOME_STYLES_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/WelcomeWindow/rccp_welcome_window.uss";
    private static readonly string WIZARD_STYLES_PATH = RCCP_AssetUtilities.BasePath + "Editor/UI/SetupWizard/rccp_setup_wizard.uss";

    #region Style Sheets

    /// <summary>
    /// Loads and attaches all three USS files (theme, welcome reuse, wizard-specific)
    /// to the given element.
    /// </summary>
    public static void AttachStyleSheets(VisualElement element) {

        var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(THEME_PATH);
        var welcomeStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(WELCOME_STYLES_PATH);
        var wizardStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(WIZARD_STYLES_PATH);

        if (theme != null) element.styleSheets.Add(theme);
        if (welcomeStyles != null) element.styleSheets.Add(welcomeStyles);
        if (wizardStyles != null) element.styleSheets.Add(wizardStyles);

    }

    #endregion

    #region Sidebar

    /// <summary>
    /// Builds the wizard sidebar: top logo tile (RCCP_Banner resource) + vertical list
    /// of step indicators reflecting completed / current / upcoming state.
    /// Caller must rebuild the sidebar on every step change so the highlight moves.
    /// </summary>
    public static VisualElement CreateSidebar(string[] stepTitles, int currentStep) {

        VisualElement sidebar = new VisualElement();
        sidebar.AddToClassList("rccp-wizard-sidebar");

        // Logo tile at the top — stays pinned, never scrolls.
        VisualElement banner = new VisualElement();
        banner.AddToClassList("rccp-wizard-sidebar-banner");
        var tex = Resources.Load<Texture2D>("Editor Icons/RCCP_Banner");
        if (tex != null)
            banner.style.backgroundImage = tex;
        sidebar.Add(banner);

        // Step indicators live inside a ScrollView so a docked-short window
        // (where Unity ignores minSize) can't let them push the main column's
        // footer off-screen. flex-grow:1 makes the scroll region absorb all
        // remaining sidebar height.
        ScrollView stepList = new ScrollView(ScrollViewMode.Vertical);
        stepList.style.flexGrow = 1;
        stepList.style.flexShrink = 1;
        sidebar.Add(stepList);

        for (int i = 0; i < stepTitles.Length; i++) {

            VisualElement indicator = new VisualElement();
            indicator.AddToClassList("rccp-wizard-step-indicator");

            if (i == currentStep)
                indicator.AddToClassList("rccp-wizard-step-indicator--current");
            else if (i < currentStep)
                indicator.AddToClassList("rccp-wizard-step-indicator--done");

            // Badge shows step number or "✓" when done.
            Label badge = new Label(i < currentStep ? "\u2713" : (i + 1).ToString());
            badge.AddToClassList("rccp-wizard-step-indicator__badge");
            indicator.Add(badge);

            Label label = new Label(stepTitles[i]);
            label.AddToClassList("rccp-wizard-step-indicator__label");
            indicator.Add(label);

            stepList.Add(indicator);

        }

        return sidebar;

    }

    #endregion

    #region Progress

    /// <summary>
    /// Creates a progress bar track with a fill element.
    /// Fill width is set as a percentage based on current step.
    /// </summary>
    public static VisualElement CreateProgressBar(int currentStep, int totalSteps) {

        VisualElement track = new VisualElement();
        track.AddToClassList("rccp-wizard-progress");

        VisualElement fill = new VisualElement();
        fill.AddToClassList("rccp-wizard-progress__fill");

        float percent = totalSteps > 1
            ? (currentStep / (float)(totalSteps - 1)) * 100f
            : 100f;

        fill.style.width = Length.Percent(percent);

        track.Add(fill);

        return track;

    }

    #endregion

    #region Header

    /// <summary>
    /// Creates the step header with step counter, title, and progress bar.
    /// </summary>
    public static VisualElement CreateStepHeader(int stepNumber, int totalSteps, string title) {

        VisualElement header = new VisualElement();
        header.AddToClassList("rccp-wizard-header");

        Label counter = new Label($"Step {stepNumber} / {totalSteps}");
        counter.AddToClassList("rccp-wizard-header__title");
        header.Add(counter);

        Label subtitle = new Label(title);
        subtitle.AddToClassList("rccp-wizard-header__subtitle");
        header.Add(subtitle);

        header.Add(CreateProgressBar(stepNumber - 1, totalSteps));

        return header;

    }

    #endregion

    #region Field Components

    /// <summary>
    /// Creates a horizontal field row with a label and an arbitrary field element.
    /// </summary>
    public static VisualElement CreateFieldRow(string label, VisualElement field) {

        VisualElement row = new VisualElement();
        row.AddToClassList("rccp-wizard-field-row");

        Label rowLabel = new Label(label);
        rowLabel.AddToClassList("rccp-wizard-field-row__label");
        row.Add(rowLabel);

        field.AddToClassList("rccp-wizard-field-row__field");
        row.Add(field);

        return row;

    }

    /// <summary>
    /// Creates a field row containing a Slider paired with a numeric FloatField.
    /// The two stay in sync; the FloatField clamps input to [min, max].
    /// Use this instead of Slider.showInputField (which renders too small in tight rows).
    /// </summary>
    public static VisualElement CreateSliderField(string label, float min, float max, float currentValue, System.Action<float> onChange) {

        float clampedInitial = Mathf.Clamp(currentValue, min, max);

        VisualElement container = new VisualElement();
        container.AddToClassList("rccp-wizard-slider-with-field");

        Slider slider = new Slider(null, min, max);
        slider.value = clampedInitial;
        slider.AddToClassList("rccp-wizard-slider-with-field__slider");

        FloatField field = new FloatField();
        field.value = clampedInitial;
        field.AddToClassList("rccp-wizard-slider-with-field__field");

        slider.RegisterValueChangedCallback(evt => {
            onChange?.Invoke(evt.newValue);
            if (!Mathf.Approximately(field.value, evt.newValue))
                field.SetValueWithoutNotify(evt.newValue);
        });

        field.RegisterValueChangedCallback(evt => {
            float clamped = Mathf.Clamp(evt.newValue, min, max);
            onChange?.Invoke(clamped);
            if (!Mathf.Approximately(slider.value, clamped))
                slider.SetValueWithoutNotify(clamped);
            if (!Mathf.Approximately(evt.newValue, clamped))
                field.SetValueWithoutNotify(clamped);
        });

        container.Add(slider);
        container.Add(field);

        return CreateFieldRow(label, container);

    }

    /// <summary>
    /// Creates a wheel slot row with a short label (e.g. "FL") and a GameObject ObjectField.
    /// </summary>
    public static VisualElement CreateWheelSlot(string label, GameObject currentValue, System.Action<GameObject> onChange) {

        VisualElement slot = new VisualElement();
        slot.AddToClassList("rccp-wizard-wheel-slot");

        Label slotLabel = new Label(label);
        slotLabel.AddToClassList("rccp-wizard-wheel-slot__label");
        slot.Add(slotLabel);

        ObjectField objectField = new ObjectField();
        objectField.objectType = typeof(GameObject);
        objectField.allowSceneObjects = true;
        objectField.value = currentValue;
        objectField.AddToClassList("rccp-wizard-wheel-slot__field");

        objectField.RegisterValueChangedCallback(evt => {
            onChange?.Invoke(evt.newValue as GameObject);
        });

        slot.Add(objectField);

        return slot;

    }

    /// <summary>
    /// Creates a toggle row with a label, toggle, and a description.
    /// </summary>
    public static VisualElement CreateToggleRow(string label, string description, bool value, System.Action<bool> onChange) {

        VisualElement row = new VisualElement();
        row.AddToClassList("rccp-wizard-toggle-row");

        Toggle toggle = new Toggle();
        toggle.value = value;
        toggle.label = "";

        toggle.RegisterValueChangedCallback(evt => {
            onChange?.Invoke(evt.newValue);
        });

        row.Add(toggle);

        Label toggleLabel = new Label(label);
        toggleLabel.AddToClassList("rccp-wizard-toggle-row__label");
        row.Add(toggleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-wizard-toggle-row__description");
        row.Add(descLabel);

        return row;

    }

    /// <summary>
    /// Creates a collider row with a toggle, mesh name, and volume text.
    /// </summary>
    public static VisualElement CreateColliderRow(string meshName, bool selected, string volumeText, System.Action<bool> onChange) {

        VisualElement row = new VisualElement();
        row.AddToClassList("rccp-wizard-collider-row");

        Toggle toggle = new Toggle();
        toggle.value = selected;
        toggle.label = "";

        toggle.RegisterValueChangedCallback(evt => {
            onChange?.Invoke(evt.newValue);
        });

        row.Add(toggle);

        Label nameLabel = new Label(meshName);
        nameLabel.AddToClassList("rccp-wizard-collider-row__name");
        row.Add(nameLabel);

        Label volumeLabel = new Label(volumeText);
        volumeLabel.AddToClassList("rccp-wizard-collider-row__volume");
        row.Add(volumeLabel);

        return row;

    }

    #endregion

    #region Summary

    /// <summary>
    /// Creates a clickable summary row with a label, value text, and a ">" arrow.
    /// </summary>
    public static VisualElement CreateSummaryRow(string label, string value, System.Action onClick) {

        VisualElement row = new VisualElement();
        row.AddToClassList("rccp-wizard-summary-row");

        Label rowLabel = new Label(label);
        rowLabel.AddToClassList("rccp-wizard-summary-row__label");
        row.Add(rowLabel);

        Label valueLabel = new Label(value);
        valueLabel.AddToClassList("rccp-wizard-summary-row__value");
        row.Add(valueLabel);

        Label arrow = new Label(">");
        arrow.AddToClassList("rccp-wizard-summary-row__arrow");
        row.Add(arrow);

        row.RegisterCallback<ClickEvent>(evt => {
            onClick?.Invoke();
        });

        return row;

    }

    #endregion

    #region Footer

    /// <summary>
    /// Creates the wizard footer with Back and Next/Finish buttons.
    /// </summary>
    public static VisualElement CreateFooter(System.Action onBack, System.Action onNext, bool canGoBack, bool canGoNext, bool isLastStep) {

        VisualElement footer = new VisualElement();
        footer.AddToClassList("rccp-wizard-footer");

        Button backButton = new Button(() => onBack?.Invoke());
        backButton.text = "Back";
        backButton.AddToClassList("rccp-welcome-button");
        backButton.SetEnabled(canGoBack);
        footer.Add(backButton);

        Button nextButton = new Button(() => onNext?.Invoke());
        nextButton.text = isLastStep ? "Finish Setup" : "Next";
        nextButton.AddToClassList("rccp-welcome-button");
        nextButton.AddToClassList("rccp-welcome-button--primary");
        nextButton.SetEnabled(canGoNext);
        footer.Add(nextButton);

        return footer;

    }

    #endregion

}

#endif

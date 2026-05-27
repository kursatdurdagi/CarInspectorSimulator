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
/// AI Assistant tab content for RCCP Scene View Overlay.
/// Provides quick launch buttons for each AI Assistant panel type.
/// This tab is only shown when RCCP AI Assistant package is installed.
/// </summary>
public class RCCP_AIAssistantTab : IRCCP_OverlayContent {

    #region Variables

    private VisualElement rootElement;
    private VisualElement buttonsContainer;

    #endregion

    #region Interface Implementation

    public VisualElement CreateContent(string searchQuery) {

        rootElement = new VisualElement();
        rootElement.name = "rccp-ai-assistant-tab";
        rootElement.AddToClassList("rccp-scene-tools-ai");
        rootElement.style.flexGrow = 1;
        rootElement.style.flexShrink = 1;
        rootElement.style.height = Length.Percent(100);

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Header.
        Label headerLabel = new Label("Quick Launch AI Assistant");
        headerLabel.AddToClassList("rccp-scene-tools-ai__title");
        headerLabel.style.fontSize = 10 * scale;
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        headerLabel.style.marginBottom = 8 * scale;
        headerLabel.style.marginTop = 6 * scale;
        rootElement.Add(headerLabel);

        // Description.
        Label descLabel = new Label("Select a panel to open the AI Assistant:");
        descLabel.AddToClassList("rccp-scene-tools-ai__description");
        descLabel.style.fontSize = 8 * scale;
        descLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        descLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        descLabel.style.marginBottom = 8 * scale;
        rootElement.Add(descLabel);

        // Buttons container.
        buttonsContainer = new VisualElement();
        buttonsContainer.name = "ai-buttons-container";
        buttonsContainer.AddToClassList("rccp-scene-tools-ai__list");
        buttonsContainer.style.paddingLeft = 6 * scale;
        buttonsContainer.style.paddingRight = 6 * scale;

        // Create scroll view.
        ScrollView scrollView = RCCP_SceneToolsUI.CreateScrollView("rccp-scene-tools-ai__scroll");
        scrollView.Add(buttonsContainer);
        rootElement.Add(scrollView);

        // Create panel buttons.
        CreatePanelButtons(searchQuery);

        return rootElement;

    }

    public void OnUpdate() {

        // No periodic update needed.

    }

    public void OnDestroy() {

        // Cleanup if needed.

    }

    #endregion

    #region Panel Buttons

    private void CreatePanelButtons(string searchQuery) {

        buttonsContainer.Clear();

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Vehicle Creation.
        CreatePanelButton(
            "Vehicle Creation",
            "Create a new RCCP vehicle from a 3D model",
            "\ud83d\ude97",
            AIPanelTone.Green,
            () => OpenAIAssistantPanel("VehicleCreation"),
            searchQuery
        );

        // Vehicle Customization.
        CreatePanelButton(
            "Vehicle Customization",
            "Modify an existing RCCP vehicle",
            "\ud83d\udd27",
            AIPanelTone.Blue,
            () => OpenAIAssistantPanel("VehicleCustomization"),
            searchQuery
        );

        // Behaviors.
        CreatePanelButton(
            "Behaviors",
            "Configure driving presets (Arcade, Drift, Simulation)",
            "\ud83c\udfae",
            AIPanelTone.Purple,
            () => OpenAIAssistantPanel("Behaviors"),
            searchQuery
        );

        // Wheels.
        CreatePanelButton(
            "Wheels",
            "Suspension, friction, camber and caster settings",
            "\u2699\ufe0f",
            AIPanelTone.Neutral,
            () => OpenAIAssistantPanel("Wheels"),
            searchQuery
        );

        // Audio.
        CreatePanelButton(
            "Audio",
            "Engine sound layers and audio configuration",
            "\ud83d\udd0a",
            AIPanelTone.Brown,
            () => OpenAIAssistantPanel("Audio"),
            searchQuery
        );

        // Lights.
        CreatePanelButton(
            "Lights",
            "Headlights, brake lights, and indicators",
            "\ud83d\udca1",
            AIPanelTone.Yellow,
            () => OpenAIAssistantPanel("Lights"),
            searchQuery
        );

        // Damage.
        CreatePanelButton(
            "Damage",
            "Mesh deformation and damage settings",
            "\ud83d\udca5",
            AIPanelTone.Red,
            () => OpenAIAssistantPanel("Damage"),
            searchQuery
        );

        // Diagnostics.
        CreatePanelButton(
            "Diagnostics",
            "Local vehicle checks (no AI required)",
            "\ud83d\udcca",
            AIPanelTone.Teal,
            () => OpenAIAssistantPanel("Diagnostics"),
            searchQuery
        );

        // Open Full Window button.
        CreateOpenWindowButton();

    }

    private void CreatePanelButton(string title, string description, string icon, AIPanelTone tone, System.Action onClick, string searchQuery) {

        // Filter check.
        if (!string.IsNullOrEmpty(searchQuery)) {
            string lowerSearch = searchQuery.ToLower();
            if (!title.ToLower().Contains(lowerSearch) && !description.ToLower().Contains(lowerSearch)) {
                return;
            }
        }

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-scene-tools-ai-card");
        card.style.flexDirection = FlexDirection.Row;
        card.style.paddingTop = 8 * scale;
        card.style.paddingBottom = 8 * scale;
        card.style.paddingLeft = 8 * scale;
        card.style.paddingRight = 8 * scale;
        card.style.marginBottom = 4 * scale;
        card.style.borderTopLeftRadius = 4;
        card.style.borderTopRightRadius = 4;
        card.style.borderBottomLeftRadius = 4;
        card.style.borderBottomRightRadius = 4;
        card.AddToClassList("rccp-scene-tools-ai-card--clickable");
        ApplyAIPanelTone(card, tone);

        RCCP_SceneToolsUI.EnableHoverClass(card, "rccp-scene-tools-ai-card--hover");
        card.RegisterCallback<ClickEvent>(evt => onClick());

        // Icon.
        Label iconLabel = new Label(icon);
        iconLabel.AddToClassList("rccp-scene-tools-ai-card__icon");
        iconLabel.style.fontSize = 20 * scale;
        iconLabel.style.marginRight = 8 * scale;
        iconLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        iconLabel.style.width = 28 * scale;
        card.Add(iconLabel);

        // Info container.
        VisualElement infoContainer = new VisualElement();
        infoContainer.AddToClassList("rccp-scene-tools-ai-card__info");
        infoContainer.style.flexGrow = 1;
        infoContainer.style.justifyContent = Justify.Center;

        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-scene-tools-ai-card__title");
        titleLabel.style.fontSize = 10 * scale;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 1 * scale;
        infoContainer.Add(titleLabel);

        Label descLabel = new Label(description);
        descLabel.AddToClassList("rccp-scene-tools-ai-card__description");
        descLabel.style.fontSize = 8 * scale;
        descLabel.style.color = EditorGUIUtility.isProSkin
            ? new Color(0.7f, 0.7f, 0.7f)
            : new Color(0.4f, 0.4f, 0.4f);
        descLabel.style.whiteSpace = WhiteSpace.Normal;
        infoContainer.Add(descLabel);

        card.Add(infoContainer);

        // Arrow indicator.
        Label arrowLabel = new Label("\u25b6");
        arrowLabel.AddToClassList("rccp-scene-tools-ai-card__arrow");
        arrowLabel.style.fontSize = 10 * scale;
        arrowLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        arrowLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        card.Add(arrowLabel);

        buttonsContainer.Add(card);

    }

    private void CreateOpenWindowButton() {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement separator = new VisualElement();
        separator.AddToClassList("rccp-scene-tools-ai__separator");
        separator.style.height = 1;
        separator.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        separator.style.marginTop = 10 * scale;
        separator.style.marginBottom = 10 * scale;
        buttonsContainer.Add(separator);

        Button openWindowButton = new Button(() => {
            OpenAIAssistantWindow();
        });
        openWindowButton.text = "Open Full AI Assistant Window";
        openWindowButton.AddToClassList("rccp-scene-tools-button");
        openWindowButton.AddToClassList("rccp-scene-tools-button--accent");
        openWindowButton.style.height = 28 * scale;
        openWindowButton.style.fontSize = 10 * scale;
        openWindowButton.style.borderTopLeftRadius = 4;
        openWindowButton.style.borderTopRightRadius = 4;
        openWindowButton.style.borderBottomLeftRadius = 4;
        openWindowButton.style.borderBottomRightRadius = 4;

        buttonsContainer.Add(openWindowButton);

    }

    private static void ApplyAIPanelTone(VisualElement card, AIPanelTone tone) {

        card.RemoveFromClassList("rccp-scene-tools-ai-card--green");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--blue");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--purple");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--neutral");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--brown");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--yellow");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--red");
        card.RemoveFromClassList("rccp-scene-tools-ai-card--teal");

        switch (tone) {
            case AIPanelTone.Green:
                card.AddToClassList("rccp-scene-tools-ai-card--green");
                break;
            case AIPanelTone.Blue:
                card.AddToClassList("rccp-scene-tools-ai-card--blue");
                break;
            case AIPanelTone.Purple:
                card.AddToClassList("rccp-scene-tools-ai-card--purple");
                break;
            case AIPanelTone.Neutral:
                card.AddToClassList("rccp-scene-tools-ai-card--neutral");
                break;
            case AIPanelTone.Brown:
                card.AddToClassList("rccp-scene-tools-ai-card--brown");
                break;
            case AIPanelTone.Yellow:
                card.AddToClassList("rccp-scene-tools-ai-card--yellow");
                break;
            case AIPanelTone.Red:
                card.AddToClassList("rccp-scene-tools-ai-card--red");
                break;
            case AIPanelTone.Teal:
                card.AddToClassList("rccp-scene-tools-ai-card--teal");
                break;
        }

    }

    private void OpenAIAssistantPanel(string panelType) {

        // Open the AI Assistant window and switch to the requested panel.
        var windowType = System.Type.GetType("BoneCrackerGames.RCCP.AIAssistant.RCCP_AIAssistantWindow, Assembly-CSharp-Editor");
        if (windowType != null) {
            var method = windowType.GetMethod("ShowWindowWithPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null) {
                method.Invoke(null, new object[] { panelType });
                return;
            }
        }

        // Fallback: just open the window without panel selection.
        OpenAIAssistantWindow();

    }

    private void OpenAIAssistantWindow() {

        // Use reflection to open AI Assistant window.
        var windowType = System.Type.GetType("BoneCrackerGames.RCCP.AIAssistant.RCCP_AIAssistantWindow, Assembly-CSharp-Editor");
        if (windowType != null) {
            var method = windowType.GetMethod("ShowWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, null);
        }

    }

    #endregion

    private enum AIPanelTone {
        Green,
        Blue,
        Purple,
        Neutral,
        Brown,
        Yellow,
        Red,
        Teal
    }

}

#endif

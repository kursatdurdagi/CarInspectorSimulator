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
using System.Linq;

/// <summary>
/// Diagnostics tab content for RCCP Scene View Overlay.
/// Provides scene validation summary and missing component warnings.
/// </summary>
public class RCCP_DiagnosticsTab : IRCCP_OverlayContent {

    #region Variables

    private VisualElement rootElement;
    private VisualElement diagnosticsContainer;
    private Label statusLabel;

    #endregion

    #region Interface Implementation

    public VisualElement CreateContent(string searchQuery) {

        rootElement = new VisualElement();
        rootElement.name = "rccp-diagnostics-tab";
        rootElement.AddToClassList("rccp-scene-tools-diagnostics");
        rootElement.style.flexGrow = 1;
        rootElement.style.flexShrink = 1;
        rootElement.style.height = Length.Percent(100);

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Create status label.
        statusLabel = new Label();
        statusLabel.AddToClassList("rccp-scene-tools-diagnostics__status");
        statusLabel.style.marginBottom = 4 * scale;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.height = 14 * scale;
        statusLabel.style.fontSize = 9 * scale;
        rootElement.Add(statusLabel);

        // Create diagnostics container.
        diagnosticsContainer = new VisualElement();
        diagnosticsContainer.name = "diagnostics-container";
        diagnosticsContainer.AddToClassList("rccp-scene-tools-diagnostics__list");
        diagnosticsContainer.style.paddingBottom = 20 * scale;

        // Create scroll view.
        ScrollView scrollView = RCCP_SceneToolsUI.CreateScrollView("rccp-scene-tools-diagnostics__scroll");
        scrollView.Add(diagnosticsContainer);
        rootElement.Add(scrollView);

        // Run diagnostics.
        RefreshDiagnostics(searchQuery);

        return rootElement;

    }

    public void OnUpdate() {

        // Periodic refresh can be added here if needed.

    }

    public void OnDestroy() {

        // Cleanup if needed.

    }

    #endregion

    #region Diagnostics Display

    private void RefreshDiagnostics(string searchQuery) {

        diagnosticsContainer.Clear();

        var stats = RCCP_SceneDataCache.GetStatistics();
        var vehicles = RCCP_SceneDataCache.GetVehicles();

        // Update status.
        UpdateStatusLabel(stats);

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Scene Overview Section.
        CreateSection("Scene Overview", diagnosticsContainer);
        CreateDiagnosticCard(
            "RCCP Settings",
            stats.hasRCCPSettings ? "Found" : "Missing",
            stats.hasRCCPSettings ? DiagnosticLevel.Success : DiagnosticLevel.Error,
            stats.hasRCCPSettings ? null : (System.Action)(() => Selection.activeObject = RCCP_Settings.Instance),
            stats.hasRCCPSettings ? "\u2705" : "\u274c"
        );

        CreateDiagnosticCard(
            "RCCP Scene Manager",
            stats.hasRCCPSceneManager ? "Found" : "Missing",
            stats.hasRCCPSceneManager ? DiagnosticLevel.Success : DiagnosticLevel.Warning,
            null,
            stats.hasRCCPSceneManager ? "\u2705" : "\u26a0\ufe0f"
        );

        CreateDiagnosticCard(
            "Total Vehicles",
            stats.totalVehicles.ToString(),
            stats.totalVehicles > 0 ? DiagnosticLevel.Success : DiagnosticLevel.Info,
            null,
            "\ud83d\ude97"
        );

        // Vehicle Status Section.
        if (vehicles.Count > 0) {

            CreateSection("Vehicle Status", diagnosticsContainer);

            CreateDiagnosticCard(
                "Fully Configured",
                $"{stats.fullyConfiguredVehicles} / {stats.totalVehicles}",
                stats.fullyConfiguredVehicles == stats.totalVehicles ? DiagnosticLevel.Success : DiagnosticLevel.Warning,
                null,
                "\u2705"
            );

            if (stats.vehiclesWithMissingRequired > 0) {
                CreateDiagnosticCard(
                    "Missing Required Components",
                    $"{stats.vehiclesWithMissingRequired} vehicle(s)",
                    DiagnosticLevel.Error,
                    null,
                    "\u274c"
                );
            }

            if (stats.partiallyConfiguredVehicles > 0) {
                CreateDiagnosticCard(
                    "Missing Optional Components",
                    $"{stats.partiallyConfiguredVehicles} vehicle(s)",
                    DiagnosticLevel.Warning,
                    null,
                    "\u26a0\ufe0f"
                );
            }

            // Per-vehicle diagnostics.
            CreateSection("Vehicle Details", diagnosticsContainer);

            foreach (var vehicle in vehicles) {

                if (vehicle == null) continue;

                // Filter check.
                if (!string.IsNullOrEmpty(searchQuery)) {
                    string lowerSearch = searchQuery.ToLower();
                    if (!vehicle.gameObject.name.ToLower().Contains(lowerSearch)) {
                        continue;
                    }
                }

                var status = RCCP_SceneDataCache.GetVehicleComponentStatus(vehicle);
                CreateVehicleDiagnosticCard(vehicle, status);

            }

        }

        // Action buttons.
        CreateActionButtons();

    }

    private void CreateSection(string title, VisualElement parent) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        Label sectionLabel = RCCP_SceneToolsUI.CreateSectionTitle(title, scale);
        sectionLabel.style.fontSize = 9 * scale;
        sectionLabel.style.marginTop = 5 * scale;
        sectionLabel.style.marginBottom = 2 * scale;
        parent.Add(sectionLabel);

    }

    private void CreateDiagnosticCard(string title, string value, DiagnosticLevel level, System.Action onClick, string icon) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-scene-tools-diagnostic-card");
        card.style.paddingTop = 3 * scale;
        card.style.paddingBottom = 3 * scale;
        card.style.paddingLeft = 5 * scale;
        card.style.paddingRight = 5 * scale;
        card.style.marginBottom = 1 * scale;

        ApplyDiagnosticLevel(card, level);

        // Icon.
        Label iconLabel = new Label(icon);
        iconLabel.AddToClassList("rccp-scene-tools-diagnostic-card__icon");
        iconLabel.style.fontSize = 10 * scale;
        iconLabel.style.marginRight = 5 * scale;
        iconLabel.style.width = 14 * scale;
        card.Add(iconLabel);

        // Title.
        Label titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-scene-tools-diagnostic-card__title");
        titleLabel.style.fontSize = 8 * scale;
        card.Add(titleLabel);

        // Value.
        Label valueLabel = new Label(value);
        valueLabel.AddToClassList("rccp-scene-tools-diagnostic-card__value");
        valueLabel.style.fontSize = 8 * scale;
        valueLabel.style.color = GetLevelColor(level);
        card.Add(valueLabel);

        // Make clickable if action provided.
        if (onClick != null) {
            card.AddToClassList("rccp-scene-tools-diagnostic-card--clickable");
            card.RegisterCallback<ClickEvent>(evt => onClick());
        }

        diagnosticsContainer.Add(card);

    }

    private void CreateVehicleDiagnosticCard(RCCP_CarController vehicle, RCCP_SceneDataCache.VehicleComponentStatus status) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement card = new VisualElement();
        card.AddToClassList("rccp-scene-tools-diagnostic-detail-card");
        card.style.paddingTop = 3 * scale;
        card.style.paddingBottom = 3 * scale;
        card.style.paddingLeft = 5 * scale;
        card.style.paddingRight = 5 * scale;
        card.style.marginBottom = 1 * scale;

        ApplyVehicleStatus(card, status);

        // Header row.
        VisualElement headerRow = new VisualElement();
        headerRow.AddToClassList("rccp-scene-tools-diagnostic-detail-card__header");
        headerRow.style.marginBottom = 2 * scale;

        Label vehicleLabel = new Label(vehicle.gameObject.name);
        vehicleLabel.AddToClassList("rccp-scene-tools-diagnostic-detail-card__title");
        vehicleLabel.style.fontSize = 8 * scale;
        headerRow.Add(vehicleLabel);

        Button selectButton = new Button(() => {
            Selection.activeGameObject = vehicle.gameObject;
            EditorGUIUtility.PingObject(vehicle.gameObject);
        });
        selectButton.text = "Select";
        selectButton.AddToClassList("rccp-scene-tools-button");
        selectButton.style.height = 14 * scale;
        selectButton.style.fontSize = 7 * scale;
        headerRow.Add(selectButton);

        card.Add(headerRow);

        // Component status.
        VisualElement componentsRow = new VisualElement();
        componentsRow.AddToClassList("rccp-scene-tools-diagnostic-detail-card__components");

        // Required components.
        AddComponentIndicator(componentsRow, "Engine", status.hasEngine, true, scale);
        AddComponentIndicator(componentsRow, "Gearbox", status.hasGearbox, true, scale);
        AddComponentIndicator(componentsRow, "Clutch", status.hasClutch, true, scale);
        AddComponentIndicator(componentsRow, "Differential", status.hasDifferential, true, scale);
        AddComponentIndicator(componentsRow, "Axles", status.hasAxles, true, scale);
        AddComponentIndicator(componentsRow, "Input", status.hasInput, true, scale);

        // Optional components.
        AddComponentIndicator(componentsRow, "Audio", status.hasAudio, false, scale);
        AddComponentIndicator(componentsRow, "Lights", status.hasLights, false, scale);
        AddComponentIndicator(componentsRow, "Stability", status.hasStability, false, scale);
        AddComponentIndicator(componentsRow, "Damage", status.hasDamage, false, scale);

        card.Add(componentsRow);

        diagnosticsContainer.Add(card);

    }

    private void AddComponentIndicator(VisualElement parent, string name, bool hasComponent, bool isRequired, float scale) {

        VisualElement indicator = new VisualElement();
        indicator.AddToClassList("rccp-scene-tools-component-indicator");
        indicator.style.marginRight = 5 * scale;
        indicator.style.marginBottom = 1 * scale;

        // Status dot.
        VisualElement dot = new VisualElement();
        dot.AddToClassList("rccp-scene-tools-component-indicator__dot");
        dot.style.width = 5 * scale;
        dot.style.height = 5 * scale;
        dot.style.marginRight = 2 * scale;

        if (hasComponent) {
            dot.AddToClassList("rccp-scene-tools-component-indicator__dot--ok");
        } else if (isRequired) {
            dot.AddToClassList("rccp-scene-tools-component-indicator__dot--error");
        } else {
            dot.AddToClassList("rccp-scene-tools-component-indicator__dot--neutral");
        }

        indicator.Add(dot);

        // Name.
        Label nameLabel = new Label(name);
        nameLabel.AddToClassList("rccp-scene-tools-component-indicator__label");
        nameLabel.AddToClassList(hasComponent
            ? "rccp-scene-tools-component-indicator__label--present"
            : "rccp-scene-tools-component-indicator__label--missing");
        nameLabel.style.fontSize = 7 * scale;
        indicator.Add(nameLabel);

        parent.Add(indicator);

    }

    private void CreateActionButtons() {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement buttonsContainer = RCCP_SceneToolsUI.CreateActionBar(scale, 5f, 5f, "rccp-scene-tools-diagnostics__actions");

        // Full Diagnostics button only if AI Assistant is installed.
        if (RCCP_SceneToolsController.IsAIAssistantInstalled) {

            Button fullDiagButton = new Button(() => {
                OpenAIAssistantWindow();
            });
            fullDiagButton.text = "Full Diagnostics";
            fullDiagButton.AddToClassList("rccp-scene-tools-button");
            fullDiagButton.style.flexGrow = 1;
            fullDiagButton.style.marginRight = 2 * scale;
            fullDiagButton.style.height = 18 * scale;
            fullDiagButton.style.fontSize = 8 * scale;
            buttonsContainer.Add(fullDiagButton);

        } else {

            Button getAIDiagButton = new Button(() => {
                Application.OpenURL(RCCP_AssetPaths.AIAssistant);
            });
            getAIDiagButton.text = "Get AI Diagnostics";
            getAIDiagButton.AddToClassList("rccp-scene-tools-button");
            getAIDiagButton.AddToClassList("rccp-scene-tools-button--promo");
            getAIDiagButton.tooltip = "Get full AI-powered diagnostics on Asset Store";
            getAIDiagButton.style.flexGrow = 1;
            getAIDiagButton.style.marginRight = 2 * scale;
            getAIDiagButton.style.height = 18 * scale;
            getAIDiagButton.style.fontSize = 8 * scale;
            buttonsContainer.Add(getAIDiagButton);

        }

        Button refreshButton = new Button(() => RefreshDiagnostics(""));
        refreshButton.text = "Refresh";
        refreshButton.AddToClassList("rccp-scene-tools-button");
        refreshButton.style.flexGrow = 1;
        refreshButton.style.marginLeft = RCCP_SceneToolsController.IsAIAssistantInstalled ? 2 * scale : 0;
        refreshButton.style.height = 18 * scale;
        refreshButton.style.fontSize = 8 * scale;
        buttonsContainer.Add(refreshButton);

        diagnosticsContainer.Add(buttonsContainer);

    }

    private void UpdateStatusLabel(RCCP_SceneDataCache.SceneStatistics stats) {

        int issues = stats.vehiclesWithMissingRequired + (stats.hasRCCPSettings ? 0 : 1);
        statusLabel.RemoveFromClassList("rccp-scene-tools-diagnostics__status--ok");
        statusLabel.RemoveFromClassList("rccp-scene-tools-diagnostics__status--warn");

        if (issues == 0) {
            statusLabel.text = "\u2705 All checks passed";
            statusLabel.style.color = new Color(0.3f, 0.8f, 0.3f);
            statusLabel.AddToClassList("rccp-scene-tools-diagnostics__status--ok");
        } else {
            statusLabel.text = $"\u26a0\ufe0f {issues} issue(s) found";
            statusLabel.style.color = new Color(1f, 0.6f, 0.2f);
            statusLabel.AddToClassList("rccp-scene-tools-diagnostics__status--warn");
        }

    }

    private static void ApplyDiagnosticLevel(VisualElement card, DiagnosticLevel level) {

        card.RemoveFromClassList("rccp-scene-tools-diagnostic-card--success");
        card.RemoveFromClassList("rccp-scene-tools-diagnostic-card--warning");
        card.RemoveFromClassList("rccp-scene-tools-diagnostic-card--error");
        card.RemoveFromClassList("rccp-scene-tools-diagnostic-card--info");

        switch (level) {
            case DiagnosticLevel.Success:
                card.AddToClassList("rccp-scene-tools-diagnostic-card--success");
                break;
            case DiagnosticLevel.Warning:
                card.AddToClassList("rccp-scene-tools-diagnostic-card--warning");
                break;
            case DiagnosticLevel.Error:
                card.AddToClassList("rccp-scene-tools-diagnostic-card--error");
                break;
            default:
                card.AddToClassList("rccp-scene-tools-diagnostic-card--info");
                break;
        }

    }

    private static void ApplyVehicleStatus(VisualElement card, RCCP_SceneDataCache.VehicleComponentStatus status) {

        card.RemoveFromClassList("rccp-scene-tools-diagnostic-detail-card--ok");
        card.RemoveFromClassList("rccp-scene-tools-diagnostic-detail-card--warn");
        card.RemoveFromClassList("rccp-scene-tools-diagnostic-detail-card--error");

        if (status.IsFullyConfigured) {
            card.AddToClassList("rccp-scene-tools-diagnostic-detail-card--ok");
        } else if (status.HasCriticalIssues) {
            card.AddToClassList("rccp-scene-tools-diagnostic-detail-card--error");
        } else {
            card.AddToClassList("rccp-scene-tools-diagnostic-detail-card--warn");
        }

    }

    private Color GetLevelColor(DiagnosticLevel level) {

        switch (level) {
            case DiagnosticLevel.Success:
                return new Color(0.3f, 0.8f, 0.3f);
            case DiagnosticLevel.Warning:
                return new Color(1f, 0.8f, 0.2f);
            case DiagnosticLevel.Error:
                return new Color(0.9f, 0.3f, 0.3f);
            default:
                return new Color(0.7f, 0.7f, 0.7f);
        }

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

    #region Enums

    private enum DiagnosticLevel {
        Info,
        Success,
        Warning,
        Error
    }

    #endregion

}

#endif

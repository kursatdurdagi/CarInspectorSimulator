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
/// Vehicles tab content for RCCP Scene View Overlay.
/// Provides quick access to all RCCP vehicles in the scene.
/// </summary>
public class RCCP_VehiclesTab : IRCCP_OverlayContent {

    #region Variables

    private VisualElement rootElement;
    private VisualElement vehiclesContainer;
    private Label statusLabel;

    #endregion

    #region Interface Implementation

    public VisualElement CreateContent(string searchQuery) {

        rootElement = new VisualElement();
        rootElement.name = "rccp-vehicles-tab";
        rootElement.AddToClassList("rccp-scene-tools-vehicles");
        rootElement.style.flexGrow = 1;
        rootElement.style.flexShrink = 1;
        rootElement.style.height = Length.Percent(100);

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Create status label.
        statusLabel = new Label();
        statusLabel.AddToClassList("rccp-scene-tools-vehicles__status");
        statusLabel.style.marginBottom = 4 * scale;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        statusLabel.style.height = 14 * scale;
        statusLabel.style.fontSize = 9 * scale;
        rootElement.Add(statusLabel);

        // Create vehicles container.
        vehiclesContainer = new VisualElement();
        vehiclesContainer.name = "vehicles-container";
        vehiclesContainer.AddToClassList("rccp-scene-tools-vehicles__list");
        vehiclesContainer.style.paddingBottom = 20 * scale;

        // Create scroll view.
        ScrollView scrollView = RCCP_SceneToolsUI.CreateScrollView("rccp-scene-tools-vehicles__scroll");
        scrollView.Add(vehiclesContainer);
        rootElement.Add(scrollView);

        // Load vehicles.
        RefreshVehicles(searchQuery);

        return rootElement;

    }

    public void OnUpdate() {

        // Periodic refresh handled by RefreshVehicles if needed.

    }

    public void OnDestroy() {

        // Cleanup if needed.

    }

    #endregion

    #region Vehicle Display

    private void RefreshVehicles(string searchQuery) {

        vehiclesContainer.Clear();

        var vehicles = RCCP_SceneDataCache.GetVehicles(searchQuery);
        var stats = RCCP_SceneDataCache.GetStatistics();

        // Update status.
        UpdateStatusLabel(stats);

        if (vehicles.Count == 0) {

            CreateEmptyState();
            return;

        }

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        // Create vehicle cards.
        foreach (var vehicle in vehicles) {

            if (vehicle == null) continue;

            CreateVehicleCard(vehicle);

        }

        // Add action buttons at the bottom.
        CreateActionButtons();

    }

    private void CreateEmptyState() {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement emptyState = new VisualElement();
        emptyState.AddToClassList("rccp-scene-tools-empty-state");
        emptyState.style.paddingTop = 30 * scale;
        emptyState.style.paddingBottom = 30 * scale;

        Label iconLabel = new Label("\ud83d\ude97");
        iconLabel.AddToClassList("rccp-scene-tools-empty-state__icon");
        iconLabel.style.fontSize = 36 * scale;
        iconLabel.style.marginBottom = 8 * scale;
        emptyState.Add(iconLabel);

        Label titleLabel = new Label("No RCCP Vehicles Found");
        titleLabel.AddToClassList("rccp-scene-tools-empty-state__title");
        titleLabel.style.fontSize = 12 * scale;
        titleLabel.style.marginBottom = 4 * scale;
        emptyState.Add(titleLabel);

        Label descLabel = new Label("Add RCCP_CarController to a vehicle.");
        descLabel.AddToClassList("rccp-scene-tools-empty-state__description");
        descLabel.style.fontSize = 10 * scale;
        emptyState.Add(descLabel);

        // Show AI Assistant button only if installed.
        if (RCCP_SceneToolsController.IsAIAssistantInstalled) {

            Button createButton = new Button(() => {
                OpenAIAssistantWindow();
            });
            createButton.text = "Open AI Assistant";
            createButton.AddToClassList("rccp-scene-tools-button");
            createButton.AddToClassList("rccp-scene-tools-button--accent");
            createButton.style.marginTop = 12 * scale;
            createButton.style.paddingTop = 6 * scale;
            createButton.style.paddingBottom = 6 * scale;
            createButton.style.paddingLeft = 12 * scale;
            createButton.style.paddingRight = 12 * scale;
            createButton.style.backgroundColor = new Color(0.86f, 0.49f, 0.24f, 0.85f);
            createButton.style.color = Color.white;
            createButton.style.borderTopLeftRadius = 4;
            createButton.style.borderTopRightRadius = 4;
            createButton.style.borderBottomLeftRadius = 4;
            createButton.style.borderBottomRightRadius = 4;
            emptyState.Add(createButton);

        } else {

            Button getAIButton = new Button(() => {
                Application.OpenURL(RCCP_AssetPaths.AIAssistant);
            });
            getAIButton.text = "Get AI Assistant";
            getAIButton.AddToClassList("rccp-scene-tools-button");
            getAIButton.AddToClassList("rccp-scene-tools-button--promo");
            getAIButton.tooltip = "Configure vehicles with AI - Get on Asset Store";
            getAIButton.style.marginTop = 12 * scale;
            getAIButton.style.paddingTop = 6 * scale;
            getAIButton.style.paddingBottom = 6 * scale;
            getAIButton.style.paddingLeft = 12 * scale;
            getAIButton.style.paddingRight = 12 * scale;
            getAIButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.65f, 0.85f);
            getAIButton.style.color = Color.white;
            getAIButton.style.borderTopLeftRadius = 4;
            getAIButton.style.borderTopRightRadius = 4;
            getAIButton.style.borderBottomLeftRadius = 4;
            getAIButton.style.borderBottomRightRadius = 4;
            emptyState.Add(getAIButton);

        }

        vehiclesContainer.Add(emptyState);

    }

    private void CreateVehicleCard(RCCP_CarController vehicle) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();
        var status = RCCP_SceneDataCache.GetVehicleComponentStatus(vehicle);

        VisualElement card = new VisualElement();
        card.name = $"card-{vehicle.gameObject.name.ToLower().Replace(" ", "-")}";
        card.AddToClassList("rccp-scene-tools-vehicle-card");
        card.style.paddingTop = 4 * scale;
        card.style.paddingBottom = 4 * scale;
        card.style.paddingLeft = 6 * scale;
        card.style.paddingRight = 6 * scale;
        card.style.marginBottom = 1 * scale;
        ApplyVehicleCardStatus(card, status);

        // Icon.
        Label iconLabel = new Label("\ud83d\ude97");
        iconLabel.AddToClassList("rccp-scene-tools-vehicle-card__icon");
        iconLabel.style.fontSize = 14 * scale;
        iconLabel.style.marginRight = 4 * scale;
        card.Add(iconLabel);

        // Info container.
        VisualElement infoContainer = new VisualElement();
        infoContainer.AddToClassList("rccp-scene-tools-vehicle-card__info");

        Label titleLabel = new Label(vehicle.gameObject.name);
        titleLabel.AddToClassList("rccp-scene-tools-vehicle-card__title");
        titleLabel.style.fontSize = 9 * scale;
        infoContainer.Add(titleLabel);

        Label componentLabel = new Label($"Components: {status.ActiveComponents}/{status.TotalComponents}");
        componentLabel.AddToClassList("rccp-scene-tools-vehicle-card__meta");
        componentLabel.style.fontSize = 8 * scale;
        infoContainer.Add(componentLabel);

        card.Add(infoContainer);

        // Status indicator.
        VisualElement statusIndicator = new VisualElement();
        statusIndicator.AddToClassList("rccp-scene-tools-status-dot");
        statusIndicator.style.width = 6 * scale;
        statusIndicator.style.height = 6 * scale;
        statusIndicator.style.alignSelf = Align.Center;
        statusIndicator.style.marginRight = 3 * scale;
        ApplyVehicleStatusIndicator(statusIndicator, status);

        card.Add(statusIndicator);

        // Action buttons container.
        VisualElement actionsContainer = new VisualElement();
        actionsContainer.AddToClassList("rccp-scene-tools-vehicle-card__actions");

        Button selectButton = new Button(() => SelectVehicle(vehicle));
        selectButton.text = "Select";
        selectButton.AddToClassList("rccp-scene-tools-button");
        selectButton.style.width = 36 * scale;
        selectButton.style.height = 16 * scale;
        selectButton.style.fontSize = 8 * scale;
        selectButton.style.marginRight = 2 * scale;
        actionsContainer.Add(selectButton);

        Button frameButton = new Button(() => FrameVehicle(vehicle));
        frameButton.text = "Frame";
        frameButton.AddToClassList("rccp-scene-tools-button");
        frameButton.style.width = 36 * scale;
        frameButton.style.height = 16 * scale;
        frameButton.style.fontSize = 8 * scale;
        frameButton.style.marginRight = 2 * scale;
        actionsContainer.Add(frameButton);

        // AI button only if AI Assistant is installed.
        if (RCCP_SceneToolsController.IsAIAssistantInstalled) {

            Button aiButton = new Button(() => OpenAIAssistant(vehicle));
            aiButton.text = "AI";
            aiButton.AddToClassList("rccp-scene-tools-button");
            aiButton.AddToClassList("rccp-scene-tools-button--accent");
            aiButton.tooltip = "Open AI Assistant for this vehicle";
            aiButton.style.width = 22 * scale;
            aiButton.style.height = 16 * scale;
            aiButton.style.fontSize = 8 * scale;
            actionsContainer.Add(aiButton);

        } else {

            Button aiPromoButton = new Button(() => Application.OpenURL(RCCP_AssetPaths.AIAssistant));
            aiPromoButton.text = "AI";
            aiPromoButton.AddToClassList("rccp-scene-tools-button");
            aiPromoButton.AddToClassList("rccp-scene-tools-button--promo");
            aiPromoButton.tooltip = "Get AI Assistant on Asset Store";
            aiPromoButton.style.width = 22 * scale;
            aiPromoButton.style.height = 16 * scale;
            aiPromoButton.style.fontSize = 8 * scale;
            actionsContainer.Add(aiPromoButton);

        }

        card.Add(actionsContainer);

        vehiclesContainer.Add(card);

    }

    private void CreateActionButtons() {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        VisualElement buttonsContainer = RCCP_SceneToolsUI.CreateActionBar(scale, 6f, 6f, "rccp-scene-tools-vehicles__actions");

        Button frameAllButton = new Button(FrameAllVehicles);
        frameAllButton.text = "Frame All";
        frameAllButton.AddToClassList("rccp-scene-tools-button");
        frameAllButton.style.flexGrow = 1;
        frameAllButton.style.marginRight = 2 * scale;
        frameAllButton.style.height = 20 * scale;
        frameAllButton.style.fontSize = 9 * scale;
        buttonsContainer.Add(frameAllButton);

        Button refreshButton = new Button(() => RefreshVehicles(""));
        refreshButton.text = "Refresh";
        refreshButton.AddToClassList("rccp-scene-tools-button");
        refreshButton.style.flexGrow = 1;
        refreshButton.style.marginLeft = 2 * scale;
        refreshButton.style.height = 20 * scale;
        refreshButton.style.fontSize = 9 * scale;
        buttonsContainer.Add(refreshButton);

        vehiclesContainer.Add(buttonsContainer);

    }

    private void UpdateStatusLabel(RCCP_SceneDataCache.SceneStatistics stats) {

        float scale = RCCP_SceneViewOverlay.GetStaticScaleFactor();

        statusLabel.text = $"Vehicles: {stats.totalVehicles} | Configured: {stats.fullyConfiguredVehicles}";
        statusLabel.RemoveFromClassList("rccp-scene-tools-vehicles__status--neutral");
        statusLabel.RemoveFromClassList("rccp-scene-tools-vehicles__status--ok");
        statusLabel.RemoveFromClassList("rccp-scene-tools-vehicles__status--warn");
        statusLabel.RemoveFromClassList("rccp-scene-tools-vehicles__status--error");

        if (stats.totalVehicles == 0) {
            statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            statusLabel.AddToClassList("rccp-scene-tools-vehicles__status--neutral");
        } else if (stats.fullyConfiguredVehicles == stats.totalVehicles) {
            statusLabel.style.color = new Color(0.3f, 0.8f, 0.3f); // Green
            statusLabel.AddToClassList("rccp-scene-tools-vehicles__status--ok");
        } else if (stats.vehiclesWithMissingRequired > 0) {
            statusLabel.style.color = new Color(0.9f, 0.3f, 0.3f); // Red
            statusLabel.AddToClassList("rccp-scene-tools-vehicles__status--error");
        } else {
            statusLabel.style.color = new Color(1f, 0.8f, 0.2f); // Orange
            statusLabel.AddToClassList("rccp-scene-tools-vehicles__status--warn");
        }

    }

    private static void ApplyVehicleCardStatus(VisualElement card, RCCP_SceneDataCache.VehicleComponentStatus status) {

        card.RemoveFromClassList("rccp-scene-tools-vehicle-card--ok");
        card.RemoveFromClassList("rccp-scene-tools-vehicle-card--warn");
        card.RemoveFromClassList("rccp-scene-tools-vehicle-card--error");

        if (status.IsFullyConfigured) {
            card.AddToClassList("rccp-scene-tools-vehicle-card--ok");
        } else if (status.HasCriticalIssues) {
            card.AddToClassList("rccp-scene-tools-vehicle-card--error");
        } else {
            card.AddToClassList("rccp-scene-tools-vehicle-card--warn");
        }

    }

    private static void ApplyVehicleStatusIndicator(VisualElement indicator, RCCP_SceneDataCache.VehicleComponentStatus status) {

        indicator.RemoveFromClassList("rccp-scene-tools-status-dot--ok");
        indicator.RemoveFromClassList("rccp-scene-tools-status-dot--warn");
        indicator.RemoveFromClassList("rccp-scene-tools-status-dot--error");

        if (status.IsFullyConfigured) {
            indicator.AddToClassList("rccp-scene-tools-status-dot--ok");
            indicator.tooltip = "Fully configured";
        } else if (status.HasCriticalIssues) {
            indicator.AddToClassList("rccp-scene-tools-status-dot--error");
            indicator.tooltip = "Missing required components";
        } else {
            indicator.AddToClassList("rccp-scene-tools-status-dot--warn");
            indicator.tooltip = "Missing optional components";
        }

    }

    #endregion

    #region Actions

    private void SelectVehicle(RCCP_CarController vehicle) {

        if (vehicle != null) {
            Selection.activeGameObject = vehicle.gameObject;
            EditorGUIUtility.PingObject(vehicle.gameObject);
        }

    }

    private void FrameVehicle(RCCP_CarController vehicle) {

        if (vehicle != null) {
            Selection.activeGameObject = vehicle.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

    }

    private void OpenAIAssistant(RCCP_CarController vehicle) {

        if (vehicle != null) {
            Selection.activeGameObject = vehicle.gameObject;

            // Open AI Assistant and switch to VehicleCustomization panel.
            var windowType = System.Type.GetType("BoneCrackerGames.RCCP.AIAssistant.RCCP_AIAssistantWindow, Assembly-CSharp-Editor");
            if (windowType != null) {
                var method = windowType.GetMethod("ShowWindowWithPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null) {
                    method.Invoke(null, new object[] { "VehicleCustomization" });
                    return;
                }
            }

            // Fallback: just open the window.
            OpenAIAssistantWindow();
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

    private void FrameAllVehicles() {

        var vehicles = RCCP_SceneDataCache.GetVehicles();

        if (vehicles.Count > 0) {
            Selection.objects = vehicles.Select(v => v.gameObject).Cast<Object>().ToArray();
            SceneView.lastActiveSceneView?.FrameSelected();
        }

    }

    #endregion

}

#endif

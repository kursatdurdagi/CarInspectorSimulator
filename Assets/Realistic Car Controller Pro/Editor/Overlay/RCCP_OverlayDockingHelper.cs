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
using System;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.Overlays;
#endif

/// <summary>
/// Helper class that provides docking guidance and tips for the RCCP Scene View Overlay.
/// Helps users understand how to dock/undock the overlay panel.
/// </summary>
public static class RCCP_OverlayDockingHelper {

    #region Constants

    // Preference keys for tracking user experience.
    private const string PREF_FIRST_TIME_SHOWN = "RCCP_Overlay_FirstTimeShown";
    private const string PREF_DOCKING_TIP_SHOWN = "RCCP_Overlay_DockingTipShown";
    private const string PREF_HELP_DISMISSED_COUNT = "RCCP_Overlay_HelpDismissedCount";
    private const string PREF_LAST_DOCKED_STATE = "RCCP_Overlay_LastDockedState";

    #endregion

    #region Public Methods

    /// <summary>
    /// Checks if this is the first time the overlay is being shown.
    /// </summary>
    public static bool IsFirstTimeUser() {

        return !EditorPrefs.GetBool(PREF_FIRST_TIME_SHOWN, false);

    }

    /// <summary>
    /// Marks that the overlay has been shown to the user.
    /// </summary>
    public static void MarkAsShown() {

        EditorPrefs.SetBool(PREF_FIRST_TIME_SHOWN, true);

    }

    /// <summary>
    /// Shows a help dialog explaining how to dock the overlay.
    /// </summary>
    public static void ShowDockingHelp() {

        string title = "RCCP Scene Tools - Docking Guide";
        string message = "The RCCP Scene Tools panel can be docked to your Scene view toolbar for quick access!\n\n" +
                       "HOW TO DOCK:\n" +
                       "- Drag the panel header (where it says 'RCCP Scene Tools')\n" +
                       "- Drop it onto the Scene view toolbar (near the top)\n" +
                       "- The panel will snap into place as a button\n\n" +
                       "HOW TO UNDOCK:\n" +
                       "- Click the docked button to show the panel\n" +
                       "- Drag the panel away from the toolbar\n\n" +
                       "HOW TO CLOSE:\n" +
                       "- Click the x button in the top-right corner\n" +
                       "- Reopen via Tools > BoneCracker Games > Realistic Car Controller Pro > Scene Tools\n\n" +
                       "TIPS:\n" +
                       "- Right-click the panel header for more options\n" +
                       "- The panel remembers its docked/undocked state";

        EditorUtility.DisplayDialog(title, message, "Got it!");
        EditorPrefs.SetBool(PREF_DOCKING_TIP_SHOWN, true);

    }

    /// <summary>
    /// Creates a help button for inline header placement.
    /// </summary>
    public static Button CreateHelpButtonInline() {

        Button helpButton = new Button(() => ShowDockingHelp());
        helpButton.text = "?";
        helpButton.tooltip = "Learn how to dock this panel to the toolbar";

        // Styling via USS classes — colors and hover in rccp_scene_tools.uss.
        helpButton.AddToClassList("rccp-scene-tools-header-btn");
        helpButton.AddToClassList("rccp-scene-tools-header-btn--accent");

        return helpButton;

    }

    /// <summary>
    /// Creates a close button for inline header placement.
    /// </summary>
    public static Button CreateCloseButtonInline(System.Action onClose) {

        Button closeButton = new Button(onClose);
        closeButton.text = "\u00d7";
        closeButton.tooltip = "Close panel (reopen via Tools > BoneCracker Games > Realistic Car Controller Pro > Scene Tools)";

        // Styling via USS classes — colors and hover in rccp_scene_tools.uss.
        closeButton.AddToClassList("rccp-scene-tools-header-btn");
        closeButton.AddToClassList("rccp-scene-tools-header-btn--close");

        return closeButton;

    }

    /// <summary>
    /// Creates a dock/undock toggle button for inline header placement.
    /// </summary>
    public static Button CreateDockToggleButtonInline(System.Action onClick, bool isDocked) {

        Button dockButton = new Button(onClick);
        dockButton.text = isDocked ? "\u2197" : "\u2199"; // Arrow symbols.
        dockButton.tooltip = isDocked
            ? "Undock from toolbar (float panel)"
            : "Show docking instructions";

        // Styling via USS classes — colors and hover in rccp_scene_tools.uss.
        dockButton.AddToClassList("rccp-scene-tools-header-btn");
        dockButton.AddToClassList("rccp-scene-tools-header-btn--dock");
        dockButton.AddToClassList(isDocked
            ? "rccp-scene-tools-header-btn--dock-docked"
            : "rccp-scene-tools-header-btn--dock-floating");

        return dockButton;

    }

    /// <summary>
    /// Creates a first-time user tooltip.
    /// </summary>
    public static VisualElement CreateFirstTimeTooltip() {

        VisualElement tooltip = new VisualElement();
        tooltip.name = "first-time-tooltip";
        tooltip.AddToClassList("rccp-scene-tools-tooltip");

        // Arrow pointing to header.
        VisualElement arrow = new VisualElement();
        arrow.AddToClassList("rccp-scene-tools-tooltip__arrow");
        tooltip.Add(arrow);

        // Title.
        Label titleLabel = new Label("TIP: You can dock this panel!");
        titleLabel.AddToClassList("rccp-scene-tools-tooltip__title");
        tooltip.Add(titleLabel);

        // Description.
        Label descLabel = new Label("Drag this panel to the Scene view toolbar to dock it as a button for quick access.");
        descLabel.AddToClassList("rccp-scene-tools-tooltip__description");
        tooltip.Add(descLabel);

        // Dismiss button.
        Button dismissButton = new Button(() => {
            tooltip.style.display = DisplayStyle.None;
            MarkAsShown();
        });
        dismissButton.text = "Got it";
        dismissButton.AddToClassList("rccp-scene-tools-tooltip__dismiss");
        tooltip.Add(dismissButton);

        // Auto-hide after 10 seconds.
        var hideTimer = EditorApplication.timeSinceStartup;
        EditorApplication.CallbackFunction autoHide = null;
        autoHide = () => {
            if (EditorApplication.timeSinceStartup - hideTimer > 10) {
                if (tooltip.style.display != DisplayStyle.None) {
                    tooltip.style.display = DisplayStyle.None;
                    MarkAsShown();
                }
                EditorApplication.update -= autoHide;
            }
        };
        EditorApplication.update += autoHide;

        return tooltip;

    }

    /// <summary>
    /// Creates a context menu for the overlay.
    /// </summary>
    public static void ShowContextMenu(Vector2 mousePosition, System.Action closeAction = null) {

        GenericMenu menu = new GenericMenu();

        if (closeAction != null) {

            menu.AddItem(new GUIContent("Close Panel"), false, () => {
                closeAction();
            });

            menu.AddSeparator("");

        }

        menu.AddItem(new GUIContent("Dock to Toolbar"), false, () => {
            Debug.Log("[RCCP] To dock: Drag the panel header to the Scene view toolbar");
            ShowDockingHelp();
        });

        menu.AddItem(new GUIContent("Show Docking Help"), false, ShowDockingHelp);

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Reset Position"), false, () => {
            Debug.Log("[RCCP] Panel position reset to default");
        });

        // Only show AI Assistant option if installed.
        if (RCCP_SceneToolsController.IsAIAssistantInstalled) {
            menu.AddItem(new GUIContent("Open AI Assistant Window"), false, () => {
                // Use reflection to call ShowWindow since it's in a separate package.
                var windowType = System.Type.GetType("RCCP_AIAssistantWindow, Assembly-CSharp-Editor");
                if (windowType != null) {
                    var method = windowType.GetMethod("ShowWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    method?.Invoke(null, null);
                }
            });
        }

        menu.ShowAsContext();

    }

    /// <summary>
    /// Saves the current docked state.
    /// </summary>
    public static void SaveDockedState(bool isDocked) {

        EditorPrefs.SetBool(PREF_LAST_DOCKED_STATE, isDocked);

    }

    /// <summary>
    /// Gets the last docked state.
    /// </summary>
    public static bool GetLastDockedState() {

        return EditorPrefs.GetBool(PREF_LAST_DOCKED_STATE, false);

    }

    /// <summary>
    /// Shows a notification when the panel is docked/undocked.
    /// </summary>
    public static void ShowDockingNotification(bool isDocked) {

        if (SceneView.lastActiveSceneView != null) {

            string message = isDocked ?
                "Panel docked to toolbar! Click the button to toggle." :
                "Panel undocked. Drag to toolbar to re-dock.";

            SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message), 2f);

        }

    }

    #endregion

}

#endif

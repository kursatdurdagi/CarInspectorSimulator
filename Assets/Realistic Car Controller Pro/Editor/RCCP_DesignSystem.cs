//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

/// <summary>
/// Centralized editor utilities for RCCP inspectors.
/// Caches the shared GUISkin and provides common drawing helpers
/// used across all 40+ component editors.
/// </summary>
public static class RCCP_DesignSystem {

    #region Skin

    private static GUISkin _skin;

    /// <summary>
    /// Lazy-cached reference to the shared RCCP_Gui skin.
    /// </summary>
    public static GUISkin Skin {
        get {
            if (_skin == null)
                _skin = Resources.Load<GUISkin>("RCCP_Gui");
            return _skin;
        }
    }

    #endregion

    #region Behavior Override

    /// <summary>
    /// Returns true when a behavior preset is active and should override
    /// per-component settings. Replaces the duplicate BehaviorSelected()
    /// methods found in multiple editors.
    /// </summary>
    public static bool IsBehaviorOverridden(Component subComponent) {

        bool state = RCCP_Settings.Instance.overrideBehavior;

        if (subComponent.GetComponentInParent<RCCP_CarController>(true).ineffectiveBehavior)
            state = false;

        return state;

    }

    /// <summary>
    /// Draws the standard behavior-override warning HelpBox.
    /// </summary>
    public static void DrawBehaviorOverrideWarning() {

        EditorGUILayout.HelpBox("Settings with red labels will be overridden by the selected behavior in RCCP_Settings", MessageType.None);

    }

    #endregion

    #region Drawing Helpers

    /// <summary>
    /// Draws the empty skin-box separator used as a visual divider between sections.
    /// Replaces the pattern: BeginVertical(GUI.skin.box); EndVertical();
    /// </summary>
    public static void DrawSkinSeparator() {

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.EndVertical();

    }

    /// <summary>
    /// Draws the standard "Back" button that navigates to the parent RCCP_CarController.
    /// </summary>
    public static void DrawBackButton(Component subComponent) {

        if (GUILayout.Button("Back"))
            Selection.activeGameObject = subComponent.GetComponentInParent<RCCP_CarController>(true).gameObject;

    }

    /// <summary>
    /// Draws the standard "Back" button that navigates to a specific parent component type.
    /// Use for sub-editors that navigate to an intermediate parent (e.g., RCCP_OtherAddons).
    /// </summary>
    public static void DrawBackButton<T>(Component subComponent) where T : Component {

        if (GUILayout.Button("Back"))
            Selection.activeGameObject = subComponent.GetComponentInParent<T>(true).gameObject;

    }

    /// <summary>
    /// Handles the checkComponents dialog pattern. When the parent CarController
    /// has checkComponents set, shows an error dialog or navigates back.
    /// </summary>
    public static void HandleCheckComponents(Component subComponent, List<string> errorMessages) {

        RCCP_CarController carController = subComponent.GetComponentInParent<RCCP_CarController>(true);

        if (carController.checkComponents) {

            carController.checkComponents = false;

            if (errorMessages.Count > 0) {

                if (EditorUtility.DisplayDialog("Realistic Car Controller Pro | Errors found", errorMessages.Count + " Errors found!", "Cancel", "Check"))
                    Selection.activeGameObject = carController.gameObject;

            } else {

                Selection.activeGameObject = carController.gameObject;
                Debug.Log("No errors found");

            }

        }

    }

    /// <summary>
    /// Resets the component's transform to local origin.
    /// Many sub-components must stay at (0,0,0) relative to their parent.
    /// </summary>
    public static void ResetTransform(Component subComponent) {

        subComponent.transform.localPosition = Vector3.zero;
        subComponent.transform.localRotation = Quaternion.identity;

    }

    #endregion

    #region Scoped Helpers

    /// <summary>
    /// Temporarily changes GUI.color and restores it on dispose.
    /// Usage: using (new RCCP_DesignSystem.ColorScope(Color.red)) { ... }
    /// </summary>
    public struct ColorScope : IDisposable {

        private readonly Color _previousColor;

        public ColorScope(Color tempColor) {
            _previousColor = GUI.color;
            GUI.color = tempColor;
        }

        public void Dispose() {
            GUI.color = _previousColor;
        }

    }

    /// <summary>
    /// Temporarily sets GUI.skin to null (Unity default) and restores on dispose.
    /// Used when PropertyField needs the default skin (e.g., UnityEvent drawers).
    /// Usage: using (new RCCP_DesignSystem.DefaultSkinScope(GUI.skin)) { ... }
    /// </summary>
    public struct DefaultSkinScope : IDisposable {

        private readonly GUISkin _previousSkin;

        public DefaultSkinScope(GUISkin currentSkin) {
            _previousSkin = currentSkin;
            GUI.skin = null;
        }

        public void Dispose() {
            GUI.skin = _previousSkin;
        }

    }

    #endregion

}
#endif

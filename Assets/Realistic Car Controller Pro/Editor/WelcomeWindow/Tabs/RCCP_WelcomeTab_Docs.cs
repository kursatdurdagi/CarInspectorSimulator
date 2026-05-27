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
/// Documentation tab for the Welcome Window.
/// Provides links to documentation, tutorials, and support resources.
/// </summary>
public class RCCP_WelcomeTab_Docs : IRCCP_WelcomeTabContent {

    /// <summary>
    /// Creates the Documentation tab content.
    /// </summary>
    public VisualElement CreateContent() {

        VisualElement root = new VisualElement();

        // Section header.
        var section = RCCP_WelcomeWindowUI.CreateSection("Documentation", "Access comprehensive documentation, tutorials, and support resources.");

        // Primary CTA — full online docs.
        section.Add(RCCP_WelcomeWindowUI.CreateButton("Open Documentation", () => {
            Application.OpenURL("https://www.bonecrackergames.com/realistic-car-controller-pro/");
        }, "primary"));

        // Secondary actions (default styling).
        section.Add(RCCP_WelcomeWindowUI.CreateButton("Browse Documentation Files", () => {
            EditorUtility.RevealInFinder("Assets/Realistic Car Controller Pro/Documentation");
        }));

        section.Add(RCCP_WelcomeWindowUI.CreateButton("YouTube Tutorials", () => {
            Application.OpenURL(RCCP_AssetPaths.YTVideos);
        }));

        root.Add(section);

        return root;

    }

    /// <summary>
    /// Called when the tab becomes visible.
    /// </summary>
    public void OnActivated() { }

    /// <summary>
    /// Called when the tab is hidden.
    /// </summary>
    public void OnDeactivated() { }

}

#endif

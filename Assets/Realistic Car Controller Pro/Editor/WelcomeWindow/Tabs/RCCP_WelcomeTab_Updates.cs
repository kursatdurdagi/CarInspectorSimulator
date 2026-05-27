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

/// <summary>
/// Updates tab for the Welcome Window.
/// Shows current version and a link to the Asset Store. Detailed update instructions
/// are tucked behind a foldout so experienced users see just the version + CTA.
/// </summary>
public class RCCP_WelcomeTab_Updates : IRCCP_WelcomeTabContent {

    /// <summary>
    /// Creates the Updates tab content.
    /// </summary>
    public VisualElement CreateContent() {

        VisualElement root = new VisualElement();

        root.Add(RCCP_WelcomeWindowUI.CreateSection("Updates", "Current Version: " + RCCP_Version.version));

        root.Add(RCCP_WelcomeWindowUI.CreateButton("Check for Updates on the Asset Store", () => {
            Application.OpenURL(RCCP_AssetPaths.assetStorePath);
        }, "primary"));

        // Collapsible update instructions — most users know the drill.
        var howTo = new Foldout();
        howTo.text = "How to update (if you haven't done this before)";
        howTo.value = false;
        howTo.style.marginTop = 8;

        howTo.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Always delete the existing 'Realistic Car Controller Pro' folder before reimporting from the Asset Store.",
            "info"
        ));

        howTo.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "IMPORTANT: Keep your own scripts, scenes, prefabs, and assets OUTSIDE the 'Realistic Car Controller Pro' folder. Anything you've stored inside that folder will be permanently deleted in Step 2. Move your personal work out before proceeding.",
            "warning"
        ));

        howTo.Add(RCCP_WelcomeWindowUI.CreateStep(1, "Backup Your Project", "Back up the whole project and move any of your own files out of 'Assets/Realistic Car Controller Pro/' first."));
        howTo.Add(RCCP_WelcomeWindowUI.CreateStep(2, "Delete Old Version", "Delete 'Assets/Realistic Car Controller Pro/' completely. Any personal files still inside will be lost."));
        howTo.Add(RCCP_WelcomeWindowUI.CreateStep(3, "Import New Version", "Download and import the latest version from the Asset Store."));

        root.Add(howTo);

        return root;

    }

    public void OnActivated() { }
    public void OnDeactivated() { }

}

#endif

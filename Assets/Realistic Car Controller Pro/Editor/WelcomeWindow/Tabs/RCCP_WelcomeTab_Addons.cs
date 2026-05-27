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
/// Addons tab content for the RCCP Welcome Window.
/// Provides addon package management (import/download).
/// </summary>
public class RCCP_WelcomeTab_Addons : IRCCP_WelcomeTabContent {

    private readonly RCCP_WelcomeWindowController controller;
    private VisualElement grid;
    private VisualElement compileIndicator;

    public RCCP_WelcomeTab_Addons(RCCP_WelcomeWindowController controller) {
        this.controller = controller;
    }

    public VisualElement CreateContent() {

        var root = new VisualElement();

        // Intro section — keeps the block-background heading style used elsewhere.
        root.Add(RCCP_WelcomeWindowUI.CreateSection("Addon Packages",
            "Extend RCCP with multiplayer networking, AI configuration, visual effects, and more."));

        // Compiling indicator — shown only while Unity is compiling scripts or refreshing the asset
        // database. Disables the card grid below so users can't fire a second import package while
        // Unity is already busy (which otherwise blocks behind the "Waiting for Unity's code to
        // finish executing" modal).
        compileIndicator = BuildCompileIndicator();
        compileIndicator.style.display = DisplayStyle.None;
        root.Add(compileIndicator);

        // Card grid — all addons flow as wrapping cards, no separators needed.
        grid = new VisualElement();
        grid.AddToClassList("rccp-welcome-feature-card-row");

        // RCCP AI Assistant.
#if !BCG_RCCP_AI
        grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
            "RCCP AI Assistant (NEW)",
            "Configure vehicles with AI-powered assistance using natural language.",
            false,
            "Get on Asset Store",
            () => Application.OpenURL(RCCP_AssetPaths.AIAssistant),
            "promo"
        ));
#else
        grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
            "RCCP AI Assistant",
            "Configure vehicles with AI-powered assistance using natural language.",
            true, "", null
        ));
#endif

        // Demo Content.
#if !RCCP_DEMO
        grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
            "Demo Content",
            "Demo scenes, vehicles, and example setups.",
            false,
            "Import",
            () => {
                bool decision = EditorUtility.DisplayDialog(
                    "Realistic Car Controller Pro | Import Demo Content",
                    "Import demo assets? This will increase build size.",
                    "Yes, import", "No");
                if (decision)
                    RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.demoPackage, "Demo Content");
            }
        ));
#else
        grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
            "Demo Content",
            "Demo scenes, vehicles, and example setups.",
            true, "", null
        ));
#endif

        // Photon PUN2.
        bool photonInstalled = false;
#if PHOTON_UNITY_NETWORKING
        photonInstalled = true;
#endif
        bool photonAndRCCInstalled = false;
#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
        photonAndRCCInstalled = true;
#endif

        if (photonAndRCCInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Photon PUN2",
                "Multiplayer networking via Photon.",
                true, "", null
            ));
        } else if (photonInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Photon PUN2",
                "Photon detected. Import the RCCP integration package.",
                false,
                "Import Integration",
                () => RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.PhotonPUN2, "Photon PUN2 Integration")
            ));
        } else {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Photon PUN2",
                "Multiplayer networking via Photon.",
                false,
                "Download",
                () => Application.OpenURL(RCCP_AssetPaths.photonPUN2)
            ));
        }

        // BCG Shared Assets.
        bool bcgInstalled = false;
#if BCG_ENTEREXIT
        bcgInstalled = true;
#endif

        if (bcgInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "BCG Shared Assets",
                "Enter and exit vehicles with FPS/TPS character controllers.",
                true, "", null
            ));
        } else {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "BCG Shared Assets",
                "Enter and exit vehicles with FPS/TPS character controllers.",
                false,
                "Import",
                () => RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.BCGSharedAssets, "BCG Shared Assets")
            ));
        }

        // Mirror.
        bool mirrorInstalled = false;
#if MIRROR
        mirrorInstalled = true;
#endif
        bool mirrorAndRCCPInstalled = false;
#if RCCP_MIRROR && MIRROR
        mirrorAndRCCPInstalled = true;
#endif

        if (mirrorAndRCCPInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Mirror",
                "Multiplayer networking via Mirror.",
                true, "", null
            ));
        } else if (mirrorInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Mirror",
                "Mirror detected. Import the RCCP integration package.",
                false,
                "Import Integration",
                () => RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.mirror, "Mirror Integration")
            ));
        } else {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Mirror",
                "Multiplayer networking via Mirror.",
                false,
                "Download",
                () => Application.OpenURL(RCCP_AssetPaths.mirror)
            ));
        }

        // ProFlares. No scripting symbol exists — detect via type probe.
        bool proFlaresInstalled = IsTypePresent("ProFlareBatch");

        if (proFlaresInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "ProFlares",
                "ProFlares detected. Import the RCCP integration package.",
                false,
                "Import Integration",
                () => RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.ProFlare, "ProFlares Integration")
            ));
        } else {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "ProFlares",
                "High-quality lens flare effects for vehicle lights.",
                false,
                "Download",
                () => Application.OpenURL(RCCP_AssetPaths.proFlares)
            ));
        }

        // Realistic Traffic Controller.
        bool rtcInstalled = false;
#if BCG_RTRC
        rtcInstalled = true;
#endif
        bool rtcPresent = IsTypePresent("RTC_AICarController") || IsTypePresent("RTC_SceneManager");

        if (rtcInstalled) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Realistic Traffic Controller",
                "AI traffic system integration.",
                true, "", null
            ));
        } else if (rtcPresent) {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Realistic Traffic Controller",
                "RTC detected. Import the RCCP integration package.",
                false,
                "Import Integration",
                () => RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.RTC, "Realistic Traffic Controller Integration")
            ));
        } else {
            grid.Add(RCCP_WelcomeWindowUI.CreateFeatureCard(
                "Realistic Traffic Controller",
                "AI traffic system integration.",
                false,
                "Download",
                () => Application.OpenURL(RCCP_AssetPaths.RTC)
            ));
        }

        root.Add(grid);

        // Poll compile state 4x/sec. Cheap, and the scheduler auto-stops when root detaches.
        root.schedule.Execute(UpdateCompileState).Every(250);
        UpdateCompileState();

        return root;

    }

    /// <summary>
    /// Builds the yellow "compiling" banner shown above the addon grid. The dot + text reuse the
    /// warning help-box style so the look matches the rest of the Welcome Window.
    /// </summary>
    private static VisualElement BuildCompileIndicator() {

        return RCCP_WelcomeWindowUI.CreateHelpBox(
            "Unity is compiling scripts — addon buttons are disabled until compilation finishes.",
            "warning"
        );

    }

    /// <summary>
    /// Toggles the compile indicator and disables the card grid while Unity is busy, so users
    /// can't queue a second import on top of an in-progress one.
    /// </summary>
    private void UpdateCompileState() {

        if (grid == null || compileIndicator == null)
            return;

        bool busy = EditorApplication.isCompiling || EditorApplication.isUpdating;

        compileIndicator.style.display = busy ? DisplayStyle.Flex : DisplayStyle.None;
        grid.SetEnabled(!busy);

    }

    public void OnActivated() { }
    public void OnDeactivated() { }

    /// <summary>
    /// Returns true if a type with the given simple name exists in any loaded assembly.
    /// Used to detect third-party assets that don't ship a scripting define symbol.
    /// </summary>
    private static bool IsTypePresent(string simpleName) {

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies) {
            try {
                if (asm.GetType(simpleName, false) != null)
                    return true;
            } catch { }
        }

        return false;

    }

}

#endif

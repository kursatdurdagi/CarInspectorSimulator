//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Welcome tab content for the RCCP Welcome Window.
/// Provides a quick start guide with 4 steps.
/// </summary>
public class RCCP_WelcomeTab_Welcome : IRCCP_WelcomeTabContent {

    private readonly RCCP_WelcomeWindowController controller;
    private VisualElement root;

    public RCCP_WelcomeTab_Welcome(RCCP_WelcomeWindowController controller) {
        this.controller = controller;
    }

    public VisualElement CreateContent() {

        root = new VisualElement();
        Rebuild();
        return root;

    }

    private void Rebuild() {

        if (root == null) return;
        root.Clear();

        // Diagnostics strip — shows project state at a glance.
        root.Add(BuildDiagnosticsStrip());

        // Welcome section (header + intro body).
        var welcomeSection = RCCP_WelcomeWindowUI.CreateSection(
            "Welcome!",
            "Thank you for purchasing and using Realistic Car Controller Pro. " +
            "Follow these steps to get started quickly."
        );
        root.Add(welcomeSection);

        // ─── Getting Started title ───
        var gettingStartedTitle = new Label("Getting Started");
        gettingStartedTitle.AddToClassList("rccp-welcome-section__title");
        root.Add(gettingStartedTitle);

        // ─── 4-card row ───
        var stepRow = new VisualElement();
        stepRow.AddToClassList("rccp-welcome-step-row");

        stepRow.Add(RCCP_WelcomeWindowUI.CreateStepCard(1,
            "Open Prototype Scene",
            "A ready-made scene with a ground plane and a vehicle. Perfect for testing.",
            "Open Scene",
            () => {
                RCCP_DemoScenes.Instance.GetPaths();
                RCCP_WelcomeWindowController.OpenDemoSceneSafe(RCCP_DemoScenes.Instance.path_demo_protototype, "Prototype");
            }
        ));

        stepRow.Add(RCCP_WelcomeWindowUI.CreateStepCard(2,
            "Create Your Vehicle",
            "Use the Setup Wizard to add RCCP components to any vehicle model.",
            "Open Setup Wizard",
            () => RCCP_SetupWizard.ShowWindow()
        ));

        stepRow.Add(RCCP_WelcomeWindowUI.CreateStepCard(3,
            "Add UI Canvas",
            "Add the RCCP UI Canvas for speedometer, buttons, and mobile controls.",
            "Add UI Canvas",
            () => RCCP_EditorWindows.CreateRCCUICanvas()
        ));

#if RCCP_DEMO
        stepRow.Add(RCCP_WelcomeWindowUI.CreateStepCard(4,
            "Explore Demo Scenes",
            "Open the City demo to see RCCP features in action.",
            "Open City Demo",
            () => OpenDemoScene(RCCP_DemoScenes.Instance.path_demo_City, "City")
        ));
#else
        stepRow.Add(RCCP_WelcomeWindowUI.CreateStepCard(4,
            "Import Demo Content",
            "Get the example scenes, vehicles, and setups to explore RCCP.",
            "Import Demo Assets",
            ImportDemoAssets
        ));
#endif

        root.Add(stepRow);

        // ─── Quick Start Guide title ───
        var qsTitle = new Label("Quick Start Guide");
        qsTitle.AddToClassList("rccp-welcome-section__title");
        root.Add(qsTitle);

        // ─── 4-column Quick Start row ───
        var qsRow = new VisualElement();
        qsRow.AddToClassList("rccp-welcome-quickstart-row");

        // Column 1: Documentation
        qsRow.Add(BuildQuickStartCol("Documentation",
            (text: "Online Docs", action: (System.Action)(() => Application.OpenURL("https://www.bonecrackergames.com/realistic-car-controller-pro/")), variant: "link"),
            (text: "Local Docs", action: (System.Action)(() => EditorUtility.RevealInFinder("Assets/Realistic Car Controller Pro/Documentation")), variant: "link")
        ));

        // Column 2: Demos / Examples — gated on RCCP_DEMO. When demo content
        // isn't imported the links would silently no-op, so swap to a single
        // import prompt instead of showing dead links.
#if RCCP_DEMO
        qsRow.Add(BuildQuickStartCol("Examples",
            (text: "City", action: (System.Action)(() => OpenDemoScene(RCCP_DemoScenes.Instance.path_demo_City, "City")), variant: "link"),
            (text: "Damage", action: (System.Action)(() => OpenDemoScene(RCCP_DemoScenes.Instance.path_demo_Damage, "Damage")), variant: "link"),
            (text: "Transport", action: (System.Action)(() => OpenDemoScene(RCCP_DemoScenes.Instance.path_demo_Transport, "Transport")), variant: "link")
        ));
#else
        qsRow.Add(BuildQuickStartCol("Examples",
            (text: "Import Demo Assets", action: (System.Action)ImportDemoAssets, variant: "link")
        ));
#endif

        // Column 3: Community
        qsRow.Add(BuildQuickStartCol("Community",
            (text: "YouTube", action: (System.Action)(() => Application.OpenURL(RCCP_AssetPaths.YTVideos)), variant: "link"),
            (text: "Asset Store", action: (System.Action)(() => Application.OpenURL(RCCP_AssetPaths.assetStorePath)), variant: "link"),
            (text: "Support", action: (System.Action)(() => Application.OpenURL("mailto:bonecrackergames@gmail.com")), variant: "link")
        ));

        // Column 4: Key Features (static bullet list)
        var featuresCol = new VisualElement();
        featuresCol.AddToClassList("rccp-welcome-quickstart-col");

        var featuresTitle = new Label("Key Features");
        featuresTitle.AddToClassList("rccp-welcome-quickstart__title");
        featuresCol.Add(featuresTitle);

        string[] features = {
            "• Advanced Physics",
            "• Modular Components",
            "• Damage + Customization",
            "• AI Vehicles & Mobile"
        };
        foreach (var line in features) {
            var bullet = new Label(line);
            bullet.AddToClassList("rccp-welcome-quickstart__bullet");
            featuresCol.Add(bullet);
        }
        qsRow.Add(featuresCol);

        root.Add(qsRow);

        // Startup preference.
        var startupToggle = new Toggle("Show this window on editor startup");
        startupToggle.value = EditorPrefs.GetBool(RCCP_WelcomeWindowController.ShowOnStartupPrefKey, false);
        startupToggle.RegisterValueChangedCallback(evt => {
            EditorPrefs.SetBool(RCCP_WelcomeWindowController.ShowOnStartupPrefKey, evt.newValue);
        });
        startupToggle.style.marginTop = 8;
        root.Add(startupToggle);

        // Small cross-promo link at the end of the Welcome tab (moved off Docs).
        root.Add(RCCP_WelcomeWindowUI.CreateButton("Other BoneCracker Games Assets", () => {
            Application.OpenURL(RCCP_AssetPaths.otherAssets);
        }, "link"));

    }

    /// <summary>
    /// Builds one column of the Quick Start row: bold title + inline link buttons separated by " | ".
    /// </summary>
    private static VisualElement BuildQuickStartCol(string title, params (string text, System.Action action, string variant)[] links) {

        var col = new VisualElement();
        col.AddToClassList("rccp-welcome-quickstart-col");

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("rccp-welcome-quickstart__title");
        col.Add(titleLabel);

        var linkRow = new VisualElement();
        linkRow.AddToClassList("rccp-welcome-quickstart__row");

        for (int i = 0; i < links.Length; i++) {

            var (text, action, variant) = links[i];
            linkRow.Add(RCCP_WelcomeWindowUI.CreateButton(text, action, variant));

            if (i < links.Length - 1) {
                var sep = new Label("|");
                sep.style.marginLeft = 4;
                sep.style.marginRight = 4;
                sep.style.color = new UnityEngine.Color(0.55f, 0.55f, 0.55f);
                linkRow.Add(sep);
            }

        }

        col.Add(linkRow);
        return col;

    }

    /// <summary>
    /// Shows a confirmation dialog and imports the RCCP demo content .unitypackage.
    /// Invoked by the Getting Started card and Quick Start link when RCCP_DEMO is not defined.
    /// </summary>
    private static void ImportDemoAssets() {

        bool decision = EditorUtility.DisplayDialog(
            "Realistic Car Controller Pro | Import Demo Content",
            "Import demo scenes, vehicles, and example setups? This will increase project size.",
            "Yes, import",
            "Cancel"
        );

        if (decision)
            RCCP_WelcomeWindowController.ImportPackageSafe(RCCP_AddonPackages.Instance.demoPackage, "Demo Content");

    }

    /// <summary>
    /// Opens a demo scene by path. Shows a dialog explaining why if the scene is missing
    /// instead of silently no-oping.
    /// </summary>
    private static void OpenDemoScene(string path, string displayName) {

        RCCP_DemoScenes.Instance.GetPaths();
        RCCP_WelcomeWindowController.OpenDemoSceneSafe(path, displayName);

    }

    /// <summary>
    /// Computes project-state indicators and renders them as a compact strip at the top of the tab.
    /// Called fresh every Rebuild so state reflects the moment the tab is activated.
    /// </summary>
    private VisualElement BuildDiagnosticsStrip() {

        var strip = new VisualElement();
        strip.AddToClassList("rccp-welcome-diagnostics");

        // Prototype scene available?
        RCCP_DemoScenes.Instance.GetPaths();
        bool prototypeReady = !string.IsNullOrEmpty(RCCP_DemoScenes.Instance.path_demo_protototype)
            && File.Exists(RCCP_DemoScenes.Instance.path_demo_protototype);
        strip.Add(BuildDiagItem(
            prototypeReady ? "Prototype scene: ready" : "Prototype scene: missing",
            prototypeReady ? "ok" : "warn"
        ));

        // Demo scenes in Build Settings.
        var demoCounts = CountDemoScenesInBuild();
        if (demoCounts.total > 0) {
            string state = demoCounts.inBuild == demoCounts.total
                ? "ok"
                : (demoCounts.inBuild == 0 ? "warn" : "warn");
            strip.Add(BuildDiagItem(
                $"Demos in Build: {demoCounts.inBuild}/{demoCounts.total}",
                state
            ));
        }

        // AI Assistant installed?
#if BCG_RCCP_AI
        bool aiInstalled = true;
#else
        bool aiInstalled = false;
#endif
        strip.Add(BuildDiagItem(
            aiInstalled ? "AI Assistant: installed" : "AI Assistant: not installed",
            aiInstalled ? "ok" : "warn"
        ));

        return strip;

    }

    private static Label BuildDiagItem(string text, string state) {

        var label = new Label(text);
        label.AddToClassList("rccp-welcome-diagnostics__item");
        label.AddToClassList($"rccp-welcome-diagnostics__item--{state}");
        return label;

    }

    private static (int inBuild, int total) CountDemoScenesInBuild() {

        RCCP_DemoScenes.Instance.GetPaths();
        var demos = RCCP_DemoScenes.Instance;

        string[] paths = {
            demos.path_demo_City,
            demos.path_demo_CityWithAI,
            demos.path_city_AIO,
            demos.path_demo_CarSelection,
            demos.path_demo_APIBlank,
            demos.path_demo_BlankMobile,
            demos.path_demo_Damage,
            demos.path_demo_Customization,
            demos.path_demo_OverrideInputs,
            demos.path_demo_Transport,
        };

        int total = 0;
        int inBuild = 0;
        var buildScenes = EditorBuildSettings.scenes;

        foreach (string p in paths) {

            if (string.IsNullOrEmpty(p) || !File.Exists(p))
                continue;

            total++;

            foreach (var s in buildScenes) {
                if (s.path == p && s.enabled) {
                    inBuild++;
                    break;
                }
            }

        }

        return (inBuild, total);

    }

    public void OnActivated() {

        Rebuild();

    }

    public void OnDeactivated() { }

}

#endif

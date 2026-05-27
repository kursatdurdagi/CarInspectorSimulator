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
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using BoneCrackerGames.RCCP.CoreProtection;

/// <summary>
/// Controller for the RCCP Welcome Window.
/// Manages tab state, verification logic, and content providers.
/// </summary>
public class RCCP_WelcomeWindowController {

    #region Tab Management

    /// <summary>EditorPrefs key for the "open on startup" toggle.</summary>
    public const string ShowOnStartupPrefKey = "RCCP_WelcomeWindow_ShowOnStartup";

    private readonly string[] tabNames = { "Welcome", "Demos", "Addons", "Shaders", "Keys", "Updates", "Docs" };
    private readonly Dictionary<int, IRCCP_WelcomeTabContent> contentProviders = new Dictionary<int, IRCCP_WelcomeTabContent>();

    public string[] TabNames => tabNames;

    public int CurrentTabIndex {
        get => SessionState.GetInt("RCCP_WelcomeWindow_TabIndex", 0);
        set => SessionState.SetInt("RCCP_WelcomeWindow_TabIndex", value);
    }

    public IRCCP_WelcomeTabContent GetContentProvider(int index) {

        if (contentProviders.TryGetValue(index, out var provider))
            return provider;

        provider = CreateContentProvider(index);
        contentProviders[index] = provider;
        return provider;

    }

    private IRCCP_WelcomeTabContent CreateContentProvider(int index) {

        switch (index) {
            case 0: return new RCCP_WelcomeTab_Welcome(this);
            case 1: return new RCCP_WelcomeTab_Demos(this);
            case 2: return new RCCP_WelcomeTab_Addons(this);
            case 3: return new RCCP_WelcomeTab_Shaders();
            case 4: return new RCCP_WelcomeTab_Keys();
            case 5: return new RCCP_WelcomeTab_Updates();
            case 6: return new RCCP_WelcomeTab_Docs();
            default: return new RCCP_WelcomeTab_Welcome(this);
        }

    }

    #endregion

    #region Verification

    public string InvoiceInput { get; set; } = "";
    public bool IsVerifying { get; private set; }
    public string VerificationMessage { get; private set; } = "";
    public bool IsErrorMessage { get; private set; }
    public bool IsVerified => RCCP_CoreServerProxy.IsVerified;

    public event Action OnVerificationStateChanged;

    /// <summary>
    /// Coroutine host object for EditorCoroutineUtility.
    /// </summary>
    private static readonly object coroutineHost = new object();

    public void StartCoreVerification() {

        if (IsVerifying)
            return;

        // Clicking Verify with an empty invoice used to be a silent no-op;
        // surface the missing input as an error message in the same panel.
        if (string.IsNullOrEmpty(InvoiceInput)) {
            VerificationMessage = "Please enter your invoice number first.";
            IsErrorMessage = true;
            OnVerificationStateChanged?.Invoke();
            return;
        }

        IsVerifying = true;
        VerificationMessage = "";
        OnVerificationStateChanged?.Invoke();

        // Register device first, then verify invoice in the callback.
        RCCP_CoreServerProxy.RegisterDevice(coroutineHost, (regSuccess, regMessage) => {

            if (regSuccess) {

                // Verify invoice. On failure the DLL populates result.Error (not
                // result.Message), so fall through to Error when Success is false —
                // otherwise a wrong invoice silently closes the panel with no feedback.
                RCCP_CoreServerProxy.VerifyInvoice(coroutineHost, InvoiceInput, (result) => {

                    if (result.Success) {
                        VerificationMessage = !string.IsNullOrEmpty(result.Message)
                            ? result.Message
                            : "Invoice verified successfully.";
                    } else {
                        VerificationMessage = !string.IsNullOrEmpty(result.Error)
                            ? result.Error
                            : "Verification failed. Please check your invoice number and try again.";
                    }
                    IsErrorMessage = !result.Success;
                    IsVerifying = false;
                    OnVerificationStateChanged?.Invoke();

                });

            } else {

                VerificationMessage = regMessage;
                IsErrorMessage = true;
                IsVerifying = false;
                OnVerificationStateChanged?.Invoke();

            }

        });

    }

    #endregion

    #region First-Run

    public bool ShowFirstRunSetup { get; set; }
    public bool ForceShowVerification { get; set; }

    public bool IsInputSystemInstalled() {

        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies) {
            if (assembly.GetName().Name == "Unity.InputSystem")
                return true;
        }

        return false;

    }

    #endregion

    #region Scene Helpers

    /// <summary>
    /// Opens a demo scene by path if it exists. Shows a dialog explaining why when the
    /// scene is missing (addon not imported / asset deleted) instead of silently no-oping.
    /// </summary>
    public static void OpenDemoSceneSafe(string path, string displayName) {

        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return;
        }

        EditorUtility.DisplayDialog(
            "Realistic Car Controller Pro | Scene Not Found",
            $"The '{displayName}' scene could not be opened.\n\n" +
            "This usually means the addon that ships this scene is not imported, " +
            "or the scene file has been moved or deleted. Open the Addons tab to " +
            "install the required package, then try again.",
            "OK"
        );

    }

    /// <summary>
    /// Imports a .unitypackage whose location is stored as a UnityEngine.Object reference
    /// on RCCP_AddonPackages. Shows a dialog instead of silently no-oping when the Object
    /// reference is missing or its asset path can't be resolved (which otherwise produces
    /// an empty ImportPackage("", true) call that fails without user-visible feedback).
    /// </summary>
    public static void ImportPackageSafe(UnityEngine.Object package, string displayName) {

        string path = package != null ? AssetDatabase.GetAssetPath(package) : null;

        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            AssetDatabase.ImportPackage(path, true);
            return;
        }

        EditorUtility.DisplayDialog(
            "Realistic Car Controller Pro | Installer Missing",
            $"The '{displayName}' installer package could not be found.\n\n" +
            "The reference on RCCP_AddonPackages is missing or its file has been deleted. " +
            "Reimport Realistic Car Controller Pro from the Asset Store to restore the installer.",
            "OK"
        );

    }


    /// <summary>
    /// Adds all RCCP demo scenes (core + installed integrations) to Build Settings.
    /// Each scene group is gated by its scripting-define symbol so only scenes whose
    /// addon is actually imported get added. When the AIO menu scene is present
    /// (demo content installed), it is forced to index 0 because the AIO scene loads
    /// every other demo scene by name at runtime.
    /// Returns (added, skipped, aioMovedToFirst) — skipped counts scenes that were already present.
    /// </summary>
    public (int added, int skipped, bool aioMovedToFirst) AddDemoScenesToBuildSettings() {

        RCCP_DemoScenes.Instance.GetPaths();

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var demoScenes = RCCP_DemoScenes.Instance;

        int added = 0;
        int skipped = 0;

        void AddIfNew(string path) {
            if (string.IsNullOrEmpty(path)) return;
            if (scenes.Exists(s => s.path == path)) { skipped++; return; }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            added++;
        }

        // Prototype scene ships with core RCCP — always add it if present.
        AddIfNew(demoScenes.path_demo_protototype);

        // Demo Content addon — gated by RCCP_DEMO so stale paths don't sneak in
        // after the addon is uninstalled but the ScriptableObject still holds them.
#if RCCP_DEMO
        AddIfNew(demoScenes.path_city_AIO);
        AddIfNew(demoScenes.path_demo_City);
        AddIfNew(demoScenes.path_demo_CityWithAI);
        AddIfNew(demoScenes.path_demo_CarSelection);
        AddIfNew(demoScenes.path_demo_APIBlank);
        AddIfNew(demoScenes.path_demo_BlankMobile);
        AddIfNew(demoScenes.path_demo_Damage);
        AddIfNew(demoScenes.path_demo_Customization);
        AddIfNew(demoScenes.path_demo_OverrideInputs);
        AddIfNew(demoScenes.path_demo_Transport);
#endif

        // BCG Shared Assets (character controller + Enter/Exit). Blank FPS/TPS ship
        // with the addon itself; City FPS/TPS additionally require the Demo Content city.
#if BCG_ENTEREXIT
        BCG_DemoScenes.Instance.GetPaths();
        AddIfNew(BCG_DemoScenes.Instance.path_demo_BlankFPS);
        AddIfNew(BCG_DemoScenes.Instance.path_demo_BlankTPS);
#if RCCP_DEMO
        AddIfNew(BCG_DemoScenes.Instance.path_demo_CityFPS);
        AddIfNew(BCG_DemoScenes.Instance.path_demo_CityTPS);
#endif
#endif

#if RCCP_PHOTON && PHOTON_UNITY_NETWORKING
        RCCP_DemoScenes_Photon.Instance.GetPaths();
        AddIfNew(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby);
        AddIfNew(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City);
#endif

#if RCCP_MIRROR && MIRROR
        RCCP_DemoScenes_Mirror.Instance.GetPaths();
        AddIfNew(RCCP_DemoScenes_Mirror.Instance.path_Demo_Blank_Mirror);
#endif

#if BCG_RTRC
        AddIfNew(demoScenes.path_demo_CityWithTraffic);
#endif

        // The AIO menu scene loads every other demo scene by name, so it must run first.
        // If demo content is installed (AIO path resolved) and AIO ended up anywhere but index 0, hoist it.
        bool aioMovedToFirst = false;
        if (!string.IsNullOrEmpty(demoScenes.path_city_AIO)) {
            int aioIdx = scenes.FindIndex(s => s.path == demoScenes.path_city_AIO);
            if (aioIdx > 0) {
                var aioScene = scenes[aioIdx];
                scenes.RemoveAt(aioIdx);
                scenes.Insert(0, aioScene);
                aioMovedToFirst = true;
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        return (added, skipped, aioMovedToFirst);

    }

    public void DeleteDemoContent() {

        if (!EditorUtility.DisplayDialog(
            "Realistic Car Controller Pro | Delete Demo Content",
            "Are you sure you want to delete all demo content? This cannot be undone.",
            "Delete",
            "Cancel"))
            return;

        if (RCCP_DemoContent.Instance != null && RCCP_DemoContent.Instance.content != null) {
            foreach (var folder in RCCP_DemoContent.Instance.content) {
                if (folder != null) {
                    string path = AssetDatabase.GetAssetPath(folder);
                    if (!string.IsNullOrEmpty(path))
                        FileUtil.DeleteFileOrDirectory(path);
                }
            }
        }

        RCCP_SetScriptingSymbol.SetEnabled("RCCP_DEMO", false);
        AssetDatabase.Refresh();

    }

    #endregion

}

#endif

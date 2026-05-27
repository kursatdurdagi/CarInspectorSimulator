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
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using BoneCrackerGames.RCCP.CoreProtection;

public class RCCP_InitLoad {

    /// <summary>
    /// SessionState key used to guard the "Show on startup" Welcome Window from reopening on every
    /// domain reload within a single editor session.
    /// </summary>
    private const string WelcomeShownThisSessionKey = "RCCP_WelcomeWindow_ShownThisSession";

    /// <summary>
    /// EditorPrefs key set during first-import (Cycle 1) to request that the next InitOnLoad
    /// cycle (Cycle 2, post-recompile) open the Welcome Window's first-run modal. Persisted via
    /// EditorPrefs so it survives the domain reload triggered by the BCG_RCCP symbol set.
    /// </summary>
    private const string PendingFirstRunWindowKey = "RCCP_PendingFirstRunWindow";

    /// <summary>
    /// EditorPrefs key set in CheckRP Cycle 1 (URP/HDRP detected, BCG_URP/BCG_HDRP not yet
    /// defined) to request that the next InitOnLoad cycle open the RP Converter window.
    /// Persisted via EditorPrefs so it survives the domain reload triggered by the
    /// BCG_URP/BCG_HDRP symbol set — opening the window in the same call races the queued
    /// reload, and EditorWindow.set_minSize NREs on a null HostView.
    /// </summary>
    private const string PendingRPConverterKey = "RCCP_PendingRPConverter";

    [InitializeOnLoadMethod]
    public static void InitOnLoad() {

        EditorApplication.delayCall += EditorDelayedUpdate;

    }

    public static void EditorDelayedUpdate() {

        RCCP_Installation.CheckProjectLayers();

        CheckSymbols();

        RCCP_Installation.CheckMissingWheelSlipParticles();

        RCCP_DemoScenes.Instance.GetPaths();

        RCCP_SceneUpdater.Check();

#if RCCP_PHOTON
        RCCP_DemoScenes_Photon.Instance.GetPaths();
#endif

#if BCG_ENTEREXIT
        BCG_DemoScenes.Instance.GetPaths();
#endif

#if RCCP_MIRROR
        RCCP_DemoScenes_Mirror.Instance.GetPaths();
#endif

        CheckRP();

        InitializeVerification();

    }

    /// <summary>
    /// Initializes purchase verification: records first-seen date and auto-registers with server.
    /// Registration is non-blocking — no UI is forced open.
    /// </summary>
    public static void InitializeVerification() {

        // Record first-seen date (survives reimport via EditorPrefs/registry)
        RCCP_CoreServerProxy.EnsureFirstSeenDate();

        // Auto-register with server in background (gets server-side created_at for anti-tamper)
        if (!RCCP_CoreServerProxy.IsRegistered && !RCCP_CoreServerProxy.IsVerified) {

            RCCP_CoreServerProxy.RegisterDevice(RCCP_Settings.Instance, (success, message) => { });

        }

    }

    public static void CheckSymbols() {

        bool hasKey = false;

#if BCG_RCCP && !RCCP_DEMO

        if (!RCCP_DemoContent.Instance.dontAskDemoContent) {

            // Mark as seen so this block only runs once. Demo import is available
            // from the Welcome Window (Demos tab) instead of a modal dialog.
            RCCP_DemoContent.Instance.dontAskDemoContent = true;
            EditorUtility.SetDirty(RCCP_DemoContent.Instance);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

#endif

#if BCG_RCCP
        hasKey = true;
#endif

        if (!hasKey) {

            // Cycle 1: flipping BCG_RCCP queues a recompile + domain reload. Opening the
            // Welcome Window in this same call would race the reload — EditorWindow.set_position
            // throws NRE because m_Parent (HostView) isn't wired yet while the editor is mid
            // asset-import / mid-recompile. Defer the open to the next InitOnLoad cycle by
            // persisting an EditorPrefs flag that survives the domain reload.
            EditorPrefs.SetBool(PendingFirstRunWindowKey, true);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_RCCP", true);

            EditorApplication.delayCall += () => {

                RCCP_Installation.CheckAllLayers();
                RCCP_SceneUpdater.CheckAllScenes();

            };

        } else if (EditorPrefs.GetBool(PendingFirstRunWindowKey, false)) {

            // Cycle 2 (post-recompile after first import): clear the flag and open the
            // first-run modal once the editor is settled. Mark the session-shown flag so the
            // verified/show-on-startup branch below doesn't also try to open the window.
            EditorPrefs.DeleteKey(PendingFirstRunWindowKey);
            SessionState.SetBool(WelcomeShownThisSessionKey, true);
            EditorApplication.delayCall += () => RCCP_WelcomeWindow.OpenWindowFirstRun();

        } else if ((EditorPrefs.GetBool(RCCP_WelcomeWindowController.ShowOnStartupPrefKey, false)
                    || !RCCP_CoreServerProxy.IsVerified)
                   && !SessionState.GetBool(WelcomeShownThisSessionKey, false)) {

            // Open once per editor session when either (a) the user opted in via the
            // "Show on startup" toggle, or (b) the device is not yet verified — in the
            // unverified case we force the window so the grace banner / Verify Now entry
            // point stays visible regardless of the toggle.
            // [InitializeOnLoadMethod] fires on every domain reload (e.g. each Play Mode
            // entry), so the SessionState flag survives reloads but resets on editor
            // restart — matching "once per session".
            SessionState.SetBool(WelcomeShownThisSessionKey, true);
            EditorApplication.delayCall += () => RCCP_WelcomeWindow.OpenWindow();

        }

    }

    public static void CheckRP() {

        // Cycle 2 (post-recompile after URP/HDRP detection in a previous cycle): the symbol
        // is now defined, open the converter once the editor is settled. Run this BEFORE the
        // pipeline detection so we don't re-trigger the Cycle 1 path on the same symbol that
        // was just enabled.
        if (EditorPrefs.GetBool(PendingRPConverterKey, false)) {

            EditorPrefs.DeleteKey(PendingRPConverterKey);
            EditorApplication.delayCall += () => RCCP_RenderPipelineConverterWindow.Init();
            return;

        }

        RenderPipelineAsset activePipeline;

        activePipeline = GraphicsSettings.currentRenderPipeline;

        if (activePipeline == null) {

            RCCP_SetScriptingSymbol.SetEnabled("BCG_URP", false);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);

        } else if (activePipeline.GetType().ToString().Contains("Universal")) {

#if !BCG_URP
            // Cycle 1: flipping BCG_URP queues a recompile + domain reload. Opening the
            // converter window in this same call races the reload — EditorWindow.set_minSize
            // dereferences m_Parent (HostView) which isn't wired yet during the queued reload.
            // Defer the open to the next InitOnLoad cycle via EditorPrefs.
            EditorPrefs.SetBool(PendingRPConverterKey, true);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_URP", true);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);
#endif

        } else if (activePipeline.GetType().ToString().Contains("HD")) {

#if !BCG_HDRP
            // Cycle 1: same race as the URP branch above, deferred via EditorPrefs.
            EditorPrefs.SetBool(PendingRPConverterKey, true);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_HDRP", true);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_URP", false);
#endif

        } else {

            RCCP_SetScriptingSymbol.SetEnabled("BCG_URP", false);
            RCCP_SetScriptingSymbol.SetEnabled("BCG_HDRP", false);

        }

    }

}

#endif

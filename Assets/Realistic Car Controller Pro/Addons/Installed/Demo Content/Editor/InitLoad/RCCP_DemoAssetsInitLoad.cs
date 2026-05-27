//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class RCCP_DemoAssetsInitLoad {

    /// <summary>
    /// EditorPrefs key set during first-import (Cycle 1, pre-recompile) to request that the
    /// next InitOnLoad cycle (Cycle 2, post-recompile) show the welcome dialogs and offer the
    /// RP material conversion. Persisted via EditorPrefs so it survives the domain reload
    /// triggered by the RCCP_DEMO symbol set — modal dialogs and window opens during the
    /// reload race the editor's HostView wiring and can NRE / hang.
    /// </summary>
    private const string PendingFirstRunKey = "RCCP_DemoAssets_PendingFirstRun";

    [InitializeOnLoadMethod]
    public static void InitOnLoad() {

        EditorApplication.delayCall += CheckSymbols;

    }

    public static void CheckSymbols() {

        bool hasKey = false;

#if RCCP_DEMO
        hasKey = true;
#endif

        if (!hasKey) {

            // Cycle 1: flipping RCCP_DEMO queues a recompile + domain reload. Showing modal
            // dialogs or opening EditorWindows in this same call would race the reload — the
            // dialogs can hang the editor, and EditorWindow.set_position can NRE because
            // m_Parent isn't wired yet. Defer the UI to the next InitOnLoad cycle.
            EditorPrefs.SetBool(PendingFirstRunKey, true);
            RCCP_SetScriptingSymbol.SetEnabled("RCCP_DEMO", true);

        } else if (EditorPrefs.GetBool(PendingFirstRunKey, false)) {

            // Cycle 2 (post-recompile after first import): clear the flag and run the
            // first-run UI once the editor is settled.
            EditorPrefs.DeleteKey(PendingFirstRunKey);

            EditorApplication.delayCall += () => {

                EditorUtility.DisplayDialog("Realistic Car Controller Pro | Demo Assets", "Demo assets have been imported successfully. You can add them to your build settings from welcome window (Tools --> BCG --> RCCP --> Welcome Window).\n\nRemember that, this will increase your build size even if you don't use any of them. You can always remove demo assets from the project at welcome window.", "Close");
                EditorUtility.DisplayDialog("Realistic Car Controller Pro | Demo Scenes", "Demo Scenes have been imported successfully. You can add them to your build settings from welcome window.", "Close");

                RCCP_Installation.CheckAllLayers();

                RCCP_SceneUpdater.Check();

                RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;

                if (rp == null)   // Built-in → nothing to convert
                    return;

                bool isURP = rp.GetType().ToString().Contains("Universal");
                bool isHDRP = rp.GetType().ToString().Contains("HD");

                if (!isURP && !isHDRP)
                    return;

                string rpName = isURP ? "URP" : "HDRP";
                bool ok = EditorUtility.DisplayDialog(
                    "Convert Materials",
                    $"Your project is using {rpName}.\n\n" +
                    $"You'll need to convert the imported assets to be working with {rpName}.?\n\n" +
                    $"You can open the RCCP Render Pipeline Converter Window and proceed.",
                    "Yes, open converter",
                    "No thanks"
                );

                if (!ok)
                    return;

                RCCP_RenderPipelineConverterWindow.Init();

            };

        }

    }

}

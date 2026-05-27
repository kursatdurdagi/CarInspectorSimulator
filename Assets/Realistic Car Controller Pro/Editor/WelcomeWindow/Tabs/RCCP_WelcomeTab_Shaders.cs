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
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Shaders tab for the Welcome Window.
/// Detects the active render pipeline and counts RCCP materials that need conversion.
/// </summary>
public class RCCP_WelcomeTab_Shaders : IRCCP_WelcomeTabContent {

    private VisualElement root;

    /// <summary>
    /// Creates the Shaders tab content.
    /// </summary>
    public VisualElement CreateContent() {

        root = new VisualElement();
        Rebuild();
        return root;

    }

    private void Rebuild() {

        if (root == null) return;
        root.Clear();

        string pipeline = DetectPipeline();
        int wrongPipelineMats = CountMaterialsNeedingConversion(pipeline);

        // Section header with detected pipeline.
        root.Add(RCCP_WelcomeWindowUI.CreateSection(
            "Render Pipeline",
            $"Detected: {pipeline}"
        ));

        if (pipeline == "Built-in") {

            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                "You're on the Built-in Render Pipeline. RCCP works out of the box — no conversion required.",
                "success"
            ));

            // Still offer the converter in case users want to revert from URP/HDRP.
            root.Add(RCCP_WelcomeWindowUI.CreateButton("Open Render Pipeline Converter", () => {
                RCCP_RenderPipelineConverterWindow.Init();
            }));

            return;

        }

        if (pipeline == "Unknown") {

            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                "Could not identify the active render pipeline. Check Project Settings > Graphics.",
                "warning"
            ));
            root.Add(RCCP_WelcomeWindowUI.CreateButton("Open Render Pipeline Converter", () => {
                RCCP_RenderPipelineConverterWindow.Init();
            }));
            return;

        }

        // URP or HDRP path.
        if (wrongPipelineMats > 0) {

            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                $"{wrongPipelineMats} RCCP material(s) are on shaders that don't match {pipeline}. " +
                "Open the converter to upgrade them.",
                "warning"
            ));

            root.Add(RCCP_WelcomeWindowUI.CreateButton("Open Render Pipeline Converter", () => {
                RCCP_RenderPipelineConverterWindow.Init();
            }, "primary"));

            root.Add(RCCP_WelcomeWindowUI.CreateButton("Rescan materials", () => Rebuild(), "link"));

        } else {

            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                $"All RCCP materials are on {pipeline}-compatible shaders. You're good to go.",
                "success"
            ));

            root.Add(RCCP_WelcomeWindowUI.CreateButton("Open Render Pipeline Converter", () => {
                RCCP_RenderPipelineConverterWindow.Init();
            }));

            root.Add(RCCP_WelcomeWindowUI.CreateButton("Rescan materials", () => Rebuild(), "link"));

        }

    }

    /// <summary>
    /// Returns "Built-in", "URP", "HDRP", or "Unknown".
    /// </summary>
    private static string DetectPipeline() {

        var pipeline = GraphicsSettings.currentRenderPipeline;

        if (pipeline == null)
            return "Built-in";

        string typeName = pipeline.GetType().ToString();

        if (typeName.Contains("Universal"))
            return "URP";

        if (typeName.Contains("HD"))
            return "HDRP";

        return "Unknown";

    }

    /// <summary>
    /// Scans RCCP material assets and counts those whose shader doesn't match the active pipeline.
    /// For URP/HDRP, a material is "wrong" if it's on a bare RCCP_Shader_* or on the other pipeline's variant.
    /// </summary>
    private static int CountMaterialsNeedingConversion(string pipeline) {

        if (pipeline != "URP" && pipeline != "HDRP")
            return 0;

        string[] searchFolders = GetSearchFolders();
        if (searchFolders.Length == 0)
            return 0;

        string[] guids = AssetDatabase.FindAssets("t:Material", searchFolders);
        string wrongSuffix = pipeline == "URP" ? "_HDRP" : "_URP";
        string rightSuffix = "_" + pipeline;
        int count = 0;

        foreach (string guid in guids) {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null || mat.shader == null)
                continue;

            string shaderName = mat.shader.name;

            if (!shaderName.StartsWith("RCCP_Shader_"))
                continue;

            if (shaderName.EndsWith(rightSuffix))
                continue;

            if (shaderName.EndsWith(wrongSuffix)) {
                count++;
                continue;
            }

            // Bare RCCP_Shader_* without pipeline suffix — needs conversion.
            count++;

        }

        return count;

    }

    private static string[] GetSearchFolders() {

        var folders = new System.Collections.Generic.List<string>();

        if (Directory.Exists("Assets/Realistic Car Controller Pro"))
            folders.Add("Assets/Realistic Car Controller Pro");

        if (Directory.Exists("Assets/BoneCracker Games Shared Assets"))
            folders.Add("Assets/BoneCracker Games Shared Assets");

        return folders.ToArray();

    }

    /// <summary>
    /// Called when the tab becomes visible. Refresh counts in case the user converted externally.
    /// </summary>
    public void OnActivated() {

        Rebuild();

    }

    /// <summary>
    /// Called when the tab is hidden.
    /// </summary>
    public void OnDeactivated() { }

}

#endif

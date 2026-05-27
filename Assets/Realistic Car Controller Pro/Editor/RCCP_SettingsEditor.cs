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
using BoneCrackerGames.RCCP.CoreProtection;

[CustomEditor(typeof(RCCP_Settings))]
public class RCCP_SettingsEditor : Editor {

    RCCP_Settings RCCSettingsAsset;
    GUISkin skin;
    Vector2 scrollPos;

    #region Fold States

    bool foldGeneralSettings;
    bool foldBehaviorSettings;
    bool foldControllerSettings;
    bool foldUISettings;
    bool foldWheelPhysics;
    bool foldTagsAndLayers;
    bool foldOptimization;
    bool foldResourcesSettings;

    // Resources sub-foldouts.
    bool foldResourcesLightPrefabs;
    bool foldResourcesCameraPrefabs;
    bool foldResourcesUIPrefabs;
    bool foldResourcesParticles;
    bool foldResourcesSoundFX;
    bool foldResourcesOtherPrefabs;
    bool foldResourcesRendering;

    #endregion

    #region Cached SerializedProperties

    // General.
    SerializedProperty prop_multithreading;
    SerializedProperty prop_overrideFixedTimeStep;
    SerializedProperty prop_fixedTimeStep;
    SerializedProperty prop_overrideFPS;
    SerializedProperty prop_maxAngularVelocity;
    SerializedProperty prop_maxFPS;
    SerializedProperty prop_useFixedWheelColliders;
    SerializedProperty prop_autoReset;

    // Behavior.
    SerializedProperty prop_overrideBehavior;
    SerializedProperty prop_behaviorTypes;

    // Controller.
    SerializedProperty prop_autoSaveLoadInputRebind;
    SerializedProperty prop_mobileControllerEnabled;
    SerializedProperty prop_mobileController;
    SerializedProperty prop_gyroSensitivity;

    // UI.
    SerializedProperty prop_useMPH;
    SerializedProperty prop_useTelemetry;
    SerializedProperty prop_useInputDebugger;

    // Tags & Layers.
    SerializedProperty prop_setLayers;
    SerializedProperty prop_RCCPLayer;
    SerializedProperty prop_RCCPWheelColliderLayer;
    SerializedProperty prop_RCCPDetachablePartLayer;
    SerializedProperty prop_RCCPPropLayer;
    SerializedProperty prop_RCCPObstacleLayer;

    // Optimization.
    SerializedProperty prop_useHeadLightsAsVertexLights;
    SerializedProperty prop_useBrakeLightsAsVertexLights;
    SerializedProperty prop_useReverseLightsAsVertexLights;
    SerializedProperty prop_useIndicatorLightsAsVertexLights;
    SerializedProperty prop_useOtherLightsAsVertexLights;

    // Resources - Light Prefabs.
    SerializedProperty prop_lightsSetupData;
    SerializedProperty prop_lightBox;

    // Resources - Camera Prefabs.
    SerializedProperty prop_RCCPMainCamera;
    SerializedProperty prop_RCCPHoodCamera;
    SerializedProperty prop_RCCPWheelCamera;
    SerializedProperty prop_RCCPCinematicCamera;
    SerializedProperty prop_RCCPFixedCamera;

    // Resources - UI Prefabs.
    SerializedProperty prop_RCCPCanvas;
    SerializedProperty prop_RCCPTelemetry;

    // Resources - Particles.
    SerializedProperty prop_contactParticles;
    SerializedProperty prop_scratchParticles;
    SerializedProperty prop_wheelSparkleParticles;

    // Resources - Sound FX.
    SerializedProperty prop_audioMixer;
    SerializedProperty prop_engineLowClipOn;
    SerializedProperty prop_engineLowClipOff;
    SerializedProperty prop_engineMedClipOn;
    SerializedProperty prop_engineMedClipOff;
    SerializedProperty prop_engineHighClipOn;
    SerializedProperty prop_engineHighClipOff;
    SerializedProperty prop_engineIdleClipOn;
    SerializedProperty prop_engineIdleClipOff;
    SerializedProperty prop_engineStartClip;
    SerializedProperty prop_reversingClip;
    SerializedProperty prop_windClip;
    SerializedProperty prop_brakeClip;
    SerializedProperty prop_wheelDeflateClip;
    SerializedProperty prop_wheelInflateClip;
    SerializedProperty prop_wheelFlatClip;
    SerializedProperty prop_indicatorClip;
    SerializedProperty prop_bumpClip;
    SerializedProperty prop_NOSClip;
    SerializedProperty prop_turboClip;
    SerializedProperty prop_gearClips;
    SerializedProperty prop_crashClips;
    SerializedProperty prop_blowoutClip;
    SerializedProperty prop_exhaustFlameClips;

    // Resources - Other Prefabs.
    SerializedProperty prop_exhaustGas;
    SerializedProperty prop_skidmarksManager;
    SerializedProperty prop_wheelBlur;

    // Resources - Rendering.
    SerializedProperty prop_lensFlareData;
    SerializedProperty prop_flare;
    SerializedProperty prop_flarePrefab;
    SerializedProperty prop_hdrpVolumeProfilePrefab;
    SerializedProperty prop_defaultDecalMaterial;
    SerializedProperty prop_defaultNeonMaterial;
    SerializedProperty prop_vehicleColliderMaterial;

    #endregion

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

        // Load fold states.
        RCCP_Settings inst = RCCP_Settings.Instance;

        foldGeneralSettings = inst.foldGeneralSettings;
        foldBehaviorSettings = inst.foldBehaviorSettings;
        foldControllerSettings = inst.foldControllerSettings;
        foldUISettings = inst.foldUISettings;
        foldWheelPhysics = inst.foldWheelPhysics;
        foldTagsAndLayers = inst.foldTagsAndLayers;
        foldOptimization = inst.foldOptimization;
        foldResourcesSettings = inst.foldResourcesSettings;
        foldResourcesLightPrefabs = inst.foldResourcesLightPrefabs;
        foldResourcesCameraPrefabs = inst.foldResourcesCameraPrefabs;
        foldResourcesUIPrefabs = inst.foldResourcesUIPrefabs;
        foldResourcesParticles = inst.foldResourcesParticles;
        foldResourcesSoundFX = inst.foldResourcesSoundFX;
        foldResourcesOtherPrefabs = inst.foldResourcesOtherPrefabs;
        foldResourcesRendering = inst.foldResourcesRendering;

        // Cache all serialized properties.
        prop_multithreading = serializedObject.FindProperty("multithreading");
        prop_overrideFixedTimeStep = serializedObject.FindProperty("overrideFixedTimeStep");
        prop_fixedTimeStep = serializedObject.FindProperty("fixedTimeStep");
        prop_overrideFPS = serializedObject.FindProperty("overrideFPS");
        prop_maxAngularVelocity = serializedObject.FindProperty("maxAngularVelocity");
        prop_maxFPS = serializedObject.FindProperty("maxFPS");
        prop_useFixedWheelColliders = serializedObject.FindProperty("useFixedWheelColliders");
        prop_autoReset = serializedObject.FindProperty("autoReset");

        prop_overrideBehavior = serializedObject.FindProperty("overrideBehavior");
        prop_behaviorTypes = serializedObject.FindProperty("behaviorTypes");

        prop_autoSaveLoadInputRebind = serializedObject.FindProperty("autoSaveLoadInputRebind");
        prop_mobileControllerEnabled = serializedObject.FindProperty("mobileControllerEnabled");
        prop_mobileController = serializedObject.FindProperty("mobileController");
        prop_gyroSensitivity = serializedObject.FindProperty("gyroSensitivity");

        prop_useMPH = serializedObject.FindProperty("useMPH");
        prop_useTelemetry = serializedObject.FindProperty("useTelemetry");
        prop_useInputDebugger = serializedObject.FindProperty("useInputDebugger");

        prop_setLayers = serializedObject.FindProperty("setLayers");
        prop_RCCPLayer = serializedObject.FindProperty("RCCPLayer");
        prop_RCCPWheelColliderLayer = serializedObject.FindProperty("RCCPWheelColliderLayer");
        prop_RCCPDetachablePartLayer = serializedObject.FindProperty("RCCPDetachablePartLayer");
        prop_RCCPPropLayer = serializedObject.FindProperty("RCCPPropLayer");
        prop_RCCPObstacleLayer = serializedObject.FindProperty("RCCPObstacleLayer");

        prop_useHeadLightsAsVertexLights = serializedObject.FindProperty("useHeadLightsAsVertexLights");
        prop_useBrakeLightsAsVertexLights = serializedObject.FindProperty("useBrakeLightsAsVertexLights");
        prop_useReverseLightsAsVertexLights = serializedObject.FindProperty("useReverseLightsAsVertexLights");
        prop_useIndicatorLightsAsVertexLights = serializedObject.FindProperty("useIndicatorLightsAsVertexLights");
        prop_useOtherLightsAsVertexLights = serializedObject.FindProperty("useOtherLightsAsVertexLights");

        prop_lightsSetupData = serializedObject.FindProperty("lightsSetupData");
        prop_lightBox = serializedObject.FindProperty("lightBox");

        prop_RCCPMainCamera = serializedObject.FindProperty("RCCPMainCamera");
        prop_RCCPHoodCamera = serializedObject.FindProperty("RCCPHoodCamera");
        prop_RCCPWheelCamera = serializedObject.FindProperty("RCCPWheelCamera");
        prop_RCCPCinematicCamera = serializedObject.FindProperty("RCCPCinematicCamera");
        prop_RCCPFixedCamera = serializedObject.FindProperty("RCCPFixedCamera");

        prop_RCCPCanvas = serializedObject.FindProperty("RCCPCanvas");
        prop_RCCPTelemetry = serializedObject.FindProperty("RCCPTelemetry");

        prop_contactParticles = serializedObject.FindProperty("contactParticles");
        prop_scratchParticles = serializedObject.FindProperty("scratchParticles");
        prop_wheelSparkleParticles = serializedObject.FindProperty("wheelSparkleParticles");

        prop_audioMixer = serializedObject.FindProperty("audioMixer");
        prop_engineLowClipOn = serializedObject.FindProperty("engineLowClipOn");
        prop_engineLowClipOff = serializedObject.FindProperty("engineLowClipOff");
        prop_engineMedClipOn = serializedObject.FindProperty("engineMedClipOn");
        prop_engineMedClipOff = serializedObject.FindProperty("engineMedClipOff");
        prop_engineHighClipOn = serializedObject.FindProperty("engineHighClipOn");
        prop_engineHighClipOff = serializedObject.FindProperty("engineHighClipOff");
        prop_engineIdleClipOn = serializedObject.FindProperty("engineIdleClipOn");
        prop_engineIdleClipOff = serializedObject.FindProperty("engineIdleClipOff");
        prop_engineStartClip = serializedObject.FindProperty("engineStartClip");
        prop_reversingClip = serializedObject.FindProperty("reversingClip");
        prop_windClip = serializedObject.FindProperty("windClip");
        prop_brakeClip = serializedObject.FindProperty("brakeClip");
        prop_wheelDeflateClip = serializedObject.FindProperty("wheelDeflateClip");
        prop_wheelInflateClip = serializedObject.FindProperty("wheelInflateClip");
        prop_wheelFlatClip = serializedObject.FindProperty("wheelFlatClip");
        prop_indicatorClip = serializedObject.FindProperty("indicatorClip");
        prop_bumpClip = serializedObject.FindProperty("bumpClip");
        prop_NOSClip = serializedObject.FindProperty("NOSClip");
        prop_turboClip = serializedObject.FindProperty("turboClip");
        prop_gearClips = serializedObject.FindProperty("gearClips");
        prop_crashClips = serializedObject.FindProperty("crashClips");
        prop_blowoutClip = serializedObject.FindProperty("blowoutClip");
        prop_exhaustFlameClips = serializedObject.FindProperty("exhaustFlameClips");

        prop_exhaustGas = serializedObject.FindProperty("exhaustGas");
        prop_skidmarksManager = serializedObject.FindProperty("skidmarksManager");
        prop_wheelBlur = serializedObject.FindProperty("wheelBlur");

        prop_lensFlareData = serializedObject.FindProperty("lensFlareData");
        prop_flare = serializedObject.FindProperty("flare");
        prop_flarePrefab = serializedObject.FindProperty("flarePrefab");
        prop_hdrpVolumeProfilePrefab = serializedObject.FindProperty("hdrpVolumeProfilePrefab");
        prop_defaultDecalMaterial = serializedObject.FindProperty("defaultDecalMaterial");
        prop_defaultNeonMaterial = serializedObject.FindProperty("defaultNeonMaterial");
        prop_vehicleColliderMaterial = serializedObject.FindProperty("vehicleColliderMaterial");

    }

    private void OnDestroy() {

        RCCP_Settings inst = RCCP_Settings.Instance;

        inst.foldGeneralSettings = foldGeneralSettings;
        inst.foldBehaviorSettings = foldBehaviorSettings;
        inst.foldControllerSettings = foldControllerSettings;
        inst.foldUISettings = foldUISettings;
        inst.foldWheelPhysics = foldWheelPhysics;
        inst.foldTagsAndLayers = foldTagsAndLayers;
        inst.foldOptimization = foldOptimization;
        inst.foldResourcesSettings = foldResourcesSettings;
        inst.foldResourcesLightPrefabs = foldResourcesLightPrefabs;
        inst.foldResourcesCameraPrefabs = foldResourcesCameraPrefabs;
        inst.foldResourcesUIPrefabs = foldResourcesUIPrefabs;
        inst.foldResourcesParticles = foldResourcesParticles;
        inst.foldResourcesSoundFX = foldResourcesSoundFX;
        inst.foldResourcesOtherPrefabs = foldResourcesOtherPrefabs;
        inst.foldResourcesRendering = foldResourcesRendering;

    }

    public override void OnInspectorGUI() {

        RCCSettingsAsset = (RCCP_Settings)target;
        serializedObject.Update();
        GUI.skin = skin;

        if (!RCCP_CoreServerProxy.IsVerified)
            DrawVerificationBanner();

        EditorGUIUtility.labelWidth = 250;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("RCCP Asset Settings Editor Window", EditorStyles.boldLabel);

        using (new RCCP_DesignSystem.ColorScope(new Color(.75f, 1f, .75f))) {
            EditorGUILayout.LabelField("This editor will keep update necessary .asset files in your project for RCCP. Don't change directory of the ''Resources/RCCP Assets''.", EditorStyles.helpBox);
        }

        EditorGUILayout.Space();
        EditorGUI.indentLevel++;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        EditorGUILayout.Space();
        DrawGeneralSettings();
        DrawBehaviorSettings();
        DrawControllerSettings();
        DrawUISettings();
        DrawWheelPhysics();
        DrawTagsAndLayers();
        DrawOptimization();
        DrawResources();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        DrawFooter();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(RCCSettingsAsset);

    }

    #region Section Drawers

    private void DrawGeneralSettings() {

        foldGeneralSettings = EditorGUILayout.Foldout(foldGeneralSettings, "General Settings");

        if (foldGeneralSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("General", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(prop_multithreading, new GUIContent("Multithreading"));
            EditorGUILayout.PropertyField(prop_overrideFixedTimeStep, new GUIContent("Override FixedTimeStep"));

            if (RCCSettingsAsset.overrideFixedTimeStep)
                EditorGUILayout.PropertyField(prop_fixedTimeStep, new GUIContent("Fixed Timestep"));

            EditorGUILayout.PropertyField(prop_overrideFPS, new GUIContent("Override FPS"));

            if (RCCSettingsAsset.overrideFPS)
                EditorGUILayout.PropertyField(prop_maxFPS, new GUIContent("Maximum FPS"));

            EditorGUILayout.PropertyField(prop_maxAngularVelocity, new GUIContent("Maximum Angular Velocity (rad/s)"));

            EditorGUILayout.HelpBox("These settings affect all RCCP vehicles globally. Per-vehicle overrides can be set on each RCCP_CarController component.", MessageType.Info);

            GUILayout.Label("Wheel Physics", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop_useFixedWheelColliders, new GUIContent("Use Fixed WheelColliders"));
            EditorGUILayout.PropertyField(prop_autoReset, new GUIContent("Auto Reset"));

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawBehaviorSettings() {

        foldBehaviorSettings = EditorGUILayout.Foldout(foldBehaviorSettings, "Behavior Settings");

        if (foldBehaviorSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Behavior Settings", EditorStyles.boldLabel);

            using (new RCCP_DesignSystem.ColorScope(new Color(.75f, 1f, .75f))) {
                EditorGUILayout.HelpBox("Using behavior preset will override wheelcollider settings, chassis joint, antirolls, and other stuff. Using ''Custom'' mode will not override anything.", MessageType.Info);
            }

            EditorGUILayout.PropertyField(prop_overrideBehavior, new GUIContent("Override Behavior"));

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prop_behaviorTypes, new GUIContent("Behavior Types"), true);
            EditorGUI.indentLevel--;

            if (RCCSettingsAsset.overrideBehavior) {

                List<string> behaviorTypeStrings = new List<string>();

                for (int i = 0; i < RCCSettingsAsset.behaviorTypes.Length; i++)
                    behaviorTypeStrings.Add(RCCSettingsAsset.behaviorTypes[i].behaviorName);

                using (new RCCP_DesignSystem.ColorScope(new Color(.5f, 1f, 1f, 1f))) {
                    RCCSettingsAsset.behaviorSelectedIndex = GUILayout.Toolbar(RCCSettingsAsset.behaviorSelectedIndex, behaviorTypeStrings.ToArray());
                }

            } else {

                EditorGUILayout.HelpBox("Enable 'Override Behavior' to activate behavior presets. When disabled, each vehicle uses its own component settings.", MessageType.None);

            }

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawControllerSettings() {

        foldControllerSettings = EditorGUILayout.Foldout(foldControllerSettings, "Controller Settings");

        if (foldControllerSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("Main Controller Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop_autoSaveLoadInputRebind, new GUIContent("Auto Save Load Input Rebind"));

            EditorGUILayout.Space();

            GUILayout.Label("Mobile Controller Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop_mobileControllerEnabled, new GUIContent("Mobile Controller Enabled"));

            if (RCCSettingsAsset.mobileControllerEnabled) {

                EditorGUILayout.Space();
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(prop_mobileController, new GUIContent("Mobile Controller Type"));
                EditorGUILayout.HelpBox("Mobile UI controller buttons will be used to receive player inputs through the RCCP_InputManager.", MessageType.Info);
                EditorGUILayout.PropertyField(prop_gyroSensitivity, new GUIContent("Gyro Sensitivity"));
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

            }

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawUISettings() {

        foldUISettings = EditorGUILayout.Foldout(foldUISettings, "UI Settings");

        if (foldUISettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("UI Dashboard Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop_useMPH, new GUIContent("Use MPH"));

            GUILayout.Label("Debug Overlays", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop_useTelemetry, new GUIContent("Use Telemetry"));
            EditorGUILayout.PropertyField(prop_useInputDebugger, new GUIContent("Use Input Debugger"));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawWheelPhysics() {

        foldWheelPhysics = EditorGUILayout.Foldout(foldWheelPhysics, "Wheel Physics Settings");

        if (foldWheelPhysics) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (RCCP_GroundMaterials.Instance.frictions != null && RCCP_GroundMaterials.Instance.frictions.Length > 0) {

                GUILayout.Label("Ground Physic Materials", EditorStyles.boldLabel);

                for (int i = 0; i < RCCP_GroundMaterials.Instance.frictions.Length; i++) {

                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.ObjectField("Ground Physic Materials " + i, RCCP_GroundMaterials.Instance.frictions[i].groundMaterial, typeof(PhysicsMaterial), false);
                    EditorGUILayout.EndVertical();

                }

                EditorGUILayout.Space();

            }

            using (new RCCP_DesignSystem.ColorScope(new Color(.5f, 1f, 1f, 1f))) {
                if (GUILayout.Button("Configure Ground Physic Materials"))
                    Selection.activeObject = RCCP_GroundMaterials.Instance;
            }

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawTagsAndLayers() {

        foldTagsAndLayers = EditorGUILayout.Foldout(foldTagsAndLayers, "Tags & Layers");

        if (foldTagsAndLayers) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Tags & Layers", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(prop_setLayers, new GUIContent("Set Layers Auto"), false);

            if (RCCSettingsAsset.setLayers) {

                DrawLayerField(prop_RCCPLayer, "Vehicle Layer", RCCSettingsAsset.RCCPLayer);
                DrawLayerField(prop_RCCPWheelColliderLayer, "WheelCollider Layer", RCCSettingsAsset.RCCPWheelColliderLayer);
                DrawLayerField(prop_RCCPDetachablePartLayer, "DetachablePart Layer", RCCSettingsAsset.RCCPDetachablePartLayer);
                DrawLayerField(prop_RCCPPropLayer, "Prop Layer", RCCSettingsAsset.RCCPPropLayer);
                DrawLayerField(prop_RCCPObstacleLayer, "Obstacle Layer", RCCSettingsAsset.RCCPObstacleLayer);

                using (new RCCP_DesignSystem.ColorScope(new Color(.75f, 1f, .75f))) {
                    EditorGUILayout.HelpBox("These layers are used for masking wheel rays, light masks, and projector masks. Layers are auto-created on import. If a layer shows as missing, create it in Edit --> Project Settings --> Tags & Layers.", MessageType.Info);
                }

            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawOptimization() {

        foldOptimization = EditorGUILayout.Foldout(foldOptimization, "Optimization");

        if (foldOptimization) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Optimization", EditorStyles.boldLabel);

            GUILayout.Label("Lights", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(prop_useHeadLightsAsVertexLights, new GUIContent("Head Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(prop_useBrakeLightsAsVertexLights, new GUIContent("Brake Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(prop_useReverseLightsAsVertexLights, new GUIContent("Reverse Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(prop_useIndicatorLightsAsVertexLights, new GUIContent("Indicator Lights As Vertex Lights"));
            EditorGUILayout.PropertyField(prop_useOtherLightsAsVertexLights, new GUIContent("Other Lights As Vertex Lights"));

            using (new RCCP_DesignSystem.ColorScope(new Color(.75f, 1f, .75f))) {
                EditorGUILayout.HelpBox("Always use vertex lights for mobile platform. Even only one pixel light will drop your performance dramaticaly!", MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    private void DrawResources() {

        foldResourcesSettings = EditorGUILayout.Foldout(foldResourcesSettings, "Resources");

        if (foldResourcesSettings) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Resources", EditorStyles.boldLabel);

            using (new RCCP_DesignSystem.ColorScope(new Color(.75f, 1f, .75f))) {
                EditorGUILayout.HelpBox("These are the initial resources for the initialization. RCCP will use these resources to setup things.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Light Prefabs.
            DrawResourcesLightPrefabs();

            // Camera Prefabs.
            DrawResourcesCameraPrefabs();

            // UI Prefabs.
            DrawResourcesUIPrefabs();

            // Particles.
            DrawResourcesParticles();

            // Sound FX.
            DrawResourcesSoundFX();

            // Other Prefabs.
            DrawResourcesOtherPrefabs();

            // Rendering.
            DrawResourcesRendering();

            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.Space();

    }

    #endregion

    #region Resources Sub-Section Drawers

    private void DrawResourcesLightPrefabs() {

        foldResourcesLightPrefabs = EditorGUILayout.Foldout(foldResourcesLightPrefabs, "Light Prefabs");

        if (foldResourcesLightPrefabs) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(prop_lightsSetupData, new GUIContent("Lights Setup Data"), true);
            EditorGUILayout.PropertyField(prop_lightBox, new GUIContent("Light Box"), false);
            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesCameraPrefabs() {

        foldResourcesCameraPrefabs = EditorGUILayout.Foldout(foldResourcesCameraPrefabs, "Camera Prefabs");

        if (foldResourcesCameraPrefabs) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(prop_RCCPMainCamera, new GUIContent("RCCP Main Camera"), false);
            EditorGUILayout.PropertyField(prop_RCCPHoodCamera, new GUIContent("RCCP Hood Camera"), false);
            EditorGUILayout.PropertyField(prop_RCCPCinematicCamera, new GUIContent("RCCP Cinematic Camera"), false);
            EditorGUILayout.PropertyField(prop_RCCPWheelCamera, new GUIContent("RCCP Wheel Camera"), false);
            EditorGUILayout.PropertyField(prop_RCCPFixedCamera, new GUIContent("RCCP Fixed Camera"), false);
            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesUIPrefabs() {

        foldResourcesUIPrefabs = EditorGUILayout.Foldout(foldResourcesUIPrefabs, "UI Prefabs");

        if (foldResourcesUIPrefabs) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(prop_RCCPCanvas, new GUIContent("RCCP UI Canvas"), false);
            EditorGUILayout.PropertyField(prop_RCCPTelemetry, new GUIContent("RCCP Telemetry Canvas"), false);
            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesParticles() {

        foldResourcesParticles = EditorGUILayout.Foldout(foldResourcesParticles, "Particles");

        if (foldResourcesParticles) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(prop_contactParticles, new GUIContent("Contact Particles"), false);
            EditorGUILayout.PropertyField(prop_scratchParticles, new GUIContent("Scratch Particles"), false);
            EditorGUILayout.PropertyField(prop_wheelSparkleParticles, new GUIContent("Wheel Sparkle Particles"), false);
            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesSoundFX() {

        foldResourcesSoundFX = EditorGUILayout.Foldout(foldResourcesSoundFX, "Sound FX");

        if (foldResourcesSoundFX) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.PropertyField(prop_audioMixer, new GUIContent("Audio Mixer"), false);
            EditorGUILayout.Space();

            GUILayout.Label("Engine Sounds", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(prop_engineIdleClipOn, new GUIContent("Engine Idle On"), false);
            EditorGUILayout.PropertyField(prop_engineIdleClipOff, new GUIContent("Engine Idle Off"), false);
            EditorGUILayout.PropertyField(prop_engineLowClipOn, new GUIContent("Engine Low On"), false);
            EditorGUILayout.PropertyField(prop_engineLowClipOff, new GUIContent("Engine Low Off"), false);
            EditorGUILayout.PropertyField(prop_engineMedClipOn, new GUIContent("Engine Med On"), false);
            EditorGUILayout.PropertyField(prop_engineMedClipOff, new GUIContent("Engine Med Off"), false);
            EditorGUILayout.PropertyField(prop_engineHighClipOn, new GUIContent("Engine High On"), false);
            EditorGUILayout.PropertyField(prop_engineHighClipOff, new GUIContent("Engine High Off"), false);
            EditorGUILayout.PropertyField(prop_engineStartClip, new GUIContent("Engine Start Clip"), false);

            EditorGUILayout.Space();
            GUILayout.Label("Vehicle Sounds", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(prop_reversingClip, new GUIContent("Reverse Transmission Clip"), false);
            EditorGUILayout.PropertyField(prop_windClip, new GUIContent("Wind Clip"), false);
            EditorGUILayout.PropertyField(prop_brakeClip, new GUIContent("Brake Clip"), false);
            EditorGUILayout.PropertyField(prop_indicatorClip, new GUIContent("Indicator Clip"), false);
            EditorGUILayout.PropertyField(prop_bumpClip, new GUIContent("Bump Clip"), false);
            EditorGUILayout.PropertyField(prop_NOSClip, new GUIContent("NOS Clip"), false);
            EditorGUILayout.PropertyField(prop_turboClip, new GUIContent("Turbo Clip"), false);
            EditorGUILayout.PropertyField(prop_exhaustFlameClips, new GUIContent("Exhaust Flame Clips"), true);
            EditorGUILayout.PropertyField(prop_gearClips, new GUIContent("Gear Shifting Clips"), true);
            EditorGUILayout.PropertyField(prop_crashClips, new GUIContent("Crash Clips"), true);

            EditorGUILayout.Space();
            GUILayout.Label("Wheel Sounds", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(prop_wheelDeflateClip, new GUIContent("Wheel Deflate Clip"), false);
            EditorGUILayout.PropertyField(prop_wheelInflateClip, new GUIContent("Wheel Inflate Clip"), false);
            EditorGUILayout.PropertyField(prop_wheelFlatClip, new GUIContent("Wheel Flat Clip"), false);
            EditorGUILayout.PropertyField(prop_blowoutClip, new GUIContent("Blowout Clips"), true);

            EditorGUILayout.Space();

            if (GUILayout.Button("Configure Wheel Slip Sounds"))
                Selection.activeObject = RCCP_GroundMaterials.Instance;

            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesOtherPrefabs() {

        foldResourcesOtherPrefabs = EditorGUILayout.Foldout(foldResourcesOtherPrefabs, "Other Prefabs");

        if (foldResourcesOtherPrefabs) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.PropertyField(prop_exhaustGas, new GUIContent("Exhaust Gas"), false);
            EditorGUILayout.PropertyField(prop_skidmarksManager, new GUIContent("Skidmarks Manager"), false);
            EditorGUILayout.PropertyField(prop_wheelBlur, new GUIContent("Wheel Blur"), false);
            EditorGUILayout.EndVertical();

        }

    }

    private void DrawResourcesRendering() {

        foldResourcesRendering = EditorGUILayout.Foldout(foldResourcesRendering, "Rendering");

        if (foldResourcesRendering) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

#if BCG_URP || BCG_HDRP
            EditorGUILayout.PropertyField(prop_lensFlareData, new GUIContent("LensFlare Data SRP"), false);
#endif
            EditorGUILayout.PropertyField(prop_flare, new GUIContent("Lens Flare (Legacy)"), false);
            EditorGUILayout.PropertyField(prop_flarePrefab, new GUIContent("Flare Prefab"), false);
#if BCG_HDRP
            EditorGUILayout.PropertyField(prop_hdrpVolumeProfilePrefab, new GUIContent("HDRP Volume Profile Prefab"), false);
#endif
            EditorGUILayout.PropertyField(prop_defaultDecalMaterial, new GUIContent("Default Decal Material"), false);
            EditorGUILayout.PropertyField(prop_defaultNeonMaterial, new GUIContent("Default Neon Material"), false);
            EditorGUILayout.PropertyField(prop_vehicleColliderMaterial, new GUIContent("Vehicle Collider Material"), false);

            EditorGUILayout.EndVertical();

        }

    }

    #endregion

    #region Helpers

    private void DrawLayerField(SerializedProperty prop, string label, string layerName) {

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop, new GUIContent(label), false);

        bool layerExists = LayerMask.NameToLayer(layerName) != -1;

        if (layerExists) {
            using (new RCCP_DesignSystem.ColorScope(Color.green)) {
                GUILayout.Label("OK", EditorStyles.miniLabel, GUILayout.Width(20));
            }
        } else {
            using (new RCCP_DesignSystem.ColorScope(new Color(1f, 0.4f, 0.4f))) {
                GUILayout.Label("Missing", EditorStyles.miniLabel, GUILayout.Width(45));
            }
        }

        EditorGUILayout.EndHorizontal();

    }

    #endregion

    #region Footer & Verification

    private void DrawFooter() {

        EditorGUILayout.BeginVertical(GUI.skin.button);

        using (new RCCP_DesignSystem.ColorScope(new Color(.5f, 1f, 1f, 1f))) {
            if (GUILayout.Button("Open PDF Documentation")) {

                UnityEngine.Object docAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RCCP_AssetPaths.documentationPath);
                if (docAsset != null)
                    AssetDatabase.OpenAsset(docAsset);
                else
                    EditorUtility.RevealInFinder("Assets/Realistic Car Controller Pro/Documentation");

            }
        }

        EditorGUILayout.LabelField("Realistic Car Controller Pro " + RCCP_Version.version + " \nBoneCracker Games", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));
        EditorGUILayout.LabelField("Developed by Ekrem Bugra Ozdoganlar", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        EditorGUILayout.EndVertical();

    }

    private void DrawVerificationBanner() {

        Color bannerColor = new Color(1f, 0.58f, 0f, 0.2f);
        Color textColor = new Color(1f, 0.85f, 0.5f);

        Rect bannerRect = EditorGUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(32));
        EditorGUI.DrawRect(bannerRect, bannerColor);

        GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel) {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            normal = { textColor = textColor },
            padding = new RectOffset(8, 0, 0, 0)
        };

        float buttonWidth = 100;
        Rect textRect = new Rect(bannerRect.x, bannerRect.y, bannerRect.width - buttonWidth - 8, bannerRect.height);
        GUI.Label(textRect, "Please verify your Asset Store purchase.", textStyle);

        Rect buttonRect = new Rect(bannerRect.xMax - buttonWidth - 4, bannerRect.y + 5, buttonWidth, bannerRect.height - 10);
        if (GUI.Button(buttonRect, "Verify Now")) {
            RCCP_WelcomeWindow.OpenWindowWithVerification();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

    }

    #endregion

}
#endif

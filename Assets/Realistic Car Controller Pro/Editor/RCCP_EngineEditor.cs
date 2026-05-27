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
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(RCCP_Engine))]
public class RCCP_EngineEditor : Editor {

    RCCP_Engine prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;
    private bool statsEnabled = true;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Engine)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Main power generator of the vehicle. Produces and transmits the generated power to the clutch.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideEngineRPM"), new GUIContent("Override Engine RPM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunning"), new GUIContent("Engine Running"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineStarting"), new GUIContent("Engine Starting"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCalculateDifferentialRatio"), new GUIContent("Auto Calculate Differential Ratio"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumSpeed"), new GUIContent("Maximum Speed"));

        if (!prop.autoCalculateDifferentialRatio)
            EditorGUILayout.HelpBox("Auto Calculate Differential Ratio is disabled. Maximum Speed has no effect; author finalDriveRatio manually on each Differential.", MessageType.Info);
        else if (prop.maximumSpeed <= 0f)
            EditorGUILayout.HelpBox("Maximum Speed is 0 or negative. Differential ratio auto-calculation will be skipped and current ratio will be kept.", MessageType.Info);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRPM"), new GUIContent("Engine RPM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minEngineRPM"), new GUIContent("Min Engine RPM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxEngineRPM"), new GUIContent("Max Engine RPM"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineAccelerationRate"), new GUIContent("Acceleration Rate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDynamicAcceleration"), new GUIContent("Dynamic Acceleration"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineCouplingToWheelsRate"), new GUIContent("Coupling To Wheels Rate"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineDecelerationRate"), new GUIContent("Deceleration Rate"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineBraking"), new GUIContent("Engine Braking"));

        if (prop.engineBraking) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineBrakingCoefficient"), new GUIContent("Braking Coefficient"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineBrakingMinRPMFactor"), new GUIContent("Min RPM Factor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineBrakingThrottleCutoff"), new GUIContent("Throttle Cutoff"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overRevEngineBrakingMultiplier"), new GUIContent("Over-Rev Multiplier"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCreateNMCurve"), new GUIContent("Auto Create Torque Curve"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maximumTorqueAsNM"), new GUIContent("Maximum Torque As NM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("peakRPM"), new GUIContent("Peak Torque RPM"));

        if (!prop.autoCreateNMCurve)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("NMCurve"), new GUIContent("Torque Curve NM"));
        else
            prop.CheckAndCreateNMCurve();

        // --- Torque & Power Graph ---
        EditorGUILayout.Space(4);

        var torqueCurveData = new RCCP_EditorGraph.CurveData {
            curve = prop.NMCurve,
            color = new Color(0.86f, 0.49f, 0.24f),
            label = "Torque",
            xMin = prop.minEngineRPM,
            xMax = prop.maxEngineRPM
        };

        var powerKeys = new Keyframe[32];
        float maxPower = 0f;

        for (int i = 0; i < 32; i++) {

            float rpm = Mathf.Lerp(prop.minEngineRPM, prop.maxEngineRPM, i / 31f);
            float torqueNorm = prop.NMCurve.Evaluate(rpm);
            float powerKW = torqueNorm * prop.maximumTorqueAsNM * rpm * 2f * Mathf.PI / 60f / 1000f;
            if (powerKW > maxPower) maxPower = powerKW;
            powerKeys[i] = new Keyframe(rpm, powerKW);

        }

        if (maxPower > 0f)
            for (int i = 0; i < powerKeys.Length; i++)
                powerKeys[i].value /= maxPower;

        var powerCurveData = new RCCP_EditorGraph.CurveData {
            curve = new AnimationCurve(powerKeys),
            color = new Color(0.3f, 0.5f, 0.9f),
            label = "Power",
            xMin = prop.minEngineRPM,
            xMax = prop.maxEngineRPM
        };

        var engineGraphTex = RCCP_EditorGraph.RenderCurves(
            new[] { torqueCurveData, powerCurveData }, 400, 150);

        RCCP_EditorGraph.DrawGraphLayout(engineGraphTex,
            "Engine Torque & Power", "RPM", null, 150f,
            new[] {
                new RCCP_EditorGraph.LegendEntry { color = new Color(0.86f, 0.49f, 0.24f), label = "Torque" },
                new RCCP_EditorGraph.LegendEntry { color = new Color(0.3f, 0.5f, 0.9f), label = "Power" }
            });

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRevLimiter"), new GUIContent("Rev Limiter"));

        if (prop.engineRevLimiter) {

            //EditorGUILayout.PropertyField(serializedObject.FindProperty("revLimiterThreshold"), new GUIContent("Rev Limiter Threshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("revLimiterCutFrequency"), new GUIContent("Rev Limiter Cut Frequency"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("turboCharged"), new GUIContent("Turbo Charged"));

        if (prop.turboCharged) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargePsi"), new GUIContent("Turbo Charge PSI"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxTurboChargePsi"), new GUIContent("Max Turbo Charge PSI"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargerCoEfficient"), new GUIContent("Turbo Charger Coefficient"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineFriction"), new GUIContent("Engine Friction"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("engineInertia"), new GUIContent("Engine Inertia"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("simulateEngineTemperature"), new GUIContent("Simulate Engine Temperature"));

        if (prop.simulateEngineTemperature) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTemperature"), new GUIContent("Engine Temperature"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("optimalTemperature"), new GUIContent("Optimal Temperature"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ambientTemperature"), new GUIContent("Ambient Temperature"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableVVT"), new GUIContent("Enable VVT"));

        if (prop.enableVVT) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("vvtOptimalRange"), new GUIContent("VVT Optimal Range"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vvtTorqueMultiplier"), new GUIContent("VVT Torque Multiplier"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableKnockDetection"), new GUIContent("Enable Knock Detection"));

        EditorGUILayout.Space();

        statsEnabled = EditorGUILayout.BeginToggleGroup(new GUIContent("Realtime Statistics"), statsEnabled);

        if (statsEnabled) {

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("Statistics will be updated at runtime", MessageType.Info);

            GUI.enabled = false;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRPM"), new GUIContent("Current Engine RPM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("producedTorqueAsNM"), new GUIContent("Produced Torque NM"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fuelInput"), new GUIContent("Current Fuel Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idleInput"), new GUIContent("Current Idle Input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineLoad"), new GUIContent("Current Engine Load"));

            if (prop.turboCharged) {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("turboChargePsi"), new GUIContent("Current Turbo PSI"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("turboBlowOut"), new GUIContent("Turbo Blow Out"));

            }

            if (prop.simulateEngineTemperature)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("engineTemperature"), new GUIContent("Current Engine Temperature"));

            if (prop.enableKnockDetection)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("knockFactor"), new GUIContent("Current Knock Factor"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("cutFuel"), new GUIContent("Rev Limiter Active"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineRunning"), new GUIContent("Engine Running"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engineStarting"), new GUIContent("Engine Starting"));

            GUI.enabled = true;

        }

        EditorGUILayout.EndToggleGroup();

        EditorGUILayout.Space();

        GUI.skin = null;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outputEvent"), new GUIContent("Output Event"));
        GUI.skin = skin;

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Add Output To Clutch")) {

                AddListener();
                EditorUtility.SetDirty(prop);

            }

            RCCP_DesignSystem.DrawBackButton(prop);

            RCCP_DesignSystem.HandleCheckComponents(prop, errorMessages);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        if (!EditorApplication.isPlaying) {

            if (prop.autoCalculateDifferentialRatio)
                prop.UpdateMaximumSpeed();

            if (GUI.changed)
                EditorSceneManager.MarkSceneDirty(prop.gameObject.scene);

        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private void AddListener() {

        if (prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true) == null) {

            Debug.LogError("Clutch not found. Event is not added.");
            return;

        }

        prop.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true),
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Clutch>(true), targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(prop.outputEvent, methodDelegate);

    }

}
#endif

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

[CustomEditor(typeof(RCCP_Input))]
public class RCCP_InputEditor : Editor {

    RCCP_Input prop;
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Input)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Receives player inputs from the RCCP_InputManager. All connected systems to the vehicle will be using player inputs if this is attached.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("overridePlayerInputs"), new GUIContent("Override Player Inputs"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideExternalInputs"), new GUIContent("Override External Inputs"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("throttleInput"), new GUIContent("Throttle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("steerInput"), new GUIContent("Steer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeInput"), new GUIContent("Brake"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeInput"), new GUIContent("Handbrake"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("clutchInput"), new GUIContent("Clutch"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nosInput"), new GUIContent("Nos"));

        EditorGUILayout.Space();

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("steeringCurve"), new GUIContent("Steering Curve"));

        // --- Steering Curve Graph ---
        EditorGUILayout.Space(4);

        var steerGraphTex = RCCP_EditorGraph.RenderCurve(
            prop.steeringCurve, 400, 120,
            new Color(0.86f, 0.49f, 0.24f),
            new Color(0.15f, 0.15f, 0.15f),
            0f, 200f, 0f, 1f);

        RCCP_EditorGraph.DrawGraphLayout(steerGraphTex,
            "Steering vs Speed", "Speed (km/h)", null, 120f,
            new[] {
                new RCCP_EditorGraph.LegendEntry { color = new Color(0.86f, 0.49f, 0.24f), label = "Steer Multiplier" }
            });

        EditorGUILayout.PropertyField(serializedObject.FindProperty("steeringLimiter"), new GUIContent("Steering Limiter"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("counterSteering"), new GUIContent("Counter Steering"));

        if (prop.counterSteering)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("counterSteerFactor"), new GUIContent("Counter Steering Factor"));

        GUI.color = guiColor;

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReverse"), new GUIContent("Auto Reverse Gear"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("inverseThrottleBrakeOnReverse"), new GUIContent("Inverse Throttle - Brake On Reverse Gear"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cutThrottleWhenShifting"), new GUIContent("Cut Throttle While Shifting"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyBrakeOnDisable"), new GUIContent("Apply Brake On Disable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyHandBrakeOnDisable"), new GUIContent("Apply Handbrake On Disable"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Deadzones", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("steeringDeadzone"), new GUIContent("Steering Deadzone"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("throttleDeadzone"), new GUIContent("Throttle Deadzone"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeDeadzone"), new GUIContent("Brake Deadzone"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeDeadzone"), new GUIContent("Handbrake Deadzone"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("clutchDeadzone"), new GUIContent("Clutch Deadzone"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nosDeadzone"), new GUIContent("Nos Deadzone"));

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton(prop);

            EditorGUILayout.EndVertical();

        }

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            RCCP_DesignSystem.DrawBehaviorOverrideWarning();

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }


}
#endif

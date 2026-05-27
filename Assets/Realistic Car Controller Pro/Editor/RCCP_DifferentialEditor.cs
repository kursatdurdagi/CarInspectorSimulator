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

[CustomEditor(typeof(RCCP_Differential))]
public class RCCP_DifferentialEditor : Editor {

    RCCP_Differential prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Differential)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox(
            "Transmits the received power from the engine → clutch → gearbox to the axle. " +
            "Open differential = RPM difference between both wheels will decide to which wheel needs more traction or not. " +
            "Limited = almost same with open with slip limitation. Higher percents = more close to the locked system. " +
            "Locked = both wheels will have the same traction.",
            MessageType.Info,
            true
        );

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("differentialType"),
            new GUIContent("Differential Type")
        );

        GUI.color = guiColor;

        if (prop.differentialType == RCCP_Differential.DifferentialType.Limited)
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("limitedSlipRatio"),
                new GUIContent("Limited Slip Ratio")
            );

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("overrideDifferential"),
            new GUIContent("Override Differential")
        );

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("finalDriveRatio"),
            new GUIContent("Final Drive Ratio")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("connectedAxle"),
            new GUIContent("Connected Axle"),
            true
        );

        EditorGUILayout.Space();

        GUI.enabled = false;
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("receivedTorqueAsNM"),
            new GUIContent("Received Torque As NM")
        );
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("producedTorqueAsNM"),
            new GUIContent("Produced Torque As NM")
        );
        GUI.enabled = true;

        DrawConnectionButtons();

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton(prop);

            RCCP_DesignSystem.HandleCheckComponents(prop, errorMessages);

            EditorGUILayout.EndVertical();

        }

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            RCCP_DesignSystem.DrawBehaviorOverrideWarning();

        RCCP_DesignSystem.ResetTransform(prop);

        // --- Statistics Section ---
        DrawStatisticsSection();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    /// <summary>
    /// Draws read-only differential calculation statistics in the inspector.
    /// </summary>
    private void DrawStatisticsSection() {

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        GUI.enabled = false;

        EditorGUILayout.FloatField(
            new GUIContent("Left Wheel RPM"),
            prop.leftWheelRPM
        );
        EditorGUILayout.FloatField(
            new GUIContent("Right Wheel RPM"),
            prop.rightWheelRPM
        );
        EditorGUILayout.FloatField(
            new GUIContent("Wheel Slip Ratio"),
            prop.wheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Left Wheel Slip Ratio"),
            prop.leftWheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Right Wheel Slip Ratio"),
            prop.rightWheelSlipRatio
        );
        EditorGUILayout.FloatField(
            new GUIContent("Output Left (Nm)"),
            prop.outputLeft
        );
        EditorGUILayout.FloatField(
            new GUIContent("Output Right (Nm)"),
            prop.outputRight
        );
        EditorGUILayout.FloatField(
            new GUIContent("Produced Torque As NM"),
            prop.producedTorqueAsNM
        );

        GUI.enabled = true;
        EditorGUILayout.EndVertical();

    }

    private void DrawConnectionButtons() {

        if (prop.connectedAxle == null) {

            RCCP_Axle[] axle = prop.GetComponentInParent<RCCP_CarController>(true)
                .GetComponentsInChildren<RCCP_Axle>(true);

            if (axle != null && axle.Length > 0) {

                for (int i = 0; i < axle.Length; i++) {

                    if (GUILayout.Button("Connect to " + axle[i].gameObject.name)) {

                        prop.connectedAxle = axle[i];
                        EditorUtility.SetDirty(prop);

                    }

                }

            }

        } else {

            GUI.color = Color.red;

            if (GUILayout.Button(
                "Remove connection to " + prop.connectedAxle.gameObject.name
            )) {

                bool decision = EditorUtility.DisplayDialog(
                    "Realistic Car Controller Pro | Remove connection to " +
                    prop.connectedAxle.gameObject.name,
                    "Are you sure want to remove connection to the " +
                    prop.connectedAxle.gameObject.name + "?",
                    "Yes",
                    "No"
                );

                if (decision) {

                    prop.connectedAxle = null;
                    EditorUtility.SetDirty(prop);

                }

            }

            GUI.color = guiColor;

        }

    }


}
#endif

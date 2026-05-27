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

[CustomEditor(typeof(RCCP_Limiter))]
public class RCCP_LimiterEditor : Editor {

    RCCP_Limiter prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Limiter)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Limits the maximum speed of the vehicle per each gear. Be sure length of the float array is same with the length of the gearbox gears.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("limitSpeedAtGear"), new GUIContent("Limit Speed At Gear"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSet"), new GUIContent("Auto Set From Gearbox"));

        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("limitingNow"), new GUIContent("Limiting Now"), true);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Downhill Force", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyDownhillForce"), new GUIContent("Apply Downhill Force"));

        if (prop.applyDownhillForce) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("downhillForceStrength"), new GUIContent("Downhill Force Strength"));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton<RCCP_OtherAddons>(prop);

            EditorGUILayout.EndVertical();

        }

        Undo.RecordObject(prop.transform, "Reset Limiter Transform");
        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
#endif

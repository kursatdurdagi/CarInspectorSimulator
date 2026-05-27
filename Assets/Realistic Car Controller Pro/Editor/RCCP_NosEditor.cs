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

[CustomEditor(typeof(RCCP_Nos))]
public class RCCP_NosEditor : Editor {

    RCCP_Nos prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Nos)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("NOS / Boost used to multiply engine torque for a limited time.", MessageType.Info, true);

        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("nosInUse"), new GUIContent("Nos In Use"));
        GUI.enabled = true;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("torqueMultiplier"), new GUIContent("Torque Multiplier"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("durationTime"), new GUIContent("Duration Time"));

        GUI.enabled = false;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("timer"), new GUIContent("Timer"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("amount"), new GUIContent("Amount"));
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("regenerateTime"), new GUIContent("Regenerate Time"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("regenerateRate"), new GUIContent("Regenerate Rate"));

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton<RCCP_OtherAddons>(prop);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
#endif

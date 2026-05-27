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

[CustomEditor(typeof(RCCP_AeroDynamics))]
public class RCCP_AeroDynamicsEditor : Editor {

    RCCP_AeroDynamics prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_AeroDynamics)target;
        serializedObject.Update();
        GUI.skin = skin;

        Transform com = prop.COM;

        EditorGUILayout.HelpBox("Manages the dynamics of the vehicle.", MessageType.Info, true);

        if (GUILayout.Button(new GUIContent("COM", "Centre of mass. Must be placed correctly. You can google it for vehicles to see which locations are suitable.")))
            Selection.activeGameObject = prop.COM.gameObject;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("dynamicCOM"), new GUIContent("Dynamic COM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreRigidbodyDragOnAccelerate"), new GUIContent("Ignore Drag On Accelerate"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("downForce"), new GUIContent("Downforce"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("airResistance"), new GUIContent("Air Resistance"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelResistance"), new GUIContent("Wheel Resistance"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoReset"), new GUIContent("Auto Reset"));

        if (prop.autoReset)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoResetTime"), new GUIContent("Auto Reset Timer"));

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton(prop);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
#endif

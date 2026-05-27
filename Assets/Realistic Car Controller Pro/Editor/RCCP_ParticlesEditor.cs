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

[CustomEditor(typeof(RCCP_Particles))]
public class RCCP_ParticlesEditor : Editor {

    RCCP_Particles prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    GUISkin orgskin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Particles)target;
        serializedObject.Update();

        if (orgskin == null)
            orgskin = GUI.skin;

        GUI.skin = skin;

        EditorGUILayout.HelpBox("Particles.", MessageType.Info, true);

        GUI.skin = orgskin;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionFilter"), new GUIContent("Collision Filter"));
        GUI.skin = skin;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("contactSparklePrefab"), new GUIContent("Contact Sparkle Prefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scratchSparklePrefab"), new GUIContent("Scratch Sparkle Prefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelSparklePrefab"), new GUIContent("Wheel Sparkle Prefab"));

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton(prop);

            RCCP_DesignSystem.HandleCheckComponents(prop, errorMessages);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
#endif

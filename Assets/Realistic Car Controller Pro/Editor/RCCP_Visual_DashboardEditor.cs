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

[CustomEditor(typeof(RCCP_Visual_Dashboard))]
public class RCCP_Visual_DashboardEditor : Editor {

    RCCP_Visual_Dashboard prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Visual_Dashboard)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Visual elements of the dashboard such as steering wheel, needles, interior lights, etc...", MessageType.Info, true);

        DrawDefaultInspector();

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

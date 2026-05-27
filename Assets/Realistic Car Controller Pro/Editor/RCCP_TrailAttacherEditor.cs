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

[CustomEditor(typeof(RCCP_TrailerAttacher))]
public class RCCP_TrailAttacherEditor : Editor {

    RCCP_TrailerAttacher prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_TrailerAttacher)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("It's a trigger box collider only. If this box collider triggers with another box collider with this component, Configurable Joint of the target gameobject will be connected to the other one.", MessageType.Info, true);

        DrawDefaultInspector();

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (prop.GetComponentInParent<RCCP_CarController>(true) && !EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton<RCCP_OtherAddons>(prop);

            EditorGUILayout.EndVertical();

        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

}
#endif

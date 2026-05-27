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

[CustomEditor(typeof(RCCP_Recorder))]
public class RCCP_RecorderEditor : Editor {

    RCCP_Recorder prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Recorder)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Records and replays the clips on this vehicle.", MessageType.Info, true);

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

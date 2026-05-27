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

[CustomEditor(typeof(RCCP_Axles))]
public class RCCP_AxlesEditor : Editor {

    RCCP_Axles prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Axles)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("All axles will be connected to this manager. Create / remove / edit any axle.", MessageType.Info, true);

        RCCP_Axle[] allAxles = prop.GetComponentsInChildren<RCCP_Axle>(true);

        if (allAxles != null && allAxles.Length > 0) {

            for (int i = 0; i < allAxles.Length; i++) {

                if (allAxles[i] != null) {

                    if (GUILayout.Button(allAxles[i].transform.name))
                        Selection.activeGameObject = allAxles[i].gameObject;

                }

            }

        }

        EditorGUILayout.Space();

        GUI.color = Color.green;

        if (GUILayout.Button("Create New Axle")) {

            bool decision = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Creating a new axle", "Are you sure want to create a new axle?", "Yes", "No");

            if (decision) {

                CreateNewAxle();
                EditorUtility.SetDirty(prop);

            }

        }

        GUI.color = guiColor;

        EditorGUILayout.Space();

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

    private void CreateNewAxle() {

        GameObject newAxle = new GameObject("RCCP_Axle_New");
        newAxle.transform.SetParent(prop.transform, false);
        newAxle.AddComponent<RCCP_Axle>();

    }

}
#endif

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

[CustomEditor(typeof(RCCP_Exterior_Cameras))]
public class RCCP_ExteriorCamerasEditor : Editor {

    RCCP_Exterior_Cameras prop;
    GUISkin skin;

    private void OnEnable() {

        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Exterior_Cameras)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Exterior cameras attached to the vehicle as hood camera and wheel camera.", MessageType.Info, true);

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_HoodCamera hoodCamera = prop.GetComponentInChildren<RCCP_HoodCamera>(true);
            RCCP_WheelCamera wheelCamera = prop.GetComponentInChildren<RCCP_WheelCamera>(true);

            if (hoodCamera == null) {

                if (GUILayout.Button("Add Hood / Cockpit Camera")) {

                    Selection.activeGameObject = CreateHoodCamera();
                    SceneView.FrameLastActiveSceneView();
                    EditorUtility.SetDirty(prop);

                }

            } else {

                if (GUILayout.Button("Select Hood / Cockpit Camera")) {

                    Selection.activeGameObject = hoodCamera.gameObject;
                    SceneView.FrameLastActiveSceneView();

                }

            }

            if (wheelCamera == null) {

                if (GUILayout.Button("Add Wheel Camera")) {

                    Selection.activeGameObject = CreateWheelCamera();
                    SceneView.FrameLastActiveSceneView();
                    EditorUtility.SetDirty(prop);

                }

            } else {

                if (GUILayout.Button("Select Wheel Camera")) {

                    Selection.activeGameObject = wheelCamera.gameObject;
                    SceneView.FrameLastActiveSceneView();

                }

            }

            RCCP_DesignSystem.DrawBackButton<RCCP_OtherAddons>(prop);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private GameObject CreateHoodCamera() {

        GameObject hoodCam = Instantiate(RCCP_Settings.Instance.RCCPHoodCamera, prop.transform.position, prop.transform.rotation, prop.transform);
        hoodCam.name = RCCP_Settings.Instance.RCCPHoodCamera.name;
        hoodCam.GetComponent<RCCP_HoodCamera>().Reset();
        return hoodCam;

    }

    private GameObject CreateWheelCamera() {

        GameObject wheelCam = Instantiate(RCCP_Settings.Instance.RCCPWheelCamera, prop.transform.position, prop.transform.rotation, prop.transform);
        wheelCam.name = RCCP_Settings.Instance.RCCPWheelCamera.name;
        return wheelCam;

    }

}
#endif

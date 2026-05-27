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

[CustomEditor(typeof(RCCP_Axle))]
public class RCCP_AxleEditor : Editor {

    RCCP_Axle prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Axle)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Transmits the received power from the differential and share to the wheels (if differential is connected to this axle). Steering, braking, traction, and all wheel related processes are managed by this axle. Has two connected wheels.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftWheelModel"), new GUIContent("Left Wheel Model"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightWheelModel"), new GUIContent("Right Wheel Model"));
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftWheelCollider"), new GUIContent("Left WheelCollider"));

        if (prop.leftWheelCollider) {

            if (!EditorApplication.isPlaying && prop.leftWheelModel)
                prop.leftWheelCollider.wheelModel = prop.leftWheelModel;

            if (GUILayout.Button("Edit"))
                Selection.activeGameObject = prop.leftWheelCollider.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightWheelCollider"), new GUIContent("Right WheelCollider"));

        if (prop.rightWheelCollider) {

            if (!EditorApplication.isPlaying && prop.rightWheelModel)
                prop.rightWheelCollider.wheelModel = prop.rightWheelModel;

            if (GUILayout.Button("Edit"))
                Selection.activeGameObject = prop.rightWheelCollider.gameObject;

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAlignWheelColliders"), new GUIContent("Auto Align WheelColliders"));

        EditorGUILayout.Space();

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("antirollForce"), new GUIContent("AntiRoll Force"));

        GUI.color = guiColor;

        EditorGUILayout.Space();

        if (prop.isPower)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("powerMultiplier"), new GUIContent("Power Multiplier"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isSteer"), new GUIContent("Is Steer"));

        if (prop.isSteer) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSteerAngle"), new GUIContent("Maximum Steer Angle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerSpeed"), new GUIContent("Steering Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerMultiplier"), new GUIContent("Steer Multiplier"));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("isBrake"), new GUIContent("Is Brake"));

        if (prop.isBrake) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxBrakeTorque"), new GUIContent("Maximum Brake Torque"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeMultiplier"), new GUIContent("Brake Multiplier"));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("isHandbrake"), new GUIContent("Is Handbrake"));

        if (prop.isHandbrake) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeMultiplier"), new GUIContent("Handbrake Multiplier"));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.Space();

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            if (prop.autoAlignWheelColliders && !EditorApplication.isPlaying) {

                if (prop.leftWheelCollider)
                    prop.leftWheelCollider.AlignWheel();

                if (prop.rightWheelCollider)
                    prop.rightWheelCollider.AlignWheel();

            }

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Back"))
                Selection.activeGameObject = prop.GetComponentInParent<RCCP_Axles>(true).gameObject;

            GUI.color = Color.red;

            if (GUILayout.Button("Remove")) {

                RemoveAxle();
                EditorUtility.SetDirty(prop);

            }

            GUI.color = guiColor;

            RCCP_DesignSystem.HandleCheckComponents(prop, errorMessages);

            EditorGUILayout.EndVertical();

        }

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            RCCP_DesignSystem.DrawBehaviorOverrideWarning();

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }


    public void RemoveAxle() {

        bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(prop.gameObject);

        if (isPrefab) {

            bool unpackPrefab = EditorUtility.DisplayDialog("Realistic Car Controller Pro | Can't Remove The Object", "This vehicle is connected to a prefab. In order to delete a gameobject, you must ubpack the prefab. Would you like to unpack the prefab completely?", "Unpack", "Don't Unpack");

            if (unpackPrefab)
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(prop.gameObject), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            return;

        } else {

            DestroyImmediate(prop.gameObject);

        }

    }

}
#endif

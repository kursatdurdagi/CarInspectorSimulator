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

[CustomEditor(typeof(RCCP_Clutch))]
public class RCCP_ClutchEditor : Editor {

    RCCP_Clutch prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Clutch)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("Connecter between engine and the gearbox. Transmits the received power from the engine to the gearbox or not.", MessageType.Info, true);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("clutchInput"), new GUIContent("Input"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("clutchInertia"), new GUIContent("Inertia"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("automaticClutch"), new GUIContent("Automatic Clutch"));

        if (prop.automaticClutch) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageRPM"), new GUIContent("Engage RPM"));

        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("forceToPressClutch"), new GUIContent("Force To Press Clutch"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pressClutchWhileShiftingGears"), new GUIContent("Press Clutch While Shifting Gears"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pressClutchWhileHandbraking"), new GUIContent("Press Clutch While Handbraking"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideClutch"), new GUIContent("Override Clutch"));

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("receivedTorqueAsNM"), new GUIContent("Received Torque As NM"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("producedTorqueAsNM"), new GUIContent("Produced Torque As NM"));

        GUI.skin = null;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outputEvent"), new GUIContent("Output Event"));
        GUI.skin = skin;

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (GUILayout.Button("Add Output To Gearbox")) {

                AddListener();
                EditorUtility.SetDirty(prop);

            }

            RCCP_DesignSystem.DrawBackButton(prop);

            RCCP_DesignSystem.HandleCheckComponents(prop, errorMessages);

            EditorGUILayout.EndVertical();

        }

        RCCP_DesignSystem.ResetTransform(prop);

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    private void AddListener() {

        if (prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true) == null) {

            Debug.LogError("Gearbox not found. Event is not added.");
            return;

        }

        prop.outputEvent = new RCCP_Event_Output();

        var targetinfo = UnityEvent.GetValidMethodInfo(prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true),
"ReceiveOutput", new Type[] { typeof(RCCP_Output) });

        var methodDelegate = Delegate.CreateDelegate(typeof(UnityAction<RCCP_Output>), prop.GetComponentInParent<RCCP_CarController>(true).GetComponentInChildren<RCCP_Gearbox>(true), targetinfo) as UnityAction<RCCP_Output>;
        UnityEventTools.AddPersistentListener(prop.outputEvent, methodDelegate);

    }

}
#endif

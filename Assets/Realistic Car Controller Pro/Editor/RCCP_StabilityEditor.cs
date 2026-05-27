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

[CustomEditor(typeof(RCCP_Stability))]
public class RCCP_StabilityEditor : Editor {

    RCCP_Stability prop;
    List<string> errorMessages = new List<string>();
    GUISkin skin;
    private Color guiColor;

    // Foldout state — persists per-editor-instance, default collapsed so new users
    // aren't overwhelmed by the 11 ESP V2 tuning parameters.
    private bool showESPV2AdvancedTuning = false;

    // Default collapsed so tuning users aren't forced to look at live telemetry.
    // Toggled open when debugging ESP/ABS/TCS behavior in play mode.
    private bool showDebugStatistics = false;

    private void OnEnable() {

        guiColor = GUI.color;
        skin = RCCP_DesignSystem.Skin;

    }

    public override bool RequiresConstantRepaint() {
        return showDebugStatistics && Application.isPlaying;
    }

    public override void OnInspectorGUI() {

        prop = (RCCP_Stability)target;
        serializedObject.Update();
        GUI.skin = skin;

        EditorGUILayout.HelpBox("ABS = Anti-skid braking system, ESP = Detects vehicle skidding movements, and actively counteracts them., TCS = Detects if a loss of traction occurs among the vehicle's wheels.", MessageType.Info, true);

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("ABS"), new GUIContent("ABS"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ESP"), new GUIContent("ESP"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TCS"), new GUIContent("TCS"));

        GUI.color = guiColor;

        EditorGUILayout.Space();

        if (prop.ABS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageABSThreshold"), new GUIContent("Engage ABS Threshold"));
        if (prop.ESP)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("espDeadband"), new GUIContent("ESP Activation Threshold (deg/s)"));
        if (prop.TCS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("engageTCSThreshold"), new GUIContent("Engage TCS Threshold"));

        EditorGUILayout.Space();

        if (prop.ABS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ABSIntensity"), new GUIContent("ABSIntensity"));
        if (prop.ESP)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ESPIntensity"), new GUIContent("ESPIntensity"));
        if (prop.TCS)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TCSIntensity"), new GUIContent("TCSIntensity"));

        // --- ESP V2 tuning section ---
        if (prop.ESP) {

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ESP V2", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("espMode"), new GUIContent("ESP Mode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveSpeedFactor"), new GUIContent("Preserve Speed (Arcade)"));

            // Advanced tuning is hidden behind a collapsed foldout so new users see only
            // the primary ESP Mode dropdown. Power users who need to tune hysteresis,
            // bicycle-model gains, PD controller, or sideslip thresholds can expand.
            showESPV2AdvancedTuning = EditorGUILayout.Foldout(showESPV2AdvancedTuning, "Advanced Tuning", true);

            if (showESPV2AdvancedTuning) {

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Hysteresis & Timing", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espDeactivationDeadband"), new GUIContent("Deactivation Threshold (deg/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espMinInterventionTime"), new GUIContent("Min Intervention Time (s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espModeCommitTime"), new GUIContent("Mode Commit Time (s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espMinNoticeableBrakeTorque"), new GUIContent("Indicator Brake Threshold (Nm)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espUIMinHoldTime"), new GUIContent("UI Hold Time (s)"));

                EditorGUILayout.LabelField("Bicycle Model", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("understeerGradient"), new GUIContent("Understeer Gradient (K_us)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("estimatedMu"), new GUIContent("Estimated μ (Friction)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("yawRateTimeConstant"), new GUIContent("Yaw Rate Lag τ (s)"));

                EditorGUILayout.LabelField("PD Controller", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espPGain"), new GUIContent("P Gain (× mass)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espDGain"), new GUIContent("D Gain (× mass)"));

                EditorGUILayout.LabelField("Sideslip β", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sideslipMaxAngle"), new GUIContent("Max Sideslip Angle (deg)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sideslipMaxRate"), new GUIContent("Max Sideslip Rate (deg/s)"));

                EditorGUILayout.LabelField("Smoothing", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("espBrakeSmoothing"), new GUIContent("ESP Brake Smoothing"));
                EditorGUI.indentLevel--;

            }

        }

        EditorGUILayout.Space();

        showDebugStatistics = EditorGUILayout.Foldout(showDebugStatistics, "Debug / Statistics", true, EditorStyles.foldoutHeader);

        if (showDebugStatistics) {

            EditorGUI.indentLevel++;
            GUI.enabled = false;

            EditorGUILayout.LabelField("Engagement", EditorStyles.miniLabel);
            if (prop.ABS)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ABSEngaged"), new GUIContent("ABS Engaged"));
            if (prop.ESP) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ESPEngaged"), new GUIContent("ESP Active"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ESPIndicatorEngaged"), new GUIContent("ESP Indicator"));
            }
            if (prop.TCS)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("TCSEngaged"), new GUIContent("TCS Engaged"));

            if (prop.ESP) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("ESP V2 Telemetry", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugYawActualDegS"), new GUIContent("Yaw Actual (deg/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugYawRefDegS"), new GUIContent("Yaw Reference (deg/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugYawErrorDegS"), new GUIContent("Yaw Error (deg/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugSideslipAngleDeg"), new GUIContent("Sideslip β (deg)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugSideslipRateDegS"), new GUIContent("Sideslip Rate (deg/s)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugActiveWheelIndex"), new GUIContent("Active Wheel (-1=None / 0=FL / 1=FR / 2=RL / 3=RR)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugIsOversteer"), new GUIContent("Is Oversteer"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Drift / Grounding", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftIntensity"), new GUIContent("Drift Intensity (0-1)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("groundedFactor"), new GUIContent("Grounded Factor (0-1)"));

            GUI.enabled = true;
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.Space();

        if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
            GUI.color = Color.red;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("steeringHelper"), new GUIContent("Steering Helper"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tractionHelper"), new GUIContent("Traction Helper"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDragHelper"), new GUIContent("Angular Drag Helper"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("driftAngleLimiter"), new GUIContent("Drift Angle Limiter"));

        if (prop.driftAngleLimiter) {

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDriftAngle"), new GUIContent("Max Drift Angle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftAngleCorrectionFactor"), new GUIContent("Drift Angle Correction Factor"));
            EditorGUI.indentLevel--;

        }

        EditorGUILayout.Space();

        if (prop.steeringHelper) {

            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerHelperStrength"), new GUIContent("Steering Helper Strength"));

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Speed Range", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minSpeedHelperStrength"), new GUIContent("Min Strength (low speed)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSpeedHelperStrength"), new GUIContent("Max Strength (high speed)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fullHelperSpeed"), new GUIContent("Full Strength Speed (km/h)"));

            EditorGUILayout.LabelField("Steer Assist", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minSteerAngleForAssist"), new GUIContent("Min Steer Angle (deg)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerAssistMinSpeed"), new GUIContent("Assist Min Speed (km/h)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerAssistMaxSpeed"), new GUIContent("Assist Max Speed (km/h)"));

            EditorGUILayout.LabelField("High Speed Safety", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("highSpeedThreshold"), new GUIContent("High Speed Threshold (km/h)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("highSpeedSafetyRange"), new GUIContent("Safety Range (km/h)"));

            EditorGUILayout.LabelField("Smoothing", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("steerHelperForceSmoothing"), new GUIContent("Force Smoothing"));
            EditorGUI.indentLevel--;

        }
        if (prop.tractionHelper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tractionHelperStrength"), new GUIContent("Traction Helper Strength"));
        if (prop.angularDragHelper)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("angularDragHelperStrength"), new GUIContent("Angular Drag Helper Strength"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("driftForceSmoothing"), new GUIContent("Drift Force Smoothing"));

        GUI.color = guiColor;

        EditorGUILayout.Space();

        // Drift system parameters section. Resolve the active behavior per-vehicle when the
        // Stability component is parented to a CarController — useCustomBehavior + customBehaviorIndex
        // override the global selection, so reading RCCP_Settings.SelectedBehaviorType alone would
        // miss per-vehicle drift presets. Fall back to the global selection when the component is
        // inspected in isolation (no parent CarController found).
        RCCP_Settings.BehaviorType activeBehavior = prop.CarController != null
            ? prop.CarController.GetVehicleBehaviorType()
            : (RCCP_Settings.Instance != null ? RCCP_Settings.Instance.SelectedBehaviorType : null);
        bool driftModeActive = activeBehavior != null && activeBehavior.driftMode;

        if (driftModeActive || Application.isPlaying) {

            EditorGUILayout.LabelField("Drift System", EditorStyles.boldLabel);

            if (Application.isPlaying) {

                GUI.enabled = false;
                EditorGUILayout.Slider(new GUIContent("Drift Intensity"), prop.driftIntensity, 0f, 1f);
                GUI.enabled = true;

            }

            if (RCCP_DesignSystem.IsBehaviorOverridden(prop))
                GUI.color = Color.red;

            EditorGUILayout.LabelField("Forces", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftYawTorqueMultiplier"), new GUIContent("Yaw Torque Multiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftForwardForceMultiplier"), new GUIContent("Forward Force Multiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftSidewaysForceMultiplier"), new GUIContent("Sideways Force Multiplier"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftMinSpeed"), new GUIContent("Min Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftFullForceSpeed"), new GUIContent("Full Force Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftThrottleYawFactor"), new GUIContent("Throttle Yaw Factor"));
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Friction", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftRearSidewaysStiffnessMin"), new GUIContent("Rear Sideways Stiffness Min"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftRearForwardStiffnessMin"), new GUIContent("Rear Forward Stiffness Min"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftFrontSidewaysStiffnessMin"), new GUIContent("Front Sideways Stiffness Min"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftFrictionResponseSpeed"), new GUIContent("Friction Response Speed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftFrictionRecoverySpeed"), new GUIContent("Friction Recovery Speed"));
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Recovery", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftMaxAngularVelocity"), new GUIContent("Max Angular Velocity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftCounterSteerRecoveryBoost"), new GUIContent("Counter Steer Recovery Boost"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("driftMomentumMaintenanceForce"), new GUIContent("Momentum Maintenance Force"));
            EditorGUI.indentLevel--;

            GUI.color = guiColor;

        }

        EditorGUILayout.Space();
        RCCP_DesignSystem.DrawSkinSeparator();

        if (!EditorUtility.IsPersistent(prop)) {

            EditorGUILayout.BeginVertical(GUI.skin.box);

            RCCP_DesignSystem.DrawBackButton(prop);

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


}
#endif

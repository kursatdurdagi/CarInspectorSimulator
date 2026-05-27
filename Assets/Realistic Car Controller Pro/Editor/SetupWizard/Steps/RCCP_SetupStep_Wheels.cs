//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

/// <summary>
/// Step 1: Wheel assignment for the RCCP Setup Wizard.
/// Auto-detects wheels and allows manual assignment.
/// </summary>
public class RCCP_SetupStep_Wheels : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        var section = RCCP_WelcomeWindowUI.CreateSection("Wheel Assignment",
            "Assign the 4 wheel meshes for your vehicle. Use auto-detection or drag wheel GameObjects manually.");

        // Auto-detect button.
        if (controller.HasDetectionResult) {

            string severity = controller.DetectionMessageType == MessageType.Error ? "error"
                : controller.DetectionMessageType == MessageType.Warning ? "warning" : "success";
            section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(controller.DetectionMessage, severity));

            section.Add(RCCP_WelcomeWindowUI.CreateButton("Re-detect Wheels", () => {
                controller.AutoDetectAllWheels();
                Rebuild(root, controller);
            }));

        } else {

            section.Add(RCCP_WelcomeWindowUI.CreateButton("Auto Detect Wheels", () => {
                controller.AutoDetectAllWheels();
                Rebuild(root, controller);
            }, "primary"));

        }

        section.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Front wheels.
        section.Add(RCCP_WelcomeWindowUI.CreateSubtitle("Front Wheels"));

        section.Add(RCCP_SetupWizardUI.CreateWheelSlot("FL", data.frontWheels[0], go => data.frontWheels[0] = go));
        section.Add(RCCP_SetupWizardUI.CreateWheelSlot("FR", data.frontWheels[1], go => data.frontWheels[1] = go));

        section.Add(RCCP_WelcomeWindowUI.CreateButton("Swap L \u2194 R", () => {
            controller.SwapFrontWheels();
            Rebuild(root, controller);
        }));

        section.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Rear wheels.
        section.Add(RCCP_WelcomeWindowUI.CreateSubtitle("Rear Wheels"));

        section.Add(RCCP_SetupWizardUI.CreateWheelSlot("RL", data.rearWheels[0], go => data.rearWheels[0] = go));
        section.Add(RCCP_SetupWizardUI.CreateWheelSlot("RR", data.rearWheels[1], go => data.rearWheels[1] = go));

        section.Add(RCCP_WelcomeWindowUI.CreateButton("Swap L \u2194 R", () => {
            controller.SwapRearWheels();
            Rebuild(root, controller);
        }));

        // Wheel type.
        section.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        var wheelTypeField = new EnumField("Wheel Type", data.wheelType);
        wheelTypeField.RegisterValueChangedCallback(evt => data.wheelType = (RCCP_SetupWizardController.WheelType)evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Wheel Type", wheelTypeField));

        // Unassigned candidates.
        if (controller.HasDetectionResult && controller.LastDetection.unassigned != null && controller.LastDetection.unassigned.Length > 0) {

            section.Add(RCCP_WelcomeWindowUI.CreateSeparator());
            section.Add(RCCP_WelcomeWindowUI.CreateSubtitle("Unassigned Candidates"));

            foreach (var go in controller.LastDetection.unassigned) {

                if (go == null) continue;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;

                var label = new Label(go.name);
                label.style.flexGrow = 1;
                label.style.fontSize = 11;
                row.Add(label);

                var pingBtn = RCCP_WelcomeWindowUI.CreateButton("Ping", () => {
                    EditorGUIUtility.PingObject(go);
                    Selection.activeGameObject = go;
                    SceneView.lastActiveSceneView?.FrameSelected();
                });
                row.Add(pingBtn);

                section.Add(row);

            }

        }

        root.Add(section);

        return root;

    }

    private void Rebuild(VisualElement root, RCCP_SetupWizardController controller) {

        root.Clear();
        root.Add(CreateContent(controller));

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) {

        if (!controller.WheelAutoDetectDone) {
            controller.AutoDetectAllWheels();
            controller.WheelAutoDetectDone = true;
        }

    }

}

#endif

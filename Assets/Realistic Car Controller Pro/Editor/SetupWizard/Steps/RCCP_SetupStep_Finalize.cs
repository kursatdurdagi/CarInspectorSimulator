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

/// <summary>
/// Step 6: Finalize step for the RCCP Setup Wizard.
/// Displays a summary of all settings and scene setup options.
/// </summary>
public class RCCP_SetupStep_Finalize : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        // Summary section.
        var summary = RCCP_WelcomeWindowUI.CreateSection("Setup Summary",
            "Review your vehicle configuration. Click any section to go back and edit.");

        // Vehicle.
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Vehicle", data.vehicleName, () => controller.CurrentStep = 0));

        // Mass.
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Mass", $"{data.mass:F0} kg", () => controller.CurrentStep = 0));

        // Wheels.
        string fl = data.frontWheels[0] ? data.frontWheels[0].name : "None";
        string fr = data.frontWheels[1] ? data.frontWheels[1].name : "None";
        string rl = data.rearWheels[0] ? data.rearWheels[0].name : "None";
        string rr = data.rearWheels[1] ? data.rearWheels[1].name : "None";
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Wheels", $"{fl}, {fr}, {rl}, {rr}", () => controller.CurrentStep = 1));

        // Suspension.
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Suspension",
            $"Spring: {data.springForce:F0} | Damper: {data.damperForce:F0}",
            () => controller.CurrentStep = 2));

        // Engine.
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Engine",
            $"{data.driveType} | {data.maxEngineTorque:F0} Nm | {data.maxSpeed:F0} km/h",
            () => controller.CurrentStep = 3));

        // Components count.
        int addonCount = 0;
        if (data.addInputs) addonCount++;
        if (data.addDynamics) addonCount++;
        if (data.addStability) addonCount++;
        if (data.addAudio) addonCount++;
        if (data.addCustomizer) addonCount++;
        if (data.addLights) addonCount++;
        if (data.addDamage) addonCount++;
        if (data.addParticles) addonCount++;
        if (data.addLOD) addonCount++;
        if (data.addOtherAddons) addonCount++;
        summary.Add(RCCP_SetupWizardUI.CreateSummaryRow("Components", $"{addonCount} / 10 enabled", () => controller.CurrentStep = 4));

        root.Add(summary);

        // Scene setup section.
        var sceneSection = RCCP_WelcomeWindowUI.CreateSection("Scene Setup");

        bool hasCamera = Object.FindAnyObjectByType<RCCP_Camera>(FindObjectsInactive.Include) != null;
        bool hasGround = controller.SceneHasGroundCollider();

        if (!hasCamera) {

            sceneSection.Add(RCCP_WelcomeWindowUI.CreateHelpBox("No RCCP Camera in scene.", "warning"));
            sceneSection.Add(RCCP_WelcomeWindowUI.CreateButton("Add RCCP Camera", () => {

                // Re-check at click time so rapid clicks (or a missed UI refresh) can't create duplicates.
                RCCP_Camera existing = Object.FindAnyObjectByType<RCCP_Camera>(FindObjectsInactive.Include);
                if (existing != null) {
                    Selection.activeGameObject = existing.gameObject;
                    EditorGUIUtility.PingObject(existing.gameObject);
                    controller.ReloadCurrentStep();
                    return;
                }

                if (RCCP_Settings.Instance.RCCPMainCamera == null) {
                    EditorUtility.DisplayDialog(
                        "Realistic Car Controller Pro | Missing Camera Prefab",
                        "The RCCP main camera prefab is not assigned in RCCP_Settings.",
                        "OK");
                    return;
                }

                GameObject camInstance = (GameObject)PrefabUtility.InstantiatePrefab(RCCP_Settings.Instance.RCCPMainCamera.gameObject);
                camInstance.name = RCCP_Settings.Instance.RCCPMainCamera.name;
                Undo.RegisterCreatedObjectUndo(camInstance, "Add RCCP Camera");

                Selection.activeGameObject = camInstance;
                EditorGUIUtility.PingObject(camInstance);
                controller.ReloadCurrentStep();

            }, "primary"));

        } else {

            sceneSection.Add(RCCP_WelcomeWindowUI.CreateHelpBox("RCCP Camera found in scene.", "success"));

        }

        if (!hasGround) {

            sceneSection.Add(RCCP_WelcomeWindowUI.CreateHelpBox("No ground collider in scene.", "warning"));
            sceneSection.Add(RCCP_WelcomeWindowUI.CreateButton("Add Ground Plane", () => {

                // Re-check at click time so rapid clicks (or a missed UI refresh) can't create duplicates.
                if (controller.SceneHasGroundCollider()) {
                    controller.ReloadCurrentStep();
                    return;
                }

                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "Ground";
                plane.transform.localScale = new Vector3(20f, 1f, 20f);
                plane.isStatic = true;
                Undo.RegisterCreatedObjectUndo(plane, "Add Ground Plane");

                Selection.activeGameObject = plane;
                EditorGUIUtility.PingObject(plane);
                controller.ReloadCurrentStep();

            }));

        } else {

            sceneSection.Add(RCCP_WelcomeWindowUI.CreateHelpBox("Ground collider found in scene.", "success"));

        }

        // UI Canvas toggle.
        bool hasCanvas = Object.FindAnyObjectByType<RCCP_UIManager>(FindObjectsInactive.Include) != null;

        if (!hasCanvas) {
            sceneSection.Add(RCCP_SetupWizardUI.CreateToggleRow("Add UI Canvas", "Speedometer, buttons, mobile controls", data.addUICanvas, v => data.addUICanvas = v));
        } else {
            sceneSection.Add(RCCP_WelcomeWindowUI.CreateHelpBox("RCCP UI Canvas already in scene.", "success"));
        }

        root.Add(sceneSection);

        return root;

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) { }

}

#endif

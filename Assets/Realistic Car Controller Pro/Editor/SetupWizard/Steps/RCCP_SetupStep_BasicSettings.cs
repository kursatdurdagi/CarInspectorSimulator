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
/// Step 0: Basic settings for the RCCP Setup Wizard.
/// Vehicle name, mass, handling type, and selection validation.
/// </summary>
public class RCCP_SetupStep_BasicSettings : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        // ── Target Vehicle ─────────────────────────────────────────────
        // Always shown at the top so the user can see (and change) which
        // GameObject the wizard is locked onto. Once captured, the wizard
        // holds this reference through every subsequent step, so clicking
        // elsewhere in the Hierarchy mid-setup won't swap the target.
        var targetSection = RCCP_WelcomeWindowUI.CreateSection("Target Vehicle",
            "The GameObject being configured. The wizard remembers it for every step — you can click elsewhere without losing it.");

        var targetField = new ObjectField();
        targetField.objectType = typeof(GameObject);
        targetField.allowSceneObjects = true;
        targetField.value = controller.SelectedVehicle;

        targetField.RegisterValueChangedCallback(evt => {

            GameObject go = evt.newValue as GameObject;

            if (go == null) {
                controller.ReleaseVehicle();
                controller.ReloadCurrentStep();
                return;
            }

            if (!RCCP_SetupWizardController.IsValidVehicleCandidate(go)) {

                // Revert to the previous captured value so the field doesn't
                // display an object the wizard refuses to use.
                targetField.SetValueWithoutNotify(controller.SelectedVehicle);

                EditorUtility.DisplayDialog("Invalid Vehicle",
                    "The selected GameObject cannot be used as a vehicle target.\n\n" +
                    "Requirements:\n" +
                    "- Must be a scene object (not a project asset)\n" +
                    "- Must be active in the hierarchy\n" +
                    "- Must not already have RCCP_CarController",
                    "OK");
                return;

            }

            controller.CaptureVehicle(go);
            controller.ReloadCurrentStep();

        });

        targetSection.Add(RCCP_SetupWizardUI.CreateFieldRow("Vehicle", targetField));
        root.Add(targetSection);

        // No captured target yet — prompt the user to pick one.
        if (!controller.IsSelectionValid()) {

            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
                "Drag your vehicle GameObject into the Target Vehicle field above, or select it in the Hierarchy.\n\n" +
                "Requirements:\n" +
                "- Must be a scene object (not a project asset)\n" +
                "- Must be active in the hierarchy\n" +
                "- Must not already have RCCP_CarController",
                "warning"
            ));

            return root;

        }

        var vehicle = controller.SelectedVehicle;

        // Scene readiness hints.
        bool missingCamera = Object.FindAnyObjectByType<RCCP_Camera>(FindObjectsInactive.Include) == null;
        bool missingGround = !controller.SceneHasGroundCollider();

        if (missingCamera || missingGround) {

            string warning = "Scene readiness check:";
            if (missingCamera) warning += "\n- No RCCP Camera found.";
            if (missingGround) warning += "\n- No ground collider found.";
            warning += "\n\nThese can be added in the final step.";
            root.Add(RCCP_WelcomeWindowUI.CreateHelpBox(warning, "warning"));

        }

        var section = RCCP_WelcomeWindowUI.CreateSection("Vehicle Settings");

        // Vehicle name.
        if (!controller.NameCleanedUp && vehicle != null) {
            data.vehicleName = controller.CleanVehicleName(vehicle.name);
            controller.NameCleanedUp = true;
        }

        var nameField = new TextField();
        nameField.value = data.vehicleName;
        nameField.RegisterValueChangedCallback(evt => data.vehicleName = evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Vehicle Name", nameField));

        // Mass.
        var massField = new FloatField();
        massField.value = data.mass;
        massField.RegisterValueChangedCallback(evt => data.mass = evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Mass (kg)", massField));

        // Handling type.
        var handlingField = new EnumField("Handling", data.handlingType);
        handlingField.RegisterValueChangedCallback(evt => data.handlingType = (RCCP_SetupWizardController.HandlingType)evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Handling Type", handlingField));

        section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Balanced: Good for most vehicles. Stable: Easier to control, less drift. Realistic: More challenging physics.",
            "info"
        ));

        root.Add(section);

        // Vehicle state detection.
        controller.DetectVehicleState();

        bool anyShown = data.isPrefab || data.hasExistingRigidbodies || data.hasExistingWheelColliders;

        if (anyShown) {

            var prepSection = RCCP_WelcomeWindowUI.CreateSection("Model Preparation");

            if (data.isPrefab) {
                bool isModelPrefab = PrefabUtility.IsPartOfModelPrefab(vehicle);
                string label = isModelPrefab
                    ? "Unpack model prefab (recommended for full editing)"
                    : "Unpack prefab (recommended for full editing)";
                prepSection.Add(RCCP_SetupWizardUI.CreateToggleRow(label, "", data.unpackPrefab, v => data.unpackPrefab = v));
            }

            if (data.hasExistingRigidbodies)
                prepSection.Add(RCCP_SetupWizardUI.CreateToggleRow("Remove existing Rigidbodies", "Recommended", data.removeExistingRigidbodies, v => data.removeExistingRigidbodies = v));

            if (data.hasExistingWheelColliders)
                prepSection.Add(RCCP_SetupWizardUI.CreateToggleRow("Remove existing WheelColliders", "Recommended", data.removeExistingWheelColliders, v => data.removeExistingWheelColliders = v));

            prepSection.Add(RCCP_SetupWizardUI.CreateToggleRow("Fix pivot position", "Centers the vehicle origin", data.fixPivot, v => data.fixPivot = v));

            root.Add(prepSection);

        }

        return root;

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) {

        // If the capture was cleared (e.g. user deleted the vehicle and came
        // back), try to auto-capture whatever is now selected. No-op when a
        // target is already locked in.
        controller.TryCaptureFromSelection();

    }

}

#endif

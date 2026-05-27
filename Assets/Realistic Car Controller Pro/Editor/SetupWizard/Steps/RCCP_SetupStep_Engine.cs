//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using UnityEngine.UIElements;
using UnityEditor.UIElements;

/// <summary>
/// Step 3: Engine setup for the RCCP Setup Wizard.
/// Configures drive type, torque, RPM range, and max speed.
/// </summary>
public class RCCP_SetupStep_Engine : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        var section = RCCP_WelcomeWindowUI.CreateSection("Engine & Drivetrain",
            "Configure the engine power output and drivetrain type.");

        // Drive type.
        var driveField = new EnumField("Drive Type", data.driveType);
        driveField.RegisterValueChangedCallback(evt => data.driveType = (RCCP_SetupWizardController.DriveType)evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Drive Type", driveField));

        // Live warning container — refreshed whenever an engine-related value changes.
        var warningContainer = new VisualElement();

        void RefreshWarnings() {
            warningContainer.Clear();
            var issues = controller.GetEngineValidationIssues();
            if (issues.Count == 0)
                return;
            string msg = "Please review the values below:\n- " + string.Join("\n- ", issues);
            warningContainer.Add(RCCP_WelcomeWindowUI.CreateHelpBox(msg, "warning"));
        }

        // Max engine torque.
        var torqueField = new FloatField();
        torqueField.value = data.maxEngineTorque;
        torqueField.RegisterValueChangedCallback(evt => { data.maxEngineTorque = evt.newValue; RefreshWarnings(); });
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Max Torque (Nm)", torqueField));

        // Min engine RPM.
        var minRpmField = new FloatField();
        minRpmField.value = data.minEngineRPM;
        minRpmField.RegisterValueChangedCallback(evt => { data.minEngineRPM = evt.newValue; RefreshWarnings(); });
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Min RPM", minRpmField));

        // Max engine RPM.
        var maxRpmField = new FloatField();
        maxRpmField.value = data.maxEngineRPM;
        maxRpmField.RegisterValueChangedCallback(evt => { data.maxEngineRPM = evt.newValue; RefreshWarnings(); });
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Max RPM", maxRpmField));

        // Max speed.
        var speedField = new FloatField();
        speedField.value = data.maxSpeed;
        speedField.RegisterValueChangedCallback(evt => { data.maxSpeed = evt.newValue; RefreshWarnings(); });
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Max Speed (km/h)", speedField));

        // Recalculate button.
        section.Add(RCCP_WelcomeWindowUI.CreateButton("Recalculate Torque from Mass", () => {
            data.maxEngineTorque = data.mass * 0.2f;
            torqueField.value = data.maxEngineTorque;
            RefreshWarnings();
        }));

        section.Add(warningContainer);

        root.Add(section);

        RefreshWarnings();

        return root;

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) {

        if (!controller.EngineAutoCalcDone) {
            controller.Data.maxEngineTorque = controller.Data.mass * 0.2f;
            controller.EngineAutoCalcDone = true;
        }

    }

}

#endif

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
/// Step 2: Suspension setup for the RCCP Setup Wizard.
/// Configures suspension distance, spring force, and damper force.
/// </summary>
public class RCCP_SetupStep_Suspension : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        var section = RCCP_WelcomeWindowUI.CreateSection("Suspension Settings",
            "Configure the suspension spring and damper values. These affect how the vehicle handles bumps and body roll.");

        // Suspension distance.
        section.Add(RCCP_SetupWizardUI.CreateSliderField(
            "Suspension Distance", 0.05f, 0.5f, data.suspensionDistance,
            v => data.suspensionDistance = v));

        // Spring force.
        var springField = new FloatField();
        springField.value = data.springForce;
        springField.RegisterValueChangedCallback(evt => data.springForce = evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Spring Force", springField));

        // Damper force.
        var damperField = new FloatField();
        damperField.value = data.damperForce;
        damperField.RegisterValueChangedCallback(evt => data.damperForce = evt.newValue);
        section.Add(RCCP_SetupWizardUI.CreateFieldRow("Damper Force", damperField));

        // Recalculate button.
        section.Add(RCCP_WelcomeWindowUI.CreateButton("Recalculate from Mass", () => {
            data.springForce = data.mass * 30f;
            data.damperForce = data.springForce * 0.1f;
            springField.value = data.springForce;
            damperField.value = data.damperForce;
        }));

        section.Add(RCCP_WelcomeWindowUI.CreateHelpBox(
            "Spring and damper are auto-calculated from vehicle mass. Adjust manually if needed.",
            "info"));

        root.Add(section);

        return root;

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) {

        if (!controller.SuspensionAutoCalcDone) {
            controller.Data.springForce = controller.Data.mass * 30f;
            controller.Data.damperForce = controller.Data.springForce * 0.1f;
            controller.SuspensionAutoCalcDone = true;
        }

    }

}

#endif

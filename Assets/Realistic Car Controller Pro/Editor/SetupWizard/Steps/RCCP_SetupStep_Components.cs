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

/// <summary>
/// Step 4: Addon components selection for the RCCP Setup Wizard.
/// Toggles 10 optional vehicle systems.
/// </summary>
public class RCCP_SetupStep_Components : IRCCP_SetupWizardStep {

    public VisualElement CreateContent(RCCP_SetupWizardController controller) {

        var root = new VisualElement();
        var data = controller.Data;

        var section = RCCP_WelcomeWindowUI.CreateSection("Addon Components",
            "Select which optional systems to add to your vehicle. All are recommended for a complete setup.");

        // Select All / None buttons.
        section.Add(RCCP_WelcomeWindowUI.CreateButtonRow(
            ("Select All", () => { controller.ToggleAllAddons(true); Rebuild(root, controller); }, "success"),
            ("Select None", () => { controller.ToggleAllAddons(false); Rebuild(root, controller); }, "danger")
        ));

        section.Add(RCCP_WelcomeWindowUI.CreateSeparator());

        // Addon toggles.
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Inputs", "Keyboard, controller, and mobile input handling", data.addInputs, v => data.addInputs = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Dynamics", "Aerodynamics and body tilt simulation", data.addDynamics, v => data.addDynamics = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Stability", "ABS, ESP, TCS and stability assists", data.addStability, v => data.addStability = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Audio", "Engine sound, skid, crash audio", data.addAudio, v => data.addAudio = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Customizer", "Paint, wheels, upgrades system", data.addCustomizer, v => data.addCustomizer = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Lights", "Headlights, brake lights, indicators", data.addLights, v => data.addLights = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Damage", "Deformable body and detachable parts", data.addDamage, v => data.addDamage = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Particles", "Exhaust, dust, and tire smoke effects", data.addParticles, v => data.addParticles = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("LOD", "Level of detail for performance", data.addLOD, v => data.addLOD = v));
        section.Add(RCCP_SetupWizardUI.CreateToggleRow("Other Addons", "Recorder, fuel tank, trail effects", data.addOtherAddons, v => data.addOtherAddons = v));

        root.Add(section);

        return root;

    }

    private void Rebuild(VisualElement root, RCCP_SetupWizardController controller) {

        root.Clear();
        root.Add(CreateContent(controller));

    }

    public void OnStepEntered(RCCP_SetupWizardController controller) { }

}

#endif

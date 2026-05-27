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
/// Interface for Setup Wizard steps.
/// Each step creates its own UI content and receives a callback when entered.
/// </summary>
public interface IRCCP_SetupWizardStep {

    /// <summary>
    /// Creates the UI content for this step.
    /// </summary>
    VisualElement CreateContent(RCCP_SetupWizardController controller);

    /// <summary>
    /// Called when the wizard navigates to this step.
    /// </summary>
    void OnStepEntered(RCCP_SetupWizardController controller);

}

#endif

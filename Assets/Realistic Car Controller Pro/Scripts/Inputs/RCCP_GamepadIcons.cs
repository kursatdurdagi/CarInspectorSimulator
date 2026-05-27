//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

////TODO: have updateBindingUIEvent receive a control path string, too (in addition to the device layout name)

/// <summary>
/// This is an example for how to override the default display behavior of bindings. The component
/// hooks into <see cref="RebindActionUI.updateBindingUIEvent"/> which is triggered when UI display
/// of a binding should be refreshed. It then checks whether we have an icon for the current binding
/// and if so, replaces the default text display with an icon.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Input/RCCP Gamepad Icons")]
public class RCCP_GamepadIcons : RCCP_GenericComponent {
    /// <summary>
    /// Icon sprites mapped to Xbox gamepad controls.
    /// </summary>
    [Tooltip("Icon sprite set used for Xbox-compatible gamepad bindings.")]
    public GamepadIcons xbox;
    /// <summary>
    /// Icon sprites mapped to PlayStation (DualSense) gamepad controls.
    /// </summary>
    [Tooltip("Icon sprite set used for PlayStation DualSense gamepad bindings.")]
    public GamepadIcons ps4;

    protected void OnEnable() {
        // Hook into all updateBindingUIEvents on all RebindActionUI components in our hierarchy.
        var rebindUIComponents = transform.GetComponentsInChildren<RCCP_UI_RebindInput>();
        foreach (var component in rebindUIComponents) {
            component.updateBindingUIEvent.AddListener(OnUpdateBindingDisplay);
            component.UpdateBindingDisplay();
        }
    }

    /// <summary>
    /// Callback invoked when a binding display is refreshed. Replaces text with a gamepad icon sprite if one is available for the bound control.
    /// </summary>
    /// <param name="component">The rebind input UI component being updated.</param>
    /// <param name="bindingDisplayString">The default text representation of the binding.</param>
    /// <param name="deviceLayoutName">The device layout name (e.g., "XInputControllerWindows", "DualSenseGamepadHID").</param>
    /// <param name="controlPath">The input control path on the device (e.g., "buttonSouth", "leftTrigger").</param>
    protected void OnUpdateBindingDisplay(RCCP_UI_RebindInput component, string bindingDisplayString, string deviceLayoutName, string controlPath) {
        if (string.IsNullOrEmpty(deviceLayoutName) || string.IsNullOrEmpty(controlPath))
            return;

        var icon = default(Sprite);
        if (InputSystem.IsFirstLayoutBasedOnSecond(deviceLayoutName, "DualSenseGamepadHID"))
            icon = ps4.GetSprite(controlPath);
        else if (InputSystem.IsFirstLayoutBasedOnSecond(deviceLayoutName, "Gamepad"))
            icon = xbox.GetSprite(controlPath);

        var textComponent = component.bindingText;

        // Grab Image component.
        var imageGO = textComponent.transform.parent.Find("ActionBindingIcon");
        imageGO.TryGetComponent<Image>(out var imageComponent);

        if (icon != null) {
            textComponent.gameObject.SetActive(false);
            imageComponent.sprite = icon;
            imageComponent.gameObject.SetActive(true);
        } else {
            textComponent.gameObject.SetActive(true);
            imageComponent.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Maps gamepad control paths to their corresponding icon sprites for a specific controller type.
    /// </summary>
    [Serializable]
    public struct GamepadIcons {
        /// <summary>
        /// Icon sprite for the south face button (A on Xbox, Cross on PlayStation).
        /// </summary>
        [Header("Face Buttons")]
        [Tooltip("South face button icon (A on Xbox, Cross on PlayStation).")]
        public Sprite buttonSouth;
        /// <summary>
        /// Icon sprite for the north face button (Y on Xbox, Triangle on PlayStation).
        /// </summary>
        [Tooltip("North face button icon (Y on Xbox, Triangle on PlayStation).")]
        public Sprite buttonNorth;
        /// <summary>
        /// Icon sprite for the east face button (B on Xbox, Circle on PlayStation).
        /// </summary>
        [Tooltip("East face button icon (B on Xbox, Circle on PlayStation).")]
        public Sprite buttonEast;
        /// <summary>
        /// Icon sprite for the west face button (X on Xbox, Square on PlayStation).
        /// </summary>
        [Tooltip("West face button icon (X on Xbox, Square on PlayStation).")]
        public Sprite buttonWest;
        /// <summary>
        /// Icon sprite for the start/menu button.
        /// </summary>
        [Tooltip("Start or Menu button icon.")]
        public Sprite startButton;
        /// <summary>
        /// Icon sprite for the select/view button.
        /// </summary>
        [Tooltip("Select or View button icon.")]
        public Sprite selectButton;
        /// <summary>
        /// Icon sprite for the left trigger (LT/L2).
        /// </summary>
        [Header("Triggers / Shoulders")]
        [Tooltip("Left trigger icon (LT on Xbox, L2 on PlayStation).")]
        public Sprite leftTrigger;
        /// <summary>
        /// Icon sprite for the right trigger (RT/R2).
        /// </summary>
        [Tooltip("Right trigger icon (RT on Xbox, R2 on PlayStation).")]
        public Sprite rightTrigger;
        /// <summary>
        /// Icon sprite for the left shoulder bumper (LB/L1).
        /// </summary>
        [Tooltip("Left shoulder bumper icon (LB on Xbox, L1 on PlayStation).")]
        public Sprite leftShoulder;
        /// <summary>
        /// Icon sprite for the right shoulder bumper (RB/R1).
        /// </summary>
        [Tooltip("Right shoulder bumper icon (RB on Xbox, R1 on PlayStation).")]
        public Sprite rightShoulder;
        /// <summary>
        /// Icon sprite for the directional pad.
        /// </summary>
        [Header("D-Pad")]
        [Tooltip("Directional pad base icon.")]
        public Sprite dpad;
        /// <summary>
        /// Icon sprite for the D-pad up direction.
        /// </summary>
        [Tooltip("D-pad up direction icon.")]
        public Sprite dpadUp;
        /// <summary>
        /// Icon sprite for the D-pad down direction.
        /// </summary>
        [Tooltip("D-pad down direction icon.")]
        public Sprite dpadDown;
        /// <summary>
        /// Icon sprite for the D-pad left direction.
        /// </summary>
        [Tooltip("D-pad left direction icon.")]
        public Sprite dpadLeft;
        /// <summary>
        /// Icon sprite for the D-pad right direction.
        /// </summary>
        [Tooltip("D-pad right direction icon.")]
        public Sprite dpadRight;
        /// <summary>
        /// Icon sprite for the left analog stick.
        /// </summary>
        [Header("Sticks")]
        [Tooltip("Left analog stick icon.")]
        public Sprite leftStick;
        /// <summary>
        /// Icon sprite for the right analog stick.
        /// </summary>
        [Tooltip("Right analog stick icon.")]
        public Sprite rightStick;
        /// <summary>
        /// Icon sprite for pressing the left analog stick (L3).
        /// </summary>
        [Tooltip("Left stick press icon (L3).")]
        public Sprite leftStickPress;
        /// <summary>
        /// Icon sprite for pressing the right analog stick (R3).
        /// </summary>
        [Tooltip("Right stick press icon (R3).")]
        public Sprite rightStickPress;

        /// <summary>
        /// Returns the icon sprite corresponding to the given input control path, or null if no mapping exists.
        /// </summary>
        /// <param name="controlPath">The input system control path (e.g., "buttonSouth", "dpad/up").</param>
        /// <returns>The matching sprite, or null if no icon is mapped for the control path.</returns>
        public Sprite GetSprite(string controlPath) {
            // From the input system, we get the path of the control on device. So we can just
            // map from that to the sprites we have for gamepads.
            switch (controlPath) {
                case "buttonSouth": return buttonSouth;
                case "buttonNorth": return buttonNorth;
                case "buttonEast": return buttonEast;
                case "buttonWest": return buttonWest;
                case "start": return startButton;
                case "select": return selectButton;
                case "leftTrigger": return leftTrigger;
                case "rightTrigger": return rightTrigger;
                case "leftTriggerButton": return leftTrigger;
                case "rightTriggerButton": return rightTrigger;
                case "leftShoulder": return leftShoulder;
                case "rightShoulder": return rightShoulder;
                case "dpad": return dpad;
                case "dpad/up": return dpadUp;
                case "dpad/down": return dpadDown;
                case "dpad/left": return dpadLeft;
                case "dpad/right": return dpadRight;
                case "leftStick": return leftStick;
                case "rightStick": return rightStick;
                case "leftStickPress": return leftStickPress;
                case "rightStickPress": return rightStickPress;
            }
            return null;
        }
    }
}


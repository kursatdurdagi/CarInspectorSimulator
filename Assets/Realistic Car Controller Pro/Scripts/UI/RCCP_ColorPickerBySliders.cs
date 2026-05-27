//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Color Picker with UI Sliders.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP Color Picker By Sliders")]
public class RCCP_ColorPickerBySliders : RCCP_UIComponent {

    /// <summary>
    /// Color types.
    /// </summary>
    [Tooltip("Which vehicle color property these sliders control.")]
    public ColorType colorType = ColorType.WheelSmoke;
    public enum ColorType {

        WheelSmoke,
        Headlights

    }

    /// <summary>
    /// Main color.
    /// </summary>
    [Tooltip("Currently selected color composed from the RGB sliders.")]
    public Color color;
    private Color oldColor;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    [Tooltip("Slider controlling the red channel (0-1).")]
    public Slider redSlider;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    [Tooltip("Slider controlling the green channel (0-1).")]
    public Slider greenSlider;

    /// <summary>
    /// Sliders per color channel.
    /// </summary>
    [Tooltip("Slider controlling the blue channel (0-1).")]
    public Slider blueSlider;

    private void OnEnable() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        switch (colorType) {

            case ColorType.Headlights:

                //  If player vehicle doesn't have the decal manager component, return.
                if (!playerVehicle.Customizer.CustomizationManager)
                    return;

                color = playerVehicle.Customizer.CustomizationManager.customizationData.headlightColor;
                break;

            case ColorType.WheelSmoke:

                //  If player vehicle doesn't have the decal manager component, return.
                if (!playerVehicle.Customizer.CustomizationManager)
                    return;

                color = playerVehicle.Customizer.CustomizationManager.customizationData.wheelSmokeColor;
                break;

        }

        oldColor = color;

        redSlider.SetValueWithoutNotify(color.r);
        greenSlider.SetValueWithoutNotify(color.g);
        blueSlider.SetValueWithoutNotify(color.b);

    }

    private void Update() {

        // Assigning new color to main color.
        color = new Color(redSlider.value, greenSlider.value, blueSlider.value);

        if (oldColor != color) {

            if (!enabled)
                return;

            //  Finding the player vehicle.
            RCCP_CarController playerVehicle = RCCPSceneManager.activePlayerVehicle;

            //  If no player vehicle found, return.
            if (!playerVehicle)
                return;

            //  If player vehicle doesn't have the customizer component, return.
            if (!playerVehicle.Customizer)
                return;

            switch (colorType) {

                case ColorType.Headlights:

                    //  If player vehicle doesn't have the decal manager component, return.
                    if (!playerVehicle.Customizer.CustomizationManager)
                        return;

                    //  Set the decal.
                    playerVehicle.Customizer.CustomizationManager.SetHeadlightsColor(color);

                    break;

                case ColorType.WheelSmoke:

                    //  If player vehicle doesn't have the decal manager component, return.
                    if (!playerVehicle.Customizer.CustomizationManager)
                        return;

                    //  Set the decal.
                    playerVehicle.Customizer.CustomizationManager.SetSmokeColor(color);

                    break;

            }

        }

        oldColor = color;

    }

}

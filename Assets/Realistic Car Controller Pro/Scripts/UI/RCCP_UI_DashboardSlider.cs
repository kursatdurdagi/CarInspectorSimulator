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
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI dashboard sliders for mobile / desktop.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Dashboard Slider")]
public class RCCP_UI_DashboardSlider : RCCP_UIComponent {

    /// <summary>
    /// Button types.
    /// </summary>
    public enum ButtonType { AutomaticGear_D, AutomaticGear_N, AutomaticGear_R, AutomaticGear_P };
    [Tooltip("Initial automatic gear position when the slider is enabled.")]
    public ButtonType buttonType = ButtonType.AutomaticGear_D;

    /// <summary>
    /// Slider.
    /// </summary>
    [Tooltip("UI Slider used to select automatic gear position (D/N/R/P).")]
    public Slider slider;

    private int sliderValue = 0;
    private int sliderValueOld = 0;

    private void Awake() {

        if (!slider)
            TryGetComponent(out slider);

        sliderValue = (int)slider.value;
        sliderValueOld = sliderValue;

    }

    private void OnEnable() {

        //if (!slider)
        //    slider = GetComponent<Slider>();

        //switch (buttonType) {

        //    case ButtonType.AutomaticGear_D:

        //        slider.value = 0;
        //        RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.D);
        //        break;

        //    case ButtonType.AutomaticGear_N:

        //        slider.value = 1;
        //        RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.N);
        //        break;

        //    case ButtonType.AutomaticGear_R:

        //        slider.value = 2;
        //        RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.R);
        //        break;

        //    case ButtonType.AutomaticGear_P:

        //        slider.value = 3;
        //        RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.P);
        //        break;

        //}

    }

    private void Update() {

        sliderValue = (int)slider.value;

        if (sliderValue != sliderValueOld)
            OnValueChanged();

        sliderValueOld = sliderValue;

    }

    /// <summary>Handles slider value changes by sending the corresponding automatic gear command (D/N/R/P).</summary>
    public void OnValueChanged() {

        if (!slider)
            TryGetComponent(out slider);

        switch (sliderValue) {

            case 0:

                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.D);
                break;

            case 1:

                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.N);
                break;

            case 2:

                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.R);
                break;

            case 3:

                RCCP_InputManager.Instance.AutomaticGear(RCCP_Gearbox.SemiAutomaticDNRPGear.P);
                break;

        }

    }

}

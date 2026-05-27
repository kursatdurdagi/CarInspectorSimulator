//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Sets target audio.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Set Audio")]
public class RCCP_UI_SetAudio : RCCP_UIComponent {

    private TMP_Dropdown dropdown;


    private void OnEnable() {

        if (!dropdown)
            TryGetComponent(out dropdown);

        bool audioPaused = AudioListener.pause;

        dropdown.SetValueWithoutNotify(audioPaused ? 1 : 0);

    }

    /// <summary>Sets the audio quality level based on the dropdown selection.</summary>
    /// <param name="dropdown">The dropdown UI element with the selected audio quality option.</param>
    public void SetAudio(TMP_Dropdown dropdown) {

        AudioListener.pause = dropdown.value == 1 ? true : false;

    }

}

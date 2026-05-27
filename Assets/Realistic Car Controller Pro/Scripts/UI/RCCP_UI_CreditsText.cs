//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>UI component that displays RCCP version and credits text.</summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Credits Text")]
public class RCCP_UI_CreditsText : RCCP_UIComponent {

    private TextMeshProUGUI text;

    private void OnEnable() {

        if (!text)
            TryGetComponent(out text);

        if (!text)
            return;

        string creditsText = "Realistic Car Controller Pro " + RCCP_Version.version + "\nBoneCracker Games\nEkrem Bugra Ozdoganlar";

        text.text = creditsText;

    }

}

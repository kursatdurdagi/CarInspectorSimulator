//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>UI button that resets all input rebindings to their default values.</summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Input/RCCP UI Rebind Input Reset")]
public class RCCP_UI_RebindInputReset : RCCP_UIComponent {

    /// <summary>Array of rebind input UI components to reset when the button is clicked.</summary>
    [Tooltip("All rebind input UI elements that will be restored to default bindings on click.")]
    public RCCP_UI_RebindInput[] rebindInputs;

    /// <summary>Resets all referenced rebind input components to their default bindings.</summary>
    public void OnClick() {

        for (int i = 0; i < rebindInputs.Length; i++) {

            if (rebindInputs[i] != null)
                rebindInputs[i].ResetToDefault();

        }

    }

}

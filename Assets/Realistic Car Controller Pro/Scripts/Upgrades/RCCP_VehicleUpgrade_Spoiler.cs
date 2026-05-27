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

/// <summary>
/// Upgradable spoiler.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Spoiler")]
public class RCCP_VehicleUpgrade_Spoiler : RCCP_Component {

    /// <summary>
    /// Renderer of the spoiler.
    /// </summary>
    [Tooltip("Mesh renderer of the spoiler model used for painting.")]
    public MeshRenderer bodyRenderer;

    /// <summary>
    /// Material index of the renderer.
    /// </summary>
    [Tooltip("Material index on the renderer to paint (-1 to skip painting).")]
    [Min(-1)] public int index = -1;

    /// <summary>
    /// Target keyword for painting. Use "_BaseColor" for URP shaders.
    /// </summary>
    [Tooltip("Shader color property name to modify (e.g. _BaseColor for URP).")]
    public string id = "_BaseColor";

    /// <summary>
    /// Applies a paint color to the spoiler body renderer.
    /// </summary>
    /// <param name="newColor">The new paint color to apply to the spoiler.</param>
    public void UpdatePaint(Color newColor) {

        if (index == -1)
            return;

        if (bodyRenderer)
            bodyRenderer.materials[index].SetColor(id, newColor);
        else
            Debug.LogError("Body renderer of this spoiler is not selected!");

    }

    private void Reset() {

        bodyRenderer = GetComponentInChildren<MeshRenderer>(true);

    }

}

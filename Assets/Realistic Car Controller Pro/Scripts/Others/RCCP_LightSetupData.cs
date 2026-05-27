//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Data container that holds the user�s light selections.</summary>
[System.Serializable]
public class RCCP_LightSetupData {

    /// <summary>Default light intensity for headlights (low beam and high beam).</summary>
    [Tooltip("Default light intensity for headlights (low and high beam).")]
    [Min(0f)] public float defaultIntensityForHeadlights = 2.5f;
    /// <summary>Default light intensity for brake lights.</summary>
    [Tooltip("Default light intensity for brake lights.")]
    [Min(0f)] public float defaultIntensityForBrakeLights = 1f;
    /// <summary>Default light intensity for reverse lights.</summary>
    [Tooltip("Default light intensity for reverse lights.")]
    [Min(0f)] public float defaultIntensityForReverseLights = 1f;
    /// <summary>Default light intensity for turn indicator lights.</summary>
    [Tooltip("Default light intensity for turn indicator lights.")]
    [Min(0f)] public float defaultIntensityForIndicatorLights = 1f;

    /// <summary>Default color tint for headlights.</summary>
    [Tooltip("Default color tint applied to headlights.")]
    public Color headlightColor = new Color(1f, 1f, .9f, 1f);
    /// <summary>Default color tint for brake lights.</summary>
    [Tooltip("Default color tint applied to brake lights.")]
    public Color brakelightColor = new Color(1f, .1f, .05f, 1f);
    /// <summary>Default color tint for tail lights.</summary>
    [Tooltip("Default color tint applied to tail lights.")]
    public Color taillightColor = new Color(1f, .05f, .05f, 1f);
    /// <summary>Default color tint for reverse lights.</summary>
    [Tooltip("Default color tint applied to reverse lights.")]
    public Color reverselightColor = new Color(.9f, 1f, 1f, 1f);
    /// <summary>Default color tint for turn indicator lights.</summary>
    [Tooltip("Default color tint applied to turn indicator lights.")]
    public Color indicatorColor = new Color(1f, .5f, 0f, 1f);

    /// <summary>Whether lens flare effects are enabled on vehicle lights.</summary>
    [Tooltip("Enables lens flare effects on vehicle lights.")]
    public bool useLensFlares = true;

    /// <summary>Lens flare data asset for Scriptable Render Pipeline (URP/HDRP).</summary>
    [Tooltip("Lens flare data asset for SRP (URP/HDRP) rendering.")]
    public Object lensFlareSRP;
    /// <summary>Legacy lens flare asset for the built-in render pipeline.</summary>
    [Tooltip("Legacy lens flare asset for the built-in render pipeline.")]
    public Flare flare;

}
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
using UnityEngine.Rendering;
using System.Threading.Tasks;

/// <summary>
/// Represents a vehicle light such as headlight, brake light, indicator, or reverse light. 
/// Supports emissive materials, lens flares, and damage/break logic.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Addons/RCCP Light")]
public class RCCP_Light : RCCP_Component {

    private Light _lightSource;

    /// <summary>
    /// Backing reference for the Unity Light component on this GameObject.
    /// </summary>
    private Light LightSource {

        get {

            if (_lightSource == null)
                TryGetComponent(out _lightSource);

            return _lightSource;

        }

    }

    /// <summary>
    /// Defines the type of this light (e.g., low-beam headlight, brake, turn signal, etc.).
    /// </summary>
    public enum LightType { Headlight_LowBeam, Headlight_HighBeam, Brakelight, Taillight, Reverselight, IndicatorLeftLight, IndicatorRightLight }
    [Tooltip("Function of this light on the vehicle (headlight, brake, indicator, etc.)")]
    public LightType lightType = LightType.Headlight_LowBeam;

    /// <summary>
    /// Unity LightRenderMode to force pixel or vertex lighting. 
    /// Overridden by Realistic Car Controller settings if not manually overridden.
    /// </summary>
    [Tooltip("Force pixel or vertex rendering for this light; Auto defers to RCCP global settings")]
    public LightRenderMode lightRendererMode = LightRenderMode.Auto;
    [Tooltip("When true, keeps the manually set render mode instead of using RCCP global settings")]
    public bool overrideRenderMode = false;

    /// <summary>
    /// Target intensity for this light. Zero means off.
    /// </summary>
    [Space()]
    [Tooltip("Target brightness when this light is active; smoothly transitions toward this value")]
    [Range(.1f, 10f)] public float intensity = 1f;

    /// <summary>
    /// A smoothing factor for transitions in the light�s intensity.
    /// </summary>
    [Tooltip("How quickly the light fades in and out; higher values give snappier transitions")]
    [Range(.1f, 1f)] public float smoothness = .5f;

    /// <summary>
    /// A single emissive renderer used by this light for glow effects, typically a lens or reflective surface.
    /// </summary>
    [Tooltip("Mesh renderer whose material receives emission color changes to simulate the light glow")]
    [Space()] public MeshRenderer emissiveRenderer;

    /// <summary>
    /// Index of the material in the emissiveRenderer�s array to apply emission changes to.
    /// </summary>
    [Tooltip("Index of the material in the renderer's array that should glow with emission")]
    [Min(0)] public int emissiveMaterialIndex = 0;

    /// <summary>
    /// A cached reference to the actual Material used for emission, fetched from emissiveRenderer.
    /// </summary>
    private Material emissiveMaterial;

    /// <summary>
    /// Color to set for the emissive property on the chosen material.
    /// </summary>
    [Tooltip("HDR color applied to the emissive material when the light is on")]
    [Space()] public Color emissiveColor = Color.white;

    /// <summary>
    /// Shader keyword used to enable emission on the material.
    /// </summary>
    [Tooltip("Shader keyword toggled to enable emission on the material (e.g. _EMISSION)")]
    public string shaderKeywordEmissionEnable = "_EMISSION";

    /// <summary>
    /// Shader keyword used to set the emission color for the material.
    /// </summary>
    [Tooltip("Shader property name for setting the emission color (e.g. _EmissionColor or _EmissiveColor)")]
    public string shaderKeywordEmissionColor = "_EmissionColor";

    /// <summary>
    /// Defines an optional additional set of emissive renderers that this light also controls.
    /// </summary>
    [System.Serializable]
    public class RCCP_EmissiveRenderer {

        [Tooltip("Mesh renderer whose material receives emission color changes")]
        public MeshRenderer emissiveRenderer;
        [Tooltip("Index of the material in the renderer's array to apply emission to")]
        [Min(0)] public int emissiveMaterialIndex = 0;
        [Tooltip("Cached runtime material instance used for emission; auto-assigned at runtime")]
        [Space()] public Material emissiveMaterial;
        [Tooltip("HDR color applied to the emissive material when the light is on")]
        public Color emissiveColor = Color.white;
        [Tooltip("Shader keyword toggled to enable emission (e.g. _EMISSION)")]
        public string shaderKeywordEmissionEnable = "_EMISSION";
        [Tooltip("Shader property name for the emission color (e.g. _EmissionColor)")]
        public string shaderKeywordEmissionColor = "_EmissionColor";

    }

    /// <summary>
    /// Optional array of extra emissive renderers. Each entry can have a unique material index and color settings.
    /// </summary>
    [Tooltip("Extra emissive renderers controlled by this light, each with independent material settings")]
    public RCCP_EmissiveRenderer[] additionalEmissiveRenderers = new RCCP_EmissiveRenderer[0];

    /// <summary>
    /// Enables or disables lens flares. For URP/HDRP, uses LensFlareComponentSRP if present.
    /// </summary>
    [Tooltip("Enable distance- and angle-based lens flare effects for this light")]
    [Space()] public bool useLensFlares = true;

#if !BCG_URP && !BCG_HDRP
    // Legacy pipeline lens flare reference
    private LensFlare lensFlare;
#else
    // SRP lens flare reference
    private LensFlareComponentSRP lensFlare_SRP;
#endif

    /// <summary>
    /// The maximum base brightness assigned to the lens flare. 
    /// Actual brightness can be lower based on angle/distance to camera.
    /// </summary>
    [Tooltip("Base brightness of the lens flare before camera distance and angle attenuation")]
    [Range(0f, 10f)] public float flareBrightness = 1.5f;

    /// <summary>
    /// Computed final brightness for the lens flare after factoring in camera distance and angle.
    /// </summary>
    private float finalFlareBrightness = 0f;

    /// <summary>
    /// Determines if this light can be broken from collisions.
    /// </summary>
    [Tooltip("Whether collision impacts can break this light, permanently turning it off until repaired")]
    [Space()] public bool isBreakable = true;

    /// <summary>
    /// Current durability of the light. Decreases upon impact; if it falls below breakPoint, the light is broken.
    /// </summary>
    [Tooltip("Current durability of the light; decreases on impact and breaks when it falls below the break point")]
    [Min(0f)] public float strength = 100f;

    /// <summary>
    /// The threshold below which the light is considered broken.
    /// </summary>
    [Tooltip("Strength threshold below which the light is considered broken and stops emitting")]
    [Min(0)] public int breakPoint = 35;

    /// <summary>
    /// Remembers the original strength for repairs.
    /// </summary>
    private float orgStrength = 100f;

    /// <summary>
    /// True if the light is currently broken, causing it to remain off.
    /// </summary>
    [Tooltip("Whether this light has been destroyed by collision damage and remains off until repaired")]
    public bool broken = false;

    /// <summary>
    /// Reference to the main camera for calculating lens flare brightness.
    /// </summary>
    private Camera mainCam;

    public override void Start() {

        base.Start();

        if (useLensFlares) {

#if !BCG_URP && !BCG_HDRP
            TryGetComponent(out lensFlare);
#else
            TryGetComponent(out lensFlare_SRP);
#endif

        }

        orgStrength = strength;

        // Optionally override the render mode based on RCCPSettings unless user specifically set overrideRenderMode = true.
        if (!overrideRenderMode) {

            switch (lightType) {

                case LightType.Headlight_LowBeam:

                    if (RCCPSettings.useHeadLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.Brakelight:
                case LightType.Taillight:

                    if (RCCPSettings.useBrakeLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.Reverselight:

                    if (RCCPSettings.useReverseLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

                case LightType.IndicatorLeftLight:
                case LightType.IndicatorRightLight:

                    if (RCCPSettings.useIndicatorLightsAsVertexLights)
                        lightRendererMode = LightRenderMode.ForceVertex;
                    else
                        lightRendererMode = LightRenderMode.ForcePixel;

                    break;

            }

        }

        InvokeRepeating(nameof(FindMainCamera), 0f, 1f);

    }

    /// <summary>Finds and caches the main camera reference for distance-based light culling.</summary>
    public void FindMainCamera() {

        mainCam = Camera.main;

    }

    private void Update() {

        if (!CarController) {

            Lighting(0f);

#if !BCG_URP && !BCG_HDRP
            // Built-in pipeline lens flare
            if (lensFlare)
                LensFlare();
#else
            // URP/HDRP pipeline lens flare
            if (lensFlare_SRP)
                LensFlare_SRP();
#endif

            return;

        }

        // If no Lights component on the CarController, skip.
        if (!CarController.Lights) {

            Lighting(0f);

#if !BCG_URP && !BCG_HDRP
            // Built-in pipeline lens flare
            if (lensFlare)
                LensFlare();
#else
            // URP/HDRP pipeline lens flare
            if (lensFlare_SRP)
                LensFlare_SRP();
#endif

            return;

        }

        // Dynamically adjust intensity based on the light type and the vehicle�s current lighting state.
        switch (lightType) {

            case LightType.Headlight_LowBeam:

                if (CarController.Lights.lowBeamHeadlights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.Headlight_HighBeam:

                if (CarController.Lights.highBeamHeadlights) {

                    if (!CarController.Lights.highBeamWithLowBeamOnly)
                        Lighting(intensity);
                    else if (CarController.Lights.highBeamWithLowBeamOnly && CarController.Lights.lowBeamHeadlights)
                        Lighting(intensity);
                    else
                        Lighting(0f);

                } else {

                    Lighting(0f);

                }

                break;

            case LightType.Brakelight:

                float tailIntensity = 0f;

                // If the vehicle does not have a separate tailLight for running lights, 
                // we can blend a partial intensity when low beams are on.
                if (!CarController.Lights.tailLightFound) {

                    if (CarController.Lights.lowBeamHeadlights)
                        tailIntensity = .3f;
                    else
                        tailIntensity = 0f;

                }

                if (CarController.Lights.brakeLights)
                    Lighting(Mathf.Clamp(intensity + tailIntensity, 0f, intensity));
                else
                    Lighting(tailIntensity);

                break;

            case LightType.Taillight:

                if (CarController.Lights.lowBeamHeadlights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.Reverselight:

                if (CarController.Lights.reverseLights)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.IndicatorLeftLight:

                if ((CarController.Lights.indicatorsLeft || CarController.Lights.indicatorsAll) && CarController.Lights.indicatorTimer < .5f)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

            case LightType.IndicatorRightLight:

                if ((CarController.Lights.indicatorsRight || CarController.Lights.indicatorsAll) && CarController.Lights.indicatorTimer < .5f)
                    Lighting(intensity);
                else
                    Lighting(0f);

                break;

        }

        // Update the emissiveMaterial�s emission color if assigned.
        if (emissiveRenderer) {

            if (emissiveMaterial == null)
                emissiveMaterial = emissiveRenderer.materials[emissiveMaterialIndex];

            if (emissiveMaterial && shaderKeywordEmissionEnable != "")
                emissiveMaterial.EnableKeyword(shaderKeywordEmissionEnable);

            if (emissiveMaterial && shaderKeywordEmissionColor != "")
                emissiveMaterial.SetColor(shaderKeywordEmissionColor, emissiveColor * LightSource.intensity);

            if (emissiveMaterial && emissiveMaterial.HasProperty("_EmissiveExposureWeight"))
                emissiveMaterial.SetFloat("_EmissiveExposureWeight", .5f);

        }

        // Update each of the additional emissive renderers if defined.
        if (additionalEmissiveRenderers != null && additionalEmissiveRenderers.Length > 0) {
            for (int i = 0; i < additionalEmissiveRenderers.Length; i++) {
                var emissiveData = additionalEmissiveRenderers[i];
                if (emissiveData == null)
                    continue;

                // Check if we have a valid renderer and a valid index in that renderer's materials array.
                if (emissiveData.emissiveRenderer == null)
                    continue;

                if (emissiveData.emissiveMaterialIndex < 0
                    || emissiveData.emissiveMaterialIndex >= emissiveData.emissiveRenderer.materials.Length)
                    continue;

                // Assign the emissive material if not yet assigned.
                if (emissiveData.emissiveMaterial == null) {
                    emissiveData.emissiveMaterial =
                        emissiveData.emissiveRenderer.materials[emissiveData.emissiveMaterialIndex];
                }

                // Enable emission keyword, if specified.
                if (emissiveData.emissiveMaterial
                    && !string.IsNullOrEmpty(emissiveData.shaderKeywordEmissionEnable)) {
                    emissiveData.emissiveMaterial.EnableKeyword(emissiveData.shaderKeywordEmissionEnable);
                }

                // Update the emission color, if specified.
                if (emissiveData.emissiveMaterial
                    && !string.IsNullOrEmpty(emissiveData.shaderKeywordEmissionColor)) {
                    emissiveData.emissiveMaterial.SetColor(
                        emissiveData.shaderKeywordEmissionColor,
                        emissiveData.emissiveColor * LightSource.intensity
                    );
                }

                if (emissiveData.emissiveMaterial && emissiveData.emissiveMaterial.HasProperty("_EmissiveExposureWeight"))
                    emissiveData.emissiveMaterial.SetFloat("_EmissiveExposureWeight", .5f);

            }
        }

        LightSource.renderMode = lightRendererMode;

#if !BCG_URP && !BCG_HDRP
        // Built-in pipeline lens flare
        if (lensFlare)
            LensFlare();
#else
        // URP/HDRP pipeline lens flare
        if (lensFlare_SRP)
            LensFlare_SRP();
#endif

    }

    /// <summary>
    /// Smoothly transitions the light�s intensity toward the given target. If broken, the light is forced to 0.
    /// </summary>
    /// <param name="_intensity">Desired intensity to approach.</param>
    private void Lighting(float _intensity) {

        if (!broken)
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, _intensity, Time.deltaTime * smoothness * 100f);
        else
            LightSource.intensity = Mathf.Lerp(LightSource.intensity, 0f, Time.deltaTime * smoothness * 100f);

    }

#if !BCG_URP && !BCG_HDRP
    /// <summary>
    /// Built-in pipeline lens flare logic. Adjusts brightness based on camera distance and angle relative to the light.
    /// </summary>
    private void LensFlare() {

        if (!useLensFlares)
            return;

        if (!mainCam)
            return;

        Vector3 transformPos = transform.position;
        Vector3 transformDir = transform.forward;
        Vector3 camPos = mainCam.transform.position;

        float distanceTocam = Vector3.Distance(transformPos, camPos);
        float angle = Vector3.Angle(transformDir, camPos - transformPos);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (4f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare.brightness = finalFlareBrightness * LightSource.intensity;
        lensFlare.color = LightSource.color;

    }
#else
    /// <summary>
    /// URP/HDRP SRP lens flare logic. Adjusts brightness based on camera distance and angle relative to the light.
    /// </summary>
    private void LensFlare_SRP() {

        if (!useLensFlares)
            return;

        if (!mainCam)
            return;

        float distanceTocam = Vector3.Distance(transform.position, mainCam.transform.position);
        float angle = Vector3.Angle(transform.forward, mainCam.transform.position - transform.position);

        if (!Mathf.Approximately(angle, 0f))
            finalFlareBrightness = flareBrightness * (8f / distanceTocam) * ((300f - (3f * angle)) / 300f) / 3f;
        else
            finalFlareBrightness = flareBrightness;

        if (finalFlareBrightness < 0)
            finalFlareBrightness = 0f;

        lensFlare_SRP.attenuationByLightShape = false;
        lensFlare_SRP.intensity = finalFlareBrightness * LightSource.intensity;

    }
#endif

    /// <summary>
    /// Called by RCCP_Damage or similar logic on collisions. Reduces strength by the given impulse. 
    /// If strength falls below 'breakPoint', the light is considered broken.
    /// </summary>
    /// <param name="impulse">Collision impulse magnitude.</param>
    public void OnCollision(float impulse) {

        if (!enabled)
            return;

        if (broken)
            return;

        strength -= impulse * 20f;
        strength = Mathf.Clamp(strength, 0f, Mathf.Infinity);

        if (strength <= breakPoint)
            broken = true;

    }

    /// <summary>
    /// Repairs the light, resetting its strength to the original and removing any 'broken' status.
    /// </summary>
    public void OnRepair() {

        if (!enabled)
            return;

        strength = orgStrength;
        broken = false;

    }

    /// <summary>
    /// Retrieves the current color of this Light component.
    /// </summary>
    /// <returns>The current color of this light component.</returns>
    public Color GetLightColor() {

        return LightSource.color;

    }

    /// <summary>
    /// Assigns a new color to this Light component.
    /// </summary>
    /// <param name="color">The new color to apply to this light.</param>
    public void SetLightColor(Color color) {

        LightSource.color = color;

    }

    /// <summary>
    /// Resets intensity and lens flare brightness, typically called when reloading vehicle states.
    /// </summary>
    public void Reload() {

        LightSource.intensity = 0f;
        finalFlareBrightness = 0f;

    }

    private void OnValidate() {

#if BCG_HDRP

        // HDRP Lit shader expects "_EmissiveColor" rather than "_EmissionColor"
        shaderKeywordEmissionEnable = string.IsNullOrEmpty(shaderKeywordEmissionEnable)
            ? "_EMISSION"
            : shaderKeywordEmissionEnable;

        shaderKeywordEmissionColor = string.IsNullOrEmpty(shaderKeywordEmissionColor) || shaderKeywordEmissionColor == "_EmissionColor"
            ? "_EmissiveColor"
            : shaderKeywordEmissionColor;

#else

        // Ensures all additional emissive renderers have default color and keywords if not set.
        shaderKeywordEmissionEnable = string.IsNullOrEmpty(shaderKeywordEmissionEnable)
    ? "_EMISSION"
    : shaderKeywordEmissionEnable;

        shaderKeywordEmissionColor = string.IsNullOrEmpty(shaderKeywordEmissionColor)
            ? "_EmissionColor"
            : shaderKeywordEmissionColor;

#endif

        if (additionalEmissiveRenderers != null && additionalEmissiveRenderers.Length >= 1) {

            for (int i = 0; i < additionalEmissiveRenderers.Length; i++) {

                if (additionalEmissiveRenderers[i] != null) {

                    if (additionalEmissiveRenderers[i].emissiveColor == new Color(0f, 0f, 0f, 0f))
                        additionalEmissiveRenderers[i].emissiveColor = Color.white;

                    if (additionalEmissiveRenderers[i].shaderKeywordEmissionEnable == "")
                        additionalEmissiveRenderers[i].shaderKeywordEmissionEnable = shaderKeywordEmissionEnable;

                    if (additionalEmissiveRenderers[i].shaderKeywordEmissionColor == "")
                        additionalEmissiveRenderers[i].shaderKeywordEmissionColor = shaderKeywordEmissionColor;

                }

            }

        }

#if UNITY_EDITOR && !BCG_URP && !BCG_HDRP
        // Ensure LensFlare ignores vehicle layers so the car doesn't occlude its own flares
        ValidateLensFlareIgnoreLayers();
#endif

    }

#if UNITY_EDITOR && !BCG_URP && !BCG_HDRP
    /// <summary>
    /// Ensures the LensFlare component ignores RCCP vehicle layers.
    /// Called from OnValidate to automatically fix existing lights.
    /// </summary>
    private void ValidateLensFlareIgnoreLayers() {

        if (!TryGetComponent<LensFlare>(out var flare)) return;

        // Safety check for OnValidate - settings might not be loaded yet
        if (RCCP_Settings.Instance == null) return;

        // Build mask from valid layer strings only
        int expectedMask = 0;

        if (!string.IsNullOrEmpty(RCCP_Settings.Instance.RCCPLayer))
            expectedMask |= LayerMask.GetMask(RCCP_Settings.Instance.RCCPLayer);

        if (!string.IsNullOrEmpty(RCCP_Settings.Instance.RCCPWheelColliderLayer))
            expectedMask |= LayerMask.GetMask(RCCP_Settings.Instance.RCCPWheelColliderLayer);

        if (!string.IsNullOrEmpty(RCCP_Settings.Instance.RCCPDetachablePartLayer))
            expectedMask |= LayerMask.GetMask(RCCP_Settings.Instance.RCCPDetachablePartLayer);

        // If mask is 0, no valid layers found - skip
        if (expectedMask == 0) return;

        // Use SerializedObject to access the hidden property
        var serializedFlare = new UnityEditor.SerializedObject(flare);
        var ignoreLayers = serializedFlare.FindProperty("m_IgnoreLayers");

        if (ignoreLayers != null && (ignoreLayers.intValue & expectedMask) != expectedMask) {
            // Add our layers to existing mask (preserve user's custom layers)
            ignoreLayers.intValue |= expectedMask;
            serializedFlare.ApplyModifiedPropertiesWithoutUndo();
        }

        // Ensure brightness is 0 (RCCP_Light controls it dynamically)
        if (flare.brightness != 0f)
            flare.brightness = 0f;

    }
#endif

}

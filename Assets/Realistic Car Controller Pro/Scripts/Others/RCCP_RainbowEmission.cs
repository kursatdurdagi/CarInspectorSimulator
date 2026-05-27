//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Rainbow Emission")]
/// <summary>Cycles the emission color of a target renderer smoothly through the HSV hue range for a rainbow effect.</summary>
public class RCCP_RainbowEmission : MonoBehaviour {

    [Tooltip("Name of the GameObject whose renderer emission color will cycle. Found once on Start via GameObject.Find.")]
    [SerializeField] private string targetName = "Plane";

    [Tooltip("Hue cycles per second. 0.25 = one full rainbow every 4 seconds.")]
    [Range(0f, 2f)] [SerializeField] private float cycleSpeed = 0.25f;

    [Tooltip("Emission brightness multiplier. Use values > 1 together with HDR + Bloom for a glow effect.")]
    [Range(0f, 10f)] [SerializeField] private float intensity = 1f;

    private Material targetMaterial;
    private float hue;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Start() {

        GameObject target = GameObject.Find(targetName);

        if (target == null) {
            Debug.LogWarning($"RCCP_RainbowEmission: No GameObject named '{targetName}' found in the scene.");
            enabled = false;
            return;
        }

        if (!target.TryGetComponent(out Renderer r)) {
            Debug.LogWarning($"RCCP_RainbowEmission: '{targetName}' has no Renderer component.");
            enabled = false;
            return;
        }

        targetMaterial = r.material;
        targetMaterial.EnableKeyword("_EMISSION");

    }

    private void Update() {

        hue = Mathf.Repeat(hue + cycleSpeed * Time.deltaTime, 1f);

        Color rgb = Color.HSVToRGB(hue, 1f, 1f) * intensity;
        rgb.a = 1f;

        targetMaterial.SetColor(EmissionColorId, rgb);

    }

}

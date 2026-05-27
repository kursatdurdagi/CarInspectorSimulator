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

/// <summary>
/// Fades in a CanvasGroup when the GameObject is enabled.
/// Attach to UI pages that have a CanvasGroup component.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Page Fader")]
public class RCCP_UI_PageFader : MonoBehaviour {

    [Tooltip("Duration of the fade-in animation in seconds.")]
    [Range(0.05f, 1f)]
    public float duration = 0.2f;

    private CanvasGroup canvasGroup;

    private void Awake() {

        TryGetComponent(out canvasGroup);

    }

    private void OnEnable() {

        if (canvasGroup == null)
            TryGetComponent(out canvasGroup);

        StopAllCoroutines();
        StartCoroutine(FadeIn());

    }

    private IEnumerator FadeIn() {

        canvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < duration) {

            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;

        }

        canvasGroup.alpha = 1f;

    }

}

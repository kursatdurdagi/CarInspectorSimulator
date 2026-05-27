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
/// UI informer panel with the text.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Informer")]
public class RCCP_UI_Informer : RCCP_UIComponent {

    private static RCCP_UI_Informer instance;

    /// <summary>
    /// Informer as instance.
    /// </summary>
    public static RCCP_UI_Informer Instance {

        get {

#if !UNITY_2022_1_OR_NEWER
            if (instance == null)
                instance = FindObjectOfType<RCCP_UI_Informer>();
#else
            if (instance == null)
                instance = FindAnyObjectByType<RCCP_UI_Informer>();
#endif

            return instance;

        }

    }

    /// <summary>
    /// Informer text.
    /// </summary>
    [Tooltip("TextMeshPro label that displays the informer message to the player.")]
    public TMP_Text informerText;

    /// <summary>
    /// Canvas group.
    /// </summary>
    [Tooltip("CanvasGroup whose GameObject is toggled on/off to show and hide the informer panel.")]
    public CanvasGroup cGroup;

    /// <summary>
    /// Timer to deactive the canvas.
    /// </summary>
    [Tooltip("Duration in seconds the informer message stays visible before auto-hiding.")]
    [Min(0f)] public float timer = 3f;

    /// <summary>
    /// Timer.
    /// </summary>
    [Min(0f)] private float time = 0f;

    private void OnEnable() {

        RCCP_Events.OnRCCPUIInformer += RCCP_Events_OnRCCPUIInformer;

    }

    private void RCCP_Events_OnRCCPUIInformer(string text) {

        Display(text);

    }

    private void Update() {

        //  Timer.
        time -= Time.deltaTime;

        //  Limiting the timer.
        if (time < 0)
            time = 0f;

        //  If timer is 0, disable the canvas group.
        if (time <= 0 && cGroup.gameObject.activeSelf)
            cGroup.gameObject.SetActive(false);

    }

    /// <summary>
    /// Displaying the target string.
    /// </summary>
    /// <param name="textToDisplay"></param>
    public void Display(string textToDisplay) {

        //  If no informer text found, or no canvas group found, return.
        if (!informerText || !cGroup)
            return;

        time = timer;
        cGroup.gameObject.SetActive(true);
        if (informerText.gameObject.TryGetComponent<Animator>(out var animator))
            animator.Play(0);
        informerText.text = textToDisplay;

    }

    private void OnDisable() {

        RCCP_Events.OnRCCPUIInformer -= RCCP_Events_OnRCCPUIInformer;

    }

}

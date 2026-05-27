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
/// UI Steering Wheel controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP UI Steering Wheel")]
public class RCCP_UI_SteeringWheelController : RCCP_UIComponent {

    /// <summary>
    /// Actual steering wheel texture. We'll be using to get events from it.
    /// </summary>
    private Image steeringWheelTexture;     //  Steering wheel texture.

    /// <summary>
    /// Input value between -1f and 1f.
    /// </summary>
    [Tooltip("Current steering input value between -1 (full left) and 1 (full right).")]
    [Range(-1f, 1f)] public float input = 0f;

    /// <summary>
    /// Current steering angle.
    /// </summary>
    private float steeringWheelAngle = 0f;

    /// <summary>
    /// Maximum steering angle.
    /// </summary>
    [Tooltip("Maximum on-screen rotation of the steering wheel texture, in degrees.")]
    [Min(0f)] public float steeringWheelMaximumsteerAngle = 270f;

    /// <summary>
    /// Steering wheel will reset itself if player doesn't use it with X amount of speed.
    /// </summary>
    [Tooltip("Speed at which the wheel auto-centers back to neutral when the player releases it.")]
    [Min(0f)] public float steeringWheelResetPosSpeed = 20f;

    /// <summary>
    /// Steering wheel center dead zone radius tolerance.
    /// </summary>
    [Tooltip("Pixel radius around the wheel center where touch input is ignored (dead zone).")]
    [Min(0f)] public float steeringWheelCenterDeadZoneRadius = 5f;

    /// <summary>
    /// Rect transform of the actual steering wheel.
    /// </summary>
    private RectTransform steeringWheelRect;

    /// <summary>
    /// Canvas group.
    /// </summary>
    private CanvasGroup steeringWheelCanvasGroup;

    private float steeringWheelTempAngle, steeringWheelNewAngle = 0f;
    private bool steeringWheelPressed = false;

    private Vector2 steeringWheelCenter, steeringWheelTouchPos = new Vector2();

    private EventTrigger eventTrigger;

    private void Awake() {

        //	Initializing the ui wheel with proper event triggers.
        SteeringWheelInit();

    }

    private void OnEnable() {

        //  Make sure to reset then when enabling / disabling the steering wheel.
        steeringWheelPressed = false;
        input = 0f;

    }

    private void LateUpdate() {

        //	Visual steering wheel controlling.
        SteeringWheelControlling();

        //	Receiving input from the steering wheel.
        input = GetSteeringWheelInput();

    }

    /// <summary>
    /// Initialization of the steering wheel.
    /// </summary>
    private void SteeringWheelInit() {

        TryGetComponent(out steeringWheelTexture);

        if (steeringWheelRect && !steeringWheelTexture)
            return;

        steeringWheelRect = steeringWheelTexture.rectTransform;
        steeringWheelTexture.TryGetComponent(out steeringWheelCanvasGroup);
        steeringWheelCenter = steeringWheelRect.position;

        SteeringWheelEventsInit();

    }

    /// <summary>
    /// Events Initialization For Steering Wheel.
    /// </summary>
    private void SteeringWheelEventsInit() {

        steeringWheelTexture.TryGetComponent(out eventTrigger);

        var a = new EventTrigger.TriggerEvent();
        a.AddListener(data => {
            var evData = (PointerEventData)data;
            data.Use();

            steeringWheelPressed = true;
            steeringWheelTouchPos = evData.position;
            steeringWheelTempAngle = Vector2.Angle(Vector2.up, evData.position - steeringWheelCenter);
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = a, eventID = EventTriggerType.PointerDown });


        var b = new EventTrigger.TriggerEvent();
        b.AddListener(data => {
            var evData = (PointerEventData)data;
            data.Use();
            steeringWheelTouchPos = evData.position;
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = b, eventID = EventTriggerType.Drag });


        var c = new EventTrigger.TriggerEvent();
        c.AddListener(data => {
            steeringWheelPressed = false;
        });

        eventTrigger.triggers.Add(new EventTrigger.Entry { callback = c, eventID = EventTriggerType.EndDrag });

    }

    /// <summary>Returns the current normalized steering wheel input value.</summary>
    /// <returns>Steering input from -1 (full left) to 1 (full right).</returns>
    public float GetSteeringWheelInput() {

        return Mathf.Round(steeringWheelAngle / steeringWheelMaximumsteerAngle * 100) / 100;

    }

    /// <summary>Returns whether the steering wheel is currently being held by the player.</summary>
    /// <returns>True if the player is touching/holding the steering wheel.</returns>
    public bool IsPressed() {

        return steeringWheelPressed;

    }

    private void SteeringWheelControlling() {

        if (!steeringWheelCanvasGroup || !steeringWheelRect) {

            if (steeringWheelTexture.gameObject)
                steeringWheelTexture.gameObject.SetActive(false);

            return;

        }

        if (!steeringWheelTexture.gameObject.activeSelf)
            steeringWheelTexture.gameObject.SetActive(true);

        if (steeringWheelPressed) {

            steeringWheelNewAngle = Vector2.Angle(Vector2.up, steeringWheelTouchPos - steeringWheelCenter);

            if (Vector2.Distance(steeringWheelTouchPos, steeringWheelCenter) > steeringWheelCenterDeadZoneRadius) {

                if (steeringWheelTouchPos.x > steeringWheelCenter.x)
                    steeringWheelAngle += steeringWheelNewAngle - steeringWheelTempAngle;
                else
                    steeringWheelAngle -= steeringWheelNewAngle - steeringWheelTempAngle;

            }

            if (steeringWheelAngle > steeringWheelMaximumsteerAngle)
                steeringWheelAngle = steeringWheelMaximumsteerAngle;
            else if (steeringWheelAngle < -steeringWheelMaximumsteerAngle)
                steeringWheelAngle = -steeringWheelMaximumsteerAngle;

            steeringWheelTempAngle = steeringWheelNewAngle;

        } else {

            if (!Mathf.Approximately(0f, steeringWheelAngle)) {

                float deltaAngle = steeringWheelResetPosSpeed;

                if (Mathf.Abs(deltaAngle) > Mathf.Abs(steeringWheelAngle)) {
                    steeringWheelAngle = 0f;
                    return;
                }

                steeringWheelAngle = Mathf.MoveTowards(steeringWheelAngle, 0f, deltaAngle * (Time.deltaTime * 100f));

            }

        }

        steeringWheelRect.eulerAngles = new Vector3(0f, 0f, -steeringWheelAngle);

    }

    private void OnDisable() {

        //  Make sure to reset then when enabling / disabling the steering wheel.
        steeringWheelPressed = false;
        input = 0f;

    }

}

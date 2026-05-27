//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#pragma warning disable 0414

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mobile UI Drag used for orbiting Showroom Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Mobile/RCCP UI Mobile Drag")]
public class RCCP_UI_MobileDrag : RCCP_UIComponent, IDragHandler, IEndDragHandler {

    /// <summary>
    /// Showroom camera.
    /// </summary>
    private RCCP_ShowroomCamera showroomCamera;

    private void Awake() {

#if !UNITY_2022_1_OR_NEWER
        showroomCamera = FindObjectOfType<RCCP_ShowroomCamera>(true);
#else
        showroomCamera = FindAnyObjectByType<RCCP_ShowroomCamera>(FindObjectsInactive.Include);
#endif

    }

    /// <summary>Handles drag input for mobile camera orbiting.</summary>
    /// <param name="eventData">Pointer event data from the UI event system.</param>
    public void OnDrag(PointerEventData data) {

        if (showroomCamera)
            showroomCamera.OnDrag(data);

    }

    /// <summary>Stops drag input when the user releases the touch.</summary>
    /// <param name="eventData">Pointer event data from the UI event system.</param>
    public void OnEndDrag(PointerEventData data) {

        //

    }

}

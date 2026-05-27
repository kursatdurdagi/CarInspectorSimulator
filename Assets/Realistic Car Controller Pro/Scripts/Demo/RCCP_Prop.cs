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

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Prop")]
/// <summary>Destructible prop object that reacts to vehicle collisions with physics impulse and optional self-destruction.</summary>
public class RCCP_Prop : RCCP_GenericComponent {

    /// <summary>Seconds to wait after a qualifying vehicle collision before destroying this prop. Set to 0 to disable auto-destroy.</summary>
    [Tooltip("Seconds to wait after a qualifying vehicle collision before destroying this prop. Set to 0 to disable auto-destroy.")]
    [Min(0f)] public float destroyAfterCollision = 3f;

    private void Awake() {

#if UNITY_2022_2_OR_NEWER
        IgnoreLayers();
#endif

    }

    private void OnEnable() {

        if (RCCPSettings.setLayers && RCCPSettings.RCCPPropLayer != "")
            gameObject.layer = LayerMask.NameToLayer(RCCPSettings.RCCPPropLayer);

        if (TryGetComponent<Rigidbody>(out var rigid))
            rigid.Sleep();

    }

    private void Reset() {

        if (RCCP_Settings.Instance.setLayers && RCCP_Settings.Instance.RCCPPropLayer != "")
            gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer);

#if UNITY_2022_2_OR_NEWER
        IgnoreLayers();
#endif

    }

#if UNITY_2022_2_OR_NEWER
    private void IgnoreLayers() {

        //  Getting collider.
        Collider[] partColliders = GetComponentsInChildren<Collider>(true);

        LayerMask curLayerMask = -1;

        foreach (Collider collider in partColliders) {

            curLayerMask = collider.excludeLayers;
            curLayerMask |= (1 << LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer));
            collider.excludeLayers = curLayerMask;

        }

    }
#endif

    private void OnCollisionEnter(Collision collision) {

        if (destroyAfterCollision <= 0 || collision.impulse.magnitude < 100)
            return;

        Destroy(gameObject, destroyAfterCollision);

    }

}

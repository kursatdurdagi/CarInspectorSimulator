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

#if BCG_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if BCG_URP
using UnityEngine.Rendering.Universal;
#endif

#if BCG_URP || BCG_HDRP

/// <summary>
/// Upgradable decal.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Decal")]
[RequireComponent(typeof(DecalProjector))]
public class RCCP_VehicleUpgrade_Decal : RCCP_Component {

    private DecalProjector decalRenderer;     //  Renderer, actually a box.

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material">The decal material to apply to the projector.</param>
    public void SetDecal(Material material) {

        //  Getting the mesh renderer.
        if (!decalRenderer)
            decalRenderer = GetComponentInChildren<DecalProjector>();

        //  Return if renderer not found.
        if (!decalRenderer)
            return;

        //  Setting material of the renderer.
        decalRenderer.material = material;

    }

    /// <summary>Editor callback that validates and refreshes the decal projector material.</summary>
    public void OnValidate() {

        if (!TryGetComponent<DecalProjector>(out var dp))
            return;

        dp.scaleMode = DecalScaleMode.InheritFromHierarchy;
        dp.pivot = Vector3.zero;
        dp.drawDistance = 500f;

        if (RCCP_Settings.Instance == null)
            return;

        if (dp.material == null)
            dp.material = RCCP_Settings.Instance.defaultDecalMaterial;

        if (dp.material != null && dp.material.name.Contains("Default"))
            dp.material = RCCP_Settings.Instance.defaultDecalMaterial;

    }

}

#else

/// <summary>
/// Upgradable decal.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Decal")]
public class RCCP_VehicleUpgrade_Decal : RCCP_Component {

    /// <summary>
    /// Sets target material of the decal.
    /// </summary>
    /// <param name="material">The decal material to apply to the projector.</param>
    public void SetDecal(Material material) {

        //Debug.LogError("Decals are working with URP only!");
        return;

    }

}
#endif
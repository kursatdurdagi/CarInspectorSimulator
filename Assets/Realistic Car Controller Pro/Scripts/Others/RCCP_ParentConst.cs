//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

/// <summary>
/// Maintains a fixed parent-child relationship by continuously syncing position and rotation to the target parent transform.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Parent Const")]
public class RCCP_ParentConst : RCCP_GenericComponent {

    private ParentConstraint parentConstraint;

    /// <summary>
    /// Unity Parent Const component.
    /// </summary>
    public ParentConstraint ParentConstraint {

        get {

            if (parentConstraint == null)
                TryGetComponent(out parentConstraint);

            if (parentConstraint == null)
                parentConstraint = gameObject.AddComponent<ParentConstraint>();

            return parentConstraint;

        }

    }

    /// <summary>Restores the original parent-child relationship and local transform.</summary>
    public void Restore() {

        foreach (Transform item in transform)
            item.SetParent(transform.parent, true);

#if UNITY_EDITOR

        if (UnityEditor.EditorApplication.isPlaying)
            Destroy(gameObject);
        else
            DestroyImmediate(gameObject);

#else

 Destroy(gameObject);

#endif

    }

}

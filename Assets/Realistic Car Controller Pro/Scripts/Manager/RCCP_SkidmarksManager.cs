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

/// <summary>
/// Manages skidmark instances for all ground material types, creating and recycling skidmark meshes as needed.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Skidmarks Manager")]
[DefaultExecutionOrder(-50)]
public class RCCP_SkidmarksManager : RCCP_Singleton<RCCP_SkidmarksManager> {

    /// <summary>
    /// All skidmarks.
    /// </summary>
    private RCCP_Skidmarks[] skidmarks;

    /// <summary>
    /// Index of the skidmarks.
    /// </summary>
    private int[] skidmarksIndexes;

    /// <summary>
    /// Last index of the ground.
    /// </summary>
    private int _lastGroundIndex = 0;

    private void Awake() {

        //  Creating new skidmarks and initializing them with given ground materials in RCCP Ground Materials.
        skidmarks = new RCCP_Skidmarks[RCCPGroundMaterials.frictions.Length];
        skidmarksIndexes = new int[skidmarks.Length];

        for (int i = 0; i < skidmarks.Length; i++) {

            skidmarks[i] = Instantiate(RCCPGroundMaterials.frictions[i].skidmark, Vector3.zero, Quaternion.identity);
            skidmarks[i].transform.name = skidmarks[i].transform.name + "_" + RCCPGroundMaterials.frictions[i].groundMaterial.name;
            skidmarks[i].transform.SetParent(transform, true);

        }

    }

    /// <summary>
    /// Adds a skidmark section at the given position for the specified ground material index.
    /// </summary>
    /// <param name="pos">World position of the skidmark section.</param>
    /// <param name="normal">Surface normal at the skidmark position.</param>
    /// <param name="intensity">Skidmark opacity from 0 (transparent) to 1 (fully visible).</param>
    /// <param name="width">Width of the skidmark section in world units.</param>
    /// <param name="lastIndex">Index of the previous skidmark section to connect to, or -1 for a new strip.</param>
    /// <param name="groundIndex">Index into the ground materials array identifying the surface type.</param>
    /// <returns>The index of the newly created skidmark section.</returns>
    public int AddSkidMark(Vector3 pos, Vector3 normal, float intensity, float width, int lastIndex, int groundIndex) {

        if (_lastGroundIndex != groundIndex) {

            _lastGroundIndex = groundIndex;
            return -1;

        }

        skidmarksIndexes[groundIndex] = skidmarks[groundIndex].AddSkidMark(pos, normal, intensity, width, lastIndex);

        return skidmarksIndexes[groundIndex];

    }

    /// <summary>
    /// Cleans all skidmarks.
    /// </summary>
    public void CleanSkidmarks() {

        for (int i = 0; i < skidmarks.Length; i++)
            skidmarks[i].Clean();

    }

    /// <summary>
    /// Cleans target skidmarks.
    /// </summary>
    /// <param name="index"></param>
    public void CleanSkidmarks(int index) {

        skidmarks[index].Clean();

    }

}

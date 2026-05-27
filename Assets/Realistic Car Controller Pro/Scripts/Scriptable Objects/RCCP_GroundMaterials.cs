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
/// Ground materials for variable ground physics.
/// </summary>
[System.Serializable]
public class RCCP_GroundMaterials : ScriptableObject {

    #region singleton
    private static RCCP_GroundMaterials instance;
    /// <summary>
    /// Singleton instance of the ground materials configuration, loaded from Resources.
    /// </summary>
    public static RCCP_GroundMaterials Instance { get { if (instance == null) instance = Resources.Load("RCCP_GroundMaterials") as RCCP_GroundMaterials; return instance; } }
    #endregion

    /// <summary>
    /// Defines friction, audio, particle, and skidmark properties for a specific physics material surface type.
    /// </summary>
    [System.Serializable]
    public class GroundMaterialFrictions {

        /// <summary>
        /// Physic material.
        /// </summary>
#if UNITY_2023_3_OR_NEWER
    [Tooltip("Physics material assigned to this surface type for friction lookup.")]
    public PhysicsMaterial groundMaterial;    // DOTS
#else
        [Tooltip("Physics material assigned to this surface type for friction lookup.")]
        public PhysicMaterial groundMaterial;                   // PhysX
#endif

        /// <summary>
        /// Forward stiffness.
        /// </summary>
        [Min(0f), Tooltip("Multiplier applied to the wheel's longitudinal friction on this surface.")]
        public float forwardStiffness = 1f;

        /// <summary>
        /// Sideways stiffness.
        /// </summary>
        [Min(0f), Tooltip("Multiplier applied to the wheel's lateral friction on this surface.")]
        public float sidewaysStiffness = 1f;

        /// <summary>
        /// Target slip limit.
        /// </summary>
        [Min(0f), Tooltip("Slip threshold before the wheel is considered sliding on this surface.")]
        public float slip = .25f;

        /// <summary>
        /// Damp force.
        /// </summary>
        [Min(0f), Tooltip("Damping force applied to wheel oscillation on this surface.")]
        public float damp = 1f;

        /// <summary>
        /// Volume of the ground sound.
        /// </summary>
        [Range(0f, 1f), Tooltip("Playback volume of the surface rolling/skid sound effect.")]
        public float volume = 1f;

        /// <summary>
        /// Ground particles.
        /// </summary>
        [Tooltip("Particle prefab spawned at the wheel contact point on this surface.")]
        public GameObject groundParticles;

        /// <summary>
        /// Ground audio clip.
        /// </summary>
        [Tooltip("Audio clip played when wheels roll or skid on this surface.")]
        public AudioClip groundSound;

        /// <summary>
        /// Skidmarks.
        /// </summary>
        [Tooltip("Skidmark renderer used for tire marks on this surface.")]
        public RCCP_Skidmarks skidmark;

    }

    /// <summary>
    /// Ground materials.
    /// </summary>
    [Tooltip("Array of surface friction profiles mapped to physics materials.")]
    public GroundMaterialFrictions[] frictions;

    /// <summary>
    /// Terrain ground materials.
    /// </summary>
    [System.Serializable]
    public class TerrainFrictions {

        /// <summary>
        /// Physic material.
        /// </summary>
#if UNITY_2023_3_OR_NEWER
    [Tooltip("Physics material assigned to this terrain surface for friction lookup.")]
    public PhysicsMaterial groundMaterial;    // DOTS
#else
        [Tooltip("Physics material assigned to this terrain surface for friction lookup.")]
        public PhysicMaterial groundMaterial;                   // PhysX
#endif

        /// <summary>
        /// Maps a terrain splatmap texture layer index to a ground friction profile.
        /// </summary>
        [System.Serializable]
        public class SplatmapIndexes {

            /// <summary>
            /// Terrain splatmap texture layer index that maps to the parent TerrainFrictions entry.
            /// </summary>
            [Min(0), Tooltip("Splatmap layer index that maps to the parent terrain friction profile.")]
            public int index = 0;

        }

        /// <summary>
        /// Splatmap indexes.
        /// </summary>
        [Tooltip("Splatmap texture layer indices associated with this terrain friction profile.")]
        public SplatmapIndexes[] splatmapIndexes;

    }

    /// <summary>
    /// Terrain ground materials.
    /// </summary>
    [Tooltip("Array of terrain-specific friction profiles using splatmap layer mapping.")]
    public TerrainFrictions[] terrainFrictions;

    /// <summary>
    /// Ensures all ground particle prefabs have the required RCCP_WheelSlipParticles component, adding it if missing.
    /// </summary>
    public void CheckWheelPrefabsForMissingScript() {

        if (frictions != null && frictions.Length > 0) {

            for (int i = 0; i < frictions.Length; i++) {

                if (frictions[i] != null && frictions[i].groundParticles != null) {

                    if (!frictions[i].groundParticles.TryGetComponent(out RCCP_WheelSlipParticles wsp))
                        frictions[i].groundParticles.AddComponent<RCCP_WheelSlipParticles>();

                }

            }

        }

    }

}



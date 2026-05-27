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
/// Recorded clips.
/// </summary>
public class RCCP_Records : ScriptableObject {

    #region singleton
    private static RCCP_Records instance;
    /// <summary>Singleton instance of the recordings configuration, loaded from Resources.</summary>
    public static RCCP_Records Instance { get { if (instance == null) instance = Resources.Load("RCCP_Records") as RCCP_Records; return instance; } }
    #endregion

    /// <summary>
    /// All records.
    /// </summary>
    [Tooltip("All saved record clips captured with the RCCP Recorder, available for playback.")]
    public List<RCCP_Recorder.RecordedClip> records = new List<RCCP_Recorder.RecordedClip>();

}

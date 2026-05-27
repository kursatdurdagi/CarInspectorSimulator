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
/// Represents a drivetrain output containing torque (NM) and rotational speed (RPM) values passed between drivetrain components.
/// </summary>
public class RCCP_Output {

    /// <summary>Output torque in Newton-meters.</summary>
    [Tooltip("Output torque in Newton-meters.")]
    public float NM;
    /// <summary>Output rotational speed in revolutions per minute.</summary>
    [Tooltip("Output rotational speed in revolutions per minute.")]
    public float RPM;

}

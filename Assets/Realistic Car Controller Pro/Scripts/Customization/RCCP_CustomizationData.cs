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
/// Serializable data container that stores all vehicle customization values including suspension, steering, driving aids, and visual settings.
/// </summary>
[System.Serializable]
public class RCCP_CustomizationData {

    /// <summary>
    /// Whether this customization data has been initialized with vehicle values.
    /// </summary>
    [Tooltip("Whether this data has been initialized with vehicle values.")]
    public bool initialized = false;

    /// <summary>
    /// Front axle suspension travel distance in meters.
    /// </summary>
    [Header("Suspension")]
    [Tooltip("Front axle suspension travel distance in meters.")]
    [Min(0f)] public float suspensionDistanceFront = .2f;
    /// <summary>
    /// Rear axle suspension travel distance in meters.
    /// </summary>
    [Tooltip("Rear axle suspension travel distance in meters.")]
    [Min(0f)] public float suspensionDistanceRear = .2f;

    /// <summary>
    /// Front axle suspension spring force in Newtons.
    /// </summary>
    [Tooltip("Front axle suspension spring force in Newtons.")]
    [Min(0f)] public float suspensionSpringForceFront = 55000f;
    /// <summary>
    /// Rear axle suspension spring force in Newtons.
    /// </summary>
    [Tooltip("Rear axle suspension spring force in Newtons.")]
    [Min(0f)] public float suspensionSpringForceRear = 55000f;

    /// <summary>
    /// Front axle suspension damper force for rebound and compression.
    /// </summary>
    [Tooltip("Front axle suspension damper force for rebound and compression.")]
    [Min(0f)] public float suspensionDamperFront = 3500f;
    /// <summary>
    /// Rear axle suspension damper force for rebound and compression.
    /// </summary>
    [Tooltip("Rear axle suspension damper force for rebound and compression.")]
    [Min(0f)] public float suspensionDamperRear = 3500f;

    /// <summary>
    /// Front axle suspension target position (0 = fully extended, 1 = fully compressed).
    /// </summary>
    [Tooltip("Front suspension rest position (0 = fully extended, 1 = fully compressed).")]
    [Range(0f, 1f)] public float suspensionTargetFront = .5f;
    /// <summary>
    /// Rear axle suspension target position (0 = fully extended, 1 = fully compressed).
    /// </summary>
    [Tooltip("Rear suspension rest position (0 = fully extended, 1 = fully compressed).")]
    [Range(0f, 1f)] public float suspensionTargetRear = .5f;

    /// <summary>
    /// Front wheel camber angle in degrees (negative = top of wheel tilts inward).
    /// </summary>
    [Header("Camber")]
    [Tooltip("Front wheel camber angle in degrees (negative tilts top inward).")]
    [Range(-15f, 15f)]
    public float cambersFront = 0f;
    /// <summary>
    /// Rear wheel camber angle in degrees (negative = top of wheel tilts inward).
    /// </summary>
    [Tooltip("Rear wheel camber angle in degrees (negative tilts top inward).")]
    [Range(-15f, 15f)]
    public float cambersRear = 0f;

    /// <summary>
    /// Normalized RPM threshold (0-1) at which automatic gear shifts occur.
    /// </summary>
    [Header("Transmission")]
    [Tooltip("Normalized RPM threshold (0-1) at which automatic gear shifts occur.")]
    [Range(0f, 1f)] public float gearShiftingThreshold = .8f;
    /// <summary>
    /// Clutch engagement threshold controlling how quickly the clutch engages.
    /// </summary>
    [Tooltip("How quickly the clutch engages after a gear change.")]
    [Range(0f, 1f)] public float clutchThreshold = .1f;

    /// <summary>
    /// Whether automatic counter-steering is enabled for oversteer recovery.
    /// </summary>
    [Header("Stability Aids")]
    [Tooltip("Enables automatic counter-steering during oversteer recovery.")]
    public bool counterSteering = true;
    /// <summary>
    /// Whether steering angle is limited based on vehicle speed.
    /// </summary>
    [Tooltip("Limits maximum steering angle at higher speeds.")]
    public bool steeringLimiter = true;

    /// <summary>
    /// Whether Anti-lock Braking System is enabled.
    /// </summary>
    [Tooltip("Enables Anti-lock Braking System to prevent wheel lock-up.")]
    public bool ABS;
    /// <summary>
    /// Whether Electronic Stability Program is enabled.
    /// </summary>
    [Tooltip("Enables Electronic Stability Program to reduce oversteer and understeer.")]
    public bool ESP;
    /// <summary>
    /// Whether Traction Control System is enabled.
    /// </summary>
    [Tooltip("Enables Traction Control System to limit wheel spin on acceleration.")]
    public bool TCS;
    /// <summary>
    /// Whether Steering Helper is enabled for velocity-aligned steering correction.
    /// </summary>
    [Tooltip("Enables Steering Helper for velocity-aligned steering correction.")]
    public bool SH;
    /// <summary>
    /// Whether Nitrous Oxide System is enabled.
    /// </summary>
    [Tooltip("Enables Nitrous Oxide System for temporary power boost.")]
    public bool NOS;
    /// <summary>
    /// Whether the engine rev limiter is enabled.
    /// </summary>
    [Tooltip("Enables engine rev limiter to cap maximum RPM.")]
    public bool revLimiter;
    /// <summary>
    /// Whether automatic transmission mode is active.
    /// </summary>
    [Tooltip("Uses automatic gear shifting instead of manual.")]
    public bool automaticTransmission;

    /// <summary>
    /// Custom headlight color tint.
    /// </summary>
    [Header("Lights / Colors")]
    [Tooltip("Custom color tint applied to headlights.")]
    public Color headlightColor = Color.white;
    /// <summary>
    /// Custom tire smoke particle color.
    /// </summary>
    [Tooltip("Custom color for tire smoke particles during burnouts and skids.")]
    public Color wheelSmokeColor = Color.white;

    /// <summary>
    /// Creates an empty customization data instance with default values.
    /// </summary>
    public RCCP_CustomizationData() { }

    /// <summary>
    /// Creates a customization data instance with all values specified.
    /// </summary>
    /// <param name="initialized">Whether the data is initialized.</param>
    /// <param name="suspensionDistanceFront">Front suspension travel distance.</param>
    /// <param name="suspensionDistanceRear">Rear suspension travel distance.</param>
    /// <param name="suspensionSpringForceFront">Front suspension spring force.</param>
    /// <param name="suspensionSpringForceRear">Rear suspension spring force.</param>
    /// <param name="suspensionDamperFront">Front suspension damper force.</param>
    /// <param name="suspensionDamperRear">Rear suspension damper force.</param>
    /// <param name="suspensionTargetFront">Front suspension target position.</param>
    /// <param name="suspensionTargetRear">Rear suspension target position.</param>
    /// <param name="cambersFront">Front wheel camber angle.</param>
    /// <param name="cambersRear">Rear wheel camber angle.</param>
    /// <param name="gearShiftingThreshold">Automatic gear shift RPM threshold.</param>
    /// <param name="clutchThreshold">Clutch engagement threshold.</param>
    /// <param name="counterSteering">Whether counter-steering is enabled.</param>
    /// <param name="steeringLimiter">Whether steering limiter is enabled.</param>
    /// <param name="ABS">Whether ABS is enabled.</param>
    /// <param name="ESP">Whether ESP is enabled.</param>
    /// <param name="TCS">Whether TCS is enabled.</param>
    /// <param name="SH">Whether Steering Helper is enabled.</param>
    /// <param name="NOS">Whether NOS is enabled.</param>
    /// <param name="revLimiter">Whether rev limiter is enabled.</param>
    /// <param name="automaticTransmission">Whether automatic transmission is active.</param>
    /// <param name="headlightColor">Headlight color tint.</param>
    /// <param name="wheelSmokeColor">Tire smoke particle color.</param>
    public RCCP_CustomizationData(bool initialized, float suspensionDistanceFront, float suspensionDistanceRear, float suspensionSpringForceFront, float suspensionSpringForceRear, float suspensionDamperFront, float suspensionDamperRear, float suspensionTargetFront, float suspensionTargetRear, float cambersFront, float cambersRear, float gearShiftingThreshold, float clutchThreshold, bool counterSteering, bool steeringLimiter, bool ABS, bool ESP, bool TCS, bool SH, bool NOS, bool revLimiter, bool automaticTransmission, Color headlightColor, Color wheelSmokeColor) {

        this.initialized = initialized;
        this.suspensionDistanceFront = suspensionDistanceFront;
        this.suspensionDistanceRear = suspensionDistanceRear;
        this.suspensionSpringForceFront = suspensionSpringForceFront;
        this.suspensionSpringForceRear = suspensionSpringForceRear;
        this.suspensionDamperFront = suspensionDamperFront;
        this.suspensionDamperRear = suspensionDamperRear;
        this.suspensionTargetFront = suspensionTargetFront;
        this.suspensionTargetRear = suspensionTargetRear;
        this.cambersFront = cambersFront;
        this.cambersRear = cambersRear;
        this.gearShiftingThreshold = gearShiftingThreshold;
        this.clutchThreshold = clutchThreshold;
        this.counterSteering = counterSteering;
        this.steeringLimiter = steeringLimiter;
        this.ABS = ABS;
        this.ESP = ESP;
        this.TCS = TCS;
        this.SH = SH;
        this.NOS = NOS;
        this.revLimiter = revLimiter;
        this.automaticTransmission = automaticTransmission;
        this.headlightColor = headlightColor;
        this.wheelSmokeColor = wheelSmokeColor;

    }

}

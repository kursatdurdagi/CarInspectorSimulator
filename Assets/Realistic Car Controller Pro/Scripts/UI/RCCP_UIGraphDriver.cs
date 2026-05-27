//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Reads vehicle telemetry data from the active RCCP_CarController each frame
/// and pushes it into an RCCP_UIGraph for real-time visualization.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Graph Driver")]
public class RCCP_UIGraphDriver : RCCP_UIComponent {

    /// <summary>
    /// Available data channels from the vehicle.
    /// </summary>
    public enum DataChannel {

        EngineRPM,
        Speed,
        ThrottleInput,
        BrakeInput,
        SteerInput,
        ProducedTorque,
        ForwardSlip,
        SidewaysSlip,
        GForce_Longitudinal,
        GForce_Lateral

    }

    /// <summary>
    /// Binds a data channel to a target series index on the graph.
    /// </summary>
    [System.Serializable]
    public class ChannelBinding {

        [Tooltip("Vehicle telemetry value to read each frame.")]
        public DataChannel channel;
        [Tooltip("Index of the graph series this channel pushes data into.")]
        public int targetSeriesIndex;

    }

    [Header("Target")]
    [Tooltip("The RCCP_UIGraph component to push data into.")]
    public RCCP_UIGraph targetGraph;

    [Header("Bindings")]
    [Tooltip("Map data channels to graph series indices.")]
    public List<ChannelBinding> bindings = new List<ChannelBinding>();

    private RCCP_CarController _cachedVehicle;
    private Rigidbody _cachedRigidbody;
    private Vector3 _previousVelocity;

    private void Update() {

        if (targetGraph == null) return;
        if (RCCPSceneManager == null) return;

        RCCP_CarController vehicle = RCCPSceneManager.activePlayerVehicle;

        if (vehicle == null) return;

        // Cache rigidbody when vehicle changes
        if (vehicle != _cachedVehicle) {

            _cachedVehicle = vehicle;
            vehicle.TryGetComponent(out _cachedRigidbody);
            _previousVelocity = _cachedRigidbody != null ? _cachedRigidbody.linearVelocity : Vector3.zero;

        }

        for (int i = 0; i < bindings.Count; i++) {

            float value = ReadChannel(vehicle, bindings[i].channel);
            targetGraph.PushSample(bindings[i].targetSeriesIndex, value);

        }

    }

    private float ReadChannel(RCCP_CarController vehicle, DataChannel channel) {

        switch (channel) {

            case DataChannel.EngineRPM:
                return vehicle.engineRPM;

            case DataChannel.Speed:
                return vehicle.speed;

            case DataChannel.ThrottleInput:
                return vehicle.throttleInput_V;

            case DataChannel.BrakeInput:
                return vehicle.brakeInput_V;

            case DataChannel.SteerInput:
                return vehicle.steerInput_V;

            case DataChannel.ProducedTorque:
                return vehicle.producedEngineTorque;

            case DataChannel.ForwardSlip:
                return AverageWheelValue(vehicle, true);

            case DataChannel.SidewaysSlip:
                return AverageWheelValue(vehicle, false);

            case DataChannel.GForce_Longitudinal:
                return ComputeGForce(vehicle, true);

            case DataChannel.GForce_Lateral:
                return ComputeGForce(vehicle, false);

            default:
                return 0f;

        }

    }

    private float AverageWheelValue(RCCP_CarController vehicle, bool forward) {

        if (vehicle.AllWheelColliders == null || vehicle.AllWheelColliders.Length == 0)
            return 0f;

        float sum = 0f;

        for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
            sum += forward ? vehicle.AllWheelColliders[i].ForwardSlip : vehicle.AllWheelColliders[i].SidewaysSlip;

        return sum / vehicle.AllWheelColliders.Length;

    }

    private float ComputeGForce(RCCP_CarController vehicle, bool longitudinal) {

        if (_cachedRigidbody == null) return 0f;

        Vector3 currentVelocity = _cachedRigidbody.linearVelocity;
        Vector3 acceleration = (currentVelocity - _previousVelocity) / Time.deltaTime;
        _previousVelocity = currentVelocity;

        float gForce = longitudinal
            ? Vector3.Dot(acceleration, vehicle.transform.forward) / 9.81f
            : Vector3.Dot(acceleration, vehicle.transform.right) / 9.81f;

        return gForce;

    }

    /// <summary>
    /// Display the active vehicle's engine torque curve as a static graph on the specified series.
    /// </summary>
    public void ShowTorqueCurve(int seriesIndex) {

        if (targetGraph == null || RCCPSceneManager == null) return;

        RCCP_CarController vehicle = RCCPSceneManager.activePlayerVehicle;
        if (vehicle == null) return;

        RCCP_Engine engine = vehicle.GetComponentInChildren<RCCP_Engine>();

        if (engine != null)
            targetGraph.SetStaticCurve(seriesIndex, engine.NMCurve, engine.minEngineRPM, engine.maxEngineRPM);

    }

    /// <summary>
    /// Display the active vehicle's steering curve as a static graph on the specified series.
    /// </summary>
    public void ShowSteeringCurve(int seriesIndex) {

        if (targetGraph == null || RCCPSceneManager == null) return;

        RCCP_CarController vehicle = RCCPSceneManager.activePlayerVehicle;
        if (vehicle == null) return;

        RCCP_Input input = vehicle.GetComponentInChildren<RCCP_Input>();

        if (input != null)
            targetGraph.SetStaticCurve(seriesIndex, input.steeringCurve, 0f, 200f);

    }

}

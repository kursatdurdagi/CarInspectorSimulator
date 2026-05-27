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
/// Record / Replay system for saving and playing back vehicle movement and input data.
/// Allows capturing a sequence of frames (inputs, transforms, velocities) and then replaying them.
/// </summary>
[DefaultExecutionOrder(-11)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Recorder")]
public class RCCP_Recorder : RCCP_Component {

    /// <summary>
    /// A recorded clip containing inputs, transforms, and velocities across multiple frames.
    /// </summary>
    [System.Serializable]
    public class RecordedClip {

        [Tooltip("Display name used to identify this recording in the records list.")]
        public string recordName = "New Record";

        /// <summary>
        /// All recorded inputs (throttle, brake, steer, etc.) for each frame.
        /// </summary>
        [HideInInspector] public VehicleInput[] inputs;

        /// <summary>
        /// All recorded positions/rotations for each frame.
        /// </summary>
        [HideInInspector] public VehicleTransform[] transforms;

        /// <summary>
        /// All recorded velocities/rotations for each frame.
        /// </summary>
        [HideInInspector] public VehicleVelocity[] rigids;

        public RecordedClip(VehicleInput[] _inputs, VehicleTransform[] _transforms, VehicleVelocity[] _rigids, string _recordName) {

            inputs = _inputs;
            transforms = _transforms;
            rigids = _rigids;
            recordName = _recordName;

        }

        public RecordedClip() { }

    }

    /// <summary>
    /// Holds the most recently saved or loaded recorded clip.
    /// </summary>
    [Tooltip("The currently loaded or most recently saved recorded clip used for playback.")]
    public RecordedClip recorded;

    private List<VehicleInput> Inputs;
    private List<VehicleTransform> Transforms;
    private List<VehicleVelocity> Rigidbodies;

    /// <summary>
    /// Stores a single frame of vehicle input data.
    /// </summary>
    [System.Serializable]
    public class VehicleInput {

        [Tooltip("Recorded throttle input value (0-1) for this frame.")]
        public float throttleInput;
        [Tooltip("Recorded brake input value (0-1) for this frame.")]
        public float brakeInput;
        [Tooltip("Recorded steering input value (-1 to 1) for this frame.")]
        public float steerInput;
        [Tooltip("Recorded handbrake input value (0-1) for this frame.")]
        public float handbrakeInput;
        [Tooltip("Recorded clutch input value (0-1) for this frame.")]
        public float clutchInput;
        [Tooltip("Recorded nitrous/boost input value (0-1) for this frame.")]
        public float nosInput;
        [Tooltip("Recorded drive direction: 1 for forward, -1 for reverse.")]
        public int direction;
        [Tooltip("Index of the gear that was active during this recorded frame.")]
        public int currentGear;
        [Tooltip("Gear input multiplier used during this recorded frame.")]
        public float gearInput = 1f;
        [Tooltip("Transmission state (changing, neutral, engaged) during this frame.")]
        public RCCP_Gearbox.CurrentGearState.GearState gearState;
        [Tooltip("Whether neutral gear was engaged during this recorded frame.")]
        public bool NGear;

        [Tooltip("Whether low-beam headlights were on during this recorded frame.")]
        public bool lowBeamHeadLightsOn;
        [Tooltip("Whether high-beam headlights were on during this recorded frame.")]
        public bool highBeamHeadLightsOn;
        [Tooltip("Whether the left turn indicator was active during this recorded frame.")]
        public bool indicatorsLeft;
        [Tooltip("Whether the right turn indicator was active during this recorded frame.")]
        public bool indicatorsRight;
        [Tooltip("Whether hazard lights (all indicators) were active during this recorded frame.")]
        public bool indicatorsAll;

        public VehicleInput(
            float _gasInput,
            float _brakeInput,
            float _steerInput,
            float _handbrakeInput,
            float _clutchInput,
            float _boostInput,
            int _direction,
            int _currentGear,
            RCCP_Gearbox.CurrentGearState.GearState _gearState,
            bool _NGear,
            bool _lowBeamHeadLightsOn,
            bool _highBeamHeadLightsOn,
            bool _indicatorsLeft,
            bool _indicatorsRight,
            bool _indicatorsAll
        ) {

            throttleInput = _gasInput;
            brakeInput = _brakeInput;
            steerInput = _steerInput;
            handbrakeInput = _handbrakeInput;
            clutchInput = _clutchInput;
            nosInput = _boostInput;
            direction = _direction;
            currentGear = _currentGear;
            gearState = _gearState;
            NGear = _NGear;

            lowBeamHeadLightsOn = _lowBeamHeadLightsOn;
            highBeamHeadLightsOn = _highBeamHeadLightsOn;
            indicatorsLeft = _indicatorsLeft;
            indicatorsRight = _indicatorsRight;
            indicatorsAll = _indicatorsAll;

        }

    }

    /// <summary>
    /// Records the vehicle’s position and rotation for a single frame.
    /// </summary>
    [System.Serializable]
    public class VehicleTransform {

        [Tooltip("World-space position of the vehicle at this recorded frame.")]
        public Vector3 position;
        [Tooltip("World-space rotation of the vehicle at this recorded frame.")]
        public Quaternion rotation;

        public VehicleTransform(Vector3 _pos, Quaternion _rot) {

            position = _pos;
            rotation = _rot;

        }

    }

    /// <summary>
    /// Records the vehicle’s velocity and angular velocity for a single frame.
    /// </summary>
    [System.Serializable]
    public class VehicleVelocity {

        [Tooltip("Linear velocity of the vehicle's Rigidbody at this recorded frame.")]
        public Vector3 velocity;
        [Tooltip("Angular velocity of the vehicle's Rigidbody at this recorded frame.")]
        public Vector3 angularVelocity;

        public VehicleVelocity(Vector3 _vel, Vector3 _angVel) {

            velocity = _vel;
            angularVelocity = _angVel;

        }

    }

    /// <summary>
    /// Operational modes for the recorder: neutral, recording, or playing back.
    /// </summary>
    public enum RecorderMode { Neutral, Play, Record }
    [Tooltip("Current operational state of the recorder: Neutral, Recording, or Playing back.")]
    public RecorderMode mode = RecorderMode.Neutral;

    public override void Start() {

        base.Start();

        Inputs = new List<VehicleInput>();
        Transforms = new List<VehicleTransform>();
        Rigidbodies = new List<VehicleVelocity>();

    }

    /// <summary>
    /// Begin or stop recording the current vehicle’s movements and inputs.
    /// </summary>
    public void Record() {

        // Toggle between entering record mode or stopping and saving the record.
        if (mode != RecorderMode.Record) {

            mode = RecorderMode.Record;

        } else {

            mode = RecorderMode.Neutral;
            SaveRecord();

        }

        // If we’re entering record mode, clear existing data from prior recordings.
        if (mode == RecorderMode.Record) {

            Inputs.Clear();
            Transforms.Clear();
            Rigidbodies.Clear();

        }

    }

    /// <summary>
    /// Saves the current recorded data to the 'recorded' struct and appends it to RCCP_Records.
    /// </summary>
    public void SaveRecord() {

        Debug.Log("Record saved!");

        recorded = new RecordedClip(
            Inputs.ToArray(),
            Transforms.ToArray(),
            Rigidbodies.ToArray(),
            RCCP_Records.Instance.records.Count + "_" + CarController.transform.name
        );

        RCCP_Records.Instance.records.Add(recorded);

    }

    /// <summary>
    /// Toggle playback of the last recorded clip or stop if already playing.
    /// </summary>
    public void Play() {

        if (recorded == null)
            return;

        // Toggle between playing and stopping.
        if (mode != RecorderMode.Play)
            mode = RecorderMode.Play;
        else
            mode = RecorderMode.Neutral;

        // If playing, override the vehicle so user input is replaced by recorded data.
        if (mode == RecorderMode.Play) {

            OverrideVehicle(true);
            StartCoroutine(Replay());

            if (recorded.transforms.Length > 0)
                CarController.transform.SetPositionAndRotation(recorded.transforms[0].position, recorded.transforms[0].rotation);

            StartCoroutine(Revel());

        } else {

            OverrideVehicle(false);

        }

    }

    /// <summary>
    /// Plays back a specified clip instead of the last recorded one.
    /// </summary>
    /// <param name="_recorded">The recorded clip to play.</param>
    public void Play(RecordedClip _recorded) {

        recorded = _recorded;

        Debug.Log("Replaying record " + recorded.recordName);

        if (recorded == null)
            return;

        if (mode != RecorderMode.Play)
            mode = RecorderMode.Play;
        else
            mode = RecorderMode.Neutral;

        if (mode == RecorderMode.Play) {

            OverrideVehicle(true);
            StartCoroutine(Replay());

            if (recorded.transforms.Length > 0)
                CarController.transform.SetPositionAndRotation(recorded.transforms[0].position, recorded.transforms[0].rotation);

            StartCoroutine(Revel());

        } else {

            OverrideVehicle(false);

        }

    }

    /// <summary>
    /// Stops playback or recording, returning to neutral mode.
    /// </summary>
    public void Stop() {

        mode = RecorderMode.Neutral;
        OverrideVehicle(false);

    }

    private IEnumerator Replay() {

        for (int i = 0; i < recorded.inputs.Length && mode == RecorderMode.Play; i++) {

            OverrideVehicle(true);

            RCCP_Inputs inputs = new RCCP_Inputs {
                throttleInput = recorded.inputs[i].throttleInput,
                brakeInput = recorded.inputs[i].brakeInput,
                steerInput = recorded.inputs[i].steerInput,
                handbrakeInput = recorded.inputs[i].handbrakeInput,
                clutchInput = recorded.inputs[i].clutchInput,
                nosInput = recorded.inputs[i].nosInput
            };

            if (CarController.Inputs)
                CarController.Inputs.OverrideInputs(inputs);

            if (CarController.Gearbox)
                CarController.Gearbox.OverrideGear(recorded.inputs[i].currentGear, recorded.inputs[i].gearInput, recorded.inputs[i].gearState);

            if (CarController.Lights) {

                CarController.Lights.lowBeamHeadlights = recorded.inputs[i].lowBeamHeadLightsOn;
                CarController.Lights.highBeamHeadlights = recorded.inputs[i].highBeamHeadLightsOn;
                CarController.Lights.indicatorsLeft = recorded.inputs[i].indicatorsLeft;
                CarController.Lights.indicatorsRight = recorded.inputs[i].indicatorsRight;
                CarController.Lights.indicatorsAll = recorded.inputs[i].indicatorsAll;

            }

            yield return new WaitForFixedUpdate();

        }

        mode = RecorderMode.Neutral;
        OverrideVehicle(false);

    }

    /// <summary>
    /// Applies the recorded velocities to the vehicle’s Rigidbody each physics frame.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Revel() {

        for (int i = 0; i < recorded.rigids.Length && mode == RecorderMode.Play; i++) {

            CarController.Rigid.linearVelocity = recorded.rigids[i].velocity;
            CarController.Rigid.angularVelocity = recorded.rigids[i].angularVelocity;

            yield return new WaitForFixedUpdate();

        }

        mode = RecorderMode.Neutral;
        OverrideVehicle(false);

    }

    private void FixedUpdate() {

        switch (mode) {

            case RecorderMode.Neutral:
                break;

            case RecorderMode.Play:
                // Continuously override vehicle while playing.
                OverrideVehicle(true);
                break;

            case RecorderMode.Record:

                // Record player-side inputs (_P). Playback calls Inputs.OverrideInputs() which sets
                // overrideExternalInputs = true, bypassing VehicleControlledInputs() on replay — so the
                // round-trip expects the post-processed player snapshot, not the realized axle state (_V).
                Inputs.Add(new VehicleInput(
                    CarController.throttleInput_P,
                    CarController.brakeInput_P,
                    CarController.steerInput_P,
                    CarController.handbrakeInput_P,
                    CarController.clutchInput_P,
                    CarController.nosInput_P,
                    CarController.direction,
                    CarController.currentGear,
                    CarController.Gearbox.currentGearState.gearState,
                    CarController.NGearNow,
                    CarController.lowBeamLights,
                    CarController.highBeamLights,
                    CarController.indicatorsLeftLights,
                    CarController.indicatorsRightLights,
                    CarController.indicatorsAllLights
                ));

                Transforms.Add(new VehicleTransform(
                    CarController.transform.position,
                    CarController.transform.rotation
                ));

                Rigidbodies.Add(new VehicleVelocity(
                    CarController.Rigid.linearVelocity,
                    CarController.Rigid.angularVelocity
                ));

                break;

        }

    }

    /// <summary>
    /// Overrides the vehicle’s input and gear logic with the record/playback system, disabling player input.
    /// </summary>
    /// <param name="overrideState">True to override with recorded data, false to return to normal control.</param>
    private void OverrideVehicle(bool overrideState) {

        if (CarController.Inputs) {
            CarController.Inputs.overridePlayerInputs = overrideState;
            CarController.Inputs.overrideExternalInputs = overrideState;
        }

        if (CarController.Gearbox)
            CarController.Gearbox.overrideGear = overrideState;

    }

    /// <summary>
    /// Resets the recorder to a neutral state, stopping any record or playback in progress.
    /// </summary>
    public void Reload() {

        mode = RecorderMode.Neutral;

    }

    private void Reset() {

        if (recorded == null)
            recorded = new RecordedClip();

        if (recorded != null && recorded.recordName == "New Record") {

            RCCP_CarController carController = transform.GetComponentInParent<RCCP_CarController>(true);

            if (carController != null)
                recorded.recordName = carController.transform.name;

        }

    }

}

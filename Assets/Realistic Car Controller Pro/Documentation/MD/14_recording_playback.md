# Recording and Playback

RCCP includes a built-in recording and playback system that captures vehicle movement and inputs at the physics framerate. You can record a driving session and replay it exactly, including throttle, brake, steering, gear changes, light states, and rigidbody velocities. Recordings can be saved persistently to a ScriptableObject asset for reuse across play sessions.

**Prerequisites:** The vehicle must have the `RCCP_OtherAddons` component, which provides access to the `RCCP_Recorder`. See [Vehicle Setup](03_vehicle_setup.md) for configuring a complete vehicle.


## How Recording Works

The `RCCP_Recorder` component captures three types of data every `FixedUpdate` frame:

| Data Type | Class | What It Stores |
|---|---|---|
| Vehicle Input | `VehicleInput` | Throttle, brake, steer, handbrake, clutch, NOS, direction, current gear, gear state, neutral gear flag, low beam, high beam, left/right/all indicators |
| Vehicle Transform | `VehicleTransform` | World position and rotation |
| Vehicle Velocity | `VehicleVelocity` | Rigidbody linear velocity and angular velocity |

These three arrays are stored together in a `RecordedClip` object. During playback, the recorder feeds these values back into the vehicle frame by frame, reproducing the original driving session.

### Recorder Modes

The recorder operates in one of three modes:

| Mode | Description |
|---|---|
| **Neutral** | Idle state. No recording or playback in progress. |
| **Record** | Actively capturing input, transform, and velocity data every physics frame. |
| **Play** | Playing back a previously recorded clip. Vehicle input and gearbox are overridden. |


## The RCCP_Recorder Component

`RCCP_Recorder` inherits from `RCCP_Component` and is part of the `RCCP_OtherAddons` system. It is accessed at runtime through `vehicle.OtherAddonsManager.Recorder`.

### Key Fields

| Field | Type | Description |
|---|---|---|
| `recorded` | RecordedClip | The most recently saved or loaded recorded clip |
| `mode` | RecorderMode | Current operational mode (Neutral, Record, or Play) |


## Recording a Session

### Using the Keyboard

RCCP maps recording to the **Optional** action map in the Input System. The default key bindings are:

| Action | Default Key | Description |
|---|---|---|
| Record | **R** | Toggle recording on/off |
| Replay | **P** | Toggle playback of the last recorded clip |

These bindings are defined in the `RCCP_InputActions` asset and can be customized through the Unity Input System settings.

### Using the API

The `RCCP` static class provides convenience methods for recording:

```csharp
// Start recording (call again to stop and save)
RCCP.StartStopRecord(myVehicle);

// Start/stop replay of the last recorded clip
RCCP.StartStopReplay(myVehicle);

// Replay a specific recorded clip
RCCP.StartStopReplay(myVehicle, specificClip);

// Stop both recording and playback
RCCP.StopRecordReplay(myVehicle);
```

### Using the Recorder Directly

You can also call methods on the `RCCP_Recorder` component directly:

```csharp
RCCP_Recorder recorder = myVehicle.OtherAddonsManager.Recorder;

// Toggle recording
recorder.Record();

// Toggle playback of last recorded clip
recorder.Play();

// Play a specific clip
recorder.Play(myClip);

// Stop everything and return to neutral
recorder.Stop();
```


## Playback Details

When playback starts, the recorder:

1. Sets the vehicle to `overridePlayerInputs` and `overrideExternalInputs` so recorded data takes full control
2. Overrides the gearbox so recorded gear changes are replayed
3. Moves the vehicle to the first recorded position and rotation
4. Feeds recorded inputs, velocities, and gear states frame by frame in `WaitForFixedUpdate` coroutines

When playback finishes (all frames consumed) or is stopped manually, the recorder:

1. Returns to Neutral mode
2. Restores normal player input and gearbox control

**Important:** Playback starts from the first recorded position. The vehicle is automatically teleported to that position when playback begins.


## Saving and Loading Recordings

### RCCP_Records ScriptableObject

Recorded clips are automatically saved to the `RCCP_Records` ScriptableObject when recording stops. This asset is located at:

```
Assets/Realistic Car Controller Pro/Resources/RCCP_Records.asset
```

It is loaded via `RCCP_Records.Instance` (a singleton pattern using `Resources.Load`).

| Field | Type | Description |
|---|---|---|
| `records` | List\<RecordedClip\> | All saved recorded clips |

Each `RecordedClip` has a `recordName` field that is auto-generated as `{index}_{vehicleName}` when saved.

### Playing a Saved Recording

```csharp
// Get all saved recordings
List<RCCP_Recorder.RecordedClip> allRecords = RCCP_Records.Instance.records;

// Play the first saved recording
if (allRecords.Count > 0) {
    RCCP.StartStopReplay(myVehicle, allRecords[0]);
}
```

### Note on Persistence

`RCCP_Records` is a ScriptableObject asset. In the Unity Editor, recordings saved during Play Mode will persist to the asset file. In a built game, ScriptableObject data does not persist between sessions unless you implement your own serialization. For runtime persistence in builds, consider serializing the `RecordedClip` data to JSON or binary files.


## Complete Example

```csharp
using UnityEngine;

public class RecordingExample : MonoBehaviour {

    public RCCP_CarController myVehicle;

    void Update() {
        // Press F5 to toggle recording
        if (Input.GetKeyDown(KeyCode.F5)) {
            RCCP.StartStopRecord(myVehicle);
        }

        // Press F6 to toggle replay
        if (Input.GetKeyDown(KeyCode.F6)) {
            RCCP.StartStopReplay(myVehicle);
        }

        // Press F7 to stop everything
        if (Input.GetKeyDown(KeyCode.F7)) {
            RCCP.StopRecordReplay(myVehicle);
        }
    }
}
```


## RecordedClip Data Structure

For advanced users who want to inspect or manipulate recording data:

```csharp
[System.Serializable]
public class RecordedClip {
    public string recordName;
    public VehicleInput[] inputs;       // Per-frame input data
    public VehicleTransform[] transforms; // Per-frame position/rotation
    public VehicleVelocity[] rigids;     // Per-frame velocity data
}
```

### VehicleInput Fields

| Field | Type | Description |
|---|---|---|
| `throttleInput` | float | Throttle amount (0-1) |
| `brakeInput` | float | Brake amount (0-1) |
| `steerInput` | float | Steering amount (-1 to 1) |
| `handbrakeInput` | float | Handbrake amount (0-1) |
| `clutchInput` | float | Clutch amount (0-1) |
| `nosInput` | float | Nitrous input (0-1) |
| `direction` | int | Drive direction (1 = forward, -1 = reverse) |
| `currentGear` | int | Current gear index |
| `gearState` | GearState | Gearbox state enum |
| `NGear` | bool | Whether neutral gear is engaged |
| `lowBeamHeadLightsOn` | bool | Low beam headlight state |
| `highBeamHeadLightsOn` | bool | High beam headlight state |
| `indicatorsLeft` | bool | Left indicator state |
| `indicatorsRight` | bool | Right indicator state |
| `indicatorsAll` | bool | Hazard lights (all indicators) state |

### VehicleTransform Fields

| Field | Type | Description |
|---|---|---|
| `position` | Vector3 | World position of the vehicle |
| `rotation` | Quaternion | World rotation of the vehicle |

### VehicleVelocity Fields

| Field | Type | Description |
|---|---|---|
| `velocity` | Vector3 | Rigidbody linear velocity |
| `angularVelocity` | Vector3 | Rigidbody angular velocity |


## Common Issues

| Problem | Possible Cause | Solution |
|---|---|---|
| Recording not starting | Missing `RCCP_Recorder` component | Ensure the vehicle has `RCCP_OtherAddons` which provides access to the Recorder |
| R key not triggering record | Input action not bound | Check that the **Optional** action map is enabled in `RCCP_InputActions` with the Record action bound to the R key |
| Replay looks jerky | Inconsistent physics timestep | Ensure a consistent `Time.fixedDeltaTime` value. Recording captures at the physics rate, so changes to the timestep during playback can cause uneven motion. |
| Vehicle teleports at replay start | Expected behavior | Playback always starts from the first recorded position. The vehicle is teleported there automatically. |
| Replay drifts from original path | Physics nondeterminism | Small floating-point differences in physics can cause divergence over long replays. The velocity data helps reduce this, but perfect reproduction is not guaranteed over very long clips. |
| Recordings lost after restarting the game (build) | ScriptableObject data does not persist in builds | Implement your own serialization (JSON, binary) to save `RecordedClip` data to disk in builds |
| Cannot access Recorder | Null reference on `OtherAddonsManager` | Add the `RCCP_OtherAddons` component to the vehicle. The Recorder is accessed through `vehicle.OtherAddonsManager.Recorder`. |

---

**Related topics:** [Vehicle Setup](03_vehicle_setup.md) | [Inputs](05_inputs.md) | [AI Vehicles](13_ai_vehicles.md) | [API Reference](16_api_reference.md)

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

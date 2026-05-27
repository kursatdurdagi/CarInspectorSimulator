# API Reference

This document covers every public method in the `RCCP` static class and every event in `RCCP_Events`. These are the two primary entry points you will use when integrating Realistic Car Controller Pro into your own game scripts.

---

## Overview

`RCCP` is a **static utility class**. You do not need to create an instance or attach it to a GameObject. Call any method directly:

```csharp
RCCP.MethodName();
```

Every script that calls the RCCP API should include the following at the top:

```csharp
using UnityEngine;
```

No additional `using` statements are required because RCCP uses the global namespace.

**Key points:**

- All methods are `public static` -- call them from any MonoBehaviour.
- Methods that target a specific vehicle take an `RCCP_CarController` parameter.
- Methods that target the current player vehicle operate through `RCCP_SceneManager.Instance`.
- Vehicle prefabs must have an `RCCP_CarController` component to work with this API.

---

## Vehicle Spawning and Registration

These methods handle instantiating vehicle prefabs at runtime and telling the system which vehicle the player is currently driving.

### SpawnRCC

Instantiates a vehicle prefab, activates it, and optionally registers it as the player vehicle.

```csharp
public static RCCP_CarController SpawnRCC(
    RCCP_CarController vehiclePrefab,
    Vector3 position,
    Quaternion rotation,
    bool registerAsPlayerVehicle,
    bool isControllable,
    bool isEngineRunning
)
```

| Parameter | Type | Description |
|---|---|---|
| `vehiclePrefab` | `RCCP_CarController` | The vehicle prefab to instantiate. Must have an RCCP_CarController component. |
| `position` | `Vector3` | World position where the vehicle will spawn. |
| `rotation` | `Quaternion` | World rotation applied to the spawned vehicle. |
| `registerAsPlayerVehicle` | `bool` | If `true`, the spawned vehicle becomes the active player vehicle immediately. |
| `isControllable` | `bool` | If `true`, the player can control this vehicle right away. |
| `isEngineRunning` | `bool` | If `true`, the engine starts running immediately. If `false`, the engine is killed. |

**Returns:** The spawned `RCCP_CarController` instance.

```csharp
// Spawn a car at the origin, register as player, controllable, engine on
public RCCP_CarController carPrefab;

void Start() {
    RCCP_CarController spawnedCar = RCCP.SpawnRCC(
        carPrefab,
        Vector3.zero,
        Quaternion.identity,
        true,   // register as player vehicle
        true,   // player can control it
        true    // engine starts running
    );
}
```

### RegisterPlayerVehicle

Registers an existing vehicle as the active player vehicle. The RCCP camera and UI will follow this vehicle. There are three overloads.

**Overload 1 -- Vehicle only:**

```csharp
public static void RegisterPlayerVehicle(RCCP_CarController vehicle)
```

Registers the vehicle with its current controllable and engine states unchanged.

```csharp
// Register an already-spawned vehicle as the player vehicle
RCCP.RegisterPlayerVehicle(myVehicle);
```

**Overload 2 -- Vehicle with controllable state:**

```csharp
public static void RegisterPlayerVehicle(RCCP_CarController vehicle, bool isControllable)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | The vehicle to register. |
| `isControllable` | `bool` | If `true`, enables player control. If `false`, disables it. |

```csharp
// Register vehicle and enable player control
RCCP.RegisterPlayerVehicle(myVehicle, true);
```

**Overload 3 -- Vehicle with controllable and engine state:**

```csharp
public static void RegisterPlayerVehicle(RCCP_CarController vehicle, bool isControllable, bool engineState)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | The vehicle to register. |
| `isControllable` | `bool` | If `true`, enables player control. |
| `engineState` | `bool` | If `true`, starts the engine. If `false`, kills it. |

```csharp
// Register vehicle, enable control, and start the engine
RCCP.RegisterPlayerVehicle(myVehicle, true, true);
```

### DeRegisterPlayerVehicle

Removes the current player vehicle registration. The camera and UI will no longer follow any vehicle.

```csharp
public static void DeRegisterPlayerVehicle()
```

Takes no parameters.

```csharp
// Player exits the car -- deregister so camera stops following
RCCP.DeRegisterPlayerVehicle();
```

---

## Vehicle Control

These methods control whether a vehicle accepts player input and whether its engine is running.

### SetControl

Enables or disables player control of a vehicle. When disabled, player inputs are zeroed and brakes are applied automatically.

```csharp
public static void SetControl(RCCP_CarController vehicle, bool isControllable)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle. |
| `isControllable` | `bool` | `true` to enable player input, `false` to disable it. |

```csharp
// Disable player control during a cutscene
RCCP.SetControl(playerCar, false);

// Re-enable after the cutscene
RCCP.SetControl(playerCar, true);
```

### SetExternalControl

Marks a vehicle as being driven by an external controller (AI, network, replay). When `true`, the vehicle's input component ignores the player input manager and instead uses override inputs.

```csharp
public static void SetExternalControl(RCCP_CarController vehicle, bool isExternal)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle. |
| `isExternal` | `bool` | `true` to mark as externally controlled, `false` to return to player control. |

```csharp
// Hand control to an AI driver
RCCP.SetExternalControl(aiCar, true);

// Return to player control
RCCP.SetExternalControl(aiCar, false);
```

**Note:** This method performs a null check. If `vehicle` is null, it returns silently.

### SetEngine

Starts or stops the vehicle engine.

```csharp
public static void SetEngine(RCCP_CarController vehicle, bool engineState)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle. |
| `engineState` | `bool` | `true` to start the engine, `false` to kill it. |

```csharp
// Start the engine
RCCP.SetEngine(playerCar, true);

// Kill the engine (e.g., player parked the car)
RCCP.SetEngine(playerCar, false);
```

### SetAutomaticGear

Sets the vehicle's transmission type. There are two overloads.

**Overload 1 -- Boolean (Automatic or Manual):**

```csharp
public static void SetAutomaticGear(RCCP_CarController vehicle, bool state)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle. |
| `state` | `bool` | `true` = Automatic, `false` = Manual. |

```csharp
// Switch to automatic transmission
RCCP.SetAutomaticGear(playerCar, true);
```

**Overload 2 -- TransmissionType enum:**

```csharp
public static void SetAutomaticGear(RCCP_CarController vehicle, RCCP_Gearbox.TransmissionType transmissionType)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle. |
| `transmissionType` | `RCCP_Gearbox.TransmissionType` | One of: `Manual`, `Automatic`, `Automatic_DNRP`. |

The `Automatic_DNRP` option adds a Drive/Neutral/Reverse/Park selector, similar to real automatic cars.

```csharp
// Switch to DNRP automatic mode
RCCP.SetAutomaticGear(playerCar, RCCP_Gearbox.TransmissionType.Automatic_DNRP);
```

**Note:** Both overloads check whether the vehicle has a Gearbox component. If it does not, the call is ignored.

---

## Camera

### ChangeCamera

Cycles through the available camera modes on the RCCP camera (TPS, FPS, Top-Down, etc.).

```csharp
public static void ChangeCamera()
```

Takes no parameters. Operates through `RCCP_SceneManager.Instance`.

```csharp
// Bind to a UI button to let the player cycle camera views
public void OnCameraButtonPressed() {
    RCCP.ChangeCamera();
}
```

For more details on available camera modes, see [Camera System](09_camera_system.md).

---

## Transport

Teleports a vehicle to a new position and rotation. Useful for respawning after going off-track or for checkpoint systems.

### Transport (Player Vehicle)

Transports the currently registered player vehicle.

```csharp
public static void Transport(Vector3 position, Quaternion rotation)
```

| Parameter | Type | Description |
|---|---|---|
| `position` | `Vector3` | World position to move the vehicle to. |
| `rotation` | `Quaternion` | World rotation to apply. |

```csharp
// Respawn the player car at the last checkpoint
RCCP.Transport(checkpointPosition, checkpointRotation);
```

### Transport (Specific Vehicle)

Transports a specific vehicle.

```csharp
public static void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | The vehicle to transport. |
| `position` | `Vector3` | World position to move the vehicle to. |
| `rotation` | `Quaternion` | World rotation to apply. |

```csharp
// Transport a specific AI vehicle to a new position
RCCP.Transport(aiCar, spawnPoint.position, spawnPoint.rotation);
```

### Transport (With Velocity Reset)

Transports a specific vehicle and optionally resets its velocity so it does not continue sliding after teleport.

```csharp
public static void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation, bool resetVelocity)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | The vehicle to transport. |
| `position` | `Vector3` | World position to move the vehicle to. |
| `rotation` | `Quaternion` | World rotation to apply. |
| `resetVelocity` | `bool` | If `true`, resets the Rigidbody velocity after transport. |

```csharp
// Transport and stop the car completely
RCCP.Transport(playerCar, respawnPos, respawnRot, true);
```

---

## Behavior

Behavior presets let you change how all vehicles handle at runtime (e.g., switching between "Sim" and "Arcade" modes). Presets are configured in the RCCP Settings asset. See [Settings](04_settings.md) for how to create behavior presets.

### SetBehavior (By Index)

Applies a behavior preset by its index in the `RCCP_Settings.behaviorTypes` array.

```csharp
public static void SetBehavior(int behaviorIndex)
```

| Parameter | Type | Description |
|---|---|---|
| `behaviorIndex` | `int` | Zero-based index into the behavior presets array in RCCP Settings. |

```csharp
// Apply the first behavior preset
RCCP.SetBehavior(0);
```

### SetBehavior (By Name)

Applies a behavior preset by its name. The name comparison is case-insensitive.

```csharp
public static void SetBehavior(string behaviorName)
```

| Parameter | Type | Description |
|---|---|---|
| `behaviorName` | `string` | Name of the behavior preset. Case-insensitive. |

```csharp
// Switch to the "Drift" behavior preset
RCCP.SetBehavior("Drift");
```

If no preset with the given name is found, a warning is logged to the console and no change is made.

### GetBehaviorIndexByName

Returns the index of a behavior preset by its name, or `-1` if not found.

```csharp
public static int GetBehaviorIndexByName(string behaviorName)
```

| Parameter | Type | Description |
|---|---|---|
| `behaviorName` | `string` | Name of the behavior preset. Case-insensitive. |

**Returns:** The zero-based index of the preset, or `-1` if no match is found.

```csharp
int driftIndex = RCCP.GetBehaviorIndexByName("Drift");

if (driftIndex >= 0)
    Debug.Log("Drift preset is at index " + driftIndex);
else
    Debug.Log("Drift preset not found!");
```

### GetBehaviorByName

Returns the full `BehaviorType` object for a preset, or `null` if not found.

```csharp
public static RCCP_Settings.BehaviorType GetBehaviorByName(string behaviorName)
```

| Parameter | Type | Description |
|---|---|---|
| `behaviorName` | `string` | Name of the behavior preset. Case-insensitive. |

**Returns:** The matching `RCCP_Settings.BehaviorType` object, or `null`.

```csharp
RCCP_Settings.BehaviorType simBehavior = RCCP.GetBehaviorByName("Sim");

if (simBehavior != null)
    Debug.Log("Found behavior: " + simBehavior.behaviorName);
```

---

## Mobile Input

### SetMobileController

Changes the active mobile input method at runtime. Use this to let players switch between touch controls, gyroscope, steering wheel UI, or joystick.

```csharp
public static void SetMobileController(RCCP_Settings.MobileController mobileController)
```

| Parameter | Type | Description |
|---|---|---|
| `mobileController` | `RCCP_Settings.MobileController` | One of: `TouchScreen`, `Gyro`, `SteeringWheel`, `Joystick`. |

```csharp
// Switch to gyroscope controls
RCCP.SetMobileController(RCCP_Settings.MobileController.Gyro);

// Switch to on-screen joystick
RCCP.SetMobileController(RCCP_Settings.MobileController.Joystick);
```

For more details on mobile controls, see [Mobile](07_mobile.md).

---

## Recording

These methods control the vehicle recording and replay system. The vehicle must have an `RCCP_Recorder` component attached through the OtherAddonsManager. See [Recording and Playback](14_recording_playback.md) for setup instructions.

All recording methods perform null checks on the OtherAddonsManager and Recorder components. If either is missing, the call is silently ignored.

### StartStopRecord

Toggles recording on or off for the specified vehicle. Call once to start recording, call again to stop.

```csharp
public static void StartStopRecord(RCCP_CarController vehicle)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle to record. |

```csharp
// Toggle recording with a button press
public void OnRecordButton() {
    RCCP.StartStopRecord(playerCar);
}
```

### StartStopReplay (Last Recording)

Toggles replay of the most recent recording. Call once to start replay, call again to stop.

```csharp
public static void StartStopReplay(RCCP_CarController vehicle)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle to replay. |

```csharp
// Replay the last recorded run
RCCP.StartStopReplay(playerCar);
```

### StartStopReplay (Specific Recording)

Toggles replay of a specific recorded clip.

```csharp
public static void StartStopReplay(RCCP_CarController vehicle, RCCP_Recorder.RecordedClip recordedClip)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle to replay. |
| `recordedClip` | `RCCP_Recorder.RecordedClip` | The specific recorded clip to play back. |

```csharp
// Play a specific recorded ghost lap
RCCP.StartStopReplay(ghostCar, savedGhostClip);
```

### StopRecordReplay

Stops any active recording or replay on the vehicle.

```csharp
public static void StopRecordReplay(RCCP_CarController vehicle)
```

| Parameter | Type | Description |
|---|---|---|
| `vehicle` | `RCCP_CarController` | Target vehicle to stop recording or replaying. |

```csharp
// Force stop all recording/replay
RCCP.StopRecordReplay(playerCar);
```

---

## Maintenance

### Repair (Specific Vehicle)

Triggers a repair on the specified vehicle, restoring all damage.

```csharp
public static void Repair(RCCP_CarController carController)
```

| Parameter | Type | Description |
|---|---|---|
| `carController` | `RCCP_CarController` | The vehicle to repair. |

```csharp
// Repair a specific vehicle
RCCP.Repair(playerCar);
```

### Repair (Player Vehicle)

Repairs the currently registered player vehicle. If no player vehicle is registered, this method does nothing.

```csharp
public static void Repair()
```

Takes no parameters.

```csharp
// Repair the current player car (e.g., from a UI button)
RCCP.Repair();
```

### CleanSkidmarks (All)

Removes all skidmarks from the current scene.

```csharp
public static void CleanSkidmarks()
```

Takes no parameters.

```csharp
// Clean up all tire marks (e.g., on scene reset)
RCCP.CleanSkidmarks();
```

### CleanSkidmarks (By Index)

Removes a specific set of skidmarks by index.

```csharp
public static void CleanSkidmarks(int index)
```

| Parameter | Type | Description |
|---|---|---|
| `index` | `int` | Index of the skidmark set to clean. |

```csharp
// Clean skidmark set 0 only
RCCP.CleanSkidmarks(0);
```

---

## Events Reference

`RCCP_Events` is a static class containing C# events you can subscribe to. These events fire automatically when the corresponding actions occur in RCCP. Use them to react to vehicle spawns, collisions, camera changes, and more.

### How to Subscribe and Unsubscribe

Always subscribe in `OnEnable()` and unsubscribe in `OnDisable()` to prevent memory leaks and errors from destroyed objects.

```csharp
using UnityEngine;

public class MyGameManager : MonoBehaviour {

    void OnEnable() {
        // Subscribe to events
        RCCP_Events.OnRCCPSpawned += OnVehicleSpawned;
        RCCP_Events.OnRCCPCollision += OnVehicleCollision;
        RCCP_Events.OnVehicleChanged += OnPlayerVehicleChanged;
    }

    void OnDisable() {
        // Unsubscribe from events
        RCCP_Events.OnRCCPSpawned -= OnVehicleSpawned;
        RCCP_Events.OnRCCPCollision -= OnVehicleCollision;
        RCCP_Events.OnVehicleChanged -= OnPlayerVehicleChanged;
    }

    void OnVehicleSpawned(RCCP_CarController vehicle) {
        Debug.Log("Vehicle spawned: " + vehicle.name);
    }

    void OnVehicleCollision(RCCP_CarController vehicle, Collision collision) {
        Debug.Log(vehicle.name + " collided with " + collision.gameObject.name);
    }

    void OnPlayerVehicleChanged() {
        Debug.Log("Player switched to a different vehicle.");
    }
}
```

### Events Table

| Event | Delegate Signature | When It Fires |
|---|---|---|
| `OnRCCPSpawned` | `onRCCPSpawned(RCCP_CarController rccp)` | A player vehicle spawns or becomes enabled. |
| `OnRCCPDestroyed` | `onRCCPDestroyed(RCCP_CarController rccp)` | A player vehicle is destroyed or disabled. |
| `OnRCCPAISpawned` | `onRCCPAISpawned(RCCP_CarController rccp)` | A vehicle with an AI component spawns or becomes enabled. |
| `OnRCCPAIDestroyed` | `onRCCPAIDestroyed(RCCP_CarController rccp)` | A vehicle with an AI component is destroyed or disabled. |
| `OnRCCPCollision` | `onRCCPCollision(RCCP_CarController rccp, Collision collision)` | Any RCCP vehicle collides with another object. |
| `OnRCCPCameraSpawned` | `onRCCPCameraSpawned(RCCP_Camera cam)` | The RCCP camera is spawned or initialized. |
| `OnRCCPUISpawned` | `onRCCPUISpawned(RCCP_UIManager UI)` | The RCCP UI canvas is spawned or initialized. |
| `OnRCCPUIDestroyed` | `onRCCPUIDestroyed(RCCP_UIManager UI)` | The RCCP UI canvas is destroyed or disabled. |
| `OnRCCPUIInformer` | `onRCCPUIInformer(string text)` | A UI informer message should be displayed to the player. |
| `OnBehaviorChanged` | `onBehaviorChanged()` | The global behavior preset is changed via `RCCP.SetBehavior()`. |
| `OnVehicleChanged` | `onVehicleChanged()` | The registered player vehicle changes (no vehicle reference). |
| `OnVehicleChangedToVehicle` | `onVehicleChangedToVehicle(RCCP_CarController carController)` | The registered player vehicle changes (includes the new vehicle reference). |

### Event Details

**OnRCCPSpawned / OnRCCPDestroyed** -- These fire for player vehicles (vehicles without an AI component). If a vehicle has an AI component attached, `OnRCCPAISpawned` and `OnRCCPAIDestroyed` fire instead.

**OnRCCPCollision** -- Provides both the RCCP vehicle and the Unity `Collision` object, so you can inspect contact points, impulse, and the other collider.

**OnVehicleChanged vs OnVehicleChangedToVehicle** -- Both fire when the player vehicle changes. `OnVehicleChanged` takes no parameters (useful when you just need to refresh UI). `OnVehicleChangedToVehicle` passes the new vehicle reference (useful when you need to read data from the new vehicle).

**OnRCCPUIInformer** -- Subscribe to this event to display informational messages in your own custom UI. RCCP fires this when it wants to show a message to the player (e.g., "Engine Started").

---

## RCCP_CarController Key Properties

Once you have a reference to an `RCCP_CarController`, you can read these public fields and properties to build HUD displays, telemetry systems, or game logic.

### Vehicle State

| Property | Type | Description |
|---|---|---|
| `speed` | `float` | Current speed in km/h. Signed: positive = forward, negative = reverse. |
| `absoluteSpeed` | `float` | Same as `speed` but always positive (unsigned). Use this for speedometer displays. |
| `maximumSpeed` | `float` | Calculated maximum speed based on engine RPM, gear ratios, differential ratio, and wheel diameter. |
| `direction` | `int` | `1` = forward, `-1` = reverse. |
| `engineRunning` | `bool` | `true` if the engine is currently running. |
| `engineStarting` | `bool` | `true` during the engine start animation/delay. |
| `IsGrounded` | `bool` | `true` if at least one wheel is touching the ground. |

### Engine and Drivetrain

| Property | Type | Description |
|---|---|---|
| `engineRPM` | `float` | Current engine RPM. |
| `minEngineRPM` | `float` | Idle RPM (engine will not drop below this). |
| `maxEngineRPM` | `float` | Redline RPM (engine will not exceed this). |
| `currentGear` | `int` | Current gear index. `0` = first gear. |
| `currentGearRatio` | `float` | Gear ratio of the current gear. |
| `differentialRatio` | `float` | Differential ratio. |
| `shiftingNow` | `bool` | `true` while the gearbox is in the middle of a gear change. |
| `NGearNow` | `bool` | `true` if the gearbox is currently in Neutral. |
| `reversingNow` | `bool` | `true` if the vehicle is currently in a reverse gear. |
| `producedEngineTorque` | `float` | Torque currently produced by the engine. |
| `producedGearboxTorque` | `float` | Torque output from the gearbox. |
| `producedDifferentialTorque` | `float` | Torque output from the differential. |
| `steerAngle` | `float` | Current steering angle in degrees. |

### Vehicle-Side Inputs (Read from Components)

These values reflect what the vehicle's subsystems are actually applying. Use these for accurate HUD gauges.

| Property | Type | Range | Description |
|---|---|---|---|
| `throttleInput_V` | `float` | 0 to 1 | Throttle being applied to the engine. |
| `brakeInput_V` | `float` | 0 to 1 | Brake force being applied. |
| `steerInput_V` | `float` | -1 to 1 | Steering input (-1 = full left, 1 = full right). |
| `handbrakeInput_V` | `float` | 0 to 1 | Handbrake force being applied. |
| `clutchInput_V` | `float` | 0 to 1 | Clutch engagement. |
| `nosInput_V` | `float` | 0 to 1 | Nitrous oxide input. |
| `fuelInput_V` | `float` | 0 to 1 | Fuel flow to the engine. |
| `gearInput_V` | `float` | -- | Gear shift input value. |

### Player-Side Inputs (Raw from Input Manager)

These values reflect what the player is pressing before any processing by vehicle components.

| Property | Type | Range | Description |
|---|---|---|---|
| `throttleInput_P` | `float` | 0 to 1 | Raw throttle input from the player. |
| `brakeInput_P` | `float` | 0 to 1 | Raw brake input from the player. |
| `steerInput_P` | `float` | -1 to 1 | Raw steering input from the player. |
| `handbrakeInput_P` | `float` | 0 to 1 | Raw handbrake input from the player. |
| `clutchInput_P` | `float` | 0 to 1 | Raw clutch input from the player. |
| `nosInput_P` | `float` | 0 to 1 | Raw nitrous input from the player. |

### Control Flags

| Property | Type | Description |
|---|---|---|
| `canControl` | `bool` | When `true`, the vehicle accepts player input. When `false`, inputs are zeroed and brakes apply. |
| `externalControl` | `bool` | When `true`, the vehicle is driven externally (AI, network, replay). Player input is ignored. |
| `ineffectiveBehavior` | `bool` | When `true`, global behavior presets are not applied to this vehicle. |
| `useCustomBehavior` | `bool` | When `true`, this vehicle uses its own behavior preset (`customBehaviorIndex`) instead of the global one. |
| `customBehaviorIndex` | `int` | Index into `RCCP_Settings.behaviorTypes` to use when `useCustomBehavior` is `true`. Use `RCCP.GetBehaviorIndexByName()` to resolve a name to an index. |

### Lights

| Property | Type | Description |
|---|---|---|
| `lowBeamLights` | `bool` | Low beam headlights on/off. |
| `highBeamLights` | `bool` | High beam headlights on/off. |
| `indicatorsLeftLights` | `bool` | Left indicator on/off. |
| `indicatorsRightLights` | `bool` | Right indicator on/off. |
| `indicatorsAllLights` | `bool` | Hazard lights (all indicators) on/off. |

### Other

| Property | Type | Description |
|---|---|---|
| `ConnectedTrailer` | `RCCP_TrailerController` | The currently attached trailer, or `null` if no trailer is connected. |
| `wheelRPM2Speed` | `float` | Vehicle speed calculated from wheel RPM. |
| `tractionWheelRPM2EngineRPM` | `float` | Engine RPM calculated from traction wheel RPM. |

### GetVehicleBehaviorType (V2.31+)

Returns the behavior preset that is actually active on this vehicle, honoring per-vehicle overrides. **Always use this when reading `BehaviorType` fields per-frame** — `RCCP_Settings.SelectedBehaviorType` only returns the global preset and does not respect `useCustomBehavior`.

```csharp
public RCCP_Settings.BehaviorType GetVehicleBehaviorType()
```

**Returns:** The active `BehaviorType` for this vehicle. If `useCustomBehavior` is `true` and `customBehaviorIndex` is valid, returns that custom preset; otherwise returns the global preset (which may be `null` if the index is invalid or the array is empty — every caller must null-check).

```csharp
// Read the active drift mode for this vehicle (custom OR global)
RCCP_Settings.BehaviorType behavior = playerCar.GetVehicleBehaviorType();

if (behavior != null && behavior.driftMode) {
    Debug.Log("This vehicle is in drift mode.");
}
```

---

## Practical Code Examples

### Example 1: Spawning a Vehicle and Registering as Player

This example spawns a vehicle from a prefab, registers it as the player vehicle, starts the engine, and enables control.

```csharp
using UnityEngine;

public class VehicleSpawner : MonoBehaviour {

    // Assign your vehicle prefab in the Inspector
    public RCCP_CarController vehiclePrefab;
    public Transform spawnPoint;

    void Start() {

        // Spawn the vehicle at the spawn point
        RCCP_CarController car = RCCP.SpawnRCC(
            vehiclePrefab,
            spawnPoint.position,
            spawnPoint.rotation,
            true,   // register as player vehicle
            true,   // enable player control
            true    // start engine
        );

        Debug.Log("Spawned vehicle: " + car.name);

    }
}
```

### Example 2: Switching Between Multiple Vehicles

This example lets the player cycle through a list of pre-spawned vehicles by pressing a key.

```csharp
using UnityEngine;

public class VehicleSwitcher : MonoBehaviour {

    // Assign your pre-spawned vehicles in the Inspector
    public RCCP_CarController[] vehicles;
    private int currentIndex = 0;

    void Start() {

        // Register the first vehicle as the player vehicle
        if (vehicles.Length > 0) {
            RCCP.RegisterPlayerVehicle(vehicles[0], true, true);
        }

    }

    void Update() {

        // Press V to switch to the next vehicle
        if (Input.GetKeyDown(KeyCode.V) && vehicles.Length > 1) {

            // Disable control on the current vehicle
            RCCP.SetControl(vehicles[currentIndex], false);
            RCCP.SetEngine(vehicles[currentIndex], false);

            // Move to the next vehicle
            currentIndex = (currentIndex + 1) % vehicles.Length;

            // Register the new vehicle as the player vehicle
            RCCP.RegisterPlayerVehicle(vehicles[currentIndex], true, true);

            Debug.Log("Switched to: " + vehicles[currentIndex].name);

        }

    }
}
```

### Example 3: Reading Vehicle Speed and RPM for a Custom HUD

This example reads real-time data from the player vehicle and displays it using Unity UI.

```csharp
using UnityEngine;
using UnityEngine.UI;

public class CustomHUD : MonoBehaviour {

    public Text speedText;
    public Text rpmText;
    public Text gearText;
    public Slider rpmBar;

    void Update() {

        // Get the active player vehicle from the scene manager
        RCCP_CarController car = RCCP_SceneManager.Instance.activePlayerVehicle;

        // If no player vehicle is registered, hide the HUD
        if (car == null) {
            speedText.text = "---";
            rpmText.text = "---";
            gearText.text = "-";
            return;
        }

        // Display speed (always positive for the speedometer)
        speedText.text = car.absoluteSpeed.ToString("F0") + " km/h";

        // Display engine RPM
        rpmText.text = car.engineRPM.ToString("F0") + " RPM";

        // Display current gear (add 1 for human-readable numbering)
        if (car.NGearNow)
            gearText.text = "N";
        else if (car.reversingNow)
            gearText.text = "R";
        else
            gearText.text = (car.currentGear + 1).ToString();

        // RPM bar normalized between min and max RPM
        if (rpmBar != null) {
            float normalizedRPM = Mathf.InverseLerp(car.minEngineRPM, car.maxEngineRPM, car.engineRPM);
            rpmBar.value = normalizedRPM;
        }

    }
}
```

### Example 4: Listening to Events

This example demonstrates subscribing to multiple RCCP events to build a game manager.

```csharp
using UnityEngine;

public class RaceManager : MonoBehaviour {

    void OnEnable() {
        RCCP_Events.OnRCCPSpawned += HandleVehicleSpawned;
        RCCP_Events.OnRCCPCollision += HandleCollision;
        RCCP_Events.OnBehaviorChanged += HandleBehaviorChanged;
        RCCP_Events.OnVehicleChangedToVehicle += HandleVehicleSwitch;
    }

    void OnDisable() {
        RCCP_Events.OnRCCPSpawned -= HandleVehicleSpawned;
        RCCP_Events.OnRCCPCollision -= HandleCollision;
        RCCP_Events.OnBehaviorChanged -= HandleBehaviorChanged;
        RCCP_Events.OnVehicleChangedToVehicle -= HandleVehicleSwitch;
    }

    void HandleVehicleSpawned(RCCP_CarController vehicle) {
        Debug.Log("New vehicle entered the race: " + vehicle.name);
    }

    void HandleCollision(RCCP_CarController vehicle, Collision collision) {
        // Apply score penalty for wall hits
        if (collision.gameObject.CompareTag("Wall")) {
            Debug.Log(vehicle.name + " hit a wall!");
        }
    }

    void HandleBehaviorChanged() {
        Debug.Log("Driving mode changed.");
    }

    void HandleVehicleSwitch(RCCP_CarController newVehicle) {
        Debug.Log("Player now driving: " + newVehicle.name);
    }
}
```

---

## Quick Reference Table

A summary of every method in the `RCCP` class for quick lookup.

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `SpawnRCC` | prefab, position, rotation, register, control, engine | `RCCP_CarController` | Spawn a vehicle prefab. |
| `RegisterPlayerVehicle` | vehicle | `void` | Register as player vehicle. |
| `RegisterPlayerVehicle` | vehicle, isControllable | `void` | Register with control state. |
| `RegisterPlayerVehicle` | vehicle, isControllable, engineState | `void` | Register with control and engine state. |
| `DeRegisterPlayerVehicle` | (none) | `void` | Remove player vehicle registration. |
| `SetControl` | vehicle, isControllable | `void` | Enable/disable player input. |
| `SetExternalControl` | vehicle, isExternal | `void` | Mark as AI/network controlled. |
| `SetEngine` | vehicle, engineState | `void` | Start or stop the engine. |
| `SetAutomaticGear` | vehicle, state (bool) | `void` | Toggle automatic/manual transmission. |
| `SetAutomaticGear` | vehicle, transmissionType | `void` | Set specific transmission type. |
| `ChangeCamera` | (none) | `void` | Cycle camera modes. |
| `Transport` | position, rotation | `void` | Teleport the player vehicle. |
| `Transport` | vehicle, position, rotation | `void` | Teleport a specific vehicle. |
| `Transport` | vehicle, position, rotation, resetVelocity | `void` | Teleport with optional velocity reset. |
| `SetBehavior` | behaviorIndex (int) | `void` | Apply behavior preset by index. |
| `SetBehavior` | behaviorName (string) | `void` | Apply behavior preset by name. |
| `GetBehaviorIndexByName` | behaviorName | `int` | Get preset index (-1 if not found). |
| `GetBehaviorByName` | behaviorName | `BehaviorType` | Get preset object (null if not found). |
| `SetMobileController` | mobileController | `void` | Change mobile input method. |
| `StartStopRecord` | vehicle | `void` | Toggle recording. |
| `StartStopReplay` | vehicle | `void` | Toggle replay of last recording. |
| `StartStopReplay` | vehicle, recordedClip | `void` | Toggle replay of specific clip. |
| `StopRecordReplay` | vehicle | `void` | Stop all recording/replay. |
| `Repair` | carController | `void` | Repair a specific vehicle. |
| `Repair` | (none) | `void` | Repair the player vehicle. |
| `CleanSkidmarks` | (none) | `void` | Remove all skidmarks. |
| `CleanSkidmarks` | index (int) | `void` | Remove skidmarks by index. |

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

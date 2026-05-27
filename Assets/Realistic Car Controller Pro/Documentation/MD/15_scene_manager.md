# Scene Manager

The **RCCP_SceneManager** is a singleton component that acts as the central hub for all RCCP vehicles at runtime. It tracks every vehicle in the scene, manages the active player vehicle, connects the camera and UI automatically, caches terrain data for surface detection, and applies global runtime settings such as fixed timestep and frame rate overrides.

You do not need to interact with RCCP_SceneManager directly in most cases. The static helper class `RCCP` wraps its methods so you can call `RCCP.RegisterPlayerVehicle()`, `RCCP.Transport()`, and other convenience methods from anywhere in your code.

---

## Adding the Scene Manager to Your Scene

**Menu path:** Tools > BoneCracker Games > Realistic Car Controller Pro > Create > Scene Managers > Add Scene Manager

If you forget to add it manually, RCCP_SceneManager will auto-create itself at runtime the first time any RCCP system accesses `RCCP_SceneManager.Instance`. This is because it inherits from `RCCP_Singleton<T>`, which creates a new GameObject automatically when the singleton is first requested.

That said, it is good practice to place the Scene Manager in your scene ahead of time so you can configure its Inspector properties (such as `registerLastVehicleAsPlayer`) before entering Play mode.

---

## Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `activePlayerVehicle` | `RCCP_CarController` | null | The vehicle currently receiving player input. Set via registration methods. |
| `activePlayerCamera` | `RCCP_Camera` | null | The RCCP camera following the player vehicle. Auto-assigned when an RCCP camera spawns. |
| `activePlayerCanvas` | `RCCP_UIManager` | null | The UI manager displaying dashboard gauges, buttons, etc. Auto-assigned when the UI spawns. |
| `activeMainCamera` | `Camera` | null | Unity's `Camera.main` reference, updated every frame. |
| `allVehicles` | `List<RCCP_CarController>` | empty | Every RCCP vehicle in the scene, including AI vehicles. Vehicles are added on spawn and removed on destroy. |
| `registerLastVehicleAsPlayer` | `bool` | true | When enabled, any newly spawned RCCP vehicle is automatically registered as the player vehicle. |
| `disableUIWhenNoPlayerVehicle` | `bool` | false | When enabled, the UI canvas is hidden whenever there is no active player vehicle. |

---

## Vehicle Registration

Registering a vehicle tells the Scene Manager "this is the player's car." The camera and UI automatically switch to the registered vehicle.

### Registering a Vehicle

Use the static `RCCP` class from any script:

```csharp
// Register a vehicle as the player vehicle
RCCP.RegisterPlayerVehicle(myVehicle);

// Register with controllable state (true = player can drive it)
RCCP.RegisterPlayerVehicle(myVehicle, true);

// Register with controllable state and engine state
RCCP.RegisterPlayerVehicle(myVehicle, true, true);
```

| Overload | Parameters | Behavior |
|---|---|---|
| `RegisterPlayerVehicle(vehicle)` | Vehicle only | Sets the vehicle as active. Camera follows it. |
| `RegisterPlayerVehicle(vehicle, isControllable)` | Vehicle + controllable flag | Same as above, plus enables or disables player input on the vehicle. |
| `RegisterPlayerVehicle(vehicle, isControllable, engineState)` | Vehicle + controllable + engine | Same as above, plus starts or stops the engine. |

When a vehicle is registered, the following happens automatically:

1. `activePlayerVehicle` is set to the new vehicle.
2. If an RCCP camera exists, it calls `SetTarget()` to follow the new vehicle.
3. On the next frame, the `OnVehicleChanged` and `OnVehicleChangedToVehicle` events fire (see [API Reference](16_api_reference.md) for event details).

### De-Registering a Vehicle

```csharp
RCCP.DeRegisterPlayerVehicle();
```

This disables player control on the current vehicle, sets `activePlayerVehicle` to null, and tells the camera to remove its target.

### Automatic Registration

If `registerLastVehicleAsPlayer` is enabled (the default), every vehicle that spawns via `RCCP.SpawnRCC()` or is already in the scene will be registered automatically. The last vehicle to spawn becomes the player vehicle.

If you are managing multiple vehicles and want manual control over which one the player drives, set `registerLastVehicleAsPlayer` to false and call `RCCP.RegisterPlayerVehicle()` yourself.

---

## Transport (Teleport)

Transport instantly moves a vehicle to a new position and rotation. This is useful for respawning after a crash, moving to a checkpoint, or switching between gameplay areas.

### Overloads

```csharp
// Teleport the active player vehicle
RCCP.Transport(newPosition, newRotation);

// Teleport a specific vehicle
RCCP.Transport(targetVehicle, newPosition, newRotation);

// Teleport a specific vehicle, with optional velocity reset
RCCP.Transport(targetVehicle, newPosition, newRotation, resetVelocity);
```

| Overload | Parameters | Description |
|---|---|---|
| `Transport(position, rotation)` | Position + Rotation | Teleports the active player vehicle. Always resets velocity. |
| `Transport(vehicle, position, rotation)` | Vehicle + Position + Rotation | Teleports a specific vehicle. Always resets velocity. |
| `Transport(vehicle, position, rotation, resetVelocity)` | Vehicle + Position + Rotation + bool | Teleports a specific vehicle. If `resetVelocity` is false, the vehicle keeps its current speed. |

### What Happens During Transport

1. Rigidbody interpolation is temporarily set to `None` to prevent visual glitches.
2. Linear and angular velocity are zeroed (unless `resetVelocity` is false).
3. The vehicle is moved via `Rigidbody.MovePosition()` and `Rigidbody.MoveRotation()`.
4. All wheel collider motor torques are reset to zero.
5. If the vehicle has a connected trailer, the trailer is moved along with it, maintaining its relative offset.
6. `Physics.SyncTransforms()` is called to ensure the physics engine recognizes the new position immediately.

### Example: Respawn at a Checkpoint

```csharp
public Transform respawnPoint;

public void RespawnPlayer() {
    RCCP.Transport(respawnPoint.position, respawnPoint.rotation);
}
```

---

## Terrain Caching

RCCP_SceneManager caches Unity Terrain splatmap data at startup so that wheel colliders can look up ground materials efficiently at runtime. This is how the system knows whether a wheel is on grass, asphalt, gravel, or any other surface defined in your [Ground Materials](10_ground_materials.md) setup.

### How It Works

1. On `Start()`, the Scene Manager calls `GetAllTerrains()` as a coroutine.
2. It collects all active terrains via `Terrain.activeTerrains`.
3. For each terrain, it caches:
   - The `TerrainData` reference
   - The terrain collider's physics material
   - The alphamap dimensions
   - The full splatmap data array (`GetAlphamaps`)
   - The number of texture layers
4. Once complete, the `terrainsInitialized` flag is set to true.

### Relevant Properties

| Property | Type | Description |
|---|---|---|
| `allTerrains` | `Terrain[]` | Array of all Unity Terrains found in the scene. |
| `terrains` | `Terrains[]` | Cached terrain data (splatmap, alphamap dimensions, physics material) for each terrain. |
| `terrainsInitialized` | `bool` | True once all terrain data has been cached and is ready for ground material queries. |

If your scene does not use Unity Terrains (for example, you use mesh-based roads only), these properties will remain empty and the flag will stay false. Ground material detection will then rely on physics material assignments on your mesh colliders instead.

---

## Multithreading Support

RCCP_SceneManager checks at startup whether the current platform supports async/await operations. Some platforms (such as WebGL) do not support multithreading.

```
RCCP_SceneManager.multithreadingSupported  // static bool
```

The check works by attempting to run a trivial `Task.Run()` call. If it completes within one second, `multithreadingSupported` is set to true. If it fails or times out, it is set to false and RCCP falls back to synchronous methods.

This flag is used primarily by the [Damage System](11_damage_system.md) for async mesh deformation and repair operations.

**Note:** Multithreading can be disabled globally in [RCCP Settings](04_settings.md) by unchecking the `multithreading` toggle, regardless of platform support.

---

## Runtime Settings Applied on Awake

When RCCP_SceneManager initializes, it reads several values from [RCCP_Settings](04_settings.md) and applies them to the Unity runtime:

| Setting | Condition | Effect |
|---|---|---|
| Fixed Timestep | `overrideFixedTimeStep` is true | Sets `Time.fixedDeltaTime` to the configured `fixedTimeStep` value (default 0.02). |
| Frame Rate | `overrideFPS` is true | Sets `Application.targetFrameRate` to the configured `maxFPS` value (default 120). |
| Telemetry UI | `useTelemetry` is true | Instantiates the telemetry overlay prefab for real-time vehicle data display. |
| Input Rebinds | `autoSaveLoadInputRebind` is true | Loads saved input rebind overrides from PlayerPrefs on Awake, and saves them on disable. |

These overrides are applied once during Awake. If you need different values at runtime, you can modify `Time.fixedDeltaTime` or `Application.targetFrameRate` directly after the Scene Manager initializes.

---

## Events Fired by the Scene Manager

The Scene Manager monitors the active player vehicle each frame and fires events when it changes:

| Event | Signature | When It Fires |
|---|---|---|
| `RCCP_Events.OnVehicleChanged` | `void()` | A different vehicle becomes the active player vehicle. |
| `RCCP_Events.OnVehicleChangedToVehicle` | `void(RCCP_CarController)` | Same as above, but passes the new vehicle as a parameter. |

### Example: Listen for Vehicle Changes

```csharp
void OnEnable() {
    RCCP_Events.OnVehicleChanged += OnPlayerVehicleChanged;
}

void OnDisable() {
    RCCP_Events.OnVehicleChanged -= OnPlayerVehicleChanged;
}

void OnPlayerVehicleChanged() {
    Debug.Log("Player switched to: " + 
        RCCP_SceneManager.Instance.activePlayerVehicle.name);
}
```

The Scene Manager also listens to these internal events to track vehicles:

| Event | Purpose |
|---|---|
| `OnRCCPSpawned` | Adds the vehicle to `allVehicles`. Registers as player if `registerLastVehicleAsPlayer` is true. |
| `OnRCCPAISpawned` | Adds the AI vehicle to `allVehicles`. |
| `OnRCCPDestroyed` | Removes the vehicle from `allVehicles`. |
| `OnRCCPAIDestroyed` | Removes the AI vehicle from `allVehicles`. |
| `OnRCCPCameraSpawned` | Sets `activePlayerCamera`. If a player vehicle already exists, sets it as the camera target. |
| `OnRCCPUISpawned` | Sets `activePlayerCanvas`. |

---

## Additional Methods

| Method | Parameters | Description |
|---|---|---|
| `SetBehavior(int behaviorIndex)` | Index of the behavior preset | Activates behavior override mode and applies the preset at the given index from the behavior types array in RCCP_Settings. |
| `SetMobileController(MobileController type)` | `TouchScreen`, `Gyro`, `SteeringWheel`, or `Joystick` | Switches the active mobile controller type. See [Mobile Input](07_mobile.md). |
| `ChangeCamera()` | None | Cycles to the next camera mode on the active RCCP camera. See [Camera System](09_camera_system.md). |

---

## Common Issues

### Camera not following the vehicle

- Make sure an `RCCP_Camera` component exists in the scene, or that auto-creation of the camera is enabled in [RCCP Settings](04_settings.md).
- If you manually placed the camera and the vehicle in the scene, the camera should auto-target the vehicle once `OnRCCPCameraSpawned` fires. If the vehicle spawns before the camera, the Scene Manager handles this by setting the target when the camera spawns.

### UI not showing

- Check that `disableUIWhenNoPlayerVehicle` is not hiding the canvas. If this is enabled and no vehicle is registered, the UI will be hidden.
- Ensure an `RCCP_UIManager` prefab is in the scene or configured for auto-creation in [RCCP Settings](04_settings.md).

### Vehicle not responding to input

- Confirm the vehicle is registered as the player vehicle. Call `RCCP.RegisterPlayerVehicle(vehicle, true)` to register it with input enabled.
- If `registerLastVehicleAsPlayer` is enabled, the last spawned vehicle automatically becomes the player vehicle. Earlier vehicles lose player status.

### Multiple vehicles but wrong one is controlled

- Set `registerLastVehicleAsPlayer` to false in the Scene Manager Inspector.
- Manually call `RCCP.RegisterPlayerVehicle()` on the vehicle you want the player to control.

### Terrain surface detection not working

- Make sure your scene uses Unity Terrain objects (not just mesh colliders).
- Check that `terrainsInitialized` is true after the first few frames.
- If using mesh-based roads, assign appropriate physics materials to the mesh colliders and configure them in [Ground Materials](10_ground_materials.md).

---

## Next Steps

- [API Reference](16_api_reference.md) -- Full list of public methods in the RCCP static class
- [Camera System](09_camera_system.md) -- Camera modes, setup, and configuration

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

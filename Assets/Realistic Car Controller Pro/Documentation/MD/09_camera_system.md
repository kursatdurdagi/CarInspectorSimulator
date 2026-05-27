# Camera System

RCCP uses a **single camera** that switches between modes rather than placing multiple camera GameObjects in your scene. The main `RCCP_Camera` component parents itself to different positions depending on the active mode. This keeps your hierarchy clean and avoids the overhead of maintaining multiple cameras.

For general project setup, see [Installation](01_installation.md). For vehicle configuration, see [Vehicle Setup](03_vehicle_setup.md).

---

## Camera Setup

### Adding the Camera to Your Scene

Use the Unity menu:

**Tools > BoneCracker Games > Realistic Car Controller Pro > Create > Scene > Add RCCP Camera**

This instantiates the RCCP Camera prefab referenced in `RCCP_Settings`. The prefab contains:

- The `RCCP_Camera` component (the main controller)
- A child **Pivot** GameObject used for collision offsets
- A child **Camera** (the actual Unity Camera component)

### How Targeting Works

`RCCP_Camera` uses a `CameraTarget` class that holds:

| Field | Type | Description |
|---|---|---|
| `playerVehicle` | `RCCP_CarController` | The vehicle the camera follows |
| `HoodCamera` | `RCCP_HoodCamera` | Auto-found on the vehicle via `GetComponentInChildren` |
| `WheelCamera` | `RCCP_WheelCamera` | Auto-found on the vehicle via `GetComponentInChildren` |

When a vehicle is registered as the player vehicle through `RCCP_SceneManager`, the camera's `SetTarget()` method is called automatically. If `TPSAutoFocus` is enabled (the default), the camera smoothly adjusts its distance and height to fit the vehicle's bounds.

---

## Camera Modes

The `CameraMode` enum defines all available modes:

| Mode | Enum Value | Description |
|---|---|---|
| Third-Person | `TPS` | Default follow camera behind the vehicle |
| First-Person / Hood | `FPS` | View from inside the vehicle (dashboard area) |
| Wheel | `WHEEL` | Close-up view near a wheel |
| Fixed | `FIXED` | Scene-placed camera that watches the vehicle pass by |
| Cinematic | `CINEMATIC` | Auto-tracking camera with animation support |
| Top-Down | `TOP` | Overhead view, optionally orthographic |
| Truck-Trailer | `TRUCKTRAILER` | Specialized TPS mode for vehicles towing trailers |

### Enabling and Disabling Modes

Each mode (except TPS and TRUCKTRAILER) can be toggled on or off. When a mode is disabled, the camera skips it when cycling:

| Property | Default | Controls |
|---|---|---|
| `useHoodCameraMode` | `true` | FPS mode availability |
| `useWheelCameraMode` | `true` | WHEEL mode availability |
| `useFixedCameraMode` | `true` | FIXED mode availability |
| `useCinematicCameraMode` | `true` | CINEMATIC mode availability |
| `useTopCameraMode` | `false` | TOP mode availability |

TPS mode is always available and is the fallback when other modes encounter issues (such as occlusion in Wheel mode).

---

## Third-Person Mode (TPS)

The TPS camera follows behind the vehicle at a configurable distance and height. RCCP provides two TPS sub-modes:

| Sub-Mode | Enum | Characteristics |
|---|---|---|
| TPS1 | `TPSMode.TPS1` | Original implementation using `SmoothDampAngle` per axis |
| TPS2 | `TPSMode.TPS2` | More stable approach using `Quaternion.Slerp`, includes drift support |

Select the sub-mode via the `tPSMode` property. TPS2 is the default and recommended choice.

### Transform Settings

| Property | Range | Default | Description |
|---|---|---|---|
| `TPSDistance` | 0 - 20 | 6.5 | Distance behind the vehicle |
| `TPSHeight` | 0 - 10 | 1.5 | Height above the vehicle |
| `TPSOffset` | Vector3 | (0, 0, 0.2) | Local offset relative to the vehicle |
| `TPSRotationDamping` | 0 - 1 | 0.5 | Smoothing factor for rotation tracking |
| `TPSPitch` | -45 to 45 | 7.5 | Downward pitch angle |
| `TPSYaw` | -45 to 45 | 0 | Lateral yaw offset |

### Rotation Locking

These control which vehicle rotation axes the camera follows:

| Property | Default | Effect |
|---|---|---|
| `TPSLockX` | `true` | Camera tracks vehicle pitch |
| `TPSLockY` | `true` | Camera tracks vehicle yaw |
| `TPSLockZ` | `false` | Camera tracks vehicle roll |

### Dynamic Camera

When `TPSDynamic` is enabled, the camera distance, height, and pitch angle shift based on the vehicle's acceleration. This creates a more cinematic feel during rapid speed changes. The system uses a hidden `TPSAccelerationPoint` transform to smooth the acceleration offset.

### Speed-Based FOV

The field of view interpolates between `TPSMinimumFOV` and `TPSMaximumFOV` based on vehicle speed, creating a sense of velocity.

| Property | Range | Default | Description |
|---|---|---|---|
| `TPSMinimumFOV` | 10 - 90 | 40 | FOV when stationary |
| `TPSMaximumFOV` | 10 - 160 | 60 | FOV at high speed |

### Auto Focus

When `TPSAutoFocus` is `true` (the default), `SetTarget()` triggers a coroutine that calculates the vehicle's bounding-box extent and smoothly adjusts `TPSDistance` and `TPSHeight` over 2 seconds. This ensures each vehicle is framed correctly regardless of its size.

### Auto Reverse

When `TPSAutoReverse` is `true` (the default), the camera automatically rotates 180 degrees to look forward when the vehicle reverses.

### Camera Tilt

The camera tilts along the Z axis based on lateral velocity, creating a leaning effect during turns:

| Property | Range | Default | Description |
|---|---|---|---|
| `TPSTiltMaximum` | 0 - 25 | 15 | Maximum tilt angle in degrees |
| `TPSTiltMultiplier` | 0 - 1.5 | 1 | Multiplier to fine-tune tilt intensity |

### Camera Shake

When `TPSShake` is `true` (the default), the camera applies subtle Perlin noise-based positional and rotational shake that increases with vehicle speed. Maximum shake occurs around 260 km/h.

### Free Fall Behavior

When `TPSFreeFall` is `true` (the default), the camera rotation damping drops to near zero while the vehicle is airborne. This means the camera holds its orientation in the air rather than tracking the vehicle's tumbling.

---

## Hood Camera (FPS Mode)

The hood camera provides a first-person view from inside the vehicle, typically at dashboard level.

### Setup

1. Create an empty GameObject as a child of your vehicle.
2. Position it where the driver's eyes would be (on or near the dashboard).
3. Add the `RCCP_HoodCamera` component to it.
4. (Optional) Add a `ConfigurableJoint` and `Rigidbody` to reduce camera shake. The component auto-connects the joint to the vehicle's Rigidbody on start.

The `RCCP_Camera` finds the `RCCP_HoodCamera` on the target vehicle automatically via `GetComponentInChildren`.

### FOV

The hood camera uses `hoodCameraFOV` on the `RCCP_Camera` component (default 60, range 10-160 degrees).

### Orbit in Hood Mode

When `useOrbitInHoodCameraMode` is `true` (the default), the player can look around using mouse or touch input while in FPS mode.

---

## Wheel Camera

The wheel camera provides a low-angle close-up of a vehicle wheel.

### Setup

1. Create an empty GameObject as a child of your vehicle, near one of the wheels.
2. Orient it to point at the wheel.
3. Add the `RCCP_WheelCamera` component.

The `RCCP_Camera` finds it automatically. If occlusion is detected (an obstacle between the camera and the vehicle), the camera automatically reverts to TPS mode.

### FOV

The wheel camera uses `wheelCameraFOV` on the `RCCP_Camera` component (default 60, range 10-160 degrees).

---

## Fixed Camera

The `RCCP_FixedCamera` is a singleton placed in the scene. When active, the RCCP Camera parents itself to the fixed camera's position. The fixed camera:

- Looks at the vehicle and adjusts FOV based on distance.
- Repositions itself when the vehicle moves beyond `maxDistance` (default 50 meters).
- Repositions using raycasts to find a valid viewpoint near the road.

### Setup

1. Add an `RCCP_FixedCamera` component to a GameObject in your scene.
2. Set `maxDistance` to control how far the vehicle can travel before the camera relocates.
3. Configure `minimumFOV` (default 20) and `maximumFOV` (default 60) for zoom behavior.

Only one `RCCP_FixedCamera` should exist in a scene (it is a singleton).

---

## Cinematic Camera

The `RCCP_CinematicCamera` is a singleton that smoothly tracks the vehicle from a trailing position. It includes a **Pivot** child GameObject that can hold Unity animations for camera movement.

### Setup

1. Add an `RCCP_CinematicCamera` component to a GameObject in your scene.
2. Optionally, add animations to its Pivot child for sweeping camera motions.
3. Use `RCCP_FOVForCinematicCamera` on the Pivot to feed animated FOV values back to the cinematic camera.

The cinematic camera rotates smoothly toward the vehicle's heading and maintains a following distance of about 10 meters.

---

## Top-Down Camera

The top-down camera provides an overhead view, useful for racing games with a top-down perspective.

| Property | Default | Description |
|---|---|---|
| `topCameraAngle` | (45, 45, 0) | Euler angle of the overhead view |
| `topCameraDistance` | 100 | Distance from the vehicle |
| `maximumZDistanceOffset` | 10 | Forward offset based on speed |
| `useOrthoForTopCamera` | `false` | If true, switches to orthographic projection |
| `minimumOrtSize` | 10 | Orthographic size when stationary |
| `maximumOrtSize` | 20 | Orthographic size at speed |

The camera position lerps smoothly toward the vehicle and adjusts the forward offset based on vehicle speed.

---

## Truck-Trailer Camera

When a vehicle has a connected trailer (`RCCP_TrailerController`), the TPS mode automatically switches to `TRUCKTRAILER`. This mode:

- Centers the camera between the truck and the trailer (averaged position).
- Uses `AutoFocus` to calculate combined bounds of both objects for proper framing.
- Supports manual override via `RCCP_TrailerController.manualSetCameraDistanceAndHeight`, which lets you set `TPSDistance` and `TPSHeight` on the trailer component directly.

When the trailer is disconnected, the camera automatically reverts to standard TPS mode.

---

## Orbit Controls

Orbit allows the player to rotate the camera around the vehicle using mouse or touch input in TPS and Hood modes.

### Settings

| Property | Default | Description |
|---|---|---|
| `useOrbitInTPSCameraMode` | `true` | Enable orbit in TPS |
| `useOrbitInHoodCameraMode` | `true` | Enable orbit in Hood/FPS |
| `useOrbitOnlyHolding` | `true` | Orbit only while mouse button is held |
| `orbitXSpeed` | 100 | Horizontal orbit speed |
| `orbitYSpeed` | 100 | Vertical orbit speed |
| `orbitSmooth` | 25 | Smoothing factor |
| `minOrbitY` | -15 | Minimum vertical angle |
| `maxOrbitY` | 70 | Maximum vertical angle |
| `orbitReset` | `true` | Auto-reset orbit when vehicle is moving |

### Mobile Orbit

For mobile touch input, call `RCCP_Camera.OnDrag(PointerEventData)` from a UI drag panel. This feeds touch delta into the orbit system.

### Look-Back

The `lookBackNow` property rotates the camera 180 degrees to face behind the vehicle. This is triggered by the look-back input binding (see [Inputs](05_inputs.md)).

---

## Zoom

In TPS mode, mouse scroll input adjusts the camera distance:

| Property | Range | Default | Description |
|---|---|---|---|
| `zoomScrollMultiplier` | 0.5 - 10 | 5 | Sensitivity of scroll zoom |
| `minimumScroll` | 0+ | 0 | Minimum zoom offset |
| `maximumScroll` | 0+ | 5 | Maximum zoom offset |

---

## Occlusion / Collision Detection

When `useOcclusion` is `true` (the default), the camera performs a `SphereCast` from the vehicle toward the desired camera position. If the ray hits a collider (excluding vehicle layers and triggers), the camera repositions to the hit point plus a small normal offset. This prevents the camera from clipping through walls and terrain.

The `occlusionLayerMask` controls which layers are checked. By default, it includes all layers except RCCP vehicle layers.

---

## Auto-Change Camera

When `useAutoChangeCamera` is enabled, the camera automatically cycles to the next available mode every 10 seconds. The `autoChangeCameraTimer` property tracks the current countdown. This is useful for demo scenes and attract modes.

---

## Switching Camera at Runtime

### Cycle to Next Mode

```csharp
// Using the static API
RCCP.ChangeCamera();

// Or directly on the camera component
RCCP_SceneManager.Instance.activePlayerCamera.ChangeCamera();
```

### Switch to a Specific Mode

```csharp
RCCP_Camera cam = RCCP_SceneManager.Instance.activePlayerCamera;
cam.ChangeCamera(RCCP_Camera.CameraMode.TOP);
```

### Toggle Camera On/Off

```csharp
RCCP_Camera cam = RCCP_SceneManager.Instance.activePlayerCamera;
cam.ToggleCamera(false); // Disables rendering
cam.ToggleCamera(true);  // Re-enables rendering
```

---

## Showroom Cameras

RCCP includes two additional camera components for menus and vehicle selection screens.

### RCCP_ShowroomCamera

An orbiting camera for main menus and vehicle showcases.

| Property | Default | Description |
|---|---|---|
| `target` | (none) | Transform to orbit around |
| `distance` | 8 | Distance from target |
| `orbitingNow` | `true` | Enable auto-rotation |
| `orbitSpeed` | 5 | Auto-rotation speed |
| `smooth` | `true` | Smooth position/rotation interpolation |
| `smoothingFactor` | 5 | Interpolation speed |
| `minY` / `maxY` | 5 / 35 | Vertical angle limits |
| `dragSpeed` | 10 | Manual drag rotation speed |

Call `OnDrag(PointerEventData)` from a UI element to enable player-controlled rotation. Use `ToggleAutoRotation(bool)` to start or stop automatic orbiting.

### RCCP_CameraCarSelection

A simpler orbiting camera specifically for vehicle selection UIs. It automatically rotates around the target at a fixed angle and distance. No manual drag input by default.

| Property | Default | Description |
|---|---|---|
| `target` | (none) | Transform to orbit around |
| `distance` | 5 | Distance from target |
| `speed` | 25 | Rotation speed |
| `angle` | 10 | Vertical viewing angle |

---

## Common Issues

### Camera Does Not Follow the Vehicle

- Make sure a vehicle is registered as the player vehicle. Check that `RCCP_SceneManager.Instance.activePlayerCamera.cameraTarget.playerVehicle` is not null.
- Ensure the RCCP Camera prefab is in the scene. Use the menu command to add it if missing.
- If you are spawning vehicles via script, call `RCCP.RegisterPlayerVehicle()` or use `RCCP.SpawnRCC()` which handles registration automatically.

### Camera Clips Through Walls

- Verify that `useOcclusion` is enabled on the `RCCP_Camera` component.
- Check the `occlusionLayerMask` to confirm it includes the layers of your environment colliders.
- Reduce the camera's near clip plane on the child Camera component if clipping occurs at very close range.

### Hood or Wheel Camera Is Skipped

- The hood camera requires an `RCCP_HoodCamera` component on a child of the vehicle. If it is missing, the FPS mode is skipped when cycling.
- The same applies to `RCCP_WheelCamera` for wheel mode.
- Verify the component exists and is on an active GameObject.

### Camera Shakes Excessively in Hood Mode

- Add a `ConfigurableJoint` and `Rigidbody` to the hood camera GameObject. `RCCP_HoodCamera` auto-connects the joint to the vehicle's Rigidbody.
- The `FixShake()` method is called automatically when switching to FPS mode.

### Truck-Trailer Mode Activates Unexpectedly

- This is by design. When a trailer is connected, TPS automatically switches to TRUCKTRAILER mode to frame both objects. When the trailer is disconnected, it reverts to standard TPS.

---

## Related Topics

- [Vehicle Setup](03_vehicle_setup.md) -- adding hood and wheel cameras to your vehicle
- [Settings](04_settings.md) -- configuring the camera prefab reference in RCCP_Settings
- [Inputs](05_inputs.md) -- camera switch and look-back input bindings
- [Mobile](07_mobile.md) -- touch-based orbit controls

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

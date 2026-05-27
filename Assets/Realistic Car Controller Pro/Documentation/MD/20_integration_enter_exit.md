# Integration: Enter-Exit Vehicle (BCG Shared Assets)

BCG Shared Assets is a separate addon package that provides a character controller system, allowing a player character to walk around, approach RCCP vehicles, and enter or exit them at runtime. It includes first-person (FPS), third-person (TPS), and mobile character controllers, along with camera management, input handling, and UI for interaction prompts.

This integration is not part of the core RCCP package. You must import it separately from the included installer.

---

## Overview

The enter-exit system is event-driven. When the player approaches a vehicle and presses the interaction key, the system disables the character controller, parents it to the vehicle, enables vehicle control, and switches cameras. When exiting, the reverse occurs: the character is re-enabled at the vehicle's get-out position, vehicle control is disabled, and the character camera is restored.

Key design points:

- The system uses a **singleton manager** (`BCG_EnterExitManager`) that listens to spawn, enter, and exit events from all players and vehicles in the scene.
- Vehicle control is toggled via `SendMessage("SetCanControl", ...)`, which RCCP_CarController responds to.
- Camera switching is handled by enabling/disabling the vehicle's `correspondingCamera` and the character's camera.
- A **1-second cooldown** prevents instant re-entry or re-exit.
- The player cannot exit while the vehicle speed exceeds the configured `enterExitSpeedLimit` (default: 20 km/h).

---

## Prerequisites

- Realistic Car Controller Pro installed and working.
- At least one RCCP vehicle set up in the scene.

---

## Installing

1. Open the RCCP Welcome Window: **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.
2. Navigate to the **Addons** tab.
3. Click **Download and import BCG Shared Assets**.
4. Unity will import `BCG_SharedAssets.unitypackage` from `Addons/Installers/`.
5. After import, the scripting symbol `BCG_ENTEREXIT` is added automatically by `RCCP_AddonDefineManager`.
6. Wait for Unity to recompile.

The installer package is located at:

```
Assets/Realistic Car Controller Pro/Addons/Installers/BCG_SharedAssets.unitypackage
```

After import, the BCG Shared Assets folder appears at:

```
Assets/BoneCracker Games Shared Assets/
```

---

## Scripting Symbol

| Symbol | Set When | Removed When |
|---|---|---|
| `BCG_ENTEREXIT` | `Assets/BoneCracker Games Shared Assets/` folder exists | Folder is deleted (auto-detected by `RCCP_AddonDefineManager`) |

If auto-detection fails, you can manually remove the symbol from **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.

---

## Core Components

### BCG_EnterExitManager

**Role:** Scene-level singleton that orchestrates the entire enter-exit lifecycle.

**Add Component:** `BoneCracker Games > BCG Shared Assets Pro > BCG Enter Exit Manager`

The manager:

- Listens to player spawn/destroy events and caches all active `BCG_EnterExitPlayer` instances.
- Listens to vehicle spawn/destroy events and caches all active `BCG_EnterExitVehicle` instances along with their corresponding cameras.
- Handles the `Interact` input event to trigger enter or exit.
- On enter: disables the character, parents it to the vehicle, enables the vehicle camera, starts the engine (if configured), and registers the vehicle with `RCCP_SceneManager`.
- On exit: re-enables the character at the get-out position, disables the vehicle camera, stops the engine (if configured), and clears the active vehicle from `RCCP_SceneManager`.

| Property | Type | Description |
|---|---|---|
| `activePlayer` | `BCG_EnterExitPlayer` | The currently active character controller. |
| `cachedMainCameras` | `List<GameObject>` | All vehicle cameras tracked by the manager. |
| `cachedPlayers` | `List<BCG_EnterExitPlayer>` | All spawned character controllers. |
| `cachedVehicles` | `List<BCG_EnterExitVehicle>` | All spawned vehicles with enter-exit support. |
| `cachedCanvas` | `BCG_EnterExitCharacterUICanvas` | Reference to the UI canvas for mobile/touch interaction. |

### BCG_EnterExitVehicle

**Role:** Marks an RCCP vehicle as enterable/exitable and defines its exit point and camera.

**Add Component:** `BoneCracker Games > BCG Shared Assets Pro > BCG Enter Exit Vehicle`

| Property | Type | Description |
|---|---|---|
| `correspondingCamera` | `GameObject` | The camera to activate when the player enters this vehicle. Auto-detected from `RCCP_Camera` in the scene if not assigned. |
| `driver` | `BCG_EnterExitPlayer` | The player currently driving this vehicle. `null` if empty. |
| `getOutPosition` | `Transform` | The world position where the character spawns when exiting. If none is found, a child named "Get Out Pos" is created automatically at local position `(-1.5, 0, 0)`. |

When this component is enabled, it sends `SetCanControl(false)` to the vehicle, preventing it from driving until a player enters.

**Events:**

| Event | Signature | Fired When |
|---|---|---|
| `OnBCGVehicleSpawned` | `(BCG_EnterExitVehicle)` | Component is enabled. |
| `OnBCGVehicleDestroyed` | `(BCG_EnterExitVehicle)` | Component is disabled. |

### BCG_EnterExitPlayer

**Role:** Marks a character controller as capable of entering vehicles.

**Add Component:** `BoneCracker Games > BCG Shared Assets Pro > BCG Enter Exit Player`

| Property | Type | Description |
|---|---|---|
| `isTPSController` | `bool` | Set to `true` if this character uses a TPS controller with its own camera. |
| `rayHeight` | `float` | Height offset for the interaction raycast (TPS mode). Default: `1`. |
| `canControl` | `bool` | Whether the character accepts input. |
| `showGui` | `bool` | Whether the "Press Interaction [TAB] Key To Get In" prompt is visible. |
| `targetVehicle` | `BCG_EnterExitVehicle` | The vehicle currently in front of the player (detected by raycast). |
| `playerStartsAsInVehicle` | `bool` | If `true`, the player begins the scene already inside `inVehicle`. |
| `inVehicle` | `BCG_EnterExitVehicle` | The vehicle the player is currently inside. `null` if on foot. |
| `characterCamera` | `Camera` | The FPS camera (auto-detected from children) or the TPS camera reference. |

**Detection:** The player casts a ray forward (from the camera for FPS, from `transform.position + Vector3.up * rayHeight` for TPS) with a range of **1.5 meters**. If the ray hits a collider whose parent has a `BCG_EnterExitVehicle` component, `targetVehicle` is set and the interaction prompt appears.

**Events:**

| Event | Signature | Fired When |
|---|---|---|
| `OnBCGPlayerSpawned` | `(BCG_EnterExitPlayer)` | Component is enabled. |
| `OnBCGPlayerDestroyed` | `(BCG_EnterExitPlayer)` | Component is destroyed. |
| `OnBCGPlayerEnteredAVehicle` | `(BCG_EnterExitPlayer, BCG_EnterExitVehicle)` | Player enters a vehicle. |
| `OnBCGPlayerExitedFromAVehicle` | `(BCG_EnterExitPlayer, BCG_EnterExitVehicle)` | Player exits a vehicle. |

### BCG_EnterExitSettings

**Role:** Global settings ScriptableObject loaded from `Resources/BCG_EnterExitSettings`.

| Setting | Type | Default | Description |
|---|---|---|---|
| `keepEnginesAlive` | `bool` | `true` | If `true`, the engine remains running after the player exits. |
| `startStopEngine` | `bool` | `true` | If `true`, the engine starts on enter and stops (or remains per `keepEnginesAlive`) on exit. |
| `enterExitSpeedLimit` | `float` | `20` | Maximum vehicle speed (km/h) at which the player can exit. |
| `mobileController` | `bool` | `false` | If `true`, uses mobile joystick input instead of keyboard/mouse. |
| `autoLockMouseCursor` | `bool` | `true` | If `true`, the cursor is locked when on foot and unlocked when in a vehicle. |

---

## Setup Guide

### Step 1: Import BCG Shared Assets

Follow the installation steps above.

### Step 2: Add BCG_EnterExitVehicle to Each Vehicle

1. Select your RCCP vehicle in the Hierarchy.
2. Add Component: `BCG Enter Exit Vehicle`.
3. A child object named **"Get Out Pos"** is created automatically. Reposition it to a suitable door-side location.
4. The `correspondingCamera` field auto-fills with the scene's `RCCP_Camera`. You can override it with a different camera if needed.

### Step 3: Add BCG_EnterExitPlayer to Your Character

1. Select or create your player character (use one of the included prefabs, or your own).
2. Add Component: `BCG Enter Exit Player`.
3. If using a TPS controller, enable `isTPSController` and assign the `characterCamera`.
4. If you want the player to start inside a vehicle, enable `playerStartsAsInVehicle` and assign the `inVehicle` reference.

### Step 4: Ensure BCG_EnterExitManager Exists

The manager auto-creates itself as a singleton if none exists in the scene. However, for explicit control you can add a GameObject named `_BCGEnterExitManager` and attach the `BCG_EnterExitManager` component.

### Step 5: Configure Input

The interaction input is handled by `BCG_InputManager`, which reads from Unity's Input System. The default interaction action is bound to the **Tab** key in the `Character` action map.

### Step 6: Configure UI (Optional)

For mobile or on-screen interaction buttons, add the `BCG_EnterExitCharacterUICanvas` prefab to the scene. It automatically switches between "On Foot" and "In Vehicle" UI groups.

---

## Character Controllers

BCG Shared Assets includes three character controller prefabs, all located in `Assets/BoneCracker Games Shared Assets/Prefabs/Character/`:

### BCG_FPSCharacterController

A first-person character controller using Unity's `CharacterController` component and the new Input System.

| Property | Default | Description |
|---|---|---|
| `moveSpeed` | `5` | Movement speed in units/second. |
| `gravity` | `-9.81` | Gravity applied each frame. |
| `lookSensitivity` | `2` | Mouse look sensitivity multiplier. |
| `maxLookAngle` | `90` | Maximum up/down look angle in degrees. |

**Prefab:** `BCG_FPSCharacterController.prefab`

### BCG_TPSCharacterController

A third-person character controller that rotates the character with the mouse and moves relative to facing direction.

| Property | Default | Description |
|---|---|---|
| `moveSpeed` | `5` | Movement speed in units/second. |
| `gravity` | `-9.81` | Gravity applied each frame. |
| `lookSensitivity` | `2` | Mouse look sensitivity multiplier. |
| `rotationSpeed` | `180` | Horizontal rotation speed in degrees/second. |

**Prefab:** `BCG_TPSCharacterController.prefab`

### BCG_MobileCharacterController

A mobile touch controller that reads from two on-screen joysticks (movement and camera) and provides static `move` and `mouse` vectors for other components to consume.

**Prefab:** `BCG_FPSTPS MobileController.prefab` (in `Prefabs/UI/`)

To use mobile controls, set `BCG_EnterExitSettings.Instance.mobileController = true`.

---

## Camera

### BCG_TPSCameraController

A third-person orbit camera for the character. Located at `Assets/BoneCracker Games Shared Assets/Prefabs/Character/BCG_TPSCamera.prefab`.

| Property | Default | Description |
|---|---|---|
| `target` | (auto-detected) | The transform to orbit around. Auto-finds `BCG_TPSCharacterController` if not assigned. |
| `offset` | `(0, 5, -10)` | Camera offset from target in local space. |
| `distance` | `10` | Orbit distance from target. |
| `rotationSpeed` | `5` | Camera rotation speed. |
| `minYAngle` / `maxYAngle` | `-30` / `60` | Vertical angle clamp to prevent camera flipping. |
| `smoothSpeed` | `0.125` | Camera movement smoothing factor. |

This camera is only active when the player is on foot. When the player enters a vehicle, the manager disables it and enables the vehicle's `correspondingCamera` (typically `RCCP_Camera`).

---

## Input System

### BCG_InputManager

A singleton (`RCCP_Singleton<BCG_InputManager>`) that reads from Unity's Input System using the `Character` action map.

**Input Actions (Character Map):**

| Action | Default Binding | Description |
|---|---|---|
| `Movement` | WASD / Arrow Keys | Character movement (Vector2). |
| `Aim` | Mouse Delta | Camera look (Vector2). |
| `Interact` | Tab | Enter or exit the nearest vehicle. |
| `CursorLock` | Right Mouse Button | Lock the cursor. |
| `CursorUnlock` | Escape | Unlock the cursor. |

**Events:**

| Event | Fired When |
|---|---|
| `OnInteract` | The Interact action is performed. |
| `OnCursorLock` | The CursorLock action is performed. |
| `OnCursorUnlock` | The CursorUnlock action is performed. |

The manager persists across scene loads via `DontDestroyOnLoad`. It automatically disables inputs on application pause/focus loss and re-enables them on resume.

---

## UI Components

### BCG_EnterExitCharacterUICanvas

Manages two UI groups that switch based on whether the player is on foot or in a vehicle.

| Property | Type | Description |
|---|---|---|
| `displayType` | `DisplayType` | Current mode: `OnFoot` or `InVehicle`. Managed by `BCG_EnterExitManager`. |
| `UisInVehicle` | `GameObject` | UI group shown when inside a vehicle. |
| `UisOnFoot` | `GameObject` | UI group shown when on foot. |

This canvas only activates itself if `BCG_EnterExitSettings.Instance.mobileController` is `true`. On desktop, the built-in `OnGUI` text prompt on `BCG_EnterExitPlayer` is used instead.

### BCG_UIInteractionButton

A simple button component that calls `BCG_EnterExitManager.Instance.Interact()` when tapped. Attach it to any UI Button for mobile enter/exit interaction.

---

## Common Issues

### Character does not enter the vehicle

- **Check interaction range.** The raycast detection range is 1.5 meters. The player must be close to and facing the vehicle.
- **Check colliders.** The raycast must hit a collider on the vehicle or a child of the vehicle that has `BCG_EnterExitVehicle` on its parent.
- **Check the scripting symbol.** `BCG_ENTEREXIT` must be defined. Verify in **Edit > Project Settings > Player > Scripting Define Symbols**.
- **Check `canControl`.** The `BCG_EnterExitPlayer.canControl` flag must be `true`.

### Camera does not switch when entering/exiting

- **Check `correspondingCamera`.** On the `BCG_EnterExitVehicle`, ensure `correspondingCamera` is assigned. If left empty, it auto-detects `RCCP_Camera`, but only if one exists in the scene.
- **Check for multiple cameras.** The manager disables all cached vehicle cameras except the entered vehicle's camera. If your camera is not cached (was not present at spawn time), it will not be toggled.

### Vehicle drives on its own after player exits

- This should not happen. `BCG_EnterExitVehicle.OnEnable()` sends `SetCanControl(false)` to the vehicle, and `BCG_EnterExitManager` sends `SetCanControl(false)` on exit. If it occurs, verify that no other script is overriding `canControl` on the `RCCP_CarController`.

### Player appears inside the vehicle mesh on exit

- Reposition the **"Get Out Pos"** child transform on the vehicle to a clear area beside the driver's door.

### Enter/exit happens twice rapidly

- The system has a built-in 1-second cooldown. If this still occurs, check that `BCG_EnterExitManager` is not duplicated in the scene.

---

## Lifecycle Diagram

```
Player on Foot                          Player in Vehicle
     |                                       |
     |-- Raycast hits vehicle               |
     |   targetVehicle = vehicle             |
     |   showGui = true                      |
     |                                       |
     |-- Press Interact (Tab)               |
     |   GetIn(targetVehicle)                |
     |   OnBCGPlayerEnteredAVehicle fires    |
     |                                       |
     |   Manager:                            |
     |     player.inVehicle = vehicle        |
     |     player.gameObject.SetActive(false)|
     |     Vehicle camera ON                 |
     |     SetCanControl(true)               |
     |     SetEngine(true)                   |
     |     RCCP_SceneManager.activeVehicle   |
     |                                       |
     |                             Press Interact (Tab)
     |                             GetOut()
     |                             OnBCGPlayerExitedFromAVehicle fires
     |                                       |
     |                             Manager:  |
     |                               player at getOutPosition
     |                               player.gameObject.SetActive(true)
     |                               All vehicle cameras OFF
     |                               SetCanControl(false)
     |                               SetEngine(keepEnginesAlive)
     |                               RCCP_SceneManager.activeVehicle = null
```

---

## See Also

- [Settings](04_settings.md) -- RCCP global settings including camera configuration.
- [Camera System](09_camera_system.md) -- RCCP_Camera setup and behavior.
- [Scene Manager](15_scene_manager.md) -- How RCCP tracks the active player vehicle.
- [API Reference](16_api_reference.md) -- `RCCP.RegisterPlayerVehicle()`, `RCCP.SetControl()`.

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

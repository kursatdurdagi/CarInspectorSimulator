# Integration: Photon PUN 2

Photon PUN 2 (Photon Unity Networking) enables multiplayer vehicle synchronization for RCCP vehicles. This integration package synchronizes vehicle position, rotation, velocity, all driver inputs, engine state, gear state, differential outputs, lights, and wheel RPMs across the network using Photon's serialization system.

---

## Overview

The RCCP Photon integration uses an **owner-sends, others-receive** model. The vehicle owner sends its full state to all other players every network tick. Remote players apply that state using enhanced interpolation with lag compensation, velocity smoothing, and optional extrapolation to minimize visual jitter.

Key features:

- Full drivetrain state synchronization (engine RPM, gear, clutch, differential outputs).
- Light state synchronization (low beam, high beam, indicators).
- Wheel RPM correction for accurate visual wheel spin on remote vehicles.
- Configurable lag compensation, interpolation speed, and extrapolation.
- Teleport threshold to handle large position discrepancies.
- State buffering (10-frame ring buffer) for smoother interpolation.
- Built-in lobby manager with room browsing, chat, and player statistics.

---

## Prerequisites

1. **Photon PUN 2** installed in your project. Download it from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) (free version available).
2. **Photon App ID** configured in Photon's `PhotonServerSettings` asset. You can get an App ID from the [Photon Dashboard](https://dashboard.photonengine.com/).
3. Realistic Car Controller Pro installed and working with at least one vehicle.

---

## Installing

### Step 1: Import Photon PUN 2

If you have not already installed Photon PUN 2, import it from the Asset Store. After import, Photon will prompt you to enter your App ID.

### Step 2: Import the RCCP Integration Package

1. Open the RCCP Welcome Window: **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.
2. Navigate to the **Addons** tab.
3. Under **Photon PUN2**, click **Import Photon PUN2 Integration**.
4. Unity imports `RCCP_PhotonIntegration.unitypackage` from `Addons/Installers/`.
5. Wait for Unity to recompile.

The integration files are installed to:

```
Assets/Realistic Car Controller Pro/Addons/Installed/Photon PUN 2/
```

### Step 3: Verify Scripting Symbols

After import, the `RCCP_PhotonInitLoad` editor script automatically adds the `RCCP_PHOTON` scripting symbol. Photon itself provides `PHOTON_UNITY_NETWORKING`. Both must be present for the integration scripts to compile.

---

## Scripting Symbols

| Symbol | Source | Required |
|---|---|---|
| `PHOTON_UNITY_NETWORKING` | Photon PUN 2 package (automatic) | Yes |
| `RCCP_PHOTON` | RCCP Photon integration (automatic via `RCCP_PhotonInitLoad`) | Yes |

If you remove the integration, delete the `Photon PUN 2` folder from `Addons/Installed/`, then remove `RCCP_PHOTON` from **Edit > Project Settings > Player > Scripting Define Symbols**. The `RCCP_AddonDefineManager` will also attempt to remove it automatically when it detects the folder deletion.

---

## Core Components

### RCCP_PhotonSync

The main synchronization component. Inherits from `MonoBehaviourPunCallbacks` and implements `IPunObservable`.

**Add to:** Each networked RCCP vehicle prefab (alongside `PhotonView`).

**Requirement:** `[RequireComponent(typeof(PhotonView))]`

#### Inspector Settings

| Property | Type | Default | Description |
|---|---|---|---|
| `teleportDistanceThreshold` | `float` | `4` | If the remote vehicle is farther than this from its target position, it teleports instead of interpolating. |
| `lagCompensationTime` | `float` | `0.05` | Lag compensation time in seconds. Higher values add smoothness but increase perceived latency. |
| `positionInterpolationSpeed` | `float` | `10` | Speed multiplier for position interpolation of remote vehicles. |
| `rotationInterpolationSpeed` | `float` | `10` | Speed multiplier for rotation interpolation of remote vehicles. |
| `useExtrapolation` | `bool` | `true` | Enable extrapolation to predict where the vehicle will be during network gaps. |
| `maxExtrapolationTime` | `float` | `0.1` | Maximum total prediction time (lag compensation + extrapolation) in seconds. |
| `useVelocitySmoothing` | `bool` | `true` | Enable velocity-based Rigidbody smoothing for more accurate physics on remote vehicles. |
| `velocityDampening` | `float` | `0.8` | Damping factor for velocity interpolation. |
| `photonSendRate` | `int` | `30` | Photon network send rate (packets/second). |
| `photonSerializationRate` | `int` | `30` | Photon serialization rate (serializations/second). |

#### Synchronized Data

The following data is sent every serialization tick from the owner to all remote clients:

| Category | Fields |
|---|---|
| **Inputs** | throttle, brake, steer, handbrake, NOS boost, clutch |
| **Engine** | RPM, starting flag, running flag |
| **Gearbox** | current gear index, gear input, gear state (Forward/Reverse/Neutral/Park), shifting flag |
| **Differential** | left and right output for each differential |
| **Lights** | low beam, high beam, left indicator, right indicator, hazards |
| **Wheels** | RPM for each wheel collider |
| **Transform** | position, rotation, linear velocity, angular velocity, timestamp |

#### How It Works

**Owner (local player):**

- `FixedUpdate` runs normally. No input overrides are applied.
- `OnPhotonSerializeView` writes all vehicle state to the Photon stream.

**Remote (other players):**

- `OnPhotonSerializeView` receives the state and buffers it in a 10-frame ring buffer.
- Lag compensation predicts a target position based on received velocity and network lag.
- `FixedUpdate` calls `ApplyRemoteMovement()` which interpolates the Rigidbody toward the predicted position using `MovePosition`/`MoveRotation`.
- `ApplyRemoteInputsAndState()` overrides all input and drivetrain modules on the remote vehicle to match the owner's state.
- `RCCP_WheelRPMCorrection` components (auto-added to each wheel on remote vehicles) correct visual wheel spin to match synced RPMs.

### RCCP_PhotonManager

A lobby and room management UI controller. Inherits from `MonoBehaviourPunCallbacks`.

**Role:** Handles the full Photon lifecycle: connect to server, join lobby, browse/create rooms, join rooms, chat, and load gameplay scenes.

| Feature | Description |
|---|---|
| **Connection** | Connects using `PhotonNetwork.ConnectUsingSettings()` with a player nickname. |
| **Lobby** | Joins the default typed lobby. Displays online player count, room count, and region. |
| **Room Browser** | Lists available rooms with player counts. Rooms that close or become invisible are removed. |
| **Room Creation** | Creates rooms with 4 max players, open and visible. |
| **Chat** | Buffered RPC-based chat system. Displays last 7 messages. |
| **Scene Loading** | Loads the gameplay scene via `PhotonNetwork.LoadLevel()` when a room is joined. |

### RCCP_DemoVehicles_Photon

A `ScriptableObject` singleton (loaded from `Resources/RCCP_DemoVehicles_Photon`) that holds an array of `RCCP_CarController` prefab references for spawning in Photon demos.

### RCCP_DemoScenes_Photon

A `ScriptableObject` singleton (loaded from `Resources/RCCP_DemoScenes_Photon`) that holds references to the Photon demo scenes (City and Lobby).

---

## Setting Up a Networked Vehicle

### Step 1: Start with a Working RCCP Vehicle

Ensure your vehicle works correctly in single-player before adding networking.

### Step 2: Add PhotonView

1. Select your vehicle prefab.
2. Add Component: `PhotonView`.
3. Set `Ownership Transfer` to `Fixed`.
4. Set `Synchronization` to `Unreliable`.
5. Set `Observable Search` to `Auto Find All`.

### Step 3: Add RCCP_PhotonSync

1. Add Component: `RCCP_PhotonSync`.
2. The component automatically adds itself to the PhotonView's observed list on `Start()`.
3. Adjust interpolation settings if needed (defaults work well for most cases).

### Step 4: Place the Prefab in a Resources Folder

Photon requires spawnable prefabs to be in a `Resources/` folder (or a subfolder).

For the demo system, place vehicle prefabs in:

```
Resources/Photon Vehicles/
```

The `RCCP_PhotonDemo.Spawn()` method uses this path:

```csharp
PhotonNetwork.Instantiate("Photon Vehicles/" + vehicleName, position, rotation, 0);
```

### Step 5: Register After Spawning

After `PhotonNetwork.Instantiate()`, register the vehicle with RCCP:

```csharp
RCCP_CarController vehicle = PhotonNetwork.Instantiate(prefabPath, pos, rot, 0)
    .GetComponent<RCCP_CarController>();

RCCP.RegisterPlayerVehicle(vehicle);
RCCP.SetControl(vehicle, true);

if (RCCP_SceneManager.Instance.activePlayerCamera)
    RCCP_SceneManager.Instance.activePlayerCamera.SetTarget(vehicle);
```

---

## Ownership and Authority

- **Owner:** Only the PhotonView owner sends vehicle state. The owner's inputs are processed locally by the RCCP drivetrain as normal.
- **Remote clients:** Inputs and drivetrain modules are overridden with the received network state. The vehicle's `RCCP_Input.overrideExternalInputs` is set to `true`.
- **Ownership transfer:** The `RCCP_PhotonSync.Reset()` method configures `OwnershipOption.Fixed` by default. For vehicle sharing (e.g., enter-exit with Photon), ownership must be transferred via `PhotonView.TransferOwnership()`.

---

## Demo Scenes

The integration includes two demo scenes:

| Scene | Description |
|---|---|
| `RCCP_Scene_Blank_Photon` | Minimal Photon demo. Connects, joins a random room, and provides a vehicle selection menu. |
| `RCCP_Scene_PUN2Lobby` | Full lobby UI with room browsing, creation, chat, and gameplay scene loading. |

To run the demo:

1. Configure your Photon App ID in `PhotonServerSettings`.
2. Add the Photon demo scenes to **Build Settings > Scenes In Build**.
3. Enter Play Mode. The demo auto-connects, joins a lobby, and creates/joins a room.
4. Use the in-game menu to select and spawn a vehicle.

---

## Tuning Network Performance

### Reducing Jitter on Remote Vehicles

| Adjustment | Effect |
|---|---|
| Increase `positionInterpolationSpeed` | Faster catch-up to target position, but may overshoot. |
| Increase `lagCompensationTime` | More forward prediction, smoother but higher perceived latency. |
| Enable `useVelocitySmoothing` | Smooths Rigidbody velocity transitions. |
| Decrease `teleportDistanceThreshold` | Teleports sooner when position error is large. |

### Reducing Bandwidth

| Adjustment | Effect |
|---|---|
| Decrease `photonSendRate` | Fewer packets per second. May increase jitter. |
| Decrease `photonSerializationRate` | Fewer serializations per second. |

---

## Common Issues

### Remote vehicles jitter or teleport frequently

- Check network latency. High-latency connections naturally produce more jitter.
- Increase `positionInterpolationSpeed` and `rotationInterpolationSpeed` for faster convergence.
- Ensure `useVelocitySmoothing` is enabled.
- If teleporting too often, increase `teleportDistanceThreshold`.

### Inputs not syncing on remote vehicles

- Verify that `PhotonView` ownership is correct. Only the owner sends input data.
- Ensure `RCCP_PhotonSync` is in the PhotonView's `ObservedComponents` list (auto-added on `Start()`).
- Check that both `PHOTON_UNITY_NETWORKING` and `RCCP_PHOTON` scripting symbols are defined.

### Vehicle spawns but does not move

- After spawning with `PhotonNetwork.Instantiate()`, call `RCCP.RegisterPlayerVehicle()` and `RCCP.SetControl(vehicle, true)`.
- Ensure the spawning player is the PhotonView owner.

### Cannot connect to Photon

- Verify your App ID in `PhotonServerSettings`.
- Check your internet connection.
- Review the Photon console for error messages.

### Wheels spin at wrong speed on remote vehicles

- `RCCP_WheelRPMCorrection` components are auto-added to remote vehicle wheels on `Start()`. If wheels are added dynamically after start, this may not work.

---

## Removing the Integration

1. Delete the folder: `Assets/Realistic Car Controller Pro/Addons/Installed/Photon PUN 2/`.
2. Remove the `RCCP_PHOTON` scripting symbol from **Edit > Project Settings > Player > Scripting Define Symbols** (or let `RCCP_AddonDefineManager` handle it automatically).
3. Wait for Unity to recompile.

---

## See Also

- [Vehicle Setup](03_vehicle_setup.md) -- Creating an RCCP vehicle from scratch.
- [API Reference](16_api_reference.md) -- `RCCP.RegisterPlayerVehicle()`, `RCCP.SetControl()`.
- [Integration: Mirror](22_integration_mirror.md) -- Alternative networking with Mirror.
- [Integration: Enter-Exit](20_integration_enter_exit.md) -- Character enter-exit system (compatible with Photon via ownership transfer).

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

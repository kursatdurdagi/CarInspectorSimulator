# Integration: Mirror Networking

Mirror is a free, open-source networking library for Unity. RCCP provides an integration package that enables multiplayer vehicle synchronization using Mirror's networking framework. This guide covers installation, setup, and usage.

---

## Overview

The RCCP Mirror integration follows the same design philosophy as the [Photon integration](21_integration_photon.md): the vehicle owner sends its state (inputs, drivetrain, lights, transform) to all other clients, and remote clients apply that state with interpolation. Mirror uses a server-authoritative or host model, where one player acts as both server and client (host mode) or a dedicated server relays data.

Mirror is a suitable choice when you want:

- Free, open-source networking with no per-CCU costs.
- Server-authoritative game logic.
- LAN or self-hosted dedicated server support.
- Full control over the network transport layer.

---

## Prerequisites

1. **Mirror** installed in your project. Download it from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/network/mirror-129321) (free).
2. Realistic Car Controller Pro installed and working with at least one vehicle.

---

## Installing

### Step 1: Import Mirror

If Mirror is not yet in your project, import it from the Asset Store. After import, the `MIRROR` scripting symbol is added automatically by Mirror.

### Step 2: Import the RCCP Integration Package

1. Open the RCCP Welcome Window: **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.
2. Navigate to the **Addons** tab.
3. Under **Mirror**, click **Import Mirror Integration**.
4. Unity imports `RCCP_MirrorIntegration.unitypackage` from `Addons/Installers/`.
5. Wait for Unity to recompile.

The integration files are installed to:

```
Assets/Realistic Car Controller Pro/Addons/Installed/Mirror/
```

### Step 3: Verify Scripting Symbols

After import, the `RCCP_MIRROR` scripting symbol is added automatically by `RCCP_AddonDefineManager`. Both `MIRROR` (from Mirror) and `RCCP_MIRROR` (from the integration) must be present.

---

## Scripting Symbols

| Symbol | Source | Required |
|---|---|---|
| `MIRROR` | Mirror package (automatic) | Yes |
| `RCCP_MIRROR` | RCCP Mirror integration (automatic via `RCCP_AddonDefineManager`) | Yes |

---

## Setting Up a Networked Vehicle

### Step 1: Start with a Working RCCP Vehicle

Ensure your vehicle works correctly in single-player before adding networking. See [Vehicle Setup](03_vehicle_setup.md).

### Step 2: Add NetworkIdentity

1. Select your vehicle prefab.
2. Add Component: `NetworkIdentity`.
3. This is required by Mirror for all networked objects.

### Step 3: Add the RCCP Mirror Sync Component

1. Add the RCCP Mirror synchronization component (provided in the integration package) to your vehicle.
2. This component handles serialization of vehicle state across the network, similar to `RCCP_PhotonSync` in the Photon integration.

### Step 4: Register as a Spawnable Prefab

1. Open your scene's `NetworkManager` component.
2. In the **Registered Spawnable Prefabs** list, add your RCCP vehicle prefab.
3. This allows Mirror to instantiate the vehicle on remote clients.

### Step 5: Spawn and Register

On the server or host, spawn the vehicle using Mirror's API:

```csharp
GameObject vehicleObj = Instantiate(vehiclePrefab, spawnPosition, spawnRotation);
NetworkServer.Spawn(vehicleObj, connectionToClient);

RCCP_CarController vehicle = vehicleObj.GetComponent<RCCP_CarController>();
RCCP.RegisterPlayerVehicle(vehicle);
RCCP.SetControl(vehicle, true);
```

On each client, after the vehicle is spawned, check `isLocalPlayer` or `isOwned` to determine whether to enable local input or apply remote synchronization.

---

## Vehicle Synchronization

The RCCP Mirror sync component follows the same pattern as the Photon sync:

### Owner (Local Player)

- Inputs are processed locally by the RCCP drivetrain.
- Vehicle state is sent to the server (or directly to clients in host mode).
- No input overrides are applied.

### Remote (Other Players)

- Received state is applied via interpolation.
- Input and drivetrain modules are overridden with the received network state.
- Position and rotation are interpolated using `Rigidbody.MovePosition()` and `Rigidbody.MoveRotation()`.

### Synchronized Data

The integration synchronizes the same data categories as the Photon integration:

| Category | Fields |
|---|---|
| **Inputs** | throttle, brake, steer, handbrake, NOS boost, clutch |
| **Engine** | RPM, starting flag, running flag |
| **Gearbox** | current gear, gear input, gear state, shifting flag |
| **Differential** | left and right output for each differential |
| **Lights** | low beam, high beam, left indicator, right indicator, hazards |
| **Wheels** | RPM for each wheel collider |
| **Transform** | position, rotation, linear velocity, angular velocity |

---

## Authority Model

Mirror supports two authority models. The RCCP integration can work with either:

### Server-Authoritative (Recommended)

- All vehicle state is validated by the server.
- Clients send input commands to the server.
- The server applies inputs, simulates physics, and broadcasts the result.
- Provides better cheat protection.

### Client-Authoritative

- The owning client simulates physics locally and sends results to the server.
- The server trusts the client's state and relays it to other clients.
- Lower latency for the owning player but more vulnerable to cheating.

To configure authority, use Mirror's `[ClientAuthority]` attribute on sync variables or use `[Command]` methods for server-authoritative input processing.

---

## Network Manager Setup

A typical Mirror setup for RCCP requires:

1. **NetworkManager** -- Add to a persistent GameObject in your starting scene. Configure:
   - `Network Address`: `localhost` for testing, or a server IP for production.
   - `Transport`: Choose KCP (recommended), Telepathy, or another Mirror transport.
   - `Player Prefab`: Optionally set to an RCCP vehicle, or use a lobby system.
   - `Registered Spawnable Prefabs`: Add all RCCP vehicle prefabs that can be spawned.

2. **NetworkManagerHUD** (optional) -- Add alongside `NetworkManager` for quick testing. Provides Host, Client, and Server buttons.

---

## Common Issues

### Remote vehicles desync or jitter

- Check the sync interval. A higher sync rate produces smoother results but more bandwidth.
- Ensure interpolation is enabled on the sync component.
- Check server tick rate. Mirror's default tick rate may need tuning for vehicle physics.

### Authority errors ("not the owner")

- Only the object owner (or server) can modify sync variables.
- Check `isLocalPlayer` or `isOwned` before applying inputs.
- For enter-exit scenarios, transfer authority using `NetworkIdentity.AssignClientAuthority()`.

### Vehicle spawns but does not respond to input

- After spawning, verify that the local client has authority over the vehicle.
- Call `RCCP.RegisterPlayerVehicle()` and `RCCP.SetControl(vehicle, true)` on the local client.

### Compile errors after import

- Ensure both `MIRROR` and `RCCP_MIRROR` scripting symbols are defined.
- Verify that Mirror is fully imported (check for `Mirror` namespace availability).

### High bandwidth usage

- Reduce the sync rate in the Mirror sync component settings.
- Consider using unreliable channels for transform data (position, rotation, velocity).

---

## Removing the Integration

1. Delete the folder: `Assets/Realistic Car Controller Pro/Addons/Installed/Mirror/`.
2. Remove the `RCCP_MIRROR` scripting symbol from **Edit > Project Settings > Player > Scripting Define Symbols** (or let `RCCP_AddonDefineManager` handle it automatically).
3. Wait for Unity to recompile.

---

## Comparison: Mirror vs. Photon

| Feature | Mirror | Photon PUN 2 |
|---|---|---|
| **Cost** | Free, open-source | Free tier (20 CCU), paid tiers for more |
| **Hosting** | Self-hosted or dedicated server | Photon Cloud (managed) |
| **Authority** | Server-authoritative by default | Owner-authoritative (client sends state) |
| **Transport** | Pluggable (KCP, Telepathy, Steam, etc.) | Photon proprietary |
| **LAN Support** | Yes, built-in | Limited (Photon Cloud only in free tier) |
| **Ease of Setup** | Moderate (requires server management) | Easy (cloud-hosted, just need App ID) |
| **Scalability** | Depends on server hardware | Scales via Photon Cloud pricing tiers |

---

## See Also

- [Vehicle Setup](03_vehicle_setup.md) -- Creating an RCCP vehicle from scratch.
- [API Reference](16_api_reference.md) -- `RCCP.RegisterPlayerVehicle()`, `RCCP.SetControl()`.
- [Integration: Photon PUN 2](21_integration_photon.md) -- Alternative networking with Photon.
- [Integration: Enter-Exit](20_integration_enter_exit.md) -- Character enter-exit system.

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

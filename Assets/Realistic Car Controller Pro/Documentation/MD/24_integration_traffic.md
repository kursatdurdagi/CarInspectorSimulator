# Integration: Realistic Traffic Controller

Realistic Traffic Controller (RTC) is a separate BoneCracker Games product that provides AI-driven traffic simulation with lane-following, traffic lights, intersections, and vehicle spawning. The RCCP integration package allows RTC traffic vehicles to use the RCCP drivetrain, and enables the player's RCCP vehicle to coexist with RTC's traffic system.

---

## Overview

Realistic Traffic Controller manages:

- **Traffic lanes:** Spline-based road networks that AI vehicles follow.
- **Traffic lights:** Intersection signal controllers with configurable phases.
- **Traffic spawner:** Spawns and despawns traffic vehicles based on distance from the player.
- **Traffic AI:** Lane keeping, speed limits, braking, overtaking, and obstacle avoidance.

The RCCP integration bridges the two systems so that:

- RCCP vehicles can be used as traffic participants driven by RTC's AI.
- The player's RCCP vehicle is recognized by RTC's traffic system (traffic vehicles avoid it, stop behind it, etc.).
- Traffic vehicles use RCCP's physics and drivetrain for realistic movement.

---

## Prerequisites

1. **Realistic Traffic Controller** installed in your project. Purchase from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/behavior-ai/realistic-traffic-controller-258961).
2. Realistic Car Controller Pro installed and working.

---

## Installing

### Step 1: Import Realistic Traffic Controller

If RTC is not already in your project, purchase and import it from the Asset Store.

### Step 2: Import the RCCP Integration Package

1. Open the RCCP Welcome Window: **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.
2. Navigate to the **Addons** tab.
3. Under **Realistic Traffic Controller**, you will see two buttons:
   - **Download and import Realistic Traffic Controller** -- Opens the RTC Asset Store page.
   - **Import Integration For Realistic Traffic Controller** -- Imports the RCCP integration package.
4. Click **Import Integration For Realistic Traffic Controller**.
5. Unity imports `RCCP_RealisticTrafficControllerIntegration.unitypackage` from `Addons/Installers/`.
6. Wait for Unity to recompile.

The installer package is located at:

```
Assets/Realistic Car Controller Pro/Addons/Installers/RCCP_RealisticTrafficControllerIntegration.unitypackage
```

After import, the integration files are installed to:

```
Assets/Realistic Car Controller Pro/Addons/Installed/Realistic Traffic Controller/
```

---

## Scripting Symbol

The RTC integration uses the `BCG_RTRC` scripting symbol. This is typically set automatically when RTC and the integration package are both present.

| Symbol | Source | Required |
|---|---|---|
| `BCG_RTRC` | RTC integration (automatic) | Yes |

The `BCG_ENTEREXIT` vehicle script also references `BCG_RTC` for camera detection when RTC vehicles are used with the enter-exit system.

---

## Setup Guide

### Step 1: Set Up the Traffic Network

Follow RTC's documentation to create your traffic lane network:

1. Create road splines using RTC's road editor.
2. Set up intersections with traffic light controllers.
3. Configure lane speed limits and connectivity.
4. Place the RTC traffic spawner in the scene.

### Step 2: Configure RCCP Vehicles as Traffic Participants

The integration allows RCCP vehicle prefabs to be used as traffic vehicles:

1. Create RCCP vehicle prefabs that will serve as traffic vehicles.
2. Register them with RTC's vehicle pool (in the RTC traffic spawner configuration).
3. The integration component bridges RTC's AI input to RCCP's input system, so the traffic AI controls throttle, brake, and steering through RCCP's drivetrain.

### Step 3: Configure the Player Vehicle

The player's RCCP vehicle must be recognized by the traffic system:

1. Ensure the player vehicle is on the correct physics layer.
2. RTC's traffic AI uses raycasting and layer-based detection to identify obstacles. The player vehicle's layer must be included in RTC's obstacle detection layers.
3. Configure RTC's settings to treat the player's layer as a "vehicle" obstacle.

### Step 4: Layer Configuration

Proper layer setup is critical for traffic interaction:

| Layer Purpose | Description |
|---|---|
| Traffic vehicles | Layer assigned to RTC-managed traffic vehicles. |
| Player vehicle | Layer assigned to the player's RCCP vehicle. |
| Road/terrain | Layer for road surfaces and terrain. |

Both traffic and player vehicle layers must be included in each other's collision and detection matrices. Configure this in **Edit > Project Settings > Physics** (Layer Collision Matrix).

---

## Features

### Traffic AI Behavior

When using RCCP vehicles as traffic participants, the traffic AI:

- Follows assigned traffic lanes at configured speed limits.
- Obeys traffic light signals (stops at red, proceeds on green).
- Brakes when approaching slower vehicles or obstacles.
- Detects and avoids the player vehicle.
- Uses RCCP's steering, throttle, and brake inputs through the integration bridge.

### Traffic Spawning and Despawning

RTC's traffic spawner creates and destroys traffic vehicles based on distance from a reference point (usually the player camera):

- Vehicles are spawned on lanes within the spawn radius.
- Vehicles beyond the despawn radius are removed.
- This keeps the active vehicle count manageable for performance.

### Traffic Lights

RTC manages traffic light phases at intersections. Traffic vehicles respect signals automatically. The player is not forced to stop at red lights (no gameplay enforcement), but traffic vehicles will stop, creating realistic intersection behavior.

---

## Common Issues

### Traffic not spawning

- **Check RTC setup.** Ensure the RTC traffic spawner is in the scene and configured with vehicle prefabs.
- **Check lanes.** Traffic vehicles need valid lane splines to follow. Verify that lanes are properly connected.
- **Check spawn distance.** If the spawn radius is too small or the player is not near any lanes, no traffic will appear.
- **Check the integration.** Ensure the integration package is imported and the scripting symbol is defined.

### Traffic colliding with the player

- **Check layer setup.** The player vehicle and traffic vehicles must be on layers that are included in each other's detection masks.
- **Check physics layers.** In **Edit > Project Settings > Physics**, ensure the traffic and player layers can collide.
- **Check RTC obstacle detection.** RTC's AI uses raycasts to detect obstacles. The player vehicle's layer must be in the obstacle detection layer mask.

### Traffic vehicles not moving

- **Check RCCP integration.** The integration component must be present on traffic vehicle prefabs to bridge RTC's AI commands to RCCP's input system.
- **Check vehicle setup.** Each traffic vehicle must be a valid RCCP vehicle (with `RCCP_CarController`, engine, gearbox, etc.).
- **Check `canControl`.** RCCP vehicles default to `canControl = false` in some configurations. The integration should set this appropriately.

### Traffic vehicles driving through red lights

- **Check traffic light configuration.** Verify that RTC's traffic light controllers are properly assigned to their intersection lanes.
- **Check stop line positions.** RTC uses stop line positions to determine where vehicles should wait at red lights.

### Performance issues with many traffic vehicles

- Reduce the maximum traffic vehicle count in RTC's spawner settings.
- Reduce the spawn/despawn radius.
- Use simpler RCCP vehicle configurations for traffic (fewer components, simpler physics).
- Consider disabling unnecessary RCCP features on traffic vehicles (e.g., damage, customization).

---

## Removing the Integration

1. Delete the folder: `Assets/Realistic Car Controller Pro/Addons/Installed/Realistic Traffic Controller/`.
2. If the `BCG_RTRC` scripting symbol is still defined, remove it from **Edit > Project Settings > Player > Scripting Define Symbols**.
3. Wait for Unity to recompile.

Note: Removing the integration does not remove RTC itself. RTC will continue to work independently; it simply will not use RCCP vehicles as traffic participants.

---

## See Also

- [Vehicle Setup](03_vehicle_setup.md) -- Creating RCCP vehicles (also for traffic vehicle prefabs).
- [AI Vehicles](13_ai_vehicles.md) -- RCCP's built-in AI vehicle navigation (separate from RTC traffic).
- [Settings](04_settings.md) -- Global RCCP settings.
- [Integration: Enter-Exit](20_integration_enter_exit.md) -- Character enter-exit system (compatible with RTC vehicles via `BCG_RTC` symbol).

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

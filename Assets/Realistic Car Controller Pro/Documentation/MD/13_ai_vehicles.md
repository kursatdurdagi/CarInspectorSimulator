# AI Vehicles

RCCP includes a waypoint-based AI driving system built on Unity's NavMesh pathfinding. AI vehicles follow predefined waypoint paths, avoid obstacles dynamically, and respect brake zones for speed reduction at corners and intersections. The system supports four distinct behavior modes and requires no player input to operate.

**Prerequisites:** Before setting up AI vehicles, you should have a working vehicle with all core components as described in [Vehicle Setup](03_vehicle_setup.md). You also need a baked NavMesh in your scene (Window > AI > Navigation).


## How the AI System Works

The AI system uses three main components working together:

| Component | Purpose | Location |
|---|---|---|
| `RCCP_AI` | Main AI driver that computes steering, throttle, and braking | Attached to the vehicle |
| `RCCP_AIWaypointsContainer` | Holds the ordered list of waypoints that define a driving path | Scene object |
| `RCCP_AIBrakeZone` | Trigger volume that tells AI vehicles to slow to a target speed | Scene object |

The AI driver relies on a `NavMeshAgent` for pathfinding. When the `RCCP_AI` component is added or enabled, it automatically creates a child `NavMeshAgent` if one does not already exist. The agent does not move the vehicle directly. Instead, it calculates a path that the AI uses to compute steering and speed inputs.

Every `FixedUpdate`, the AI:

1. Updates its NavMesh destination based on the current behavior mode
2. Computes steering using a look-ahead point along the path
3. Calculates a safe cornering speed from the tightest turn radius ahead
4. Applies PID speed control for smooth throttle and braking
5. Checks for stuck conditions and recovers automatically
6. Applies obstacle avoidance adjustments from `RCCP_AIDynamicObstacleAvoidance`


## Behavior Modes

The `RCCP_AI` component supports four behavior modes, set via the `behaviour` field in the Inspector:

| Mode | Description |
|---|---|
| **FollowWaypoints** | Loops through waypoints at moderate speed. Good for traffic or patrol vehicles. |
| **RaceWaypoints** | Races through waypoints more aggressively with an extended look-ahead distance for smoother cornering at speed. |
| **FollowTarget** | Follows a target Transform at a set distance. Stops when within range. Requires the `target` field to be assigned. |
| **ChaseTarget** | Chases and intercepts a target using velocity prediction. More aggressive than FollowTarget. Requires the `target` field to be assigned. |


## Setting Up an AI Vehicle

### Step 1: Prepare the Vehicle

Start with a fully configured RCCP vehicle that has `RCCP_CarController` and all required drivetrain components (engine, gearbox, differential, axles, wheel colliders). See [Vehicle Setup](03_vehicle_setup.md) for details.

### Step 2: Add RCCP_OtherAddons

If the vehicle does not already have the `RCCP_OtherAddons` component, add it. The AI component is managed through this addons system. You can add it via:

- Inspector: **Add Component > BoneCracker Games > Realistic Car Controller Pro > Other Addons > RCCP Other Addons**

### Step 3: Add RCCP_AI

Add the AI component to the vehicle:

- Inspector: **Add Component > BoneCracker Games > Realistic Car Controller Pro > AI > RCCP AI**

When the component is added, it automatically creates a child `NavMeshAgent` GameObject. When the AI enables at runtime, it sets `CarController.externalControl = true`, which overrides player input so the AI has full control of the vehicle.

### Step 4: Bake a NavMesh

The AI uses Unity's NavMesh for pathfinding. Open **Window > AI > Navigation** and bake a NavMesh for your scene. Make sure the NavMesh covers the roads and areas where AI vehicles will drive.

### Step 5: Assign a Waypoint Container

Drag a `RCCP_AIWaypointsContainer` from your scene into the `waypointsContainer` field on the `RCCP_AI` component. If you do not assign one, the AI will attempt to find one in the scene automatically at runtime.


## Key Properties

### Driving Settings

| Property | Type | Default | Description |
|---|---|---|---|
| `behaviour` | BehaviourType | RaceWaypoints | AI behavior mode (see table above) |
| `waypointsContainer` | RCCP_AIWaypointsContainer | null | Reference to the waypoint path |
| `target` | Transform | null | Target for FollowTarget and ChaseTarget modes |
| `maxThrottle` | float (0-1) | 1.0 | Maximum throttle input |
| `maxBrake` | float (0-1) | 1.0 | Maximum brake input |
| `agressiveness` | float (0-3) | 2.0 | Driving aggressiveness factor; higher values allow later braking |
| `steerSensitivity` | float (0-5) | 3.0 | Steering sensitivity multiplier |
| `roadGrip` | float | 1.1 | Friction coefficient used to calculate safe cornering speed |

### Waypoint Settings

| Property | Type | Default | Description |
|---|---|---|---|
| `waypointReachThreshold` | float | 25 | Distance in meters at which a waypoint is considered reached |
| `raceLookAhead` | float | 36 | Additional look-ahead distance in meters for RaceWaypoints mode |

### Steering Look-ahead

| Property | Type | Default | Description |
|---|---|---|---|
| `minLookAhead` | float | 5 | Minimum look-ahead distance in meters when stationary |
| `lookAheadPerKph` | float | 0.25 | Additional look-ahead meters per km/h of current speed |

### PID Speed Control

| Property | Type | Default | Description |
|---|---|---|---|
| `kp` | float | 0.2 | Proportional gain for speed control |
| `ki` | float | 0.01 | Integral gain for speed control |
| `kd` | float | 0.02 | Derivative gain for speed control |

### Target Following

| Property | Type | Default | Description |
|---|---|---|---|
| `followTargetDistance` | float | 5 | Distance to maintain behind target in FollowTarget mode |
| `chasePredictionTime` | float | 1 | Prediction time in seconds for intercepting targets in ChaseTarget mode |

### State

| Property | Type | Default | Description |
|---|---|---|---|
| `stopNow` | bool | false | Forces the AI to stop immediately |
| `reverseNow` | bool | false | Forces the AI to reverse |
| `checkStuck` | bool | true | Enables stuck detection and automatic recovery |


## Waypoint System

### RCCP_AIWaypointsContainer

This component holds an ordered list of `RCCP_Waypoint` child objects that define the AI driving path. The AI follows waypoints sequentially and loops back to the first waypoint after reaching the last one.

**Creating a waypoint container:**

Use the menu: **Tools > BoneCracker Games > Realistic Car Controller Pro > Create > Scene > Add AI Waypoints Container**

This creates a new GameObject with the `RCCP_AIWaypointsContainer` component.

### RCCP_Waypoint

Each waypoint is a child GameObject of the container with the `RCCP_Waypoint` component. Waypoints define the positions the AI will drive through. Position them along the road where you want the AI to drive.

### Waypoint Container Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `smoothingSubdivisions` | int | 5 | Number of subdivisions per segment when smoothing the path |
| `groundOffset` | float | 1.0 | Height above ground to place each waypoint during ground snapping |
| `groundLayerMask` | LayerMask | Everything | Layers to include when raycasting for ground |

### Waypoint Container Context Menu Tools

Right-click the `RCCP_AIWaypointsContainer` component in the Inspector to access these tools:

- **Smooth Waypoints** -- Applies Catmull-Rom spline interpolation to create a smoother curve through the waypoints. Replaces existing waypoints with new interpolated positions.
- **Place Waypoints Above Ground** -- Raycasts downward from each waypoint and repositions them at a uniform height above the ground surface.

### Tips for Good Waypoint Placement

- Place waypoints at regular intervals along your road
- Add extra waypoints at corners for smoother turns
- Keep waypoints centered on the road
- Use the **Smooth Waypoints** context menu for a more natural driving line
- Use **Place Waypoints Above Ground** after adjusting terrain


## Brake Zones

### RCCP_AIBrakeZone

A brake zone is a trigger volume that tells AI vehicles to reduce speed. When an AI vehicle enters a brake zone, it limits its target speed to the zone's `targetSpeed` value. When the vehicle exits, normal speed calculation resumes.

| Property | Type | Range | Default | Description |
|---|---|---|---|---|
| `targetSpeed` | float | 5 - 360 | 50 | Target speed in km/h that AI vehicles should not exceed inside this zone |

**Creating brake zones:**

Use the menu: **Tools > BoneCracker Games > Realistic Car Controller Pro > Create > Scene > Add AI Brake Zones Container**

This creates a `RCCP_AIBrakeZonesContainer` with a `BoxCollider` (set as trigger). Each child brake zone also requires a `BoxCollider` marked as trigger.

### RCCP_AIBrakeZonesContainer

This is a parent container that holds multiple `RCCP_AIBrakeZone` children. It draws red gizmos in the Scene view to visualize each brake zone's bounds. At runtime it automatically sets brake zone GameObjects to the "Ignore Raycast" layer.

### Placing Brake Zones

- Place brake zones before sharp turns where the AI should slow down
- Place brake zones at intersections or narrow passages
- Scale the `BoxCollider` to cover the area where braking should begin
- Set the `targetSpeed` low enough for the AI to safely navigate the turn


## Obstacle Avoidance

### RCCP_AIDynamicObstacleAvoidance

This optional component provides dynamic obstacle detection and avoidance. It works alongside `RCCP_AI` by contributing additional steering input when obstacles are detected.

**How it works:**

1. Detects obstacles within a configurable radius using `Physics.OverlapSphere`
2. Samples obstacle bounds with raycasts to find actual threat points
3. Assesses collision risk using trajectory prediction
4. Calculates a safe avoidance direction (left or right) based on clearance
5. Applies steering and braking inputs proportional to threat urgency

**Key properties:**

| Property | Type | Default | Description |
|---|---|---|---|
| `autoDetectObstacles` | bool | true | Automatically detect obstacles from scene colliders |
| `dynamicObstacleLayers` | LayerMask | -- | Layers considered for automatic detection |
| `detectionRadius` | float | 10 | Maximum detection radius in meters |
| `predictionTime` | float | 1 | Seconds to look ahead when predicting positions |
| `riskThreshold` | float | 0.3 | Minimum risk level (0-1) to trigger avoidance |
| `vehicleWidth` | float | 2 | Vehicle width for collision prediction |
| `vehicleLength` | float | 4 | Vehicle length for collision prediction |
| `safetyMargin` | float | 0.5 | Safety margin added to vehicle dimensions |
| `emergencyBrakeDistance` | float | 2 | Distance at which maximum braking is applied |

**Adding and removing obstacles manually:**

```csharp
RCCP_AIDynamicObstacleAvoidance avoidance = vehicle.GetComponent<RCCP_AIDynamicObstacleAvoidance>();

// Add a specific obstacle to track
avoidance.AddObstacle(obstacleTransform);

// Remove an obstacle from tracking
avoidance.RemoveObstacle(obstacleTransform);
```


## Spawning AI Vehicles from Code

Use the `RCCP.SpawnRCC` static method to instantiate AI vehicles at runtime. For AI vehicles, set `registerAsPlayerVehicle` to `false` and `isControllable` to `false`:

```csharp
// Spawn an AI vehicle
RCCP_CarController aiVehicle = RCCP.SpawnRCC(
    aiPrefab,        // Vehicle prefab with RCCP_AI component
    spawnPos,        // World position
    spawnRot,        // World rotation
    false,           // Do not register as player vehicle
    false,           // Not controllable by player
    true             // Start with engine running
);
```

The vehicle must have the `RCCP_AI` component on the prefab. When it spawns and `OnEnable` runs, the AI detects the `RCCP_AI` component and fires `RCCP_Events.OnRCCPAISpawned` instead of the normal player spawn event.

### Changing AI Behavior at Runtime

```csharp
RCCP_AI ai = aiVehicle.OtherAddonsManager.AI;

// Switch to follow a target
ai.behaviour = RCCP_AI.BehaviourType.FollowTarget;
ai.target = playerVehicle.transform;

// Force stop
ai.stopNow = true;

// Reset the AI state
ai.Reload();
```


## Events

The RCCP event system provides two AI-specific events:

| Event | Signature | Fired When |
|---|---|---|
| `RCCP_Events.OnRCCPAISpawned` | `onRCCPAISpawned(RCCP_CarController)` | An AI vehicle is enabled (has `RCCP_AI` component on `OtherAddonsManager`) |
| `RCCP_Events.OnRCCPAIDestroyed` | `onRCCPAIDestroyed(RCCP_CarController)` | An AI vehicle is disabled or destroyed |

**Subscribing to AI events:**

```csharp
void OnEnable() {
    RCCP_Events.OnRCCPAISpawned += OnAISpawned;
    RCCP_Events.OnRCCPAIDestroyed += OnAIDestroyed;
}

void OnDisable() {
    RCCP_Events.OnRCCPAISpawned -= OnAISpawned;
    RCCP_Events.OnRCCPAIDestroyed -= OnAIDestroyed;
}

void OnAISpawned(RCCP_CarController aiVehicle) {
    Debug.Log("AI vehicle spawned: " + aiVehicle.name);
}

void OnAIDestroyed(RCCP_CarController aiVehicle) {
    Debug.Log("AI vehicle destroyed: " + aiVehicle.name);
}
```

The distinction between player and AI spawn events is automatic. If the vehicle has an `RCCP_AI` component registered through `OtherAddonsManager`, it fires the AI events. Otherwise, it fires the standard player events. See [API Reference](16_api_reference.md) for the full event reference.


## Stuck Detection and Recovery

When `checkStuck` is enabled (the default), the AI monitors whether the vehicle is stuck. If the vehicle has been applying throttle but moving below 2 km/h for more than 2 seconds, it triggers a recovery routine that:

1. Temporarily enables `autoReverse` on the input component
2. Reverses for 1.5 seconds
3. Restores normal settings and shifts back to first gear


## Scene View Gizmos

When the game is playing, `RCCP_AI` draws helpful gizmos in the Scene view:

- **Green line** -- From the vehicle to its current NavMesh destination
- **Cyan sphere** -- The current target waypoint
- **Cyan path** -- The NavMesh path corners
- **Magenta sphere** -- The steering look-ahead point
- **Text label** -- Shows current behavior mode and speed in km/h

Brake zone containers draw semi-transparent **red boxes** showing each brake zone's bounds.


## Common Issues

| Problem | Possible Cause | Solution |
|---|---|---|
| AI not moving | No waypoint container assigned | Assign a `RCCP_AIWaypointsContainer` to the `waypointsContainer` field |
| AI not moving | NavMesh not baked | Bake a NavMesh covering the driving area (Window > AI > Navigation) |
| AI not moving | Missing drivetrain components | Verify the vehicle has engine, gearbox, differential, and axles |
| AI driving off the road | Not enough waypoints | Add more waypoints along the road, especially at curves |
| AI not braking at turns | No brake zones placed | Add `RCCP_AIBrakeZone` triggers before sharp turns |
| AI jittering at waypoints | `waypointReachThreshold` too small | Increase the threshold so the AI transitions smoothly between waypoints |
| AI cutting corners | `raceLookAhead` too high | Reduce the look-ahead value or switch to FollowWaypoints mode |
| AI colliding with obstacles | No obstacle avoidance component | Add `RCCP_AIDynamicObstacleAvoidance` and configure its `dynamicObstacleLayers` |
| AI stuck at an obstacle | Stuck detection disabled | Enable `checkStuck` on the `RCCP_AI` component |

---

**Related topics:** [Vehicle Setup](03_vehicle_setup.md) | [Settings](04_settings.md) | [Inputs](05_inputs.md) | [Recording and Playback](14_recording_playback.md)

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

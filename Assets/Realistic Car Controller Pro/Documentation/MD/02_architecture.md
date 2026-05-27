# Architecture Overview

This guide explains how Realistic Car Controller Pro (RCCP) is built under the hood. Understanding the architecture will help you work with the asset more confidently, whether you are setting up your first vehicle or building a full driving game.

## How RCCP Works

RCCP is a **component-based vehicle physics system**. Instead of putting all vehicle logic into a single massive script, RCCP splits every subsystem (engine, gearbox, wheels, stability, lights, damage, and more) into its own independent component.

Here is the key idea: every RCCP vehicle has one main controller called `RCCP_CarController` on the root GameObject. All other subsystems live on child GameObjects and automatically register themselves with that controller when they initialize. You do not need to manually wire up references between components -- RCCP handles it for you.

This modular design means you can:

- Add or remove features by adding or removing components (want NOS? add the component. Don't need damage? remove it)
- Understand one system at a time without being overwhelmed by the whole vehicle
- Extend the vehicle with your own scripts by listening to events and reading public properties

## Component System

### The Base Class Family Tree

Every script in RCCP inherits from one of a few base classes. Think of it like a family tree where each branch serves a different purpose:

```
MonoBehaviour (Unity's built-in base)
|
|-- RCCP_GenericComponent
|   |   Base for global managers (not attached to vehicles).
|   |   Provides cached access to Settings, GroundMaterials, SceneManager.
|   |
|   +-- RCCP_Singleton<T>
|       Thread-safe singleton pattern. Auto-creates a GameObject
|       if no instance exists in the scene.
|
|-- RCCP_MainComponent
|   Base for RCCP_CarController itself.
|   Caches references to ALL vehicle subsystem components.
|
|-- RCCP_Component (implements IRCCP_Component)
|   |   Base for ALL vehicle subsystems (engine, gearbox, wheels, etc.).
|   |   Auto-discovers the parent RCCP_CarController and registers itself.
|   |
|   +-- RCCP_UpgradeComponent
|       Base for customization/upgrade components (paint, spoiler, wheels).
|       Has access to the vehicle's customization Loadout.
|
|-- RCCP_UIComponent
    Base for UI components (dashboard displays, buttons, sliders).
    Provides cached access to Settings and SceneManager.
```

**All six base classes are `abstract` in V2.31.1+.** This means `RCCP_GenericComponent`, `RCCP_Singleton<T>`, `RCCP_MainComponent`, `RCCP_Component`, `RCCP_UpgradeComponent`, and `RCCP_UIComponent` no longer appear in Unity's **Add Component** menu, and direct instantiation is a compile error -- calls like `AddComponent<RCCP_Component>()` or `new RCCP_GenericComponent()` will not build. Always use a concrete subclass instead (`RCCP_Engine`, `RCCP_Gearbox`, `RCCP_Stability`, and so on). This was a deliberate change to prevent accidental misuse, since the base classes have no behavior on their own.

**Why does this matter?** When you look at any RCCP script, knowing which base class it uses tells you immediately what kind of component it is and what it has access to.

### Auto-Registration

When any component that inherits from `RCCP_Component` initializes, it automatically:

1. Finds the `RCCP_CarController` on its parent GameObject using `GetComponentInParent`
2. Calls the `Register()` method
3. `Register()` uses a type-switch to assign the component to the correct slot on the car controller

For example, when `RCCP_Engine` initializes, the registration assigns it to `CarController.Engine`. When `RCCP_Gearbox` initializes, it goes to `CarController.Gearbox`. This happens automatically -- you never need to drag-and-drop references in the Inspector.

Here is a simplified view of how registration works internally:

```csharp
// Inside RCCP_Component.Register() -- simplified for clarity
switch (component) {
    case RCCP_Engine:
        carController.Engine = component as RCCP_Engine;
        break;
    case RCCP_Clutch:
        carController.Clutch = component as RCCP_Clutch;
        break;
    case RCCP_Gearbox:
        carController.Gearbox = component as RCCP_Gearbox;
        break;
    // ... and so on for every component type
}
```

### Registered Component Types

Here is the full list of components that auto-register with the car controller:

**Core Drivetrain:**

| Component | Slot on CarController | Purpose |
|-----------|----------------------|---------|
| RCCP_Engine | .Engine | Power generation |
| RCCP_Clutch | .Clutch | Engine-to-gearbox connection |
| RCCP_Gearbox | .Gearbox | Gear ratios and shifting |
| RCCP_Differential | (updates differentials list) | Torque splitting between wheels |
| RCCP_Axles | .AxleManager | Manages all axle groups |
| RCCP_Axle | (added to AxleManager.Axles list) | Groups left and right wheels |
| RCCP_WheelCollider | (managed by parent Axle) | Individual wheel physics |

**Vehicle Systems:**

| Component | Slot on CarController | Purpose |
|-----------|----------------------|---------|
| RCCP_AeroDynamics | .AeroDynamics | Downforce and drag |
| RCCP_Audio | .Audio | Engine, skid, crash sounds |
| RCCP_Input | .Inputs | Input processing for this vehicle |
| RCCP_Lights | .Lights | Light group manager |
| RCCP_Light | (registered with .Lights) | Individual light (headlight, brake, etc.) |
| RCCP_Stability | .Stability | ABS, ESP, TCS, steering helpers |
| RCCP_Damage | .Damage | Mesh deformation and repair |
| RCCP_Particles | .Particles | Wheel smoke, sparks, debris |
| RCCP_Customizer | .Customizer | Paint, upgrades, loadout system |
| RCCP_Lod | .LOD | Level-of-detail optimization |

**Addon Components** (registered via the OtherAddons manager):

| Component | Slot on OtherAddonsManager | Purpose |
|-----------|---------------------------|---------|
| RCCP_Recorder | .Recorder | Record and replay vehicle movement |
| RCCP_Exhausts | .Exhausts | Exhaust flame and smoke effects |
| RCCP_Limiter | .Limiter | Speed limiter |
| RCCP_Nos | .Nos | Nitrous oxide boost |
| RCCP_TrailerAttacher | .TrailAttacher | Trailer hitch connection |
| RCCP_Visual_Dashboard | .Dashboard | 3D dashboard gauges |
| RCCP_Exterior_Cameras | .ExteriorCameras | Hood cam, bumper cam, etc. |
| RCCP_AI | .AI | Waypoint-following AI driver |
| RCCP_WheelBlur | .WheelBlur | Motion blur effect on spinning wheels |
| RCCP_FuelTank | .FuelTank | Fuel consumption system |
| RCCP_BodyTilt | .BodyTilt | Body lean in corners |

## Drivetrain Chain

Power flows through the vehicle in a chain, just like a real car. Each component in the chain receives input from the previous one and passes its output to the next:

```
  Throttle Input
       |
       v
  [1. ENGINE] ----> Generates torque based on RPM and throttle
       |
       v
  [2. CLUTCH] ----> Connects or disconnects engine from gearbox
       |
       v
  [3. GEARBOX] ---> Multiplies torque by current gear ratio
       |
       v
  [4. DIFFERENTIAL] -> Splits torque between left and right wheels
       |
       v
  [5. AXLE] -------> Groups left and right wheels together
       |
       v
  [6. WHEEL COLLIDERS] -> Apply torque to Unity's physics system
```

### Step-by-Step Breakdown

**1. RCCP_Engine** -- The power plant. It generates torque based on the current RPM and throttle input. The engine has a torque curve (configurable in the Inspector) that determines how much power is available at each RPM. Think of it as the heart of the vehicle.

**2. RCCP_Clutch** -- Connects (or disconnects) the engine from the gearbox, just like pressing the clutch pedal in a real manual car. When the clutch is disengaged, the engine spins freely without driving the wheels. This matters during gear shifts and when starting from a stop.

**3. RCCP_Gearbox** -- Multiplies the engine's torque by the current gear ratio. Lower gears provide more torque (for acceleration), while higher gears allow higher top speed. Three transmission modes are available:

| Mode | Description |
|------|-------------|
| Manual | Player shifts gears manually |
| Automatic | Gears shift automatically based on RPM thresholds |
| Automatic_DNRP | Semi-automatic with a selector for Drive, Neutral, Reverse, and Park |

**4. RCCP_Differential** -- Splits torque between the left and right wheels on an axle. Without a differential, both wheels would always spin at the same speed, making cornering unrealistic. Four differential types are available:

| Type | Behavior |
|------|----------|
| Open | Sends more torque to the wheel with less resistance (realistic, but can cause wheelspin) |
| Limited | Partially locks when one wheel spins faster than the other |
| FullLocked | Both wheels always receive equal torque (great for off-road, causes understeer on pavement) |
| Direct | Torque is applied directly without differential calculations |

The differential also applies the **final drive ratio**, which is one last gear reduction before the wheels.

**5. RCCP_Axle** -- Groups a left and right wheel together. Each axle has flags that control its role:

| Flag | Meaning |
|------|---------|
| isPower | This axle receives engine power |
| isSteer | This axle turns with steering input |
| isBrake | This axle applies braking force |
| isHandbrake | This axle responds to the handbrake |

**Important:** The `isPower` flag is set automatically by the Differential component based on which axles it is connected to. You do not set this flag manually. The Differential determines whether the vehicle is FWD, RWD, or AWD by which axles receive its output.

**6. RCCP_WheelCollider** -- A wrapper around Unity's built-in WheelCollider. Handles ground contact detection, friction calculation, slip, and aligns the visual wheel model (the 3D mesh you see) with the physics wheel's position and rotation.

### How Speed Is Calculated

The vehicle's speed is calculated from the **Rigidbody's velocity** (converted to km/h), not from wheel RPM. This gives an accurate reading even when wheels are spinning or locked:

```csharp
// From RCCP_CarController -- simplified
speed = transform.InverseTransformDirection(Rigid.linearVelocity).z * 3.6f;
```

A separate `wheelRPM2Speed` value is also calculated from wheel RPM for drivetrain calculations, but the main `speed` property uses physics velocity.

## Execution Order

Unity normally runs all scripts in an undefined order, which can cause problems when one script depends on another being initialized first. RCCP enforces its ordering automatically through `RCCP_ScriptExecutionOrderManager`, an editor-only `[InitializeOnLoad]` helper that writes the correct `MonoImporter` execution order for every RCCP script on every assembly reload. Every order-sensitive class also carries a matching `[DefaultExecutionOrder]` attribute baked directly onto the type, so the ordering survives projects where the editor enforcement helper is missing.

Scripts with lower numbers run first:

| Order | Component | Why It Runs at This Priority |
|-------|-----------|------------------------------|
| -50 | RCCP_SceneManager | Must track vehicles and cache terrain data before anything else |
| -50 | RCCP_InputManager | Must process input before vehicles try to read it |
| -50 | RCCP_SkidmarksManager | Shared rendering system must be ready before wheels query it |
| -11 | RCCP_AI | Writes `Inputs.OverrideInputs(...)` before CarController.PlayerInputs reads at -10 |
| -11 | RCCP_Recorder | Same; writes recorded input snapshot before CarController reads it |
| -10 | RCCP_CarController | Main vehicle controller — polls component state and forwards inputs |
| -8 | RCCP_Limiter | Writes `Engine.cutFuel` before Engine consumes it the same frame |
| -7 | RCCP_Engine | Starts the drivetrain torque chain (`Output()` → `Clutch.ReceiveOutput`) |
| -6 | RCCP_Clutch | Reads stored received torque, applies clutch slip, forwards to Gearbox |
| -5 | RCCP_Axles | Axle manager collects axles before other components need wheel data |
| -5 | RCCP_Lights | Light manager ready before individual lights register |
| -5 | RCCP_OtherAddons | Addon manager ready before addon components register |
| -5 | RCCP_Exhausts | Exhaust system initializes before particle effects |
| -5 | RCCP_Gearbox | Drivetrain stage between Clutch and Differential (parents at -5 have no FixedUpdate, so tying is collision-free) |
| -4 | RCCP_Differential | Splits torque to L/R wheels and stores motor torque on the connected `RCCP_Axle` |
| -3 | RCCP_AeroDynamics | Writes `Rigidbody.centerOfMass` and `linearDamping` before WheelCollider physics consumes them |
| -2 | RCCP_Axle | Populates wheel motor/brake torque accumulators via `AddMotorTorque` |
| -1 | RCCP_Stability | Modifies accumulators (ESP brake add + motor cut) before wheels read them |
| 0 | RCCP_WheelCollider | Consumes the accumulators, applies values to Unity's `WheelCollider`, resets for next frame |
| 5 | RCCP_Camera | Camera follows vehicle after it has updated its position |
| 10 | RCCP_Customizer | Late initialization for customization system |
| 10 | RCCP_Lod | LOD decisions made after all other updates |
| 10 | RCCP_BodyTilt | Body tilt applied after physics update |

### The Drivetrain Chain (-7 → -4) and the Accumulator Pipeline (-2 → -1 → 0)

RCCP's drivetrain components don't propagate torque synchronously. Each upstream component's `Output()` calls `outputEvent.Invoke(...)`; the downstream component's `ReceiveOutput()` simply stores the received torque to a field. The downstream's own `FixedUpdate` reads that stored field, recomputes, and re-fires its own `Output()`. The chain therefore advances **one stage per fixed frame** unless the components run in upstream-to-downstream order within the same frame. With Engine at -7, Clutch at -6, Gearbox at -5, and Differential at -4, the entire chain (Engine → Clutch → Gearbox → Differential → Axle.ReceiveOutput) completes in a single fixed frame.

The `-2 → -1 → 0` sequence at the wheel end is load-bearing. `RCCP_Axle` writes motor and brake torque into per-wheel accumulators at `-2`. `RCCP_Stability` then modifies those accumulators at `-1` — ESP can add differential brake torque and cut motor torque on a driven wheel in the same frame. Finally, `RCCP_WheelCollider` consumes the accumulators at `0` and pushes the final values into Unity's `WheelCollider` API. Without this strict ordering, motor torque and ESP brake torque would fight on the same driven wheel (for example, a RWD rear wheel during understeer or a FWD front wheel during oversteer) because `RCCP_Stability` would overwrite accumulators that `RCCP_WheelCollider` had already consumed.

`RCCP_AI` and `RCCP_Recorder` sit at `-11` (just before `RCCP_CarController` at `-10`) so the inputs they push via `Inputs.OverrideInputs(...)` are visible the same frame the controller polls them. `RCCP_Limiter` sits at `-8` so its `cutFuel` directive is honored before `RCCP_Engine` reads it. `RCCP_AeroDynamics` sits at `-3` so its `Rigidbody` modifications are visible to `RCCP_WheelCollider` and Unity's physics step.

### Custom Components

You generally do not need to worry about execution order unless you are writing custom components that interact with RCCP internals. If you do, place your scripts at order `0` or higher to ensure RCCP is ready. If your component writes into the wheel torque accumulators, place it between `-2` and `-1` (or explicitly after `RCCP_Stability` at order `0`) and make sure it does not clobber values Stability has already applied. If your component pushes input via `Inputs.OverrideInputs(...)`, set its order to `-11` or earlier so `RCCP_CarController` sees the values the same frame.

### Verifying the Execution Order

You do not need to click anything to enforce this ordering. `RCCP_ScriptExecutionOrderManager` is an `[InitializeOnLoad]` editor helper that runs automatically on every assembly reload and writes the correct `MonoImporter` execution order for every RCCP script. Every order-sensitive class also carries a matching `[DefaultExecutionOrder]` attribute baked directly onto the type, so the ordering still works in projects where the editor enforcement helper is not present.

If you ever see drifty handling after a major refactor or after manually editing `ProjectSettings/ScriptExecutionOrder.asset`, verify that the ordering has not been overridden manually in that asset or by another editor script in your project.

## Singleton Services

RCCP uses several global managers that exist as single instances throughout your project. These "singletons" provide shared services that any script can access.

| Singleton | Type | How It Is Created | Purpose |
|-----------|------|-------------------|---------|
| `RCCP_Settings.Instance` | ScriptableObject | `Resources.Load` from the Resources folder | Master configuration: behaviors, physics, layers, prefab references |
| `RCCP_SceneManager.Instance` | MonoBehaviour | Auto-created by `RCCP_Singleton<T>` | Tracks the active player vehicle, camera, UI, and terrain data |
| `RCCP_InputManager.Instance` | MonoBehaviour | Auto-created by `RCCP_Singleton<T>` | Processes all input from keyboard, gamepad, and mobile controls |
| `RCCP_SkidmarksManager.Instance` | MonoBehaviour | Auto-created by `RCCP_Singleton<T>` | Shared skidmark rendering for all vehicles in the scene |
| `RCCP_GroundMaterials.Instance` | ScriptableObject | `Resources.Load` from the Resources folder | Defines surface friction, particles, and sounds per physics material |
| `RCCP_ChangableWheels.Instance` | ScriptableObject | `Resources.Load` from the Resources folder | Stores wheel swap presets for the customization system |

### Runtime Settings (Important)

`RCCP_RuntimeSettings` is a static class that creates **runtime clones** of your ScriptableObject settings when the game starts. This is important because:

- Without cloning, any changes you make to settings during play mode would permanently modify the asset files on disk
- With `RCCP_RuntimeSettings`, play-mode changes only affect the clone, so your original settings are always safe

**Rule of thumb:** In your runtime scripts, always access settings through `RCCP_RuntimeSettings` (which RCCP components do automatically through their base class properties). The `RCCP_Settings.Instance` property returns the original asset and should only be used in editor code.

## Events System

RCCP provides a static event system through the `RCCP_Events` class. You can subscribe to these events from your own scripts to react to things happening in the vehicle system without directly referencing RCCP components.

### Available Events

| Event | Parameter | When It Fires |
|-------|-----------|--------------|
| `OnRCCPSpawned` | `RCCP_CarController` | A vehicle is enabled and registered as the player vehicle |
| `OnRCCPDestroyed` | `RCCP_CarController` | A vehicle is disabled or destroyed |
| `OnRCCPAISpawned` | `RCCP_CarController` | An AI-controlled vehicle is enabled |
| `OnRCCPAIDestroyed` | `RCCP_CarController` | An AI-controlled vehicle is disabled |
| `OnRCCPCollision` | `RCCP_CarController`, `Collision` | A vehicle collides with something |
| `OnRCCPCameraSpawned` | `RCCP_Camera` | The RCCP camera is enabled |
| `OnRCCPUISpawned` | `RCCP_UIManager` | The RCCP UI canvas is enabled |
| `OnRCCPUIDestroyed` | `RCCP_UIManager` | The RCCP UI canvas is disabled |
| `OnRCCPUIInformer` | `string` | A UI informer message is displayed to the player |
| `OnBehaviorChanged` | (none) | The global behavior preset is changed |
| `OnVehicleChanged` | (none) | The active player vehicle is switched |
| `OnVehicleChangedToVehicle` | `RCCP_CarController` | The active player vehicle is switched (includes a reference to the new vehicle) |

### How to Subscribe to Events

Here is a complete example showing how to listen for a vehicle spawn event:

```csharp
using UnityEngine;

public class MyGameManager : MonoBehaviour {

    void OnEnable() {
        // Subscribe when this script is enabled
        RCCP_Events.OnRCCPSpawned += OnVehicleSpawned;
        RCCP_Events.OnRCCPCollision += OnVehicleCollision;
    }

    void OnDisable() {
        // ALWAYS unsubscribe when disabled to prevent memory leaks
        RCCP_Events.OnRCCPSpawned -= OnVehicleSpawned;
        RCCP_Events.OnRCCPCollision -= OnVehicleCollision;
    }

    void OnVehicleSpawned(RCCP_CarController vehicle) {
        Debug.Log("Player vehicle spawned: " + vehicle.name);
    }

    void OnVehicleCollision(RCCP_CarController vehicle, Collision collision) {
        Debug.Log(vehicle.name + " hit " + collision.gameObject.name);
    }

}
```

**Important:** Always unsubscribe from events in `OnDisable()`. If you forget, the event system will hold a reference to your destroyed object, which causes memory leaks and null reference exceptions.

## Behavior Presets

Behavior presets are pre-configured driving profiles that control how vehicles feel to drive. Think of them like driving modes: "Sport" mode, "Drift" mode, "Comfort" mode, and so on.

### What a Behavior Preset Controls

Each behavior preset (`BehaviorType`) configures the following systems:

| Category | Settings |
|----------|----------|
| **Stability Aids** | ABS (anti-lock brakes), ESP (electronic stability), TCS (traction control), steering helper, traction helper, angular drag helper |
| **Drift Mode** | Enable/disable drift, drift forces (yaw torque, forward push, sideways push), min/max speed, throttle yaw factor |
| **Drift Friction** | Rear/front sideways and forward grip reduction, response and recovery speed |
| **Drift Recovery** | Max angular velocity, counter-steer recovery boost, momentum maintenance |
| **Steering** | Steering curve (speed vs angle), sensitivity, counter-steering, limiting |
| **Differential** | Which differential type to use (Open, Limited, FullLocked, Direct) |
| **Gear Shifting** | RPM threshold for upshift, shift delay range |
| **Wheel Friction** | Forward and sideways friction curves for front and rear wheels |
| **Anti-Roll** | Minimum anti-roll bar force |

### Global vs Per-Vehicle Behaviors

By default, when you change the active behavior using `RCCP.SetBehavior()`, it applies to **all vehicles** in the scene. However, individual vehicles can opt out:

| Setting | Effect |
|---------|--------|
| `ineffectiveBehavior = true` | This vehicle ignores ALL global behavior changes completely |
| `useCustomBehavior = true` | This vehicle uses its own behavior preset index instead of the global one |

This is useful when you want AI vehicles to behave differently from the player, or when different vehicles in a garage should have different handling characteristics.

See [Settings](04_settings.md) for detailed behavior configuration.

## Resources and ScriptableObjects

RCCP stores its configuration in ScriptableObject assets inside the `Resources/` folder. These assets are loaded automatically at runtime using `Resources.Load`.

| Asset File | Script | What It Configures |
|-----------|--------|-------------------|
| RCCP_Settings.asset | RCCP_Settings | Master configuration: behaviors, physics, audio settings, layer assignments, prefab references |
| RCCP_GroundMaterials.asset | RCCP_GroundMaterials | Per-surface friction, particle effects, and sound clips |
| RCCP_ChangableWheels.asset | RCCP_ChangableWheels | Wheel model presets for the customization system |
| RCCP_DemoVehicles.asset | RCCP_DemoVehicles | List of demo vehicle prefabs |
| RCCP_DemoContent.asset | RCCP_DemoContent | Prefab references used by demo scenes |
| RCCP_DemoScenes.asset | RCCP_DemoScenes | List of included demo scenes |
| RCCP_CustomizationSetups.asset | RCCP_CustomizationSetups | UI prefab references for the customization system |
| RCCP_InputActions.asset | RCCP_InputActions | Input action definitions for the Input System |
| RCCP_Records.asset | RCCP_Records | Stored vehicle recording clips for replay |
| RCCP_PrototypeContent.asset | RCCP_PrototypeContent | Content used by the prototype test scene |
| RCCP_AddonPackages.asset | RCCP_AddonPackages | References to installable addon packages |

You can find all of these in `Assets/Realistic Car Controller Pro/Resources/`. Open them in the Inspector to view and modify their settings. The `Resources/` folder also contains three subfolders (`Editor/`, `Editor Icons/`, and `Generated/`) used internally by RCCP for editor-only resources -- you do not normally need to touch them.

## Next Steps

- [Vehicle Setup](03_vehicle_setup.md) -- Set up your first vehicle using the Setup Wizard
- [Settings](04_settings.md) -- Configure behavior presets and physics settings
- [API Reference](16_api_reference.md) -- All public methods for controlling vehicles from code

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)  
**Need help?** See [Troubleshooting](25_troubleshooting.md)

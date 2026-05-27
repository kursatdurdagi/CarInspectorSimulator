# Customization

RCCP's customization system lets players modify vehicles at runtime: paint color, wheels, spoilers, sirens, decals, neons, and performance upgrades. All customization is managed through the `RCCP_Customizer` component and its eight manager sub-components, with persistent save/load via PlayerPrefs and JSON serialization.

## RCCP_Customizer Setup

Add the `RCCP_Customizer` component to your vehicle (it inherits from `RCCP_Component`). This is the central hub that coordinates all customization managers and handles save/load operations.

### Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `saveFileName` | string | Vehicle name | Unique identifier for saving/loading this vehicle's customization. Auto-populated from the vehicle's GameObject name. **Must be unique per vehicle** if you want independent save slots. |
| `autoInitialize` | bool | `true` | Automatically initializes all manager sub-components. Set to `false` for networked vehicles where you want to sync customization data manually. |
| `autoLoadLoadout` | bool | `true` | Loads the last saved customization loadout on Awake. |
| `autoSave` | bool | `true` | Automatically saves customization changes. |
| `initializeMethod` | InitializeMethod | `Start` | Controls when managers are initialized. Options: `Awake`, `OnEnable`, `Start`, `DelayedWithFixedUpdate`. |

### Initialization Timing

The `InitializeMethod` enum controls when all upgrade managers are initialized:

| Value | When | Use Case |
|---|---|---|
| `Awake` | During Awake() | When customization must be ready before other components. |
| `OnEnable` | During OnEnable() | When re-enabling a pooled vehicle. |
| `Start` | During Start() | Default. Suitable for most scenarios. |
| `DelayedWithFixedUpdate` | After one FixedUpdate frame | When other physics components need to initialize first. |

### Basic API

```csharp
RCCP_Customizer customizer = vehicle.GetComponentInChildren<RCCP_Customizer>();

// Manually initialize all managers
customizer.Initialize();

// Save the current customization state
customizer.Save();

// Load previously saved customization
customizer.Load();

// Delete saved data and restore defaults
customizer.Delete();

// Hide/show all visual upgrades (spoilers, sirens, decals, neons)
customizer.HideAll();
customizer.ShowAll();
```

## Manager Components

The customizer delegates work to eight specialized manager components. Each manager handles one category of customization and is found automatically via `GetComponentInChildren`.

| Manager | Visual Component | Category |
|---|---|---|
| `RCCP_VehicleUpgrade_PaintManager` | `RCCP_VehicleUpgrade_Paint` | Vehicle body color |
| `RCCP_VehicleUpgrade_WheelManager` | `RCCP_VehicleUpgrade_Wheel` | Wheel model swapping |
| `RCCP_VehicleUpgrade_SpoilerManager` | `RCCP_VehicleUpgrade_Spoiler` | Rear spoilers |
| `RCCP_VehicleUpgrade_SirenManager` | `RCCP_VehicleUpgrade_Siren` | Police lights / sirens |
| `RCCP_VehicleUpgrade_DecalManager` | `RCCP_VehicleUpgrade_Decal` | Body decals |
| `RCCP_VehicleUpgrade_NeonManager` | `RCCP_VehicleUpgrade_Neon` | Underglow neons |
| `RCCP_VehicleUpgrade_UpgradeManager` | (see Performance Upgrades) | Engine, brake, handling, speed |
| `RCCP_VehicleUpgrade_CustomizationManager` | (see Tuning Data) | Suspension, transmission, aids |

### Paint System

**Components:** `RCCP_VehicleUpgrade_PaintManager` / `RCCP_VehicleUpgrade_Paint`

The paint system applies a color to the vehicle body materials at runtime. The selected color is stored in the loadout as a `Color` value (alpha of `0` means no paint has been applied).

Use `RCCP_ColorPickerBySliders` for a ready-made UI color picker with RGB sliders.

```csharp
// Apply a custom paint color
RCCP_VehicleUpgrade_PaintManager paintManager = customizer.PaintManager;
if (paintManager != null)
    paintManager.Initialize();  // Applies the color stored in the loadout
```

### Wheel Customization

**Components:** `RCCP_VehicleUpgrade_WheelManager` / `RCCP_VehicleUpgrade_Wheel`

Wheel customization swaps the visual wheel models on all axles at runtime. Available wheel presets are defined in the `RCCP_ChangableWheels` ScriptableObject located in `Resources/`.

Each preset in `RCCP_ChangableWheels` contains a wheel prefab. The wheel index stored in the loadout (`-1` means no custom wheels applied) maps to this array.

To add new wheel options:

1. Open `RCCP_ChangableWheels` from `Tools > BoneCracker Games > Realistic Car Controller Pro > Configuration > Wheels`.
2. Add your wheel prefab to the list.
3. The new wheel is immediately available for selection at runtime.

### Spoiler System

**Components:** `RCCP_VehicleUpgrade_SpoilerManager` / `RCCP_VehicleUpgrade_Spoiler`

Add or swap spoiler prefabs on the vehicle. Each `RCCP_VehicleUpgrade_Spoiler` component represents one spoiler option. The manager tracks which spoiler is active by index (`-1` means none).

### Siren / Police Lights

**Components:** `RCCP_VehicleUpgrade_SirenManager` / `RCCP_VehicleUpgrade_Siren`

Adds police-style flashing lights and optional siren audio to the vehicle. Uses the `RCCP_PoliceLights` component for the light animation. The siren index (`-1` means none) identifies which siren preset is active.

### Decals

**Components:** `RCCP_VehicleUpgrade_DecalManager` / `RCCP_VehicleUpgrade_Decal`

Applies decal graphics to the vehicle body using Unity's `DecalProjector` component. Supports up to **4 decal slots**: front, back, left, and right. Each slot has its own index in the loadout (`-1` means no decal applied).

> **Important:** Decals require **URP or HDRP**. The `DecalProjector` component is not available in the built-in render pipeline. If you are using the built-in pipeline, decals will not be visible. See [Render Pipelines](18_render_pipelines.md) for pipeline setup instructions.

### Neons

**Components:** `RCCP_VehicleUpgrade_NeonManager` / `RCCP_VehicleUpgrade_Neon`

Adds underglow lighting effects beneath the vehicle using `DecalProjector`. The neon index (`-1` means none) identifies which neon preset is active.

> **Important:** Like decals, neons require **URP or HDRP**. They will not work with the built-in render pipeline. See [Render Pipelines](18_render_pipelines.md) for pipeline setup instructions.

### Performance Upgrades

**Component:** `RCCP_VehicleUpgrade_UpgradeManager`

Performance upgrades modify the vehicle's driving characteristics. Four upgrade categories are available, each with its own level (starting at `0` for stock):

| Upgrade | Component | Effect |
|---|---|---|
| Engine | `RCCP_VehicleUpgrade_Engine` | Increases maximum engine torque output |
| Brake | `RCCP_VehicleUpgrade_Brake` | Increases brake force for shorter stopping distances |
| Handling | `RCCP_VehicleUpgrade_Handling` | Improves suspension stiffness and steering response |
| Speed | `RCCP_VehicleUpgrade_Speed` | Increases maximum achievable speed |

The `RCCP_VehicleUpgrade_UpgradeManager` tracks the current level for each category:

```csharp
RCCP_VehicleUpgrade_UpgradeManager upgradeManager = customizer.UpgradeManager;
if (upgradeManager != null) {
    int engineLevel = upgradeManager.EngineLevel;
    int brakeLevel = upgradeManager.BrakeLevel;
    int handlingLevel = upgradeManager.HandlingLevel;
    int speedLevel = upgradeManager.SpeedLevel;
}
```

### Tuning Data (RCCP_VehicleUpgrade_CustomizationManager)

**Component:** `RCCP_VehicleUpgrade_CustomizationManager`

This manager handles detailed vehicle tuning stored in `RCCP_CustomizationData`. Unlike the other upgrade categories that use simple level integers, this stores the full set of tunable parameters:

#### Suspension

| Field | Type | Default | Description |
|---|---|---|---|
| `suspensionDistanceFront` | float | `0.2` | Front axle travel distance in meters |
| `suspensionDistanceRear` | float | `0.2` | Rear axle travel distance in meters |
| `suspensionSpringForceFront` | float | `55000` | Front spring force in Newtons |
| `suspensionSpringForceRear` | float | `55000` | Rear spring force in Newtons |
| `suspensionDamperFront` | float | `3500` | Front damper force |
| `suspensionDamperRear` | float | `3500` | Rear damper force |
| `suspensionTargetFront` | float | `0.5` | Front target position (0 = extended, 1 = compressed) |
| `suspensionTargetRear` | float | `0.5` | Rear target position (0 = extended, 1 = compressed) |

#### Camber and Transmission

| Field | Type | Default | Description |
|---|---|---|---|
| `cambersFront` | float | `0` | Front wheel camber angle (-15 to 15 degrees) |
| `cambersRear` | float | `0` | Rear wheel camber angle (-15 to 15 degrees) |
| `gearShiftingThreshold` | float | `0.8` | Normalized RPM threshold for automatic gear shifts (0-1) |
| `clutchThreshold` | float | `0.1` | Clutch engagement speed (0-1) |

#### Stability Aids

| Field | Type | Default | Description |
|---|---|---|---|
| `counterSteering` | bool | `true` | Automatic counter-steering for oversteer recovery |
| `steeringLimiter` | bool | `true` | Limit steering angle based on vehicle speed |
| `ABS` | bool | `false` | Anti-lock Braking System |
| `ESP` | bool | `false` | Electronic Stability Program |
| `TCS` | bool | `false` | Traction Control System |
| `SH` | bool | `false` | Steering Helper (velocity-aligned steering correction) |
| `NOS` | bool | `false` | Nitrous Oxide System |
| `revLimiter` | bool | `false` | Engine rev limiter |
| `automaticTransmission` | bool | `false` | Automatic transmission mode |

#### Visual Settings

| Field | Type | Default | Description |
|---|---|---|---|
| `headlightColor` | Color | White | Custom headlight color tint |
| `wheelSmokeColor` | Color | White | Custom tire smoke particle color |

## Loadout System

All customization state is stored in an `RCCP_CustomizationLoadout` instance, a serializable class that serves as the single source of truth for a vehicle's customization.

### Loadout Fields

| Field | Type | Default | Description |
|---|---|---|---|
| `paint` | Color | White (alpha 0) | Vehicle body paint color. Alpha of `0` means no paint applied. |
| `spoiler` | int | `-1` | Equipped spoiler index. `-1` means none. |
| `siren` | int | `-1` | Equipped siren index. `-1` means none. |
| `wheel` | int | `-1` | Equipped wheel set index. `-1` means none. |
| `engineLevel` | int | `0` | Engine upgrade level. `0` is stock. |
| `handlingLevel` | int | `0` | Handling upgrade level. `0` is stock. |
| `brakeLevel` | int | `0` | Brake upgrade level. `0` is stock. |
| `speedLevel` | int | `0` | Speed upgrade level. `0` is stock. |
| `decalIndexFront` | int | `-1` | Front decal index. `-1` means none. |
| `decalIndexBack` | int | `-1` | Back decal index. `-1` means none. |
| `decalIndexLeft` | int | `-1` | Left side decal index. `-1` means none. |
| `decalIndexRight` | int | `-1` | Right side decal index. `-1` means none. |
| `neonIndex` | int | `-1` | Neon underglow index. `-1` means none. |
| `customizationData` | RCCP_CustomizationData | (defaults) | Detailed tuning data (suspension, aids, transmission). |

### How Save / Load Works

The customizer uses `PlayerPrefs` with JSON serialization:

```csharp
// Saving: serializes the loadout to JSON and stores it in PlayerPrefs
PlayerPrefs.SetString(saveFileName, JsonUtility.ToJson(loadout));

// Loading: reads JSON from PlayerPrefs and deserializes to a loadout
loadout = JsonUtility.FromJson<RCCP_CustomizationLoadout>(
    PlayerPrefs.GetString(saveFileName)
);
```

The `saveFileName` is the PlayerPrefs key. Each vehicle **must** have a unique `saveFileName` to avoid overwriting another vehicle's data.

### Loadout Update Flow

When any manager component changes a value, it calls `loadout.UpdateLoadout(component)` which uses a type-switch to copy the relevant data:

- `RCCP_VehicleUpgrade_PaintManager` updates `paint`
- `RCCP_VehicleUpgrade_WheelManager` updates `wheel`
- `RCCP_VehicleUpgrade_UpgradeManager` updates `engineLevel`, `brakeLevel`, `handlingLevel`, `speedLevel`
- `RCCP_VehicleUpgrade_SpoilerManager` updates `spoiler`
- `RCCP_VehicleUpgrade_SirenManager` updates `siren`
- `RCCP_VehicleUpgrade_CustomizationManager` updates `customizationData`
- `RCCP_VehicleUpgrade_DecalManager` updates `decalIndexFront/Back/Left/Right`
- `RCCP_VehicleUpgrade_NeonManager` updates `neonIndex`

## Customization UI

RCCP provides ready-made UI prefabs for customization screens, managed through the `RCCP_CustomizationSetups` ScriptableObject (located in `Resources/RCCP_CustomizationSetups.asset`).

### UI Prefab References

The `RCCP_CustomizationSetups` singleton holds references to UI prefabs for each category:

| Field | Description |
|---|---|
| `paints` | Paint color selection UI panel |
| `wheels` | Wheel selection UI panel |
| `upgrades` | Performance upgrade UI panel |
| `spoilers` | Spoiler selection UI panel |
| `sirens` | Siren/police light UI panel |
| `decals` | Decal selection UI panel |
| `neons` | Neon underglow UI panel |
| `customization` | Suspension and handling tuning UI panel |

### Key UI Components

| Component | Description |
|---|---|
| `RCCP_UI_Customizer` | Main UI controller for the customization screen |
| `RCCP_UI_CustomizationSlider` | Slider UI element for tuning parameters |
| `RCCP_CustomizationTrigger` | Collider-based trigger zone that opens customization when the player drives in |
| `RCCP_CustomizationStation` | Dedicated customization area with camera positioning |
| `RCCP_ColorPickerBySliders` | RGB color picker with sliders for the paint system |

## Networking Considerations

For networked multiplayer vehicles:

1. Set `autoInitialize` to `false` on remote vehicles.
2. Sync the `RCCP_CustomizationLoadout` data across the network using your networking solution (Mirror, Photon, etc.).
3. After receiving loadout data, call `customizer.Initialize()` manually to apply the customization.
4. Do not use `autoSave` or `autoLoadLoadout` on remote vehicles since their state comes from the network, not from local PlayerPrefs.

## Common Issues

### Decals or neons not visible

Decals and neons use Unity's `DecalProjector`, which is only available in **URP** and **HDRP**. If you are using the built-in render pipeline, these features will not work. See [Render Pipelines](18_render_pipelines.md) for migration instructions.

### Loadout not saving or loading

- Verify that `saveFileName` is set and not empty. It defaults to the vehicle's GameObject name.
- Ensure each vehicle has a **unique** `saveFileName`. Two vehicles sharing the same name will overwrite each other's data.
- Check that `autoSave` is enabled, or call `customizer.Save()` manually after changes.

### Paint not applying

- Confirm the vehicle has a `RCCP_VehicleUpgrade_PaintManager` and at least one `RCCP_VehicleUpgrade_Paint` component.
- Check that the vehicle's body material supports color changes (the shader must have a `_Color` or `_BaseColor` property).

### Wheels not changing

- Open `RCCP_ChangableWheels` via `Tools > BoneCracker Games > Realistic Car Controller Pro > Configuration > Wheels` and verify it has entries.
- Ensure the `RCCP_VehicleUpgrade_WheelManager` component exists on the vehicle.

### Customization not applied on spawn

- Check the `initializeMethod` setting. If set to `DelayedWithFixedUpdate`, there will be a one-frame delay before customization is visible.
- Verify `autoLoadLoadout` is `true` if you want saved data applied automatically.
- If spawning vehicles from prefabs at runtime, make sure the prefab has the `RCCP_Customizer` component with a valid `saveFileName`.

## Related Topics

- [Vehicle Setup](03_vehicle_setup.md) -- Adding components to a vehicle
- [Damage System](11_damage_system.md) -- Collision damage and repair
- [Render Pipelines](18_render_pipelines.md) -- URP/HDRP setup for decals and neons
- [Troubleshooting](25_troubleshooting.md) -- General debugging guide

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

# Damage System

RCCP includes a comprehensive damage system that supports four types of vehicle damage: **mesh deformation**, **detachable parts**, **wheel damage**, and **light breakage**. All damage types are managed through the `RCCP_Damage` component attached to the vehicle, and every type can be toggled independently.

When a collision occurs, RCCP calculates the impulse magnitude, converts it to a contact point, and distributes the damage across all enabled subsystems within range. The system caches original mesh data at startup so that vehicles can be fully repaired at any time.

## RCCP_Damage Component

The `RCCP_Damage` component is the central controller for all vehicle damage. Add it to any vehicle that should support collision damage.

### Key Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `automaticInstallation` | bool | `true` | When enabled, automatically finds all damageable meshes, detachable parts, lights, and wheels on the vehicle at startup. When disabled, you must assign each array manually in the Inspector. |
| `damageFilter` | LayerMask | Everything | Controls which layers can cause damage to the vehicle. Only collisions with objects on these layers will trigger damage calculations. |
| `maximumDamage` | float | `0.75` | Maximum vertex displacement distance in meters. Limits how far any single vertex can move from its original position. Set to `0` to disable the limit. |
| `processInactiveGameobjects` | bool | `false` | Whether to include inactive child GameObjects when collecting meshes and parts during automatic installation. |
| `saveName` | string | Vehicle name | Identifier used for saving and loading damage data via JSON. Auto-populated from the vehicle's GameObject name. |

## Mesh Deformation

Mesh deformation displaces individual vertices of the vehicle's body meshes when a collision occurs. Vertices within the deformation radius of the contact point are pushed inward along the collision direction, producing realistic crumple effects.

### How It Works

1. On collision, the system converts the contact point to local space for each mesh.
2. An **octree** spatial structure is used for fast nearest-vertex lookup, avoiding the cost of iterating every vertex on every collision.
3. Vertices within `deformationRadius` of the contact point are displaced. Damage is stronger at the center and falls off toward the edges.
4. Original mesh vertex positions are cached at startup so they can be restored during repair.
5. Meshes must have **Read/Write Enabled** in their import settings. Non-readable meshes are automatically skipped with a console warning.

### Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `meshDeformation` | bool | `true` | Master toggle for mesh deformation. |
| `deformationRadius` | float | `0.75` | Radius around the contact point (in meters) within which vertices are affected. Larger values create wider dents. |
| `deformationMultiplier` | float | `1.0` | Scales the amount of vertex displacement. Higher values produce deeper dents from the same collision force. |
| `recalculateNormals` | bool | `false` | Recalculates mesh normals after deformation. Enable this if lighting looks incorrect on deformed areas. Costs some performance. |
| `recalculateBounds` | bool | `false` | Recalculates mesh bounds after deformation. Enable this if parts of the mesh disappear from view after heavy damage. Costs some performance. |

### Example: Adjusting Deformation Sensitivity

```csharp
// Make the vehicle more resistant to dents
RCCP_Damage damage = vehicle.GetComponentInChildren<RCCP_Damage>();
damage.deformationMultiplier = 0.5f;  // Half the normal deformation
damage.deformationRadius = 0.5f;      // Smaller affected area
damage.maximumDamage = 0.4f;          // Limit maximum displacement
```

## Detachable Parts (RCCP_DetachablePart)

Detachable parts are vehicle body panels (hoods, doors, bumpers, trunks) that can become loose and eventually fall off the vehicle when damaged. Each part uses a `ConfigurableJoint` to attach it to the vehicle body.

### Setup Requirements

1. The part must be a **separate child GameObject** of the vehicle with its own `Rigidbody`.
2. A `ConfigurableJoint` is required (automatically created when adding the component via `Reset()`).
3. The part's GameObject and children must be on the **RCCP_DetachablePart** layer.
4. The joint's `connectedBody` should reference the vehicle's main `Rigidbody`.

### Part Types

The `DetachablePartType` enum identifies the role of each part:

| Part Type | Damage Multiplier | Description |
|---|---|---|
| `Bumper_F` | 1.5x | Front bumper -- takes the most damage |
| `Bumper_R` | 1.5x | Rear bumper -- takes the most damage |
| `Trunk` | 1.2x | Trunk lid |
| `Hood` | 1.0x | Engine hood |
| `Other` | 1.0x | Any other body panel |
| `Door` | 0.8x | Doors -- slightly more resistant |

Damage multipliers are applied when `useDamageWeighting` is enabled (default: `true`). Bumpers absorb 50% more damage per collision than hoods, while doors absorb 20% less.

### Damage Lifecycle

A detachable part goes through three stages as its `strength` decreases:

1. **Locked** -- The `ConfigurableJoint` motions are locked. The part is rigidly attached to the vehicle.
2. **Loose** (`strength <= loosePoint`) -- Joint motions are unlocked to their original settings. The part wobbles and can flap in the wind (controlled by `addTorqueAfterLoose`).
3. **Detached** (`strength <= detachPoint`) -- The part breaks free from the vehicle, becomes an independent physics object, and is deactivated after `deactiveAfterSeconds`.

### Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `partType` | DetachablePartType | `Hood` | Identifies this part's role for damage weighting. |
| `strength` | float | `100` | Current durability. Decreases on each collision. |
| `lockAtStart` | bool | `true` | Lock the ConfigurableJoint motions at startup so the part stays firmly attached. |
| `isDetachable` | bool | `true` | Whether this part can fully detach. If `false`, the part can become loose but never falls off. |
| `loosePoint` | int | `50` | Strength threshold below which the part becomes loose (joint unlocks). |
| `detachPoint` | int | `0` | Strength threshold below which the part fully detaches from the vehicle. |
| `deactiveAfterSeconds` | float | `5.0` | Seconds after detachment before the part's GameObject is deactivated. |
| `addTorqueAfterLoose` | Vector3 | `(0,0,0)` | Torque applied in local space when the part is loose, scaled by vehicle speed. Creates a flapping effect. |
| `useDamageWeighting` | bool | `true` | Apply the part-type-based damage multiplier. |
| `COM` | Transform | Auto-created | Optional center of mass override for the part's Rigidbody. |

### RCCP_Damage Part Settings

The `RCCP_Damage` component has its own toggles that control whether detachable parts receive damage at all:

| Property | Type | Default | Description |
|---|---|---|---|
| `partDamage` | bool | `true` | Master toggle for detachable part damage. |
| `partDamageRadius` | float | `1.0` | Radius around the contact point in which parts are checked for damage. |
| `partDamageMultiplier` | float | `1.0` | Global multiplier applied to all part damage (stacks with per-part type multipliers). |

## Wheel Damage

Wheel damage displaces `RCCP_WheelCollider` positions on collision, simulating bent axles and misaligned wheels. When damage exceeds `maximumDamage`, wheels can optionally detach from the vehicle entirely.

### Configuration

| Property | Type | Default | Description |
|---|---|---|---|
| `wheelDamage` | bool | `true` | Master toggle for wheel damage. |
| `wheelDamageRadius` | float | `2.0` | Radius around the contact point within which wheels are affected. |
| `wheelDamageMultiplier` | float | `1.0` | Scales the amount of wheel displacement. |
| `wheelDetachment` | bool | `true` | When enabled, wheels that exceed `maximumDamage` displacement will detach from the vehicle. |

When a wheel detaches, `RCCP_WheelCollider.DetachWheel()` is called, which separates the wheel model from the vehicle and creates an independent physics object.

## Light Damage

Light damage reduces the `strength` of `RCCP_Light` components near the collision point. When a light's strength falls below its `breakPoint`, the light is marked as `broken` and turns off.

### RCCP_Light Damage Properties

Each `RCCP_Light` component has its own durability settings:

| Property | Type | Default | Description |
|---|---|---|---|
| `isBreakable` | bool | `true` | Whether this light can be broken by collisions. |
| `strength` | float | `100` | Current durability. Reduced by `impulse * 20` on each nearby collision. |
| `breakPoint` | int | `35` | Strength threshold below which the light is considered broken. |
| `broken` | bool | `false` | Read at runtime to check if the light is broken. |

### RCCP_Damage Light Settings

| Property | Type | Default | Description |
|---|---|---|---|
| `lightDamage` | bool | `true` | Master toggle for light damage. |
| `lightDamageRadius` | float | `0.75` | Radius around the contact point within which lights are checked. |
| `lightDamageMultiplier` | float | `1.0` | Scales the damage applied to lights. |

## Saving and Loading Damage

Damage state can be persisted between sessions using JSON serialization via PlayerPrefs.

### DamageData Class

The `RCCP_Damage.DamageData` class stores the complete damage snapshot:

- `originalMeshData` -- Original vertex positions for all meshes
- `damagedMeshData` -- Current (deformed) vertex positions
- `originalWheelData` -- Original wheel positions and rotations
- `damagedWheelData` -- Current wheel positions
- `lightData` -- Boolean array of broken/intact state for each light

### Save / Load API

```csharp
RCCP_Damage damage = vehicle.GetComponentInChildren<RCCP_Damage>();

// Save current damage state
damage.Save();

// Load previously saved damage state
damage.Load();

// Delete saved damage data
damage.Delete();
```

The `saveName` property is used as the PlayerPrefs key (with `_DamageData` appended). Make sure each vehicle has a unique `saveName` if you want independent save slots.

## Repairing Vehicles

There are two ways to repair a vehicle:

### Using the Public API

```csharp
// Repair a specific vehicle
RCCP.Repair(carController);

// Repair the current player vehicle
RCCP.Repair();
```

Both methods set `repairNow = true` on the vehicle's `RCCP_Damage` component, which triggers the repair process on the next frame.

### What Repair Does

When `repairNow` is set to `true`:

1. **Meshes** -- All deformed vertices are moved back to their original positions.
2. **Wheels** -- Wheel positions and rotations are restored. Deflated tires are re-inflated via `Inflate()`.
3. **Detachable Parts** -- Each part's `OnRepair()` is called: strength is restored, the ConfigurableJoint is recreated if destroyed, joint properties are restored, and the part is re-enabled if it was deactivated.
4. **Lights** -- Each light's `OnRepair()` is called: strength is restored and `broken` is set to `false`.

The `repaired` flag is set to `true` once all vertices have returned to within a small tolerance (`0.002` units) of their original positions. If the repair is not complete in one frame, it continues on subsequent frames.

## Multithreading

When `RCCP_SceneManager.multithreadingSupported` is `true`, mesh deformation and repair operations use `Task.Run()` to offload vertex calculations to background threads. This prevents frame drops on vehicles with high-polygon meshes. The system uses a `CancellationTokenSource` to safely cancel async operations when the component is destroyed or disabled.

When multithreading is not supported, equivalent synchronous methods (`CheckRepairRaw()`, `CheckDamageRaw()`) are used instead.

## Common Issues

### Damage not showing on collision

- Verify `automaticInstallation` is enabled, or that meshes are manually assigned to the `meshFilters` array.
- Check that the colliding object's layer is included in `damageFilter`.
- Confirm the vehicle's meshes have **Read/Write Enabled** in their import settings. Non-readable meshes are silently skipped.
- Ensure meshes have enough vertices for visible deformation. Low-poly meshes may not show noticeable dents.

### Detachable parts not falling off

- The part's GameObject and its children must be on the **RCCP_DetachablePart** layer.
- Verify that `strength`, `loosePoint`, and `detachPoint` are set correctly. The default values are `100`, `50`, and `0` respectively.
- Make sure `isDetachable` is `true` on the `RCCP_DetachablePart` component.
- Check that the part has a valid `ConfigurableJoint` connected to the vehicle's `Rigidbody`.

### Repair not working

- `repairNow` must be set to `true`. Use `RCCP.Repair(vehicle)` rather than manipulating the flag directly.
- If the vehicle was not damaged (`repaired` is already `true`), the repair process will not run.
- After calling repair, check `repaired` on subsequent frames to confirm completion.

### Wheel detachment not triggering

- Confirm `wheelDetachment` is `true` on the `RCCP_Damage` component.
- The wheel displacement must exceed `maximumDamage` before detachment is triggered.
- Make sure `wheelDamage` is enabled.

### Performance concerns with deformation

- Enable multithreading via `RCCP_SceneManager` for vehicles with high-polygon meshes.
- Reduce `deformationRadius` to limit the number of vertices processed per collision.
- Disable `recalculateNormals` and `recalculateBounds` if they are not visually necessary.

## Related Topics

- [Vehicle Setup](03_vehicle_setup.md) -- Adding components to a vehicle
- [Customization](12_customization.md) -- Paint, wheels, and upgrades
- [Troubleshooting](25_troubleshooting.md) -- General debugging guide

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

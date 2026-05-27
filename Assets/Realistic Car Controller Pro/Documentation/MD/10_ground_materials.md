# Ground Materials

RCCP uses **PhysicMaterial-based surface detection** to vary friction, particles, sounds, and skidmarks depending on what surface the wheels are touching. Every ground collider in your scene can be mapped to a set of surface properties, and Unity Terrains are supported through splatmap-based detection.

For general project setup, see [Installation](01_installation.md). For vehicle configuration, see [Vehicle Setup](03_vehicle_setup.md).

---

## Overview

The ground materials system works as follows:

1. Each wheel checks which PhysicMaterial is on the collider it is touching.
2. The system looks up that PhysicMaterial in the `RCCP_GroundMaterials` asset.
3. If a match is found, the corresponding friction, particle, sound, and skidmark settings are applied to that wheel.
4. For Unity Terrains, the system reads the splatmap to determine which terrain texture has the highest weight at the wheel's position, then maps that texture to a PhysicMaterial.

---

## RCCP_GroundMaterials ScriptableObject

`RCCP_GroundMaterials` is a singleton ScriptableObject loaded from the Resources folder. Access it at runtime via:

```csharp
RCCP_GroundMaterials groundMats = RCCP_GroundMaterials.Instance;
```

The asset is located at:

```
Assets/Realistic Car Controller Pro/Resources/RCCP_GroundMaterials.asset
```

It contains two main arrays:

| Array | Type | Purpose |
|---|---|---|
| `frictions` | `GroundMaterialFrictions[]` | Per-surface settings for regular colliders |
| `terrainFrictions` | `TerrainFrictions[]` | Splatmap-to-PhysicMaterial mappings for terrains |

---

## GroundMaterialFrictions

Each entry in the `frictions` array defines how a specific surface type behaves. Here are all the fields:

| Field | Type | Default | Description |
|---|---|---|---|
| `groundMaterial` | `PhysicsMaterial` | (none) | The Unity physics material assigned to ground colliders. This is the key used for surface matching. |
| `forwardStiffness` | `float` | 1 | Affects braking and acceleration grip. Higher values mean more grip along the wheel's forward axis. |
| `sidewaysStiffness` | `float` | 1 | Affects cornering grip. Higher values mean less lateral sliding. |
| `slip` | `float` | 0.25 | Slip threshold before particles and skidmarks begin to appear. Lower values make effects trigger sooner. |
| `damp` | `float` | 1 | Damping force applied to the wheel on this surface. |
| `volume` | `float` | 1 | Volume of the tire-on-surface sound (range 0 to 1). |
| `groundParticles` | `GameObject` | (none) | Particle prefab to spawn when the wheel slips on this surface (dust, gravel, snow, etc.). Must have or will receive an `RCCP_WheelSlipParticles` component. |
| `groundSound` | `AudioClip` | (none) | Audio clip played when the wheel rolls on this surface. |
| `skidmark` | `RCCP_Skidmarks` | (none) | Skidmark prefab used to draw tire marks on this surface. |

> **Note on PhysicsMaterial vs PhysicMaterial:** In Unity 2023.3 and newer (including Unity 6), the type is `PhysicsMaterial`. In older versions, it is `PhysicMaterial`. RCCP handles this automatically with preprocessor directives.

### Example Configurations

| Surface | forwardStiffness | sidewaysStiffness | slip | damp | Particles | Sound |
|---|---|---|---|---|---|---|
| Asphalt | 1.0 | 1.0 | 0.25 | 1.0 | None or light smoke | Tire hum |
| Gravel | 0.7 | 0.7 | 0.15 | 0.8 | Gravel dust | Crunching gravel |
| Grass | 0.5 | 0.5 | 0.1 | 0.6 | Grass/dirt particles | Soft rolling |
| Sand | 0.4 | 0.4 | 0.1 | 0.5 | Sand spray | Sand whoosh |
| Ice | 0.15 | 0.15 | 0.05 | 0.3 | None | Ice scrape |

---

## Setting Up a New Surface

Follow these steps to add a new ground surface type:

### Step 1: Create a PhysicMaterial

1. In the Unity Project window, right-click and select **Create > Physic Material** (or **Physics Material** in Unity 6).
2. Name it descriptively (for example, `PM_Gravel`).
3. Configure its friction and bounciness values as needed.

### Step 2: Assign It to Your Ground Collider

1. Select the ground GameObject in your scene (a MeshCollider, BoxCollider, etc.).
2. In the Inspector, find the collider component.
3. Drag your new PhysicMaterial into the **Material** field of the collider.

### Step 3: Open the Ground Materials Asset

1. Navigate to `Assets/Realistic Car Controller Pro/Resources/`.
2. Select `RCCP_GroundMaterials.asset`.
3. Alternatively, open it from the menu: **Tools > BoneCracker Games > Realistic Car Controller Pro > Configuration > Ground Physics Materials**.

### Step 4: Add a New Friction Entry

1. In the Inspector for `RCCP_GroundMaterials`, find the **Frictions** array.
2. Increase the array size by 1.
3. In the new entry, assign your PhysicMaterial to the `groundMaterial` field.

### Step 5: Configure the Entry

1. Set `forwardStiffness` and `sidewaysStiffness` to control grip.
2. Adjust `slip` to control when visual effects begin.
3. Set `damp` for the surface damping.
4. Assign a particle prefab to `groundParticles` (optional).
5. Assign an audio clip to `groundSound` (optional).
6. Assign a skidmark prefab to `skidmark` (optional).

### Step 6: Validate Particle Prefabs

`RCCP_GroundMaterials` includes a `CheckWheelPrefabsForMissingScript()` method that ensures all assigned particle prefabs have the required `RCCP_WheelSlipParticles` component. This is called automatically, but you can trigger it manually if needed.

---

## Terrain Support

RCCP supports Unity Terrains through splatmap-based surface detection. Instead of using a single PhysicMaterial for the entire terrain, the system checks which terrain texture (layer) has the highest weight at each wheel's position and maps it to the corresponding ground material.

### How It Works

1. On scene start, `RCCP_SceneManager` calls `GetAllTerrains()` to find and cache all `Terrain.activeTerrains`.
2. For each terrain, the following data is cached in a `Terrains` class:

| Field | Description |
|---|---|
| `terrain` | Reference to the Unity Terrain component |
| `mTerrainData` | Cached `TerrainData` for alphamap access |
| `terrainCollider` | PhysicMaterial from the terrain's `TerrainCollider` |
| `alphamapWidth` | Width of the terrain alphamap in pixels |
| `alphamapHeight` | Height of the terrain alphamap in pixels |
| `mSplatmapData` | Full alphamap array `[x, y, textureIndex]` |
| `mNumTextures` | Number of terrain texture layers |

3. At runtime, each wheel that detects it is on a terrain surface reads the cached splatmap data to find which texture has the highest weight at its world position.
4. The `TerrainFrictions` entries in `RCCP_GroundMaterials` map terrain layer indexes to PhysicMaterials.

### TerrainFrictions Configuration

The `terrainFrictions` array in `RCCP_GroundMaterials` contains entries with:

| Field | Type | Description |
|---|---|---|
| `groundMaterial` | `PhysicsMaterial` | The PhysicMaterial that this terrain layer maps to. Must match an entry in the `frictions` array. |
| `splatmapIndexes` | `SplatmapIndexes[]` | Array of terrain texture layer indexes that use this PhysicMaterial. |

Each `SplatmapIndexes` entry has a single field:

| Field | Type | Description |
|---|---|---|
| `index` | `int` | The terrain layer index (starting at 0) in the terrain's texture list. |

### Setting Up Terrain Surfaces

1. Open `RCCP_GroundMaterials.asset`.
2. In the **Terrain Frictions** array, add one entry per surface type present on your terrain.
3. For each entry:
   - Assign the `groundMaterial` to the PhysicMaterial that also exists in your `frictions` array.
   - Add the splatmap layer indexes that correspond to this surface type. For example, if your terrain's first texture (index 0) is grass, set `index = 0`.
4. Make sure the PhysicMaterial on the TerrainCollider is also present in the `frictions` array as a fallback.

### Example

Suppose your terrain has three layers:

| Layer Index | Texture | Desired Surface |
|---|---|---|
| 0 | Grass | Grass (low grip) |
| 1 | Dirt Path | Gravel (medium grip) |
| 2 | Rock | Asphalt (high grip) |

You would create three `terrainFrictions` entries, each pointing to the appropriate PhysicMaterial (which must also have a matching entry in the `frictions` array with particles, sounds, and skidmarks configured).

---

## Skidmarks System

Skidmarks are the tire marks left on the ground when wheels slip. RCCP uses a shared manager to handle all skidmark rendering.

### Architecture

| Component | Role |
|---|---|
| `RCCP_SkidmarksManager` | Singleton manager. Creates one `RCCP_Skidmarks` instance per ground material at scene start. Routes wheel slip data to the correct skidmark instance. |
| `RCCP_Skidmarks` | The actual skidmark renderer. Uses a ring buffer of `MarkSection` entries and dynamically builds a mesh. Supports both multithreaded and single-threaded mesh generation. |

### How Skidmarks Are Generated

1. `RCCP_SkidmarksManager` instantiates one `RCCP_Skidmarks` prefab per entry in the `frictions` array on Awake.
2. When a wheel slips, it calls `RCCP_SkidmarksManager.Instance.AddSkidMark()` with:
   - `pos` -- world position of the mark
   - `normal` -- surface normal
   - `intensity` -- opacity from 0 (transparent) to 1 (fully visible)
   - `width` -- width in meters
   - `lastIndex` -- index of the previous section (-1 for a new strip)
   - `groundIndex` -- which ground material the wheel is on
3. The skidmark prefab builds quad strips from connected sections, stored in a ring buffer with a maximum of `maxMarks` sections (default 1024).

### Skidmark Prefab Settings

Each `RCCP_Skidmarks` prefab on the skidmark GameObject has:

| Property | Default | Description |
|---|---|---|
| `maxMarks` | 1024 | Maximum number of mark sections before the ring buffer wraps |
| `groundOffset` | 0.02 | Height above the surface in meters to prevent z-fighting |
| `minDistance` | 0.04 | Minimum distance between consecutive sections. Closer sections are skipped. |

### Cleaning Skidmarks at Runtime

```csharp
// Clean all skidmarks in the scene
RCCP.CleanSkidmarks();

// Clean skidmarks for a specific ground material index
RCCP.CleanSkidmarks(0); // Cleans skidmarks for the first ground material
```

These methods call through to `RCCP_SkidmarksManager.Instance.CleanSkidmarks()`, which resets the mark counter and triggers a mesh rebuild.

### Performance Notes

- `RCCP_Skidmarks` supports **multithreaded mesh generation** when `RCCP_SceneManager.multithreadingSupported` is true. The vertex, normal, UV, color, and triangle arrays are built on a background thread, then applied to the mesh on the main thread.
- If multithreading is not supported, the `DrawRaw()` fallback builds the mesh synchronously.
- The ring buffer design (`maxMarks`) means old skidmarks are automatically overwritten, preventing unlimited memory growth.

---

## Common Issues

### No Skidmarks on a Surface

- Verify that the `skidmark` field is assigned in the matching `GroundMaterialFrictions` entry.
- Confirm that the `slip` threshold is not set too high. If `slip` is very large, the wheel may never exceed it.
- Check that the `RCCP_SkidmarksManager` singleton is present in the scene. It is created automatically, but verify it exists under `DontDestroyOnLoad`.

### Wrong Particles or Sounds on a Surface

- The most common cause is a mismatch between the PhysicMaterial on the collider and the one in the `frictions` array. Select the ground collider and verify its **Material** field matches the `groundMaterial` in the corresponding friction entry.
- If you duplicated a collider, the PhysicMaterial reference may have been lost. Reassign it.

### Terrain Surface Not Detected

- `RCCP_SceneManager` caches terrain data on Awake. If the terrain is instantiated or enabled **after** the scene starts, the cache will be empty. Make sure all terrains are active before play mode begins.
- Verify that the terrain layer indexes in `terrainFrictions[].splatmapIndexes` match the actual layer order in your Terrain's texture list.
- Check that `RCCP_SceneManager.Instance.terrainsInitialized` is `true` at runtime.

### Particles Do Not Appear

- Ensure the `groundParticles` prefab is assigned in the friction entry.
- The prefab must have or will automatically receive an `RCCP_WheelSlipParticles` component. If the prefab is missing a ParticleSystem, no particles will be visible.
- Check that the particle prefab's emission settings and lifetime are configured to actually produce visible particles.

### Skidmarks Flicker or Z-Fight

- Increase the `groundOffset` value on the `RCCP_Skidmarks` prefab. The default of 0.02 meters works for most surfaces, but very uneven terrain may need a higher value.

---

## Related Topics

- [Vehicle Setup](03_vehicle_setup.md) -- configuring wheels that interact with ground materials
- [Settings](04_settings.md) -- global settings that reference the ground materials asset
- [Camera System](09_camera_system.md) -- camera behavior over different surfaces
- [Architecture](02_architecture.md) -- how singletons like `RCCP_GroundMaterials` and `RCCP_SkidmarksManager` are loaded

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

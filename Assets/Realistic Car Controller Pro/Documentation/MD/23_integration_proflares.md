# Integration: ProFlares

ProFlares (Ultimate Lens Flares for Unity3D) is a third-party asset that provides high-quality, GPU-accelerated lens flare effects. The RCCP ProFlares integration replaces Unity's built-in lens flares on vehicle lights with ProFlares components, producing more realistic and performant headlight and taillight flare effects.

---

## Overview

RCCP vehicles use `RCCP_Light` components for headlights, taillights, brake lights, reverse lights, and indicators. By default, these lights can use Unity's built-in lens flare system. The ProFlares integration package adds ProFlares-compatible flare components to these lights, giving you access to ProFlares' advanced features:

- Multi-element lens flare compositions (ghost, halo, streak, starburst).
- GPU-accelerated rendering with minimal CPU overhead.
- Per-element color, intensity, and animation controls.
- Occlusion testing with smooth fade in/out.
- Compatible with all render pipelines (Built-in, URP, HDRP).

---

## Prerequisites

1. **ProFlares** installed in your project. Purchase and import from the [Unity Asset Store](https://assetstore.unity.com/packages/tools/particles-effects/proflares-ultimate-lens-flares-for-unity3d-12845).
2. Realistic Car Controller Pro installed and working with at least one vehicle that has `RCCP_Light` components.

---

## Installing

### Step 1: Import ProFlares

If ProFlares is not already in your project, purchase and import it from the Asset Store.

### Step 2: Import the RCCP Integration Package

1. Open the RCCP Welcome Window: **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.
2. Navigate to the **Addons** tab.
3. Under **ProFlares**, you will see two buttons:
   - **Download and import ProFlares** -- Opens the ProFlares Asset Store page.
   - **Import ProFlares Integration** -- Imports the RCCP integration package.
4. Click **Import ProFlares Integration**.
5. Unity imports `RCCP_ProFlareIntegration.unitypackage` from `Addons/Installers/`.
6. Wait for Unity to recompile.

The installer package is located at:

```
Assets/Realistic Car Controller Pro/Addons/Installers/RCCP_ProFlareIntegration.unitypackage
```

---

## How It Works

The integration package adds ProFlares lens flare components to RCCP vehicle light setups. When ProFlares is installed and the integration is imported:

- Vehicle headlights and taillights gain ProFlares flare elements instead of (or in addition to) Unity's built-in `LensFlare` component.
- Flare intensity is driven by the `RCCP_Light` component's state (on/off, brightness level).
- ProFlares handles its own occlusion testing, so flares fade correctly when obscured by geometry.

---

## Configuration

After importing the integration, you can configure ProFlares settings on each vehicle light:

### Flare Intensity

ProFlares flare elements have their own intensity controls. These should be tuned per-light to match the visual style of your project. Brighter headlights typically use higher flare intensity, while indicators and brake lights use lower values.

### Flare Color

ProFlares supports per-element color. Match the flare color to the light color:

| Light Type | Typical Flare Color |
|---|---|
| Headlights (low beam) | Warm white / light yellow |
| Headlights (high beam) | Bright white |
| Taillights | Red |
| Brake lights | Bright red |
| Reverse lights | White |
| Indicators | Orange / amber |

### Flare Elements

ProFlares compositions can include multiple elements (ghosts, halos, streaks). For vehicle lights, a simple configuration with 1-3 elements (primary flare + subtle ghost) usually looks best. Overly complex flare setups can look unrealistic for automotive lighting.

### Occlusion

ProFlares performs its own occlusion checks. Ensure that:

- The flare source position matches the light source position.
- Occlusion layers are configured to include scene geometry but exclude the vehicle's own mesh (to prevent the vehicle body from occluding its own headlights at certain camera angles).

---

## Benefits Over Built-in Lens Flares

| Feature | Unity Built-in | ProFlares |
|---|---|---|
| **Performance** | CPU-based, can be expensive with many lights | GPU-accelerated, scales better |
| **Visual quality** | Basic single-element flares | Multi-element compositions with animations |
| **Customization** | Limited (brightness, color, flare texture) | Full per-element control (color, size, offset, rotation, animation) |
| **Occlusion** | Basic raycast | Smooth fade with configurable speed |
| **Render pipeline** | Built-in only (deprecated in URP/HDRP) | All pipelines supported |

---

## Common Issues

### Flares not showing

- **Check that ProFlares is imported.** The integration package requires the ProFlares asset to be present in the project.
- **Check the camera.** ProFlares requires a `ProFlareManager` component on the camera (or in the scene). Ensure one exists.
- **Check flare intensity.** If intensity is set to 0, flares will not be visible.
- **Check layers.** ProFlares uses layer-based occlusion. Ensure the flare source is on a visible layer.

### Flares appear on wrong lights

- Verify that each `RCCP_Light` component's ProFlares reference points to the correct flare element.
- Check that low beam and high beam flares are assigned to their respective light groups.

### Performance issues with many vehicles

- ProFlares is GPU-accelerated, so it scales better than Unity's built-in system. However, many simultaneous flare sources can still affect performance.
- Reduce the number of flare elements per light.
- Disable flares on distant vehicles (ProFlares supports distance-based culling).

### Flares persist when lights are off

- The RCCP integration should drive flare intensity based on light state. If flares remain visible when lights are off, check that the integration script is properly connected to the `RCCP_Light` component's on/off state.

---

## Removing the Integration

Since the ProFlares integration does not use an automatically managed scripting symbol like the Photon or Mirror integrations, removal is straightforward:

1. Delete the ProFlares integration files from the `Addons/Installed/` folder (if any were installed there).
2. If the integration added any components to your vehicle prefabs, remove those components manually.
3. The vehicle lights will revert to using Unity's built-in lens flare system (or no flares if none are configured).

---

## See Also

- [Vehicle Setup](03_vehicle_setup.md) -- Creating and configuring RCCP vehicles.
- [Settings](04_settings.md) -- Global RCCP settings including light configuration.
- [Integration: Enter-Exit](20_integration_enter_exit.md) -- Character enter-exit system.
- [Integration: Photon PUN 2](21_integration_photon.md) -- Multiplayer with Photon (light states are synced).

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

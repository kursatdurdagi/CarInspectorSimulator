# Troubleshooting and FAQ

This guide covers the most common issues you may encounter when using Realistic Car Controller Pro, organized by category. Each entry includes the problem, its likely cause, and a step-by-step solution.

**Tip:** Always check Unity's Console window (**Window > General > Console**) first. Most RCCP errors include descriptive messages that point directly to the cause.

---

## Installation Issues

### Import and Package Errors

| Problem | Cause | Solution |
|---------|-------|----------|
| "Input System not found" error after import | Unity's Input System package is not installed | Open **Window > Package Manager**, select **Unity Registry**, search for **Input System**, and click **Install**. See [Installation](01_installation.md). |
| "Newtonsoft.Json not found" error | Missing package dependency | Open **Window > Package Manager**, select **Unity Registry**, search for **Newtonsoft Json**, and click **Install**. |
| Compilation errors immediately after import | Conflicting packages or missing dependencies | Read the full error in Console. Ensure both **Input System** and **Newtonsoft Json** packages are installed. If errors persist, try deleting the `Library` folder and reopening the project. |
| "Editor Coroutines not found" error | Missing editor coroutines package | Open **Window > Package Manager**, select **Unity Registry**, search for **Editor Coroutines**, and click **Install**. |
| Pink or magenta materials everywhere | Wrong render pipeline shaders | You need to import the correct shader package for your pipeline (URP or HDRP). See [Render Pipelines](18_render_pipelines.md). Built-in pipeline works without additional imports. |
| Missing layers warning on first run | RCCP layers not yet created | Open **Tools > BoneCracker Games > Realistic Car Controller Pro > Settings**. RCCP creates its three layers (`RCCP_Vehicle`, `RCCP_DetachablePart`, `RCCP_Prop`) automatically. If this fails, restart Unity. |
| "All allowed layers have been filled" | Unity has no free layer slots (max 32) | Remove unused layers in **Edit > Project Settings > Tags and Layers** to free up at least 3 slots for RCCP. |
| NullReferenceException on entering Play mode | `RCCP_Settings` asset missing from Resources | Verify the file `Assets/Realistic Car Controller Pro/Resources/RCCP_Settings.asset` exists. If missing, reimport the package. |
| "Missing script" warnings on prefabs | A previously installed addon was removed but references remain | Either reimport the addon, or select the affected prefab and remove the missing script components manually via the Inspector context menu. |
| Duplicate class or namespace errors | Multiple versions of RCCP in the project | Ensure you only have one copy of the RCCP folder. Delete any older versions before importing a new one. |

### Upgrade Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Errors after upgrading RCCP version | Old cached data or renamed scripts | Delete the `Library` folder and reopen the project. Unity will reimport everything. |
| Vehicle prefabs broken after upgrade | Serialized references changed between versions | Re-run the Setup Wizard on affected vehicles, or recreate them from fresh models. See [Vehicle Setup](03_vehicle_setup.md). |
| Behavior presets reset after upgrade | Settings asset was overwritten | Reconfigure your behavior presets in **Tools > BoneCracker Games > Realistic Car Controller Pro > Settings**. See [Settings](04_settings.md). |

---

## Vehicle Setup Issues

### Vehicle Does Not Move

| Problem | Cause | Solution |
|---------|-------|----------|
| Vehicle does not move at all | No power reaching the wheels | Check the **RCCP_Differential** component's drive type (FWD, RWD, or AWD). Verify that the **RCCP_Engine** component exists on the vehicle. Confirm axles have `isPower` set correctly (this is controlled by the Differential, not set directly). |
| Engine starts but wheels do not spin | Clutch or gearbox misconfiguration | Check that **RCCP_Clutch** and **RCCP_Gearbox** components exist. Verify gear ratios are not zero. |
| Vehicle moves only in reverse | Gear ratios inverted | Check the gear ratios in **RCCP_Gearbox**. Forward gears should have positive ratios. |
| Vehicle barely moves, very slow | Torque too low or gear ratios too tall | Increase `maxTorqueAsNM` on **RCCP_Engine**. Lower the final drive ratio on **RCCP_Gearbox** for more acceleration. |

### Vehicle Behaves Incorrectly

| Problem | Cause | Solution |
|---------|-------|----------|
| Vehicle flips immediately on Play | Wheel colliders intersecting the ground plane | Increase suspension distance on **RCCP_Axle** components. Verify wheel collider radius matches the visual wheel size. Check that the wheel collider center offset is correct. |
| Vehicle bounces continuously | Suspension damper force too low | Increase the damper value on **RCCP_Axle** components. A good starting range is 2000-5000 for normal cars. |
| Wheels floating above the ground | Suspension distance too large or center offset wrong | Decrease suspension distance on **RCCP_Axle**. Check the wheel collider center Y offset. |
| Vehicle slides sideways excessively | Sideways friction too low | Adjust sideways friction values in the active behavior preset (**RCCP_Settings > Behavior Types**). Increase `sidewaysExtremumValue` and `sidewaysAsymptoteValue`. |
| Vehicle is too fast or too slow | Engine torque or speed limiter settings | Adjust `maxTorqueAsNM` and `maxEngineRPM` on **RCCP_Engine**. Check if **RCCP_Limiter** is attached and capping speed. |
| Wheels spinning in the wrong visual direction | Incorrect wheel model pivot orientation | The wheel mesh pivot must be centered on the wheel. Check your 3D model's axis orientation -- the rotation axis should align with Unity's conventions. |
| Steering feels unresponsive at low speed | Steering curve dropping off too early | Check the steering curve on the behavior preset or **RCCP_Axle**. The Y value at low speeds (left side of the curve) should be close to 1.0. The curve format is X = speed in km/h, Y = steering multiplier (0 to 1). |
| Steering too sensitive at high speed | Steering curve not reducing at high speed | Edit the steering curve so the Y value drops toward 0.2-0.4 at higher X (speed) values. |

### Component Hierarchy Errors

| Problem | Cause | Solution |
|---------|-------|----------|
| "Couldn't find RCCP_CarController in parent" | Subsystem component placed on the wrong GameObject | All RCCP subsystem components (Engine, Gearbox, Axle, etc.) must be placed on child GameObjects of the root GameObject that has `RCCP_CarController`. Move the component to the correct hierarchy position. |
| "This component is missing RCCP_CarController on parent" | Same as above | Same fix -- ensure the component is a child of the RCCP_CarController GameObject. |
| "Customizer component couldn't found" | RCCP_Customizer not added to the vehicle | Add an **RCCP_Customizer** component to the vehicle before adding upgrade components. See [Customization](12_customization.md). |

---

## Input Issues

### No Input Response

| Problem | Cause | Solution |
|---------|-------|----------|
| Vehicle ignores all input | `canControl` is false on RCCP_CarController | Select the vehicle in the Inspector and set `canControl = true`. This flag is also toggled by `RCCP.SetControl()`. |
| Vehicle ignores player input but AI works | `externalControl` is true | Set `externalControl = false` on **RCCP_CarController**. When true, the vehicle ignores RCCP_InputManager and expects external input (AI, replay, network). |
| Input works in editor but not in build | Input System not included in build | Go to **Edit > Project Settings > Player > Other Settings** and ensure **Active Input Handling** is set to **Both** or **Input System Package (New)**. |
| RCCP_InputActions.Instance is null | Input Actions asset missing from Resources | Check that `Assets/Realistic Car Controller Pro/Resources/RCCP_InputActions.asset` exists. Reimport the package if missing. |

### Specific Input Problems

| Problem | Cause | Solution |
|---------|-------|----------|
| Gamepad not detected | Input System not recognizing the device | Open **Window > Analysis > Input Debugger** to see connected devices. Ensure your gamepad is listed. Try disconnecting and reconnecting. |
| Key rebindings not saving between sessions | Auto-save disabled in settings | Enable `autoSaveLoadInputRebind` in **RCCP_Settings**. See [Settings](04_settings.md). |
| Mobile controls not appearing | Mobile controller not enabled | Set `mobileControllerEnabled = true` in **RCCP_Settings > Mobile Input** section. See [Mobile](07_mobile.md). |
| Both keyboard and mobile controls active | Mobile controller enabled on desktop | Set `mobileControllerEnabled = false` in **RCCP_Settings** for desktop platforms. Use platform-specific checks or preprocessor directives. |
| Vehicle drives itself without input | `overridePlayerInputs` is true on RCCP_Input | Check the **RCCP_Input** component and set `overridePlayerInputs = false`. Also verify `externalControl` is false on the car controller. |
| Logitech wheel not responding | SDK not initialized or wrong mapping | See [Logitech Steering Wheels](08_logitech_steering_wheels.md) for detailed setup instructions. |

---

## Physics Issues

### Unrealistic Behavior

| Problem | Cause | Solution |
|---------|-------|----------|
| Unrealistic drifting on every turn | Drift mode enabled in behavior preset | Check the active behavior preset in **RCCP_Settings**. Set `driftMode = false` if you want realistic grip driving. |
| Vehicle jitters or vibrates at low speed | Physics timestep too large | Lower `fixedTimeStep` in **RCCP_Settings**. Try 0.01 instead of the default 0.02. Do not go below 0.005 as this severely impacts performance. |
| Vehicle flies off ramps unrealistically | Max angular velocity too high | Lower `maxAngularVelocity` in **RCCP_Settings**. The default is 6. Try values between 4-7 for realistic behavior. |
| Wheels clip through the ground | Wheel collider mass too low or fixed colliders disabled | Enable `useFixedWheelColliders` in **RCCP_Settings**. This uses higher-mass wheel colliders that resist sinking. |
| Car spins out too easily | Rear friction too low or stability aids disabled | Enable ESP/TCS in the behavior preset. Increase rear sideways friction values. Check the **RCCP_Stability** component settings. |

### Stability and Aids

| Problem | Cause | Solution |
|---------|-------|----------|
| ABS not working (wheels lock up) | ABS disabled in behavior preset | Enable ABS in the active behavior preset under **RCCP_Settings**. |
| TCS not working (wheels spin freely) | TCS disabled in behavior preset | Enable TCS in the active behavior preset. |
| ESP not working (car oversteers) | ESP disabled on the vehicle | Enable **ESP** on the `RCCP_Stability` component. If it is already on, raise **ESP Intensity** (default 0.5) or lower **ESP Deadband** (default 6 deg/s → try 4 deg/s). |
| ESP dashboard light flickers on and off | `ESPIndicatorEngaged` threshold or hold time too low | Raise **Min Noticeable Brake Torque** (default 75 Nm) and/or **UI Min Hold Time** (default 0.1 s) on `RCCP_Stability`. Or bind your dashboard lamp to `ESPIndicatorEngaged`, not `ESPEngaged`. See [Telemetry and Debug](17_telemetry_debug.md). |
| ESP engages on every small steering input | Deadband too low for the target vehicle | Raise **ESP Deadband** to 6–10 deg/s (default 6). Alternatively, widen hysteresis by also raising **ESP Deactivation Deadband** (default 2.5 deg/s). |
| ESP brakes the wrong wheel during rapid transitions | Classification flipping between oversteer and understeer | Raise **ESP Mode Commit Time** (default 0.2 s). Check that **Understeer Gradient (K_us)** matches the vehicle archetype — the V2.31.1 default is 0.01 (aggressive sport baseline); use 0.0035 for a typical passenger sedan, 0.002 for a sportier feel, 0.005+ for an SUV. |
| ESP bleeds too much speed through corners in arcade games | ESP brake deceleration not compensated | Lower **Preserve Speed Factor** on `RCCP_Stability` (0 = realistic — ESP bleeds speed; 1 = full cancellation of ESP brake drag — V2.31.1 default). Set **ESP Mode** to `Sport` to also disable the motor-torque cut. |
| ESP never engages at high speed | `ψ̇_ref` being clamped by friction limit before the error matters | Raise **Estimated Mu (μ)** (default 0.85) toward 1.0 for dry asphalt gameplay. Lower it (0.3–0.6) only if the game surface is genuinely wet/icy. |
| Stability aids too aggressive | Aid strength values too high | Lower **ESP Intensity** / **TCS Intensity** / **ABS Intensity** (all on `RCCP_Stability`) and the behavior-preset steering helper strengths. See [Settings](04_settings.md). |
| Motor torque and ESP brake torque fight on the same driven wheel | Script execution order corrupted | The expected order is Axle (-2) → Stability (-1) → WheelCollider (0) and is enforced automatically via `[InitializeOnLoad]` on `RCCP_ScriptExecutionOrderManager`. If you suspect drift, verify nothing has overridden the ordering in `ProjectSettings/ScriptExecutionOrder.asset` or another editor script. See [Architecture](02_architecture.md#execution-order). |

### Differential Problems

| Problem | Cause | Solution |
|---------|-------|----------|
| One wheel spins while the other does not | Open differential behavior | This is correct for `Open` differential type. Switch to `Limited` or `FullLocked` on **RCCP_Differential** for more equal power distribution. |
| Both wheels always spin at the same rate | Full locked differential | This is correct for `FullLocked` type. Switch to `Limited` or `Open` for more natural behavior on turns. |
| Car pulls to one side under acceleration | Unequal torque split | Check differential type. `Direct` gives equal split. `Limited` with a low slip ratio can cause slight pull. |

**Differential types explained:**

| Type | Behavior | Best For |
|------|----------|----------|
| Open | Torque shifts based on wheel slip | Realistic street driving |
| Limited | Partially locks based on slip ratio (0-100%) | General purpose, most common |
| FullLocked | Both wheels always receive equal torque | Drift setups, off-road |
| Direct | Equal 50/50 split regardless of conditions | Simplified physics |

---

## Visual and Audio Issues

### Skidmarks and Particles

| Problem | Cause | Solution |
|---------|-------|----------|
| Skidmarks not appearing | Skidmark prefab not assigned for the surface type | Open **RCCP_GroundMaterials** asset and verify each ground material has a skidmark prefab assigned. See [Ground Materials](10_ground_materials.md). |
| Particles not showing on wheel slip | Particle prefab not assigned for the surface type | Open **RCCP_GroundMaterials** asset and assign particle prefabs for each ground material. |
| Skidmarks appear but are invisible | Skidmark material or shader issue | Check that the skidmark prefab's material uses a shader compatible with your render pipeline. |
| Wrong surface effects on terrain | Terrain splatmap not recognized | Ensure your terrain's splat textures are listed in **RCCP_GroundMaterials** in the correct order matching the terrain layers. |

### Lights

| Problem | Cause | Solution |
|---------|-------|----------|
| Vehicle lights not working | RCCP_Lights component missing or no RCCP_Light children | Add **RCCP_Lights** to the vehicle, then add **RCCP_Light** components to individual light GameObjects. See [Vehicle Setup](03_vehicle_setup.md). |
| Lens flares not showing | Wrong flare component for your pipeline | Built-in pipeline uses Unity's `LensFlare` component. URP and HDRP require `LensFlareComponentSRP`. See [Render Pipelines](18_render_pipelines.md). |
| Headlights illuminate the vehicle body | Light culling mask includes vehicle layer | Exclude the `RCCP_Vehicle` layer from the headlight's culling mask in the Light component. |

### Damage

| Problem | Cause | Solution |
|---------|-------|----------|
| Damage not deforming the mesh | RCCP_Damage not configured or mesh has too few vertices | Check that `automaticInstallation` is true on **RCCP_Damage**, or manually assign mesh filters. The mesh needs sufficient vertex density to show visible deformation. |
| Detachable parts not falling off | RCCP_DetachablePart not configured | Add **RCCP_DetachablePart** to each part that should detach. Configure the break force threshold. See [Damage System](11_damage_system.md). |
| "Configurable Joint not found" warning | Joint setup incomplete on detachable part | Ensure the detachable part has a properly configured `ConfigurableJoint` with a connected body. |

### Audio

| Problem | Cause | Solution |
|---------|-------|----------|
| No engine sound | RCCP_Audio component missing or no audio clips | Add **RCCP_Audio** to the vehicle. Check **RCCP_Settings** to ensure engine audio clips are assigned in the audio section. |
| Engine sound pitch is wrong | RPM-to-pitch mapping incorrect | Adjust the audio settings on **RCCP_Audio**. The pitch scales with engine RPM. |
| Audio cuts out at distance | Audio source max distance too short | Increase the max distance on the AudioSource components attached to the vehicle's audio child objects. |

### Decals and Neons

| Problem | Cause | Solution |
|---------|-------|----------|
| Decals not visible | Using Built-in Render Pipeline | Decals require **URP** or **HDRP**. They are not supported on the Built-in pipeline. See [Render Pipelines](18_render_pipelines.md). |
| Neon lights not visible | Using Built-in Render Pipeline | Neons also require **URP** or **HDRP**. |

---

## Camera Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| No camera following the vehicle | RCCP_Camera not in the scene | Add an **RCCP_Camera** prefab to your scene, or ensure `RCCP_SceneManager` is present (it can auto-create the camera). See [Camera System](09_camera_system.md). |
| Camera clips through objects | Occlusion layer mask not configured | Set the `occlusionLayerMask` on **RCCP_Camera** to include environment layers but exclude RCCP vehicle layers. |
| Camera mode not switching | Camera mode disabled in settings | Check `useHoodCameraMode`, `useWheelCameraMode`, `useFixedCameraMode`, `useCinematicCameraMode`, `useTopCameraMode` on **RCCP_Camera**. Disabled modes are skipped when cycling. |
| "UniversalAdditionalCameraData component couldn't found" | URP camera data missing | Select the actual Camera child of **RCCP_Camera** in the Inspector. Unity should auto-add the `UniversalAdditionalCameraData` component. If not, add it manually. This is required for URP post-processing and lens flares. |
| Hood camera shaking violently | ConfigurableJoint misconfiguration | Check the **RCCP_HoodCamera** component. If you see "has a ConfigurableJoint with no connected body," remove the joint and rigidbody, then reconfigure. |

**Available camera modes:**

| Mode | Description |
|------|-------------|
| TPS | Third-person chase camera (default) |
| FPS | First-person hood camera |
| WHEEL | Wheel-level camera |
| FIXED | Fixed position camera |
| CINEMATIC | Cinematic angle camera |
| TOP | Top-down camera |
| TRUCKTRAILER | Truck and trailer view |

---

## Performance Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Low FPS with many vehicles | Too many physics calculations per frame | Add **RCCP_Lod** to each vehicle to automatically disable expensive logic (audio, skidmarks, wheel alignment) at distance. Enable `multithreading` in **RCCP_Settings**. Reduce total vehicle count. |
| High CPU usage from physics | Fixed timestep too low (too many physics steps per frame) | Increase `fixedTimeStep` in **RCCP_Settings**. The default 0.02 is a good balance. Never go below 0.005. |
| Memory spike on scene load | Terrain splatmap caching | This is normal. **RCCP_SceneManager** caches terrain data on the first frame for ground material detection. The spike is temporary. |
| Skidmark rendering is slow | Too many skidmark sections | Skidmarks are managed by **RCCP_SkidmarksManager**. If multithreading is supported and enabled, mesh rebuilds happen on background threads. If performance is still an issue, reduce the maximum skidmark count in the skidmark prefab settings. |
| "Multithreading is disabled on this platform" | Platform does not support async operations | This is a warning, not an error. RCCP falls back to synchronous methods automatically. Performance may be slightly lower on single-threaded platforms. |

### Performance Optimization Checklist

1. Enable `multithreading` in RCCP_Settings (on by default)
2. Add **RCCP_Lod** to all vehicles, especially AI/traffic vehicles
3. Keep `fixedTimeStep` at 0.02 or higher for large vehicle counts
4. Limit the number of simultaneously active vehicles
5. Use LOD Groups on vehicle meshes (standard Unity LOD, separate from RCCP_Lod)
6. Disable `useTelemetry` and `useInputDebugger` in builds

---

## Customization Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Customization loadout not saving | `saveFileName` is empty or not unique | Set a unique `saveFileName` on the **RCCP_Customizer** component for each vehicle. If left empty, it defaults to the GameObject name, but duplicates will overwrite each other. Loadouts are stored in `PlayerPrefs`. |
| Paint color not applying | Material does not support color changes | Ensure the vehicle body material uses a shader with a `_Color` or `_BaseColor` property. The **RCCP_VehicleUpgrade_Paint** component needs the body material reference assigned. |
| Wheel swap not working | No wheels in the changeable wheels list | Add wheel prefabs to the **RCCP_ChangableWheels** asset located in `Assets/Realistic Car Controller Pro/Resources/`. See [Customization](12_customization.md). |
| "RCCP_ChangableWheels doesn't have wheelIndex" error | Requesting a wheel index that does not exist | Check that the wheel index you are requesting is within the range of the wheels array in **RCCP_ChangableWheels**. |
| Upgrade components not working | Missing upgrade manager | Add **RCCP_VehicleUpgrade_UpgradeManager** to the vehicle before adding individual upgrade components (Engine, Brake, Handling, Speed). |
| "Engine couldn't found in the vehicle" | Upgrade component missing a required dependency | **RCCP_VehicleUpgrade_Engine** and **RCCP_VehicleUpgrade_Speed** require an **RCCP_Engine** component. **RCCP_VehicleUpgrade_Brake** requires **RCCP_Axle** components. **RCCP_VehicleUpgrade_Handling** requires **RCCP_Stability**. |
| Spoiler not appearing | Body renderer not assigned | Set the body renderer reference on **RCCP_VehicleUpgrade_Spoiler**. Without it, the spoiler cannot position itself correctly. |
| Customization resets on scene load | `autoInitialize` is false or loadout not saved | Ensure `autoInitialize = true` on **RCCP_Customizer** and that `Save()` was called after changes. |

---

## Networking Issues

### Photon PUN 2

| Problem | Cause | Solution |
|---------|-------|----------|
| Remote vehicles jittering | High latency or low sync rate | Increase the sync rate on **RCCP_PhotonSync**. Enable interpolation for smoother visual updates. See [Photon PUN 2](21_integration_photon.md). |
| Input not working for local player | Ownership not assigned correctly | Ensure `photonView.IsMine` is true before sending input. Only the owner should send control inputs. |
| Customization not syncing across network | autoInitialize on for remote vehicles | Set `RCCP_Customizer.autoInitialize = false` for non-local (remote) vehicles. Sync customization data separately via RPC. |

### Mirror

| Problem | Cause | Solution |
|---------|-------|----------|
| Authority errors on vehicle control | Wrong ownership model | Check `isLocalPlayer` before sending input commands. Only the local player should control the vehicle. See [Mirror Networking](22_integration_mirror.md). |
| Vehicle snapping on remote clients | Sync interval too low or no interpolation | Increase the sync interval and enable interpolation on **RCCP_MirrorSync**. |

---

## AI Vehicle Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| AI vehicle does not follow waypoints | Waypoint container not assigned | Assign an **RCCP_AIWaypointsContainer** to the **RCCP_AI** component. See [AI Vehicles](13_ai_vehicles.md). |
| AI vehicle gets stuck | Stuck detection not configured or obstacles blocking | Check the stuck detection settings on **RCCP_AI**. Ensure the waypoint path is clear. The AI has a built-in stuck timer that triggers reverse. |
| AI vehicle ignores brake zones | Brake zone container not assigned | Assign an **RCCP_AIBrakeZonesContainer** to the **RCCP_AI** component. Brake zone triggers must be on the "Ignore Raycast" layer. |
| AI vehicle collides with obstacles | Dynamic obstacle avoidance not configured | Add **RCCP_AIDynamicObstacleAvoidance** to the AI vehicle. Set `dynamicObstacleLayers` to include layers with obstacles. |
| AI drives erratically at intersections | Waypoint spacing too tight | Increase spacing between waypoints at intersections. Use the waypoint editor to smooth the path. |

---

## Demo Scene Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| "This scene couldn't found in the Build Settings" | Demo scene not added to build settings | Go to **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window** and click **Add Demo Scenes To Build Settings**. |
| Demo scene works but my scene does not | Missing scene manager or camera | Your scene needs at minimum: an **RCCP_SceneManager** (auto-created via singleton), an **RCCP_Camera**, and a properly set up vehicle. Compare your scene to `RCCP_Scene_Blank_Prototype`. |
| Demo vehicles not spawning | RCCP_DemoVehicles asset not configured | Check `Assets/Realistic Car Controller Pro/Resources/RCCP_DemoVehicles.asset`. Ensure it has vehicle prefabs assigned. |

---

## Common Error Messages Reference

This table lists the most frequently seen error and warning messages, what they mean, and how to fix them.

| Error Message | Meaning | Fix |
|---------------|---------|-----|
| "Couldn't find RCCP_CarController in parent, this component named X is disabled!" | A subsystem component is not a child of an RCCP_CarController | Move the component to be under the RCCP_CarController hierarchy |
| "This component named X is missing RCCP_CarController on parent" | Same as above | Same fix |
| "Customizer component couldn't found on the X!" | An upgrade component cannot find RCCP_Customizer | Add RCCP_Customizer to the vehicle |
| "Engine couldn't found in the vehicle" | Engine upgrade/speed upgrade missing RCCP_Engine | Add RCCP_Engine to the vehicle |
| "Axles couldn't found in your vehicle" | Brake upgrade missing RCCP_Axle components | Add RCCP_Axle components to the vehicle |
| "Stability component couldn't found in the vehicle" | Handling upgrade missing RCCP_Stability | Add RCCP_Stability to the vehicle |
| "Body renderer of this spoiler is not selected!" | Spoiler component has no renderer reference | Assign the body renderer in the RCCP_VehicleUpgrade_Spoiler Inspector |
| "Body material is not selected for this painter" | Paint component has no material reference | Assign the body material in RCCP_VehicleUpgrade_Paint |
| "WheelCollider is not selected for this caliper named X" | Caliper has no wheel reference | Assign the WheelCollider reference in RCCP_Caliper |
| "Wheel model is not selected for X. Disabling this wheelcollider." | RCCP_WheelCollider has no visual wheel model | Assign the wheel model transform in the RCCP_WheelCollider Inspector |
| "No ParticleSystem found on this exhaust named X" | Exhaust object missing ParticleSystem | Add a ParticleSystem to the exhaust GameObject |
| "Particles couldn't be found on this GameObject named X" | Wheel slip particle object missing ParticleSystem | Ensure the wheel slip prefab has a ParticleSystem component |
| "Terrain data of the X is missing!" | Terrain has no terrain data asset | Assign terrain data to the terrain in the Inspector |
| "RCCP_InputActions.Instance is null" | Input actions asset missing from Resources | Reimport RCCP or restore the RCCP_InputActions asset to the Resources folder |
| "Could not find action map: X" | Input action maps not found in the input asset | Reimport RCCP. The input asset should contain Driving, Camera, and Replay action maps. |
| "Behavior preset 'X' not found." | Trying to switch to a nonexistent behavior | Check the behavior name matches one defined in RCCP_Settings.behaviorTypes |
| "Camera target not found!" | Showroom camera has no target assigned | Assign a target transform on RCCP_ShowroomCamera |
| "Damage component couldn't found on the vehicle named: X!" | RCCP_DamageData used without RCCP_Damage | Add RCCP_Damage to the vehicle |
| "RCCP_UIManager couldn't be found in the scene." | Demo customization station missing UI manager | Add RCCP_UIManager to the scene (included in demo scene prefabs) |
| "Corrupt preference file for X" | PlayerPrefs data corrupted | Delete the corrupted key using `PlayerPrefs.DeleteKey()` or clear all with `PlayerPrefs.DeleteAll()` |

---

## Frequently Asked Questions

### General

**Q: What Unity versions does RCCP support?**
A: RCCP V2.41.1 requires Unity 6000.0 (Unity 6) or newer.

**Q: Which render pipelines are supported?**
A: Built-in, URP, and HDRP are all supported. You need to import the matching shader package for URP or HDRP. See [Render Pipelines](18_render_pipelines.md).

**Q: Does RCCP work with the old Input Manager?**
A: No. RCCP requires Unity's **Input System** package (the new input system). Make sure your project's Active Input Handling is set to **Input System Package (New)** or **Both** in Player Settings.

**Q: Can I use RCCP in a mobile game?**
A: Yes. Enable `mobileControllerEnabled` in RCCP_Settings and choose a mobile controller type (TouchScreen, Gyro, SteeringWheel, or Joystick). See [Mobile](07_mobile.md).

### Vehicle Setup

**Q: How do I set up a new vehicle from scratch?**
A: Use the Setup Wizard at **Tools > BoneCracker Games > Realistic Car Controller Pro > Vehicle Setup Wizard**. It walks you through the entire process. See [Vehicle Setup](03_vehicle_setup.md).

**Q: Why does the Differential set wheel power instead of the Axle component?**
A: The `isPower` flag on axles is controlled by **RCCP_Differential** based on its drive type (FWD/RWD/AWD). You do not set it directly on the axle. This is a common source of confusion.

**Q: What is `RCCP_RuntimeSettings` and why do my runtime changes not persist?**
A: `RCCP_RuntimeSettings` creates runtime clones of ScriptableObjects so that Play mode changes do not modify your asset files. Modify the runtime clone during gameplay, not the original asset. Changes to runtime clones are lost when you exit Play mode, by design.

**Q: How do I add a new component type to RCCP?**
A: If you create a new class inheriting from `RCCP_Component`, you must also add a case for it in `RCCP_Component.Register()`. The registration uses a type-switch, so missing your type means the car controller will not cache a reference to it.

### Physics

**Q: What is the recommended fixedTimeStep?**
A: The default of **0.02** (50 Hz) works well for most cases. Use **0.01** (100 Hz) for more precise physics at the cost of performance. Never go below **0.005**.

**Q: How do steering curves work?**
A: Steering curves use X = speed in km/h and Y = steering multiplier from 0 to 1. They control how much steering angle is available at a given speed. A Y value of 1 at X=0 means full steering at standstill. A Y value of 0.3 at X=200 means 30% steering at 200 km/h. The Y axis is a multiplier, **not** degrees.

**Q: What does `useFixedWheelColliders` do?**
A: It increases wheel collider mass to prevent wheels from sinking through the ground at high speeds or under heavy load. Recommended to leave enabled (default: true).

### Customization

**Q: Where are customization loadouts saved?**
A: Loadouts are saved to `PlayerPrefs` using the `saveFileName` from **RCCP_Customizer**. They persist between sessions on the same machine. For cloud saves, you would need to implement your own save system.

**Q: How do I reset a vehicle's customization?**
A: Call `RCCP_Customizer.Delete()` on the vehicle to remove its saved loadout from PlayerPrefs, then reload the vehicle.

### Networking

**Q: Can I use RCCP with networking solutions other than Photon and Mirror?**
A: Yes. Use the `overridePlayerInputs` and `externalControl` flags on the vehicle to feed input from your networking solution. Override RCCP_Input values on remote clients using the external input API. See [Overriding Inputs](06_overriding_inputs.md).

---

## Diagnostic Steps

When you encounter an issue not listed above, follow these steps:

### Step 1: Read the Console

Open **Window > General > Console** in Unity. Look for red (error) and yellow (warning) messages. RCCP prefixes most messages with the component name, making it easier to locate the source.

### Step 2: Check the Minimal Setup

Ensure your scene has the bare minimum:
- A GameObject with **RCCP_CarController** (plus Rigidbody, auto-added)
- Child objects with **RCCP_Engine**, **RCCP_Clutch**, **RCCP_Gearbox**, **RCCP_Differential**
- At least one **RCCP_Axle** with wheel colliders and wheel models assigned
- An **RCCP_Input** component
- An **RCCP_SceneManager** in the scene (created automatically via singleton)

### Step 3: Test in a Clean Scene

Create a new empty scene and set up a single vehicle. If it works there but not in your main scene, the issue is in your scene configuration, not RCCP itself.

### Step 4: Test with the Prototype Scene

Open `Assets/Realistic Car Controller Pro/Scenes/RCCP_Scene_Blank_Prototype.unity` and press Play. If the demo vehicle works correctly, your RCCP installation is fine.

### Step 5: Enable Telemetry

Set `useTelemetry = true` in **RCCP_Settings** to see real-time vehicle data overlaid on screen during Play mode. This shows engine RPM, speed, gear, wheel slip, and other diagnostics. See [Telemetry and Debug](17_telemetry_debug.md).

### Step 6: Verify Script Execution Order

If the issue is physics-flavored (motor torque getting cut when it shouldn't, ESP braking the wrong wheel, strange drivetrain fights during stability events, AI inputs visibly lagging the player override, or the speed limiter not cutting fuel cleanly), your `ScriptExecutionOrder` may have been overridden. RCCP enforces the ordering automatically via `[InitializeOnLoad]` on `RCCP_ScriptExecutionOrderManager`. Every order-sensitive class also carries a matching `[DefaultExecutionOrder]` attribute baked directly onto the type, so ordering survives even in projects where the editor helper is missing — you should not normally need to act.

If you suspect drift, open `ProjectSettings/ScriptExecutionOrder.asset` and confirm both ordering ranges are intact: the drivetrain chain (`-7 Engine → -6 Clutch → -5 Gearbox → -4 Differential`) and the wheel accumulator pipeline (`-2 Axle → -1 Stability → 0 WheelCollider`). Also check that input writers (`RCCP_AI`, `RCCP_Recorder`) sit at `-11` and `RCCP_Limiter` sits at `-8`. See [Architecture — Execution Order](02_architecture.md#execution-order).

### Step 7: Contact Support

If you cannot resolve the issue, contact us with the following information:

- Your Unity version (e.g., Unity 6000.0.49f1)
- RCCP version (V2.41.1)
- Your render pipeline (Built-in, URP, or HDRP)
- The full error message copied from Console
- Steps to reproduce the issue
- Screenshots if applicable

**Email:** bonecrackergames@gmail.com
**Website:** [www.bonecrackergames.com](https://www.bonecrackergames.com)

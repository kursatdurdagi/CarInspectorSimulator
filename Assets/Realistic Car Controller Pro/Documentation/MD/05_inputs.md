# Inputs

## Overview

RCCP uses Unity's **New Input System** (`com.unity.inputsystem` package) for all player input. This replaces the legacy `Input.GetAxis` / `Input.GetKey` approach with a modern, data-driven system that supports multiple devices out of the box.

Key points:

- All input processing goes through the **RCCP_InputManager** singleton.
- Input actions and their bindings are defined in a Unity **InputActionAsset** file located at `Assets/Realistic Car Controller Pro/InputActions/RCCP_InputActions.inputactions`.
- The asset is referenced through the **RCCP_InputActions** ScriptableObject stored in `Resources/RCCP_InputActions.asset`.
- Each vehicle has an **RCCP_Input** component that receives processed inputs from the manager and applies them to the car controller.
- Four control schemes are preconfigured: Keyboard & Mouse, Gamepad, Logitech G920 racing wheel, and Oculus Quest.

## Input Architecture

Input flows through three layers before reaching the vehicle:

```
InputActionAsset (.inputactions file)
        |
        v
RCCP_InputManager  (singleton, reads actions, fires static events)
        |
        v
RCCP_Input  (per-vehicle component, processes and applies inputs)
        |
        v
RCCP_CarController  (vehicle systems consume throttle, brake, steer, etc.)
```

### Action Maps

The InputActionAsset contains three action maps:

| Action Map | Purpose | Actions |
|---|---|---|
| **Vehicle** | Driving controls | Throttle, Brake, Steering, Handbrake, NOS, Clutch, Engine, Lights, Indicators, Gears, Trail Detach |
| **Camera** | Camera control | MouseInput, Change Camera, Look Back, Zoom, Hold (orbit) |
| **Optional** | Recording and replay | Record, Replay |

All three maps are enabled simultaneously at runtime. The RCCP_InputManager caches references to each map on initialization for performance.

### RCCP_Inputs Struct

The `RCCP_Inputs` class carries all analog input values between the manager and vehicle components:

| Field | Type | Range | Description |
|---|---|---|---|
| `throttleInput` | float | 0 to 1 | Throttle pedal amount |
| `brakeInput` | float | 0 to 1 | Brake pedal amount |
| `steerInput` | float | -1 to 1 | Steering direction (negative = left, positive = right) |
| `handbrakeInput` | float | 0 to 1 | Handbrake engagement |
| `clutchInput` | float | 0 to 1 | Clutch disengagement |
| `nosInput` | float | 0 to 1 | Nitrous oxide activation |
| `mouseInput` | Vector2 | unbounded | Mouse/stick delta for camera orbit |

Button-press actions (engine start, lights, gear shifts, etc.) are not carried in this struct. They are delivered via static events instead. See [Input Events](#input-events) below.

## Default Keyboard Bindings

### Vehicle Controls

| Action | Keyboard Key | Description |
|---|---|---|
| Throttle | W / Up Arrow | Accelerate |
| Brake | S / Down Arrow | Brake / Decelerate |
| Steer Left | A / Left Arrow | Turn left |
| Steer Right | D / Right Arrow | Turn right |
| Handbrake | Space | Engage handbrake |
| NOS (Nitrous) | F | Activate nitrous oxide boost |
| Clutch | M | Disengage clutch |
| Start/Stop Engine | I | Toggle engine on/off |
| Low Beam Headlights | L | Toggle low beam headlights |
| High Beam Headlights | K | Toggle high beam headlights |
| Indicator Left | Q | Toggle left turn signal |
| Indicator Right | E | Toggle right turn signal |
| Indicator Hazard | Z | Toggle hazard lights |
| Gear Shift Up | Left Shift | Shift up one gear |
| Gear Shift Down | Left Ctrl | Shift down one gear |
| Gear N (Neutral) | N | Toggle neutral gear |
| Gear 1 | 1 | Shift directly to 1st gear |
| Gear 2 | 2 | Shift directly to 2nd gear |
| Gear 3 | 3 | Shift directly to 3rd gear |
| Gear 4 | 4 | Shift directly to 4th gear |
| Gear 5 | 5 | Shift directly to 5th gear |
| Gear 6 | 6 | Shift directly to 6th gear |
| Trail Detach | T | Detach attached trailer |

### Camera Controls

| Action | Keyboard / Mouse | Description |
|---|---|---|
| Change Camera | C | Cycle through camera modes |
| Look Back | B | Look behind vehicle (hold) |
| Camera Orbit | Mouse Delta | Orbit camera around vehicle |
| Hold Orbit | Left Mouse Button | Enable camera orbit mode |
| Zoom | Mouse Scroll Wheel | Zoom camera in/out |

### Optional Controls

| Action | Keyboard Key | Description |
|---|---|---|
| Record | R | Start/stop recording vehicle state |
| Replay | P | Start/stop replaying recorded state |

## Default Gamepad Bindings

These bindings use the standard Gamepad layout. Button names follow Unity's convention: Button South = A (Xbox) / X (PlayStation), Button East = B (Xbox) / O (PlayStation), and so on.

### Vehicle Controls

| Action | Gamepad Input | Description |
|---|---|---|
| Throttle | Right Trigger | Accelerate |
| Brake | Left Trigger | Brake / Decelerate |
| Steering | Left Stick (horizontal) | Turn left/right |
| Handbrake | Button South (A / X) | Engage handbrake |
| NOS (Nitrous) | Button East (B / O) | Activate nitrous |
| Start/Stop Engine | Button North (Y / Triangle) | Toggle engine |
| Low Beam Headlights | D-Pad Up | Toggle low beam |
| High Beam Headlights | Left Stick Down | Toggle high beam |
| Indicator Left | D-Pad Left | Toggle left signal |
| Indicator Right | D-Pad Right | Toggle right signal |
| Indicator Hazard | D-Pad Down | Toggle hazard lights |
| Gear Shift Up | Right Shoulder (RB / R1) | Shift up |
| Gear Shift Down | Left Shoulder (LB / L1) | Shift down |
| Trail Detach | Right Stick Press | Detach trailer |

### Camera Controls

| Action | Gamepad Input | Description |
|---|---|---|
| Change Camera | Left Stick Press | Cycle camera modes |
| Look Back | Button West (X / Square) | Look behind (hold) |
| Camera Orbit | Right Stick | Orbit camera |
| Hold Orbit | Right Stick Press | Toggle orbit mode |

## Additional Control Schemes

RCCP includes preconfigured bindings for two additional devices:

- **Logitech G920 Racing Wheel** -- Mapped to wheel axis for steering, pedals for throttle/brake/clutch, and wheel buttons for all other actions. The control scheme name is `G920`.
- **Oculus Quest Controllers** -- Mapped to triggers for throttle/brake, thumbstick for steering, and controller buttons for other actions. The control scheme name is `Oculus Quest`.

You can view and edit all bindings by opening the InputActionAsset at `Assets/Realistic Car Controller Pro/InputActions/RCCP_InputActions.inputactions` in Unity's Input Actions editor.

## Transmission Modes

RCCP supports three transmission types, configured on the **RCCP_Gearbox** component. The active transmission mode determines how gear input is interpreted.

### Automatic

The gearbox shifts automatically based on engine RPM thresholds. No manual gear input is required from the player. The shift point is controlled by the `shiftThreshold` property on RCCP_Gearbox (higher values = shifts at higher RPM for a sportier feel).

When **Auto Reverse** is enabled on RCCP_Input (default: on), the vehicle automatically shifts into reverse when the player holds the brake at low speed, and shifts back to first gear when accelerating forward.

### Manual

The player shifts gears manually using the Gear Shift Up and Gear Shift Down inputs (Left Shift / Left Ctrl by default). Direct gear selection is also available using number keys 1-6, N for neutral, or by pressing Gear_R for reverse (only available on G920 wheel scheme by default).

### Automatic DNRP (Semi-Automatic)

Uses a Drive / Neutral / Reverse / Park selector similar to a real automatic vehicle. Within Drive mode, gears shift automatically. The player selects the operating mode (D, N, R, or P) using the gear selector inputs.

## Input Component Settings

Each vehicle has an **RCCP_Input** component with per-vehicle input processing settings. These let you fine-tune how raw input translates into vehicle behavior.

### Steering Curve

An `AnimationCurve` that reduces the maximum steering angle as vehicle speed increases. This prevents unrealistic sharp turns at high speed.

- **X axis** = vehicle speed in km/h
- **Y axis** = steering multiplier (0 to 1)
- Default curve: full steering at 0 km/h, reduced to 0.2 at 100 km/h, reduced to 0.15 at 200 km/h

### Steering Limiter

When enabled (default: on), reduces the allowed steering angle when the vehicle is skidding sideways at speeds above 15 km/h. This prevents the player from oversteering into an unrecoverable spin.

### Counter Steering

When enabled (default: on), automatically applies a slight counter-steer correction based on front axle sideways slip. This helps stabilize the vehicle during oversteer situations.

The strength is controlled by `counterSteerFactor` (0 to 1, default: 0.5).

### Auto Reverse

When enabled (default: on) and the transmission is set to Automatic, the vehicle automatically shifts into reverse when the brake is held firmly (over 75% input) while nearly stopped (under 3 km/h). It shifts back to first gear when the vehicle starts moving forward again.

### Inverse Throttle/Brake on Reverse

When enabled (default: on) and the transmission is Automatic, the throttle and brake inputs are swapped while the vehicle is in reverse gear. This means pressing the throttle key drives the vehicle backward and pressing the brake key slows it down, which feels more natural.

### Cut Throttle When Shifting

When enabled (default: on), throttle input is forced to zero during an active gear shift. This creates a brief power interruption similar to a real manual gearbox, preventing harsh gear engagement.

### Brake/Handbrake on Disable

Two options control what happens when the vehicle loses player control (`canControl` becomes false):

- **Apply Brake On Disable** (default: off) -- Applies full brake when the vehicle is not controllable.
- **Apply HandBrake On Disable** (default: on) -- Applies full handbrake when the vehicle is not controllable. This prevents the vehicle from rolling away.

### Deadzones

Configurable per input axis to prevent analog stick drift or minor input noise from affecting the vehicle. Each deadzone is a float from 0 to 0.2 (default: 0.05 for all axes).

| Deadzone Property | Applies To |
|---|---|
| `steeringDeadzone` | Steering input |
| `throttleDeadzone` | Throttle input |
| `brakeDeadzone` | Brake input |
| `handbrakeDeadzone` | Handbrake input |
| `nosDeadzone` | Nitrous input |
| `clutchDeadzone` | Clutch input |

Values below the deadzone threshold are treated as zero. The remaining range is remapped to 0-1 so you do not lose any usable range.

## Input Rebinding at Runtime

RCCP supports letting players remap controls at runtime using Unity's built-in interactive rebinding API.

### How It Works

1. The player clicks a rebind button in the UI.
2. An overlay appears with a "Waiting for input..." prompt.
3. The player presses the desired key or button.
4. The new binding is applied and displayed.
5. Bindings are saved to PlayerPrefs as JSON.

### Key Classes

| Class | Purpose |
|---|---|
| `RCCP_RebindSaveLoad` | Static utility that saves and loads binding overrides to/from PlayerPrefs using the key `"rebinds"`. |
| `RCCP_UI_RebindInput` | UI component that handles interactive rebinding for a single action. Attach to a UI button, assign an `InputActionReference`, and wire up the text labels. |
| `RCCP_UI_RebindInputReset` | UI component that resets all rebind overrides back to defaults. |

### Auto Save/Load

When `autoSaveLoadInputRebind` is enabled in [RCCP_Settings](04_settings.md), rebind overrides are automatically saved after changes and loaded on startup via `RCCP_RebindSaveLoad.Save()` and `RCCP_RebindSaveLoad.Load()`.

### Setting Up Rebind UI

1. Create a UI button for each action you want to be rebindable.
2. Add the `RCCP_UI_RebindInput` component to each button.
3. Assign the `InputActionReference` for the target action.
4. Assign TextMeshPro labels for the action name and current binding display.
5. Optionally assign a rebind overlay GameObject that appears during rebinding.
6. Call `StartInteractiveRebind()` from the button's OnClick event.

## Input Events

The RCCP_InputManager fires static events for all button-press actions. These events are not carried in the RCCP_Inputs struct because they are one-shot triggers rather than continuous analog values.

### Gear Events

| Event | Signature | Fired When |
|---|---|---|
| `OnGearShiftedUp` | `void()` | Player shifts up one gear |
| `OnGearShiftedDown` | `void()` | Player shifts down one gear |
| `OnGearShiftedTo` | `void(int gearIndex)` | Player selects a specific gear (0-5 for gears 1-6, -1 for reverse) |
| `OnGearShiftedToN` | `void()` | Player toggles neutral gear |
| `OnGearToggle` | `void(TransmissionType)` | Transmission type is switched |
| `OnAutomaticGear` | `void(SemiAutomaticDNRPGear)` | DNRP selector position changes (D, N, R, or P) |

### Camera Events

| Event | Signature | Fired When |
|---|---|---|
| `OnChangedCamera` | `void()` | Player cycles to next camera |
| `OnLookBackCamera` | `void(bool state)` | Look-back pressed (true) or released (false) |
| `OnHoldOrbitCamera` | `void(bool state)` | Orbit hold pressed (true) or released (false) |

### Light Events

| Event | Signature | Fired When |
|---|---|---|
| `OnPressedLowBeamLights` | `void()` | Low beam headlights toggled |
| `OnPressedHighBeamLights` | `void()` | High beam headlights toggled |
| `OnPressedLeftIndicatorLights` | `void()` | Left turn signal toggled |
| `OnPressedRightIndicatorLights` | `void()` | Right turn signal toggled |
| `OnPressedIndicatorLights` | `void()` | Hazard lights toggled |

### Engine Events

| Event | Signature | Fired When |
|---|---|---|
| `OnStartEngine` | `void()` | Engine start/stop requested |
| `OnStopEngine` | `void()` | Engine stop requested |

### Stability Helper Events

| Event | Signature | Fired When |
|---|---|---|
| `OnSteeringHelper` | `void()` | Steering helper toggled |
| `OnTractionHelper` | `void()` | Traction helper toggled |
| `OnAngularDragHelper` | `void()` | Angular drag helper toggled |
| `OnABS` | `void()` | ABS toggled |
| `OnESP` | `void()` | ESP toggled |
| `OnTCS` | `void()` | TCS toggled |

### Other Events

| Event | Signature | Fired When |
|---|---|---|
| `OnTrailerDetach` | `void()` | Trailer detach requested |
| `OnRecord` | `void()` | Recording toggled |
| `OnReplay` | `void()` | Replay toggled |
| `OnOptions` | `void()` | Options menu requested |

### Subscribing to Events

To listen for input events from your own scripts:

```csharp
void OnEnable() {
    RCCP_InputManager.OnGearShiftedUp += HandleGearUp;
    RCCP_InputManager.OnPressedLowBeamLights += HandleLowBeam;
}

void OnDisable() {
    RCCP_InputManager.OnGearShiftedUp -= HandleGearUp;
    RCCP_InputManager.OnPressedLowBeamLights -= HandleLowBeam;
}

void HandleGearUp() {
    Debug.Log("Player shifted up!");
}

void HandleLowBeam() {
    Debug.Log("Low beam toggled!");
}
```

Always unsubscribe in `OnDisable()` to prevent memory leaks and errors from destroyed objects.

## Mobile Input

When `mobileControllerEnabled` is turned on in [RCCP_Settings](04_settings.md), the RCCP_InputManager switches from reading the InputActionAsset to reading values from `RCCP_MobileInputs.Instance` instead. This provides touch-based on-screen controls for throttle, brake, steering, handbrake, and NOS.

For detailed information on setting up mobile controls, see [Mobile](07_mobile.md).

## Troubleshooting

### No input response at all

- Verify the `com.unity.inputsystem` package is installed in the Package Manager.
- Make sure the `RCCP_InputActions` ScriptableObject exists in `Resources/` and has a valid InputActionAsset reference.
- Check the Console for errors like "Could not find action map: Vehicle" which indicate a misconfigured InputActionAsset.
- Ensure the Active Input Handling in Project Settings > Player is set to "Input System Package (New)" or "Both".

### Vehicle not responding to input

- Confirm the vehicle has an `RCCP_Input` component attached.
- Check that `canControl` is true on the `RCCP_CarController`.
- Check that `externalControl` is false on the `RCCP_CarController` (if true, the vehicle expects inputs from an external source like AI).
- Make sure `overridePlayerInputs` is false on the `RCCP_Input` component (unless you intend to override inputs from code).

### Stuck inputs after alt-tabbing

RCCP automatically handles application focus and pause events. When the application loses focus or is paused, all input values are reset to zero and the InputActionAsset is temporarily disabled. If you still experience stuck inputs, check that no custom code is overriding the RCCP_InputManager inputs.

### Rebindings not saving

- Verify `autoSaveLoadInputRebind` is enabled in [RCCP_Settings](04_settings.md).
- Rebindings are stored in PlayerPrefs under the key `"rebinds"`. If PlayerPrefs are being cleared elsewhere in your project, saved bindings will be lost.
- Call `RCCP_RebindSaveLoad.Save()` manually after rebinding if auto-save is not enabled.

### Gamepad not detected

- Make sure your gamepad is connected before entering Play Mode.
- Check that the Gamepad control scheme exists in the InputActionAsset.
- Some gamepads require specific drivers. Install the manufacturer's driver if Unity does not detect the device.

## Next Steps

- [Overriding Inputs](06_overriding_inputs.md) -- Control vehicles programmatically from code
- [Mobile](07_mobile.md) -- Set up mobile touch controls
- [Settings](04_settings.md) -- Configure global input settings and rebinding

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

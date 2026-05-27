# Mobile Controls

Realistic Car Controller Pro includes a complete mobile input system with four controller types to choose from. The system is driven by the `RCCP_MobileInputs` component, which reads values from on-screen UI elements and feeds them into the main input pipeline. All mobile controller settings live inside `RCCP_Settings`, and the visual controls are part of the RCCP UI Canvas prefab.

---

## Overview

RCCP provides four mobile controller types:

| Controller | Steering Method | UI Elements Shown |
|---|---|---|
| TouchScreen | Left/Right buttons | Steer Left, Steer Right, Throttle, Brake, Handbrake, NOS |
| Gyro | Device tilt (accelerometer) | Throttle, Brake, Handbrake, NOS (no steering buttons) |
| SteeringWheel | Rotatable on-screen wheel | Steering Wheel, Throttle, Brake, Handbrake, NOS |
| Joystick | Draggable analog stick | Joystick, Throttle, Brake, Handbrake, NOS |

The NOS button only appears when the active player vehicle has a NOS addon component attached.

---

## Enabling Mobile Controls

1. Open RCCP Settings: **Tools > BoneCracker Games > Realistic Car Controller Pro > Settings**
2. In the **Mobile Input** section, enable `mobileControllerEnabled` (set to `true`)
3. Select the desired controller type from the `mobileController` dropdown

When `mobileControllerEnabled` is `false`, the mobile canvas is automatically hidden regardless of platform. When `true`, the canvas activates and shows the appropriate UI elements for the selected controller type.

### Settings Reference

| Field | Type | Default | Description |
|---|---|---|---|
| `mobileControllerEnabled` | bool | `false` | Master toggle for mobile controls |
| `mobileController` | enum | `TouchScreen` | Active controller type |
| `gyroSensitivity` | float | `2.5` | Accelerometer sensitivity multiplier (Gyro mode only) |

---

## Controller Types

### TouchScreen

The simplest mobile input mode. Six on-screen buttons handle all vehicle controls:

- **Steer Left / Steer Right** -- each is a `RCCP_UIController` that outputs a value from 0 to 1
- **Throttle** -- ramps up while held, ramps down when released
- **Brake** -- same behavior as throttle
- **Handbrake** -- toggle-style press
- **NOS** -- only visible when the vehicle has a NOS addon

Steering is computed as `-left.input + right.input`, giving a final range of -1 (full left) to 1 (full right).

#### Sensitivity and Gravity

Each `RCCP_UIController` button has two tuning parameters:

| Parameter | Default | What It Does |
|---|---|---|
| `sensitivity` | `5.0` | How fast the input value ramps up toward 1 when the button is held. Higher values produce snappier response. |
| `gravity` | `5.0` | How fast the input value returns to 0 when the button is released. Higher values make the input drop off faster. |

Both values are multiplied by `Time.deltaTime` each frame, so the actual ramp speed is framerate-independent. The input is clamped between 0 and 1.

### Gyro (Accelerometer)

In Gyro mode, the device's built-in accelerometer controls steering. RCCP uses Unity's New Input System accelerometer (`UnityEngine.InputSystem.Accelerometer.current`) to read the device tilt.

- The X component of the acceleration vector is multiplied by `gyroSensitivity` and added to the steering input
- The steering left/right buttons are hidden
- Throttle, brake, handbrake, and NOS buttons remain visible and functional
- The accelerometer device is automatically enabled when Gyro mode is active

**Tuning tip:** Start with the default `gyroSensitivity` of 2.5. Increase it if steering feels too sluggish on tilt, decrease it if small movements cause oversteering.

### Steering Wheel

An on-screen rotatable steering wheel rendered by `RCCP_UI_SteeringWheelController`. The player drags the wheel image to rotate it, and the rotation angle is converted to a normalized steering value between -1 and 1.

| Parameter | Default | Description |
|---|---|---|
| `steeringWheelMaximumsteerAngle` | `270` | Maximum rotation angle in degrees (both directions). The wheel can rotate from -270 to +270. |
| `steeringWheelResetPosSpeed` | `20` | How fast the wheel auto-centers when released. Uses `Mathf.MoveTowards` scaled by `Time.deltaTime * 100`. |
| `steeringWheelCenterDeadZoneRadius` | `5` | Pixel radius around the wheel center where touch input is ignored. Prevents jitter from imprecise touches near the center. |

The steering input is calculated as:

```
input = Round(currentAngle / maximumAngle * 100) / 100
```

This gives two decimal places of precision. When the player releases the wheel, it smoothly returns to center at the configured reset speed.

The steering wheel uses Unity's `EventTrigger` system with three events: `PointerDown` (start tracking), `Drag` (update angle), and `EndDrag` (release and auto-center).

### Joystick

A standard analog joystick handled by `RCCP_UI_Joystick`. It consists of a background sprite and a draggable handle sprite.

- `inputHorizontal` (range -1 to 1) is used for steering
- `inputVertical` (range -1 to 1) is available but not used for throttle/brake by default
- The handle snaps back to center when released
- If the drag distance exceeds half the background size, the input is clamped to a normalized direction (magnitude of 1)

The joystick implements `IDragHandler`, `IPointerUpHandler`, and `IPointerDownHandler` from Unity's EventSystems.

---

## Switching Controller Type at Runtime

You can change the active mobile controller type from code at any time:

```csharp
// Switch to Gyro mode
RCCP.SetMobileController(RCCP_Settings.MobileController.Gyro);

// Switch to Joystick mode
RCCP.SetMobileController(RCCP_Settings.MobileController.Joystick);

// Switch to TouchScreen mode
RCCP.SetMobileController(RCCP_Settings.MobileController.TouchScreen);

// Switch to SteeringWheel mode
RCCP.SetMobileController(RCCP_Settings.MobileController.SteeringWheel);
```

There is also a ready-made UI component, `RCCP_UI_SetMobileController`, that you can attach to buttons in your own UI. It accepts an integer index:

| Index | Controller |
|---|---|
| 0 | TouchScreen |
| 1 | Gyro |
| 2 | SteeringWheel |
| 3 | Joystick |

---

## How Mobile Input Flows

Understanding the input pipeline helps when debugging:

1. `RCCP_MobileInputs` reads values from its assigned UI components (`throttle`, `brake`, `left`, `right`, `ebrake`, `nos`, `steeringWheel`, `joystick`) every frame in `Update()`
2. It computes combined values: `steerInput = -left.input + right.input + steeringWheel.input + joystick.inputHorizontal` (only the active controller contributes since others are disabled)
3. In Gyro mode, the accelerometer reading is added to `steerInput`
4. NOS input is added to throttle input (then clamped to 0-1)
5. All values are clamped to their valid ranges
6. `RCCP_InputManager` reads these values from `RCCP_MobileInputs.Instance` and passes them to the active vehicle

---

## UI Canvas Setup

The mobile UI is part of the **RCCP UI Canvas** prefab, which is referenced in `RCCP_Settings`. The canvas contains all mobile control elements, and `RCCP_MobileInputs` manages which elements are visible based on the active controller type.

### Sporty Mobile UI Skin

The current default mobile UI is the **Sporty** skin. The legacy button set was replaced in V2.31.1+ to give a clearer, more focused on-screen control surface:

- **Removed legacy buttons:** Demo, EBrake, NOS, Fuel, NOS-Bottle.
- **Added clearer buttons:** HandBrake, NOS1.

The four input methods (TouchScreen, Gyro, SteeringWheel, Joystick) are unchanged -- only the button artwork and the labels for handbrake / NOS were updated.

### Customizing the Layout

To customize button positions, sizes, or visuals:

1. Locate the RCCP UI Canvas prefab in your scene (or in the project at the path referenced by RCCP Settings)
2. Open the prefab and find the mobile control GameObjects
3. Modify RectTransform positions, sizes, and Image sprites as needed
4. The `RCCP_UIController`, `RCCP_UI_SteeringWheelController`, and `RCCP_UI_Joystick` components must remain on the correct GameObjects for input to work
5. Make sure the `RCCP_MobileInputs` component references are not broken after rearranging

### Required Scene Components

- An **EventSystem** must be present in the scene for touch/pointer input to work
- The RCCP UI Canvas must be in the scene (it is normally instantiated automatically if referenced in Settings)

---

## Platform Notes

- **iOS and Android** are both fully supported
- Gyro mode uses `UnityEngine.InputSystem.Accelerometer`, which works on most modern mobile devices
- The accelerometer device is enabled automatically by RCCP when Gyro mode is selected
- Test mobile controls using **Unity Remote** (for quick iteration) or by building directly to device (for accurate touch behavior)
- On desktop platforms, mobile controls still work if `mobileControllerEnabled` is set to `true` -- this is useful for testing

---

## Common Issues

| Problem | Likely Cause | Solution |
|---|---|---|
| Controls not showing on device | `mobileControllerEnabled` is `false` | Enable it in RCCP Settings |
| Steering too sensitive in Gyro mode | `gyroSensitivity` too high | Lower the value (try 1.0 - 2.0) |
| Steering too sluggish in Gyro mode | `gyroSensitivity` too low | Raise the value (try 3.0 - 5.0) |
| Buttons not responding to touch | Missing EventSystem in scene | Add an EventSystem GameObject to the scene |
| NOS button not visible | Vehicle has no NOS addon | Add RCCP NOS component to the vehicle via OtherAddonsManager |
| TouchScreen buttons respond slowly | Low `sensitivity` on RCCP_UIController | Increase the `sensitivity` value on the button component |
| Input sticks after switching controllers | UI element not resetting | Inputs auto-reset on enable/disable; check that references in RCCP_MobileInputs are assigned |
| Steering wheel jitters near center | Dead zone too small | Increase `steeringWheelCenterDeadZoneRadius` |

---

## Next Steps

- [Inputs](05_inputs.md) -- Keyboard and gamepad input configuration
- [Logitech Steering Wheels](08_logitech_steering_wheels.md) -- Hardware racing wheel support
- [Settings](04_settings.md) -- Full RCCP Settings reference including mobile options

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)
**Need help?** See [Troubleshooting](25_troubleshooting.md)

# Logitech Steering Wheels

Realistic Car Controller Pro supports hardware racing wheels and pedals through Unity's New Input System. Since RCCP uses an `InputActionAsset` for all input bindings, any USB controller that Unity recognizes as a gamepad or joystick device can drive your vehicles -- including Logitech racing wheels and other third-party peripherals.

---

## Overview

RCCP does not include a proprietary Logitech SDK integration. Instead, it relies on Unity's Input System package, which automatically detects connected HID (Human Interface Device) peripherals. When a racing wheel is connected, Unity maps its axes and buttons through the Input System's device layer, and RCCP's `RCCP_InputManager` reads those values through the same `InputActionAsset` used for keyboard and gamepad input.

For advanced features like force feedback, RCCP exposes per-wheel `engineBrakeTorqueNm` on each `RCCP_WheelCollider`. A haptic bridge can normalize this runtime torque value to drive resistance effects. Full force feedback requires the Logitech Gaming SDK (available separately on the Unity Asset Store).

---

## Supported Devices

| Device | Status |
|---|---|
| Logitech G29 | Supported via Input System |
| Logitech G920 | Supported via Input System |
| Logitech G923 | Supported via Input System |
| Logitech G27 (legacy) | Supported if recognized by OS |
| Thrustmaster T300 / T150 | Supported if recognized by Input System |
| Fanatec wheels | Supported if recognized by Input System |
| Other USB wheels/pedals | Supported if Unity Input System detects them as a gamepad or joystick |

The key requirement is that Windows (or macOS) recognizes the device and Unity's Input System can see it. If the device shows up in Unity's Input Debugger, it will work with RCCP.

---

## Prerequisites

Before using a racing wheel with RCCP, make sure you have:

1. **Unity's Input System package** installed (required by RCCP -- see [Installation](01_installation.md))
2. **Device drivers** installed on your OS:
   - For Logitech: install [Logitech G HUB](https://www.logitechg.com/innovation/g-hub.html) on Windows
   - For Thrustmaster: install the official Thrustmaster drivers
   - For Fanatec: install the Fanatec driver package
3. **Steering wheel connected and recognized** by the operating system before launching Unity
4. The RCCP `InputActionAsset` configured with appropriate bindings (included by default)

---

## Setup

### Step 1: Connect Your Wheel

1. Connect the steering wheel via USB
2. Verify it appears in your OS device manager (Windows: Device Manager > Human Interface Devices)
3. If using Logitech G HUB, confirm the wheel is detected and calibrated there

### Step 2: Verify in Unity

1. Open your Unity project
2. Go to **Window > Analysis > Input Debugger**
3. Your steering wheel should appear in the device list (typically as a Joystick or HID device)
4. If the wheel does not appear, check your drivers and USB connection

### Step 3: Check Input Bindings

RCCP uses an `InputActionAsset` stored at:

```
Assets/Realistic Car Controller Pro/InputActions/RCCP_InputActions.inputactions
```

This asset is loaded at runtime through the `RCCP_InputActions` ScriptableObject in the Resources folder. The default bindings include:

| Action | Keyboard | Gamepad / Wheel |
|---|---|---|
| Steering | A/D or Arrow Keys | Left Stick X axis |
| Throttle | W or Up Arrow | Right Trigger |
| Brake | S or Down Arrow | Left Trigger |
| Handbrake | Space | Button South (A/Cross) |
| Gear Shift Up | Left Shift | Button North (Y/Triangle) |
| Gear Shift Down | Left Ctrl | Button West (X/Square) |
| NOS | N | Button East (B/Circle) |

Racing wheels typically map their steering axis to the Left Stick X binding and their pedals to the trigger axes. If your specific wheel maps differently, you can customize the bindings.

### Step 4: Customize Bindings (If Needed)

If your wheel's axes do not match the default bindings:

1. Open the `RCCP_InputActions.inputactions` asset in Unity's Input Actions editor
2. Select the **Vehicle** action map
3. Click on the action you want to rebind (e.g., "Steering")
4. Add a new binding or modify the existing gamepad binding to match your wheel's axis
5. Save the asset

Players can also rebind controls at runtime if you include RCCP's rebinding UI. Rebinding overrides are saved to PlayerPrefs via `RCCP_RebindSaveLoad`.

---

## Input Architecture

Understanding how input flows from the wheel to the vehicle helps with debugging:

1. **Hardware** -- Wheel sends axis/button data over USB
2. **OS Driver** -- Logitech G HUB (or equivalent) translates raw USB data to standard HID input
3. **Unity Input System** -- Detects the device and maps it to the `InputActionAsset` bindings
4. **RCCP_InputManager** -- Reads the Input System actions each frame and populates `RCCP_Inputs`
5. **RCCP_CarController** -- Receives input values and applies them to engine, steering, and brakes

The `RCCP_InputManager` uses these named actions from the **Vehicle** action map:

| Action Name | Maps To |
|---|---|
| `Throttle` | `inputs.throttleInput` |
| `Brake` | `inputs.brakeInput` |
| `Steering` | `inputs.steerInput` |
| `Handbrake` | `inputs.handbrakeInput` |
| `NOS` | `inputs.nosInput` |
| `Clutch` | `inputs.clutchInput` |
| `Start/Stop Engine` | Engine toggle |
| `Gear Shift Up` / `Gear Shift Down` | Gear changes |

---

## Force Feedback

### Built-In Support

RCCP includes an `engineBrakeTorqueNm` property on each `RCCP_WheelCollider`. This value is calculated every physics frame by the drivetrain when engine braking is active. It is a torque magnitude in Nm, so normalize it for the range expected by your force feedback SDK.

You can read this value from your own force feedback script:

```csharp
RCCP_CarController vehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

// Get engine-brake torque from the front-left wheel and normalize it for haptics
float brakeTorqueNm = vehicle.FrontAxle.leftWheelCollider.engineBrakeTorqueNm;
float feedback = Mathf.Clamp01(brakeTorqueNm / 500f);
```

### Logitech Gaming SDK (Optional)

For full force feedback with Logitech wheels (spring force, damper, surface effects), you need the **Logitech Gaming SDK** package, available on the Unity Asset Store:

[Logitech Gaming SDK on Asset Store](https://assetstore.unity.com/packages/tools/integration/logitech-gaming-sdk-6630)

This is a separate third-party package and is not included with RCCP. Once installed, you can write a bridge script that reads RCCP's `engineBrakeTorqueNm` and other vehicle state values, then sends normalized values to the Logitech SDK's force feedback API.

---

## Pedal Configuration

Most racing wheels have separate pedal axes. Common configurations:

### Combined Pedals (Single Axis)

Some wheels report throttle and brake on a single combined axis. If this happens:

1. Open Input Debugger and identify which axis the pedals use
2. You may need to split the axis in your InputAction bindings using composite bindings (positive/negative parts of a single axis)

### Separate Pedals (Recommended)

Most modern wheels (G29, G920, G923) report throttle and brake as separate axes. The default RCCP bindings expect:

- **Throttle** on the Right Trigger axis
- **Brake** on the Left Trigger axis

If your pedals map to different axes, rebind them in the `RCCP_InputActions.inputactions` asset.

### Clutch Pedal

RCCP supports clutch input through the `Clutch` action in the Vehicle action map. If your wheel has a clutch pedal, bind it to this action for manual clutch control.

---

## Testing

1. Connect the steering wheel and launch Unity
2. Open **Window > Analysis > Input Debugger** to confirm the device is detected
3. Open any RCCP demo scene (e.g., from [Demo Content](19_demo_content.md))
4. Press Play
5. Turn the wheel -- the vehicle should steer
6. Press the throttle pedal -- the vehicle should accelerate
7. Press the brake pedal -- the vehicle should slow down
8. If any input does not work, check the Input Debugger to see which axes are being activated and adjust your bindings accordingly

---

## Troubleshooting

| Problem | Likely Cause | Solution |
|---|---|---|
| Wheel not detected in Input Debugger | Driver issue or USB problem | Ensure G HUB (or equivalent) is running, firmware is updated, try a different USB port |
| Steering direction is reversed | Axis polarity inverted | Invert the Steering binding in the InputActions asset, or use the "Invert" processor |
| Pedals not responding | Pedal axes not mapped correctly | Check Input Debugger to identify the correct axes, then update bindings |
| Combined pedal axis (throttle + brake on one axis) | Wheel reports combined axis | Use composite bindings to split positive and negative ranges |
| Force feedback not working | Logitech SDK not installed | Install the Logitech Gaming SDK package from the Asset Store |
| Wheel works in other games but not Unity | Input System not detecting HID device | Check that the Input System package is installed and the device appears in Input Debugger |
| Buttons on wheel not doing anything | Buttons not bound to RCCP actions | Open InputActions asset and add bindings for the specific button indices |
| Input feels laggy or delayed | Frame rate or input polling | Ensure the project runs at a stable frame rate; check that the Input System update mode is set to "Dynamic Update" in Project Settings > Input System Package |

---

## Next Steps

- [Inputs](05_inputs.md) -- Default keyboard and gamepad input bindings
- [Mobile](07_mobile.md) -- On-screen mobile touch controls
- [Settings](04_settings.md) -- Configure input and controller settings

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)
**Need help?** See [Troubleshooting](25_troubleshooting.md)

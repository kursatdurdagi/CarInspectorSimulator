# Telemetry and Debug

## Overview

Realistic Car Controller Pro includes built-in telemetry and input debug overlays that display real-time vehicle data during play mode. These tools help you tune vehicle parameters, diagnose handling problems, and verify that inputs are reaching the vehicle correctly.

There are two independent overlays:

- **Telemetry** -- a full-screen UI panel showing per-wheel forces, slip values, drivetrain data, and all input channels.
- **Input Debugger** -- a lightweight notification system that logs input-related actions (gear shifts, light toggles, stability system changes) to an on-screen informer panel.

Both are disabled by default and intended for development use only. You do not need to add any components manually -- RCCP instantiates the telemetry prefab automatically when the setting is enabled.

## Enabling the Overlays

Open the RCCP Settings window from the Unity menu:

**Tools > BoneCracker Games > Realistic Car Controller Pro > Settings**

Scroll to the **Units** section to find the debug overlay toggles:

| Setting | Field | Default | Description |
|---|---|---|---|
| Use Telemetry | `RCCP_Settings.useTelemetry` | Off | Shows a real-time vehicle data overlay at runtime (speed, RPM, gear, forces, slip values). |
| Use Input Debugger | `RCCP_Settings.useInputDebugger` | Off | Displays input-related notifications on screen at runtime (gear shifts, light toggles, stability system changes). |

You can also toggle these from code:

```csharp
RCCP_Settings.Instance.useTelemetry = true;
RCCP_Settings.Instance.useInputDebugger = true;
```

**Important:** The telemetry prefab is instantiated by `RCCP_SceneManager` during its `OnEnable`. If you change the setting at runtime after the scene has already loaded, the telemetry panel will not appear until the next scene load. For runtime toggling, enable the setting before entering play mode.

## Telemetry Display

The `RCCP_Telemetry` component is a Canvas-based UI overlay that reads data directly from the active player vehicle every frame. It is attached to the telemetry prefab located at:

```
Assets/Realistic Car Controller Pro/Prefabs/UI/RCCP_Telemetry.prefab
```

The prefab reference is stored in `RCCP_Settings.RCCPTelemetry`. When `useTelemetry` is enabled, `RCCP_SceneManager` instantiates this prefab automatically at scene start.

### How It Works

Each frame, `RCCP_Telemetry.Update()` reads the active player vehicle via `RCCP_SceneManager.activePlayerVehicle` and updates all UI Text elements. If no active player vehicle exists, the display goes blank. When you switch vehicles, the telemetry automatically follows the new active vehicle.

### Per-Wheel Data

The telemetry panel shows data for up to four wheels. Each wheel panel displays the following values:

| Field | Source | Unit | Description |
|---|---|---|---|
| Name | `WheelCollider.name` | -- | The name of the WheelCollider GameObject. |
| RPM | `WheelCollider.rpm` | RPM | Current rotational speed of the wheel. |
| Torque | `WheelCollider.motorTorque` | Nm | Motor torque currently applied to the wheel. |
| Brake | `WheelCollider.brakeTorque` | Nm | Brake torque currently applied to the wheel. |
| Force | `RCCP_WheelCollider.bumpForce` | N | Suspension bump force acting on the wheel. |
| Angle | `WheelCollider.steerAngle` | Degrees | Current steering angle of the wheel. |
| Slip_Sd | `RCCP_WheelCollider.SidewaysSlip` | -- | Sideways (lateral) slip value. Higher values mean more sliding. |
| Slip_Fwd | `RCCP_WheelCollider.ForwardSlip` | -- | Forward (longitudinal) slip value. Higher values mean more wheelspin or lockup. |
| Hit | `wheelHit.collider.name` | -- | Name of the collider the wheel is currently touching. Empty if airborne. |

**Note:** Even if your vehicle has more than four wheels, the telemetry panel displays data for the first four only.

### Vehicle Status

Below the wheel panels, the telemetry displays overall vehicle state:

| Field | Source | Unit | Description |
|---|---|---|---|
| ABS | `RCCP_Stability.ABSEngaged` | -- | "Engaged" or "Not Engaged". Shows "Not Equipped" if no Stability component. |
| ESP | `RCCP_Stability.ESPEngaged` | -- | "Engaged" or "Not Engaged". This is the **raw** ESP state — true the moment ESP decides to intervene, including micro-corrections. For a driver-facing dashboard light, use `ESPIndicatorEngaged` instead (see below). Shows "Not Equipped" if no Stability component. |
| TCS | `RCCP_Stability.TCSEngaged` | -- | "Engaged" or "Not Engaged". Shows "Not Equipped" if no Stability component. |
| Wheel Speed Average | `carController.wheelRPM2Speed` | km/h | Average speed derived from wheel RPM. |
| Speed | `carController.speed` | km/h | Physical speed of the Rigidbody (signed value). |
| Engine RPM | `carController.engineRPM` | RPM | Current engine revolutions per minute. |
| Final Torque | `carController.producedDifferentialTorque` | Nm | Torque output from the differential to the driven wheels. |
| Gear | `carController.currentGear` | -- | Current gear number (1, 2, 3...), "N" for neutral, or "R" for reverse. |
| Controllable | `carController.IsControllableByPlayer()` | -- | "True" if the player can currently control this vehicle. |

### Input Channels

The telemetry panel shows two columns of input values -- player inputs (raw input from keyboard, gamepad, or mobile) and vehicle inputs (the values actually applied to the vehicle after processing):

| Player Input | Vehicle Input | Range | Description |
|---|---|---|---|
| Player Throttle | Vehicle Throttle | 0.0 -- 1.0 | Accelerator input. |
| Player Steer | Vehicle Steer | -1.0 -- 1.0 | Steering input (negative = left, positive = right). |
| Player Brake | Vehicle Brake | 0.0 -- 1.0 | Brake pedal input. |
| Player Handbrake | Vehicle Handbrake | 0.0 -- 1.0 | Handbrake/parking brake input. |
| Player Clutch | Vehicle Clutch | 0.0 -- 1.0 | Clutch pedal input. |

The difference between player and vehicle inputs helps you diagnose issues where input processing (smoothing, clamping, or override systems) may be altering the raw player input before it reaches the drivetrain.

## Input Debugger

The input debugger is a notification-based system rather than a persistent display. When enabled, it fires `RCCP_Events.Event_OnRCCPUIInformer` messages whenever the player triggers an input action. These messages appear briefly on screen via the `RCCP_UI_Informer` component (included in the RCCP Canvas prefab).

### Logged Actions

The input debugger logs the following actions:

| Action | Example Message |
|---|---|
| Gear shift up | "Shifted Up" |
| Gear shift down | "Shifted Down" |
| Shift to specific gear | "Shifted To: 3" |
| Start/stop engine | "Starting Engine" / "Killing Engine" / "Stopped Engine" |
| Toggle low beam lights | "Switched Low Beam Lights To True" |
| Toggle high beam lights | "Switched High Beam Lights To True" |
| Toggle left indicators | "Switched Left Indicators To True" |
| Toggle right indicators | "Switched Right Indicators To True" |
| Toggle all indicators | "Switched All Indicators To True" |
| Toggle ABS | "Switched ABS To True" |
| Toggle ESP | "Switched ESP To True" |
| Toggle TCS | "Switched TCS To True" |
| Toggle steering helper | "Switched Steering Helper To True" |
| Toggle traction helper | "Switched Traction Helper To True" |
| Toggle angular drag helper | "Switched Angular Drag Helper To True" |

Each message appears for a few seconds and then fades out. This is useful for verifying that gamepad buttons, keyboard keys, or mobile UI buttons are correctly triggering vehicle actions.

## Reading Vehicle Data in Code

You do not need the telemetry UI to read vehicle data at runtime. All telemetry values are public properties on `RCCP_CarController` that you can access directly from any script:

```csharp
// Get the active player vehicle
RCCP_CarController vehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

// Basic vehicle state
float speed = vehicle.speed;                        // km/h (signed)
float rpm = vehicle.engineRPM;                      // engine RPM
int gear = vehicle.currentGear;                     // current gear index
int direction = vehicle.direction;                  // 1 = forward, -1 = reverse
bool isGrounded = vehicle.IsGrounded;               // true if any wheel is on the ground
float wheelSpeed = vehicle.wheelRPM2Speed;          // average wheel speed in km/h
float diffTorque = vehicle.producedDifferentialTorque; // differential output torque

// Player inputs (raw from input device)
float throttleP = vehicle.throttleInput_P;          // 0..1
float steerP = vehicle.steerInput_P;                // -1..1
float brakeP = vehicle.brakeInput_P;                // 0..1
float handbrakeP = vehicle.handbrakeInput_P;        // 0..1
float clutchP = vehicle.clutchInput_P;              // 0..1

// Vehicle inputs (processed values applied to drivetrain)
float throttleV = vehicle.throttleInput_V;          // 0..1
float steerV = vehicle.steerInput_V;                // -1..1
float brakeV = vehicle.brakeInput_V;                // 0..1
float handbrakeV = vehicle.handbrakeInput_V;        // 0..1
float clutchV = vehicle.clutchInput_V;              // 0..1

// Stability system status (requires Stability component)
if (vehicle.Stability) {
    bool absActive = vehicle.Stability.ABSEngaged;

    // ESPEngaged = raw runtime state. Use this for logging, analytics,
    // driver-assist telemetry, or any decision that should react the
    // moment ESP intervenes (including micro-corrections).
    bool espActive = vehicle.Stability.ESPEngaged;

    // ESPIndicatorEngaged = debounced dashboard state. Gated by a minimum
    // brake torque (default 75 Nm) and a UI hold time (default 0.1 s)
    // so dashboard lights do not flicker on micro-corrections. Use this
    // to drive the on-screen ESP warning lamp.
    bool espDashboardLit = vehicle.Stability.ESPIndicatorEngaged;

    bool tcsActive = vehicle.Stability.TCSEngaged;

    // ESP V2 diagnostics (exposed as debug* fields for tuning/telemetry)
    float yawRef   = vehicle.Stability.debugYawRefDegS;     // reference yaw rate (deg/s)
    float yawError = vehicle.Stability.debugYawErrorDegS;   // actual - reference (deg/s, signed)
    float beta     = vehicle.Stability.debugSideslipAngleDeg; // sideslip angle β (deg)
    bool oversteering = vehicle.Stability.debugIsOversteer;
}

// Per-wheel data
foreach (RCCP_WheelCollider wc in vehicle.AllWheelColliders) {
    float wheelRPM = wc.WheelCollider.rpm;
    float motorTorque = wc.WheelCollider.motorTorque;
    float brakeTorque = wc.WheelCollider.brakeTorque;
    float steerAngle = wc.WheelCollider.steerAngle;
    float sidewaysSlip = wc.SidewaysSlip;
    float forwardSlip = wc.ForwardSlip;
    float suspensionForce = wc.bumpForce;
    bool grounded = wc.WheelCollider.isGrounded;
}
```

### Building a Custom HUD

If the built-in telemetry panel does not fit your UI design, you can build your own using the properties shown above. A minimal speed and RPM display might look like this:

```csharp
using UnityEngine;
using TMPro;

public class SimpleSpeedDisplay : MonoBehaviour {

    public TMP_Text speedText;
    public TMP_Text rpmText;

    private void Update() {

        RCCP_CarController vehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        if (!vehicle)
            return;

        speedText.text = Mathf.Abs(vehicle.speed).ToString("F0") + " km/h";
        rpmText.text = vehicle.engineRPM.ToString("F0") + " RPM";

    }

}
```

## Common Uses

### Performance Tuning

Watch the **Slip_Sd** (sideways slip) and **Slip_Fwd** (forward slip) values while driving. High sideways slip on corner entry means the tires are losing lateral grip -- consider adjusting the sideways friction curve in [Ground Materials](10_ground_materials.md). High forward slip under acceleration indicates wheelspin -- you may want to enable or strengthen TCS, or adjust the forward friction curve.

### Suspension Tuning

Monitor the **Force** (suspension bump force) values across all four wheels. Uneven force distribution suggests the spring rates or damper values need adjustment. If one wheel consistently shows much higher force than the others, the vehicle's center of mass may need repositioning. See [Vehicle Setup](03_vehicle_setup.md) for suspension configuration.

### Input Debugging

If the vehicle is not responding to controls, use both overlays together:

1. Enable the **Input Debugger** to confirm that key presses and button inputs are firing events.
2. Enable the **Telemetry** to check whether player input values (Player Throttle, Player Steer, etc.) are non-zero.
3. Compare player inputs to vehicle inputs -- if player values are correct but vehicle values are zero, an input override or the controllable state may be blocking input. See [Overriding Inputs](06_overriding_inputs.md).

### Drivetrain Verification

Use the telemetry to verify your drivetrain configuration:

- **FWD:** Only front wheels should show non-zero Torque values.
- **RWD:** Only rear wheels should show non-zero Torque values.
- **AWD:** All driven wheels should show Torque values according to the differential split.
- **Gear display** should match your expected gear count and shift points.
- **Final Torque** shows the combined output after engine, clutch, gearbox, and differential processing.

## Script Execution Order

RCCP enforces the script execution order automatically through `RCCP_ScriptExecutionOrderManager`, an `[InitializeOnLoad]` editor helper that runs on every assembly reload and writes the correct `MonoImporter` execution order for every RCCP script. Every order-sensitive class also carries a matching `[DefaultExecutionOrder]` attribute baked directly onto the type, so the ordering still works even in projects without the editor enforcement manager. You do not need to invoke any menu items — the ordering is restored every time the editor compiles.

If you see physics drift after a major refactor or a manual edit of `ProjectSettings/ScriptExecutionOrder.asset`, open that file and confirm both ordering ranges are intact: the drivetrain chain (`-7 RCCP_Engine → -6 RCCP_Clutch → -5 RCCP_Gearbox → -4 RCCP_Differential`) and the wheel accumulator pipeline (`-2 RCCP_Axle → -1 RCCP_Stability → 0 RCCP_WheelCollider`). Verify no other editor script is overriding these. See [Architecture Overview — Execution Order](02_architecture.md#execution-order) for the full table and why this ordering is load-bearing.

## Next Steps

- [Settings](04_settings.md) -- Enable or disable telemetry and input debugger in the global settings panel.
- [API Reference](16_api_reference.md) -- Full list of vehicle properties you can read from code.
- [Vehicle Setup](03_vehicle_setup.md) -- Tune your vehicle based on telemetry data.
- [Overriding Inputs](06_overriding_inputs.md) -- Understand the difference between player and vehicle input channels.

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

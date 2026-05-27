# Overriding Inputs

This guide explains how to take programmatic control of an RCCP vehicle by overriding its default input pipeline. You will learn the three main approaches, when to use each one, and how to avoid the most common mistakes.

## When to Override Inputs

Override inputs any time you need code -- not the player -- to drive a vehicle:

- **AI-controlled vehicles** -- bots, traffic, opponents
- **Scripted sequences** -- cutscenes, tutorials, cinematics
- **Replay / playback system** -- replaying a recorded session
- **Networked vehicles** -- vehicles driven by remote players
- **Automated testing** -- driving a predefined path to verify physics

## Understanding the Override System

### The Input Pipeline

Every frame, the `RCCP_Input` component on the vehicle runs through these steps:

1. **Fetch inputs** from `RCCP_InputManager` (keyboard, gamepad, mobile).
2. **Apply deadzones** to each axis.
3. **Post-process** with steering limiter, counter steering, auto-reverse, and the speed-based steering curve.

When you override inputs, you are injecting your own values at step 1 so the rest of the pipeline still applies normally.

### Key Flags

The system uses two boolean flags on `RCCP_Input` and two on `RCCP_CarController`:

| Flag | Lives On | Default | Purpose |
|---|---|---|---|
| `overridePlayerInputs` | `RCCP_Input` | `false` | When `true`, the component skips reading from `RCCP_InputManager` and uses whatever values you write to the `inputs` field. |
| `overrideExternalInputs` | `RCCP_Input` | `false` | When `true`, the post-processing step (steering limiter, counter steering, auto-reverse, steering curve) is skipped entirely. |
| `canControl` | `RCCP_CarController` | `true` | When `false`, the vehicle ignores player input. Set via `RCCP.SetControl()`. |
| `externalControl` | `RCCP_CarController` | `false` | When `true`, the vehicle is marked as externally driven (AI, replay, network). Set via `RCCP.SetExternalControl()`. |

A vehicle only reads player input when **both** `canControl` is `true` **and** `externalControl` is `false`. This is checked by `RCCP_CarController.IsControllableByPlayer()`.

### The RCCP_Inputs Class

`RCCP_Inputs` is a serializable class that holds all driving axes:

| Field | Range | Description |
|---|---|---|
| `throttleInput` | 0 to 1 | Accelerator pedal. 0 = no throttle, 1 = full throttle. |
| `brakeInput` | 0 to 1 | Brake pedal. 0 = no brake, 1 = full brake. |
| `steerInput` | -1 to 1 | Steering wheel. -1 = full left, 0 = center, 1 = full right. |
| `handbrakeInput` | 0 to 1 | Handbrake lever. 0 = released, 1 = fully engaged. |
| `clutchInput` | 0 to 1 | Clutch pedal. 0 = engaged, 1 = fully disengaged. |
| `nosInput` | 0 to 1 | Nitrous oxide. 0 = off, 1 = full. |

You can construct an `RCCP_Inputs` instance either with the default constructor (all zeros) or by passing all six values plus a `Vector2` for mouse/camera input.

## Method 1: Direct Property Override

The most straightforward approach. Get the `RCCP_Input` component, enable the override flag, and write values directly to its `inputs` field every frame.

```csharp
using UnityEngine;

public class DirectOverrideExample : MonoBehaviour {

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;

        // Tell RCCP_Input to stop reading from keyboard/gamepad
        vehicleInput.overridePlayerInputs = true;
    }

    void Update() {
        // Write your desired values every frame
        vehicleInput.inputs.throttleInput = 0.8f;  // 80% throttle
        vehicleInput.inputs.steerInput = -0.5f;     // Turn left
        vehicleInput.inputs.brakeInput = 0f;         // No brake
    }

    void OnDisable() {
        // Always restore normal input when done
        if (vehicleInput != null)
            vehicleInput.overridePlayerInputs = false;
    }
}
```

### When to Use

Use this method when you have a simple, self-contained script that controls one vehicle and does not need to interact with RCCP's scene management.

## Method 2: Using the OverrideInputs Method

`RCCP_Input` provides a convenience method that sets `overridePlayerInputs = true` and assigns your `RCCP_Inputs` struct in one call.

```csharp
using UnityEngine;

public class StructOverrideExample : MonoBehaviour {

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;
    }

    void Update() {
        // Build an inputs struct with the values you want
        RCCP_Inputs myInputs = new RCCP_Inputs();
        myInputs.throttleInput = 0.6f;
        myInputs.steerInput = Mathf.Sin(Time.time); // Oscillate left-right

        // This sets overridePlayerInputs = true and applies the struct
        vehicleInput.OverrideInputs(myInputs);
    }

    void OnDisable() {
        // Restore normal input
        if (vehicleInput != null)
            vehicleInput.DisableOverrideInputs();
    }
}
```

The matching restore method is `DisableOverrideInputs()`, which sets `overridePlayerInputs` back to `false`.

### When to Use

Use this method when you prefer to build a complete input snapshot each frame rather than setting individual fields. It is also useful when passing inputs from one system to another (for example, a network packet deserialized into an `RCCP_Inputs` object).

## Method 3: Using RCCP.SetExternalControl

Call `RCCP.SetExternalControl(vehicle, true)` to mark the vehicle as externally controlled. This sets `externalControl = true` on `RCCP_CarController`, which tells the input system that a non-player system is driving.

```csharp
using UnityEngine;

public class ExternalControlExample : MonoBehaviour {

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;

        // Mark this vehicle as externally controlled
        RCCP.SetExternalControl(vehicle, true);

        // Also override player inputs so our values are used
        vehicleInput.overridePlayerInputs = true;
    }

    void Update() {
        vehicleInput.inputs.throttleInput = 0.5f;
        vehicleInput.inputs.steerInput = CalculateSteering();
    }

    float CalculateSteering() {
        // Your AI or network steering logic here
        return 0f;
    }

    void OnDisable() {
        if (vehicle != null)
            RCCP.SetExternalControl(vehicle, false);
        if (vehicleInput != null)
            vehicleInput.overridePlayerInputs = false;
    }
}
```

### When to Use

Use this method for AI vehicles, traffic, and any vehicle that should be permanently (or semi-permanently) driven by code. The `externalControl` flag is checked by many RCCP systems -- for example, input event listeners for lights, gears, and stability toggles all call `IsControllableByPlayer()` and will correctly ignore keyboard events for externally controlled vehicles.

## Method 4: Using RCCP.SetControl

Call `RCCP.SetControl(vehicle, false)` to disable player control entirely. This sets `canControl = false` on `RCCP_CarController`.

```csharp
using UnityEngine;

public class DisableControlExample : MonoBehaviour {

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;

        // Disable player control
        RCCP.SetControl(vehicle, false);

        // Override inputs so our scripted values are used
        vehicleInput.overridePlayerInputs = true;
    }

    void Update() {
        vehicleInput.inputs.throttleInput = 0.7f;
        vehicleInput.inputs.brakeInput = 0f;
        vehicleInput.inputs.steerInput = 0f;
    }

    void OnDisable() {
        // Return control to the player
        if (vehicleInput != null)
            vehicleInput.overridePlayerInputs = false;
        if (vehicle != null)
            RCCP.SetControl(vehicle, true);
    }
}
```

### When to Use

Use this for temporary takeovers where the player should not be able to control the vehicle at all -- cutscenes, tutorials, respawn sequences. Combine with `overridePlayerInputs = true` to inject your own values during that period.

## Practical Code Examples

### AI-Controlled Vehicle

A minimal AI controller that drives toward a target transform.

```csharp
using UnityEngine;

public class SimpleAIDriver : MonoBehaviour {

    public Transform target;

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;

        // Mark as externally controlled and override inputs
        RCCP.SetExternalControl(vehicle, true);
        vehicleInput.overridePlayerInputs = true;
    }

    void Update() {
        if (target == null) return;

        // Calculate direction to target
        Vector3 directionToTarget = target.position - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        // Steering: use the dot product with the vehicle's right axis
        float dot = Vector3.Dot(transform.right, directionToTarget.normalized);
        vehicleInput.inputs.steerInput = Mathf.Clamp(dot * 2f, -1f, 1f);

        // Throttle: drive forward if far away, brake if close
        if (distanceToTarget > 10f) {
            vehicleInput.inputs.throttleInput = 0.6f;
            vehicleInput.inputs.brakeInput = 0f;
        } else {
            vehicleInput.inputs.throttleInput = 0f;
            vehicleInput.inputs.brakeInput = 0.8f;
        }
    }

    void OnDisable() {
        if (vehicle != null)
            RCCP.SetExternalControl(vehicle, false);
        if (vehicleInput != null)
            vehicleInput.overridePlayerInputs = false;
    }
}
```

### Cutscene: Drive Forward Then Stop

A coroutine-based scripted sequence that drives a vehicle forward, brakes to a stop, and then returns control to the player.

```csharp
using UnityEngine;
using System.Collections;

public class CutsceneDriver : MonoBehaviour {

    public RCCP_CarController vehicle;

    public void StartCutscene() {
        StartCoroutine(DriveCutscene());
    }

    IEnumerator DriveCutscene() {
        RCCP_Input input = vehicle.Inputs;

        // Take control away from the player
        RCCP.SetControl(vehicle, false);
        input.overridePlayerInputs = true;

        // Drive forward at 70% throttle for 3 seconds
        input.inputs.throttleInput = 0.7f;
        input.inputs.brakeInput = 0f;
        input.inputs.steerInput = 0f;
        yield return new WaitForSeconds(3f);

        // Brake to a stop for 2 seconds
        input.inputs.throttleInput = 0f;
        input.inputs.brakeInput = 1f;
        yield return new WaitForSeconds(2f);

        // Release all inputs
        input.inputs.brakeInput = 0f;

        // Return control to the player
        input.overridePlayerInputs = false;
        RCCP.SetControl(vehicle, true);
    }
}
```

### Network Vehicle (Remote Player)

A sketch showing how you might apply inputs received from the network.

```csharp
using UnityEngine;

public class NetworkVehicleReceiver : MonoBehaviour {

    private RCCP_CarController vehicle;
    private RCCP_Input vehicleInput;

    // These would be updated by your networking layer
    [HideInInspector] public float netThrottle;
    [HideInInspector] public float netBrake;
    [HideInInspector] public float netSteer;
    [HideInInspector] public float netHandbrake;

    void Start() {
        vehicle = GetComponent<RCCP_CarController>();
        vehicleInput = vehicle.Inputs;

        // This is a remote vehicle -- mark it as external
        RCCP.SetExternalControl(vehicle, true);
        vehicleInput.overridePlayerInputs = true;
    }

    void Update() {
        // Apply the latest values from the network
        vehicleInput.inputs.throttleInput = netThrottle;
        vehicleInput.inputs.brakeInput = netBrake;
        vehicleInput.inputs.steerInput = netSteer;
        vehicleInput.inputs.handbrakeInput = netHandbrake;
    }
}
```

## Disabling Post-Processing

By default, even when you override player inputs, the vehicle still applies post-processing: the steering limiter, counter steering, auto-reverse logic, and the speed-based steering curve. This is usually desirable because it makes the vehicle behave realistically.

If you need full raw control (for example, for a replay system where the recorded values should be applied exactly), set `overrideExternalInputs = true` as well:

```csharp
vehicleInput.overridePlayerInputs = true;    // Skip reading from InputManager
vehicleInput.overrideExternalInputs = true;  // Skip steering limiter, counter steer, etc.
```

When `overrideExternalInputs` is `true`, the values you write to `inputs` are used directly with only deadzone processing applied.

## Resetting Inputs

If you need to zero out all input values at once, call `ResetInputs()` on the `RCCP_Input` component:

```csharp
vehicleInput.ResetInputs();
```

This sets all six axes (throttle, brake, steer, handbrake, clutch, NOS) to zero. Note that this also resets the internal `inputs` struct, so any override values you previously set will be cleared.

## Quick Reference: Which Flags to Set

| Scenario | `overridePlayerInputs` | `overrideExternalInputs` | `canControl` | `externalControl` |
|---|---|---|---|---|
| Normal player driving | `false` | `false` | `true` | `false` |
| AI / traffic vehicle | `true` | `false` | `true` | `true` |
| Cutscene (player locked out) | `true` | `false` | `false` | `false` |
| Replay (exact recorded values) | `true` | `true` | `false` | `true` |
| Network remote player | `true` | `false` | `true` | `true` |

## Common Pitfalls

- **Forgetting `overridePlayerInputs = true`.** If you only set values on the `inputs` field but leave the override flag off, the `RCCP_InputManager` will overwrite your values every frame.

- **Not restoring flags when done.** Always set `overridePlayerInputs = false` (and re-enable `canControl` / disable `externalControl` as needed) when you want the player to take control back. Consider using `OnDisable()` or `OnDestroy()` as a safety net.

- **Setting input values only once.** In the coroutine example above, the values persist because `overridePlayerInputs` prevents the input system from overwriting them. But if you are not using a coroutine, you must set values every frame in `Update()`.

- **Setting `externalControl` but not `overridePlayerInputs`.** The `externalControl` flag tells other RCCP systems the vehicle is not player-driven, but it does not by itself redirect input. You typically need both flags set together.

- **Inputs resetting on state change.** When `canControl` or `externalControl` changes, `RCCP_Input` automatically resets all input values to zero. If you toggle these flags, make sure to re-apply your override values on the next frame.

- **Confusing `RCCP_Inputs` (the class) with `RCCP_Input` (the component).** `RCCP_Inputs` is the data class holding the axis values. `RCCP_Input` is the MonoBehaviour component attached to the vehicle that processes those values.

## Next Steps

- [Inputs](05_inputs.md) -- Default keyboard and gamepad bindings
- [AI Vehicles](13_ai_vehicles.md) -- Full AI system with waypoints
- [API Reference](16_api_reference.md) -- All public methods in the RCCP static class

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

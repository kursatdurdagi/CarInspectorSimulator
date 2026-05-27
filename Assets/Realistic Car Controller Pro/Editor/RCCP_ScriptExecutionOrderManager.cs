//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright (c) 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Manages script execution order for RCCP scripts. Automatically sets up correct execution order on import.
/// </summary>
[InitializeOnLoad]
public class RCCP_ScriptExecutionOrderManager {

    /// <summary>
    /// Script execution order definitions. Negative values execute earlier, positive values execute later.
    ///
    /// TIMING HIERARCHY (every order-sensitive class is enforced; no FixedUpdate component sits at default 0
    /// without an explicit entry, so input-to-wheel-torque latency is bounded to a single fixed frame):
    /// -50: Singletons (SceneManager, InputManager, SkidmarksManager) - must exist before anything accesses them
    /// -11: AI / Recorder - write Inputs.OverrideInputs BEFORE CarController.PlayerInputs reads at -10
    /// -10: RCCP_CarController - main controller, polls component state from previous frame
    ///  -8: RCCP_Limiter - writes Engine.cutFuel BEFORE Engine consumes it in RevLimiter()
    ///  -7: RCCP_Engine - starts the drivetrain torque chain (Output → Clutch.ReceiveOutput)
    ///  -6: RCCP_Clutch - applies clutch slip, forwards to Gearbox
    ///  -5: Parent containers (Axles, Lights, OtherAddons, Exhausts) AND RCCP_Gearbox
    ///      Parents have no FixedUpdate so tying with Gearbox here is collision-free
    ///  -4: RCCP_Differential - splits torque, calls Axle.ReceiveOutput storing motor torque
    ///  -3: RCCP_AeroDynamics - writes Rigidbody.centerOfMass / linearDamping
    ///  -2: RCCP_Axle - populates wheel torque accumulators via AddMotorTorque (consumes Differential's same-frame store)
    ///  -1: RCCP_Stability - modifies accumulators: ESP brake + motor cut, TCS / ABS cuts
    ///   0: RCCP_WheelCollider - consumes accumulators, applies to Unity WheelCollider, resets for next frame
    ///   5: RCCP_Camera - runs after physics state ready
    ///  10: Late components (Customizer, Lod, BodyTilt) - run after everything
    /// </summary>
    private static readonly Dictionary<string, int> ExecutionOrders = new Dictionary<string, int>() {

        // === SINGLETONS (-50) ===
        // These must be ready before ANY component tries to access them
        { "RCCP_SceneManager", -50 },
        { "RCCP_InputManager", -50 },
        { "RCCP_SkidmarksManager", -50 },

        // === INPUT WRITERS (-11) ===
        // AI / Recorder call Inputs.OverrideInputs which CarController.PlayerInputs (at -10) reads same frame.
        // Order 0 here would make every AI/replay input apply 1 fixed frame late.
        { "RCCP_AI", -11 },
        { "RCCP_Recorder", -11 },

        // === CORE CONTROLLER (-10) ===
        // Main vehicle controller - initializes all child components via GetAllComponents()
        { "RCCP_CarController", -10 },

        // === LIMITER (-8) ===
        // Limiter writes Engine.cutFuel each frame; Engine reads cutFuel in RevLimiter().
        // Limiter must run before Engine so the directive is honored same frame.
        { "RCCP_Limiter", -8 },

        // === DRIVETRAIN CHAIN (-7 to -4) ===
        // Each stage's FixedUpdate reads the field its upstream stored, recomputes, and re-fires Output()
        // which stores in the next stage's field. Same-frame ordering ensures the chain completes in 1 frame
        // instead of pipelining across N frames as it does at undefined order 0.
        { "RCCP_Engine", -7 },           // Producer; fires outputEvent → Clutch.ReceiveOutput stores
        { "RCCP_Clutch", -6 },           // Reads stored, applies slip, fires Output → Gearbox stores
        // RCCP_Gearbox at -5 below (tied with parent containers; no FixedUpdate conflict)
        { "RCCP_Differential", -4 },     // Reads stored, splits L/R, calls Axle.ReceiveOutput storing motor torque

        // === PARENT CONTAINERS + GEARBOX (-5) ===
        // Parents must be registered with CarController BEFORE their child components
        // (Awake/OnEnable order). Otherwise child components get disabled in Register().
        // None of the parents have FixedUpdate, so RCCP_Gearbox sharing this priority is safe.
        { "RCCP_OtherAddons", -5 },      // Parent of: AI, Nos, Recorder, FuelTank, Exhausts, etc.
        { "RCCP_Axles", -5 },            // Parent of: RCCP_Axle components
        { "RCCP_Lights", -5 },           // Parent of: RCCP_Light components
        { "RCCP_Exhausts", -5 },         // Parent of: RCCP_Exhaust components
        { "RCCP_Gearbox", -5 },          // Drivetrain stage between Clutch and Differential

        // === AERODYNAMICS (-3) ===
        // Writes Rigidbody.centerOfMass (when dynamicCOM=true) and Rigidbody.linearDamping every fixed frame.
        // Must run before WheelCollider physics so the suspension solver sees the current values.
        { "RCCP_AeroDynamics", -3 },

        // === TORQUE ACCUMULATOR PIPELINE (-2, -1, 0) ===
        // RCCP_Axle populates wheel torque accumulators (AddMotorTorque) BEFORE
        // RCCP_Stability modifies them (ESP zeros motor on braked powered wheels), BEFORE
        // RCCP_WheelCollider consumes them in its FixedUpdate.
        // Without this ordering, motor torque and ESP brake torque can fight on the
        // same driven wheel (e.g. RWD rear during understeer, FWD front during oversteer).
        { "RCCP_Axle", -2 },             // Populates wheel motor/brake torque accumulators
        { "RCCP_Stability", -1 },        // Modifies accumulators: ESP brake + motor cut
        { "RCCP_WheelCollider", 0 },     // Consumes accumulators, applies to Unity WheelCollider, resets

        // === CAMERA (5) ===
        // Slightly late to ensure vehicle state is fully ready
        { "RCCP_Camera", 5 },

        // === LATE EXECUTION (10) ===
        // These run after all core systems are ready
        { "RCCP_Customizer", 10 },       // Needs all systems ready for customization
        { "RCCP_Lod", 10 },              // Level of detail - runs after rendering setup
        { "RCCP_BodyTilt", 10 },         // Visual effect - runs late
    };

    /// <summary>
    /// Static constructor - called when Unity loads assemblies.
    /// </summary>
    static RCCP_ScriptExecutionOrderManager() {

        // Delay execution to ensure all scripts are loaded
        EditorApplication.delayCall += OnDelayedInit;

    }

    /// <summary>
    /// Delayed initialization to ensure Unity is fully ready.
    /// </summary>
    private static void OnDelayedInit() {

        EditorApplication.delayCall -= OnDelayedInit;
        ValidateExecutionOrders();

    }

    /// <summary>
    /// Validates and sets execution orders for all RCCP scripts.
    /// </summary>
    public static void ValidateExecutionOrders() {

        bool anyChanged = false;

        foreach (var kvp in ExecutionOrders) {

            string scriptName = kvp.Key;
            int targetOrder = kvp.Value;

            MonoScript script = FindScript(scriptName);

            if (script == null) {
                // Script not found - might not be installed yet
                continue;
            }

            int currentOrder = MonoImporter.GetExecutionOrder(script);

            if (currentOrder != targetOrder) {

                MonoImporter.SetExecutionOrder(script, targetOrder);
                anyChanged = true;
                Debug.Log($"[RCCP] Set execution order for {scriptName}: {targetOrder}");

            }

        }

        if (anyChanged)
            Debug.Log("[RCCP] Script execution order validated successfully.");

    }

    /// <summary>
    /// Resets all RCCP script execution orders to default (0).
    /// </summary>
    public static void ResetExecutionOrders() {

        foreach (var kvp in ExecutionOrders) {

            string scriptName = kvp.Key;
            MonoScript script = FindScript(scriptName);

            if (script == null)
                continue;

            int currentOrder = MonoImporter.GetExecutionOrder(script);

            if (currentOrder != 0) {

                MonoImporter.SetExecutionOrder(script, 0);
                Debug.Log($"[RCCP] Reset execution order for {scriptName} to 0");

            }

        }

        Debug.Log("[RCCP] All script execution orders have been reset to default.");

    }

    /// <summary>
    /// Shows current execution orders in the console.
    /// </summary>
    public static void ShowExecutionOrders() {

        Debug.Log("[RCCP] Current Script Execution Orders:");

        foreach (var kvp in ExecutionOrders) {

            string scriptName = kvp.Key;
            int targetOrder = kvp.Value;

            MonoScript script = FindScript(scriptName);

            if (script == null) {
                Debug.Log($"  {scriptName}: NOT FOUND");
                continue;
            }

            int currentOrder = MonoImporter.GetExecutionOrder(script);
            string status = currentOrder == targetOrder ? "OK" : $"MISMATCH (expected {targetOrder})";
            Debug.Log($"  {scriptName}: {currentOrder} [{status}]");

        }

    }

    /// <summary>
    /// Finds a MonoScript by class name.
    /// </summary>
    /// <param name="className">Name of the class to find.</param>
    /// <returns>MonoScript asset if found, null otherwise.</returns>
    private static MonoScript FindScript(string className) {

        // Search for script assets matching the class name
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {className}");

        foreach (string guid in guids) {

            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

            if (script != null && script.name == className)
                return script;

        }

        return null;

    }

}
#endif

//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// Centralized cache for RCCP scene data and vehicles.
/// Optimized for performance with lazy loading and smart caching.
/// </summary>
public static class RCCP_SceneDataCache {

    #region Cached Data

    // Vehicle references.
    private static List<RCCP_CarController> cachedVehicles = new List<RCCP_CarController>();

    // Component collections per vehicle.
    private static Dictionary<RCCP_CarController, VehicleComponentStatus> vehicleComponentStatus = new Dictionary<RCCP_CarController, VehicleComponentStatus>();

    // Statistics.
    private static SceneStatistics currentStatistics;

    // Cache validity.
    private static bool isCacheDirty = true;
    private static double lastCacheTime;
    private static Scene lastCachedScene;
    private const double CACHE_LIFETIME = 1.0; // 1 second cache lifetime.

    // Lifecycle tracking.
    private static int usageCount;
    private static bool isInitialized;

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the cache system.
    /// </summary>
    public static void Initialize() {

        usageCount++;

        if (isInitialized)
            return;

        EditorApplication.hierarchyChanged += MarkCacheDirty;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        isInitialized = true;
        MarkCacheDirty();

    }

    /// <summary>
    /// Updates the cache if needed.
    /// </summary>
    public static void Update() {

        Scene currentScene = EditorSceneManager.GetActiveScene();

        if (isCacheDirty || lastCachedScene != currentScene ||
            (EditorApplication.timeSinceStartup - lastCacheTime) > CACHE_LIFETIME) {

            RefreshCache();

        }

    }

    /// <summary>
    /// Forces cache refresh.
    /// </summary>
    public static void ForceRefresh() {

        RefreshCache();

    }

    /// <summary>
    /// Gets all cached vehicles.
    /// </summary>
    public static List<RCCP_CarController> GetVehicles(string filter = "") {

        Update();

        if (string.IsNullOrEmpty(filter))
            return new List<RCCP_CarController>(cachedVehicles);

        string lowerFilter = filter.ToLower();
        return cachedVehicles.Where(v => v != null &&
            v.gameObject.name.ToLower().Contains(lowerFilter)
        ).ToList();

    }

    /// <summary>
    /// Gets component status for a specific vehicle.
    /// </summary>
    public static VehicleComponentStatus GetVehicleComponentStatus(RCCP_CarController vehicle) {

        Update();

        if (vehicle != null && vehicleComponentStatus.TryGetValue(vehicle, out VehicleComponentStatus status)) {
            return status;
        }

        return new VehicleComponentStatus();

    }

    /// <summary>
    /// Gets scene statistics.
    /// </summary>
    public static SceneStatistics GetStatistics() {

        Update();
        return currentStatistics;

    }

    /// <summary>
    /// Cleanup the cache system.
    /// </summary>
    public static void Cleanup() {

        if (usageCount > 0)
            usageCount--;

        if (usageCount > 0)
            return;

        if (!isInitialized)
            return;

        EditorApplication.hierarchyChanged -= MarkCacheDirty;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        isInitialized = false;
        ClearCache();

    }

    #endregion

    #region Private Methods

    private static void RefreshCache() {

        // Cache vehicles.
#pragma warning disable CS0618
        cachedVehicles = Object.FindObjectsByType<RCCP_CarController>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
#pragma warning restore CS0618

        // Cache component status for each vehicle.
        vehicleComponentStatus.Clear();
        foreach (var vehicle in cachedVehicles) {

            if (vehicle == null) continue;

            var status = new VehicleComponentStatus();

            // Check required components.
            status.hasEngine = vehicle.GetComponentInChildren<RCCP_Engine>(true) != null;
            status.hasGearbox = vehicle.GetComponentInChildren<RCCP_Gearbox>(true) != null;
            status.hasClutch = vehicle.GetComponentInChildren<RCCP_Clutch>(true) != null;
            status.hasDifferential = vehicle.GetComponentInChildren<RCCP_Differential>(true) != null;
            status.hasAxles = vehicle.GetComponentInChildren<RCCP_Axles>(true) != null;
            status.hasInput = vehicle.GetComponentInChildren<RCCP_Input>(true) != null;

            // Check optional components.
            status.hasAudio = vehicle.GetComponentInChildren<RCCP_Audio>(true) != null;
            status.hasLights = vehicle.GetComponentInChildren<RCCP_Lights>(true) != null;
            status.hasStability = vehicle.GetComponentInChildren<RCCP_Stability>(true) != null;
            status.hasDamage = vehicle.GetComponentInChildren<RCCP_Damage>(true) != null;
            status.hasOtherAddons = vehicle.GetComponentInChildren<RCCP_OtherAddons>(true) != null;

            // Count components.
            status.totalRequiredComponents = 6;
            status.activeRequiredComponents = 0;
            if (status.hasEngine) status.activeRequiredComponents++;
            if (status.hasGearbox) status.activeRequiredComponents++;
            if (status.hasClutch) status.activeRequiredComponents++;
            if (status.hasDifferential) status.activeRequiredComponents++;
            if (status.hasAxles) status.activeRequiredComponents++;
            if (status.hasInput) status.activeRequiredComponents++;

            status.totalOptionalComponents = 5;
            status.activeOptionalComponents = 0;
            if (status.hasAudio) status.activeOptionalComponents++;
            if (status.hasLights) status.activeOptionalComponents++;
            if (status.hasStability) status.activeOptionalComponents++;
            if (status.hasDamage) status.activeOptionalComponents++;
            if (status.hasOtherAddons) status.activeOptionalComponents++;

            vehicleComponentStatus[vehicle] = status;

        }

        // Update statistics.
        UpdateStatistics();

        // Update cache state.
        isCacheDirty = false;
        lastCacheTime = EditorApplication.timeSinceStartup;
        lastCachedScene = EditorSceneManager.GetActiveScene();

    }

    private static void UpdateStatistics() {

        int fullyConfigured = 0;
        int partiallyConfigured = 0;
        int missingRequired = 0;

        foreach (var kvp in vehicleComponentStatus) {

            var status = kvp.Value;

            if (status.activeRequiredComponents == status.totalRequiredComponents) {
                fullyConfigured++;
            } else if (status.activeRequiredComponents > 0) {
                partiallyConfigured++;
            } else {
                missingRequired++;
            }

        }

        currentStatistics = new SceneStatistics {

            totalVehicles = cachedVehicles.Count,
            fullyConfiguredVehicles = fullyConfigured,
            partiallyConfiguredVehicles = partiallyConfigured,
            vehiclesWithMissingRequired = missingRequired,
            hasRCCPSettings = RCCP_Settings.Instance != null,
            hasRCCPSceneManager = Object.FindAnyObjectByType<RCCP_SceneManager>(FindObjectsInactive.Include) != null

        };

    }

    private static void ClearCache() {

        cachedVehicles.Clear();
        vehicleComponentStatus.Clear();
        isCacheDirty = true;

    }

    private static void MarkCacheDirty() {

        isCacheDirty = true;

    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) {

        MarkCacheDirty();

    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state) {

        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.EnteredPlayMode) {

            MarkCacheDirty();

        }

    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Structure holding component status for a vehicle.
    /// </summary>
    public struct VehicleComponentStatus {

        // Required components.
        [Tooltip("Whether the vehicle has an Engine component attached.")]
        public bool hasEngine;
        [Tooltip("Whether the vehicle has a Gearbox component attached.")]
        public bool hasGearbox;
        [Tooltip("Whether the vehicle has a Clutch component attached.")]
        public bool hasClutch;
        [Tooltip("Whether the vehicle has a Differential component attached.")]
        public bool hasDifferential;
        [Tooltip("Whether the vehicle has Axle components attached.")]
        public bool hasAxles;
        [Tooltip("Whether the vehicle has an Input component attached.")]
        public bool hasInput;

        // Optional components.
        [Tooltip("Whether the vehicle has an Audio component attached.")]
        public bool hasAudio;
        [Tooltip("Whether the vehicle has a Lights component attached.")]
        public bool hasLights;
        [Tooltip("Whether the vehicle has a Stability component attached.")]
        public bool hasStability;
        [Tooltip("Whether the vehicle has a Damage component attached.")]
        public bool hasDamage;
        [Tooltip("Whether the vehicle has any other addon components attached.")]
        public bool hasOtherAddons;

        // Counts.
        [Tooltip("Total number of required drivetrain components expected.")]
        public int totalRequiredComponents;
        [Tooltip("Number of required components currently present and active.")]
        public int activeRequiredComponents;
        [Tooltip("Total number of optional components expected.")]
        public int totalOptionalComponents;
        [Tooltip("Number of optional components currently present and active.")]
        public int activeOptionalComponents;

        public int TotalComponents => totalRequiredComponents + totalOptionalComponents;
        public int ActiveComponents => activeRequiredComponents + activeOptionalComponents;

        public bool IsFullyConfigured => activeRequiredComponents == totalRequiredComponents;
        public bool HasCriticalIssues => activeRequiredComponents < totalRequiredComponents;

    }

    /// <summary>
    /// Structure holding scene statistics.
    /// </summary>
    public struct SceneStatistics {

        [Tooltip("Total number of RCCP vehicles found in the current scene.")]
        public int totalVehicles;
        [Tooltip("Vehicles with all required and optional components present.")]
        public int fullyConfiguredVehicles;
        [Tooltip("Vehicles missing one or more optional components.")]
        public int partiallyConfiguredVehicles;
        [Tooltip("Vehicles missing one or more required drivetrain components.")]
        public int vehiclesWithMissingRequired;
        [Tooltip("Whether the RCCP_Settings ScriptableObject is loaded in Resources.")]
        public bool hasRCCPSettings;
        [Tooltip("Whether an RCCP_SceneManager singleton exists in the scene.")]
        public bool hasRCCPSceneManager;

    }

    #endregion

}

#endif

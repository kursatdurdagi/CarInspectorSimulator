//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Scene manager that contains current player vehicle, current player camera, current player UI, current player character, recording/playing mechanim, and other vehicles as well.
/// </summary>
[DefaultExecutionOrder(-50)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/RCCP Scene Manager")]
public class RCCP_SceneManager : RCCP_Singleton<RCCP_SceneManager> {

    /// <summary>
    /// Current active player vehicle.
    /// </summary>
    [Tooltip("Currently registered player vehicle receiving input.")]
    public RCCP_CarController activePlayerVehicle;

    /// <summary>
    /// Current active player camera as RCCP Camera.
    /// </summary>
    [Tooltip("Active RCCP Camera following the player vehicle.")]
    public RCCP_Camera activePlayerCamera;

    /// <summary>
    /// Current active UI canvas.
    /// </summary>
    [Tooltip("Active UI manager canvas for the player.")]
    public RCCP_UIManager activePlayerCanvas;

    /// <summary>
    /// Current active main camera.
    /// </summary>
    [Tooltip("Cached reference to the scene's main camera.")]
    public Camera activeMainCamera;

    /// <summary>
    /// Last selected player vehicle.
    /// </summary>
    private RCCP_CarController lastActivePlayerVehicle;

    /// <summary>
    /// Registers the lastly spawned vehicle as player vehicle.
    /// </summary>
    [Tooltip("Automatically register the last spawned vehicle as the player vehicle.")]
    public bool registerLastVehicleAsPlayer = true;

    /// <summary>
    /// Disables the UI when there is no any player vehicle.
    /// </summary>
    [Tooltip("Hide the UI canvas when no player vehicle is registered.")]
    public bool disableUIWhenNoPlayerVehicle = false;

    /// <summary>
    /// Multithreading is supported on this platform?
    /// </summary>
    public static bool multithreadingSupported = false;

    /// <summary>
    /// All vehicles on the scene.
    /// </summary>
    [Tooltip("All RCCP vehicles currently present in the scene.")]
    public List<RCCP_CarController> allVehicles = new List<RCCP_CarController>();

    /// <summary>
    /// All terrains on the scene.
    /// </summary>
    [Tooltip("All active terrains discovered in the scene for surface detection.")]
    public Terrain[] allTerrains;

    /// <summary>
    /// Cached terrain data used for per-wheel surface detection (splatmap lookups for ground material friction).
    /// </summary>
    public class Terrains {

        //	Terrain data.
        /// <summary>
        /// Reference to the Unity Terrain component.
        /// </summary>
        [Tooltip("Reference to the Unity Terrain component.")]
        public Terrain terrain;
        /// <summary>
        /// Cached TerrainData for splatmap and alphamap access.
        /// </summary>
        [Tooltip("Cached TerrainData for splatmap and alphamap access.")]
        public TerrainData mTerrainData;
        /// <summary>
        /// Physics material assigned to the terrain collider.
        /// </summary>
#if UNITY_2023_3_OR_NEWER
    [Tooltip("Physics material assigned to the terrain collider.")]
    public PhysicsMaterial terrainCollider;    // DOTS
#else
        [Tooltip("Physics material assigned to the terrain collider.")]
        public PhysicMaterial terrainCollider;                   // PhysX
#endif
        /// <summary>
        /// Width of the terrain alphamap in pixels.
        /// </summary>
        [Tooltip("Width of the terrain alphamap in pixels.")]
        public int alphamapWidth;
        /// <summary>
        /// Height of the terrain alphamap in pixels.
        /// </summary>
        [Tooltip("Height of the terrain alphamap in pixels.")]
        public int alphamapHeight;

        /// <summary>
        /// Cached splatmap alpha values [x, y, textureIndex] for surface detection.
        /// </summary>
        [Tooltip("Cached splatmap alpha values for surface detection.")]
        public float[,,] mSplatmapData;
        /// <summary>
        /// Number of terrain texture layers in the splatmap.
        /// </summary>
        [Tooltip("Number of terrain texture layers in the splatmap.")]
        public float mNumTextures;

    }

    /// <summary>
    /// All collected terrains with cached data for surface friction lookups.
    /// </summary>
    [Tooltip("Cached terrain data used for per-wheel splatmap surface lookups.")]
    public Terrains[] terrains;     //  All collected terrains with custom class.
    /// <summary>
    /// Whether all terrain data has been initialized and is ready for surface queries.
    /// </summary>
    [HideInInspector] public bool terrainsInitialized = false;        //  All terrains are initialized yet?

    private bool asyncAttempted = false;
    private bool asyncReceived = false;

    private void Awake() {

        //  Listening events.
        RCCP_Events.OnRCCPCameraSpawned += RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned += RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned += RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned += RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPUIDestroyed += RCCP_Events_OnRCCPUIDestroyed;
        RCCP_Events.OnRCCPDestroyed += RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed += RCCP_Events_OnRCCPAIDestroyed;

        //  Instantiate telemetry UI if it's enabled in RCCP Settings.
        if (RCCPSettings.useTelemetry)
            Instantiate(RCCPSettings.RCCPTelemetry, Vector3.zero, Quaternion.identity);

        // Overriding Fixed TimeStep.
        if (RCCPSettings.overrideFixedTimeStep)
            Time.fixedDeltaTime = RCCPSettings.fixedTimeStep;

        // Overriding FPS.
        if (RCCPSettings.overrideFPS)
            Application.targetFrameRate = RCCPSettings.maxFPS;

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Load();

    }

    #region ONSPAWNED

    /// <summary>
    /// When RCCP vehicle is spawned.
    /// </summary>
    /// <param name="RCCP">The player vehicle that was spawned.</param>
    private void RCCP_Events_OnRCCPSpawned(RCCP_CarController RCCP) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(RCCP))
            allVehicles.Add(RCCP);

        //  Registers the last spawned vehicle as player vehicle.
        if (registerLastVehicleAsPlayer)
            RegisterPlayer(RCCP);

    }

    /// <summary>
    /// When an AI vehicle is spawned, adds it to the tracked vehicles list.
    /// </summary>
    /// <param name="AI">The AI-controlled vehicle that was spawned.</param>
    private void RCCP_Events_OnRCCPAISpawned(RCCP_CarController AI) {

        //  If all vehicles list doesn't contain spawned vehicle, add it to the list.
        if (!allVehicles.Contains(AI))
            allVehicles.Add(AI);

    }

    /// <summary>
    /// When RCCP Camera spawned.
    /// </summary>
    /// <param name="BCGCamera">The RCCP camera instance that was spawned.</param>
    private void RCCP_Events_OnRCCPCameraSpawned(RCCP_Camera cam) {

        activePlayerCamera = cam;

        //  If there's already a player vehicle registered, set it as the camera target.
        //  This handles the case where the vehicle was pre-placed in the scene and fired
        //  OnRCCPSpawned before the camera was ready.
        if (activePlayerVehicle != null)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// When RCCP Canvas spawned.
    /// </summary>
    /// <param name="UI">The UI manager instance that was spawned.</param>
    private void RCCP_Events_OnRCCPUISpawned(RCCP_UIManager UI) {

        activePlayerCanvas = UI;

    }

    /// <summary>
    /// When RCCP Canvas destroyed or disabled.
    /// </summary>
    /// <param name="UI">The UI manager instance that was destroyed.</param>
    private void RCCP_Events_OnRCCPUIDestroyed(RCCP_UIManager UI) {

        if (activePlayerCanvas == UI)
            activePlayerCanvas = null;

    }

    #endregion

    #region ONDESTROYED

    /// <summary>
    /// When a vehicle destroyed.
    /// </summary>
    /// <param name="RCCP">The player vehicle that was destroyed.</param>
    private void RCCP_Events_OnRCCPPlayerDestroyed(RCCP_CarController RCCP) {

        if (allVehicles.Contains(RCCP))
            allVehicles.Remove(RCCP);

    }

    /// <summary>
    /// When an ai vehicle destroyed.
    /// </summary>
    /// <param name="RCCP">The AI vehicle that was destroyed.</param>
    private void RCCP_Events_OnRCCPAIDestroyed(RCCP_CarController AI) {

        if (allVehicles.Contains(AI))
            allVehicles.Remove(AI);

    }

    #endregion

    private void Start() {

        //  Getting all terrains.
        StartCoroutine(GetAllTerrains());

        //  Checking mutlithreading.
        StartCoroutine(CheckMT());

#if BCG_URP
        Invoke(nameof(CheckURPCamera), .5f);
#endif

    }

#if BCG_URP
    private void CheckURPCamera() {

        if (activeMainCamera != null) {

            activeMainCamera.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var cameraData);

            if (cameraData != null && cameraData.renderPostProcessing == false)
                cameraData.renderPostProcessing = true;

            if (cameraData == null)
                Debug.LogError("'UniversalAdditionalCameraData' component couldn't found on the RCCP_Camera! Please select the 'actual camera' of the RCCP_Camera in your editor, it will add the missing component to the camera. Otherwise you can't see the lensflares along with post processing effects.");

        }

    }
#endif

    private IEnumerator CheckMT() {

        if (!RCCPSettings.multithreading) {

            asyncAttempted = false;
            asyncReceived = false;
            multithreadingSupported = false;
            yield break;

        }

        asyncAttempted = false;
        asyncReceived = false;

        CheckingMT();

        float timer = 1f;

        while (timer > 0) {

            timer -= Time.deltaTime;
            yield return null;

        }

        if (asyncAttempted && asyncReceived)
            multithreadingSupported = true;
        else
            multithreadingSupported = false;

        if (!multithreadingSupported)
            Debug.LogWarning("Multithreading is disabled on this platform, async can't be used with it. Regular methods will be used.");

        yield return null;

    }

    private async void CheckingMT() {

        asyncAttempted = true;
        asyncReceived = false;

        await Task.Run(() => { });

        asyncReceived = true;

    }

    /// <summary>
    /// Getting all terrains.
    /// </summary>
    /// <returns></returns>
    public IEnumerator GetAllTerrains() {

        yield return new WaitForFixedUpdate();
        allTerrains = Terrain.activeTerrains;
        yield return new WaitForFixedUpdate();

        //  If terrains found...
        if (allTerrains != null && allTerrains.Length >= 1) {

            terrains = new Terrains[allTerrains.Length];

            for (int i = 0; i < allTerrains.Length; i++) {

                if (allTerrains[i].terrainData == null) {

                    Debug.LogError("Terrain data of the " + allTerrains[i].transform.name + " is missing! Check the terrain data...");
                    yield return null;

                }

            }

            //  Initializing terrains.
            for (int i = 0; i < terrains.Length; i++) {

                terrains[i] = new Terrains();
                terrains[i].terrain = allTerrains[i];
                terrains[i].mTerrainData = allTerrains[i].terrainData;
                allTerrains[i].TryGetComponent<TerrainCollider>(out var terrainCol);
                terrains[i].terrainCollider = terrainCol != null ? terrainCol.sharedMaterial : null;
                terrains[i].alphamapWidth = allTerrains[i].terrainData.alphamapWidth;
                terrains[i].alphamapHeight = allTerrains[i].terrainData.alphamapHeight;

                terrains[i].mSplatmapData = allTerrains[i].terrainData.GetAlphamaps(0, 0, terrains[i].alphamapWidth, terrains[i].alphamapHeight);
                terrains[i].mNumTextures = terrains[i].mSplatmapData.Length / (terrains[i].alphamapWidth * terrains[i].alphamapHeight);

            }

            terrainsInitialized = true;

        }

    }

    private void Update() {

        //  When player vehicle changed...
        if (activePlayerVehicle) {

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChanged();

            if (activePlayerVehicle != lastActivePlayerVehicle)
                RCCP_Events.Event_OnVehicleChangedToVehicle(activePlayerVehicle);

            lastActivePlayerVehicle = activePlayerVehicle;

        }

        //  Checking UI canvas.
        if (disableUIWhenNoPlayerVehicle && activePlayerCanvas)
            CheckCanvas();

        //  Getting main camera.
        if (Camera.main != null)
            activeMainCamera = Camera.main;

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle.
    /// </summary>
    /// <param name="playerVehicle">The vehicle to register as the active player vehicle.</param>
    public void RegisterPlayer(RCCP_CarController playerVehicle) {

        activePlayerVehicle = playerVehicle;

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle">The vehicle to register as the active player vehicle.</param>
    /// <param name="isControllable">Whether the vehicle should accept player input.</param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Registers the target vehicle as player vehicle. Also sets controllable state and engine state of the vehicle.
    /// </summary>
    /// <param name="playerVehicle"></param>
    /// <param name="isControllable"></param>
    /// <param name="engineState"></param>
    public void RegisterPlayer(RCCP_CarController playerVehicle, bool isControllable, bool engineState) {

        activePlayerVehicle = playerVehicle;
        activePlayerVehicle.SetCanControl(isControllable);
        activePlayerVehicle.SetEngine(engineState);

        if (activePlayerCamera)
            activePlayerCamera.SetTarget(activePlayerVehicle);

    }

    /// <summary>
    /// Deregisters the player vehicle.
    /// </summary>
    public void DeRegisterPlayer() {

        if (activePlayerVehicle)
            activePlayerVehicle.SetCanControl(false);

        activePlayerVehicle = null;

        if (activePlayerCamera)
            activePlayerCamera.RemoveTarget();

    }

    /// <summary>
    /// Manages UI canvas visibility based on whether a controllable player vehicle exists.
    /// </summary>
    public void CheckCanvas() {

        //if (!activePlayerVehicle || !activePlayerVehicle.canControl || !activePlayerVehicle.gameObject.activeInHierarchy || !activePlayerVehicle.enabled) {

        //    activePlayerCanvas.SetDisplayType(RCC_UIDashboardDisplay.DisplayType.Off);

        //    return;

        //}

        //if (activePlayerCanvas.displayType != RCC_UIDashboardDisplay.DisplayType.Customization)
        //    activePlayerCanvas.displayType = RCC_UIDashboardDisplay.DisplayType.Full;

    }

    ///<summary>
    /// Activates behavior override and sets the behavior preset at the given index.
    ///</summary>
    /// <param name="behaviorIndex">Index into the behaviorTypes array to activate.</param>
    public void SetBehavior(int behaviorIndex) {

        RCCPSettings.overrideBehavior = true;
        RCCPSettings.behaviorSelectedIndex = behaviorIndex;

        RCCP_Events.Event_OnBehaviorChanged();

    }

    /// <summary>
    /// Sets the active mobile controller type (TouchScreen, Gyro, SteeringWheel, or Joystick).
    /// </summary>
    /// <param name="mobileController">The mobile controller type to activate.</param>
    public void SetMobileController(RCCP_Settings.MobileController mobileController) {

        RCCPSettings.mobileController = mobileController;

    }

    /// <summary>
    /// Changes current camera mode.
    /// </summary>
    public void ChangeCamera() {

        if (activePlayerCamera)
            activePlayerCamera.ChangeCamera();

    }

    /// <summary>
    /// Transport player vehicle the specified position and rotation.
    /// </summary>
    /// <param name="position">Position.</param>
    /// <param name="rotation">Rotation.</param>
    public void Transport(Vector3 position, Quaternion rotation) {

        if (activePlayerVehicle) {

            RigidbodyInterpolation interpolation = activePlayerVehicle.Rigid.interpolation;
            activePlayerVehicle.Rigid.interpolation = RigidbodyInterpolation.None;

            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.Rigid.MovePosition(position);
            activePlayerVehicle.Rigid.MoveRotation(rotation);

            activePlayerVehicle.Rigid.linearVelocity = Vector3.zero;
            activePlayerVehicle.Rigid.angularVelocity = Vector3.zero;

            activePlayerVehicle.Rigid.interpolation = interpolation;

            for (int i = 0; i < activePlayerVehicle.AllWheelColliders.Length; i++)
                activePlayerVehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            RCCP_TrailerController trailer = activePlayerVehicle.ConnectedTrailer;

            if (trailer) {

                trailer.TryGetComponent<Rigidbody>(out var trailerRigid);

                if (trailerRigid) {

                    if (trailerRigid) {

                        // Store original interpolation settings for trailer
                        RigidbodyInterpolation trailerInterpolation = trailerRigid.interpolation;
                        trailerRigid.interpolation = RigidbodyInterpolation.None;

                        // Calculate new trailer position and rotation relative to the vehicle
                        Vector3 trailerOffset = trailerRigid.transform.position - activePlayerVehicle.transform.position;
                        Quaternion trailerRelativeRotation = Quaternion.Inverse(activePlayerVehicle.transform.rotation) * trailerRigid.transform.rotation;

                        // Move trailer relative to the new vehicle position
                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        trailerRigid.MovePosition(position + rotation * trailerOffset);
                        trailerRigid.MoveRotation(rotation * trailerRelativeRotation);

                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        // Restore interpolation settings for trailer
                        trailerRigid.interpolation = trailerInterpolation;

                    }

                }

            }

            Physics.SyncTransforms();

        }

    }

    /// <summary>
    /// Transport target vehicle the specified position and rotation.
    /// </summary>
    /// <param name="vehicle">The vehicle to transport.</param>
    /// <param name="position">Target world position.</param>
    /// <param name="rotation">Target world rotation.</param>
    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation) {
     
        if (vehicle) {

            RigidbodyInterpolation interpolation = vehicle.Rigid.interpolation;
            vehicle.Rigid.interpolation = RigidbodyInterpolation.None;

            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.Rigid.MovePosition(position);
            vehicle.Rigid.MoveRotation(rotation);

            vehicle.Rigid.linearVelocity = Vector3.zero;
            vehicle.Rigid.angularVelocity = Vector3.zero;

            vehicle.Rigid.interpolation = interpolation;

            for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

            RCCP_TrailerController trailer = vehicle.ConnectedTrailer;

            if (trailer) {

                trailer.TryGetComponent<Rigidbody>(out var trailerRigid);

                if (trailerRigid) {

                    if (trailerRigid) {

                        // Store original interpolation settings for trailer
                        RigidbodyInterpolation trailerInterpolation = trailerRigid.interpolation;
                        trailerRigid.interpolation = RigidbodyInterpolation.None;

                        // Calculate new trailer position and rotation relative to the vehicle
                        Vector3 trailerOffset = trailerRigid.transform.position - vehicle.transform.position;
                        Quaternion trailerRelativeRotation = Quaternion.Inverse(vehicle.transform.rotation) * trailerRigid.transform.rotation;

                        // Move trailer relative to the new vehicle position
                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        trailerRigid.MovePosition(position + rotation * trailerOffset);
                        trailerRigid.MoveRotation(rotation * trailerRelativeRotation);

                        trailerRigid.linearVelocity = Vector3.zero;
                        trailerRigid.angularVelocity = Vector3.zero;

                        // Restore interpolation settings for trailer
                        trailerRigid.interpolation = trailerInterpolation;

                    }

                }

            }

            Physics.SyncTransforms();

        }

    }

    /// <summary>
    /// Transports the target vehicle to the specified position and rotation, with optional velocity reset.
    /// </summary>
    /// <param name="vehicle">The vehicle to transport.</param>
    /// <param name="position">Target world position.</param>
    /// <param name="rotation">Target world rotation.</param>
    /// <param name="resetVelocity">If true, resets the vehicle's linear and angular velocity to zero.</param>
    public void Transport(RCCP_CarController vehicle, Vector3 position, Quaternion rotation, bool resetVelocity) {

        if (vehicle) {

            if (resetVelocity) {

                RigidbodyInterpolation interpolation = vehicle.Rigid.interpolation;
                vehicle.Rigid.interpolation = RigidbodyInterpolation.None;

                vehicle.Rigid.linearVelocity = Vector3.zero;
                vehicle.Rigid.angularVelocity = Vector3.zero;

                vehicle.Rigid.MovePosition(position);
                vehicle.Rigid.MoveRotation(rotation);

                vehicle.Rigid.linearVelocity = Vector3.zero;
                vehicle.Rigid.angularVelocity = Vector3.zero;

                vehicle.Rigid.interpolation = interpolation;

                for (int i = 0; i < vehicle.AllWheelColliders.Length; i++)
                    vehicle.AllWheelColliders[i].WheelCollider.motorTorque = 0f;

                Physics.SyncTransforms();

            } else {

                vehicle.Rigid.MovePosition(position);
                vehicle.Rigid.MoveRotation(rotation);

                Physics.SyncTransforms();

            }

        }

    }

    private void OnDisable() {

        if (RCCPSettings.autoSaveLoadInputRebind)
            RCCP_RebindSaveLoad.Save();

    }

    private void OnDestroy() {

        RCCP_Events.OnRCCPCameraSpawned -= RCCP_Events_OnRCCPCameraSpawned;
        RCCP_Events.OnRCCPSpawned -= RCCP_Events_OnRCCPSpawned;
        RCCP_Events.OnRCCPAISpawned -= RCCP_Events_OnRCCPAISpawned;
        RCCP_Events.OnRCCPUISpawned -= RCCP_Events_OnRCCPUISpawned;
        RCCP_Events.OnRCCPUIDestroyed -= RCCP_Events_OnRCCPUIDestroyed;
        RCCP_Events.OnRCCPDestroyed -= RCCP_Events_OnRCCPPlayerDestroyed;
        RCCP_Events.OnRCCPAIDestroyed -= RCCP_Events_OnRCCPAIDestroyed;

    }

}

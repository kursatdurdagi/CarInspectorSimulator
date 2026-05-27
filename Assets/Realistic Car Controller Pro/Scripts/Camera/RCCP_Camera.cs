//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Main RCCP Camera controller. Includes 6 different camera modes with many customizable settings. 
/// It doesn't use different cameras on your scene like *other* assets. Simply it parents the camera to their positions, that's all. 
/// No need to be Einstein. Also supports collision detection.
/// Summary: This class manages various camera modes (TPS, FPS, Wheel, Fixed, Cinematic, Top, TruckTrailer),
/// camera transitions, occlusion checks, orbit controls, auto-focus, collisions, and field of view adjustments.
/// </summary>
[DefaultExecutionOrder(5)]
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Camera/RCCP Camera")]
public class RCCP_Camera : RCCP_GenericComponent {

    /// <summary>
    /// Camera target with custom class.
    /// Summary: Holds references to the car controller and child cameras (hood, wheel).
    /// </summary>
    [System.Serializable]
    public class CameraTarget {

        /// <summary>
        /// Summary: The active vehicle for this camera target.
        /// </summary>
        [Tooltip("The vehicle this camera is currently following.")]
        public RCCP_CarController playerVehicle;

        /// <summary>
        /// Summary: Cached reference to the hood camera found on the vehicle.
        /// </summary>
        private RCCP_HoodCamera _hoodCamera;
        public RCCP_HoodCamera HoodCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_hoodCamera)
                    _hoodCamera = playerVehicle.GetComponentInChildren<RCCP_HoodCamera>();

                return _hoodCamera;

            }

        }

        /// <summary>
        /// Summary: Cached reference to the wheel camera found on the vehicle.
        /// </summary>
        private RCCP_WheelCamera _wheelCamera;
        public RCCP_WheelCamera WheelCamera {

            get {

                if (!playerVehicle)
                    return null;

                if (!_wheelCamera)
                    _wheelCamera = playerVehicle.GetComponentInChildren<RCCP_WheelCamera>();

                return _wheelCamera;

            }

        }

    }

    /// <summary>
    /// Summary: Returns the position of the player vehicle, optionally using CenterPosition if calculateCenterPosition is true.
    /// </summary>
    public Vector3 GetPlayerPosition {
        get {

            if (cameraTarget == null)
                return Vector3.zero;

            if (cameraTarget.playerVehicle == null)
                return Vector3.zero;

            if (!calculateCenterPosition)
                return cameraTarget.playerVehicle.transform.position;

            return cameraTarget.playerVehicle.CenterPosition;

        }

    }

    /// <summary>
    /// Summary: Returns the rotation of the player vehicle.
    /// </summary>
    public Quaternion GetPlayerRotation {
        get {

            if (cameraTarget == null)
                return Quaternion.identity;

            if (cameraTarget.playerVehicle == null)
                return Quaternion.identity;

            return cameraTarget.playerVehicle.transform.rotation;

        }

    }

    /// <summary>
    /// Actual camera target with custom class. Holds references to the car controller, hood, and wheel cameras.
    /// </summary>
    [Header("Camera Target")]
    [Tooltip("Reference to the vehicle and its sub-cameras (hood, wheel) that this camera follows.")]
    public CameraTarget cameraTarget = new CameraTarget();

    /// <summary>
    /// Summary: Indicates if the camera is actively rendering. If false, the camera won't be shown.
    /// </summary>
    [Tooltip("When disabled, the camera stops rendering and hides its GameObject.")]
    public bool isRendering = true;

    /// <summary>
    /// Summary: Reference to the actual Camera component. It's a child of this GameObject's pivot.
    /// </summary>
    [Tooltip("The child Camera component used for rendering the game view.")]
    public Camera actualCamera;

    /// <summary>
    /// Summary: Pivot center of the camera. Used for making offsets and collision movements.
    /// </summary>
    [Tooltip("Pivot object for camera offsets, collision displacement, and parenting.")]
    public GameObject pivot;

    /// <summary>
    /// Summary: Received inputs from the player (mouse, scroll, etc.).
    /// </summary>
    [Tooltip("Cached player input state used for orbit, zoom, and look-back controls.")]
    public RCCP_Inputs inputs;

    /// <summary>
    /// Summary: If true, uses the center of the vehicle for positioning instead of transform.position.
    /// </summary>
    [Tooltip("Uses the vehicle's bounding-box center instead of its transform origin for positioning.")]
    public bool calculateCenterPosition = true;

    /// <summary>
    /// Summary: Enumeration of all camera modes available in RCCP_Camera.
    /// </summary>
    public enum CameraMode { TPS, FPS, WHEEL, FIXED, CINEMATIC, TOP, TRUCKTRAILER }

    /// <summary>
    /// Summary: Currently selected camera mode.
    /// </summary>
    [Header("Camera Mode")]
    [Tooltip("Currently active camera mode (TPS, FPS, Wheel, Fixed, Cinematic, Top, or TruckTrailer).")]
    public CameraMode cameraMode = CameraMode.TPS;

    /// <summary>
    /// Last camera mode before this frame. Used to check when the camera mode has changed.
    /// </summary>
    private CameraMode lastCameraMode = CameraMode.TPS;

    /// <summary>
    /// Summary: Enumeration for two types of TPS modes (TPS1, TPS2).
    /// TPS1 is the original; TPS2 is a more stable approach.
    /// </summary>
    public enum TPSMode { TPS1, TPS2 }

    /// <summary>
    /// Summary: Chooses which TPS mode to use.
    /// </summary>
    [Tooltip("Selects TPS algorithm: TPS1 is the classic approach, TPS2 is more stable with drift support.")]
    public TPSMode tPSMode = TPSMode.TPS2;

    /// <summary>
    /// Summary: Singleton reference to the RCCP_FixedCamera instance.
    /// </summary>
    private RCCP_FixedCamera FixedCamera { get { return RCCP_FixedCamera.Instance; } }

    /// <summary>
    /// Summary: Singleton reference to the RCCP_CinematicCamera instance.
    /// </summary>
    private RCCP_CinematicCamera CinematicCamera { get { return RCCP_CinematicCamera.Instance; } }

    /// <summary>
    /// Summary: If true, locks the X rotation of the camera to the vehicle's X.
    /// </summary>
    [Header("TPS Settings")]
    [Tooltip("Locks the camera's pitch (X) rotation to follow the vehicle's pitch.")]
    public bool TPSLockX = true;

    /// <summary>
    /// Summary: If true, locks the Y rotation of the camera to the vehicle's Y.
    /// </summary>
    [Tooltip("Locks the camera's yaw (Y) rotation to follow the vehicle's heading.")]
    public bool TPSLockY = true;

    /// <summary>
    /// Summary: If true, locks the Z rotation of the camera to the vehicle's Z.
    /// </summary>
    [Tooltip("Locks the camera's roll (Z) rotation to follow the vehicle's roll.")]
    public bool TPSLockZ = false;

    /// <summary>
    /// Summary: Camera rotation won't track the vehicle if the vehicle is not grounded (in free fall).
    /// </summary>
    [Tooltip("Stops tracking vehicle rotation when airborne so the camera stays level.")]
    public bool TPSFreeFall = true;

    /// <summary>
    /// Summary: If true, adjusts distance, height, and pitch angle relative to vehicle velocity for dynamic camera behavior.
    /// </summary>
    [Tooltip("Dynamically adjusts camera offset based on vehicle acceleration for a cinematic feel.")]
    public bool TPSDynamic = false;

    /// <summary>
    /// Summary: Enables or disables top camera mode usage when cycling cameras.
    /// </summary>
    [Header("Camera Modes Toggle")]
    [Tooltip("Includes the top-down camera when cycling through camera modes.")]
    public bool useTopCameraMode = false;

    /// <summary>
    /// Summary: Enables or disables hood camera mode usage when cycling cameras.
    /// </summary>
    [Tooltip("Includes the hood (first-person) camera when cycling through camera modes.")]
    public bool useHoodCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables orbit control in TPS camera mode.
    /// </summary>
    [Tooltip("Allows the player to orbit the camera with mouse or touch in TPS mode.")]
    public bool useOrbitInTPSCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables orbit control in hood camera mode.
    /// </summary>
    [Tooltip("Allows the player to orbit the camera with mouse or touch in hood mode.")]
    public bool useOrbitInHoodCameraMode = true;

    /// <summary>
    /// Summary: If true, orbit control only works while mouse button (or UI) is held.
    /// </summary>
    [Tooltip("Requires holding the mouse button or touch to orbit instead of free-look.")]
    public bool useOrbitOnlyHolding = true;

    /// <summary>
    /// Summary: Indicates whether player is currently holding the orbit button (mouse/touch).
    /// </summary>
    [Tooltip("True while the player is holding the orbit input button.")]
    public bool orbitHolding = false;

    /// <summary>
    /// Summary: Enables or disables wheel camera mode usage when cycling cameras.
    /// </summary>
    [Tooltip("Includes the wheel close-up camera when cycling through camera modes.")]
    public bool useWheelCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables fixed camera mode usage when cycling cameras.
    /// </summary>
    [Tooltip("Includes the fixed roadside camera when cycling through camera modes.")]
    public bool useFixedCameraMode = true;

    /// <summary>
    /// Summary: Enables or disables cinematic camera mode usage when cycling cameras.
    /// </summary>
    [Tooltip("Includes the cinematic chase camera when cycling through camera modes.")]
    public bool useCinematicCameraMode = true;

    /// <summary>
    /// Summary: If true, uses an orthographic camera for the top camera mode.
    /// </summary>
    [Tooltip("Switches the top-down camera to orthographic projection instead of perspective.")]
    public bool useOrthoForTopCamera = false;

    /// <summary>
    /// Summary: If true, checks for occlusion between camera and vehicle to avoid clipping or obstacles.
    /// </summary>
    [Header("Occlusion")]
    [Tooltip("Prevents the camera from clipping through walls by repositioning on collision.")]
    public bool useOcclusion = true;

    /// <summary>
    /// Summary: Camera will be occluded by these layers (SphereCast).
    /// </summary>
    [Tooltip("Physics layers that can block the camera line-of-sight to the vehicle.")]
    public LayerMask occlusionLayerMask = -1;

    private readonly RaycastHit[] occlusionHits = new RaycastHit[16];
    private int cachedRCCPLayer = -1;
    private int cachedRCCPPropLayer = -1;
    private int cachedRCCPDetachableLayer = -1;

    /// <summary>
    /// Summary: Stores the occluded position if an obstacle is detected.
    /// </summary>
    private Vector3 occluded = Vector3.zero;

    /// <summary>
    /// Summary: If true, camera mode will change automatically (like every 10 seconds) for cinematic effect.
    /// </summary>
    [Header("Auto Camera Change")]
    [Tooltip("Automatically cycles through camera modes at a fixed interval for cinematic replays.")]
    public bool useAutoChangeCamera = false;

    /// <summary>
    /// Summary: A timer used for auto-switching camera modes if useAutoChangeCamera is true.
    /// </summary>
    [Min(0f), Tooltip("Elapsed seconds since the last automatic camera switch.")]
    public float autoChangeCameraTimer = 0f;

    /// <summary>
    /// Summary: Predefined angle for top camera mode.
    /// </summary>
    [Header("Top Camera")]
    [Tooltip("Euler angles defining the viewing direction of the top-down camera.")]
    public Vector3 topCameraAngle = new Vector3(45f, 45f, 0f);

    /// <summary>
    /// Summary: Distance from the target in top camera mode.
    /// </summary>
    [Min(0f), Tooltip("How far the top-down camera is placed from the vehicle.")]
    public float topCameraDistance = 100f;

    /// <summary>
    /// Summary: Maximum forward offset in top camera mode, based on vehicle speed.
    /// </summary>
    [Min(0f), Tooltip("Maximum forward look-ahead offset in top-down mode based on speed.")]
    public float maximumZDistanceOffset = 10f;

    /// <summary>
    /// Summary: Internally used offset for top camera mode, recalculated based on current speed.
    /// </summary>
    [Min(0f)] private float topCameraDistanceOffset = 0f;

    /// <summary>
    /// Summary: Internally used target position (used in top camera and others).
    /// </summary>
    private Vector3 targetPosition = Vector3.zero;

    /// <summary>
    /// Summary: Used for detecting if vehicle direction changes (reverse/forward) to reset orbit.
    /// </summary>
    [Min(-1)] private int direction = 1;

    /// <summary>
    /// Summary: Main TPS camera distance from the player.
    /// </summary>
    [Header("TPS Transform")]
    [Range(0f, 20f), Tooltip("Distance behind the vehicle in third-person mode.")]
    public float TPSDistance = 6.5f;

    /// <summary>
    /// Summary: Main TPS camera height above the player.
    /// </summary>
    [Range(0f, 10f), Tooltip("Height above the vehicle in third-person mode.")]
    public float TPSHeight = 1.5f;

    /// <summary>
    /// Summary: Damping factor for smoothing rotation in TPS mode.
    /// </summary>
    [Range(0f, 1f), Tooltip("How quickly the camera rotation catches up to the vehicle (higher = faster).")]
    public float TPSRotationDamping = .5f;

    /// <summary>
    /// Summary: Maximum tilt angle for the camera in TPS mode, based on lateral velocity.
    /// </summary>
    [Range(0f, 25f), Tooltip("Maximum roll tilt angle applied when the vehicle moves sideways.")]
    public float TPSTiltMaximum = 15f;

    /// <summary>
    /// Summary: Tilt multiplier used to fine-tune tilt effect in TPS mode.
    /// </summary>
    [Range(0f, 1.5f), Tooltip("Scales the lateral tilt effect in TPS mode.")]
    public float TPSTiltMultiplier = 1f;

    /// <summary>
    /// Summary: Additional Yaw offset in TPS mode.
    /// </summary>
    [Range(-45f, 45f), Tooltip("Constant horizontal rotation offset applied in TPS mode.")]
    public float TPSYaw = 0f;

    /// <summary>
    /// Summary: Additional Pitch offset in TPS mode.
    /// </summary>
    [Range(-45f, 45f), Tooltip("Constant vertical look-down angle applied in TPS mode.")]
    public float TPSPitch = 7.5f;

    /// <summary>
    /// Summary: If true, the camera automatically adjusts TPSDistance/TPSHeight based on the vehicle's bounds.
    /// </summary>
    [Tooltip("Automatically adjusts distance and height based on the vehicle's bounding box.")]
    public bool TPSAutoFocus = true;

    /// <summary>
    /// Summary: If true, camera automatically rotates for reverse gear.
    /// </summary>
    [Tooltip("Swings the camera behind the vehicle when reversing.")]
    public bool TPSAutoReverse = true;

    /// <summary>
    /// Summary: If true, camera will shake slightly in TPS mode, based on vehicle speed.
    /// </summary>
    [Tooltip("Adds a subtle speed-dependent shake to the TPS camera for immersion.")]
    public bool TPSShake = true;

    /// <summary>
    /// Summary: TPS position offset local to the vehicle.
    /// </summary>
    [Tooltip("Local-space position offset relative to the vehicle in TPS mode.")]
    public Vector3 TPSOffset = new Vector3(0f, 0f, .2f);

    /// <summary>
    /// Summary: Internally used temporary offset for TPS, allowing smooth transitions.
    /// </summary>
    private Vector3 _TPSOffset = Vector3.zero;

    /// <summary>
    /// Summary: Initial rotation for the camera when the game starts (TPS).
    /// </summary>
    [Tooltip("Initial Euler rotation applied to the TPS camera on spawn or reset.")]
    public Vector3 TPSStartRotation = new Vector3(0f, 0f, 0f);

    private bool TPSStartRotationApplied = false;

    /// <summary>
    /// Summary: Stores the last rotation of the camera in TPS mode to smooth transitions.
    /// </summary>
    private Quaternion TPSLastRotation = Quaternion.identity;

    /// <summary>
    /// Summary: The current tilt angle used when tilting the camera in TPS mode.
    /// </summary>
    private float TPSTiltAngle = 0f;

    /// <summary>
    /// Summary: Main field of view target used by all camera modes. The actual camera FOV will lerp to this.
    /// </summary>
    [Header("Field of View")]
    [Min(0f), Tooltip("Target FOV the camera lerps toward each frame.")]
    public float targetFieldOfView = 60f;

    /// <summary>
    /// Summary: The minimum FOV in TPS mode, based on speed.
    /// </summary>
    [Range(10f, 90f), Tooltip("FOV when the vehicle is stationary in TPS mode.")]
    public float TPSMinimumFOV = 40f;

    /// <summary>
    /// Summary: The maximum FOV in TPS mode, based on speed.
    /// </summary>
    [Range(10f, 160f), Tooltip("FOV at maximum speed in TPS mode.")]
    public float TPSMaximumFOV = 60f;

    /// <summary>
    /// Summary: Default hood camera FOV.
    /// </summary>
    [Range(10f, 160f), Tooltip("Field of view used in hood (first-person) camera mode.")]
    public float hoodCameraFOV = 60f;

    /// <summary>
    /// Summary: Default wheel camera FOV.
    /// </summary>
    [Range(10f, 160f), Tooltip("Field of view used in wheel close-up camera mode.")]
    public float wheelCameraFOV = 60f;

    /// <summary>
    /// Summary: Minimum orthographic size when in top camera mode, based on speed.
    /// </summary>
    [Min(0f), Tooltip("Smallest orthographic size (or FOV) for the top-down camera at low speed.")]
    public float minimumOrtSize = 10f;

    /// <summary>
    /// Summary: Maximum orthographic size when in top camera mode, based on speed.
    /// </summary>
    [Min(0f), Tooltip("Largest orthographic size (or FOV) for the top-down camera at high speed.")]
    public float maximumOrtSize = 20f;

    /// <summary>
    /// Summary: Keeps track of how many times the camera has switched modes, used in the switch/case cycle.
    /// </summary>
    [Min(0)] internal int cameraSwitchCount = 0;

    /// <summary>
    /// Summary: Velocity values for smoothing the camera's position (used by MoveTowards).
    /// </summary>
    private float xVelocity, yVelocity, zVelocity = 0f;
    private Vector3 accelerationVelocity = Vector3.zero;

    /// <summary>
    /// Dynamic camera offset vector in local space. X = lateral velocity (sway), Y = unused, Z = longitudinal acceleration (surge).
    /// </summary>
    [Tooltip("Dynamic camera offset: X is lateral sway from velocity, Z is longitudinal surge from acceleration.")]
    public Vector3 acceleration = Vector3.zero;

    /// <summary>
    /// Summary: Stores the last frame's velocity for calculating acceleration.
    /// </summary>
    [Tooltip("Previous frame's local velocity, used to compute acceleration.")]
    public Vector3 lastVelocity = Vector3.zero;

    /// <summary>
    /// Summary: Smoothed version of the acceleration to avoid abrupt camera movements.
    /// </summary>
    [Tooltip("Damped acceleration vector used for smooth dynamic camera offsets.")]
    public Vector3 acceleration_Smoothed = Vector3.zero;

    /// <summary>
    /// Summary: Position offset for collision effects.
    /// </summary>
    private Vector3 collisionPos = Vector3.zero;

    /// <summary>
    /// Summary: Rotation offset for collision effects.
    /// </summary>
    private Quaternion collisionRot = Quaternion.identity;

    /// <summary>
    /// Summary: Multiplier for zooming in/out with scroll input in TPS mode.
    /// </summary>
    [Header("Zoom")]
    [Range(.5f, 10f), Tooltip("Sensitivity of the scroll wheel for zooming in and out.")]
    public float zoomScrollMultiplier = 5f;

    /// <summary>
    /// Summary: Internally tracked scroll input for zoom in TPS mode.
    /// </summary>
    private float zoomScroll = 0;

    /// <summary>
    /// Summary: Minimum and maximum allowable scroll values in TPS mode.
    /// </summary>
    [Min(0f), Tooltip("Closest zoom distance added by scroll input.")]
    public float minimumScroll = 0f;
    [Min(0f), Tooltip("Farthest zoom distance added by scroll input.")]
    public float maximumScroll = 5f;

    /// <summary>
    /// Summary: Raw orbit X and Y input from the mouse / UI.
    /// </summary>
    private float orbitX, orbitY = 0f;

    /// <summary>
    /// Summary: Smoothed orbit X and Y values.
    /// </summary>
    private float orbitX_Smoothed, orbitY_Smoothed = 0f;

    /// <summary>
    /// Summary: Minimum Orbit Y angle.
    /// </summary>
    [Header("Orbit Controls")]
    [Range(-90f, 0f), Tooltip("Lowest vertical angle the player can orbit to (looking up).")]
    public float minOrbitY = -15f;

    /// <summary>
    /// Summary: Maximum Orbit Y angle.
    /// </summary>
    [Range(0f, 90f), Tooltip("Highest vertical angle the player can orbit to (looking down).")]
    public float maxOrbitY = 70f;

    /// <summary>
    /// Summary: Orbit X speed (mouse drag speed).
    /// </summary>
    [Min(0f), Tooltip("Horizontal orbit sensitivity for mouse or touch input.")]
    public float orbitXSpeed = 100f;

    /// <summary>
    /// Summary: Orbit Y speed (mouse drag speed).
    /// </summary>
    [Min(0f), Tooltip("Vertical orbit sensitivity for mouse or touch input.")]
    public float orbitYSpeed = 100f;

    /// <summary>
    /// Summary: Smooth factor for orbit transitions.
    /// </summary>
    [Min(0f), Tooltip("Smoothing speed for orbit input interpolation.")]
    public float orbitSmooth = 25f;

    /// <summary>
    /// Summary: If true, orbits reset automatically after some time if the vehicle is moving.
    /// </summary>
    [Tooltip("Automatically resets the orbit angles when the vehicle is moving and input stops.")]
    public bool orbitReset = true;
    private float orbitResetTimer = 0f;
    private float oldOrbitX, oldOrbitY = 0f;

    /// <summary>
    /// Summary: If true, camera looks behind the vehicle in TPS mode (e.g., reversing).
    /// </summary>
    [Tooltip("When true, the TPS camera looks behind the vehicle (e.g., rear-view while reversing).")]
    public bool lookBackNow = false;

    private Coroutine autoFocusCoroutine;

    private void Awake() {

        // Getting Camera.
        actualCamera = GetComponentInChildren<Camera>();

        // Creating pivot position of the camera.
        if (!pivot) {

            pivot = transform.Find("Pivot").gameObject;

            if (!pivot)
                pivot = new GameObject("Pivot");

            pivot.transform.SetParent(transform);
            pivot.transform.localPosition = Vector3.zero;
            pivot.transform.localRotation = Quaternion.identity;
            actualCamera.transform.SetParent(pivot.transform, true);

        }

    }

    private void OnEnable() {

        // Calling this event when BCG Camera spawned.
        RCCP_Events.Event_OnRCCPCameraSpawned(this);

        // Listening player vehicle collisions for crashing effects.
        RCCP_InputManager.OnChangedCamera += RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera += RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera += RCCP_InputManager_OnHoldOrbitCamera;
        RCCP_Events.OnRCCPCollision += RCCP_Events_OnRCCPCollision;

    }

    /// <summary>
    /// Look back or not on TPS mode.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnLookBackCamera(bool state) {

        lookBackNow = state;

    }

    /// <summary>
    /// Orbit camera if holding.
    /// </summary>
    /// <param name="state"></param>
    private void RCCP_InputManager_OnHoldOrbitCamera(bool state) {

        orbitHolding = state;

    }

    /// <summary>
    /// When player presses the change camera button, cycles to the next camera mode.
    /// </summary>
    private void RCCP_InputManager_OnChangedCamera() {

        ChangeCamera();

    }

    /// <summary>
    /// Applies collision offset to the camera pivot for impact shake effect.
    /// </summary>
    private void RCCP_Events_OnRCCPCollision(RCCP_CarController rccp, Collision collision) {

        if (cameraTarget == null || cameraTarget.playerVehicle == null)
            return;

        if (rccp != cameraTarget.playerVehicle)
            return;

        if (cameraMode != CameraMode.TPS && cameraMode != CameraMode.TRUCKTRAILER)
            return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce < 5f)
            return;

        float magnitude = Mathf.Clamp01(impactForce / 40f);

        Vector3 dir = transform.InverseTransformDirection(collision.relativeVelocity.normalized);

        collisionPos = dir * magnitude * .5f;
        collisionRot = Quaternion.Euler(dir.z * magnitude * 5f, 0f, -dir.x * magnitude * 5f);

    }

    /// <summary>
    /// Sets target vehicle of the camera, resets camera modes, and possibly auto-focuses if enabled.
    /// Summary: This is how you initialize which car the camera should follow.
    /// </summary>
    /// <param name="player"></param>
    public void SetTarget(RCCP_CarController player) {

        // Setting target vehicle.
        cameraTarget = new CameraTarget {
            playerVehicle = player
        };

        // If auto focus is enabled, adjust distance and height of the camera automatically.
        if (TPSAutoFocus)
            StartAutoFocus(AutoFocus());

        // And reset the camera modes.
        ResetCamera();

        TPSLastRotation = player.transform.rotation;

    }

    /// <summary>
    /// Clears the current camera target.
    /// </summary>
    public void RemoveTarget() {

        transform.SetParent(null);
        cameraTarget.playerVehicle = null;

    }

    private void Update() {

        // If it's inactive, disable the camera's GameObject.
        if (!isRendering) {

            if (actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(false);

        } else {

            if (!actualCamera.gameObject.activeSelf)
                actualCamera.gameObject.SetActive(true);

        }

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Receive inputs.
        Inputs();

        // Lerp current field of view to target field of view.
        actualCamera.fieldOfView = Mathf.Lerp(actualCamera.fieldOfView, targetFieldOfView, Time.deltaTime * 5f);

        // Smooth acceleration for dynamic camera offset.
        if (TPSDynamic)
            acceleration_Smoothed = Vector3.SmoothDamp(acceleration_Smoothed, acceleration, ref accelerationVelocity, .3f);
        else
            acceleration_Smoothed = Vector3.zero;

    }

    private void LateUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        if (Time.timeScale <= 0)
            return;

        // Checks occlusion if it's enabled.
        if (!useOcclusion)
            occluded = Vector3.zero;

        // Run the corresponding method with the chosen camera mode.
        switch (cameraMode) {

            case CameraMode.TPS:

                if (useOrbitInTPSCameraMode)
                    ORBIT();

                if (tPSMode == TPSMode.TPS1)
                    TPS();
                else
                    TPS2();

                break;

            case CameraMode.FPS:

                if (useOrbitInHoodCameraMode)
                    ORBIT();

                FPS();

                break;

            case CameraMode.WHEEL:
                WHEEL();
                break;

            case CameraMode.FIXED:
                FIXED();
                break;

            case CameraMode.CINEMATIC:
                CINEMATIC();
                break;

            case CameraMode.TOP:
                TOP();
                break;

            case CameraMode.TRUCKTRAILER:

                if (useOrbitInTPSCameraMode)
                    ORBIT();

                TRUCKTRAILER();
                break;

        }

        // If camera mode changed during this frame, reset the modes.
        if (lastCameraMode != cameraMode)
            ResetCamera();

        lastCameraMode = cameraMode;

        if (useAutoChangeCamera)
            autoChangeCameraTimer += Time.deltaTime;
        else
            autoChangeCameraTimer = 0f;

        // If auto change camera is enabled, change the camera mode each 10 seconds.
        if (useAutoChangeCamera && autoChangeCameraTimer >= 10) {

            autoChangeCameraTimer = 0f;
            ChangeCamera();

        }

    }

    private void FixedUpdate() {

        if (!IsCameraActive())
            return;

        // Early out if we don't have the player vehicle.
        if (!cameraTarget.playerVehicle)
            return;

        // Even if we have the player vehicle and it's disabled, return.
        if (!cameraTarget.playerVehicle.gameObject.activeSelf)
            return;

        Vector3 currentSpeed = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity);
        acceleration = currentSpeed - lastVelocity;
        lastVelocity = currentSpeed;

        acceleration = Vector3.ClampMagnitude(acceleration, .1f);

        // X: lateral sway from velocity (not acceleration) — smoother camera response to slides.
        // Z: longitudinal surge from actual acceleration, scaled up and fading at high speed.
        float speedFade = 1f - Mathf.InverseLerp(0f, 200f, cameraTarget.playerVehicle.absoluteSpeed);
        acceleration.x = currentSpeed.x * .04f * speedFade;
        acceleration.y = 0f;
        acceleration.z *= 1.75f * speedFade;

    }

    /// <summary>
    /// Summary: Receives player inputs for orbit and scroll in camera modes.
    /// </summary>
    private void Inputs() {

        // Receiving player inputs
        inputs = RCCP_InputManager.Instance.GetInputs();

        if (!useOrbitOnlyHolding) {

            // Setting orbits.
            orbitX += inputs.mouseInput.x;
            orbitY -= inputs.mouseInput.y;

        }

        // Clamping orbit Y.
        orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

        // Smoothing orbits.
        orbitX_Smoothed = Mathf.Lerp(orbitX_Smoothed, orbitX, Time.deltaTime * orbitSmooth);
        orbitY_Smoothed = Mathf.Lerp(orbitY_Smoothed, orbitY, Time.deltaTime * orbitSmooth);

        // Zoom scroll input via new Input System.
        if (Mouse.current != null) {
            zoomScroll += Mouse.current.scroll.ReadValue().y * zoomScrollMultiplier * Time.deltaTime * 0.01f;
            zoomScroll = Mathf.Clamp(zoomScroll, -maximumScroll, -minimumScroll);
        }

    }

    /// <summary>
    /// Summary: Cycles to the next camera mode, skipping modes not in use or missing references.
    /// </summary>
    public void ChangeCamera() {

        cameraSwitchCount++;

        // Adjust if your total camera modes have changed
        if (cameraSwitchCount > 6)  // now that we have 7 modes, adjust the max as needed
            cameraSwitchCount = 0;

        switch (cameraSwitchCount) {
            case 0:
                cameraMode = CameraMode.TPS;
                break;
            case 1:
                if (useHoodCameraMode && cameraTarget.HoodCamera)
                    cameraMode = CameraMode.FPS;
                else
                    ChangeCamera();
                break;
            case 2:
                if (useWheelCameraMode && cameraTarget.WheelCamera)
                    cameraMode = CameraMode.WHEEL;
                else
                    ChangeCamera();
                break;
            case 3:
                if (useFixedCameraMode && FixedCamera)
                    cameraMode = CameraMode.FIXED;
                else
                    ChangeCamera();
                break;
            case 4:
                if (useCinematicCameraMode && CinematicCamera)
                    cameraMode = CameraMode.CINEMATIC;
                else
                    ChangeCamera();
                break;
            case 5:
                if (useTopCameraMode)
                    cameraMode = CameraMode.TOP;
                else
                    ChangeCamera();
                break;
            case 6: // your new camera mode index
                cameraMode = CameraMode.TRUCKTRAILER;
                break;
        }

    }

    /// <summary>
    /// Summary: Directly switches to the specified camera mode.
    /// </summary>
    /// <param name="mode"></param>
    public void ChangeCamera(CameraMode mode) {

        cameraMode = mode;

    }

    /// <summary>
    /// Summary: First-person camera mode (hood/driver camera). 
    /// If orbit is on for hood camera, uses orbitX and orbitY for rotation.
    /// </summary>
    private void FPS() {

        // Assigning orbit rotation, and transform rotation.
        if (useOrbitInHoodCameraMode)
            transform.rotation = GetPlayerRotation * Quaternion.Euler(orbitY_Smoothed, orbitX_Smoothed, 0f);
        else
            transform.rotation = GetPlayerRotation;

    }

    /// <summary>
    /// Summary: Wheel camera mode, typically used for close-ups of wheels.
    /// If occluded, reverts to TPS mode.
    /// </summary>
    private void WHEEL() {

        if (useOcclusion) {

            occluded = OccludeRay();

            if (occluded != Vector3.zero)
                ChangeCamera(CameraMode.TPS);

        }

    }

    /// <summary>
    /// Summary: Original TPS camera logic with distance, height, tilt, etc.
    /// </summary>
    private void TPS() {

        if (cameraTarget.playerVehicle.ConnectedTrailer && cameraTarget.playerVehicle.ConnectedTrailer.gameObject.activeSelf && cameraMode != CameraMode.TRUCKTRAILER) {

            ChangeCamera(CameraMode.TRUCKTRAILER);
            return;

        }

        // Setting rotation of the camera.
        transform.rotation = TPSLastRotation;

        // If TPS Auto Reverse is enabled and vehicle is moving backwards, reset X and Y orbits when direction changes.
        direction = cameraTarget.playerVehicle.direction;

        int directionAngle = 0;
        float rotDamp = TPSRotationDamping;

        if (TPSAutoReverse)
            directionAngle = (direction == 1 ? 0 : 180);

        // If player is looking back, override directionAngle and set full damping for quick response.
        if (lookBackNow) {

            directionAngle = 180;
            rotDamp = 1f;

        }

        // If TPSFreeFall is true and not grounded, reduce damping drastically.
        if (TPSFreeFall && Time.time >= 1f) {

            if (!cameraTarget.playerVehicle.IsGrounded)
                rotDamp = -10f;

        }

        float xAngle = 0f;

        // If locked, smoothly track vehicle's X angle.
        if (TPSLockX)
            xAngle = Mathf.SmoothDampAngle(transform.eulerAngles.x, cameraTarget.playerVehicle.transform.eulerAngles.x * (directionAngle == 180 ? -1f : 1f), ref xVelocity, 1f - rotDamp);

        if (useOrbitInTPSCameraMode && orbitY != 0)
            xAngle = orbitY_Smoothed;

        float yAngle = 0f;

        // If locked, smoothly track vehicle's Y angle (plus orbit if active).
        if (TPSLockY) {

            if (!useOrbitInTPSCameraMode) {
                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);
            } else {

                if (orbitX != 0)
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + orbitX_Smoothed, ref yVelocity, .025f);
                else
                    yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraTarget.playerVehicle.transform.eulerAngles.y + directionAngle, ref yVelocity, 1f - rotDamp);

            }

        } else {

            if (useOrbitInTPSCameraMode && orbitX != 0)
                yAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, orbitX_Smoothed, ref yVelocity, .025f);

        }

        float zAngle = 0f;

        // If locked, smoothly track vehicle's Z angle.
        if (TPSLockZ)
            zAngle = Mathf.SmoothDampAngle(transform.eulerAngles.z, cameraTarget.playerVehicle.transform.eulerAngles.z, ref zVelocity, 1f - rotDamp);

        if (float.IsNaN(yAngle) || float.IsNaN(xAngle) || float.IsNaN(zAngle))
            return;

        // Position at the target.
        Vector3 position = GetPlayerPosition;

        // Rotation at the target.
        Quaternion rotation = Quaternion.Euler(xAngle, yAngle, zAngle);

        // Then offset by distance behind.
        position += rotation * (-Vector3.forward * (TPSDistance + zoomScroll));
        position += GetPlayerRotation * TPSOffset;
        position += Vector3.up * TPSHeight;

        float addTPSPitch = 0f;

        // Dynamic camera: offset position by smoothed acceleration (frame-rate independent).
        if (TPSDynamic) {
            position -= GetPlayerRotation * (acceleration_Smoothed * 2f);
            addTPSPitch = -acceleration_Smoothed.z * 10f;
        }

        transform.SetPositionAndRotation(position, rotation);

        ApplyCollisionOffset();

        CalculateTPSFieldOfView();

        float xVel = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;
        xVel = Mathf.Clamp(xVel, -1f, 1f);

        // Calculate tilt (TPS1 uses quadratic response).
        TPSTiltAngle = TPSTiltMaximum * (xVel * Mathf.Abs(xVel) / 250f);
        TPSTiltAngle *= TPSTiltMultiplier;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        TPSLastRotation = transform.rotation;

        ApplyFinalRotation(addTPSPitch);
        ApplyShake();

    }

    /// <summary>
    /// Summary: A more stable TPS camera logic (TPS2). Includes drift support, orbit, and better rotation handling.
    /// </summary>
    private void TPS2() {

        if (cameraTarget.playerVehicle.ConnectedTrailer && cameraTarget.playerVehicle.ConnectedTrailer.gameObject.activeSelf && cameraMode != CameraMode.TRUCKTRAILER) {

            ChangeCamera(CameraMode.TRUCKTRAILER);
            return;

        }

        transform.rotation = TPSLastRotation;

        int dir = TPSAutoReverse ? cameraTarget.playerVehicle.direction : 1;
        if (lookBackNow)
            dir *= -1;

        Vector3 vehicleForward = cameraTarget.playerVehicle.transform.forward * dir;

        bool driftMode = false;
        for (int i = 0; i < cameraTarget.playerVehicle.AllWheelColliders.Length; i++) {

            if (cameraTarget.playerVehicle.AllWheelColliders[i] == null)
                continue;

            if (cameraTarget.playerVehicle.AllWheelColliders[i].driftMode) {
                driftMode = true;
                break;
            }

        }

        if (driftMode) {
            Vector3 playerVelocityDirection = cameraTarget.playerVehicle.transform.InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity);
            playerVelocityDirection.y = 0f;
            playerVelocityDirection = cameraTarget.playerVehicle.transform.TransformDirection(playerVelocityDirection);

            vehicleForward = Vector3.Lerp(
                vehicleForward,
                playerVelocityDirection + (cameraTarget.playerVehicle.transform.forward * dir),
                Mathf.InverseLerp(0f, 100f, cameraTarget.playerVehicle.absoluteSpeed)
            );
        }

        Quaternion desiredRotation = Quaternion.LookRotation(vehicleForward, Vector3.up);
        Vector3 desiredEulers = desiredRotation.eulerAngles;

        if (!TPSLockX)
            desiredEulers.x = transform.eulerAngles.x;
        if (!TPSLockY)
            desiredEulers.y = transform.eulerAngles.y;
        if (!TPSLockZ)
            desiredEulers.z = 0f;
        else
            desiredEulers.z = cameraTarget.playerVehicle.transform.eulerAngles.z;

        desiredRotation = Quaternion.Euler(desiredEulers);

        float rotDamp = TPSRotationDamping * .5f;
        if (TPSFreeFall && Time.time >= 1f && !cameraTarget.playerVehicle.IsGrounded)
            rotDamp = 0f;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotDamp * 10f * Time.deltaTime
        );

        Vector3 finalEulers = transform.rotation.eulerAngles;

        if (useOrbitInTPSCameraMode) {
            if (orbitY != 0f)
                finalEulers.x = orbitY_Smoothed;
            if (orbitX != 0f)
                finalEulers.y = orbitX_Smoothed;
            if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
                finalEulers.z = 0f;
        }

        if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
            transform.rotation = Quaternion.Euler(finalEulers);

        if (TPSFreeFall && Time.time >= 1f) {
            if (!cameraTarget.playerVehicle.IsGrounded)
                _TPSOffset = Vector3.Lerp(_TPSOffset, Vector3.zero, Time.deltaTime * 10f);
            else
                _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        } else {
            _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        }

        Vector3 finalPos = GetPlayerPosition
            + GetPlayerRotation * _TPSOffset
            - transform.forward * (TPSDistance + zoomScroll)
            + Vector3.up * TPSHeight;

        float addTPSPitch = 0f;
        if (TPSDynamic) {
            finalPos -= GetPlayerRotation * (acceleration_Smoothed * 2f);
            addTPSPitch = -acceleration_Smoothed.z * 6f;
        }

        transform.position = finalPos;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        ApplyCollisionOffset();

        TPSLastRotation = transform.rotation;

        float localVelocityX = cameraTarget.playerVehicle.transform
            .InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;

        TPSTiltAngle = TPSTiltMaximum
            * (Mathf.Clamp(localVelocityX, -10f, 10f) * 0.04f)
            * TPSTiltMultiplier;

        ApplyFinalRotation(addTPSPitch);
        ApplyShake();
        CalculateTPSFieldOfView();

    }

    /// <summary>
    /// Summary: A specialized TPS camera mode for trucks with trailers, focusing on both truck and trailer center.
    /// </summary>
    private void TRUCKTRAILER() {

        if (!cameraTarget.playerVehicle.ConnectedTrailer) {
            SetTarget(cameraTarget.playerVehicle);
            ChangeCamera(CameraMode.TPS);
            return;
        }

        transform.rotation = TPSLastRotation;

        int dir = TPSAutoReverse ? cameraTarget.playerVehicle.direction : 1;
        if (lookBackNow)
            dir *= -1;

        Vector3 vehicleForward = cameraTarget.playerVehicle.transform.forward * dir;
        Quaternion desiredRotation = Quaternion.LookRotation(vehicleForward, Vector3.up);
        Vector3 desiredEulers = desiredRotation.eulerAngles;

        if (!TPSLockX)
            desiredEulers.x = transform.eulerAngles.x;
        if (!TPSLockY)
            desiredEulers.y = transform.eulerAngles.y;
        if (!TPSLockZ)
            desiredEulers.z = 0f;
        else
            desiredEulers.z = cameraTarget.playerVehicle.transform.eulerAngles.z;

        desiredRotation = Quaternion.Euler(desiredEulers);

        float rotDamp = TPSRotationDamping * .5f;
        if (TPSFreeFall && Time.time >= 1f && !cameraTarget.playerVehicle.IsGrounded)
            rotDamp = 0f;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotDamp * 10f * Time.deltaTime
        );

        Vector3 finalEulers = transform.rotation.eulerAngles;

        if (useOrbitInTPSCameraMode) {
            if (orbitY != 0f)
                finalEulers.x = orbitY_Smoothed;
            if (orbitX != 0f)
                finalEulers.y = orbitX_Smoothed;
            if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
                finalEulers.z = 0f;
        }

        if (Mathf.Abs(orbitX) > Mathf.Epsilon || Mathf.Abs(orbitY) > Mathf.Epsilon)
            transform.rotation = Quaternion.Euler(finalEulers);

        if (TPSFreeFall && Time.time >= 1f) {
            if (!cameraTarget.playerVehicle.IsGrounded)
                _TPSOffset = Vector3.Lerp(_TPSOffset, Vector3.zero, Time.deltaTime * 10f);
            else
                _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        } else {
            _TPSOffset = Vector3.Lerp(_TPSOffset, TPSOffset, Time.deltaTime * 5f);
        }

        Transform truck = cameraTarget.playerVehicle.transform;
        RCCP_TrailerController trailer = cameraTarget.playerVehicle.ConnectedTrailer;

        Vector3 combinedCenter = (truck.position + trailer.CenterPosition) * 0.5f;

        Vector3 finalPos = combinedCenter
            + GetPlayerRotation * _TPSOffset
            - transform.forward * (TPSDistance + zoomScroll)
            + Vector3.up * TPSHeight;

        float addTPSPitch = 0f;
        if (TPSDynamic) {
            finalPos -= GetPlayerRotation * (acceleration_Smoothed * 2f);
            addTPSPitch = -acceleration_Smoothed.z * 6f;
        }

        transform.position = finalPos;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                transform.position = occluded;
        }

        ApplyCollisionOffset(10f);

        TPSLastRotation = transform.rotation;

        float localVelocityX = cameraTarget.playerVehicle.transform
            .InverseTransformDirection(cameraTarget.playerVehicle.Rigid.linearVelocity).x;
        TPSTiltAngle = TPSTiltMaximum
            * (Mathf.Clamp(localVelocityX, -10f, 10f) * 0.04f)
            * TPSTiltMultiplier;

        ApplyFinalRotation(addTPSPitch);
        ApplyShake();
        CalculateTPSFieldOfView();

    }

    /// <summary>
    /// Summary: Fixed camera mode, uses RCCP_FixedCamera logic.
    /// If occluded, moves the camera closer.
    /// </summary>
    private void FIXED() {

        if (FixedCamera.transform.parent != null)
            FixedCamera.transform.SetParent(null);

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                FixedCamera.transform.position = occluded;
        }

    }

    /// <summary>
    /// Summary: Top-down camera mode (orthographic or perspective).
    /// Ortho size / FOV adjusts with speed.
    /// </summary>
    private void TOP() {

        actualCamera.orthographic = useOrthoForTopCamera;

        topCameraDistanceOffset = Mathf.Lerp(0f, maximumZDistanceOffset, cameraTarget.playerVehicle.absoluteSpeed / 100f);
        targetFieldOfView = Mathf.Lerp(minimumOrtSize, maximumOrtSize, cameraTarget.playerVehicle.absoluteSpeed / 100f);
        actualCamera.orthographicSize = Mathf.Lerp(actualCamera.orthographicSize, targetFieldOfView, Time.deltaTime * 3f);

        targetPosition = GetPlayerPosition;
        targetPosition += GetPlayerRotation * Vector3.forward * topCameraDistanceOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(topCameraAngle), Time.deltaTime * 15f);

        pivot.transform.localPosition = new Vector3(0f, 0f, -topCameraDistance);

    }

    /// <summary>
    /// Summary: Receives input for orbiting the camera, resetting if the player hasn't changed orbit for a while.
    /// </summary>
    private void ORBIT() {

        if (Mathf.Abs(orbitX - oldOrbitX) > Mathf.Epsilon) {
            if ((cameraMode == CameraMode.TPS || cameraMode == CameraMode.TRUCKTRAILER) && Mathf.Abs(oldOrbitX) < Mathf.Epsilon && Mathf.Abs(orbitX) > Mathf.Epsilon) {
                orbitX = transform.eulerAngles.y;
                orbitX_Smoothed = orbitX;
            }
            oldOrbitX = orbitX;
            orbitResetTimer = 2f;
        }

        if (Mathf.Abs(orbitY - oldOrbitY) > Mathf.Epsilon) {
            oldOrbitY = orbitY;
            orbitResetTimer = 2f;
        }

        if (orbitResetTimer > 0f)
            orbitResetTimer -= Time.deltaTime;

        orbitResetTimer = Mathf.Clamp(orbitResetTimer, 0f, 2f);

        if (orbitReset && cameraTarget.playerVehicle.absoluteSpeed >= 10f && orbitResetTimer <= 0f) {
            orbitX = 0f;
            orbitY = 0f;
        }

    }

    /// <summary>
    /// Used with mobile UI drag panel for orbiting.
    /// </summary>
    /// <param name="pointerData">Pointer event data containing the drag delta for orbit calculation.</param>
    public void OnDrag(PointerEventData pointerData) {

        if (!useOrbitOnlyHolding)
            return;

        orbitX += pointerData.delta.x * orbitXSpeed / 1000f;
        orbitY -= pointerData.delta.y * orbitYSpeed / 1000f;

        orbitY = Mathf.Clamp(orbitY, minOrbitY, maxOrbitY);

        orbitX_Smoothed = Mathf.Lerp(orbitX_Smoothed, orbitX, Time.deltaTime * orbitSmooth);
        orbitY_Smoothed = Mathf.Lerp(orbitY_Smoothed, orbitY, Time.deltaTime * orbitSmooth);

        orbitResetTimer = 2f;

    }

    /// <summary>
    /// Summary: Cinematic mode uses a separate camera system (RCCP_CinematicCamera).
    /// </summary>
    private void CINEMATIC() {

        if (CinematicCamera.transform.parent != null)
            CinematicCamera.transform.SetParent(null);

        targetFieldOfView = CinematicCamera.targetFOV;

        if (useOcclusion) {
            occluded = OccludeRay();
            if (occluded != Vector3.zero)
                ChangeCamera(CameraMode.TPS);
        }

    }

    /// <summary>
    /// Summary: Resets camera parameters (field of view, offsets, orbits) when switching modes.
    /// </summary>
    public void ResetCamera() {

        if (FixedCamera)
            FixedCamera.canTrackNow = false;

        TPSTiltAngle = 0f;

        collisionPos = Vector3.zero;
        collisionRot = Quaternion.identity;

        actualCamera.transform.localPosition = Vector3.zero;
        actualCamera.transform.localRotation = Quaternion.identity;

        pivot.transform.localPosition = collisionPos;
        pivot.transform.localRotation = collisionRot;

        if (!TPSStartRotationApplied && TPSStartRotation != Vector3.zero) {
            orbitX = TPSStartRotation.y;
            orbitY = TPSStartRotation.x;
            TPSStartRotationApplied = true;
        } else {
            orbitX = 0f;
            orbitY = 0f;
        }

        orbitHolding = false;

        zoomScroll = 0f;

        actualCamera.orthographic = false;
        occluded = Vector3.zero;

        switch (cameraMode) {

            case CameraMode.TPS:
                transform.SetParent(null);
                targetFieldOfView = TPSMinimumFOV;
                break;

            case CameraMode.FPS:
                transform.SetParent(cameraTarget.HoodCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = hoodCameraFOV;
                cameraTarget.HoodCamera.FixShake();
                break;

            case CameraMode.WHEEL:
                transform.SetParent(cameraTarget.WheelCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = wheelCameraFOV;
                cameraTarget.WheelCamera.FixShake();
                break;

            case CameraMode.FIXED:
                transform.SetParent(FixedCamera.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 60;
                FixedCamera.canTrackNow = true;
                break;

            case CameraMode.CINEMATIC:
                transform.SetParent(CinematicCamera.pivot.transform, false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                targetFieldOfView = 30f;
                break;

            case CameraMode.TOP:
                transform.SetParent(null);
                targetFieldOfView = minimumOrtSize;
                targetPosition = GetPlayerPosition;
                targetPosition += GetPlayerRotation * Vector3.forward * topCameraDistanceOffset;
                transform.position = GetPlayerPosition;
                break;

            case CameraMode.TRUCKTRAILER:
                transform.SetParent(null);
                if (cameraTarget != null && cameraTarget.playerVehicle != null) {
                    Transform truck = cameraTarget.playerVehicle.transform;
                    RCCP_TrailerController trailer = cameraTarget.playerVehicle.ConnectedTrailer;

                    if (trailer) {

                        if (trailer.manualSetCameraDistanceAndHeight) {

                            TPSDistance = trailer.TPSDistance;
                            TPSHeight = trailer.TPSHeight;

                        } else {

                            StartAutoFocus(AutoFocus(truck, trailer.transform));

                        }

                    } else {

                        // If auto focus is enabled, adjust distance and height of the camera automatically.
                        if (TPSAutoFocus)
                            StartAutoFocus(AutoFocus(truck));

                    }

                }
                targetFieldOfView = TPSMinimumFOV;
                break;

        }

        actualCamera.fieldOfView = targetFieldOfView;

    }

    /// <summary>
    /// Summary: Enables or disables the camera. If disabled, the camera won't render.
    /// </summary>
    /// <param name="state"></param>
    public void ToggleCamera(bool state) {

        isRendering = state;

    }

    /// <summary>
    /// Decays collision offset and applies it to the pivot transform.
    /// </summary>
    private void ApplyCollisionOffset(float decaySpeed = 5f) {

        if (Time.deltaTime > 0f) {
            collisionPos = Vector3.Lerp(collisionPos, Vector3.zero, Time.deltaTime * decaySpeed);
            collisionRot = Quaternion.Lerp(collisionRot, Quaternion.identity, Time.deltaTime * decaySpeed);
        }

        pivot.transform.localPosition = Vector3.Lerp(pivot.transform.localPosition, collisionPos, Time.deltaTime * 10f);
        pivot.transform.localRotation = Quaternion.Lerp(pivot.transform.localRotation, collisionRot, Time.deltaTime * 10f);

    }

    /// <summary>
    /// Applies pitch, yaw, and tilt as a final rotation offset.
    /// </summary>
    private void ApplyFinalRotation(float addTPSPitch) {

        transform.rotation *= Quaternion.Euler(TPSPitch + addTPSPitch, 0f, TPSYaw + TPSTiltAngle);

    }

    /// <summary>
    /// Applies Perlin noise-based camera shake scaled by vehicle speed.
    /// </summary>
    private void ApplyShake() {

        if (!TPSShake)
            return;

        float speed = cameraTarget.playerVehicle.absoluteSpeed;
        float maxSpeed = 260f;
        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
        float shakeMagnitude = normalizedSpeed * .1f;
        float frequency = 3f;
        float noiseX = (Mathf.PerlinNoise(Time.time * frequency, 0f) - .5f) * 1f;
        float noiseY = (Mathf.PerlinNoise(0f, Time.time * frequency) - .5f) * 1f;
        float shakeX = noiseX * shakeMagnitude;
        float shakeY = noiseY * shakeMagnitude;
        Vector3 localShakeOffset = transform.right * shakeX + transform.up * shakeY;
        transform.position += localShakeOffset;
        float noiseZ = (Mathf.PerlinNoise(Time.time * frequency, Time.time * frequency) - .5f) * 2f;
        float rotShakeZ = noiseZ * shakeMagnitude * 2f;
        transform.rotation *= Quaternion.Euler(0f, 0f, rotShakeZ);

    }

    /// <summary>
    /// Lerps target field of view based on vehicle speed.
    /// </summary>
    private void CalculateTPSFieldOfView() {

        targetFieldOfView = Mathf.Lerp(TPSMinimumFOV, TPSMaximumFOV, Mathf.Abs(cameraTarget.playerVehicle.absoluteSpeed) / 260f);

    }

    /// <summary>
    /// Performs an occlusion check from the target position to the desired camera position.
    /// Summary: Uses SphereCast to detect any obstacles and reposition if needed.
    /// </summary>
    /// <returns>Returns a new position if occluded, otherwise Vector3.zero.</returns>
    public Vector3 OccludeRay() {

        Vector3 targetFollow = GetPlayerPosition;

        Vector3 direction = transform.position - targetFollow;
        float distance = direction.magnitude;
        float cameraRadius = .2f;

        if (distance < 0.01f)
            return Vector3.zero;

        Vector3 directionNorm = direction.normalized;

        // Cache layer indices once.
        if (cachedRCCPLayer < 0) {
            cachedRCCPLayer = LayerMask.NameToLayer(RCCPSettings.RCCPLayer);
            cachedRCCPPropLayer = LayerMask.NameToLayer(RCCPSettings.RCCPPropLayer);
            cachedRCCPDetachableLayer = LayerMask.NameToLayer(RCCPSettings.RCCPDetachablePartLayer);
        }

        LayerMask properLayers = occlusionLayerMask;
        properLayers &= ~(1 << cachedRCCPLayer);
        properLayers &= ~(1 << cachedRCCPPropLayer);
        properLayers &= ~(1 << cachedRCCPDetachableLayer);

        int hitCount = Physics.SphereCastNonAlloc(
            origin: targetFollow,
            radius: cameraRadius,
            direction: directionNorm,
            results: occlusionHits,
            maxDistance: distance,
            layerMask: properLayers);

        // Find the closest valid hit.
        float closestDist = float.MaxValue;
        int closestIndex = -1;

        for (int i = 0; i < hitCount; i++) {
            if (!occlusionHits[i].collider.isTrigger
                && !occlusionHits[i].transform.IsChildOf(cameraTarget.playerVehicle.transform)
                && occlusionHits[i].distance < closestDist) {
                closestDist = occlusionHits[i].distance;
                closestIndex = i;
            }
        }

        if (closestIndex >= 0)
            return occlusionHits[closestIndex].point + occlusionHits[closestIndex].normal * cameraRadius;

        return Vector3.zero;

    }

    /// <summary>
    /// Stops any running auto-focus coroutine before starting a new one.
    /// </summary>
    private void StartAutoFocus(IEnumerator routine) {

        if (autoFocusCoroutine != null)
            StopCoroutine(autoFocusCoroutine);

        autoFocusCoroutine = StartCoroutine(routine);

    }

    /// <summary>
    /// Smoothly transitions TPSDistance and TPSHeight to target values using smoothstep easing.
    /// </summary>
    private IEnumerator AutoFocusInternal(float targetDistance, float targetHeight, float duration) {

        float startDistance = TPSDistance;
        float startHeight = TPSHeight;

        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            TPSDistance = Mathf.Lerp(startDistance, targetDistance, t);
            TPSHeight = Mathf.Lerp(startHeight, targetHeight, t);

            yield return null;
        }

        TPSDistance = targetDistance;
        TPSHeight = targetHeight;

    }

    /// <summary>
    /// AutoFocus using the current player vehicle's bounds.
    /// </summary>
    public IEnumerator AutoFocus(float duration = 2f) {

        float bounds = RCCP_GetBounds.MaxBoundsExtent(cameraTarget.playerVehicle.transform);
        return AutoFocusInternal(bounds * 2.75f, bounds * 0.65f, duration);

    }

    /// <summary>
    /// AutoFocus using a specific transform's bounds.
    /// </summary>
    public IEnumerator AutoFocus(Transform target, float duration = 2f) {

        float bounds = RCCP_GetBounds.MaxBoundsExtent(target);
        return AutoFocusInternal(bounds * 2.75f, bounds * 0.65f, duration);

    }

    /// <summary>
    /// AutoFocus using combined bounds of two transforms (truck + trailer).
    /// </summary>
    public IEnumerator AutoFocus(Transform t1, Transform t2, float duration = 3f) {

        float combinedBounds = RCCP_GetBounds.MaxBoundsExtent(t1) + RCCP_GetBounds.MaxBoundsExtent(t2);
        return AutoFocusInternal(combinedBounds * 2.75f, combinedBounds * 0.65f, duration);

    }

    /// <summary>
    /// Summary: Checks if the actual camera is active, not null, and enabled.
    /// </summary>
    /// <returns>True if the actual camera is not null, active, and enabled.</returns>
    public bool IsCameraActive() {

        if (!actualCamera)
            return false;

        if (!actualCamera.gameObject.activeSelf)
            return false;

        if (!actualCamera.isActiveAndEnabled)
            return false;

        return true;

    }

    private void OnDisable() {

        RCCP_InputManager.OnChangedCamera -= RCCP_InputManager_OnChangedCamera;
        RCCP_InputManager.OnLookBackCamera -= RCCP_InputManager_OnLookBackCamera;
        RCCP_InputManager.OnHoldOrbitCamera -= RCCP_InputManager_OnHoldOrbitCamera;
        RCCP_Events.OnRCCPCollision -= RCCP_Events_OnRCCPCollision;

    }

}

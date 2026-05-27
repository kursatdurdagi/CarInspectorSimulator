//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright (c) 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Base class for main controller (RCCP_CarController).
/// </summary>
[SelectionBase]
[DisallowMultipleComponent]
public abstract class RCCP_MainComponent : MonoBehaviour {

    /// <summary>
    /// Cached reference to the global RCCP_Settings asset.
    /// </summary>
    public RCCP_Settings RCCPSettings {

        get {

            if (_RCCPSettings == null)
                _RCCPSettings = RCCP_RuntimeSettings.RCCPSettingsInstance;

            return _RCCPSettings;

        }

    }
    private RCCP_Settings _RCCPSettings;

    /// <summary>
    /// Main car controller.
    /// </summary>
    public RCCP_CarController CarController {

        get {

            if (_carController == null)
                TryGetComponent(out _carController);

            return _carController;

        }
        set {

            _carController = value;

        }

    }

    #region COMPONENTS

    /// <summary>
    /// All RCCP vehicle subsystem components registered to this vehicle.
    /// </summary>
    [Tooltip("All RCCP vehicle subsystem components registered to this vehicle.")]
    public List<IRCCP_Component> components = new List<IRCCP_Component>();
    /// <summary>
    /// All upgrade components (paint, spoiler, wheel, etc.) registered to this vehicle.
    /// </summary>
    [Tooltip("All upgrade components (paint, spoiler, wheel, etc.) registered to this vehicle.")]
    public IRCCP_UpgradeComponent[] upgradeComponents = new IRCCP_UpgradeComponent[0];

    /// <summary>
    /// Cached reference to the vehicle's Rigidbody component.
    /// </summary>
    public Rigidbody Rigid {

        get {

            if (_rigid == null)
                TryGetComponent(out _rigid);

            return _rigid;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's engine component.
    /// </summary>
    public RCCP_Engine Engine {

        get {

            if (_engine == null)
                _engine = RCCP_TryGetComponentInChildren.Get<RCCP_Engine>(transform);

            return _engine;

        }
        set {

            _engine = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's clutch component.
    /// </summary>
    public RCCP_Clutch Clutch {

        get {

            if (_clutch == null)
                _clutch = RCCP_TryGetComponentInChildren.Get<RCCP_Clutch>(transform);

            return _clutch;

        }
        set {

            _clutch = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's gearbox component.
    /// </summary>
    public RCCP_Gearbox Gearbox {

        get {

            if (_gearbox == null)
                _gearbox = RCCP_TryGetComponentInChildren.Get<RCCP_Gearbox>(transform);

            return _gearbox;

        }
        set {

            _gearbox = value;

        }

    }

    /// <summary>
    /// All differential components attached to this vehicle (including inactive ones).
    /// </summary>
    public RCCP_Differential[] Differentials {

        get {

            if (_differentials == null)
                _differentials = GetComponentsInChildren<RCCP_Differential>(true);

            return _differentials;

        }
        set {

            _differentials = value;

        }

    }

    /// <summary>
    /// Refreshes the cached differentials array by re-scanning child components.
    /// </summary>
    public void UpdateDifferentials() {

        _differentials = GetComponentsInChildren<RCCP_Differential>(true);

    }

    /// <summary>
    /// Cached reference to the vehicle's axle manager component, which holds all axles.
    /// </summary>
    public RCCP_Axles AxleManager {

        get {

            if (_axles == null)
                _axles = RCCP_TryGetComponentInChildren.Get<RCCP_Axles>(transform);

            return _axles;

        }
        set {

            _axles = value;

        }

    }

    /// <summary>
    /// The front-most axle on the vehicle, determined by the highest local Z position of its left wheel.
    /// </summary>
    public RCCP_Axle FrontAxle {

        get {

            if (AxleManager == null)
                return null;

            if (AxleManager.Axles == null)
                return null;

            if (AxleManager.Axles.Count < 2)
                return null;

            List<RCCP_Axle> axles = AxleManager.Axles;
            float maxZ = float.MinValue;
            int frontIndex = 0;

            for (int i = 0; i < axles.Count; i++) {

                float z = axles[i].leftWheelCollider.transform.localPosition.z;

                if (z >= maxZ) {
                    maxZ = z;
                    frontIndex = i;
                }

            }

            _axleFront = axles[frontIndex];

            return _axleFront;

        }

    }

    /// <summary>
    /// The rear-most axle on the vehicle, determined by the lowest local Z position of its left wheel.
    /// </summary>
    public RCCP_Axle RearAxle {

        get {

            if (AxleManager == null)
                return null;

            if (AxleManager.Axles == null)
                return null;

            if (AxleManager.Axles.Count < 2)
                return null;

            List<RCCP_Axle> axles = AxleManager.Axles;
            float minZ = float.MaxValue;
            int rearIndex = 0;

            for (int i = 0; i < axles.Count; i++) {

                float z = axles[i].leftWheelCollider.transform.localPosition.z;

                if (z <= minZ) {
                    minZ = z;
                    rearIndex = i;
                }

            }

            _axleRear = axles[rearIndex];

            return _axleRear;

        }

    }

    /// <summary>
    /// All axles currently receiving engine power (isPower == true).
    /// </summary>
    public List<RCCP_Axle> PoweredAxles {

        get {

            //  Finding powered axles.
            RCCP_Axles am = AxleManager;

            _poweredAxles.Clear();

            if (am) {

                List<RCCP_Axle> axles = am.Axles;

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isPower)
                        _poweredAxles.Add(axles[i]);

                }

            }

            return _poweredAxles;

        }

    }

    /// <summary>
    /// All axles that have braking enabled (isBrake == true).
    /// </summary>
    public List<RCCP_Axle> BrakedAxles {

        get {

            //  Finding braking axles.
            RCCP_Axles am = AxleManager;

            _brakedAxles.Clear();

            if (am) {

                List<RCCP_Axle> axles = am.Axles;

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isBrake)
                        _brakedAxles.Add(axles[i]);

                }

            }

            return _brakedAxles;

        }

    }

    /// <summary>
    /// All axles that have steering enabled (isSteer == true).
    /// </summary>
    public List<RCCP_Axle> SteeredAxles {

        get {

            //  Finding steering  axles.
            RCCP_Axles am = AxleManager;

            _steeredAxles.Clear();

            if (am) {

                List<RCCP_Axle> axles = am.Axles;

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isSteer)
                        _steeredAxles.Add(axles[i]);

                }

            }

            return _steeredAxles;

        }

    }

    /// <summary>
    /// All axles that have handbrake enabled (isHandbrake == true).
    /// </summary>
    public List<RCCP_Axle> HandbrakedAxles {

        get {

            //  Finding handbraking axles.
            RCCP_Axles am = AxleManager;

            _handbrakedAxles.Clear();

            if (am) {

                List<RCCP_Axle> axles = am.Axles;

                for (int i = 0; i < axles.Count; i++) {

                    if (axles[i].isHandbrake)
                        _handbrakedAxles.Add(axles[i]);

                }

            }

            return _handbrakedAxles;

        }

    }

    /// <summary>
    /// All RCCP_WheelCollider components attached to the vehicle (including inactive ones).
    /// </summary>
    public RCCP_WheelCollider[] AllWheelColliders {

        get {

            if (_allWheelColliders == null || (_allWheelColliders != null && _allWheelColliders.Length < 1))
                _allWheelColliders = GetComponentsInChildren<RCCP_WheelCollider>(true);

            return _allWheelColliders;

        }
        set {

            _allWheelColliders = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's aerodynamics component (downforce and drag).
    /// </summary>
    public RCCP_AeroDynamics AeroDynamics {

        get {

            if (_aero == null)
                _aero = RCCP_TryGetComponentInChildren.Get<RCCP_AeroDynamics>(transform);

            return _aero;

        }
        set {

            _aero = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's input component, which handles player and override inputs.
    /// </summary>
    public RCCP_Input Inputs {

        get {

            if (_inputs == null)
                _inputs = RCCP_TryGetComponentInChildren.Get<RCCP_Input>(transform);

            return _inputs;

        }
        set {

            _inputs = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's audio component (engine sounds, collisions, etc.).
    /// </summary>
    public RCCP_Audio Audio {

        get {

            if (_audio == null)
                _audio = RCCP_TryGetComponentInChildren.Get<RCCP_Audio>(transform);

            return _audio;

        }
        set {

            _audio = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's lights manager component (headlights, indicators, etc.).
    /// </summary>
    public RCCP_Lights Lights {

        get {

            if (_lights == null)
                _lights = RCCP_TryGetComponentInChildren.Get<RCCP_Lights>(transform);

            return _lights;

        }
        set {

            _lights = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's stability component (ABS, ESP, TCS, steering/traction helpers).
    /// </summary>
    public RCCP_Stability Stability {

        get {

            if (_stability == null)
                _stability = RCCP_TryGetComponentInChildren.Get<RCCP_Stability>(transform);

            return _stability;

        }
        set {

            _stability = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's damage component (mesh deformation, detachable parts).
    /// </summary>
    public RCCP_Damage Damage {

        get {

            if (_damage == null)
                _damage = RCCP_TryGetComponentInChildren.Get<RCCP_Damage>(transform);

            return _damage;

        }
        set {

            _damage = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's particle effects component (tire smoke, sparks, etc.).
    /// </summary>
    public RCCP_Particles Particles {

        get {

            if (_particles == null)
                _particles = RCCP_TryGetComponentInChildren.Get<RCCP_Particles>(transform);

            return _particles;

        }
        set {

            _particles = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's customizer component (paint, wheels, upgrades, etc.).
    /// </summary>
    public RCCP_Customizer Customizer {

        get {

            if (_customizer == null)
                _customizer = RCCP_TryGetComponentInChildren.Get<RCCP_Customizer>(transform);

            return _customizer;

        }
        set {

            _customizer = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's LOD (Level of Detail) component.
    /// </summary>
    public RCCP_Lod LOD {

        get {

            if (_LOD == null)
                _LOD = RCCP_TryGetComponentInChildren.Get<RCCP_Lod>(transform);

            return _LOD;

        }
        set {

            _LOD = value;

        }

    }

    /// <summary>
    /// Cached reference to the vehicle's other addons manager (AI, NOS, recorder, trailer, etc.).
    /// </summary>
    public RCCP_OtherAddons OtherAddonsManager {

        get {

            if (_otherAddons == null)
                _otherAddons = RCCP_TryGetComponentInChildren.Get<RCCP_OtherAddons>(transform);

            return _otherAddons;

        }
        set {

            _otherAddons = value;

        }

    }

    /// <summary>
    /// Number of differentials that are active, enabled, and connected to an axle.
    /// </summary>
    public int ActiveDifferentials {

        get {

            int totalActiveDifferentials = 0;

            if (Differentials != null && Differentials.Length > 0) {

                for (int i = 0; i < Differentials.Length; i++) {

                    if (Differentials[i] == null)
                        continue;

                    if (Differentials[i].isActiveAndEnabled && Differentials[i].gameObject.activeSelf && Differentials[i].connectedAxle != null)
                        totalActiveDifferentials++;

                }

            }

            return totalActiveDifferentials;

        }

    }

    /// <summary>
    /// World-space center position of the vehicle's bounding box, computed from its mesh bounds.
    /// </summary>
    public Vector3 CenterPosition {

        get {

            if (_centerPosition == null) {

                _centerPosition = new GameObject("_CenterPosition").transform;
                _centerPosition.transform.SetParent(transform);
                _centerPosition.SetPositionAndRotation(RCCP_GetBounds.GetBoundsCenter(transform), transform.rotation);

            }

            return _centerPosition.position;

        }

    }

    //  Private fields for components.
    private RCCP_CarController _carController;
    private Rigidbody _rigid = null;
    private RCCP_Input _inputs = null;
    private RCCP_Engine _engine = null;
    private RCCP_Clutch _clutch = null;
    private RCCP_Gearbox _gearbox = null;
    private RCCP_Differential[] _differentials = null;
    private RCCP_Axles _axles = null;
    private RCCP_Axle _axleFront = null;
    private RCCP_Axle _axleRear = null;
    [SerializeField, Tooltip("Axles receiving drive torque from the differential.")]
    private List<RCCP_Axle> _poweredAxles = new List<RCCP_Axle>();
    [SerializeField, Tooltip("Axles with service brake applied.")]
    private List<RCCP_Axle> _brakedAxles = new List<RCCP_Axle>();
    [SerializeField, Tooltip("Axles that respond to steering input.")]
    private List<RCCP_Axle> _steeredAxles = new List<RCCP_Axle>();
    [SerializeField, Tooltip("Axles with handbrake (parking brake) applied.")]
    private List<RCCP_Axle> _handbrakedAxles = new List<RCCP_Axle>();
    private RCCP_AeroDynamics _aero = null;
    private RCCP_Audio _audio = null;
    private RCCP_Lights _lights = null;
    private RCCP_Stability _stability = null;
    private RCCP_Damage _damage = null;
    private RCCP_Particles _particles = null;
    private RCCP_OtherAddons _otherAddons = null;
    private RCCP_WheelCollider[] _allWheelColliders = null;
    private RCCP_Customizer _customizer = null;
    private RCCP_Lod _LOD = null;
    private Transform _centerPosition = null;

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Editor flag indicating whether the component check should be performed.
    /// </summary>
    [HideInInspector] public bool checkComponents = false;
#endif

    private void Awake() {

        GetAllComponents();

    }

    /// <summary>
    /// Finds and initializes all RCCP components and upgrade components attached to this vehicle.
    /// </summary>
    public void GetAllComponents() {

        //  Finding and initializing all components attached to this vehicle (even if they are disabled).
        CarController = this as RCCP_CarController;

        RCCP_CarController carController = CarController;
        Rigidbody rigid = Rigid;
        RCCP_Engine engine = Engine;
        RCCP_Clutch clutch = Clutch;
        RCCP_Gearbox gearbox = Gearbox;
        RCCP_Differential[] differentials = Differentials;
        RCCP_Axles axles = AxleManager;
        RCCP_Axle frontAxle = FrontAxle;
        RCCP_Axle rearAxle = RearAxle;
        RCCP_WheelCollider[] allWheelColliders = AllWheelColliders;
        RCCP_AeroDynamics aeroDynamics = AeroDynamics;
        RCCP_Input inputs = Inputs;
        RCCP_Audio audio = Audio;
        RCCP_Lights lights = Lights;
        RCCP_Stability stability = Stability;
        RCCP_Damage damage = Damage;
        RCCP_Particles particles = Particles;
        RCCP_Customizer customizer = Customizer;
        RCCP_Lod lod = LOD;
        RCCP_OtherAddons otherAddons = OtherAddonsManager;
        List<RCCP_Axle> poweredAxles = PoweredAxles;
        List<RCCP_Axle> brakedAxles = BrakedAxles;
        List<RCCP_Axle> steeringAxles = SteeredAxles;
        List<RCCP_Axle> handbrakingAxles = HandbrakedAxles;

        components = GetComponentsInChildren<IRCCP_Component>(true).ToList();
        upgradeComponents = GetComponentsInChildren<IRCCP_UpgradeComponent>(true);

        foreach (IRCCP_Component item in components)
            item.Initialize(CarController);

        foreach (IRCCP_UpgradeComponent item in upgradeComponents)
            item.Initialize(CarController);

    }

    /// <summary>
    /// Resets all vehicle state variables (speed, RPM, inputs, lights, etc.) to their defaults.
    /// </summary>
    public void ResetVehicle() {

        CarController.engineRPM = 0f;
        CarController.currentGear = 0;
        CarController.currentGearRatio = 1f;
        CarController.lastGearRatio = 1f;
        CarController.differentialRatio = 1f;
        CarController.speed = 0f;
        CarController.wheelRPM2Speed = 0f;
        CarController.tractionWheelRPM2EngineRPM = 0f;
        CarController.targetWheelSpeedForCurrentGear = 0f;
        CarController.maximumSpeed = 0f;
        CarController.producedEngineTorque = 0f;
        CarController.producedGearboxTorque = 0f;
        CarController.producedDifferentialTorque = 0f;
        CarController.direction = 1;
        CarController.engineStarting = false;
        CarController.engineRunning = false;
        CarController.shiftingNow = false;
        CarController.NGearNow = false;
        CarController.reversingNow = false;
        CarController.steerAngle = 0f;
        CarController.fuelInput_V = 0f;
        CarController.throttleInput_V = 0f;
        CarController.brakeInput_V = 0f;
        CarController.steerInput_V = 0f;
        CarController.handbrakeInput_V = 0f;
        CarController.clutchInput_V = 0f;
        CarController.gearInput_V = 0f;
        CarController.nosInput_V = 0f;
        CarController.throttleInput_P = 0f;
        CarController.brakeInput_P = 0f;
        CarController.steerInput_P = 0f;
        CarController.handbrakeInput_P = 0f;
        CarController.clutchInput_P = 0f;
        CarController.nosInput_P = 0f;
        CarController.lowBeamLights = false;
        CarController.highBeamLights = false;
        CarController.indicatorsLeftLights = false;
        CarController.indicatorsRightLights = false;
        CarController.indicatorsAllLights = false;

    }

}

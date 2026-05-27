//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI button component that loads a specific demo scene when clicked. Validates scene availability in Build Settings before loading.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/RCCP UI Scene Selector")]
public class RCCP_UI_SceneSelector : RCCP_UIComponent {

    /// <summary>
    /// Available demo scenes that can be loaded from the scene selector UI.
    /// </summary>
    public enum DemoScene {

        /// <summary>All-in-one city demo with all features enabled.</summary>
        AIO,

        /// <summary>City driving demo scene.</summary>
        City,

        /// <summary>City scene with vehicle selection UI.</summary>
        CitySelectVehicle,

        /// <summary>Vehicle damage demonstration scene.</summary>
        Damage,

        /// <summary>Vehicle transport and teleportation demo scene.</summary>
        Transport,

        /// <summary>Blank mobile controls demo scene.</summary>
        Blank,

        /// <summary>Blank scene for API testing.</summary>
        BlankAPI,

        /// <summary>Blank scene for vehicle customization testing.</summary>
        BlankCustomization,

        /// <summary>Blank scene for input override demonstration.</summary>
        BlankOverrideInputs,

        /// <summary>Photon PUN2 multiplayer lobby scene.</summary>
        PhotonLobby,

        /// <summary>Photon PUN2 multiplayer city scene.</summary>
        PhotonCity,

        /// <summary>City scene with enter/exit FPS character controller.</summary>
        CityEnterExitFPS,

        /// <summary>City scene with enter/exit TPS character controller.</summary>
        CityEnterExitTPS,

        /// <summary>Blank scene with enter/exit FPS character controller.</summary>
        BlankEnterExitFPS,

        /// <summary>Blank scene with enter/exit TPS character controller.</summary>
        BlankEnterExitTPS,

        /// <summary>City scene with AI traffic integration.</summary>
        CityTraffic,

        /// <summary>Mirror networking multiplayer demo scene.</summary>
        Mirror,

        /// <summary>City scene with AI-controlled vehicles.</summary>
        CityAI,

        /// <summary>Blank scene for automated stability system testing.</summary>
        StabilityTest

    }
    /// <summary>
    /// The demo scene this button will load when clicked.
    /// </summary>
    [Tooltip("Which demo scene this button loads when clicked.")]
    public DemoScene demoScene = DemoScene.AIO;

    /// <summary>
    /// Cached scene index string used for build settings validation.
    /// </summary>
    [HideInInspector] public string sceneIndex = "";

    /// <summary>
    /// Loads the configured demo scene. In the editor, first validates that the scene is included in Build Settings.
    /// </summary>
    public void OnClick() {

#if UNITY_EDITOR

        if (!CheckScene(demoScene)) {

            Debug.LogError("This scene couldn't found in the Build Settings. You'll probably need to add the demo scene to your Build Settings. Go to Tools --> BCG --> RCCP --> Welcome Window and click the 'Add Demo Scenes To Build Settings' button. This will add all demo scenes to your Build Settings.");
            return;

        }

#endif

        switch (demoScene) {

            case DemoScene.AIO:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_city_AIO);
                break;

            case DemoScene.City:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_City);
                break;

            case DemoScene.CitySelectVehicle:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_CarSelection);
                break;

            case DemoScene.Damage:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_Damage);
                break;

            case DemoScene.Transport:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_Transport);
                break;

            case DemoScene.Blank:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_BlankMobile);
                break;

            case DemoScene.BlankAPI:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_APIBlank);
                break;

            case DemoScene.BlankCustomization:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_Customization);
                break;

            case DemoScene.BlankOverrideInputs:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_OverrideInputs);
                break;

#if RCCP_PHOTON

            case DemoScene.PhotonLobby:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby);
                break;

            case DemoScene.PhotonCity:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City);
                break;

#endif

#if BCG_ENTEREXIT

            case DemoScene.CityEnterExitFPS:

                RCCP_DemoAIO.Instance.LoadScene(BCG_DemoScenes.Instance.path_demo_CityFPS);
                break;

            case DemoScene.CityEnterExitTPS:

                RCCP_DemoAIO.Instance.LoadScene(BCG_DemoScenes.Instance.path_demo_CityTPS);
                break;

            case DemoScene.BlankEnterExitFPS:

                RCCP_DemoAIO.Instance.LoadScene(BCG_DemoScenes.Instance.path_demo_BlankFPS);
                break;

            case DemoScene.BlankEnterExitTPS:

                RCCP_DemoAIO.Instance.LoadScene(BCG_DemoScenes.Instance.path_demo_BlankTPS);
                break;

#endif

#if RCCP_MIRROR

            case DemoScene.Mirror:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes_Mirror.Instance.path_Demo_Blank_Mirror);
                break;

#endif

#if BCG_RTRC

            case DemoScene.CityTraffic:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_CityWithTraffic);
                break;

#endif

            case DemoScene.CityAI:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_CityWithAI);
                break;

            case DemoScene.StabilityTest:

                RCCP_DemoAIO.Instance.LoadScene(RCCP_DemoScenes.Instance.path_demo_StabilityTest);
                break;

        }

    }

#if UNITY_EDITOR

    private bool CheckScene(DemoScene checkScene) {

        UnityEditor.EditorBuildSettingsScene[] allScenes = UnityEditor.EditorBuildSettings.scenes;
        bool foundScene = false;

        for (int i = 0; i < allScenes.Length; i++) {

            switch (demoScene) {

                case DemoScene.AIO:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_city_AIO)
                        foundScene = true;

                    break;

                case DemoScene.City:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_City)
                        foundScene = true;

                    break;

                case DemoScene.CitySelectVehicle:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_CarSelection)
                        foundScene = true;

                    break;

                case DemoScene.Damage:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_Damage)
                        foundScene = true;

                    break;

                case DemoScene.Transport:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_Transport)
                        foundScene = true;

                    break;

                case DemoScene.Blank:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_BlankMobile)
                        foundScene = true;

                    break;

                case DemoScene.BlankAPI:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_APIBlank)
                        foundScene = true;

                    break;

                case DemoScene.BlankCustomization:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_Customization)
                        foundScene = true;

                    break;

                case DemoScene.BlankOverrideInputs:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_OverrideInputs)
                        foundScene = true;

                    break;

                case DemoScene.CityAI:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_CityWithAI)
                        foundScene = true;

                    break;

                case DemoScene.StabilityTest:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_StabilityTest)
                        foundScene = true;

                    break;

#if BCG_RTRC

                case DemoScene.CityTraffic:

                    if (allScenes[i].path == RCCP_DemoScenes.Instance.path_demo_CityWithTraffic)
                        foundScene = true;

                    break;

#endif

#if RCCP_PHOTON

                case DemoScene.PhotonLobby:

                    if (allScenes[i].path == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby)
                        foundScene = true;

                    break;

                case DemoScene.PhotonCity:

                    if (allScenes[i].path == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City)
                        foundScene = true;

                    break;

#endif

#if BCG_ENTEREXIT

                case DemoScene.CityEnterExitFPS:

                    if (allScenes[i].path == BCG_DemoScenes.Instance.path_demo_CityFPS)
                        foundScene = true;

                    break;

                case DemoScene.CityEnterExitTPS:

                    if (allScenes[i].path == BCG_DemoScenes.Instance.path_demo_CityTPS)
                        foundScene = true;

                    break;

                case DemoScene.BlankEnterExitFPS:

                    if (allScenes[i].path == BCG_DemoScenes.Instance.path_demo_BlankFPS)
                        foundScene = true;

                    break;

                case DemoScene.BlankEnterExitTPS:

                    if (allScenes[i].path == BCG_DemoScenes.Instance.path_demo_BlankTPS)
                        foundScene = true;

                    break;

#endif
#if RCCP_MIRROR

                case DemoScene.Mirror:

                    if (allScenes[i].path == RCCP_DemoScenes_Mirror.Instance.path_Demo_Blank_Mirror)
                        foundScene = true;

                    break;

#endif
            }

        }

        return foundScene;

    }

#endif

}

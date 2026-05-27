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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// All-in-one demo scene controller that manages scene loading, addon button states (Photon, SharedAssets, Traffic), and persists across scene transitions.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Demo AIO")]
public class RCCP_DemoAIO : RCCP_GenericComponent {

    /// <summary>
    /// Singleton instance that persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    public static RCCP_DemoAIO Instance;

    /// <summary>Main content panel showing the demo scene selection menu.</summary>
    [Tooltip("Main content panel showing the demo scene selection menu.")]
    public GameObject content;
    /// <summary>Loading screen overlay displayed during asynchronous scene transitions.</summary>
    [Tooltip("Loading screen overlay displayed during scene transitions.")]
    public GameObject loading;
    /// <summary>Credits panel displayed on the main AIO scene.</summary>
    [Tooltip("Credits panel displayed on the main AIO scene.")]
    public GameObject credits;
    /// <summary>Back button that returns to the main AIO scene selection.</summary>
    [Tooltip("Back button that returns to the main AIO scene selection.")]
    public GameObject back;

    /// <summary>UI buttons for Photon PUN2 multiplayer demo scenes (disabled when addon not installed).</summary>
    [Tooltip("UI buttons for Photon PUN2 multiplayer demo scenes.")]
    public GameObject[] photonButtons;
    /// <summary>UI buttons for BCG Shared Assets enter/exit demo scenes (disabled when addon not installed).</summary>
    [Tooltip("UI buttons for BCG Shared Assets enter/exit demo scenes.")]
    public GameObject[] sharedAssetsButtons;
    /// <summary>UI buttons for Realistic Traffic Controller demo scenes (disabled when addon not installed).</summary>
    [Tooltip("UI buttons for Realistic Traffic Controller demo scenes.")]
    public GameObject[] cityTrafficButtons;

    /// <summary>Info message shown when Photon PUN2 addon is not installed.</summary>
    [Tooltip("Info panel shown when Photon PUN2 addon is not installed.")]
    public GameObject photonInfo;
    /// <summary>Info message shown when BCG Shared Assets addon is not installed.</summary>
    [Tooltip("Info panel shown when BCG Shared Assets addon is not installed.")]
    public GameObject sharedAssetsInfo;
    /// <summary>Info message shown when Realistic Traffic Controller addon is not installed.</summary>
    [Tooltip("Info panel shown when Realistic Traffic Controller addon is not installed.")]
    public GameObject cityTrafficInfo;

    private void Awake() {

        if (Instance == null) {

            Instance = this;
            DontDestroyOnLoad(gameObject);

        } else {

            Destroy(gameObject);
            return;

        }

    }

    private void Start() {

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

#if RCCP_PHOTON

        for (int i = 0; i < photonButtons.Length; i++) {
            if (photonButtons[i].TryGetComponent<Button>(out var pBtn))
                pBtn.interactable = true;
        }

        photonInfo.SetActive(false);

#else

        for (int i = 0; i < photonButtons.Length; i++) {
            if (photonButtons[i].TryGetComponent<Button>(out var pBtn))
                pBtn.interactable = false;
        }

        photonInfo.SetActive(true);

#endif

#if BCG_ENTEREXIT

        for (int i = 0; i < sharedAssetsButtons.Length; i++) {
            if (sharedAssetsButtons[i].TryGetComponent<Button>(out var sBtn))
                sBtn.interactable = true;
        }

        sharedAssetsInfo.SetActive(false);

#else

        for (int i = 0; i < sharedAssetsButtons.Length; i++) {
            if (sharedAssetsButtons[i].TryGetComponent<Button>(out var sBtn))
                sBtn.interactable = false;
        }

        sharedAssetsInfo.SetActive(true);

#endif

#if BCG_RTRC

        for (int i = 0; i < cityTrafficButtons.Length; i++) {
            if (cityTrafficButtons[i].TryGetComponent<Button>(out var cBtn))
                cBtn.interactable = true;
        }

        cityTrafficInfo.SetActive(false);

#else

        for (int i = 0; i < cityTrafficButtons.Length; i++) {
            if (cityTrafficButtons[i].TryGetComponent<Button>(out var cBtn))
                cBtn.interactable = false;
        }

        cityTrafficInfo.SetActive(true);

#endif

    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) {

        loading.SetActive(false);

    }

    /// <summary>
    /// Loads a demo scene asynchronously by path, showing the loading screen and toggling content/back panels based on whether returning to AIO.
    /// </summary>
    /// <param name="sceneIndex">The scene path to load (from RCCP_DemoScenes).</param>
    public void LoadScene(string sceneIndex) {

        loading.SetActive(true);
        SceneManager.LoadSceneAsync(sceneIndex);

        if (sceneIndex == RCCP_DemoScenes.Instance.path_city_AIO) {

            content.SetActive(true);
            back.SetActive(false);
            credits.SetActive(true);

        } else {

            content.SetActive(false);
            back.SetActive(true);
            credits.SetActive(false);

        }

#if RCCP_PHOTON

        if (sceneIndex == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby || sceneIndex == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City)
            back.SetActive(false);

#endif

    }

}

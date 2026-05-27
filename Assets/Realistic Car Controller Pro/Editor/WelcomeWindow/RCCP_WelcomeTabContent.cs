//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2026 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

#if UNITY_EDITOR

using UnityEngine.UIElements;

/// <summary>
/// Interface for Welcome Window tab content providers.
/// Each tab implements this to provide its UI Toolkit content.
/// </summary>
public interface IRCCP_WelcomeTabContent {

    /// <summary>
    /// Creates the content for this tab.
    /// </summary>
    VisualElement CreateContent();

    /// <summary>
    /// Called when the tab becomes visible.
    /// </summary>
    void OnActivated();

    /// <summary>
    /// Called when the tab is hidden.
    /// </summary>
    void OnDeactivated();

}

#endif

//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Interface for loadout of customizer.
/// </summary>
public interface IRCCP_LoadoutComponent {

    /// <summary>Updates the loadout data from the given upgrade manager component.</summary>
    /// <param name="component">The upgrade manager whose state should be saved to the loadout.</param>
    public void UpdateLoadout(MonoBehaviour component);

}

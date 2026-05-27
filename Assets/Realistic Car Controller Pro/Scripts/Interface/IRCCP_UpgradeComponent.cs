//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

/// <summary>
/// Interface for all upgrader components.
/// </summary>
public interface IRCCP_UpgradeComponent {

    /// <summary>Initializes this upgrade component with its parent car controller.</summary>
    /// <param name="connectedCarController">The parent car controller to register with.</param>
    public void Initialize(RCCP_CarController connectedCarController);

}

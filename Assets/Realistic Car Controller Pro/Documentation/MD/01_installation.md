# Installation

This guide walks you through installing Realistic Car Controller Pro (RCCP) in your Unity project, from prerequisites to your first test drive. If you follow these steps in order, you will have a working vehicle simulation running in under ten minutes.

---

## Prerequisites

Before importing RCCP, make sure your project meets these requirements:

| Requirement | Details |
|---|---|
| **Unity Version** | Unity 6000.0 or above |
| **Scripting Backend** | .NET Standard 2.1 or .NET Framework |

### Required Packages

RCCP depends on the following Unity packages. The versions below match the project's `Packages/manifest.json` for the current RCCP release:

| Package | Package ID | Version |
|---|---|---|
| Editor Coroutines | `com.unity.editorcoroutines` | 1.0.1 |
| Input System | `com.unity.inputsystem` | 1.17.0 |
| Newtonsoft JSON | `com.unity.nuget.newtonsoft-json` | 3.2.2 |
| AI Navigation | `com.unity.ai.navigation` | 2.0.12 |
| Shader Graph | `com.unity.shadergraph` | 17.3.0 |
| uGUI | `com.unity.ugui` | 2.0.0 |
| 2D Sprite | `com.unity.2d.sprite` | 1.0.0 |
| Visual Studio Editor | `com.unity.ide.visualstudio` | 2.0.22 |
| ProBuilder | `com.unity.probuilder` | 6.0.9 |

All packages are available from the Unity Registry in the Package Manager. If any of them are missing, RCCP may show compiler errors immediately after import -- so it is best to install them first.

**A note on Assembly Definitions:** RCCP does not use `.asmdef` files. All scripts compile into the default Assembly-CSharp assembly. This keeps integration simple and avoids dependency headaches when you reference RCCP types from your own code.

---

## Importing from Asset Store

1. Open the Package Manager by going to **Window > Package Manager**.
2. In the top-left dropdown, select **My Assets**.
3. Search for **Realistic Car Controller Pro** in your purchased assets list.
4. Click **Download** if you have not already, then click **Import**.
5. Leave all files selected in the import dialog and click **Import** again.
6. Wait for Unity to finish compiling. This may take a minute or two depending on your machine. Do not interact with the editor while the progress bar is active.

Once compilation finishes with no errors, RCCP is ready to configure.

---

## First-Time Setup

### Welcome Window

The first time you import RCCP, a **Welcome Window** opens automatically. If it does not appear, you can open it manually from **Tools > BoneCracker Games > Realistic Car Controller Pro > Welcome Window**.

The Welcome Window has several tabs that help you get started:

| Tab | Purpose |
|---|---|
| **Welcome** | Shows the current RCCP version, quick links, and an overview of what is new. |
| **Demos** | One-click install for demo content including sample vehicles and environments. |
| **Addons** | Install integration packages for multiplayer, traffic simulation, and other systems. |
| **Shaders** | Install the correct shader package for your render pipeline (Built-in, URP, or HDRP). |
| **Keys** | Displays the default input keybindings so you know how to control vehicles right away. |
| **Updates** | Check whether a newer version of RCCP is available on the Asset Store. |
| **DOC** | Links to online documentation, tutorials, and the support email. |

Take a moment to explore each tab. At minimum, you should install the shader package that matches your render pipeline (see the Shaders tab) to avoid pink or magenta materials.

### Layer Setup

RCCP automatically creates five layers in your project the first time it initializes. These layers are essential for the physics collision matrix to work correctly:

| Layer | Assigned To | Why It Exists |
|---|---|---|
| **RCCP_Vehicle** | All vehicle GameObjects | Lets RCCP identify vehicles in the physics system and apply vehicle-specific collision rules. |
| **RCCP_WheelCollider** | Wheel collider GameObjects | Separates wheel physics from the vehicle body so wheels can interact with surfaces independently. |
| **RCCP_DetachablePart** | Detachable body panels | Prevents detached parts (doors, bumpers, hoods) from colliding with the vehicle they came from. |
| **RCCP_Prop** | Interactive props (cones, barriers) | Allows props to react to vehicle collisions differently from static environment geometry. |
| **RCCP_Obstacle** | Obstacles | Defines collision behavior for objects that should damage or slow down vehicles. |

These layers power the collision matrix -- for example, vehicles do not collide with their own freshly-detached parts, which prevents explosive physics glitches after a crash. You should not rename or delete these layers.

### Input System

RCCP uses Unity's **New Input System** with an `InputActionAsset` called `RCCP_InputActions`. This asset is included in the package and located in the Resources folder.

Make sure the Input System package is installed and that your project's **Player Settings > Active Input Handling** is set to either **Input System Package (New)** or **Both**. If it is set to the old input manager only, RCCP inputs will not work.

---

## Installing Addon Packages

RCCP ships with optional addon packages for extended functionality. These are `.unitypackage` files located in:

```
Assets/Realistic Car Controller Pro/Addons/Installers/
```

### Available Addon Packages

| Package | Description |
|---|---|
| `RCCP_DemoAssets.unitypackage` | Demo vehicles, environments, and prefabs for testing and learning. |
| `BCG_SharedAssets.unitypackage` | Character controller for enter/exit vehicle gameplay. |
| `RCCP_PhotonIntegration.unitypackage` | Photon PUN 2 multiplayer integration. |
| `RCCP_MirrorIntegration.unitypackage` | Mirror networking integration. |
| `RCCP_ProFlareIntegration.unitypackage` | ProFlares lens flare effects for headlights and tail lights. |
| `RCCP_RealisticTrafficControllerIntegration.unitypackage` | AI traffic simulation integration. |
| `RCCP_BuiltinShaders.unitypackage` | Shaders for the Built-in Render Pipeline. |

### Shader Packages by Unity Version

Render pipeline shaders are organized by Unity version in the `Shaders/` subfolder:

| Folder | Contents |
|---|---|
| `Shaders/2021/` | URP shaders for Unity 2021 |
| `Shaders/2022/` | URP and HDRP shaders for Unity 2022 |
| `Shaders/2023/` | URP and HDRP shaders for Unity 2023 |
| `Shaders/6/` | URP and HDRP shaders for Unity 6 |
| `BCG_HDRPVolumeProfile.unitypackage` | HDRP post-processing volume preset |

### How to Install

You can install addon packages in two ways:

- **From the Welcome Window:** Open the Addons or Shaders tab and click the install button next to the package you want.
- **Manually:** Navigate to the Installers folder in the Project window and double-click the `.unitypackage` file. Unity will show an import dialog -- click Import to add the files to your project.

Only install the addons you actually need. Each addon adds scripts and assets to your project, and multiplayer integrations (Photon, Mirror) require their respective SDKs to be installed separately.

---

## Verifying Installation

The fastest way to confirm everything is working:

1. Open the prototype scene: `Assets/Realistic Car Controller Pro/Scenes/RCCP_Scene_Blank_Prototype.unity`
2. Press **Play**.
3. Drive with **WASD** keys, brake with **Space**, and look around with the **mouse**.
4. If the car drives and responds to input, your installation is successful.

If you installed the demo content, you can also try the full city scene:

```
Assets/Realistic Car Controller Pro/Addons/Installed/Demo Content/Scenes/RCCP_Scene_CityNew.unity
```

Check the **Console** window (Window > General > Console) for any errors. A clean console with no red messages means RCCP is fully operational.

---

## Upgrading from a Previous Version

Upgrading RCCP requires a clean re-import to avoid stale scripts or missing references. Follow these steps carefully:

1. **Back up your entire project.** Copy your project folder to a safe location, or commit to version control. This step is not optional.
2. **Delete the old RCCP folder.** In the Project window, right-click `Assets/Realistic Car Controller Pro` and select Delete. Wait for Unity to recompile.
3. **Import the new version.** Follow the same steps as [Importing from Asset Store](#importing-from-asset-store) above.
4. **Re-import addon packages.** Any addon packages you had installed (demo content, multiplayer integrations, shader packages) need to be imported again from the Installers folder.
5. **Check the Console.** Look for deprecation warnings or errors. Address anything in red before continuing development.

Your vehicle prefabs and scene references should survive the upgrade as long as you are using the same major Unity version. RCCP maintains consistent script GUIDs across versions to preserve serialized data.

---

## Common Installation Issues

| Problem | Cause | Solution |
|---|---|---|
| **"Input System not found"** compiler errors | The Input System package is not installed. | Open **Window > Package Manager**, switch to **Unity Registry**, find **Input System**, and click Install. |
| **Pink or magenta materials** | The shader package for your render pipeline is missing. | Install the correct shader package from the Welcome Window Shaders tab or from `Addons/Installers/Shaders/`. See [Render Pipelines](18_render_pipelines.md) for details. |
| **Missing layers warning** | The RCCP layers were not created automatically. | Go to **Tools > BoneCracker Games > Realistic Car Controller Pro > Settings** and click the layer setup button. Layers are normally created automatically on first import. |
| **"Missing script" on prefabs** | An addon integration package was removed but prefabs still reference its scripts. | Either re-import the addon package, or select the affected prefab and remove the components with missing scripts. |
| **NullReferenceException on Play** | The `RCCP_Settings` asset is missing or was accidentally deleted from the Resources folder. | Re-import RCCP to restore the Settings asset, or check that `Assets/Realistic Car Controller Pro/Resources/RCCP_Settings.asset` exists. |

If your issue is not listed here, see the full [Troubleshooting](25_troubleshooting.md) guide.

---

## Next Steps

Now that RCCP is installed and verified, here is where to go next:

- [Vehicle Setup](03_vehicle_setup.md) -- Set up your first vehicle from scratch using the Setup Wizard.
- [Settings](04_settings.md) -- Configure global RCCP behavior, physics, and input options.
- [Demo Content](19_demo_content.md) -- Explore the included demo scenes and learn from example vehicles.

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)  
**Need help?** See [Troubleshooting](25_troubleshooting.md)

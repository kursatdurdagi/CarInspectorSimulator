# Render Pipelines

RCCP supports all three Unity render pipelines: **Built-in**, **URP** (Universal Render Pipeline), and **HDRP** (High Definition Render Pipeline). By default, RCCP ships with materials authored for the Built-in pipeline. If your project uses URP or HDRP, you need to import the matching shader package and run the pipeline converter.

This page walks you through identifying your pipeline, importing the correct shaders, and converting materials so everything renders properly.

---

## Shader Packages

RCCP provides pre-built shader packages for every supported pipeline and Unity version. All packages are located in:

```
Assets/Realistic Car Controller Pro/Addons/Installers/
```

### Available Packages

| Unity Version | URP Package | HDRP Package |
|---------------|-------------|--------------|
| 2021 | `Shaders/2021/RCCP_URPShaders_2021.unitypackage` | -- |
| 2022 | `Shaders/2022/RCCP_URPShaders_2022.unitypackage` | `Shaders/2022/RCCP_HDRPShaders_2022.unitypackage` |
| 2023 | `Shaders/2023/RCCP_URPShaders_2023.unitypackage` | `Shaders/2023/RCCP_HDRPShaders_2023.unitypackage` |
| Unity 6 | `Shaders/6/RCCP_URPShaders_6.unitypackage` | `Shaders/6/RCCP_HDRPShaders_6.unitypackage` |
| Built-in | `RCCP_BuiltinShaders.unitypackage` (top-level) | -- |

An additional package, `BCG_HDRPVolumeProfile.unitypackage`, provides a pre-configured HDRP post-processing Volume Profile for the demo scenes.

**Important:** Always import the package that matches **both** your render pipeline **and** your Unity version. For example, a Unity 6 project using URP needs `RCCP_URPShaders_6.unitypackage`.

---

## Pipeline Converter Window

RCCP includes a built-in helper that guides you through the full conversion process step by step.

### Opening the Converter

- **Menu:** Tools > BoneCracker Games > Realistic Car Controller Pro > Render Pipeline Converter
- **Alternative:** GameObject > BoneCracker Games > Realistic Car Controller Pro > Render Pipeline Converter
- **Automatic:** The window opens automatically when RCCP detects a pipeline change (for example, switching from Built-in to URP).

### Converter Steps

When you open the converter in a URP or HDRP project, it presents numbered steps:

1. **Material Conversion** -- Opens Unity's built-in Render Pipeline Converter. Check "Material Upgrade", click "Initialize Converters", then "Convert Assets".
2. **Lens Flare Conversion** -- Scans all RCCP vehicle prefabs and replaces legacy `LensFlare` components with SRP-compatible `LensFlareComponentSRP` components.
3. **Custom Shader Import** -- Imports the correct RCCP shader package for your detected pipeline (URP or HDRP).
4. **Custom Shader Conversion** -- Scans all RCCP prefabs and swaps materials using base shaders (for example, `RCCP_Shader_Body`) with their pipeline-specific variants (for example, `RCCP_Shader_Body_URP` or `RCCP_Shader_Body_HDRP`).
5. **Remove Old Shaders** -- Removes shader files from other pipelines that are no longer needed.

For **URP** projects, an additional step enables post-processing on all RCCP demo cameras. For **HDRP** projects, an additional step converts demo scene directional lights and adds the HDRP Volume Profile prefab.

---

## Converting to URP

Follow these steps to set up RCCP in a Universal Render Pipeline project:

1. Open **Window > Package Manager** and install the **Universal RP** package if it is not already installed.
2. Create a **URP Renderer Asset** (or use an existing one) via **Assets > Create > Rendering > URP Asset (with Universal Renderer)**.
3. Assign the URP asset in **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings**.
4. RCCP detects the pipeline change automatically and opens the **Pipeline Converter Window**. Follow each numbered step in order.
5. If the converter does not open automatically, open it from the menu: **Tools > BoneCracker Games > Realistic Car Controller Pro > Render Pipeline Converter**.
6. Verify that the scripting symbol `BCG_URP` has been added. RCCP sets this automatically, but you can confirm in **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.
7. Materials should now render correctly. If any still appear pink, re-run steps 3 and 4 in the converter.

---

## Converting to HDRP

Follow these steps to set up RCCP in a High Definition Render Pipeline project:

1. Open **Window > Package Manager** and install the **High Definition RP** package if it is not already installed.
2. Create an **HDRP Pipeline Asset** (or use an existing one) via **Assets > Create > Rendering > HD Render Pipeline Asset**.
3. Assign the HDRP asset in **Edit > Project Settings > Graphics > Scriptable Render Pipeline Settings**.
4. RCCP detects the pipeline change automatically and opens the **Pipeline Converter Window**. Follow each numbered step in order.
5. When prompted, also import `BCG_HDRPVolumeProfile.unitypackage` from `Addons/Installers/` to get pre-configured post-processing settings for the demo scenes.
6. Verify that the scripting symbol `BCG_HDRP` has been added. RCCP sets this automatically, but you can confirm in **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.
7. Materials should now render correctly.

---

## Using the Built-in Pipeline

The Built-in pipeline is the default for RCCP. No scripting symbols or special setup is required.

If your materials appear pink (magenta) in a Built-in project -- for example, after accidentally importing URP or HDRP shaders -- import `RCCP_BuiltinShaders.unitypackage` from `Addons/Installers/` to restore the correct shaders.

---

## Scripting Define Symbols

RCCP uses scripting symbols to conditionally compile pipeline-specific code. These are managed automatically by `RCCP_InitLoad` at editor startup and `RCCP_SetScriptingSymbol` when the pipeline changes.

| Symbol | Pipeline | When It Is Set |
|--------|----------|----------------|
| `BCG_URP` | Universal RP | Automatically when a URP Pipeline Asset is detected in Graphics settings |
| `BCG_HDRP` | High Definition RP | Automatically when an HDRP Pipeline Asset is detected in Graphics settings |
| *(none)* | Built-in | Both symbols are removed when no SRP asset is assigned |

If automatic detection fails, you can add or remove these symbols manually in **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.

---

## Pipeline-Specific Features

Not all RCCP features are available in every pipeline. The table below summarizes the differences:

| Feature | Built-in | URP | HDRP |
|---------|----------|-----|------|
| Vehicle materials | Standard shader | URP Lit shader | HDRP Lit shader |
| Lens flares | Legacy `LensFlare` component | `LensFlareComponentSRP` | `LensFlareComponentSRP` |
| Decals (customization) | Not supported | `DecalProjector` | `DecalProjector` |
| Neons (customization) | Not supported | `DecalProjector` | `DecalProjector` |
| Post-processing | Legacy Post Processing Stack | Volume components | Volume components |

**Important:** Decals and neons in the [Customization](12_customization.md) system require URP or HDRP. They are not available in the Built-in pipeline because they rely on the `DecalProjector` component, which is an SRP-only feature.

---

## Troubleshooting

### Pink or Magenta Materials

This means materials are using shaders that do not exist in your current pipeline. The most common causes:

- No shader package has been imported. Open the Pipeline Converter Window and follow the steps.
- The wrong shader package was imported (for example, URP shaders in an HDRP project). Remove the incorrect shaders and import the correct package.
- You switched pipelines but did not re-run the converter. Open it from the menu and follow all steps again.

### "Shader not found" Errors in Console

Import the correct shader package for your Unity version from `Addons/Installers/Shaders/`. Make sure the Unity version folder matches your project (2021, 2022, 2023, or 6).

### BCG_URP or BCG_HDRP Symbol Not Set

RCCP sets these automatically at editor startup. If the symbol is missing:

1. Confirm that your render pipeline asset is assigned in **Edit > Project Settings > Graphics**.
2. Try reimporting `RCCP_InitLoad.cs` (right-click > Reimport) to trigger the detection again.
3. As a last resort, add the symbol manually in **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.

### Decals or Neons Not Visible

You are most likely using the Built-in pipeline. Decals and neons require URP or HDRP. See [Customization](12_customization.md) for details.

### Lens Flares Not Showing

- **Built-in pipeline:** Ensure the vehicle lights have a legacy `LensFlare` component and that a Flare asset is assigned.
- **URP / HDRP:** Ensure the lights have a `LensFlareComponentSRP` component. Run step 2 (Lens Flare Conversion) in the Pipeline Converter Window if they are still using legacy flares.

### Demo Scenes Look Wrong After Pipeline Switch

After converting to URP or HDRP, use the Pipeline Converter Window to run the demo scene conversion step. For URP this enables post-processing on cameras; for HDRP this updates directional lights and adds the Volume Profile.

---

**See also:** [Settings](04_settings.md) | [Customization](12_customization.md) | [Installation](01_installation.md)

---

**Support:** bonecrackergames@gmail.com | [www.bonecrackergames.com](https://www.bonecrackergames.com)

**Need help?** See [Troubleshooting](25_troubleshooting.md)

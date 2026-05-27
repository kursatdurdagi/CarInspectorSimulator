# Realistic Car Controller Pro - Documentation

**Version:** 2.41.1  
**Unity:** 6000.0 and above  
**Publisher:** BoneCracker Games

Welcome to the official documentation for Realistic Car Controller Pro (RCCP). This guide covers everything you need to get started, configure vehicles, and integrate RCCP into your project.

## Quick Start

1. Import RCCP from the Unity Asset Store via Package Manager
2. Let RCCP create the required layers automatically (or go to **Tools > BoneCracker Games > Realistic Car Controller Pro > Settings**)
3. Make sure Unity's **Input System** package is installed
4. Open the prototype scene: `Assets/Realistic Car Controller Pro/Scenes/RCCP_Scene_Blank_Prototype.unity`
5. Press **Play** and drive with **WASD** keys

## Documentation Index

### Getting Started

| # | Document | Description |
|---|----------|-------------|
| 01 | [Installation](01_installation.md) | Importing, layer setup, Input System, addon packages |
| 02 | [Architecture Overview](02_architecture.md) | Component hierarchy, drivetrain chain, events, behaviors |
| 03 | [Vehicle Setup](03_vehicle_setup.md) | Setup Wizard walkthrough and manual vehicle setup |
| 04 | [Settings](04_settings.md) | RCCP Settings: behaviors, physics, mobile, layers, audio |

### Input and Controls

| # | Document | Description |
|---|----------|-------------|
| 05 | [Inputs](05_inputs.md) | Keyboard and gamepad controls, input rebinding |
| 06 | [Overriding Inputs](06_overriding_inputs.md) | External vehicle control via code (AI, cutscenes, networking) |
| 07 | [Mobile](07_mobile.md) | Touch, gyro, steering wheel, and joystick controls |
| 08 | [Logitech Steering Wheels](08_logitech_steering_wheels.md) | Logitech hardware wheel setup and force feedback |

### Vehicle Systems

| # | Document | Description |
|---|----------|-------------|
| 09 | [Camera System](09_camera_system.md) | 7 camera modes, orbit controls, showroom camera |
| 10 | [Ground Materials](10_ground_materials.md) | Surface physics, terrain support, skidmarks |
| 11 | [Damage System](11_damage_system.md) | Mesh deformation, detachable parts, wheel and light damage |
| 12 | [Customization](12_customization.md) | Paint, wheels, spoilers, decals, neons, performance upgrades |
| 13 | [AI Vehicles](13_ai_vehicles.md) | AI navigation, waypoints, brake zones, obstacle avoidance |
| 14 | [Recording and Playback](14_recording_playback.md) | Record and replay vehicle sessions |

### Management and API

| # | Document | Description |
|---|----------|-------------|
| 15 | [Scene Manager](15_scene_manager.md) | Vehicle tracking, transport, runtime behavior switching |
| 16 | [API Reference](16_api_reference.md) | All public methods: spawn, register, control, repair, events |
| 17 | [Telemetry and Debug](17_telemetry_debug.md) | Real-time telemetry overlay and debug tools |

### Render Pipelines and Demo Content

| # | Document | Description |
|---|----------|-------------|
| 18 | [Render Pipelines](18_render_pipelines.md) | Built-in, URP, and HDRP setup with shader packages |
| 19 | [Demo Content](19_demo_content.md) | Demo scenes, vehicles, and environmental objects |

### Integration Guides

| # | Document | Description |
|---|----------|-------------|
| 20 | [Enter-Exit Vehicle](20_integration_enter_exit.md) | BCG Shared Assets: character enter/exit vehicles |
| 21 | [Photon PUN 2](21_integration_photon.md) | Multiplayer vehicle sync with Photon |
| 22 | [Mirror Networking](22_integration_mirror.md) | Multiplayer vehicle sync with Mirror |
| 23 | [ProFlares](23_integration_proflares.md) | Enhanced lens flare effects for vehicle lights |
| 24 | [Traffic Controller](24_integration_traffic.md) | Integration with Realistic Traffic Controller |

### Help

| # | Document | Description |
|---|----------|-------------|
| 25 | [Troubleshooting](25_troubleshooting.md) | Common issues, error messages, and solutions |

## Support

- **Email:** bonecrackergames@gmail.com
- **Website:** [www.bonecrackergames.com](https://www.bonecrackergames.com)

When reporting an issue, please include your Unity version, RCCP version, render pipeline, and any console error messages.

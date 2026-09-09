# Godot Windows Rendering Backend Recommendation

Date: 2026-09-09

## User

for godot 4.7.2, is it recommended to use dx12 or vulkan for a windows x64 only project

## Assistant

I’ll check Godot’s official guidance and version information for Windows rendering backends.

## Assistant

Godot switched the Windows default to Direct3D 12 in 4.6, citing more stable driver support. One architecture page still recommends Vulkan, so I’m checking the 4.7 settings reference to resolve that inconsistency.

## Assistant

**Use Direct3D 12 (DX12) as the default for a Windows x64-only project in Godot 4.7.2.**

Godot switched new Windows projects to DX12 in 4.6 for **more stable driver support and fewer platform quirks**. The 4.7 documentation explicitly recommends switching older Windows projects to `d3d12` too. [Release notes](https://godotengine.org/releases/4.6/#windows), [4.7 settings reference](https://docs.godotengine.org/en/4.7/classes/class_projectsettings.html#class-projectsettings-property-rendering-rendering-device-driver-windows).

My recommendation:

- **Default to DX12.** Choose Vulkan if testing your game on target hardware reveals a specific DX12 bug or a meaningful Vulkan performance advantage.
- **Keep Vulkan fallback enabled** for machines where DX12 is unsupported. [Fallback setting](https://docs.godotengine.org/en/4.7/classes/class_projectsettings.html#class-projectsettings-property-rendering-rendering-device-fallback-to-vulkan).

The project setting is `rendering/rendering_device/driver.windows = "d3d12"`. This applies to either Forward+ or Mobile; choosing between those renderers is a separate decision. [Renderer overview](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html).


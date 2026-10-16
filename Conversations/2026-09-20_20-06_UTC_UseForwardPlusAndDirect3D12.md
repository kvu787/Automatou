# Use Forward Plus And Direct3D 12

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

change the godot project to use forward+ renderer and to only use dx12

## Assistant

I’ll check the Godot project settings, switch it to Forward+ with Direct3D 12, and commit the changes.

## Assistant

The project now starts with Forward+ and Direct3D 12, with Vulkan and OpenGL fallbacks disabled. The test encountered restricted access to Godot’s shader cache; I’ll repeat it with normal access to verify startup.

## Assistant

Changed the project to **Forward+ with Direct3D 12 only**, disabling Vulkan and OpenGL fallbacks.

Verified a clean Godot startup using `D3D12 — Forward+`. Changes and conversation log committed separately.

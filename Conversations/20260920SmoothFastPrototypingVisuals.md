# Smooth Fast Prototyping Visuals

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

currently, there is unpleasant "jaggedness" in the visuals. can some kind of smoothing or anti-aliasing be applied to fix this?

## Assistant

I’ll check how the prototype renders its visuals and apply suitable anti-aliasing to smooth the jagged edges.

## Assistant

The jagged edges come mainly from filled hexagons and unit arrows. Godot’s current renderer doesn’t support 2D multisample anti-aliasing, so I’ll switch to its Mobile renderer and enable 4× anti-aliasing, then check the rendered result. ([Godot documentation](https://docs.godotengine.org/en/stable/tutorials/2d/2d_antialiasing.html))

## Assistant

Enabled **4× anti-aliasing** for smoother hexagons and unit arrows, using Godot’s Mobile renderer.

The build and interface checks passed, and I inspected the rendered result. Changes are committed. Restart using **Run.cmd** to see the improvement.

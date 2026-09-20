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

## User

is there a reason that mobile renderer is used instead of forward+

## Assistant

I’ll check Godot’s renderer guidance to explain the choice and whether Forward+ would improve this prototype.

## Assistant

Yes. I chose **Mobile because it supports the anti-aliasing we need with a simpler rendering pipeline**. Despite its name, it supports desktop applications. Godot describes it as having lower baseline rendering cost than Forward+. ([Renderer comparison](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html))

Both support **4× 2D MSAA**, so Forward+ would not inherently make these hexagons or arrows smoother. ([2D anti-aliasing documentation](https://docs.godotengine.org/en/stable/tutorials/2d/2d_antialiasing.html))

Forward+ would also be a valid choice. Its additional capabilities mainly benefit advanced 3D visuals, such as global illumination and volumetric fog, which this prototype doesn’t use. ([Feature comparison](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html))

I didn’t benchmark the two, so this was a reasonable default based on the prototype’s needs, rather than a measured performance advantage on your machine.

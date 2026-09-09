# Rectangular grid motion experiment

## User

in the experiments folder, create a godot 4.7.2 experiment that demonstrates different approaches to translating and rotating rectangular footprinted units on a square grid.

## Assistant

I’ll inspect the experiments and available Godot tools, then build a runnable comparison of movement and rotation approaches for rectangular units.

## Assistant

Godot 4.7.2 is installed. I’ll show three boards responding to the same commands: a grid snap that checks only the destination, a center turn that checks swept space, and a corner-pivot turn that checks swept space. Editable obstacles and several footprint sizes will make the differences visible, including the alignment shift needed when rotating a 3 × 2 unit.

## Assistant

The three movement rules and interactive boards are implemented. I’m checking collision behavior and the standalone export next. Swept turns use conservative padding between collision samples to prevent missed obstacles; the experiment will note that this can reject very tight clearances.

## Assistant

Godot 4.7.2 imports and runs the experiment, the collision checks pass, and the Windows executable exports and launches successfully. I also checked the rendered layout. I’m refining the starting obstacle so the first turn demonstrates a swept collision without relying on tight-clearance padding, then documenting the pivot behavior and committing the experiment and conversation separately.

## Assistant

Created [RectangularGridMotion](C:/Users/k/Repository/CSharp/Automapolis/Experiments/RectangularGridMotion/README.md) with three synchronized boards, editable obstacles, three footprint sizes, and slow-motion previews.

Double-click [Run.cmd](C:/Users/k/Repository/CSharp/Automapolis/Experiments/RectangularGridMotion/Run.cmd) to build and launch. Press **E** first to see the movement rules produce different outcomes.

Verified with Godot **4.7.2**: collision checks pass, layout reviewed, standalone executable exported and smoke-tested. Experiment and conversation committed separately.

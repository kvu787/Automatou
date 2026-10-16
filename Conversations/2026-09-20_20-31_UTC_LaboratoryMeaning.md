# Laboratory Meaning

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping

explain what the term "laboratory" means in this codebase

## Assistant

I’ll look through the prototype’s code and documentation to see how it uses “laboratory.”

## Assistant

“Laboratory” means **the prototype’s environment for experimenting with simulated worlds and unit behavior**. You create a world, place units, run or step the simulation, inspect decisions, adjust settings, and rewind to compare outcomes. The [README](/C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/README.md:15) describes this experiment loop.

In the code, `Laboratory` is the **main Godot interface class**, attached to the root scene. It coordinates the board, menus, world creator, inspector, playback, and experiment controls. Its implementation is spread across several partial-class files, including `Navigation.cs`, `Creators.cs`, and `ExperimentControls.cs`. See [Laboratory.cs](/C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Interface/Laboratory.cs:7).

The name expresses the application's purpose; it does not identify an in-world building or unit. The simulated world and its rules live separately in [World.cs](/C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Source/Simulation/World.cs:5).

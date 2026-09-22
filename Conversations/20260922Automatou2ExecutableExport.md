# Automatou2 Executable Export

## User

C:\Users\k\Repository\Automatou\Automatou2

does the build and run process do any export to EXE?

## Assistant

I’ll check Automatou2’s launch scripts and build configuration for an EXE export step.

## Assistant

No. The current process:

- Builds the C# project in Debug mode using `dotnet build` ([Run.ps1, line 32](/C:/Users/k/Repository/Automatou/Automatou2/Run.ps1:32)).
- Launches the project through the installed Godot executable with `--path` ([Run.ps1, line 42](/C:/Users/k/Repository/Automatou/Automatou2/Run.ps1:42)).

It does not export a standalone game EXE or run `dotnet publish`.


## User

Launches the project through the installed Godot executable with --path

what are the performance characteristics of running the app this way compared to exporting a release build to EXE and running the exe? use the actual godot source at "C:\Users\k\Repository\External\Godot_4-7-2" to inform your answer

## Assistant

I’ll trace the startup and build paths in your local Godot source, then check which differences could affect Automatou2’s runtime performance.

## Assistant

The source shows two separate differences: the launcher builds Automatou2’s C# code without optimizations, and it runs an optimized Godot engine that includes debug features. `--path` itself only selects the project; it does not start the editor UI. I’m checking the release export settings and which debug features add runtime work.

## Assistant

Release removes native allocation tracking and enables optimization for the main C# project. Two project-specific details also matter: Automatou2 caps rendering at 60 FPS and writes simulation logs synchronously in all configurations. Its two supporting C# projects also leave optimization unset under Godot’s `ExportRelease` configuration, so a release export needs that checked before assuming all simulation code is optimized.

## Assistant

**A release export can improve CPU performance and reduce engine overhead. For Automatou2, the C# build configuration is likely the most consequential difference.** The size of the improvement needs measurement.

I checked your local Godot source and confirmed that its commit, `ed1daf0bf`, matches the installed executable’s reported version.

1. **Your C# code currently runs without release optimizations.**
   The launcher builds `Debug`. Godot’s SDK explicitly sets `Optimize=false` for that configuration and `Optimize=true` for `ExportRelease`; the exporter selects `ExportRelease` for a release export. I also verified those evaluated properties on your actual project. This affects CPU-heavy simulation and drawing preparation. C# debug compilation also disables JIT optimizations through an assembly attribute, so this difference persists without an attached debugger. Sources: [Godot SDK settings](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/Godot.NET.Sdk/Godot.NET.Sdk/Sdk/Sdk.props:46), [export configuration](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:189), [Microsoft’s compiler documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.optimizationlevel).

2. **The installed Godot engine is already optimized, but includes additional runtime work.**
   Godot’s normal editor build enables `TOOLS_ENABLED` and `DEBUG_ENABLED`. A release template omits both. The source defaults to `speed_trace` optimization for the editor and `speed` for release—both are optimized configurations. For MSVC, both use `/O2`; other supported compiler paths distinguish `-O2` and `-O3`. These are source defaults, not a reconstruction of the distributed binaries’ complete build flags.
   A concrete cost that release removes: Godot’s native allocator performs extra allocation bookkeeping and atomic memory-usage updates under `DEBUG_ENABLED`. Debug metadata also occupies memory. This gives release an advantage in native CPU overhead and engine memory footprint, with workload-dependent savings. Sources: [build configuration](/C:/Users/k/Repository/External/Godot_4-7-2/SConstruct:533), [compiler flags](/C:/Users/k/Repository/External/Godot_4-7-2/SConstruct:842), [allocator](/C:/Users/k/Repository/External/Godot_4-7-2/core/os/memory.cpp:89).

3. **Your current launch does not run the editor UI or attach Godot’s debugger.**
   `--path` selects the project. Godot creates its editor UI only when editor mode is enabled. Your launcher supplies neither the editor flag nor debugger flags, and debugger initialization returns immediately when its connection URI is empty. Consequently, exporting will not eliminate an editor UI or active debugger that is consuming resources in this launch. Sources: [editor creation](/C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:4601), [debugger initialization](/C:/Users/k/Repository/External/Godot_4-7-2/core/debugger/engine_debugger.cpp:123).

4. **Launching an existing exported EXE skips the restore/build stage.**
   Your launcher performs both before starting Godot, even when the build is incremental. That affects time to launch, separately from frame performance. The exported application still initializes Godot and .NET; ordinary Windows export publishes a self-contained .NET application. Sources: [launcher](/C:/Users/k/Repository/Automatou/Automatou2/Run.ps1:30), [publish arguments](/C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs:230).

5. **Visible FPS might stay exactly the same.**
   Automatou2 caps rendering at **60 FPS**. If both versions comfortably meet that cap, release may provide more CPU headroom without increasing displayed FPS. With the same rendering settings, a workload limited by GPU rendering should generally benefit less than one limited by C# execution. Also, your simulation writes logs synchronously using `File.AppendAllText`; exporting does not remove that work. Sources: [rendering settings](/C:/Users/k/Repository/Automatou/Automatou2/project.godot:7), [logging](/C:/Users/k/Repository/Automatou/Automatou2/Source/Interface/MainInterface.cs:73).

One project-specific caveat: **`AutomatonPrograms` and `AutomatonContract` do not currently enable optimization for `ExportRelease`.** They use the ordinary Microsoft SDK, and evaluating that configuration leaves `Optimize` unset, whereas the main Godot project sets it to `true`. The compiler defaults to optimization being disabled. A release export setup should explicitly enable optimization for those projects too. [Compiler defaults](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/code-generation#optimize)

I have not benchmarked either mode, so I cannot justify a percentage or multiplier from the source alone.

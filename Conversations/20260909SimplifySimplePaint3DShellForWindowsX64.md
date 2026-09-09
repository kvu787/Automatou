# Simplify SimplePaint3DShell for Windows x64

## User

C:\Users\k\Repository\CSharp\Automapolis\Shells\SimplePaint3DShell only needs to run on x64 windows. rewrite accordingly.

## Assistant

I’ll inspect SimplePaint3DShell, simplify it for Windows x64, and verify the build. I’ll also record this conversation in a separate commit as required by the repository instructions.

## Assistant

The shell still has Linux/macOS export presets and portable host-launch fallbacks. I’ll remove those, target Windows x64 explicitly, and publish a self-contained x64 Kernel host so the exported game includes its required .NET runtime.

## User

also, rewrite the shell's Run.cmd as a simple wrapper Run.cmd around Run.ps1 so that most of the code is in powershell instead of batch

## Assistant

I’ll move the build workflow into `Run.ps1` and make `Run.cmd` a small wrapper. The PowerShell script will handle errors and launching, with `-BuildOnly` to build and test without opening the game.

## Assistant

The Windows x64 build and export passed, along with all 12 Kernel tests and 236 shell integration checks. `Run.cmd` is now a three-line wrapper. I’m checking launcher error handling and confirming that the exported shell and host load their bundled runtimes.

## Assistant

The exported shell and Kernel host are confirmed x64 binaries, and both load their bundled .NET runtimes. Startup and shutdown checks passed, including host cleanup. The launcher also stops correctly on missing tools and failed commands. I’m committing the implementation and conversation log separately.

## Assistant

Done. SimplePaint3DShell now targets Windows x64 only, with other platform exports and portable host fallbacks removed.

[Run.cmd](C:/Users/k/Repository/CSharp/Automapolis/Shells/SimplePaint3DShell/Run.cmd) is a three-line wrapper around [Run.ps1](C:/Users/k/Repository/CSharp/Automapolis/Shells/SimplePaint3DShell/Run.ps1). Use `Run.cmd -BuildOnly` to skip launching.

Verified build/export, 12 Kernel tests, 236 integration checks, bundled runtimes, and launcher error handling. Implementation and conversation committed separately; working tree clean.

## User

C:\Users\k\Repository\CSharp\Automapolis\Shells\SimplePaint3DShell\project.godot

why is gl still present:
config/features=PackedStringArray("4.7", "C#", "GL Compatibility")
renderer/rendering_method="gl_compatibility"

## Assistant

I’ll check how those rendering settings relate to the Windows x64 changes and record this follow-up in the conversation log.

## Assistant

I preserved the existing renderer during the Windows x64 rewrite. **Windows x64 supports OpenGL**, so those settings remain valid.

- `"GL Compatibility"` identifies the project’s renderer in its feature list.
- `renderer/rendering_method="gl_compatibility"` selects that renderer.

Godot’s Compatibility renderer suits the current 2D prototype. For the planned modern 3D presentation, **Forward+** offers more rendering features and can use **Vulkan or Direct3D 12** on Windows. That would be a separate renderer change. [Godot renderer documentation](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html).

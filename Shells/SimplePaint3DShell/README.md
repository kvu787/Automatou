# SimplePaint3DShell

SimplePaint3DShell is an optional presentation layer written in C# for Godot 4.7.2 .NET. Its current presentation is the symbolic 2d hex-grid prototype; the planned SimplePaint 3D models and camera are not implemented yet. It renders the Bastion Front as a responsive tactical hex grid with terrain colors, force symbols, sector inspection, front dispatches, and Command controls, but contains no simulation rules.

The shell starts the adjacent `KernelHost` process and exchanges one UTF-8 JSON object per line. C# presentation DTOs describe this protocol; the shell has no reference to the Kernel assembly and contains no simulation rules. Process output is queued and applied on the Godot main thread. Closing the shell terminates its host process.

## Build and run

Install the .NET 10 SDK selected by the repository's `global.json`, Godot 4.7.2
**.NET**, and its matching .NET export templates. The standard GDScript-only
Godot executable cannot load this project.

Double-click this folder's `Run.cmd` or the repository-root `Run.cmd`. Both build
the C# solution, run the Kernel and shell integration tests, publish the Kernel
host for editor play, export `Build/SimplePaint3DShell.exe`, publish its adjacent
`Build/KernelHost`, and launch the standalone game. Use `Run.cmd --build-only`
to perform the same build and checks without launching a window.

The launcher defaults to
`%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`.
Set `GODOT_EXE` to the full path of another Godot 4.7.2 .NET console executable
if needed. Close a running standalone game before exporting over its files.

The project uses `Godot.NET.Sdk/4.7.2`, targets `net10.0`, and participates in
`Automapolis.slnx`. `SimplePaint3DShell.slnx` supports the Godot editor's Debug,
ExportDebug, and ExportRelease configurations. The root solution maps Release
to the shell's ExportRelease configuration.

If NuGet cannot resolve the Godot SDK, register the packages bundled with the
.NET editor (adjust the path for a custom installation):

```powershell
dotnet nuget add source "$env:USERPROFILE\Program\Godot_v4.7.2-stable_mono_win64\GodotSharp\Tools\nupkgs" --name Godot472Local
```

For editor play, build the Debug configuration, publish the host to `KernelHost`,
and open `project.godot` in the .NET editor. See the integration commands below
for the build and publish steps. The interface and hex drawing live in
`Scripts/Main.cs` and `Scripts/HexCell.cs`; `KernelConnection.cs` handles the
separate process and `KernelProtocol.cs` defines the received presentation data.

## Controls

- Click a sector to inspect its terrain and all forces occupying it.
- Press Space, Enter, or **Advance the Front** to resolve exactly one autonomous turn.
- Enter an integer seed and click **Open Front** to start a new deterministic theater.
- For passive play, use only **Advance the Front**. Player commands remain available at any time.
- The player can channel resonance, fortify sectors, deploy soldiers, replace a lost Bastion, establish enclaves, and authorize magitech purges.

## Symbols

The grid represents terrain with colored fills; empty cells contain no symbol.
Occupied cells show a large Kernel force glyph over the terrain color, with an
occupant count in the upper-right when multiple forces share the cell.
The legend uses color swatches for terrain and symbols for forces. Hover or
select a cell to inspect all occupants.

| Symbol | Force             |
| ------ | ----------------- |
| B      | Bastion           |
| S      | Soldier           |
| r      | Ravener           |
| N      | Brood node        |
| E      | Enclave           |

Production art is being created in Blender with SimplePaint. The 2d shell
uses font glyphs and code-authored controls, with no generated image dependency.

## Platform exports

Windows, Linux, and macOS .NET export presets are included. Keep the exported `data_SimplePaint3DShell_*` runtime directory beside the executable. Publish `Automapolis.Kernel.Host` for the target platform into a `KernelHost` folder beside that executable (inside `Contents/MacOS` for a macOS app bundle). The Kernel host requires the .NET 10 runtime unless published with `--self-contained true` and the matching runtime identifier. The Windows `Run.cmd` automates the local build and launch; Linux and macOS exports have not been verified on this Windows machine.

## Hex layout

The board uses pointy-top hexagons in odd-row offset coordinates. Odd rows are
shifted half a hex to the right. Cells meet edge to edge with no spacing.
Use the **Outline Width** slider below the legend to adjust outlines immediately
(0–12 board pixels in 0.25 steps, default 2; 0 hides outlines). The current value
appears beside the slider and stays in effect when selecting cells, advancing
turns, or opening a new front during the session. The initial value is also
editable as **Hex Outline Width** on the root `SimplePaint3DShell` node in `Main.tscn`. Each cell
contributes half of the shared outline inside its polygon, so changing the width
does not move cells or change click targets. Selection and hover add separate
2-board-pixel colored outlines extending inward from the terrain edge. The gray
cell delineation stays unchanged, including between adjacent highlighted cells. Selection and hover follow the actual hex
polygon, including where adjacent button bounding boxes overlap. The inspector
shows column and row; field commands use those same coordinates. Purge radius
one covers the selected hex and its six neighbors, clipped at map boundaries.

The current prototype has single-hex forces. It does not yet implement the
planned six-direction facing, rotation costs, or variable footprints.

## Integration check

From the repository root, build the C# tests and publish the Kernel host:

```powershell
dotnet build Automapolis.slnx --configuration Debug
dotnet publish Source/Automapolis.Kernel.Host --configuration Debug --no-build --output Shells/SimplePaint3DShell/KernelHost
$godotExecutable = "$env:USERPROFILE\Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
& $godotExecutable --headless --path Shells/SimplePaint3DShell --editor --import
& $godotExecutable --headless --path Shells/SimplePaint3DShell res://Tests/HexGridSmoke.tscn
```

The C# test runs through `Tests/HexGridSmoke.tscn`. It checks the live protocol,
hex selection (including overlapping cell bounding boxes), turn advancement,
keyboard controls, resized maps, outline settings, glyphs, player interventions,
rejected commands, Unicode names, and full 64-bit seeds. Test source is compiled
only in Debug and test resources are excluded from standalone exports.

For rendered seam regression checks, run Godot without `--headless` and add
`-- --render-check` to the integration command. This scans the board interior
for exposed background pixels at five outline widths (including zero and 8.5)
and three window sizes, including 2560×1392. Preview images are saved in `Build`.
The shell disables GUI pixel snapping so fractional hex positions remain intact
in the rendered transforms as well as the layout geometry.

Hex exteriors use antialiased edge strokes over opaque polygons to smooth
diagonals without opening gaps. Interior fills and highlights use inward-only
alpha fringes so smoothing never spills onto the delineation. The smoothing
scales to one screen pixel and refreshes when the window is resized. This works with the existing
Compatibility renderer; no graphics-mode change is required.

The rendered checks also compare adjacent selected and hovered cells against an
unhighlighted image at three delineation widths. Pixels outside both terrain
interiors must remain identical. An adjacent-highlight preview is saved in `Build`.

Godot references: [C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html) and [command-line exports](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html).

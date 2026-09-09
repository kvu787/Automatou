# 2d shell (Godot)

This is the currently implemented symbolic 2d shell, distinct from the planned main 3D Shell. This optional presentation layer targets Godot 4.7.2. It renders the Bastion Front as a responsive tactical hex grid with symbols with sector inspection, front dispatches, and Command controls, but contains no simulation rules.

The 2d shell starts the adjacent `KernelHost` process and exchanges one JSON object per line. It can therefore be replaced without changing the game.

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

Windows, Linux, and macOS export presets are included. Publish `Automapolis.Kernel.Host` for the target .NET runtime into a `KernelHost` folder beside the Godot executable. The Windows `Run.cmd` automates the local build and launch.

## Hex layout

The board uses pointy-top hexagons in odd-row offset coordinates. Odd rows are
shifted half a hex to the right. Cells meet edge to edge with no spacing.
Use the **Outline Width** slider below the legend to adjust outlines immediately
(0–12 board pixels in 0.25 steps, default 2; 0 hides outlines). The current value
appears beside the slider and stays in effect when selecting cells, advancing
turns, or opening a new front during the session. The initial value is also
editable as **Hex Outline Width** on the root `Automapolis` node in `Main.tscn`. Each cell
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

Publish the Kernel host into `Shells/Godot/KernelHost`, then run Godot with
`--headless --path Shells/Godot --script Tests/HexGridSmoke.gd` from the repository
root. This checks the live protocol, hex selection (including overlapping cell
bounding boxes), turn advancement, resized maps, and player interventions. The test
scripts are excluded from standalone exports.

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

# Rectangular Grid Motion

A self-contained Godot 4.7.2 experiment with one movement rule: destination snap on a 12 x 12 square grid.

Double-click `Run.cmd` to import, verify, export, and launch `Build/RectangularGridMotion.exe`. The launcher closes an existing instance before export so Windows can replace the executable. This resets the running experiment. If it cannot close the old window, close it manually and try again. Set `GODOT_EXE` to override the default Godot 4.7.2 executable in `%UserProfile%\Program`. Matching Windows export templates are required.

## Controls

- WASD or arrows: move one cell in world coordinates.
- Q / E: turn a quarter-turn left / right.
- Footprint buttons: select 3 x 1, 4 x 2, or 3 x 2 and reset.
- Click a cell: toggle an obstacle. Occupied cells cannot become obstacles.
- R / Reset: restore the unit and default obstacle map.
- Clear obstacles: reset the unit on an empty map.

There are no dropdowns or animations. Accepted commands apply immediately. A blocked command preserves the position and facing, and shows its destination in red.

## Movement rule

Only the destination footprint must fit inside the board and avoid occupied cells. Cells between the starting and ending footprints are not checked. Touching obstacle or board edges is allowed.

The gold dot marks a persistent rear pivot centered across the unit's local width. For odd widths it is half a cell inward from the rear edge; for even widths it is one cell inward. For a 3 x 1 unit it is the center of the rear cell. The pivot moves with translations and stays fixed during turns. All supported footprints remain aligned with whole grid cells through four quarter-turns. The arrow tracks the unit's front through right, down, left, and up.

## Integer math

All movement, pivot, rotation, and collision calculations use integers, as required by [AGENTS.md](AGENTS.md). Centers and pivots use doubled cell coordinates; dimensions and obstacles use whole cells. A quarter-turn swaps and negates integer coordinates. Collision checks compare integer rectangle bounds. No angle interpolation, trigonometry, tolerance, or intermediate rotation geometry is used. Godot receives pixel coordinates for drawing; display operations do not determine movement or collisions.

## Verification

`Verify.gd` checks movement, obstacle and boundary rejection, edge contact, destination-only rotation, pivot centering, exact full rotations, and grid alignment for all footprint sizes. `VerifyKeyboard.gd` checks WASD/Q/E and confirms that the scene has no select boxes. Run either with Godot's `--headless --path <experiment folder> --script <verification script>` arguments. `Run.cmd` runs both before export.

Generated imports, executables, and local capture artifacts are ignored by `.gitignore`.

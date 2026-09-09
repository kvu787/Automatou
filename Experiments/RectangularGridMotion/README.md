# Rectangular Grid Motion

A self-contained Godot 4.7.2 experiment comparing rectangular footprints on a 12 x 12 square grid. No dependency on the main game or its kernel.

Double-click `Run.cmd` to import, verify, export, and launch `Build/RectangularGridMotion.exe`. The launcher uses the Godot 4.7.2 installation in `%UserProfile%\Program`, or the executable specified by `GODOT_EXE`. Matching Windows export templates must be installed. You can also open `project.godot` in Godot 4.7.2 and press F6 from `Main.tscn`.

## Controls

- WASD or arrow keys translate one cell in world coordinates.
- Q and E turn 90 degrees counterclockwise and clockwise.
- Select an approach from the dropdown. Only that approach runs, on one board. Switching preserves the current pose and obstacle map.
- Click a cell to toggle an obstacle. Occupied cells cannot become obstacles.
- R resets the unit and restores the obstacle arrangement.
- Clear obstacles resets the unit on an empty map.
- Choose 3 x 1, 4 x 2, or 3 x 2 footprints. Changing size resets the experiment.
- Moves and turns apply immediately, with no animation or input delay.

## Approaches

1. **Destination snap:** rotate around a fixed physical corner of the unit (initially its top-left corner). Only the destination is checked; intervening geometry is intentionally ignored. Clockwise and counterclockwise turns give different positions and facings. Four quarter-turns restore the original pose.
2. **Center sweep:** translate linearly, or rotate around the center while applying any required grid correction linearly. Check the whole attempted motion. Same-parity dimensions (3 x 1 or 4 x 2) preserve the center. Mixed-parity dimensions (3 x 2) cannot rotate 90 degrees about a fixed center and still occupy whole grid cells. The target top-left coordinate is rounded to the nearest integer, with positive half ties upward. This introduces a half-cell shift along each axis and can accumulate drift on repeated turns.
3. **Corner pivot:** translate linearly, or rotate around the same fixed physical corner used by destination snap, checking the entire sweep. The gold dot marks this corner. It moves with translations but stays fixed during successive turns. Integer corners ensure the resulting footprint stays aligned to the grid.

The unit tracks four persistent facings: right (3 o'clock), down (6), left (9), and up (12). The arrow marks its front; the footprint dimensions describe its world-space bounding rectangle. Translation preserves facing; rejected turns preserve both position and facing. Accepted commands immediately update the unit. Rejected commands leave it in place and show a static red outline of the rejected destination.

## Things to try

On the initial 3 x 1 layout, press E. The destination snap and corner turn succeed; the center turn clips the obstacle at cell (5, 2), even though its destination is clear. Select each approach and reset before pressing E to compare from identical starting poses.

Clear obstacles and select 3 x 2 (then clear obstacles again). Turn repeatedly and compare the center coordinates. Center correction drifts, while the two corner-based rules retain the same pivot across turns.

Build a corridor by clicking cells, then try translation and rotation near its walls. A footprint fitting at the destination does not imply it has enough turning clearance.

## Collision model and limits

`Motion.gd` uses a separating-axis test between an oriented rectangle and solid unit-square obstacles. Destination edge contact is permitted. Swept approaches evaluate 121 poses over each command, with conservative padding equal to a bound on maximum point travel between a sample and its nearest neighbor. This covers motion between samples rather than allowing narrow intersections to tunnel through. The same padding applies to board boundaries. It can reject grazing or extremely tight but otherwise clear motions, including motion starting flush against a wall. This is a comparison of movement policies, not an exact continuous collision solver or a path planner.

`Verify.gd` checks translations, obstacle and boundary rejection, edge contact, footprint dimension swaps, mixed-parity alignment, the starting sweep distinction, and complete clockwise and counterclockwise rotations with persistent facing and pivot. Run it with Godot's `--headless --path <experiment folder> --script Verify.gd` arguments. `Run.cmd` runs these checks before export.

All generated imports, executables, and local capture artifacts are ignored by the experiment's `.gitignore`.



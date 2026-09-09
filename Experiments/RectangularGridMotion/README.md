# Rectangular Grid Motion

A self-contained Godot 4.7.2 experiment comparing rectangular footprints on a 12 x 12 square grid. No dependency on the main game or its kernel.

Double-click `Run.cmd` to import, verify, export, and launch `Build/RectangularGridMotion.exe`. The launcher uses the Godot 4.7.2 installation in `%UserProfile%\Program`, or the executable specified by `GODOT_EXE`. Matching Windows export templates must be installed. You can also open `project.godot` in Godot 4.7.2 and press F6 from `Main.tscn`.

## Controls

- WASD or arrow keys translate one cell in world coordinates.
- Q and E turn 90 degrees counterclockwise and clockwise.
- Click a cell on any board to toggle that obstacle on all boards. Cells occupied on any board cannot become obstacles.
- R resets the units and restores the obstacle arrangement.
- Clear obstacles resets the units on an empty map.
- Choose 3 x 1, 4 x 2, or 3 x 2 footprints. Changing size resets the experiment.
- Slow motion lengthens the previews. Commands are ignored during an active preview.

## Approaches

1. **Destination snap:** retain the axis-aligned footprint's top-left cell and swap width and height on a turn. Translation and rotation happen immediately. Only the destination is checked; intervening geometry is intentionally ignored. Both turn directions yield the same occupied cells.
2. **Center sweep:** translate linearly, or rotate around the center while applying any required grid correction linearly. Check the whole attempted motion. Same-parity dimensions (3 x 1 or 4 x 2) preserve the center. Mixed-parity dimensions (3 x 2) cannot rotate 90 degrees about a fixed center and still occupy whole grid cells. The target top-left coordinate is rounded to the nearest integer, with positive half ties upward. This introduces a half-cell shift along each axis and can accumulate drift on repeated turns.
3. **Corner pivot:** translate linearly, or rotate around the footprint's current world-space top-left corner. Integer corners ensure the resulting footprint stays aligned to the grid. The pivot is chosen anew on each command, so it is not a persistent physical corner; repeated turns can move the unit across the board. The gold dot identifies the current attempted pivot.

The state is an axis-aligned occupied rectangle, not a vehicle with persistent heading. The line inside a unit shows the local horizontal axis of the current preview; it resets after each completed turn. Accepted commands update each board independently. A rejected command plays a red hypothetical preview and leaves that board's unit at its original pose.

## Things to try

On the initial 3 x 1 layout, press E. The destination snap and corner turn succeed; the center turn clips the obstacle at cell (5, 2), even though its destination is clear. Faint outlines show the attempted sweep. Reset before repeating a comparison from identical poses.

Clear obstacles and select 3 x 2 (then clear obstacles again). Turn repeatedly and compare the center coordinates. Center correction drifts, top-left snapping retains the anchor, and the corner rule moves the footprint around successive pivots.

Build a corridor by clicking cells, then try translation and rotation near its walls. A footprint fitting at the destination does not imply it has enough turning clearance.

## Collision model and limits

`Motion.gd` uses a separating-axis test between an oriented rectangle and solid unit-square obstacles. Destination edge contact is permitted. Swept approaches evaluate 121 poses over each command, with conservative padding equal to a bound on maximum point travel between a sample and its nearest neighbor. This covers motion between samples rather than allowing narrow intersections to tunnel through. The same padding applies to board boundaries. It can reject grazing or extremely tight but otherwise clear motions, including motion starting flush against a wall. This is a comparison of movement policies, not an exact continuous collision solver or a path planner.

`Verify.gd` checks translations, obstacle and boundary rejection, edge contact, footprint dimension swaps, mixed-parity alignment, the starting sweep distinction, and repeated anchored turns. Run it with Godot's `--headless --path <experiment folder> --script Verify.gd` arguments. `Run.cmd` runs these checks before export.

All generated imports, executables, and local capture artifacts are ignored by the experiment's `.gitignore`.

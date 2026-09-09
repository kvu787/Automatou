# Rectangular Grid Motion

A self-contained Godot 4.7.2 experiment comparing rectangular footprints on a 12 x 12 square grid. No dependency on the main game or its kernel.

Double-click `Run.cmd` to import, verify, export, and launch `Build/RectangularGridMotion.exe`. The launcher uses the Godot 4.7.2 installation in `%UserProfile%\Program`, or the executable specified by `GODOT_EXE`. Matching Windows export templates must be installed. You can also open `project.godot` in Godot 4.7.2 and press F6 from `Main.tscn`.

## Integer-only requirement

Every approach must use integer math only. See [AGENTS.md](AGENTS.md) for the experiment requirement. The current implementation still uses floating-point geometry and does not yet comply; the descriptions below document its existing behavior.

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

1. **Destination snap:** rotate around a fixed rear pivot centered across the unit's width. Only the destination is checked; intervening geometry is intentionally ignored. Clockwise and counterclockwise turns give different positions and facings. Four quarter-turns restore the original pose.
2. **Center sweep:** translate linearly, or rotate around the center while applying any required grid correction linearly. Check the whole attempted motion. Same-parity dimensions (3 x 1 or 4 x 2) preserve the center. Mixed-parity dimensions (3 x 2) cannot rotate 90 degrees about a fixed center and still occupy whole grid cells. The target top-left coordinate is rounded to the nearest integer, with positive half ties upward. This introduces a half-cell shift along each axis and can accumulate drift on repeated turns.
3. **Rear pivot sweep:** translate linearly, or rotate around the same rear centerline pivot used by destination snap, checking the entire sweep. It moves with translations but stays fixed during successive turns.

All pivots are centered in at least one local dimension. The center approach uses the geometric center; the rear approaches are centered across the width. The gold dot shows the pivot for every approach. For odd widths, the rear pivot is half a cell inward from the rear edge (the rear cell center for 3 x 1). For even widths, it is one cell inward and halfway across the width, between cells. This parity choice preserves whole-cell alignment through quarter-turns, including 3 x 2. Pivots offset from both centerlines are excluded.

The unit tracks four persistent facings: right (3 o'clock), down (6), left (9), and up (12). The arrow marks its front; the footprint dimensions describe its world-space bounding rectangle. Translation preserves facing; rejected turns preserve both position and facing. Accepted commands immediately update the unit. Rejected commands leave it in place and show a static red outline of the rejected destination.

## Things to try

On the initial 3 x 1 layout, press E. The destination snap and rear-pivot turn succeed; the center turn clips the obstacle at cell (5, 2), even though its destination is clear. Select each approach and reset before pressing E to compare from identical starting poses.

Clear obstacles and select 3 x 2 (then clear obstacles again). Turn repeatedly and compare the center coordinates. Center correction drifts, while the two rear-pivot rules retain the same pivot across turns.

Build a corridor by clicking cells, then try translation and rotation near its walls. A footprint fitting at the destination does not imply it has enough turning clearance.

## Collision model and limits

`Motion.gd` uses a separating-axis test between an oriented rectangle and solid unit-square obstacles. Touching edges are permitted. Cardinal translation checks the exact swept rectangle, so units can slide along walls or move away from them. Turns use adaptive interval subdivision: padded bounds prove an interval clear, while unpadded poses detect actual overlaps. Possible contacts receive finer checks instead of automatically rejecting the command. The maximum subdivision depth is 14; unresolved motion below 0.001 cell for the supported footprints is treated as contact. This is a numerical collision check, not a symbolic continuous solver. Rotations still require clearance around the entire turning footprint, even when the destination is clear.
`Verify.gd` checks translations, obstacle and boundary rejection, edge contact, footprint dimension swaps, mixed-parity alignment, the starting sweep distinction, and complete clockwise and counterclockwise rotations with persistent facing and pivot. Run it with Godot's `--headless --path <experiment folder> --script Verify.gd` arguments. `Run.cmd` runs these checks before export.

All generated imports, executables, and local capture artifacts are ignored by the experiment's `.gitignore`.






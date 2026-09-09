# Rectangular Grid Motion

A self-contained Godot 4.7.2 experiment comparing integer-only turn rejection rules on a 12 x 12 square grid. Every rule uses the same rear pivot and final quarter-turn. Sweep check defaults to **Off**, preserving destination-only movement.

Double-click `Run.cmd` to import, verify, export, and launch `Build/RectangularGridMotion.exe`. The launcher closes an existing instance before export so Windows can replace the executable. This resets the running experiment. If it cannot close the old window, close it manually and try again. Set `GODOT_EXE` to override the default Godot 4.7.2 executable in `%UserProfile%\Program`. Matching Windows export templates are required.

## Controls and preview

- WASD or arrows: move one cell in world coordinates. Translation always checks only its destination.
- Q / E or Turn left / right: attempt a quarter-turn using the selected rejection rule.
- Sweep check dropdown: change the rule without moving or resetting the unit.
- Inspect left / right: show the next turn's required cells without executing it.
- Footprint buttons: select 3 x 1, 4 x 2, or 3 x 2 and reset.
- Click a cell: toggle an obstacle. Cells occupied by the actual unit cannot become obstacles.
- R / Reset: restore the unit and default obstacles while retaining the selected sweep rule.
- Clear obstacles: reset the unit on an empty map, retaining the selected sweep rule.

Amber cells must be clear for the previewed turn. Red cells contain blocking obstacles. Inset outlines show the destination. A red board border and the outside-board count indicate required cells beyond the board. The preview always describes the **next** turn from the current pose; after an accepted turn it updates to another turn. Inspecting or switching a rule does not execute the preview.

Moves and turns apply instantly without animations. Rejection preserves position and facing. The status distinguishes a blocked destination from a sweep-rule rejection with a clear destination.

## Sweep rejection rules

All rules check destination occupancy and board limits. Enabled rules also reserve the starting footprint. The unit itself is not an obstacle. Every reserved cell must be inside the board and free of obstacles.

| Sweep check        | Additional reservation rule                                                             |
| ------------------ | --------------------------------------------------------------------------------------- |
| Off                | None. Only destination cells must be clear, preserving the original behavior.           |
| Endpoint rectangle | Every cell in the smallest axis-aligned rectangle containing both footprints.           |
| Row-first paths    | Each source cell travels to its rotated destination cell horizontally, then vertically. |
| Column-first paths | Each source cell travels to its rotated destination cell vertically, then horizontally. |
| Pivot envelope     | A conservative square around the pivot, sized to contain the unit at any orientation.   |

Row-first and column-first reserve the union of inclusive Manhattan paths between corresponding source and destination cell centers. Horizontal and vertical refer to world grid axes, not unit-facing axes. These rules deliberately differ: a turn can take inward paths or reserve an outward corridor. They are discrete cell-reservation conventions, not a deforming unit or intermediate angled rectangles.

The endpoint rectangle and path rules do not claim to contain the continuous physical rotation of a rigid rectangle. The envelope is intentionally conservative: it reserves a whole square for any orientation, rather than a directional quarter-turn sector. Its radius is the integer ceiling square root of the largest squared pivot-to-corner distance, in doubled cell units. All grid cells with positive-area intersection with that square are reserved. Cells just touching its outer boundary are not added. No trigonometry or sampled angles are used.

## Quick comparison

1. Keep 3 x 1, reset, and inspect right.
2. Add an obstacle at cell (5, 5), using zero-based column/row coordinates: one cell right and down from the gold pivot's cell.
3. Switch rules without turning. Off and Row-first paths allow the turn; Endpoint rectangle, Column-first paths, and Pivot envelope reject it even though the destination is clear.
4. Remove that obstacle and add one at (7, 7). Only Pivot envelope rejects this starting turn.

On the empty initial 3 x 1 map, the right-turn preview requires 3 cells for Off, 5 for Row-first paths, 9 for Endpoint rectangle and Column-first paths, and 49 for Pivot envelope. Inspect left or change the footprint to see different masks.

## Pivot and integer math

The gold dot marks a persistent rear pivot centered across the unit's local width. For odd widths it is half a cell inward from the rear edge; for even widths it is one cell inward. For a 3 x 1 unit it is the center of the rear cell. The pivot moves with translations and stays fixed during turns. The arrow tracks the unit's front through right, down, left, and up.

All movement, pivot, rotation, reservation, and collision calculations use integers, as required by [AGENTS.md](AGENTS.md). Centers and pivots use doubled cell coordinates; dimensions and obstacles use whole cells. A quarter-turn swaps and negates integer coordinates. Godot receives pixel coordinates for drawing; display operations do not determine movement or collisions.

## Verification

`Verify.gd` checks destination-only behavior, pivot centering, exact full rotations, whole-cell alignment, expected masks, sweep-only obstacle rejection, board boundaries, and unchanged translation for all rules. Coverage includes all footprint sizes, all four facings, and both turn directions. `VerifyKeyboard.gd` checks default Off selection, switching without movement, WASD/Q/E under every rule, preview/action agreement, and unchanged pose after rejection. `Run.cmd` runs both before export.

Generated imports, executables, and local capture artifacts are ignored by `.gitignore`.

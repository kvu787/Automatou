extends SceneTree
const Motion = preload("res://Motion.gd")
var failures := 0
func check(condition: bool, description: String) -> void:
	if not condition:
		push_error(description)
		failures += 1
func _initialize() -> void:
	var center := Vector2i(11, 9)
	var dimensions := Vector2i(3, 1)
	check(Motion.clear_pose(center, dimensions, []), "Open footprint")
	check(not Motion.clear_pose(center, dimensions, [Vector2i(4, 4)]), "Occupied cell blocked")
	check(Motion.clear_pose(center, dimensions, [Vector2i(3, 4)]), "Edge contact allowed")
	check(Motion.proposal(center, dimensions, Vector2i.RIGHT, 0, []).center == center + Vector2i(2, 0), "One-cell translation")
	check(not Motion.proposal(center, dimensions, Vector2i.RIGHT, 0, [Vector2i(7, 4)]).accepted, "Occupied destination blocked")
	check(not Motion.proposal(Vector2i(3, 1), dimensions, Vector2i.LEFT, 0, []).accepted, "Outside board blocked")
	check(Motion.proposal(Vector2i(3, 1), dimensions, Vector2i.RIGHT, 0, []).accepted, "Movement along board edge")
	# This turn fits at its destination even though a physical arc would leave the board.
	check(Motion.proposal(Vector2i(5, 13), Vector2i(1, 3), Vector2i.ZERO, 1, [], 1).accepted, "No intermediate-angle collision checks")
	for size in [Vector2i(3, 1), Vector2i(4, 2), Vector2i(3, 2)]:
		for direction in [-1, 1]:
			var start: Vector2i = Vector2i(8, 8) + size
			var position := start
			var footprint: Vector2i = size
			var heading := 0
			var pivot := Motion.pivot_for(start, size, heading)
			for quarter in range(4):
				check(Motion.pivot_for(position, footprint, heading) == pivot, "Pivot stays fixed")
				check(Motion.rotate_quarters(pivot - position, -heading).y == 0, "Pivot centered across width")
				var turn := Motion.proposal(position, footprint, Vector2i.ZERO, direction, [], heading)
				check(turn.accepted, "Open turn accepted")
				check(turn.heading == posmod(direction * (quarter + 1), 4), "Full facing cycle")
				position = turn.center
				footprint = turn.dimensions
				heading = turn.heading
				check((position - footprint).x % 2 == 0 and (position - footprint).y % 2 == 0, "Whole-cell alignment")
			check(position == start and footprint == size and heading == 0, "Four turns restore exact pose")
	print("Motion verification: %d failures" % failures)
	quit(1 if failures else 0)

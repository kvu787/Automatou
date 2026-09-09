extends SceneTree
const Motion = preload("res://Motion.gd")
var failures := 0

func check(condition: bool, description: String) -> void:
	if not condition:
		push_error(description)
		failures += 1

func _initialize() -> void:
	var center := Vector2(5.5, 4.5)
	var dimensions := Vector2(3, 1)
	check(Motion.clear_pose(center, dimensions, 0, []), "Open pose is clear")
	check(not Motion.clear_pose(center, dimensions, 0, [Vector2i(4, 4)]), "Occupied cell collides")
	check(Motion.clear_pose(center, dimensions, 0, [Vector2i(3, 4)]), "Edge contact is allowed")
	for approach in range(3):
		var move := Motion.proposal(center, dimensions, Vector2.RIGHT, 0, approach, [])
		check(move.accepted and move.center.is_equal_approx(center + Vector2.RIGHT), "One-cell translation")
		var blocked := Motion.proposal(center, dimensions, Vector2.RIGHT, 0, approach, [Vector2i(7, 4)])
		check(not blocked.accepted, "Translation cannot enter an obstacle")
		var turn := Motion.proposal(center, dimensions, Vector2.ZERO, 1, approach, [])
		check(turn.accepted and turn.dimensions == Vector2(1, 3), "Open rotation swaps dimensions")
		var outside := Motion.proposal(Vector2(1.5, 0.5), dimensions, Vector2.LEFT, 0, approach, [])
		check(not outside.accepted, "Board boundary enforced")
	var snap := Motion.proposal(center, dimensions, Vector2.ZERO, 1, 0, [Vector2i(5, 2)])
	var swept := Motion.proposal(center, dimensions, Vector2.ZERO, 1, 1, [Vector2i(5, 2)])
	check(snap.accepted and not swept.accepted, "Destination and swept collision differ")
	check(Motion.clear_pose(swept.center, swept.dimensions, 0, [Vector2i(5, 2)]), "Swept example has a clear destination")
	var actual_intersection := false
	for pose in swept.path:
		if not Motion.clear_pose(pose.center, dimensions, pose.angle, [Vector2i(5, 2)]):
			actual_intersection = true
	check(actual_intersection, "Swept example intersects without conservative padding")
	check(Motion.proposal(center, dimensions, Vector2.ZERO, 1, 2, [Vector2i(5, 2)]).accepted, "Corner pivot clears example obstacle")
	var mixed := Motion.proposal(Vector2(5.5, 5), Vector2(3, 2), Vector2.ZERO, 1, 1, [])
	check((mixed.center - mixed.dimensions * 0.5).is_equal_approx((mixed.center - mixed.dimensions * 0.5).round()), "Mixed parity rotation lands on cell edges")
	for approach in range(3):
		for direction in [-1, 1]:
			var position := center
			var footprint := dimensions
			var heading := 0
			var pivot := center - dimensions * 0.5
			for quarter in range(4):
				var turn := Motion.proposal(position, footprint, Vector2.ZERO, direction, approach, [], heading)
				check(turn.accepted, "Open full rotation is accepted")
				check(turn.heading == posmod(direction * (quarter + 1), 4), "Facing advances through all four directions")
				if approach != 1:
					check(turn.pivot.is_equal_approx(pivot), "Physical pivot stays fixed across turns")
				position = turn.center
				footprint = turn.dimensions
				heading = turn.heading
			check(position.is_equal_approx(center) and footprint == dimensions and heading == 0, "Four turns restore complete pose")
	print("Motion verification: %d failures" % failures)
	quit(1 if failures else 0)





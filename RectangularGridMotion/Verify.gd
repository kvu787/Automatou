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
	var off := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [], 0, Motion.SweepCheck.Off)
	var box := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [], 0, Motion.SweepCheck.EndpointRectangle)
	var row := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [], 0, Motion.SweepCheck.RowFirst)
	var column := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [], 0, Motion.SweepCheck.ColumnFirst)
	var envelope := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [], 0, Motion.SweepCheck.PivotEnvelope)
	check(off.required.size() == 3, "Off reserves only destination")
	check(row.required.size() == 5 and not row.required.has(Vector2i(5, 5)), "Row-first inward paths reserve five cells")
	check(box.required.size() == 9 and column.required.size() == 9, "Box and outward paths reserve a three by three region")
	check(envelope.required.size() == 49 and envelope.required.has(Vector2i(1, 1)) and envelope.required.has(Vector2i(7, 7)), "Pivot square has expected integer bounds")
	for mode in range(Motion.SweepNames.size()):
		var obstacle := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [Vector2i(5, 5)], 0, mode)
		check(obstacle.destination_clear, "Example obstacle is outside destination")
		check(obstacle.accepted == (mode == Motion.SweepCheck.Off or mode == Motion.SweepCheck.RowFirst), "Sweep-only obstruction distinguishes modes")
		var remote := Motion.proposal(center, dimensions, Vector2i.ZERO, 1, [Vector2i(7, 7)], 0, mode)
		check(remote.accepted == (mode != Motion.SweepCheck.PivotEnvelope), "Envelope reserves extra clearance")
		var movement := Motion.proposal(center, dimensions, Vector2i.RIGHT, 0, [Vector2i(5, 3)], 0, mode)
		check(movement.accepted and movement.required.size() == 3, "Sweep selection does not alter translation")
		var boundary := Motion.proposal(Vector2i(5, 13), Vector2i(1, 3), Vector2i.ZERO, 1, [], 1, mode)
		check(boundary.accepted == (mode != Motion.SweepCheck.PivotEnvelope), "Only envelope exceeds board in boundary example")
		check(boundary.outside_cells.is_empty() == boundary.accepted, "Outside-board cells explain rejection")
		for size in [Vector2i(3, 1), Vector2i(4, 2), Vector2i(3, 2)]:
			for heading in range(4):
				var footprint: Vector2i = size if heading % 2 == 0 else Vector2i(size.y, size.x)
				var position := Vector2i(8, 8) + footprint
				for direction in [-1, 1]:
					var proposal := Motion.proposal(position, footprint, Vector2i.ZERO, direction, [], heading, mode)
					for cell in Motion.footprint_cells(proposal.center, proposal.dimensions):
						check(proposal.required.has(cell), "Every mask includes destination in every facing")
					if mode != Motion.SweepCheck.Off:
						for cell in Motion.footprint_cells(position, footprint):
							check(proposal.required.has(cell), "Enabled masks include starting footprint")
	print("Motion verification: %d failures" % failures)
	quit(1 if failures else 0)


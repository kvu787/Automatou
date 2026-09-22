extends RefCounted

const BoardSize := 12

enum SweepCheck { Off, EndpointRectangle, RowFirst, ColumnFirst, PivotEnvelope }
const SweepNames := ["Off", "Endpoint rectangle", "Row-first paths", "Column-first paths", "Pivot envelope"]
const SweepDescriptions := [
	"Only the destination footprint must be clear.",
	"Reserve the smallest rectangle containing both footprints.",
	"Each cell follows a horizontal path, then a vertical path.",
	"Each cell follows a vertical path, then a horizontal path.",
	"Reserve a square around the pivot large enough for any orientation.",
]

static func inside_board(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < BoardSize and cell.y < BoardSize

static func footprint_cells(center: Vector2i, dimensions: Vector2i) -> Array[Vector2i]:
	var cells: Array[Vector2i] = []
	var lower := (center - dimensions) / 2
	for y in range(lower.y, lower.y + dimensions.y):
		for x in range(lower.x, lower.x + dimensions.x):
			cells.append(Vector2i(x, y))
	return cells

# Add an inclusive rectangle; a horizontal or vertical segment is a thin rectangle.
static func reserve_rectangle(cells: Dictionary, first: Vector2i, last: Vector2i) -> void:
	for y in range(mini(first.y, last.y), maxi(first.y, last.y) + 1):
		for x in range(mini(first.x, last.x), maxi(first.x, last.x) + 1):
			cells[Vector2i(x, y)] = true

static func floor_half(value: int) -> int:
	return value / 2 - (1 if value < 0 and value % 2 != 0 else 0)

static func turn_cells(center: Vector2i, dimensions: Vector2i, target: Vector2i, next_dimensions: Vector2i, pivot: Vector2i, turn: int, sweep_check: int) -> Array[Vector2i]:
	var required: Dictionary = {}
	var source := footprint_cells(center, dimensions)
	var destination := footprint_cells(target, next_dimensions)
	for cell in destination:
		required[cell] = true
	if sweep_check != SweepCheck.Off:
		for cell in source:
			required[cell] = true
	match sweep_check:
		SweepCheck.EndpointRectangle:
			reserve_rectangle(required, source[0].min(destination[0]), source[-1].max(destination[-1]))
		SweepCheck.RowFirst, SweepCheck.ColumnFirst:
			for cell in source:
				# Rotate each cell center exactly around the doubled-coordinate pivot.
				var end := (pivot + rotate_quarters(cell * 2 + Vector2i.ONE - pivot, turn) - Vector2i.ONE) / 2
				var bend := Vector2i(end.x, cell.y) if sweep_check == SweepCheck.RowFirst else Vector2i(cell.x, end.y)
				reserve_rectangle(required, cell, bend)
				reserve_rectangle(required, bend, end)
		SweepCheck.PivotEnvelope:
			var radius_squared := 0
			for sign_vector in [Vector2i(-1, -1), Vector2i(1, -1), Vector2i(1, 1), Vector2i(-1, 1)]:
				var offset: Vector2i = center + dimensions * sign_vector - pivot
				radius_squared = maxi(radius_squared, offset.x * offset.x + offset.y * offset.y)
			# Integer ceiling square root in doubled-cell units. No real-number math.
			var radius := 0
			while radius * radius < radius_squared:
				radius += 1
			var lower := pivot - Vector2i.ONE * radius
			var upper := pivot + Vector2i.ONE * radius
			reserve_rectangle(required, Vector2i(floor_half(lower.x), floor_half(lower.y)), Vector2i(floor_half(upper.x - 1), floor_half(upper.y - 1)))
	var cells: Array[Vector2i] = []
	for cell in required:
		cells.append(cell)
	return cells

# Centers and pivots use doubled cell coordinates; dimensions use whole cells.
static func rotate_quarters(value: Vector2i, quarters: int) -> Vector2i:
	match posmod(quarters, 4):
		0: return value
		1: return Vector2i(-value.y, value.x)
		2: return -value
		_: return Vector2i(value.y, -value.x)

static func overlaps(center: Vector2i, dimensions: Vector2i, cell: Vector2i) -> bool:
	var lower := center - dimensions
	var upper := center + dimensions
	var cell_lower := cell * 2
	var cell_upper := cell_lower + Vector2i(2, 2)
	return lower.x < cell_upper.x and upper.x > cell_lower.x and lower.y < cell_upper.y and upper.y > cell_lower.y

static func clear_pose(center: Vector2i, dimensions: Vector2i, obstacles: Array) -> bool:
	var lower := center - dimensions
	var upper := center + dimensions
	if lower.x < 0 or lower.y < 0 or upper.x > BoardSize * 2 or upper.y > BoardSize * 2:
		return false
	if lower.x % 2 != 0 or lower.y % 2 != 0:
		return false
	for cell in obstacles:
		if overlaps(center, dimensions, cell):
			return false
	return true

static func pivot_for(center: Vector2i, dimensions: Vector2i, heading: int) -> Vector2i:
	var local_dimensions := dimensions if heading % 2 == 0 else Vector2i(dimensions.y, dimensions.x)
	var rear_inset := 1 if local_dimensions.y % 2 == 1 else 2
	return center + rotate_quarters(Vector2i(rear_inset - local_dimensions.x, 0), heading)

static func proposal(center: Vector2i, dimensions: Vector2i, direction: Vector2i, turn: int, obstacles: Array, heading := 0, sweep_check := SweepCheck.Off) -> Dictionary:
	var next_dimensions := dimensions if turn == 0 else Vector2i(dimensions.y, dimensions.x)
	var pivot := pivot_for(center, dimensions, heading)
	var target := center + direction * 2
	if turn != 0:
		target = pivot + rotate_quarters(center - pivot, turn)
	var required := footprint_cells(target, next_dimensions) if turn == 0 else turn_cells(center, dimensions, target, next_dimensions, pivot, turn, sweep_check)
	var blocked_cells: Array[Vector2i] = []
	var outside_cells: Array[Vector2i] = []
	for cell in required:
		if not inside_board(cell):
			outside_cells.append(cell)
		elif obstacles.has(cell):
			blocked_cells.append(cell)
	var destination_clear := clear_pose(target, next_dimensions, obstacles)
	var accepted := destination_clear and blocked_cells.is_empty() and outside_cells.is_empty()
	return {"center": target, "dimensions": next_dimensions, "accepted": accepted, "heading": posmod(heading + turn, 4), "required": required, "blocked_cells": blocked_cells, "outside_cells": outside_cells, "destination_clear": destination_clear}


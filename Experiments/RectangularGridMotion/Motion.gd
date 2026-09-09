extends RefCounted

const BoardSize := 12

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

static func proposal(center: Vector2i, dimensions: Vector2i, direction: Vector2i, turn: int, obstacles: Array, heading := 0) -> Dictionary:
	var next_dimensions := dimensions if turn == 0 else Vector2i(dimensions.y, dimensions.x)
	var pivot := pivot_for(center, dimensions, heading)
	var target := center + direction * 2
	if turn != 0:
		target = pivot + rotate_quarters(center - pivot, turn)
	return {"center": target, "dimensions": next_dimensions, "accepted": clear_pose(target, next_dimensions, obstacles), "heading": posmod(heading + turn, 4)}

extends RefCounted

const BoardSize := 12
const Samples := 120

static func corners(center: Vector2, dimensions: Vector2, angle: float) -> PackedVector2Array:
	var points := PackedVector2Array()
	for sign_vector in [Vector2(-1, -1), Vector2(1, -1), Vector2(1, 1), Vector2(-1, 1)]:
		points.append(center + (sign_vector * dimensions * 0.5).rotated(angle))
	return points

# Separating-axis test: touching edges are allowed; positive area overlap is blocked.
static func overlaps(center: Vector2, dimensions: Vector2, angle: float, cell: Vector2, padding := 0.0) -> bool:
	var difference := cell + Vector2.ONE * 0.5 - center
	var horizontal := Vector2.RIGHT.rotated(angle)
	var vertical := Vector2.DOWN.rotated(angle)
	for axis in [Vector2.RIGHT, Vector2.DOWN, horizontal, vertical]:
		var rectangle_radius: float = absf(axis.dot(horizontal)) * dimensions.x * 0.5 + absf(axis.dot(vertical)) * dimensions.y * 0.5
		var cell_radius: float = (absf(axis.x) + absf(axis.y)) * 0.5
		if absf(difference.dot(axis)) >= rectangle_radius + cell_radius + padding - 0.00001:
			return false
	return true

static func clear_pose(center: Vector2, dimensions: Vector2, angle: float, obstacles: Array, padding := 0.0) -> bool:
	for point in corners(center, dimensions, angle):
		if point.x < padding - 0.00001 or point.y < padding - 0.00001 or point.x > BoardSize - padding + 0.00001 or point.y > BoardSize - padding + 0.00001:
			return false
	for cell in obstacles:
		if overlaps(center, dimensions, angle, Vector2(cell), padding):
			return false
	return true

# Centered across the width. Choose the rear-most interior point with matching
# coordinate parity so every quarter-turn still lands on whole grid cells.
static func pivot_for(center: Vector2, dimensions: Vector2, heading: int, approach: int) -> Vector2:
	if approach == 1:
		return center
	var local_dimensions := dimensions if heading % 2 == 0 else Vector2(dimensions.y, dimensions.x)
	var rear_inset := 0.5 if int(local_dimensions.y) % 2 == 1 else 1.0
	var offset := Vector2(rear_inset - local_dimensions.x * 0.5, 0)
	return center + offset.rotated(heading * PI * 0.5)

# Refine only intervals whose padded bound might hit something. Actual contact
# without overlap is allowed. At the depth limit, the unresolved travel bound
# is below 0.001 cell for the supported footprints and quarter-turns.
static func clear_turn(start: Vector2, target: Vector2, dimensions: Vector2, pivot: Vector2, angle: float, rear: bool, obstacles: Array, travel: float, lower := 0.0, upper := 1.0, depth := 0) -> bool:
	var middle := (lower + upper) * 0.5
	var center := pivot + (start - pivot).rotated(angle * middle) if rear else start.lerp(target, middle)
	if not clear_pose(center, dimensions, angle * middle, obstacles):
		return false
	if clear_pose(center, dimensions, angle * middle, obstacles, travel * (upper - lower) * 0.5):
		return true
	if depth == 14:
		return true
	return clear_turn(start, target, dimensions, pivot, angle, rear, obstacles, travel, lower, middle, depth + 1) and clear_turn(start, target, dimensions, pivot, angle, rear, obstacles, travel, middle, upper, depth + 1)

static func proposal(center: Vector2, dimensions: Vector2, direction: Vector2, turn: int, approach: int, obstacles: Array, heading := 0) -> Dictionary:
	var next_dimensions := dimensions if turn == 0 else Vector2(dimensions.y, dimensions.x)
	var angle := turn * PI * 0.5
	var pivot := pivot_for(center, dimensions, heading, approach)
	var target := center + direction
	if turn != 0:
		if approach == 0:
			target = pivot + (center - pivot).rotated(angle)
		elif approach == 1:
			# Mixed parity dimensions require a half-cell correction to land on the grid.
			target = (center - next_dimensions * 0.5).round() + next_dimensions * 0.5
		else:
			target = pivot + (center - pivot).rotated(angle)
	var path: Array = []
	var accepted := clear_pose(center, dimensions, 0, obstacles) and clear_pose(target, next_dimensions, 0, obstacles)
	# Bound the speed of every rectangle point for adaptive rotation checks.
	var travel := center.distance_to(target) + absf(angle) * dimensions.length() * 0.5
	if approach == 2 and turn != 0:
		travel = absf(angle) * (center.distance_to(pivot) + dimensions.length() * 0.5)
	if accepted and approach != 0:
		if turn == 0:
			# Cardinal translation sweeps exactly this axis-aligned rectangle.
			accepted = clear_pose((center + target) * 0.5, dimensions + direction.abs(), 0, obstacles)
		else:
			accepted = clear_turn(center, target, dimensions, pivot, angle, approach == 2, obstacles, travel)
	for sample in range(Samples + 1):
		var fraction := float(sample) / Samples
		var sample_center := center.lerp(target, fraction)
		if approach == 2 and turn != 0:
			sample_center = pivot + (center - pivot).rotated(angle * fraction)
		var pose := {"center": sample_center, "angle": angle * fraction}
		path.append(pose)


	return {"center": target, "dimensions": next_dimensions, "path": path, "accepted": accepted, "pivot": pivot, "heading": posmod(heading + turn, 4)}




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

static func proposal(center: Vector2, dimensions: Vector2, direction: Vector2, turn: int, approach: int, obstacles: Array) -> Dictionary:
	var next_dimensions := dimensions if turn == 0 else Vector2(dimensions.y, dimensions.x)
	var angle := turn * PI * 0.5
	var pivot := center - dimensions * 0.5
	var target := center + direction
	if turn != 0:
		if approach == 0:
			target = pivot + next_dimensions * 0.5
		elif approach == 1:
			# Mixed parity dimensions require a half-cell correction to land on the grid.
			target = (center - next_dimensions * 0.5).round() + next_dimensions * 0.5
		else:
			target = pivot + (center - pivot).rotated(angle)
	var path: Array = []
	var accepted := true
	# Padding bounds the maximum point displacement between a sample and its nearest neighbor.
	var travel := center.distance_to(target) + absf(angle) * dimensions.length() * 0.5
	if approach == 2 and turn != 0:
		travel = absf(angle) * dimensions.length()
	var padding := travel / (2.0 * Samples)
	for sample in range(Samples + 1):
		var fraction := float(sample) / Samples
		var sample_center := center.lerp(target, fraction)
		if approach == 2 and turn != 0:
			sample_center = pivot + (center - pivot).rotated(angle * fraction)
		var pose := {"center": sample_center, "angle": angle * fraction}
		path.append(pose)
		if approach != 0 and not clear_pose(sample_center, dimensions, pose.angle, obstacles, padding):
			accepted = false
	if not clear_pose(target, next_dimensions, 0.0, obstacles):
		accepted = false
	return {"center": target, "dimensions": next_dimensions, "path": path, "accepted": accepted, "pivot": pivot}

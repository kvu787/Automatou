extends Node2D

const Motion = preload("res://Motion.gd")
const Cell := 35.0
const Origins := [Vector2(40, 255), Vector2(510, 255), Vector2(980, 255)]
const Colors := [Color("58c9fc"), Color("67e0b1"), Color("f2bd69")]
const Titles := ["01  /  Destination snap", "02  /  Center sweep", "03  /  Corner pivot"]
const Descriptions := ["Keep the top-left cell; check the final footprint.", "Turn about the center, with grid correction.", "Turn about the current top-left corner."]
var dimensions := Vector2(3, 1)
var states: Array = []
var obstacles: Array = []
var elapsed := 1.0
var duration := 0.8
var status := "Try E: the obstacle clips a turn, but leaves its destination clear."
var font: Font = ThemeDB.fallback_font

func _ready() -> void:
	make_button("Reset [R]", Vector2(40, 135), reset)
	make_button("Clear obstacles", Vector2(190, 135), func(): obstacles.clear(); reset_units())
	var selector := OptionButton.new()
	selector.position = Vector2(390, 135)
	selector.size = Vector2(170, 40)
	for item in ["Footprint: 3 x 1", "Footprint: 4 x 2", "Footprint: 3 x 2"]:
		selector.add_item(item)
	selector.item_selected.connect(func(index): dimensions = [Vector2(3, 1), Vector2(4, 2), Vector2(3, 2)][index]; reset())
	add_child(selector)
	make_button("Turn left [Q]", Vector2(590, 135), func(): command(Vector2.ZERO, -1))
	make_button("Turn right [E]", Vector2(770, 135), func(): command(Vector2.ZERO, 1))
	make_button("Step right", Vector2(950, 135), func(): command(Vector2.RIGHT, 0))
	var slow := CheckButton.new()
	slow.text = "Slow motion"
	slow.position = Vector2(1150, 140)
	slow.toggled.connect(func(enabled): duration = 2.5 if enabled else 0.8)
	add_child(slow)
	reset()

func make_button(caption: String, position_value: Vector2, action: Callable) -> void:
	var button := Button.new()
	button.text = caption
	button.position = position_value
	button.size = Vector2(145, 40)
	button.pressed.connect(action)
	button.focus_mode = Control.FOCUS_NONE
	add_child(button)

func reset() -> void:
	obstacles = [Vector2i(5, 2), Vector2i(8, 6), Vector2i(8, 7), Vector2i(3, 8), Vector2i(4, 8)]
	reset_units()

func reset_units() -> void:
	states.clear()
	for approach in range(3):
		states.append({"center": Vector2(4, 4) + dimensions * 0.5, "dimensions": dimensions, "result": {}, "previous_dimensions": dimensions, "message": "Ready"})
	elapsed = duration
	queue_redraw()

func command(direction: Vector2, turn: int) -> void:
	if elapsed < duration:
		return
	for approach in range(3):
		var state: Dictionary = states[approach]
		state.previous_dimensions = state.dimensions
		state.result = Motion.proposal(state.center, state.dimensions, direction, turn, approach, obstacles)
		state.message = "Accepted" if state.result.accepted else "Blocked — preview only"
		if state.result.accepted:
			state.center = state.result.center
			state.dimensions = state.result.dimensions
	elapsed = 0.0
	status = "Same command, independent outcomes. Reset to compare from the same pose."

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_W, KEY_UP: command(Vector2.UP, 0)
			KEY_S, KEY_DOWN: command(Vector2.DOWN, 0)
			KEY_A, KEY_LEFT: command(Vector2.LEFT, 0)
			KEY_D, KEY_RIGHT: command(Vector2.RIGHT, 0)
			KEY_Q: command(Vector2.ZERO, -1)
			KEY_E: command(Vector2.ZERO, 1)
			KEY_R: reset()
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT and elapsed >= duration:
		for origin in Origins:
			var local: Vector2 = (get_global_mouse_position() - origin) / Cell
			if local.x < 0 or local.y < 0 or local.x >= Motion.BoardSize or local.y >= Motion.BoardSize:
				continue
			var cell := Vector2i(local.floor())
			if obstacles.has(cell):
				obstacles.erase(cell)
			else:
				for state in states:
					if Motion.overlaps(state.center, state.dimensions, 0, Vector2(cell)):
						status = "That cell is occupied on at least one board."
						return
				obstacles.append(cell)
			for state in states:
				state.result = {}
			status = "Obstacle map updated on all three boards."
			queue_redraw()

func _process(delta: float) -> void:
	elapsed = minf(elapsed + delta, duration)
	queue_redraw()

func label_at(position_value: Vector2, value: String, size_value := 18, color := Color("c2cddd")) -> void:
	draw_string(font, position_value, value, HORIZONTAL_ALIGNMENT_LEFT, -1, size_value, color)

func draw_unit(origin: Vector2, center: Vector2, footprint: Vector2, angle: float, color: Color, filled: bool) -> void:
	var points := Motion.corners(center, footprint, angle)
	for index in range(points.size()):
		points[index] = origin + points[index] * Cell
	if filled:
		draw_colored_polygon(points, Color(color, 0.24))
	points.append(points[0])
	draw_polyline(points, color, 2.0, true)
	var middle := origin + center * Cell
	draw_circle(middle, 3, color)
	draw_line(middle, middle + Vector2(footprint.x * Cell * 0.38, 0).rotated(angle), color, 3, true)

func _draw() -> void:
	label_at(Vector2(40, 48), "RECTANGULAR FOOTPRINTS", 16, Color("67e0b1"))
	label_at(Vector2(40, 91), "One grid. Three movement rules.", 34, Color.WHITE)
	label_at(Vector2(40, 119), "WASD / arrows: translate one cell     Q / E: quarter turn     Click a grid cell: toggle a shared obstacle", 18)
	for approach in range(states.size()):
		var origin: Vector2 = Origins[approach]
		var state: Dictionary = states[approach]
		var color: Color = Colors[approach]
		label_at(origin + Vector2(0, -43), Titles[approach], 24, color)
		label_at(origin + Vector2(0, -15), Descriptions[approach], 16)
		draw_rect(Rect2(origin, Vector2.ONE * Cell * Motion.BoardSize), Color("101c2c"))
		for line in range(Motion.BoardSize + 1):
			draw_line(origin + Vector2(line * Cell, 0), origin + Vector2(line * Cell, Motion.BoardSize * Cell), Color("263548"))
			draw_line(origin + Vector2(0, line * Cell), origin + Vector2(Motion.BoardSize * Cell, line * Cell), Color("263548"))
		for cell in obstacles:
			draw_rect(Rect2(origin + Vector2(cell) * Cell + Vector2.ONE * 2, Vector2.ONE * (Cell - 4)), Color("637084"))
		var result: Dictionary = state.result
		if not result.is_empty():
			var preview_color := color if result.accepted else Color("ff7188")
			if approach != 0:
				for index in range(0, result.path.size(), 10):
					var pose: Dictionary = result.path[index]
					draw_unit(origin, pose.center, state.previous_dimensions, pose.angle, Color(preview_color, 0.18), false)
			if approach == 2:
				draw_circle(origin + result.pivot * Cell, 5, Color("f2bd69"))
			draw_unit(origin, result.center, result.dimensions, 0, Color(preview_color, 0.5), false)
			if elapsed < duration and approach != 0:
				var index := mini(int(elapsed / duration * Motion.Samples), Motion.Samples)
				var pose: Dictionary = result.path[index]
				draw_unit(origin, pose.center, state.previous_dimensions, pose.angle, preview_color, true)
			else:
				draw_unit(origin, state.center, state.dimensions, 0, color, true)
		else:
			draw_unit(origin, state.center, state.dimensions, 0, color, true)
		label_at(origin + Vector2(0, 452), state.message, 22, color if result.is_empty() or result.accepted else Color("ff7188"))
		label_at(origin + Vector2(0, 480), "Center (%.1f, %.1f)   |   %d x %d" % [state.center.x, state.center.y, state.dimensions.x, state.dimensions.y], 17)
	label_at(Vector2(40, 785), status, 19, Color.WHITE)
	label_at(Vector2(40, 817), "Faint outlines: attempted sweep   /   Red: rejected attempt   /   Dot + line: center and local horizontal axis", 17)
	label_at(Vector2(40, 844), "Sweep checks use conservative padded samples; very tight clearances can be rejected. Destination snap has no swept collision check.", 16)


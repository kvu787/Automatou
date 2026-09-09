extends Node2D

const Motion = preload("res://Motion.gd")
const Cell := 40.0
const Origin := Vector2(40, 230)
const Titles := ["Destination snap", "Center sweep", "Corner pivot"]
const Descriptions := ["Keep the top-left cell; check the final footprint.", "Turn about the center, with grid correction; check swept space.", "Turn about the current top-left corner; check swept space."]
const UnitColor := Color("67e0b1")
var approach := 0
var initial_dimensions := Vector2(3, 1)
var dimensions := Vector2(3, 1)
var center := Vector2.ZERO
var obstacles: Array = []
var result: Dictionary = {}
var status := "Ready"
var font: Font = ThemeDB.fallback_font

func _ready() -> void:
	var selector := OptionButton.new()
	selector.position = Vector2(40, 95)
	selector.size = Vector2(260, 40)
	selector.focus_mode = Control.FOCUS_NONE
	for title in Titles:
		selector.add_item(title)
	selector.item_selected.connect(func(index): approach = index; result = {}; status = "Approach changed; current pose retained."; queue_redraw())
	add_child(selector)
	var footprint_selector := OptionButton.new()
	footprint_selector.position = Vector2(320, 95)
	footprint_selector.size = Vector2(180, 40)
	footprint_selector.focus_mode = Control.FOCUS_NONE
	for item in ["Footprint: 3 x 1", "Footprint: 4 x 2", "Footprint: 3 x 2"]:
		footprint_selector.add_item(item)
	footprint_selector.item_selected.connect(func(index): initial_dimensions = [Vector2(3, 1), Vector2(4, 2), Vector2(3, 2)][index]; reset())
	add_child(footprint_selector)
	make_button("Reset [R]", Vector2(520, 95), reset)
	make_button("Clear obstacles", Vector2(680, 95), func(): obstacles.clear(); reset_unit())
	make_button("Turn left [Q]", Vector2(560, 290), func(): command(Vector2.ZERO, -1))
	make_button("Turn right [E]", Vector2(720, 290), func(): command(Vector2.ZERO, 1))
	make_button("Up", Vector2(640, 355), func(): command(Vector2.UP, 0))
	make_button("Left", Vector2(560, 405), func(): command(Vector2.LEFT, 0))
	make_button("Right", Vector2(720, 405), func(): command(Vector2.RIGHT, 0))
	make_button("Down", Vector2(640, 455), func(): command(Vector2.DOWN, 0))
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
	reset_unit()

func reset_unit() -> void:
	dimensions = initial_dimensions
	center = Vector2(4, 4) + dimensions * 0.5
	result = {}
	status = "Ready"
	queue_redraw()

func command(direction: Vector2, turn: int) -> void:
	result = Motion.proposal(center, dimensions, direction, turn, approach, obstacles)
	status = "Accepted" if result.accepted else "Blocked; unit stays in place."
	if result.accepted:
		center = result.center
		dimensions = result.dimensions
	queue_redraw()

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
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var local := (get_global_mouse_position() - Origin) / Cell
		if local.x < 0 or local.y < 0 or local.x >= Motion.BoardSize or local.y >= Motion.BoardSize:
			return
		var cell := Vector2i(local.floor())
		if obstacles.has(cell):
			obstacles.erase(cell)
		elif Motion.overlaps(center, dimensions, 0, Vector2(cell)):
			status = "That cell is occupied."
			queue_redraw()
			return
		else:
			obstacles.append(cell)
		result = {}
		status = "Obstacle map updated."
		queue_redraw()

func label_at(position_value: Vector2, value: String, size_value := 18, color := Color("c2cddd")) -> void:
	draw_string(font, position_value, value, HORIZONTAL_ALIGNMENT_LEFT, -1, size_value, color)

func draw_unit(unit_center: Vector2, footprint: Vector2, color: Color, filled: bool) -> void:
	var rectangle := Rect2(Origin + (unit_center - footprint * 0.5) * Cell, footprint * Cell)
	if filled:
		draw_rect(rectangle, Color(color, 0.24))
	draw_rect(rectangle, color, false, 2)
	draw_circle(Origin + unit_center * Cell, 3, color)

func _draw() -> void:
	label_at(Vector2(40, 50), "Rectangular grid motion", 30, Color.WHITE)
	label_at(Vector2(40, 77), "WASD / arrows: move    Q / E: turn    Click a cell: toggle obstacle", 18)
	label_at(Vector2(40, 177), Titles[approach], 24, UnitColor)
	label_at(Vector2(40, 207), Descriptions[approach], 18)
	draw_rect(Rect2(Origin, Vector2.ONE * Cell * Motion.BoardSize), Color("101c2c"))
	for line in range(Motion.BoardSize + 1):
		draw_line(Origin + Vector2(line * Cell, 0), Origin + Vector2(line * Cell, Motion.BoardSize * Cell), Color("263548"))
		draw_line(Origin + Vector2(0, line * Cell), Origin + Vector2(Motion.BoardSize * Cell, line * Cell), Color("263548"))
	for cell in obstacles:
		draw_rect(Rect2(Origin + Vector2(cell) * Cell + Vector2.ONE * 2, Vector2.ONE * (Cell - 4)), Color("637084"))
	if not result.is_empty() and not result.accepted:
		draw_unit(result.center, result.dimensions, Color("ff7188"), false)
	draw_unit(center, dimensions, UnitColor, true)
	label_at(Vector2(560, 255), "Center (%.1f, %.1f)   |   %d x %d" % [center.x, center.y, dimensions.x, dimensions.y], 20)
	label_at(Vector2(560, 550), "Moves and turns apply immediately.", 17)
	label_at(Vector2(560, 580), "Red outline: rejected destination.", 17)
	label_at(Vector2(560, 630), "Reset before comparing approaches", 17)
	label_at(Vector2(560, 655), "from the same starting pose.", 17)
	label_at(Vector2(40, 750), status, 20, Color.WHITE)
	label_at(Vector2(40, 783), "Sweep checks are conservative and can reject very tight clearances.", 17)

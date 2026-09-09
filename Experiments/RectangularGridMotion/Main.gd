extends Node2D

const Motion = preload("res://Motion.gd")
const Cell := 40
const Origin := Vector2i(40, 230)
const UnitColor := Color("67e0b1")
var heading := 0
var sweep_check := Motion.SweepCheck.Off
var preview_turn := 1
var preview: Dictionary = {}
var initial_dimensions := Vector2i(3, 1)
var dimensions := Vector2i(3, 1)
var center := Vector2i.ZERO
var obstacles: Array = []
var result: Dictionary = {}
var status := "Ready"
var font: Font = ThemeDB.fallback_font

func _ready() -> void:
	var selector := OptionButton.new()
	selector.name = "SweepSelector"
	selector.position = Vector2i(40, 150)
	selector.size = Vector2i(350, 40)
	selector.focus_mode = Control.FOCUS_NONE
	for title in Motion.SweepNames:
		selector.add_item("Sweep check = " + title)
	selector.item_selected.connect(func(index): sweep_check = index; status = "Sweep rule changed; unit stays in place."; refresh_preview())
	add_child(selector)
	make_button("Inspect left", Vector2i(560, 595), func(): preview_turn = -1; refresh_preview())
	make_button("Inspect right", Vector2i(720, 595), func(): preview_turn = 1; refresh_preview())
	make_button("3 x 1", Vector2i(40, 95), func(): initial_dimensions = Vector2i(3, 1); reset())
	make_button("4 x 2", Vector2i(200, 95), func(): initial_dimensions = Vector2i(4, 2); reset())
	make_button("3 x 2", Vector2i(360, 95), func(): initial_dimensions = Vector2i(3, 2); reset())
	make_button("Reset [R]", Vector2i(520, 95), reset)
	make_button("Clear obstacles", Vector2i(680, 95), func(): obstacles.clear(); reset_unit())
	make_button("Turn left [Q]", Vector2i(560, 290), func(): command(Vector2i.ZERO, -1))
	make_button("Turn right [E]", Vector2i(720, 290), func(): command(Vector2i.ZERO, 1))
	make_button("Up", Vector2i(640, 355), func(): command(Vector2i.UP, 0))
	make_button("Left", Vector2i(560, 405), func(): command(Vector2i.LEFT, 0))
	make_button("Right", Vector2i(720, 405), func(): command(Vector2i.RIGHT, 0))
	make_button("Down", Vector2i(640, 455), func(): command(Vector2i.DOWN, 0))
	reset()

func make_button(caption: String, position_value: Vector2, action: Callable) -> void:
	var button := Button.new()
	button.text = caption
	button.position = position_value
	button.size = Vector2i(145, 40)
	button.pressed.connect(action)
	button.focus_mode = Control.FOCUS_NONE
	add_child(button)

func reset() -> void:
	obstacles = [Vector2i(5, 2), Vector2i(8, 6), Vector2i(8, 7), Vector2i(3, 8), Vector2i(4, 8)]
	reset_unit()

func reset_unit() -> void:
	heading = 0
	dimensions = initial_dimensions
	center = Vector2i(8, 8) + dimensions
	result = {}
	status = "Ready"
	refresh_preview()

func refresh_preview() -> void:
	preview = Motion.proposal(center, dimensions, Vector2i.ZERO, preview_turn, obstacles, heading, sweep_check)
	queue_redraw()

func command(direction: Vector2i, turn: int) -> void:
	result = Motion.proposal(center, dimensions, direction, turn, obstacles, heading, sweep_check)
	status = "Accepted" if result.accepted else ("Blocked by sweep rule; destination is clear." if result.destination_clear else "Blocked: destination is occupied or outside the board.")
	if result.accepted:
		heading = result.heading
		center = result.center
		dimensions = result.dimensions
	if turn != 0:
		preview_turn = turn
	refresh_preview()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		match event.keycode:
			KEY_W, KEY_UP: command(Vector2i.UP, 0)
			KEY_S, KEY_DOWN: command(Vector2i.DOWN, 0)
			KEY_A, KEY_LEFT: command(Vector2i.LEFT, 0)
			KEY_D, KEY_RIGHT: command(Vector2i.RIGHT, 0)
			KEY_Q: command(Vector2i.ZERO, -1)
			KEY_E: command(Vector2i.ZERO, 1)
			KEY_R: reset()
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		var mouse := Vector2i(get_global_mouse_position()) - Origin
		if mouse.x < 0 or mouse.y < 0:
			return
		var local := mouse / Cell
		if local.x < 0 or local.y < 0 or local.x >= Motion.BoardSize or local.y >= Motion.BoardSize:
			return
		var cell := local
		if obstacles.has(cell):
			obstacles.erase(cell)
		elif Motion.overlaps(center, dimensions, cell):
			status = "That cell is occupied."
			queue_redraw()
			return
		else:
			obstacles.append(cell)
		result = {}
		status = "Obstacle map updated."
		refresh_preview()

func label_at(position_value: Vector2, value: String, size_value := 18, color := Color("c2cddd")) -> void:
	draw_string(font, position_value, value, HORIZONTAL_ALIGNMENT_LEFT, -1, size_value, color)

func draw_unit(unit_center: Vector2i, footprint: Vector2i, color: Color, filled: bool) -> void:
	var rectangle := Rect2i(Origin + (unit_center - footprint) * (Cell / 2), footprint * Cell)
	if filled:
		draw_rect(rectangle, Color(color, 0.24))
	draw_rect(rectangle, color, false, 2)
	draw_circle(Origin + unit_center * (Cell / 2), 3, color)

func _draw() -> void:
	label_at(Vector2i(40, 50), "Rectangular grid motion", 30, Color.WHITE)
	label_at(Vector2i(40, 77), "WASD / arrows: move    Q / E: turn    Click a cell: toggle obstacle", 18)
	label_at(Vector2i(40, 207), Motion.SweepDescriptions[sweep_check], 18)
	draw_rect(Rect2(Origin, Vector2i.ONE * Cell * Motion.BoardSize), Color("101c2c"))
	for line in range(Motion.BoardSize + 1):
		draw_line(Origin + Vector2i(line * Cell, 0), Origin + Vector2i(line * Cell, Motion.BoardSize * Cell), Color("263548"))
		draw_line(Origin + Vector2i(0, line * Cell), Origin + Vector2i(Motion.BoardSize * Cell, line * Cell), Color("263548"))
	if not preview.is_empty():
		for cell in preview.required:
			if Motion.inside_board(cell):
				draw_rect(Rect2i(Origin + cell * Cell + Vector2i.ONE, Vector2i.ONE * (Cell - 2)), Color("51452b"))
	for cell in obstacles:
		draw_rect(Rect2(Origin + Vector2i(cell) * Cell + Vector2i.ONE * 2, Vector2i.ONE * (Cell - 4)), Color("637084"))
	if not preview.is_empty():
		for cell in preview.blocked_cells:
			draw_rect(Rect2i(Origin + cell * Cell + Vector2i.ONE * 2, Vector2i.ONE * (Cell - 4)), Color("b84e62"))
		if not preview.outside_cells.is_empty():
			draw_rect(Rect2i(Origin, Vector2i.ONE * Cell * Motion.BoardSize), Color("ff7188"), false, 3)
		# Draw only on-board destination cells, keeping overlays inside the grid.
		for cell in Motion.footprint_cells(preview.center, preview.dimensions):
			if Motion.inside_board(cell):
				draw_rect(Rect2i(Origin + cell * Cell + Vector2i.ONE * 3, Vector2i.ONE * (Cell - 6)), Color("c2cddd") if preview.accepted else Color("ff7188"), false, 2)
	draw_unit(center, dimensions, UnitColor, true)
	var forward := Motion.rotate_quarters(Vector2i.RIGHT, heading)
	var sideways := Motion.rotate_quarters(forward, 1)
	var middle := Origin + center * (Cell / 2)
	var reach := (dimensions.x if heading % 2 == 0 else dimensions.y) * Cell * 2 / 5
	var tip := middle + forward * reach
	draw_line(middle, tip, UnitColor, 3, true)
	draw_line(tip, tip - forward * 8 + sideways * 5, UnitColor, 3, true)
	draw_line(tip, tip - forward * 8 - sideways * 5, UnitColor, 3, true)
	draw_circle(Origin + Motion.pivot_for(center, dimensions, heading) * (Cell / 2), 5, Color("f2bd69"))
	label_at(Vector2i(560, 280), "Facing: " + ["Right / 3", "Down / 6", "Left / 9", "Up / 12"][heading] + " o'clock", 18)
	label_at(Vector2i(560, 255), "Center (%d.%d, %d.%d) | %d x %d" % [center.x / 2, (center.x % 2) * 5, center.y / 2, (center.y % 2) * 5, dimensions.x, dimensions.y], 20)
	label_at(Vector2i(560, 550), "Inspect next " + ("left" if preview_turn < 0 else "right") + " turn:", 17)
	label_at(Vector2i(560, 580), ("Clear" if preview.accepted else "Blocked") + " | %d required cells" % preview.required.size(), 17)
	label_at(Vector2i(560, 660), "%d blocked cells / %d outside board" % [preview.blocked_cells.size(), preview.outside_cells.size()], 17)
	label_at(Vector2i(560, 690), "Amber: required. Red: collision.", 17)
	label_at(Vector2i(40, 750), status, 20, Color.WHITE)
	label_at(Vector2i(40, 783), "Preview shows the NEXT turn. Integer rules only. Translation still checks the destination.", 17)







extends Control

const HexCell = preload("res://Scripts/HexCell.gd")

const INK := Color("#d8dbef")
const MUTED := Color("#777f9e")
const VOID := Color("#090b1d")
const PANEL := Color("#11142b")
const PANEL_RAISED := Color("#181c39")
const VIOLET := Color("#8b7cff")
const MINT := Color("#55d6c2")
const GOLD := Color("#f2c66d")
const ROSE := Color("#ef5b66")

# Legend labels and colors are presentation only. Map symbols come from Kernel glyphs.
const TERRAIN_SYMBOLS := {
	"shatteredPlain": ["·", "SHATTERED", Color("#a1adc4")],
	"ashWaste": [":", "ASH", Color("#c7aaa0")],
	"leyChannel": ["≈", "LEY", Color("#9b99ff")],
	"xenoforest": ["^", "XENO", Color("#72bd8b")],
	"fortifiedReach": ["#", "FORTIFIED", Color("#65d3cf")],
	"broodMire": ["~", "BROOD", Color("#d88bac")],
}

const FORCE_SYMBOLS := {
	"bastion": ["B", "BASTION", GOLD],
	"soldier": ["S", "SOLDIER", MINT],
	"ravener": ["r", "RAVENER", ROSE],
	"broodNode": ["N", "BROOD NODE", ROSE],
	"enclave": ["E", "ENCLAVE", MINT],
}

## Total width of an interior hex outline in board pixels; zero hides outlines.
@export_range(0.0, 12.0, 0.25) var hex_outline_width := 2.0:
	set(value):
		hex_outline_width = clampf(value, 0.0, 12.0)
		if is_instance_valid(_outline_width_slider):
			_outline_width_slider.set_value_no_signal(hex_outline_width)
		if is_instance_valid(_outline_width_value):
			_outline_width_value.text = "%0.2f px" % hex_outline_width
		for button in _cell_buttons:
			button.outline_width = hex_outline_width

var _pipe: FileAccess
var _stderr: FileAccess
var _kernel_pid := -1
var _read_buffer := ""
var _snapshot := {}
var _selected := Vector2i(0, 0)
var _cell_buttons: Array[BaseButton] = []

var _title_label: Label
var _turn_label: Label
var _seed_edit: LineEdit
var _outline_width_slider: HSlider
var _outline_width_value: Label
var _grid: Control
var _inspector: RichTextLabel
var _chronicle: RichTextLabel
var _status: Label
var _advance_button: Button
var _command_panel: VBoxContainer


func _ready() -> void:
	# Hex centers require fractional coordinates. Godot otherwise rounds each
	# Control's render transform independently, opening seams between cells.
	get_viewport().gui_snap_controls_to_pixels = false
	_build_interface()
	_start_kernel()
	get_window().min_size = Vector2i(1000, 680)


func _exit_tree() -> void:
	if _kernel_pid > 0 and OS.is_process_running(_kernel_pid):
		OS.kill(_kernel_pid)


func _process(_delta: float) -> void:
	_drain_kernel_output()


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("advance_turn") and not _seed_edit.has_focus():
		_send({"command": "advance"})
		get_viewport().set_input_as_handled()


func _build_interface() -> void:
	var backdrop := ColorRect.new()
	backdrop.color = VOID
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(backdrop)

	var margin := MarginContainer.new()
	margin.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	margin.add_theme_constant_override("margin_left", 20)
	margin.add_theme_constant_override("margin_top", 16)
	margin.add_theme_constant_override("margin_right", 20)
	margin.add_theme_constant_override("margin_bottom", 18)
	add_child(margin)

	var root := VBoxContainer.new()
	root.add_theme_constant_override("separation", 12)
	margin.add_child(root)
	root.add_child(_build_header())

	var middle := HSplitContainer.new()
	middle.size_flags_vertical = Control.SIZE_EXPAND_FILL
	middle.split_offset = 820
	middle.add_theme_constant_override("separation", 14)
	root.add_child(middle)
	middle.add_child(_build_world_panel())
	middle.add_child(_build_side_panel())
	root.add_child(_build_footer())


func _build_header() -> Control:
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel", _panel_style(PANEL, VIOLET, 1, 10))
	var margin := MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 16)
	margin.add_theme_constant_override("margin_top", 10)
	margin.add_theme_constant_override("margin_right", 12)
	margin.add_theme_constant_override("margin_bottom", 10)
	panel.add_child(margin)

	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	margin.add_child(row)

	_title_label = Label.new()
	_title_label.text = "AUTOMAPOLIS // THE BASTION FRONT"
	_title_label.add_theme_font_size_override("font_size", 24)
	_title_label.add_theme_color_override("font_color", INK)
	_title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(_title_label)

	_turn_label = Label.new()
	_turn_label.text = "TURN 000"
	_turn_label.add_theme_font_size_override("font_size", 18)
	_turn_label.add_theme_color_override("font_color", GOLD)
	row.add_child(_turn_label)

	_seed_edit = LineEdit.new()
	_seed_edit.text = "475023"
	_seed_edit.placeholder_text = "SEED"
	_seed_edit.custom_minimum_size.x = 100
	_seed_edit.add_theme_color_override("font_color", INK)
	_seed_edit.add_theme_stylebox_override("normal", _panel_style(PANEL_RAISED, MUTED, 1, 6))
	row.add_child(_seed_edit)

	var forge := Button.new()
	forge.text = "OPEN FRONT"
	forge.tooltip_text = "Create a fresh deterministic war front from this seed."
	_style_button(forge, MINT)
	forge.pressed.connect(_new_world)
	row.add_child(forge)
	return panel


func _build_world_panel() -> Control:
	var panel := PanelContainer.new()
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_theme_stylebox_override("panel", _panel_style(PANEL, Color("#282e55"), 1, 10))
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 8)
	panel.add_child(column)

	column.add_child(_build_symbol_legend())
	column.add_child(_build_outline_control())

	var scroll := ScrollContainer.new()
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	column.add_child(scroll)

	var centering := CenterContainer.new()
	centering.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	centering.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.add_child(centering)
	_grid = Control.new()
	centering.add_child(_grid)
	return panel


func _build_outline_control() -> Control:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 12)
	var label := Label.new()
	label.text = "OUTLINE WIDTH"
	label.add_theme_color_override("font_color", MUTED)
	row.add_child(label)

	_outline_width_slider = HSlider.new()
	_outline_width_slider.min_value = 0.0
	_outline_width_slider.max_value = 12.0
	_outline_width_slider.step = 0.25
	_outline_width_slider.value = hex_outline_width
	_outline_width_slider.custom_minimum_size.x = 160
	_outline_width_slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_outline_width_slider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	_outline_width_slider.tooltip_text = "Hex outline width. Set to zero to hide outlines."
	_outline_width_slider.value_changed.connect(func(value: float): hex_outline_width = value)
	row.add_child(_outline_width_slider)

	_outline_width_value = Label.new()
	_outline_width_value.custom_minimum_size.x = 72
	_outline_width_value.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_outline_width_value.add_theme_color_override("font_color", INK)
	_outline_width_value.text = "%0.2f px" % hex_outline_width
	row.add_child(_outline_width_value)
	return row


func _build_side_panel() -> Control:
	var wrapper := ScrollContainer.new()
	wrapper.custom_minimum_size.x = 330
	wrapper.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	wrapper.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	var side := VBoxContainer.new()
	side.custom_minimum_size.x = 330
	side.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	side.size_flags_vertical = Control.SIZE_EXPAND_FILL
	side.add_theme_constant_override("separation", 10)
	wrapper.add_child(side)

	_inspector = RichTextLabel.new()
	_inspector.bbcode_enabled = true
	_inspector.fit_content = false
	_inspector.custom_minimum_size.y = 220
	_inspector.add_theme_font_size_override("normal_font_size", 14)
	_inspector.add_theme_color_override("default_color", INK)
	_inspector.add_theme_stylebox_override("normal", _panel_style(PANEL, Color("#282e55"), 1, 8))
	side.add_child(_inspector)

	_command_panel = VBoxContainer.new()
	_command_panel.add_theme_constant_override("separation", 6)
	var command_heading := Label.new()
	command_heading.text = "FIELD COMMAND AUTHORITY"
	command_heading.add_theme_color_override("font_color", ROSE)
	_command_panel.add_child(command_heading)
	_add_command_button("CHANNEL +25 RESONANCE", "channel", VIOLET)
	_add_command_button("FORTIFY THE REACH", "fortify", MINT, {"terrain": "fortifiedReach"}, str(TERRAIN_SYMBOLS.fortifiedReach[0]))
	_add_command_button("DEPLOY SOLDIER", "deploy", GOLD, {"kind": "soldier"}, str(FORCE_SYMBOLS.soldier[0]))
	_add_command_button("COMMIT BASTION · IF LOST", "deploy", GOLD, {"kind": "bastion"}, str(FORCE_SYMBOLS.bastion[0]))
	_add_command_button("ESTABLISH VIGIL ANNEX", "establish", MINT, {"name": "Vigil Annex"}, str(FORCE_SYMBOLS.enclave[0]))
	_add_command_button("AUTHORIZE MAGITECH PURGE", "purge", ROSE, {"radius": 1}, str(TERRAIN_SYMBOLS.ashWaste[0]))
	side.add_child(_command_panel)

	var chronicle_heading := Label.new()
	chronicle_heading.text = "FRONT DISPATCHES"
	chronicle_heading.add_theme_color_override("font_color", VIOLET)
	side.add_child(chronicle_heading)
	_chronicle = RichTextLabel.new()
	_chronicle.bbcode_enabled = true
	_chronicle.scroll_active = true
	_chronicle.size_flags_vertical = Control.SIZE_EXPAND_FILL
	_chronicle.add_theme_font_size_override("normal_font_size", 13)
	_chronicle.add_theme_color_override("default_color", MUTED)
	_chronicle.add_theme_stylebox_override("normal", _panel_style(PANEL, Color("#282e55"), 1, 8))
	side.add_child(_chronicle)
	return wrapper


func _build_footer() -> Control:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 10)
	_status = Label.new()
	_status.text = "CONNECTING TO KERNEL…"
	_status.add_theme_color_override("font_color", MUTED)
	_status.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(_status)

	var hint := Label.new()
	hint.text = "SPACE / ENTER"
	hint.add_theme_color_override("font_color", MUTED)
	row.add_child(hint)
	_advance_button = Button.new()
	_advance_button.text = "ADVANCE THE FRONT  →"
	_advance_button.custom_minimum_size = Vector2(230, 44)
	_advance_button.add_theme_font_size_override("font_size", 16)
	_style_button(_advance_button, GOLD)
	_advance_button.pressed.connect(func(): _send({"command": "advance"}))
	row.add_child(_advance_button)
	return row


func _build_symbol_legend() -> Control:
	var legend := VBoxContainer.new()
	legend.add_theme_constant_override("separation", 4)
	for symbols in [TERRAIN_SYMBOLS, FORCE_SYMBOLS]:
		var row := HFlowContainer.new()
		row.add_theme_constant_override("h_separation", 14)
		row.add_theme_constant_override("v_separation", 4)
		for entry in symbols.values():
			var label := Label.new()
			label.text = str(entry[1]) if symbols == TERRAIN_SYMBOLS else "%s  %s" % [entry[0], entry[1]]
			label.add_theme_font_size_override("font_size", 13)
			label.add_theme_color_override("font_color", entry[2])
			var item := HBoxContainer.new()
			if symbols == TERRAIN_SYMBOLS:
				var swatch := ColorRect.new()
				swatch.custom_minimum_size = Vector2(14, 14)
				swatch.size_flags_vertical = Control.SIZE_SHRINK_CENTER
				swatch.color = PANEL_RAISED.lerp(entry[2], 0.35)
				item.add_child(swatch)
			item.add_child(label)
			row.add_child(item)
		legend.add_child(row)
	return legend


func _add_command_button(label_text: String, command: String, accent: Color, extras := {}, symbol := "") -> void:
	var button := Button.new()
	button.text = label_text if symbol.is_empty() else "%s   %s" % [symbol, label_text]
	button.alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.custom_minimum_size.y = 34
	_style_button(button, accent)
	button.pressed.connect(func():
		var payload := {"command": command, "x": _selected.x, "y": _selected.y}
		payload.merge(extras)
		_send(payload)
	)
	_command_panel.add_child(button)


func _start_kernel() -> void:
	var base_dir := OS.get_executable_path().get_base_dir()
	if OS.has_feature("editor"):
		base_dir = ProjectSettings.globalize_path("res://")
	var host_dir := base_dir.path_join("KernelHost")
	var app_host := host_dir.path_join("Automapolis.Kernel.Host.exe" if OS.get_name() == "Windows" else "Automapolis.Kernel.Host")
	var host_dll := host_dir.path_join("Automapolis.Kernel.Host.dll")
	var program := app_host
	var arguments := PackedStringArray()
	if not FileAccess.file_exists(app_host):
		program = "dotnet"
		arguments.append(host_dll)

	var process := OS.execute_with_pipe(program, arguments, false)
	if process.is_empty():
		_status.text = "KERNEL UNAVAILABLE · run Run.cmd to publish it"
		_status.add_theme_color_override("font_color", ROSE)
		_advance_button.disabled = true
		return
	_pipe = process["stdio"]
	_stderr = process["stderr"]
	_kernel_pid = process["pid"]
	_status.text = "WAR KERNEL ONLINE · awaiting front state"


func _drain_kernel_output() -> void:
	if _pipe == null:
		return
	for _attempt in range(8):
		var available := _pipe.get_length()
		if available <= 0:
			break
		var bytes := _pipe.get_buffer(mini(available, 65536))
		_read_buffer += bytes.get_string_from_utf8()
	while _read_buffer.contains("\n"):
		var newline := _read_buffer.find("\n")
		var line := _read_buffer.left(newline).strip_edges()
		_read_buffer = _read_buffer.substr(newline + 1)
		if not line.is_empty():
			_receive(line)


func _receive(line: String) -> void:
	var response = JSON.parse_string(line)
	if response == null or not response is Dictionary:
		_status.text = "KERNEL PROTOCOL ERROR"
		_status.add_theme_color_override("font_color", ROSE)
		return
	_snapshot = response.get("snapshot", {})
	_status.text = response.get("message", "World state received.")
	_status.add_theme_color_override("font_color", MINT if response.get("ok", false) else ROSE)
	_render_snapshot()


func _send(payload: Dictionary) -> void:
	if _pipe == null:
		return
	_pipe.store_line(JSON.stringify(payload))
	_pipe.flush()


func _new_world() -> void:
	var seed_value := int(_seed_edit.text) if _seed_edit.text.is_valid_int() else 475023
	_send({
		"command": "new",
		"width": 16,
		"height": 12,
		"seed": seed_value,
		"name": "The Bastion Front",
	})


func _render_snapshot() -> void:
	if _snapshot.is_empty():
		return
	_title_label.text = str(_snapshot.get("name", "AUTOMAPOLIS")).to_upper()
	_turn_label.text = "TURN %03d" % int(_snapshot.get("turn", 0))
	var width := int(_snapshot.get("width", 16))
	var height := int(_snapshot.get("height", 12))
	_grid.custom_minimum_size = Vector2((width + 0.5) * HexCell.HEX_WIDTH, (height - 1) * HexCell.ROW_STEP + 2.0 * HexCell.RADIUS)

	if _selected.x >= width or _selected.y >= height:
		_selected = Vector2i.ZERO
	for child in _grid.get_children():
		_grid.remove_child(child)
		child.queue_free()
	_cell_buttons.clear()

	var forces_by_cell := {}
	for force in _snapshot.get("forces", []):
		var key := "%d,%d" % [int(force.position.x), int(force.position.y)]
		if not forces_by_cell.has(key):
			forces_by_cell[key] = []
		forces_by_cell[key].append(force)

	var tiles: Array = _snapshot.get("tiles", [])
	for index in range(mini(tiles.size(), width * height)):
		var tile = tiles[index]
		var x := int(tile.position.x)
		var y := int(tile.position.y)
		var key := "%d,%d" % [x, y]
		var button := HexCell.new()
		var occupants: Array = forces_by_cell.get(key, [])
		var force = null
		for occupant in occupants:
			if force == null or _force_priority(occupant) > _force_priority(force):
				force = occupant
		var terrain_color := _terrain_color(str(tile.terrain))
		var background := PANEL_RAISED.lerp(terrain_color, 0.35)
		var symbol_color := terrain_color if force == null else _force_color(str(force.kind))
		button.text = "" if force == null else str(force.glyph)
		button.tooltip_text = _cell_tooltip(tile, occupants)
		button.size = Vector2(HexCell.HEX_WIDTH, 2.0 * HexCell.RADIUS)
		button.position = HexCell.cell_position(x, y)
		button.background = background
		button.outline_width = hex_outline_width
		button.symbol_color = symbol_color
		button.selected = x == _selected.x and y == _selected.y
		if occupants.size() > 1:
			_add_corner_label(button, str(occupants.size()), INK, true)
		button.pressed.connect(_select_cell.bind(Vector2i(x, y)))
		_grid.add_child(button)
		_cell_buttons.append(button)

	_render_inspector()
	_render_chronicle()


func _select_cell(point: Vector2i) -> void:
	_selected = point
	_render_snapshot()


func _render_inspector() -> void:
	var tile = _tile_at(_selected)
	if tile == null:
		return
	var text := "[color=#7e6bff][font_size=12]HEX // COLUMN %02d, ROW %02d[/font_size][/color]\n" % [_selected.x, _selected.y]
	text += "[color=#%s][font_size=22]%s  %s[/font_size][/color]\n" % [_terrain_color(str(tile.terrain)).to_html(false), str(tile.glyph), _words(str(tile.terrain)).to_upper()]
	text += "[color=#777f9e]%s[/color]\n" % str(tile.description)
	var occupants := _forces_at(_selected)
	if occupants.is_empty():
		text += "\n[color=#777f9e]No detected forces occupy this sector.[/color]"
	else:
		text += "\n[color=#4fe4c1]FORCES[/color]"
		for force in occupants:
			text += "\n[color=#%s][b]%s  %s[/b][/color] · %s · STR %d" % [_force_color(str(force.kind)).to_html(false), str(force.glyph), force.name, force.intent, int(force.strength)]
	_inspector.text = text


func _render_chronicle() -> void:
	var lines: Array = _snapshot.get("chronicle", [])
	if lines.is_empty():
		_chronicle.text = "[color=#777f9e]No dispatches have reached command.[/color]"
		return
	var text := ""
	for index in range(lines.size()):
		var color := "#d8dbef" if index == 0 else "#777f9e"
		text += "[color=%s]%s[/color]\n\n" % [color, lines[index]]
	_chronicle.text = text


func _tile_at(point: Vector2i):
	var width := int(_snapshot.get("width", 0))
	var tiles: Array = _snapshot.get("tiles", [])
	var index := point.y * width + point.x
	return tiles[index] if index >= 0 and index < tiles.size() else null


func _forces_at(point: Vector2i) -> Array:
	var result: Array = []
	for force in _snapshot.get("forces", []):
		if int(force.position.x) == point.x and int(force.position.y) == point.y:
			result.append(force)
	return result


func _cell_tooltip(tile, occupants: Array) -> String:
	var result := "%s  %s\n%s" % [str(tile.glyph), _words(str(tile.terrain)), str(tile.description)]
	for force in occupants:
		result += "\n%s  %s · %s · strength %d" % [str(force.glyph), force.name, force.intent, int(force.strength)]
	return result


func _add_corner_label(button: BaseButton, text: String, color: Color, top_right: bool) -> void:
	var label := Label.new()
	label.text = text
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	label.add_theme_font_size_override("font_size", 11)
	label.add_theme_color_override("font_color", color)
	label.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	label.offset_left = 7
	label.offset_right = -7
	label.offset_top = 8
	label.offset_bottom = -8
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT if top_right else HORIZONTAL_ALIGNMENT_LEFT
	label.vertical_alignment = VERTICAL_ALIGNMENT_TOP if top_right else VERTICAL_ALIGNMENT_BOTTOM
	button.add_child(label)


func _terrain_color(terrain: String) -> Color:
	return TERRAIN_SYMBOLS.get(terrain, ["?", "", MUTED])[2]


func _force_color(kind: String) -> Color:
	return FORCE_SYMBOLS.get(kind, ["?", "", INK])[2]


func _force_priority(force) -> int:
	return {"bastion": 5, "broodNode": 4, "enclave": 3, "ravener": 2, "soldier": 1}.get(str(force.kind), 0)


func _words(camel: String) -> String:
	var result := ""
	for character in camel:
		if character == character.to_upper() and not result.is_empty():
			result += " "
		result += character
	return result


func _style_button(button: BaseButton, accent: Color) -> void:
	button.add_theme_color_override("font_color", INK)
	button.add_theme_color_override("font_hover_color", Color.WHITE)
	button.add_theme_stylebox_override("normal", _panel_style(PANEL_RAISED, accent.darkened(0.45), 1, 6))
	button.add_theme_stylebox_override("hover", _panel_style(PANEL_RAISED.lightened(0.08), accent, 1, 6))
	button.add_theme_stylebox_override("pressed", _panel_style(PANEL_RAISED.darkened(0.12), accent, 2, 6))


func _panel_style(background: Color, border: Color, width: int, radius: int) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = border
	style.set_border_width_all(width)
	style.set_corner_radius_all(radius)
	style.content_margin_left = 10
	style.content_margin_right = 10
	style.content_margin_top = 7
	style.content_margin_bottom = 7
	return style

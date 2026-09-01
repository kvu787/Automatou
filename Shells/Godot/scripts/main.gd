extends Control

const INK := Color("#d8dbef")
const MUTED := Color("#777f9e")
const VOID := Color("#090b1d")
const PANEL := Color("#11142b")
const PANEL_RAISED := Color("#181c39")
const VIOLET := Color("#7e6bff")
const MINT := Color("#4fe4c1")
const GOLD := Color("#f4d47c")
const ROSE := Color("#ef6da8")

const TERRAIN_SPRITE_IDS := {
	"starGlass": "star_glass",
	"ashDunes": "ash_dunes",
	"aetherSea": "aether_sea",
	"crystalForest": "crystal_forest",
	"ironSteppe": "iron_steppe",
	"dreamMarsh": "dream_marsh",
}

const BEING_SPRITE_IDS := {
	"wanderer": "wanderer",
	"synthBeast": "synth_beast",
	"oracle": "oracle",
	"settlement": "settlement",
	"rift": "rift",
}

var _pipe: FileAccess
var _stderr: FileAccess
var _kernel_pid := -1
var _read_buffer := ""
var _snapshot := {}
var _selected := Vector2i(0, 0)
var _cell_buttons: Array[Button] = []

var _title_label: Label
var _turn_label: Label
var _mode_picker: OptionButton
var _seed_edit: LineEdit
var _grid: GridContainer
var _inspector: RichTextLabel
var _chronicle: RichTextLabel
var _status: Label
var _advance_button: Button
var _creator_panel: VBoxContainer


func _ready() -> void:
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
	_title_label.text = "AUTOMAPOLIS"
	_title_label.add_theme_font_size_override("font_size", 24)
	_title_label.add_theme_color_override("font_color", INK)
	_title_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	row.add_child(_title_label)

	_turn_label = Label.new()
	_turn_label.text = "TURN 000"
	_turn_label.add_theme_font_size_override("font_size", 18)
	_turn_label.add_theme_color_override("font_color", GOLD)
	row.add_child(_turn_label)

	_mode_picker = OptionButton.new()
	_mode_picker.add_item("OBSERVER · 0 PLAYER")
	_mode_picker.add_item("CREATOR · 1 PLAYER")
	_mode_picker.selected = 1
	_mode_picker.tooltip_text = "Mode takes effect when a new world is forged."
	_style_button(_mode_picker, VIOLET)
	row.add_child(_mode_picker)

	_seed_edit = LineEdit.new()
	_seed_edit.text = "475023"
	_seed_edit.placeholder_text = "SEED"
	_seed_edit.custom_minimum_size.x = 100
	_seed_edit.add_theme_color_override("font_color", INK)
	_seed_edit.add_theme_stylebox_override("normal", _panel_style(PANEL_RAISED, MUTED, 1, 6))
	row.add_child(_seed_edit)

	var forge := Button.new()
	forge.text = "REFORGE"
	forge.tooltip_text = "Create a fresh deterministic world from this mode and seed."
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

	column.add_child(_build_sprite_legend())

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
	_grid = GridContainer.new()
	_grid.add_theme_constant_override("h_separation", 3)
	_grid.add_theme_constant_override("v_separation", 3)
	centering.add_child(_grid)
	return panel


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
	_inspector.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	_inspector.fit_content = false
	_inspector.custom_minimum_size.y = 155
	_inspector.add_theme_font_size_override("normal_font_size", 14)
	_inspector.add_theme_color_override("default_color", INK)
	_inspector.add_theme_stylebox_override("normal", _panel_style(PANEL, Color("#282e55"), 1, 8))
	side.add_child(_inspector)

	_creator_panel = VBoxContainer.new()
	_creator_panel.add_theme_constant_override("separation", 6)
	var creator_heading := Label.new()
	creator_heading.text = "CREATOR INSTRUMENTS"
	creator_heading.add_theme_color_override("font_color", ROSE)
	_creator_panel.add_child(creator_heading)
	_add_creator_button("INFUSE +25 AETHER", "infuse", VIOLET)
	_add_creator_button("GROW CRYSTAL FOREST", "transmute", MINT, {"terrain": "crystalForest"}, "crystal_forest")
	_add_creator_button("SHAPE A WANDERER", "create_life", GOLD, {"kind": "wanderer"}, "wanderer")
	_add_creator_button("SHAPE AN ORACLE", "create_life", GOLD, {"kind": "oracle"}, "oracle")
	_add_creator_button("FOUND A LANTERN-CITY", "found", MINT, {"name": "Lantern Annex"}, "settlement")
	_add_creator_button("INVOKE LOCAL CATACLYSM", "cataclysm", ROSE, {"radius": 1}, "rift")
	side.add_child(_creator_panel)

	var chronicle_heading := Label.new()
	chronicle_heading.text = "WORLD CHRONICLE"
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
	_advance_button.text = "RESOLVE NEXT TURN  →"
	_advance_button.custom_minimum_size = Vector2(230, 44)
	_advance_button.add_theme_font_size_override("font_size", 16)
	_style_button(_advance_button, GOLD)
	_advance_button.pressed.connect(func(): _send({"command": "advance"}))
	row.add_child(_advance_button)
	return row


func _build_sprite_legend() -> Control:
	var legend := HBoxContainer.new()
	legend.add_theme_constant_override("separation", 11)
	var entries := [
		["starGlass", "STAR-GLASS"],
		["ashDunes", "ASH"],
		["aetherSea", "AETHER"],
		["crystalForest", "CRYSTAL"],
		["ironSteppe", "IRON"],
		["dreamMarsh", "DREAM"],
	]
	for entry in entries:
		var item := HBoxContainer.new()
		item.add_theme_constant_override("separation", 4)
		var sprite := TextureRect.new()
		sprite.texture = load(_terrain_sprite_path(entry[0]))
		sprite.custom_minimum_size = Vector2(20, 20)
		sprite.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		sprite.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
		item.add_child(sprite)
		var label := Label.new()
		label.text = entry[1]
		label.add_theme_font_size_override("font_size", 11)
		label.add_theme_color_override("font_color", MUTED)
		item.add_child(label)
		legend.add_child(item)
	return legend


func _add_creator_button(label_text: String, command: String, accent: Color, extras := {}, sprite_id := "") -> void:
	var button := Button.new()
	button.text = label_text
	button.alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.custom_minimum_size.y = 34
	button.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	if not sprite_id.is_empty():
		button.icon = load(_sprite_path(sprite_id))
		button.add_theme_constant_override("icon_max_width", 22)
	_style_button(button, accent)
	button.pressed.connect(func():
		var payload := {"command": command, "x": _selected.x, "y": _selected.y}
		payload.merge(extras)
		_send(payload)
	)
	_creator_panel.add_child(button)


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
	_status.text = "KERNEL ONLINE · awaiting world state"


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
		"mode": "observer" if _mode_picker.selected == 0 else "creator",
		"name": "Automapolis",
	})


func _render_snapshot() -> void:
	if _snapshot.is_empty():
		return
	_title_label.text = str(_snapshot.get("name", "AUTOMAPOLIS")).to_upper()
	_turn_label.text = "TURN %03d" % int(_snapshot.get("turn", 0))
	var width := int(_snapshot.get("width", 16))
	var height := int(_snapshot.get("height", 12))
	_grid.columns = width

	if _selected.x >= width or _selected.y >= height:
		_selected = Vector2i.ZERO
	for child in _grid.get_children():
		child.queue_free()
	_cell_buttons.clear()

	var beings_by_cell := {}
	for being in _snapshot.get("beings", []):
		var key := "%d,%d" % [int(being.position.x), int(being.position.y)]
		if not beings_by_cell.has(key) or _being_priority(being) > _being_priority(beings_by_cell[key]):
			beings_by_cell[key] = being

	var tiles: Array = _snapshot.get("tiles", [])
	for index in range(mini(tiles.size(), width * height)):
		var tile = tiles[index]
		var x := index % width
		var y := index / width
		var key := "%d,%d" % [x, y]
		var button := Button.new()
		var being = beings_by_cell.get(key)
		button.text = ""
		button.tooltip_text = _cell_tooltip(tile, being)
		button.custom_minimum_size = Vector2(36, 36)
		button.clip_contents = true
		button.add_theme_stylebox_override("normal", _cell_style(PANEL_RAISED, x == _selected.x and y == _selected.y))
		button.add_theme_stylebox_override("hover", _cell_style(PANEL_RAISED.lightened(0.12), true))
		button.add_theme_stylebox_override("pressed", _cell_style(PANEL_RAISED.darkened(0.12), true))
		_add_sprite_layer(button, _terrain_sprite_path(str(tile.terrain)))
		if being != null:
			_add_sprite_layer(button, _being_sprite_path(str(being.kind)))
		button.pressed.connect(_select_cell.bind(Vector2i(x, y)))
		_grid.add_child(button)
		_cell_buttons.append(button)

	var mode := str(_snapshot.get("mode", "creator"))
	_creator_panel.visible = mode == "creator"
	_render_inspector()
	_render_chronicle()


func _select_cell(point: Vector2i) -> void:
	_selected = point
	_render_snapshot()


func _render_inspector() -> void:
	var tile = _tile_at(_selected)
	if tile == null:
		return
	var text := "[color=#7e6bff][font_size=12]SELECTED // %02d,%02d[/font_size][/color]\n" % [_selected.x, _selected.y]
	text += "[img=48x48]%s[/img]  [font_size=22]%s[/font_size]\n" % [_terrain_sprite_path(str(tile.terrain)), _words(str(tile.terrain)).to_upper()]
	text += "[color=#777f9e]%s[/color]\n" % str(tile.description)
	var occupants := _beings_at(_selected)
	if occupants.is_empty():
		text += "\n[color=#777f9e]No named beings occupy this cell.[/color]"
	else:
		text += "\n[color=#4fe4c1]OCCUPANTS[/color]"
		for being in occupants:
			text += "\n[img=24x24]%s[/img]  [b]%s[/b] · %s" % [_being_sprite_path(str(being.kind)), being.name, being.intent]
	_inspector.text = text


func _render_chronicle() -> void:
	var lines: Array = _snapshot.get("chronicle", [])
	if lines.is_empty():
		_chronicle.text = "[color=#777f9e]The world has not spoken yet.[/color]"
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


func _beings_at(point: Vector2i) -> Array:
	var result: Array = []
	for being in _snapshot.get("beings", []):
		if int(being.position.x) == point.x and int(being.position.y) == point.y:
			result.append(being)
	return result


func _cell_tooltip(tile, being) -> String:
	var result := str(tile.description)
	if being != null:
		result += "\n%s · %s" % [being.name, being.intent]
	return result


func _add_sprite_layer(button: Button, path: String) -> void:
	var sprite := TextureRect.new()
	sprite.texture = load(path)
	sprite.mouse_filter = Control.MOUSE_FILTER_IGNORE
	sprite.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	sprite.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	sprite.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	sprite.offset_left = 2
	sprite.offset_top = 2
	sprite.offset_right = -2
	sprite.offset_bottom = -2
	button.add_child(sprite)


func _sprite_path(sprite_id: String) -> String:
	return "res://assets/sprites/generated/%s.png" % sprite_id


func _terrain_sprite_path(terrain: String) -> String:
	return _sprite_path(TERRAIN_SPRITE_IDS.get(terrain, "star_glass"))


func _being_sprite_path(kind: String) -> String:
	return _sprite_path(BEING_SPRITE_IDS.get(kind, "wanderer"))


func _being_priority(being) -> int:
	return {"rift": 5, "settlement": 4, "oracle": 3, "synthBeast": 2, "wanderer": 1}.get(str(being.kind), 0)


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


func _cell_style(background: Color, selected: bool) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = background
	style.border_color = GOLD if selected else background.lightened(0.16)
	style.set_border_width_all(2 if selected else 1)
	style.set_corner_radius_all(5)
	return style

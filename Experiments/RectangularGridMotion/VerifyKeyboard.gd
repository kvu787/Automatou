extends SceneTree
var failures := 0
func check(condition: bool, description: String) -> void:
	if not condition:
		push_error(description)
		failures += 1
func _initialize() -> void:
	verify.call_deferred()
func verify() -> void:
	var scene = load("res://Main.tscn").instantiate()
	root.add_child(scene)
	await process_frame
	var selector: OptionButton = scene.get_node("SweepSelector")
	check(selector.selected == 0 and scene.sweep_check == 0, "Sweep defaults to Off")
	for mode in range(selector.item_count):
		var before: Vector2i = scene.center
		selector.select(mode)
		selector.item_selected.emit(mode)
		check(scene.center == before, "Changing sweep rule preserves position")
		scene.obstacles.clear()
		scene.reset_unit()
		var original: Vector2i = scene.center
		for key in [KEY_W, KEY_S, KEY_A, KEY_D, KEY_Q, KEY_E]:
			var event := InputEventKey.new()
			event.keycode = key
			event.pressed = true
			Input.parse_input_event(event)
			await process_frame
			check(not scene.result.is_empty() and scene.result.accepted, "Keyboard command accepted in mode %d key %d" % [mode, key])
			event = InputEventKey.new()
			event.keycode = key
			event.pressed = false
			Input.parse_input_event(event)
			await process_frame
		check(scene.center == original and scene.heading == 0, "Opposite commands restore pose")
		scene.reset_unit()
		scene.obstacles = [Vector2i(5, 5)]
		scene.refresh_preview()
		var expected: bool = scene.preview.accepted
		var original_heading: int = scene.heading
		scene.command(Vector2i.ZERO, 1)
		check(scene.result.accepted == expected, "Displayed preview matches actual turn decision")
		if not expected:
			check(scene.center == original and scene.heading == original_heading, "Rejected sweep preserves full unit pose")
	print("Keyboard verification: %d failures" % failures)
	quit(1 if failures else 0)

extends SceneTree
var failures := 0
func _initialize() -> void:
	verify.call_deferred()
func verify() -> void:
	var scene = load("res://Main.tscn").instantiate()
	root.add_child(scene)
	await process_frame
	var selector: OptionButton = scene.get_child(0)
	for approach in [1, 2]:
		selector.select(approach)
		selector.item_selected.emit(approach)
		scene.obstacles.clear()
		scene.reset_unit()
		var original: Vector2 = scene.center
		for key in [KEY_W, KEY_S, KEY_A, KEY_D, KEY_Q, KEY_E]:
			var event := InputEventKey.new()
			event.keycode = key
			event.pressed = true
			Input.parse_input_event(event)
			await process_frame
			if scene.result.is_empty() or not scene.result.accepted:
				push_error("Keyboard command failed for approach %d key %d" % [approach, key])
				failures += 1
			event = InputEventKey.new()
			event.keycode = key
			event.pressed = false
			Input.parse_input_event(event)
			await process_frame
		if not scene.center.is_equal_approx(original) or scene.heading != 0:
			push_error("Opposite keyboard commands did not restore pose")
			failures += 1
	print("Keyboard verification: %d failures" % failures)
	quit(1 if failures else 0)

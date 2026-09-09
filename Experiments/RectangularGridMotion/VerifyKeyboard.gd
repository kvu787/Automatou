extends SceneTree
var failures := 0
func _initialize() -> void:
	verify.call_deferred()
func verify() -> void:
	var scene = load("res://Main.tscn").instantiate()
	root.add_child(scene)
	await process_frame
	for child in scene.get_children():
		if child is OptionButton:
			push_error("Unexpected select box")
			failures += 1
	scene.obstacles.clear()
	scene.reset_unit()
	var original: Vector2i = scene.center
	for key in [KEY_W, KEY_S, KEY_A, KEY_D, KEY_Q, KEY_E]:
		var event := InputEventKey.new()
		event.keycode = key
		event.pressed = true
		Input.parse_input_event(event)
		await process_frame
		if scene.result.is_empty() or not scene.result.accepted:
			push_error("Keyboard command failed: %d" % key)
			failures += 1
		event = InputEventKey.new()
		event.keycode = key
		event.pressed = false
		Input.parse_input_event(event)
		await process_frame
	if scene.center != original or scene.heading != 0:
		push_error("Opposite commands did not restore pose")
		failures += 1
	print("Keyboard verification: %d failures" % failures)
	quit(1 if failures else 0)

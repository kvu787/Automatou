extends SceneTree

var failures := 0

func check(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func wait_for(shell: Control, turn: int, width: int, mode: String) -> void:
    for attempt in range(400):
        if not shell._snapshot.is_empty() and int(shell._snapshot.turn) == turn and int(shell._snapshot.width) == width and str(shell._snapshot.mode) == mode:
            return
        await create_timer(0.01).timeout
    check(false, "Timed out waiting for Kernel response")

func _initialize() -> void:
    run.call_deferred()

func run() -> void:
    var shell = load("res://Main.tscn").instantiate()
    root.add_child(shell)
    await wait_for(shell, 0, 16, "command")
    if shell._snapshot.is_empty():
        shell.queue_free()
        await process_frame
        quit(1)
        return
    check(shell._snapshot.topology == "hexagonal", "Missing hex topology")
    check(shell._cell_buttons.size() == 192, "Expected all hex cells")
    var first = shell._cell_buttons[0]
    var odd = shell._cell_buttons[16]
    check(is_equal_approx(odd.position.x - first.position.x, first.HEX_WIDTH / 2.0), "Odd row not staggered")
    check(first._has_point(Vector2(first.HEX_WIDTH / 2.0, first.RADIUS)), "Hex center not clickable")
    check(not first._has_point(Vector2.ZERO), "Empty corner incorrectly clickable")
    # The lower-right hex occupies a corner of the first cell's bounding box.
    var overlap := Vector2(first.HEX_WIDTH - 2.0, 2.0 * first.RADIUS - 2.0)
    check(not first._has_point(overlap), "Bounding-box corner steals neighbor clicks")
    check(odd._has_point(first.position + overlap - odd.position), "Neighbor does not own corner")
    await process_frame
    await process_frame
    var click_position: Vector2 = first.global_position + overlap
    var motion := InputEventMouseMotion.new()
    motion.position = click_position
    root.push_input(motion, true)
    for pressed in [true, false]:
        var click := InputEventMouseButton.new()
        click.position = click_position
        click.button_index = MOUSE_BUTTON_LEFT
        click.pressed = pressed
        root.push_input(click, true)
        await process_frame
    check(shell._selected == Vector2i(0, 1), "Actual click did not select adjacent hex")
    shell._select_cell(Vector2i(3, 3))
    await process_frame
    check(shell._inspector.text.contains("COLUMN 03, ROW 03"), "Inspector selection mismatch")
    check(shell._cell_buttons[51].selected, "Selected hex not highlighted")
    shell._send({"command": "advance"})
    await wait_for(shell, 1, 16, "command")
    shell._send({"command": "new", "width": 6, "height": 8, "mode": "witness"})
    await wait_for(shell, 0, 6, "witness")
    check(shell._cell_buttons.size() == 48, "Grid resize failed")
    check(not shell._command_panel.visible, "Witness commands still visible")
    shell._send({"command": "new", "width": 16, "height": 12, "mode": "command"})
    await wait_for(shell, 0, 16, "command")
    await process_frame
    if "--capture" in OS.get_cmdline_user_args():
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("res://Build/HexGridPreview.png")
    print("Hex shell smoke: %d failures" % failures)
    shell.queue_free()
    await process_frame
    quit(failures)

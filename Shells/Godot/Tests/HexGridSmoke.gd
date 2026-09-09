extends SceneTree

var failures := 0

func check(condition: bool, message: String) -> void:
    if not condition:
        failures += 1
        push_error(message)

func wait_for(shell: Control, turn: int, width: int) -> void:
    for attempt in range(400):
        if not shell._snapshot.is_empty() and int(shell._snapshot.turn) == turn and int(shell._snapshot.width) == width:
            return
        await create_timer(0.01).timeout
    check(false, "Timed out waiting for Kernel response")

func _initialize() -> void:
    run.call_deferred()

func run() -> void:
    var shell = load("res://Main.tscn").instantiate()
    root.add_child(shell)
    await wait_for(shell, 0, 16)
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
    # Both row parities must share two exact vertices with all six neighbors.
    for center_index in [33, 49]:
        var center_cell = shell._cell_buttons[center_index]
        var neighbors := 0
        for candidate in shell._cell_buttons:
            if candidate == center_cell:
                continue
            var shared_vertices := 0
            for vertex in center_cell.polygon():
                for other_vertex in candidate.polygon():
                    if (center_cell.position + vertex).is_equal_approx(candidate.position + other_vertex):
                        shared_vertices += 1
            if shared_vertices == 2:
                neighbors += 1
        check(neighbors == 6, "Hex does not meet all six neighbors edge to edge")
    var original_polygon: PackedVector2Array = first.polygon()
    for outline_width in [0.0, 4.0, 12.0]:
        shell.hex_outline_width = outline_width
        for cell in shell._cell_buttons:
            check(is_equal_approx(cell.outline_width, outline_width), "Outline setting did not reach cell")
        check(first.polygon() == original_polygon, "Outline width changed cell geometry")
    shell.hex_outline_width = 2.0
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
    await wait_for(shell, 1, 16)
    shell._send({"command": "new", "width": 6, "height": 8})
    await wait_for(shell, 0, 6)
    check(shell._cell_buttons.size() == 48, "Grid resize failed")
    check(shell._command_panel.visible, "Player commands must remain available")
    shell._send({"command": "new", "width": 16, "height": 12})
    await wait_for(shell, 0, 16)
    shell._send({"command": "establish", "x": 2, "y": 2, "name": "Smoke Annex"})
    var established := false
    for attempt in range(400):
        for force in shell._snapshot.get("forces", []):
            if force.name == "Smoke Annex":
                established = true
        if established:
            break
        await create_timer(0.01).timeout
    check(established, "Player intervention was not accepted")
    check(not shell._snapshot.has("mode"), "Obsolete mode field remains")
    await process_frame
    if "--capture" in OS.get_cmdline_user_args():
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("res://Build/HexGridPreview.png")
    print("Hex shell smoke: %d failures" % failures)
    shell.queue_free()
    await process_frame
    quit(failures)

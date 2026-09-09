extends BaseButton

const RADIUS := 25.0
const HEX_WIDTH := 43.3012701892
const ROW_STEP := 37.5

var text := ""
var background := Color("#181c39")
var symbol_color := Color.WHITE
var selected := false
var outline_width := 2.0:
    set(value):
        outline_width = clampf(value, 0.0, HEX_WIDTH)
        queue_redraw()

func _ready() -> void:
    get_tree().root.size_changed.connect(queue_redraw)
    mouse_entered.connect(queue_redraw)
    mouse_exited.connect(queue_redraw)
    focus_entered.connect(queue_redraw)
    focus_exited.connect(queue_redraw)
    button_down.connect(queue_redraw)
    button_up.connect(queue_redraw)

static func cell_position(column: int, row: int) -> Vector2:
    return Vector2((column + 0.5 * (row & 1)) * HEX_WIDTH, row * ROW_STEP)

func polygon(inset: float = 0.0) -> PackedVector2Array:
    # Exact shared vertices keep both parity rows flush, including hit testing.
    var points := PackedVector2Array([
        Vector2(HEX_WIDTH / 2.0, 0.0),
        Vector2(HEX_WIDTH, RADIUS / 2.0),
        Vector2(HEX_WIDTH, RADIUS * 1.5),
        Vector2(HEX_WIDTH / 2.0, RADIUS * 2.0),
        Vector2(0.0, RADIUS * 1.5),
        Vector2(0.0, RADIUS / 2.0),
    ])
    var center := Vector2(HEX_WIDTH / 2.0, RADIUS)
    var factor := maxf(0.0, 1.0 - inset / (HEX_WIDTH / 2.0))
    for corner in range(6):
        points[corner] = center + (points[corner] - center) * factor
    return points

func _has_point(point: Vector2) -> bool:
    return Geometry2D.is_point_in_polygon(point, polygon())

func _draw() -> void:
    var points := polygon()
    var fill := background
    if is_hovered():
        fill = fill.lightened(0.15)
    if is_pressed():
        fill = fill.darkened(0.12)
    var border := Color("#f2c66d") if selected else Color("#73768c")
    if is_hovered() and not selected:
        border = Color("#55d6c2")
    if outline_width > 0.0:
        # Each neighbor contributes half the shared outline, entirely inside
        # its own hex. No overlapping strokes or draw-order-dependent widths.
        _draw_smooth_polygon(points, border)
        if outline_width < HEX_WIDTH:
            _draw_smooth_polygon(polygon(outline_width / 2.0), fill)
    else:
        _draw_smooth_polygon(points, fill)
    if has_focus():
        draw_circle(Vector2(HEX_WIDTH / 2.0, 8.0), 2.0, Color.WHITE)
    var font := get_theme_font("font", "Button")
    var text_size := font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 22)
    draw_string(font, Vector2((HEX_WIDTH - text_size.x) / 2.0, RADIUS + 7.0), text,
        HORIZONTAL_ALIGNMENT_LEFT, -1, 22, symbol_color)

func _draw_smooth_polygon(points: PackedVector2Array, color: Color) -> void:
    # Keep the opaque, tessellated fill so antialiasing cannot expose seams.
    # Godot's Compatibility renderer supports antialiased lines, not 2D MSAA.
    draw_colored_polygon(points, color)
    var edge := points.duplicate()
    edge.append(edge[0])
    var transform := get_viewport().get_stretch_transform() * get_global_transform_with_canvas()
    var pixel_width := 1.0 / maxf(transform.get_scale().x, 0.001)
    draw_polyline(edge, color, pixel_width, true)

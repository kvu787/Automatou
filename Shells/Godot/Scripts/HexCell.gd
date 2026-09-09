extends BaseButton

const RADIUS := 25.0
const HEX_WIDTH := 43.3012701892
const ROW_STEP := 37.5

var text := ""
var background := Color("#181c39")
var symbol_color := Color.WHITE
var selected := false

func _ready() -> void:
    mouse_entered.connect(queue_redraw)
    mouse_exited.connect(queue_redraw)
    focus_entered.connect(queue_redraw)
    focus_exited.connect(queue_redraw)
    button_down.connect(queue_redraw)
    button_up.connect(queue_redraw)

static func cell_position(column: int, row: int) -> Vector2:
    return Vector2((column + 0.5 * (row & 1)) * HEX_WIDTH, row * ROW_STEP)

func polygon() -> PackedVector2Array:
    var points := PackedVector2Array()
    var center := Vector2(HEX_WIDTH / 2.0, RADIUS)
    for corner in range(6):
        var angle := deg_to_rad(-90.0 + corner * 60.0)
        points.append(center + Vector2(cos(angle), sin(angle)) * (RADIUS - 1.0))
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
    draw_colored_polygon(points, fill)
    points.append(points[0])
    var border := Color("#f2c66d") if selected else background.lightened(0.25)
    if is_hovered() and not selected:
        border = Color("#55d6c2")
    draw_polyline(points, border, 2.0 if selected or is_hovered() else 1.0, true)
    if has_focus():
        draw_circle(Vector2(HEX_WIDTH / 2.0, 8.0), 2.0, Color.WHITE)
    var font := get_theme_font("font", "Button")
    var text_size := font.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 22)
    draw_string(font, Vector2((HEX_WIDTH - text_size.x) / 2.0, RADIUS + 7.0), text,
        HORIZONTAL_ALIGNMENT_LEFT, -1, 22, symbol_color)

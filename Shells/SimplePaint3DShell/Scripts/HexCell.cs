using Godot;

namespace SimplePaint3DShell;

public partial class HexCell : BaseButton
{
    public const float Radius = 25.0f;
    public const float HexWidth = 43.3012701892f;
    public const float RowStep = 37.5f;
    public const float HighlightWidth = 2.0f;

    public string Text { get; set; } = "";
    public Color Background { get; set; } = new("#181c39");
    public Color SymbolColor { get; set; } = Colors.White;
    public bool Selected { get; set; }

    private float _outlineWidth = 2.0f;
    public float OutlineWidth
    {
        get => _outlineWidth;
        set
        {
            _outlineWidth = Mathf.Clamp(value, 0.0f, HexWidth);
            QueueRedraw();
        }
    }

    public override void _Ready()
    {
        GetTree().Root.SizeChanged += QueueRedraw;
        MouseEntered += QueueRedraw;
        MouseExited += QueueRedraw;
        FocusEntered += QueueRedraw;
        FocusExited += QueueRedraw;
        ButtonDown += QueueRedraw;
        ButtonUp += QueueRedraw;
    }

    public override void _ExitTree() => GetTree().Root.SizeChanged -= QueueRedraw;

    public static Vector2 CellPosition(int column, int row) =>
        new((column + 0.5f * (row & 1)) * HexWidth, row * RowStep);

    public Vector2[] Polygon(float inset = 0.0f)
    {
        // Exact shared vertices keep both parity rows flush, including hit testing.
        Vector2[] points =
        [
            new(HexWidth / 2.0f, 0), new(HexWidth, Radius / 2.0f),
            new(HexWidth, Radius * 1.5f), new(HexWidth / 2.0f, Radius * 2.0f),
            new(0, Radius * 1.5f), new(0, Radius / 2.0f)
        ];
        var center = new Vector2(HexWidth / 2.0f, Radius);
        var factor = Mathf.Max(0.0f, 1.0f - inset / (HexWidth / 2.0f));
        for (var corner = 0; corner < 6; corner++)
        {
            points[corner] = center + (points[corner] - center) * factor;
        }
        return points;
    }

    public override bool _HasPoint(Vector2 point) => Geometry2D.IsPointInPolygon(point, Polygon());

    public override void _Draw()
    {
        var points = Polygon();
        var fill = Background;
        if (IsHovered()) fill = fill.Lightened(0.15f);
        if (IsPressed()) fill = fill.Darkened(0.12f);
        // Delineation keeps its own color, including between highlighted cells.
        var inset = OutlineWidth / 2.0f;
        if (OutlineWidth > 0.0f)
        {
            DrawSmoothPolygon(points, new Color("#73768c"));
            DrawInwardPolygon(inset, fill);
        }
        else
        {
            DrawSmoothPolygon(points, fill);
        }
        if (Selected || IsHovered())
        {
            var highlight = new Color(Selected ? "#f2c66d" : "#55d6c2");
            // Both antialiased edges stay inside the terrain, at equal perpendicular insets.
            DrawInwardPolygon(inset, highlight);
            DrawInwardPolygon(inset + HighlightWidth, fill);
        }
        if (HasFocus()) DrawCircle(new Vector2(HexWidth / 2.0f, 8), 2, Colors.White);
        var font = GetThemeFont("font", "Button");
        var textSize = font.GetStringSize(Text, HorizontalAlignment.Left, -1, 22);
        DrawString(font, new Vector2((HexWidth - textSize.X) / 2.0f, Radius + 7), Text,
            HorizontalAlignment.Left, -1, 22, SymbolColor);
    }

    private void DrawSmoothPolygon(Vector2[] points, Color color)
    {
        // Opaque tessellated fills prevent seams; edge strokes smooth the exterior.
        DrawColoredPolygon(points, color);
        Vector2[] edge = [.. points, points[0]];
        var transform = GetViewport().GetStretchTransform() * GetGlobalTransformWithCanvas();
        var pixelWidth = 1.0f / Mathf.Max(transform.Scale.X, 0.001f);
        DrawPolyline(edge, color, pixelWidth, true);
    }

    private void DrawInwardPolygon(float inset, Color color)
    {
        if (inset >= HexWidth / 2.0f) return;
        var transform = GetViewport().GetStretchTransform() * GetGlobalTransformWithCanvas();
        var feather = Mathf.Min(1.0f / Mathf.Max(transform.Scale.X, 0.001f), HexWidth / 2.0f - inset);
        var outer = Polygon(inset);
        var inner = Polygon(inset + feather);
        var transparent = new Color(color, 0.0f);
        // Vertex alpha feathers inward so smoothing never spills onto the delineation.
        for (var corner = 0; corner < 6; corner++)
        {
            var next = (corner + 1) % 6;
            DrawPolygon([outer[corner], outer[next], inner[next], inner[corner]],
                [transparent, transparent, color, color]);
        }
        if (inset + feather < HexWidth / 2.0f) DrawColoredPolygon(inner, color);
    }
}

using Godot;

namespace SimplePaint3DShell;

public partial class HexCell : BaseButton {
    public const float Radius = 25.0f;
    public const float HexWidth = 43.3012701892f;
    public const float RowStep = 37.5f;
    public const float HighlightWidth = 2.0f;

    public string Text { get; set; } = "";
    public Color Background { get; set; } = new("#181c39");
    public Color SymbolColor { get; set; } = Colors.White;
    public bool Selected { get; set; }

    public float OutlineWidth {
        get;
        set {
            field = Mathf.Clamp(value, 0.0f, HexWidth);
            this.QueueRedraw();
        }
    } = 2.0f;

    public override void _Ready() {
        this.GetTree().Root.SizeChanged += this.QueueRedraw;
        MouseEntered += this.QueueRedraw;
        MouseExited += this.QueueRedraw;
        FocusEntered += this.QueueRedraw;
        FocusExited += this.QueueRedraw;
        ButtonDown += this.QueueRedraw;
        ButtonUp += this.QueueRedraw;
    }

    public override void _ExitTree() {
        this.GetTree().Root.SizeChanged -= this.QueueRedraw;
    }

    public static Vector2 CellPosition(int column, int row) {
        return new((column + (0.5f * (row & 1))) * HexWidth, row * RowStep);
    }

    public static Vector2[] Polygon(float inset = 0.0f) {
        // Exact shared vertices keep both parity rows flush, including hit testing.
        Vector2[] points =
        [
            new(HexWidth / 2.0f, 0), new(HexWidth, Radius / 2.0f),
            new(HexWidth, Radius * 1.5f), new(HexWidth / 2.0f, Radius * 2.0f),
            new(0, Radius * 1.5f), new(0, Radius / 2.0f)
        ];
        Vector2 center = new(HexWidth / 2.0f, Radius);
        float factor = Mathf.Max(0.0f, 1.0f - (inset / (HexWidth / 2.0f)));
        for (int corner = 0; corner < 6; corner++) {
            points[corner] = center + ((points[corner] - center) * factor);
        }
        return points;
    }

    public override bool _HasPoint(Vector2 point) {
        return Geometry2D.IsPointInPolygon(point, Polygon());
    }

    public override void _Draw() {
        Vector2[] points = Polygon();
        Color fill = this.Background;
        if (this.IsHovered()) {
            fill = fill.Lightened(0.15f);
        }

        if (this.IsPressed()) {
            fill = fill.Darkened(0.12f);
        }
        // Delineation keeps its own color, including between highlighted cells.
        float inset = this.OutlineWidth / 2.0f;
        if (this.OutlineWidth > 0.0f) {
            this.DrawSmoothPolygon(points, new Color("#73768c"));
            this.DrawInwardPolygon(inset, fill);
        } else {
            this.DrawSmoothPolygon(points, fill);
        }
        if (this.Selected || this.IsHovered()) {
            Color highlight = new(this.Selected ? "#f2c66d" : "#55d6c2");
            // Both antialiased edges stay inside the terrain, at equal perpendicular insets.
            this.DrawInwardPolygon(inset, highlight);
            this.DrawInwardPolygon(inset + HighlightWidth, fill);
        }
        if (this.HasFocus()) {
            this.DrawCircle(new Vector2(HexWidth / 2.0f, 8), 2, Colors.White);
        }

        Font font = this.GetThemeFont("font", "Button");
        Vector2 textSize = font.GetStringSize(this.Text, HorizontalAlignment.Left, -1, 22);
        this.DrawString(font, new Vector2((HexWidth - textSize.X) / 2.0f, Radius + 7), this.Text,
            HorizontalAlignment.Left, -1, 22, this.SymbolColor);
    }

    private void DrawSmoothPolygon(Vector2[] points, Color color) {
        // Opaque tessellated fills prevent seams; edge strokes smooth the exterior.
        this.DrawColoredPolygon(points, color);
        Vector2[] edge = [.. points, points[0]];
        Transform2D transform = this.GetViewport().GetStretchTransform() * this.GetGlobalTransformWithCanvas();
        float pixelWidth = 1.0f / Mathf.Max(transform.Scale.X, 0.001f);
        this.DrawPolyline(edge, color, pixelWidth, true);
    }

    private void DrawInwardPolygon(float inset, Color color) {
        if (inset >= HexWidth / 2.0f) {
            return;
        }

        Transform2D transform = this.GetViewport().GetStretchTransform() * this.GetGlobalTransformWithCanvas();
        float feather = Mathf.Min(1.0f / Mathf.Max(transform.Scale.X, 0.001f), (HexWidth / 2.0f) - inset);
        Vector2[] outer = Polygon(inset);
        Vector2[] inner = Polygon(inset + feather);
        Color transparent = new(color, 0.0f);
        // Vertex alpha feathers inward so smoothing never spills onto the delineation.
        for (int corner = 0; corner < 6; corner++) {
            int next = (corner + 1) % 6;
            this.DrawPolygon([outer[corner], outer[next], inner[next], inner[corner]],
                [transparent, transparent, color, color]);
        }
        if (inset + feather < HexWidth / 2.0f) {
            this.DrawColoredPolygon(inner, color);
        }
    }
}

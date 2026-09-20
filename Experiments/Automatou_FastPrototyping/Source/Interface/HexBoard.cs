using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class HexBoard : Control
{
    public World World { get; set; } = null!;
    public Entity? Selected { get; set; }
    public Func<Hex, Entity?>? Preview { get; set; }
    public Action<Hex, MouseButton>? CellPressed { get; set; }
    public Action<Hex>? HoverChanged { get; set; }
    public bool Coordinates { get; set; }
    public bool DecisionOverlay { get; set; } = true;
    public bool DimUnseenEnemies { get; set; }
    public Hex? Hovered { get; private set; }
    public float Zoom { get; private set; } = 1;
    private Vector2 pan;
    private bool panning;
    private bool painting;
    private Hex? lastPainted;
    private float effectTime;
    private const float Radius = 22;
    private static readonly Color Ink = new("a3b3bc");
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        Resized += () => { if (Size.X > 0) Fit(); };
    }
    public static Vector2 Center(Hex cell) => new((float)(Math.Sqrt(3) * Radius * (cell.Q + cell.R * .5)), -Radius * 1.5f * cell.R);
    public Vector2 Screen(Hex cell) => Center(cell) * Zoom + pan;
    private Hex Hit(Vector2 point)
    {
        Vector2 world = (point - pan) / Zoom;
        double r = -world.Y / (Radius * 1.5), q = world.X / (Math.Sqrt(3) * Radius) - r / 2;
        double s = -q - r;
        int rq = (int)Math.Round(q), rr = (int)Math.Round(r), rs = (int)Math.Round(s);
        double dq = Math.Abs(rq - q), dr = Math.Abs(rr - r), ds = Math.Abs(rs - s);
        if (dq > dr && dq > ds) rq = -rr - rs; else if (dr > ds) rr = -rq - rs;
        return new(rq, rr);
    }
    private IEnumerable<Hex> DisplayCells() => World.Terrain.Keys;
    public void Fit()
    {
        if (World is null || Size.X < 1 || Size.Y < 1) return;
        var points = DisplayCells().Select(Center).ToArray();
        if (points.Length == 0) return;
        Vector2 low = new(points.Min(p => p.X) - Radius, points.Min(p => p.Y) - Radius);
        Vector2 high = new(points.Max(p => p.X) + Radius, points.Max(p => p.Y) + Radius);
        Zoom = Math.Clamp(Math.Min((Size.X - 75) / (high.X - low.X), (Size.Y - 100) / (high.Y - low.Y)), .12f, 2.8f);
        pan = Size / 2 - (low + high) / 2 * Zoom + new Vector2(0, 12);
        QueueRedraw();
    }
    public override void _GuiInput(InputEvent input)
    {
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Middle) panning = mouse.Pressed;
            if (mouse.ButtonIndex == MouseButton.Left) { painting = mouse.Pressed; lastPainted = null; }
            if (!mouse.Pressed) return;
            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            {
                float old = Zoom;
                Zoom = Math.Clamp(Zoom * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.13f : 1 / 1.13f), .12f, 4);
                pan = mouse.Position - (mouse.Position - pan) * (Zoom / old);
                QueueRedraw();
            }
            else if (mouse.ButtonIndex is MouseButton.Left or MouseButton.Right)
            {
                lastPainted = Hit(mouse.Position); CellPressed?.Invoke(lastPainted.Value, mouse.ButtonIndex); QueueRedraw();
            }
        }
        else if (input is InputEventMouseMotion motion)
        {
            if (panning && (motion.ButtonMask & MouseButtonMask.Middle) != 0) pan += motion.Relative;
            else panning = false;
            var cell = Hit(motion.Position);
            Hovered = cell; HoverChanged?.Invoke(cell);
            if (painting && (motion.ButtonMask & MouseButtonMask.Left) != 0 && lastPainted != cell)
            {
                lastPainted = cell; CellPressed?.Invoke(cell, MouseButton.Left);
            }
            QueueRedraw();
        }
    }
    public void Flash() { effectTime = .65f; QueueRedraw(); }
    public override void _Process(double delta)
    {
        if (effectTime <= 0) return;
        effectTime -= (float)delta; QueueRedraw();
    }
    private static Vector2[] Polygon(Vector2 center, float radius) => Enumerable.Range(0, 6)
        .Select(i => center + Vector2.FromAngle(Mathf.DegToRad(30 + i * 60)) * radius).ToArray();
    private void Hexagon(Hex cell, float inset, Color fill, Color? stroke = null, float width = 1)
    {
        var points = Polygon(Screen(cell), Math.Max(1, Radius * Zoom - inset));
        DrawColoredPolygon(points, fill);
        if (stroke is { } color) DrawPolyline([.. points, points[0]], color, width, true);
    }
    private void Text(Vector2 position, string text, int size, Color color) => DrawString(ThemeDB.FallbackFont, position, text, HorizontalAlignment.Left, -1, size, color);
    public override void _Draw()
    {
        if (World is null) return;
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("0b141c"));
        foreach (var cell in DisplayCells())
        {
            var position = Screen(cell);
            if (position.X < -50 || position.Y < -50 || position.X > Size.X + 50 || position.Y > Size.Y + 50) continue;
            Color fill = new(Catalog.TerrainColors[(int)World.Terrain[cell]]);
            Hexagon(cell, .7f, fill);
            if (Zoom > .65f) TerrainMark(cell, World.Terrain[cell]);
            if (Coordinates && Zoom > .85f) Text(position + new Vector2(-14, 4), $"{cell.X},{cell.Y}", 10, new Color("9eb2ab"));
        }

        if (Selected?.Unit is { } design)
            foreach (var cell in Hex.Disk(design.Range + design.Size).Select(c => c + Selected.Position))
                if (World.Terrain.ContainsKey(cell) && Hex.TurnDistance(Selected.Facing, Selected.Position.DirectionTo(cell)) <= 1)
                    Hexagon(cell, 1, new Color(.96f, .78f, .42f, .1f));
        foreach (var entity in World.Entities) DrawEntity(entity, false);
        if (DecisionOverlay && Selected is not null) DrawDecisionOverlay(Selected);
        if (Hovered is { } hovered && Preview?.Invoke(hovered) is { } preview) DrawEntity(preview, true);
        if (effectTime > 0)
            foreach (var effect in World.Effects)
            {
                Color color = effect.Hit ? new("f3c66b") : new("c0dae5"); color.A = Math.Clamp(effectTime * 2, 0, 1);
                DrawLine(Screen(effect.From), Screen(effect.To), color, 2, true);
                DrawCircle(Screen(effect.To), 9 + (1 - effectTime) * 13, color, false, 2, true);
            }
        if (Hovered is { } hover) Hexagon(hover, 1, new Color(1, 1, 1, .04f), new Color("d6eceb"), 1.5f);
        DrawRect(new Rect2(0, 0, Size.X, 48), new Color(.043f, .078f, .11f, .95f));
        Text(new Vector2(20, 29), "THE OBSERVATORY", 14, new Color("9bc0c9"));
        Text(new Vector2(Size.X - 158, 29), $"{Zoom * 100:0}%   ·   +Y ↑  +X →", 12, Ink);
        Text(new Vector2(18, Size.Y - 18), "Middle drag: pan · Wheel: zoom · F: frame", 12, Ink);
    }

    private void DrawDecisionOverlay(Entity entity)
    {
        Color memoryColor = new("87d9cc"), bondColor = new("eab0dc");
        foreach (var contact in entity.Unit.Brain.State.Contacts)
        {
            Vector2 point = Screen(contact.Position);
            DrawCircle(point, Math.Max(7, Radius * Zoom * .8f), memoryColor, false, 1.5f, true);
            if (Zoom > .4f) Text(point + new Vector2(7, -8), $"#{contact.Id} T{contact.LastSeenTurn}", 11, memoryColor);
        }
        if (entity.Unit.Brain.State.Destination is { } destination)
        {
            Vector2 point = Screen(destination);
            DrawDashedLine(Screen(entity.Position), point, memoryColor, 1.5f, 6, true);
            DrawPolyline([point + new Vector2(0, -9), point + new Vector2(9, 0), point + new Vector2(0, 9), point + new Vector2(-9, 0), point + new Vector2(0, -9)], memoryColor, 2, true);
        }
        if (entity.BondedUnitId is { } bond && World.Entities.FirstOrDefault(candidate => candidate.Id == bond) is { } ally)
        {
            DrawDashedLine(Screen(entity.Position), Screen(ally.Position), bondColor, 2, 9, true);
            DrawCircle(Screen(ally.Position), Math.Max(11, Radius * Zoom), bondColor, false, 2, true);
        }
    }
    private void TerrainMark(Hex cell, Terrain terrain)
    {
        Vector2 p = Screen(cell); float s = Zoom * 5;
        Color ink = new(1, 1, 1, .16f);
        if (terrain is Terrain.Forest or Terrain.Mountain)
            DrawPolyline([p + new Vector2(-s, s), p + new Vector2(0, -s), p + new Vector2(s, s)], ink, 1, true);
        if (terrain is Terrain.Water or Terrain.Wetlands) DrawLine(p - new Vector2(s, 0), p + new Vector2(s, 0), ink, 1, true);
        if (terrain == Terrain.ExclusionZone)
        {
            DrawLine(p - new Vector2(s, s), p + new Vector2(s, s), ink, 1, true);
            DrawLine(p - new Vector2(s, -s), p + new Vector2(s, -s), ink, 1, true);
        }
    }
    private void DrawEntity(Entity entity, bool preview)
    {
        Color color = new(Catalog.FactionColors[(int)entity.Faction]);
        if (!preview && DimUnseenEnemies && Selected is { } observer && entity.Faction != observer.Faction && !World.CanObserve(observer, entity))
            color.A = .22f;
        bool valid = !preview || World.CanOccupy(entity, entity.Position, entity.Facing, out _);
        if (!valid) color = new("f27676");
        foreach (var cell in entity.OccupiedCells())
        {
            Color fill = color.Darkened(.5f); if (preview) fill.A = .55f;
            Hexagon(cell, 2 * Zoom, fill, color, entity == Selected ? 2.5f : 1.3f);
        }
        Vector2 center = Screen(entity.Position);
        Vector2 forward = (Center(Hex.Directions[entity.Facing])).Normalized();
        Vector2 side = forward.Orthogonal(); float s = Math.Clamp(8 * Zoom, 3, 11);
        DrawColoredPolygon([center + forward * s, center - forward * s * .7f + side * s * .7f, center - forward * s * .7f - side * s * .7f], color);
        if (entity.Stationary) DrawCircle(center, s * 1.5f, color, false, 2);
        if (!preview && entity.Health < entity.MaximumHealth)
        {
            Vector2 start = center + new Vector2(-12, 14) * Zoom;
            DrawLine(start, start + new Vector2(24 * Zoom, 0), new Color("101b23"), 3);
            DrawLine(start, start + new Vector2(24 * Zoom * entity.Health / entity.MaximumHealth, 0), color, 3);
        }
        if (entity == Selected) DrawArc(center, Radius * Zoom * entity.Unit.Size + 4, 0, Mathf.Tau, 48, new Color("ffffff"), 1, true);
    }
}

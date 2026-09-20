using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class HexBoard : Control {
    public World World { get; set; } = null!;
    public Entity? Selected { get; set; }
    public Func<Hex, Entity?>? Preview { get; set; }
    public Action<Hex, MouseButton>? CellPressed { get; set; }
    public Action<Hex>? HoverChanged { get; set; }
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
    public override void _Ready() {
        this.MouseFilter = MouseFilterEnum.Stop;
        this.ClipContents = true;
        Resized += () => { if (this.Size.X > 0) { this.Fit(); } };
    }
    public static Vector2 Center(Hex cell) {
        return new((float)(Math.Sqrt(3) * Radius * (cell.Q + (cell.R * .5))), -Radius * 1.5f * cell.R);
    }

    public Vector2 Screen(Hex cell) {
        return (Center(cell) * this.Zoom) + this.pan;
    }

    private Hex Hit(Vector2 point) {
        Vector2 world = (point - this.pan) / this.Zoom;
        double r = -world.Y / (Radius * 1.5), q = (world.X / (Math.Sqrt(3) * Radius)) - (r / 2);
        double s = -q - r;
        int rq = (int)Math.Round(q), rr = (int)Math.Round(r), rs = (int)Math.Round(s);
        double dq = Math.Abs(rq - q), dr = Math.Abs(rr - r), ds = Math.Abs(rs - s);
        if (dq > dr && dq > ds) {
            rq = -rr - rs;
        } else if (dr > ds) {
            rr = -rq - rs;
        }

        return new(rq, rr);
    }
    private Dictionary<Hex, Terrain>.KeyCollection DisplayCells() {
        return this.World.Terrain.Keys;
    }

    public void Fit() {
        if (this.World is null || this.Size.X < 1 || this.Size.Y < 1) {
            return;
        }

        Vector2[] points = this.DisplayCells().Select(Center).ToArray();
        if (points.Length == 0) {
            return;
        }

        Vector2 low = new(points.Min(p => p.X) - Radius, points.Min(p => p.Y) - Radius);
        Vector2 high = new(points.Max(p => p.X) + Radius, points.Max(p => p.Y) + Radius);
        this.Zoom = Math.Clamp(Math.Min((this.Size.X - 75) / (high.X - low.X), (this.Size.Y - 100) / (high.Y - low.Y)), .12f, 2.8f);
        this.pan = (this.Size / 2) - ((low + high) / 2 * this.Zoom) + new Vector2(0, 12);
        this.QueueRedraw();
    }
    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouse) {
            if (mouse.ButtonIndex == MouseButton.Middle) {
                this.panning = mouse.Pressed;
            }

            if (mouse.ButtonIndex == MouseButton.Left) { this.painting = mouse.Pressed; this.lastPainted = null; }
            if (!mouse.Pressed) {
                return;
            }

            if (mouse.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown) {
                float old = this.Zoom;
                this.Zoom = Math.Clamp(this.Zoom * (mouse.ButtonIndex == MouseButton.WheelUp ? 1.13f : 1 / 1.13f), .12f, 4);
                this.pan = mouse.Position - ((mouse.Position - this.pan) * (this.Zoom / old));
                this.QueueRedraw();
            } else if (mouse.ButtonIndex is MouseButton.Left or MouseButton.Right) {
                this.lastPainted = this.Hit(mouse.Position); this.CellPressed?.Invoke(this.lastPainted.Value, mouse.ButtonIndex); this.QueueRedraw();
            }
        } else if (@event is InputEventMouseMotion motion) {
            if (this.panning && (motion.ButtonMask & MouseButtonMask.Middle) != 0) {
                this.pan += motion.Relative;
            } else {
                this.panning = false;
            }

            Hex cell = this.Hit(motion.Position);
            this.Hovered = cell; this.HoverChanged?.Invoke(cell);
            if (this.painting && (motion.ButtonMask & MouseButtonMask.Left) != 0 && this.lastPainted != cell) {
                this.lastPainted = cell; this.CellPressed?.Invoke(cell, MouseButton.Left);
            }
            this.QueueRedraw();
        }
    }
    public void Flash() { this.effectTime = .65f; this.QueueRedraw(); }
    public override void _Process(double delta) {
        if (this.effectTime <= 0) {
            return;
        }

        this.effectTime -= (float)delta; this.QueueRedraw();
    }
    private static Vector2[] Polygon(Vector2 center, float radius) {
        return [.. Enumerable.Range(0, 6).Select(i => center + (Vector2.FromAngle(Mathf.DegToRad(30 + (i * 60))) * radius))];
    }

    private void Hexagon(Hex cell, float inset, Color fill, Color? stroke = null, float width = 1) {
        Vector2[] points = Polygon(this.Screen(cell), Math.Max(1, (Radius * this.Zoom) - inset));
        this.DrawColoredPolygon(points, fill);
        if (stroke is { } color) {
            this.DrawPolyline([.. points, points[0]], color, width, true);
        }
    }
    private void Text(Vector2 position, string text, int size, Color color) {
        this.DrawString(ThemeDB.FallbackFont, position, text, HorizontalAlignment.Left, -1, size, color);
    }

    public override void _Draw() {
        if (this.World is null) {
            return;
        }

        this.DrawRect(new Rect2(Vector2.Zero, this.Size), new Color("0b141c"));
        foreach (Hex cell in this.DisplayCells()) {
            Vector2 position = this.Screen(cell);
            if (position.X < -50 || position.Y < -50 || position.X > this.Size.X + 50 || position.Y > this.Size.Y + 50) {
                continue;
            }

            Color fill = new(Catalog.TerrainColors[(int)this.World.Terrain[cell]]);
            this.Hexagon(cell, .7f, fill);
        }

        if (this.Selected?.Unit is { } design) {
            foreach (Hex cell in Hex.Disk(design.Range + design.Size).Select(c => c + this.Selected.Position)) {
                if (this.World.Terrain.ContainsKey(cell) && Hex.TurnDistance(this.Selected.Facing, this.Selected.Position.DirectionTo(cell)) <= 1) {
                    this.Hexagon(cell, 1, new Color(.96f, .78f, .42f, .1f));
                }
            }
        }

        foreach (Entity entity in this.World.Entities) {
            this.DrawEntity(entity, false);
        }

        if (this.DecisionOverlay && this.Selected is not null) {
            this.DrawDecisionOverlay(this.Selected);
        }

        if (this.Hovered is { } hovered && this.Preview?.Invoke(hovered) is { } preview) {
            this.DrawEntity(preview, true);
        }

        if (this.effectTime > 0) {
            foreach (BattleEffect effect in this.World.Effects) {
                Color color = effect.Hit ? new("f3c66b") : new("c0dae5"); color.A = Math.Clamp(this.effectTime * 2, 0, 1);
                this.DrawLine(this.Screen(effect.From), this.Screen(effect.To), color, 2, true);
                this.DrawCircle(this.Screen(effect.To), 9 + ((1 - this.effectTime) * 13), color, false, 2, true);
            }
        }

        if (this.Hovered is { } hover) {
            this.Hexagon(hover, 1, new Color(1, 1, 1, .04f), new Color("d6eceb"), 1.5f);
        }

        this.DrawRect(new Rect2(0, 0, this.Size.X, 48), new Color(.043f, .078f, .11f, .95f));
        this.Text(new Vector2(this.Size.X - 158, 29), $"{this.Zoom * 100:0}%   ·   +Y ↑  +X →", 12, Ink);
        this.Text(new Vector2(18, this.Size.Y - 18), "Middle drag: pan · Wheel: zoom · F: frame", 12, Ink);
    }

    private void DrawDecisionOverlay(Entity entity) {
        Color memoryColor = new("87d9cc"), bondColor = new("eab0dc");
        foreach (ContactMemory contact in entity.Unit.Brain.State.Contacts) {
            Vector2 point = this.Screen(contact.Position);
            this.DrawCircle(point, Math.Max(7, Radius * this.Zoom * .8f), memoryColor, false, 1.5f, true);
            if (this.Zoom > .4f) {
                this.Text(point + new Vector2(7, -8), $"#{contact.Id} T{contact.LastSeenTurn}", 11, memoryColor);
            }
        }
        if (entity.Unit.Brain.State.Destination is { } destination) {
            Vector2 point = this.Screen(destination);
            this.DrawDashedLine(this.Screen(entity.Position), point, memoryColor, 1.5f, 6, true);
            this.DrawPolyline([point + new Vector2(0, -9), point + new Vector2(9, 0), point + new Vector2(0, 9), point + new Vector2(-9, 0), point + new Vector2(0, -9)], memoryColor, 2, true);
        }
        if (entity.BondedUnitId is { } bond && this.World.Entities.FirstOrDefault(candidate => candidate.Id == bond) is { } ally) {
            this.DrawDashedLine(this.Screen(entity.Position), this.Screen(ally.Position), bondColor, 2, 9, true);
            this.DrawCircle(this.Screen(ally.Position), Math.Max(11, Radius * this.Zoom), bondColor, false, 2, true);
        }
    }
    private void DrawEntity(Entity entity, bool preview) {
        Color color = new(Catalog.FactionColors[(int)entity.Faction]);
        if (!preview && this.DimUnseenEnemies && this.Selected is { } observer && entity.Faction != observer.Faction && !this.World.CanObserve(observer, entity)) {
            color.A = .22f;
        }

        bool valid = !preview || this.World.CanOccupy(entity, entity.Position, out _);
        if (!valid) {
            color = new("f27676");
        }

        foreach (Hex cell in entity.OccupiedCells()) {
            Color fill = color.Darkened(.5f); if (preview) {
                fill.A = .55f;
            }

            this.Hexagon(cell, 2 * this.Zoom, fill, color, entity == this.Selected ? 2.5f : 1.3f);
        }
        Vector2 center = this.Screen(entity.Position);
        Vector2 forward = Center(Hex.Directions[entity.Facing]).Normalized();
        Vector2 side = forward.Orthogonal(); float s = Math.Clamp(8 * this.Zoom, 3, 11);
        this.DrawColoredPolygon([center + (forward * s), center - (forward * s * .7f) + (side * s * .7f), center - (forward * s * .7f) - (side * s * .7f)], color);
        if (entity.Stationary) {
            this.DrawCircle(center, s * 1.5f, color, false, 2);
        }

        if (!preview && entity.Health < entity.MaximumHealth) {
            Vector2 start = center + (new Vector2(-12, 14) * this.Zoom);
            this.DrawLine(start, start + new Vector2(24 * this.Zoom, 0), new Color("101b23"), 3);
            this.DrawLine(start, start + new Vector2(24 * this.Zoom * entity.Health / entity.MaximumHealth, 0), color, 3);
        }
        if (entity == this.Selected) {
            this.DrawArc(center, (Radius * this.Zoom * entity.Unit.Size) + 4, 0, Mathf.Tau, 48, new Color("ffffff"), 1, true);
        }
    }
}

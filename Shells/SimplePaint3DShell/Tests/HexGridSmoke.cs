using Godot;
using System.Diagnostics;
using System.Globalization;

namespace SimplePaint3DShell;

public partial class HexGridSmoke : Node {
    private int _failures;
    private int _checks;

    public override async void _Ready() {
        Main? shell = null;
        try {
            shell = GD.Load<PackedScene>("res://Main.tscn").Instantiate<Main>();
            this.AddChild(shell);
            await this.WaitFor(() => shell.Snapshot is { Turn: 0, Width: 16 }, "Initial Kernel response");
            await this.CheckInterface(shell);
            await this.CheckProtocol(shell);
            string[] arguments = OS.GetCmdlineUserArgs();
            if (arguments.Contains("--render-check")) {
                if (DisplayServer.GetName() == "headless") {
                    throw new InvalidOperationException("Render checks require a real renderer.");
                }

                await this.CheckRenderedGrid(shell);
                await this.CheckHighlightBoundaries(shell);
            }
            if (arguments.Contains("--capture")) {
                _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image image = this.GetTree().Root.GetTexture().GetImage();
                SaveImage(image, "HexGridPreview.png");
            }
        } catch (Exception exception) {
            this.Check(false, exception.ToString());
        } finally {
            shell?.QueueFree();
            await this.Frame();
            GD.Print($"SimplePaint3DShell smoke: {this._checks} checks, {this._failures} failures");
            this.GetTree().Quit(this._failures == 0 ? 0 : 1);
        }
    }

    private void Check(bool condition, string message) {
        this._checks++;
        if (condition) {
            return;
        }

        this._failures++;
        GD.PushError(message);
    }

    private async Task WaitFor(Func<bool> condition, string description) {
        Stopwatch timer = Stopwatch.StartNew();
        while (!condition()) {
            if (timer.Elapsed > TimeSpan.FromSeconds(8)) {
                throw new TimeoutException($"Timed out: {description}");
            }

            _ = await this.ToSignal(this.GetTree().CreateTimer(0.01), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private async Task Frame() {
        _ = await this.ToSignal(this.GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private async Task CheckInterface(Main shell) {
        this.Check(Engine.GetVersionInfo()["major"].AsInt32() == 4 && Engine.GetVersionInfo()["minor"].AsInt32() == 7 && Engine.GetVersionInfo()["patch"].AsInt32() == 2, "Tests require Godot 4.7.2");
        this.Check(ProjectSettings.GetSetting("application/config/name").AsString() == "SimplePaint3DShell", "Project name mismatch");
        this.Check(shell.Snapshot!.Topology == "hexagonal", "Missing hex topology");
        this.Check(shell.Snapshot.Coordinates == "oddRowOffset", "Missing offset coordinate contract");
        this.Check(shell.CellButtons.Count == 192, "Expected all hex cells");
        HexCell first = shell.CellButtons[0];
        HexCell odd = shell.CellButtons[16];
        this.Check(Mathf.IsEqualApprox(odd.Position.X - first.Position.X, HexCell.HexWidth / 2), "Odd row not staggered");
        // Both row parities must share two exact vertices with all six neighbors.
        foreach (int centerIndex in new[] { 33, 49 }) {
            HexCell center = shell.CellButtons[centerIndex];
            int neighbors = 0;
            foreach (HexCell candidate in shell.CellButtons) {
                if (candidate == center) {
                    continue;
                }

                int shared = 0;
                foreach (Vector2 vertex in HexCell.Polygon()) {
                    foreach (Vector2 other in HexCell.Polygon()) {
                        if ((center.Position + vertex).IsEqualApprox(candidate.Position + other)) {
                            shared++;
                        }
                    }
                }

                if (shared == 2) {
                    neighbors++;
                }
            }
            this.Check(neighbors == 6, "Hex does not meet all six neighbors edge to edge");
        }
        Vector2[] originalPolygon = HexCell.Polygon();
        foreach (float width in new[] { 0.0f, 4.0f, 12.0f }) {
            shell.OutlineWidthSlider.Value = width;
            this.Check(Mathf.IsEqualApprox(shell.HexOutlineWidth, width), "Slider did not update outline width");
            this.Check(shell.OutlineWidthValue.Text == width.ToString("F2", CultureInfo.InvariantCulture) + " px", "Outline readout mismatch");
            this.Check(shell.CellButtons.All(cell => Mathf.IsEqualApprox(cell.OutlineWidth, width)), "Outline setting did not reach every cell");
            this.Check(HexCell.Polygon().SequenceEqual(originalPolygon), "Outline width changed cell geometry");
        }
        shell.HexOutlineWidth = 4;
        this.Check(Mathf.IsEqualApprox(shell.OutlineWidthSlider.Value, 4), "Slider did not follow programmatic width");
        this.Check(first._HasPoint(new Vector2(HexCell.HexWidth / 2, HexCell.Radius)), "Hex center not clickable");
        this.Check(!first._HasPoint(Vector2.Zero), "Empty corner incorrectly clickable");
        Vector2 overlap = new(HexCell.HexWidth - 2, (2 * HexCell.Radius) - 2);
        this.Check(!first._HasPoint(overlap), "Bounding-box corner steals neighbor clicks");
        this.Check(odd._HasPoint(first.Position + overlap - odd.Position), "Neighbor does not own corner");
        await this.Frame();
        await this.Frame();
        Vector2 clickPosition = first.GlobalPosition + overlap;
        this.GetTree().Root.PushInput(new InputEventMouseMotion { Position = clickPosition }, true);
        foreach (bool pressed in new[] { true, false }) {
            this.GetTree().Root.PushInput(new InputEventMouseButton {
                Position = clickPosition, ButtonIndex = MouseButton.Left, Pressed = pressed
            }, true);
            await this.Frame();
        }
        this.Check(shell.Selected == new Vector2I(0, 1), "Actual click did not select adjacent hex");
        shell.SelectCell(new Vector2I(3, 3));
        await this.Frame();
        this.Check(shell.Inspector.Text.Contains("COLUMN 03, ROW 03"), "Inspector selection mismatch");
        this.Check(shell.CellButtons[51].Selected, "Selected hex not highlighted");
        this.Check(shell.CommandPanel.Visible, "Player commands must remain available");
        this.Check(shell.CommandPanel.GetChildren().OfType<Button>().Count() == 6, "Missing field commands");
        foreach (CellSnapshot tile in shell.Snapshot.Tiles) {
            HexCell cell = shell.CellButtons[(tile.Position.Y * shell.Snapshot.Width) + tile.Position.X];
            ForceSnapshot[] occupants = shell.Snapshot.Forces.Where(force => force.Position == tile.Position).ToArray();
            this.Check(occupants.Length == 0 ? cell.Text == "" : occupants.Any(force => force.Glyph == cell.Text),
                "Map must display Kernel force glyphs and color-only empty terrain");
            if (occupants.Length > 1) {
                this.Check(cell.GetChildren().OfType<Label>().Any(label => label.Text == occupants.Length.ToString(CultureInfo.InvariantCulture)),
                    "Stacked forces need an occupant count");
            }
        }
    }

    private async Task SendAndWait(Main shell, object payload) {
        int previousCount = shell.ResponseCount;
        shell.Send(payload);
        await this.WaitFor(() => shell.ResponseCount > previousCount, $"Kernel response to {payload}");
    }

    private async Task CheckProtocol(Main shell) {
        await this.SendAndWait(shell, new { command = "advance" });
        this.Check(shell.Snapshot!.Turn == 1, "Advance must resolve exactly one turn");
        shell.SelectCell(new Vector2I(15, 11));
        await this.SendAndWait(shell, new { command = "new", width = 6, height = 8 });
        this.Check(shell.Snapshot is { Width: 6, Height: 8, Turn: 0 } && shell.CellButtons.Count == 48, "Grid resize failed");
        this.Check(shell.Selected == Vector2I.Zero, "Selection outside resized map must reset");
        this.Check(shell.CellButtons.All(cell => Mathf.IsEqualApprox(cell.OutlineWidth, 4)), "Outline width lost after grid rebuild");
        shell.HexOutlineWidth = 2;
        await this.SendAndWait(shell, new { command = "new", width = 16, height = 12 });
        const string annex = "Vigil é 漢字 🚀 [Annex]";
        await this.SendAndWait(shell, new { command = "establish", x = 2, y = 2, name = annex });
        this.Check(shell.LastResponseAccepted && shell.Snapshot!.Forces.Any(force => force.Name == annex), "Unicode intervention failed to round-trip");
        shell.SelectCell(new Vector2I(2, 2));
        this.Check(shell.Inspector.GetParsedText().Contains(annex), "Inspector changed a force name containing markup brackets");
        await this.SendAndWait(shell, new { command = "unknown" });
        this.Check(!shell.LastResponseAccepted && shell.Snapshot!.Turn == 0, "Rejected command changed the world or lost its error status");
        await this.SendAndWait(shell, new { command = "advance" });
        this.Check(shell.LastResponseAccepted && shell.Snapshot!.Turn == 1, "Connection did not survive a rejected command");
        int previousCount = shell.ResponseCount;
        shell.SeedEdit.Text = "9007199254740993";
        shell.NewWorld();
        await this.WaitFor(() => shell.ResponseCount > previousCount, "64-bit seed response");
        this.Check(shell.Snapshot is { Seed: 9007199254740993, Turn: 0 }, "64-bit seed was rounded or truncated");
        previousCount = shell.ResponseCount;
        shell.SeedEdit.Text = "invalid";
        shell.NewWorld();
        await this.WaitFor(() => shell.ResponseCount > previousCount, "Fallback seed response");
        this.Check(shell.Snapshot!.Seed == 475023, "Invalid seed fallback changed");
        shell.SeedEdit.GrabFocus();
        this.PushKey(Key.Enter);
        _ = await this.ToSignal(this.GetTree().CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
        this.Check(shell.Snapshot.Turn == 0, "Editing the seed advanced a turn");
        shell.SeedEdit.ReleaseFocus();
        foreach (Key key in new[] { Key.Space, Key.Enter }) {
            int turn = shell.Snapshot.Turn;
            this.PushKey(key);
            await this.WaitFor(() => shell.Snapshot.Turn == turn + 1, $"Advance hotkey {key}");
        }
        previousCount = shell.ResponseCount;
        _ = shell.CommandPanel.GetChildren().OfType<Button>().First().EmitSignal(BaseButton.SignalName.Pressed);
        await this.WaitFor(() => shell.ResponseCount > previousCount, "Channel button response");
        this.Check(shell.LastResponseAccepted && shell.Snapshot.Turn == 2, "Field command button must intervene without advancing time");
        // Keep the original deterministic rendering fixture for raster regression checks.
        await this.SendAndWait(shell, new { command = "new", width = 16, height = 12 });
        await this.SendAndWait(shell, new { command = "establish", x = 2, y = 2, name = "Smoke Annex" });
        shell.SelectCell(new Vector2I(3, 3));
        await this.Frame();
    }

    private void PushKey(Key key) {
        foreach (bool pressed in new[] { true, false }) {
            this.GetTree().Root.PushInput(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed }, true);
        }
    }

    // Headless dummy rendering cannot read pixels. Use -- --render-check with a real renderer.
    private async Task CheckRenderedGrid(Main shell) {
        Window root = this.GetTree().Root;
        foreach (Vector2I windowSize in new[] { new Vector2I(1280, 800), new Vector2I(1600, 1000), new Vector2I(2560, 1392) }) {
            root.Size = windowSize;
            foreach (float width in new[] { 0.0f, 0.25f, 2.0f, 8.5f, 12.0f }) {
                shell.HexOutlineWidth = width;
                await this.Frame();
                await this.Frame();
                _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image rendered = root.GetTexture().GetImage();
                HexCell first = shell.CellButtons[0];
                Transform2D transform = root.GetStretchTransform() * first.GetGlobalTransformWithCanvas();
                Vector2 start = transform * new Vector2(HexCell.HexWidth, HexCell.Radius * 2);
                Vector2 finish = transform * new Vector2(HexCell.HexWidth * 15, HexCell.RowStep * 10);
                this.Check(start.X >= 0 && start.Y >= 0 && finish.X < rendered.GetWidth() && finish.Y < rendered.GetHeight(), "Raster sample falls outside viewport");
                int gaps = 0;
                for (int y = Mathf.CeilToInt(start.Y); y < Math.Min(Mathf.FloorToInt(finish.Y), rendered.GetHeight()); y++) {
                    for (int x = Mathf.CeilToInt(start.X); x < Math.Min(Mathf.FloorToInt(finish.X), rendered.GetWidth()); x++) {
                        Color pixel = rendered.GetPixel(x, y);
                        if (pixel.R < 0.15f && pixel.G < 0.15f && pixel.B < 0.25f) {
                            gaps++;
                        }
                    }
                }

                this.Check(gaps == 0, $"{gaps} background pixels inside grid at {windowSize}, outline {width:F2}");
                if (width == 8.5f) {
                    SaveImage(rendered, $"HexGridOutline{windowSize.X}.png");
                }
            }
        }
        shell.HexOutlineWidth = 2;
    }

    private async Task CheckHighlightBoundaries(Main shell) {
        Window root = this.GetTree().Root;
        HexCell selected = shell.CellButtons[51];
        HexCell hovered = shell.CellButtons[52];
        foreach (float width in new[] { 2.0f, 8.5f, 12.0f }) {
            shell.HexOutlineWidth = width;
            selected.Selected = false;
            selected.QueueRedraw();
            root.PushInput(new InputEventMouseMotion { Position = Vector2.One }, true);
            await this.Frame();
            _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image baseline = root.GetTexture().GetImage();
            selected.Selected = true;
            selected.QueueRedraw();
            root.PushInput(new InputEventMouseMotion {
                Position = hovered.GlobalPosition + new Vector2(HexCell.HexWidth / 2, HexCell.Radius)
            }, true);
            await this.Frame();
            _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            this.Check(hovered.IsHovered(), "Adjacent cell must be hovered for highlight raster check");
            using Image highlighted = root.GetTexture().GetImage();
            Transform2D selectedTransform = root.GetStretchTransform() * selected.GetGlobalTransformWithCanvas();
            Transform2D hoveredTransform = root.GetStretchTransform() * hovered.GetGlobalTransformWithCanvas();
            Vector2 start = selectedTransform * new Vector2(-2, -2);
            Vector2 finish = hoveredTransform * new Vector2(HexCell.HexWidth + 2, (HexCell.Radius * 2) + 2);
            int overwritten = 0;
            int changedInside = 0;
            Transform2D selectedInverse = selectedTransform.AffineInverse();
            Transform2D hoveredInverse = hoveredTransform.AffineInverse();
            Vector2[] selectedPolygon = HexCell.Polygon(width / 2);
            Vector2[] hoveredPolygon = HexCell.Polygon(width / 2);
            for (int y = Mathf.CeilToInt(start.Y); y < Mathf.FloorToInt(finish.Y); y++) {
                for (int x = Mathf.CeilToInt(start.X); x < Mathf.FloorToInt(finish.X); x++) {
                    Vector2 pixel = new(x + 0.5f, y + 0.5f);
                    bool inside = Geometry2D.IsPointInPolygon(selectedInverse * pixel, selectedPolygon)
                        || Geometry2D.IsPointInPolygon(hoveredInverse * pixel, hoveredPolygon);
                    if (baseline.GetPixel(x, y) == highlighted.GetPixel(x, y)) {
                        continue;
                    }

                    if (inside) {
                        changedInside++;
                    } else {
                        overwritten++;
                    }
                }
            }

            this.Check(overwritten == 0, $"Highlights changed {overwritten} pixels outside cell interiors at width {width:F2}");
            this.Check(changedInside > 0, "Highlights did not render");
            if (width == 8.5f) {
                SaveImage(highlighted, "HexGridAdjacentHighlights.png");
            }
        }
        shell.HexOutlineWidth = 2;
    }

    private static void SaveImage(Image image, string name) {
        _ = Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://Build"));
        Error error = image.SavePng($"res://Build/{name}");
        if (error != Error.Ok) {
            throw new IOException($"Could not save {name}: {error}");
        }
    }
}

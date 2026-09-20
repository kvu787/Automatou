using Godot;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SimplePaint3DShell;

public partial class Main : Control {
    private static readonly Color Ink = new("#d8dbef");
    private static readonly Color Muted = new("#777f9e");
    private static readonly Color Void = new("#090b1d");
    private static readonly Color PanelColor = new("#11142b");
    private static readonly Color PanelRaised = new("#181c39");
    private static readonly Color Violet = new("#8b7cff");
    private static readonly Color Mint = new("#55d6c2");
    private static readonly Color Gold = new("#f2c66d");
    private static readonly Color Rose = new("#ef5b66");

    private sealed record Symbol(string Glyph, string Label, Color Color);

    // Legend labels and colors are presentation only. Map glyphs come from the Kernel.
    private static readonly Dictionary<string, Symbol> TerrainSymbols = new() {
        ["shatteredPlain"] = new("·", "SHATTERED", new Color("#a1adc4")),
        ["ashWaste"] = new(":", "ASH", new Color("#c7aaa0")),
        ["leyChannel"] = new("≈", "LEY", new Color("#9b99ff")),
        ["xenoforest"] = new("^", "XENO", new Color("#72bd8b")),
        ["fortifiedReach"] = new("#", "FORTIFIED", new Color("#65d3cf")),
        ["broodMire"] = new("~", "BROOD", new Color("#d88bac"))
    };
    private static readonly Dictionary<string, Symbol> ForceSymbols = new() {
        ["bastion"] = new("B", "BASTION", Gold),
        ["soldier"] = new("S", "SOLDIER", Mint),
        ["ravener"] = new("r", "RAVENER", Rose),
        ["broodNode"] = new("N", "BROOD NODE", Rose),
        ["enclave"] = new("E", "ENCLAVE", Mint)
    };

    /// <summary>Total width of an interior hex outline in board pixels; zero hides outlines.</summary>
    [Export(PropertyHint.Range, "0,12,0.25")]
    public float HexOutlineWidth {
        get;
        set {
            field = Mathf.Clamp(value, 0, 12);
            if (IsInstanceValid(this.OutlineWidthSlider)) {
                this.OutlineWidthSlider.SetValueNoSignal(field);
            }

            if (IsInstanceValid(this.OutlineWidthValue)) {
                this.OutlineWidthValue.Text = OutlineText(field);
            }

            foreach (HexCell cell in this.CellButtons) {
                cell.OutlineWidth = field;
            }
        }
    } = 2.0f;

    private KernelConnection? _kernel;
    internal KernelSnapshot? Snapshot { get; private set; }
    internal Vector2I Selected { get; private set; }
    internal List<HexCell> CellButtons { get; } = [];
    internal int ResponseCount { get; private set; }
    internal bool LastResponseAccepted { get; private set; }
    internal HSlider OutlineWidthSlider { get; private set; } = null!;
    internal Label OutlineWidthValue { get; private set; } = null!;
    internal RichTextLabel Inspector { get; private set; } = null!;
    internal VBoxContainer CommandPanel { get; private set; } = null!;
    internal LineEdit SeedEdit { get; private set; } = null!;
    internal Label Status { get; private set; } = null!;
    private Label _titleLabel = null!;
    private Label _turnLabel = null!;
    private Control _grid = null!;
    private RichTextLabel _chronicle = null!;
    private Button _advanceButton = null!;

    public override void _Ready() {
        // Fractional hex centers must survive rendering without independent Control rounding.
        this.GetViewport().GuiSnapControlsToPixels = false;
        this.BuildInterface();
        this.StartKernel();
        this.GetWindow().MinSize = new Vector2I(1000, 680);
    }

    public override void _ExitTree() {
        this._kernel?.Dispose();
        this._kernel = null;
    }

    public override void _Process(double delta) {
        if (this._kernel is null) {
            return;
        }

        while (this._kernel.TryRead(out string? line)) {
            this.Receive(line!);
        }

        while (this._kernel.TryReadError(out string? error)) {
            this.SetStatus($"KERNEL: {error}", Rose);
        }

        if (this._kernel.HasExited) {
            this.SetStatus("KERNEL STOPPED — run Run.cmd to rebuild and restart", Rose);
            this._advanceButton.Disabled = true;
            this._kernel.Dispose();
            this._kernel = null;
        }
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (@event.IsActionPressed("advance_turn") && !this.SeedEdit.HasFocus()) {
            this.Send(new { command = "advance" });
            this.GetViewport().SetInputAsHandled();
        }
    }

    private void BuildInterface() {
        ColorRect backdrop = new() { Color = Void };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        this.AddChild(backdrop);
        MarginContainer margin = Margin(20, 16, 20, 18);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        this.AddChild(margin);
        VBoxContainer root = new();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);
        root.AddChild(this.BuildHeader());
        HSplitContainer middle = new() { SizeFlagsVertical = SizeFlags.ExpandFill, SplitOffsets = [820] };
        middle.AddThemeConstantOverride("separation", 14);
        root.AddChild(middle);
        middle.AddChild(this.BuildWorldPanel());
        middle.AddChild(this.BuildSidePanel());
        root.AddChild(this.BuildFooter());
    }

    private PanelContainer BuildHeader() {
        PanelContainer panel = Panel(PanelColor, Violet, 10);
        MarginContainer margin = Margin(16, 10, 12, 10);
        panel.AddChild(margin);
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        margin.AddChild(row);
        this._titleLabel = Label("AUTOMATOU // THE BASTION FRONT", Ink, 24);
        this._titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(this._titleLabel);
        this._turnLabel = Label("TURN 000", Gold, 18);
        row.AddChild(this._turnLabel);
        this.SeedEdit = new LineEdit {
            Text = "475023", PlaceholderText = "SEED", CustomMinimumSize = new Vector2(100, 0)
        };
        this.SeedEdit.AddThemeColorOverride("font_color", Ink);
        this.SeedEdit.AddThemeStyleboxOverride("normal", PanelStyle(PanelRaised, Muted, 1, 6));
        row.AddChild(this.SeedEdit);
        Button forge = new() {
            Text = "OPEN FRONT", TooltipText = "Create a fresh deterministic war front from this seed."
        };
        StyleButton(forge, Mint);
        forge.Pressed += this.NewWorld;
        row.AddChild(forge);
        return panel;
    }

    private PanelContainer BuildWorldPanel() {
        PanelContainer panel = Panel(PanelColor, new Color("#282e55"), 10);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        VBoxContainer column = new();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);
        column.AddChild(BuildSymbolLegend());
        column.AddChild(this.BuildOutlineControl());
        ScrollContainer scroll = new() {
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        column.AddChild(scroll);
        CenterContainer centering = new() {
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.AddChild(centering);
        this._grid = new Control();
        centering.AddChild(this._grid);
        return panel;
    }

    private HBoxContainer BuildOutlineControl() {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(Label("OUTLINE WIDTH", Muted));
        this.OutlineWidthSlider = new HSlider {
            MinValue = 0, MaxValue = 12, Step = 0.25, Value = this.HexOutlineWidth,
            CustomMinimumSize = new Vector2(160, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Hex outline width. Set to zero to hide outlines."
        };
        this.OutlineWidthSlider.ValueChanged += value => this.HexOutlineWidth = (float)value;
        row.AddChild(this.OutlineWidthSlider);
        this.OutlineWidthValue = Label(OutlineText(this.HexOutlineWidth), Ink);
        this.OutlineWidthValue.CustomMinimumSize = new Vector2(72, 0);
        this.OutlineWidthValue.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(this.OutlineWidthValue);
        return row;
    }

    private ScrollContainer BuildSidePanel() {
        ScrollContainer wrapper = new() {
            CustomMinimumSize = new Vector2(330, 0),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        VBoxContainer side = new() {
            CustomMinimumSize = new Vector2(330, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        side.AddThemeConstantOverride("separation", 10);
        wrapper.AddChild(side);
        this.Inspector = RichText(Ink, 14);
        this.Inspector.FitContent = false;
        this.Inspector.CustomMinimumSize = new Vector2(0, 220);
        side.AddChild(this.Inspector);
        this.CommandPanel = new VBoxContainer();
        this.CommandPanel.AddThemeConstantOverride("separation", 6);
        this.CommandPanel.AddChild(Label("FIELD COMMAND AUTHORITY", Rose));
        this.AddCommandButton("CHANNEL +25 RESONANCE", "channel", Violet);
        this.AddCommandButton("FORTIFY THE REACH", "fortify", Mint,
            new() { ["terrain"] = "fortifiedReach" }, TerrainSymbols["fortifiedReach"].Glyph);
        this.AddCommandButton("DEPLOY SOLDIER", "deploy", Gold,
            new() { ["kind"] = "soldier" }, ForceSymbols["soldier"].Glyph);
        this.AddCommandButton("COMMIT BASTION — IF LOST", "deploy", Gold,
            new() { ["kind"] = "bastion" }, ForceSymbols["bastion"].Glyph);
        this.AddCommandButton("ESTABLISH VIGIL ANNEX", "establish", Mint,
            new() { ["name"] = "Vigil Annex" }, ForceSymbols["enclave"].Glyph);
        this.AddCommandButton("AUTHORIZE MAGITECH PURGE", "purge", Rose,
            new() { ["radius"] = 1 }, TerrainSymbols["ashWaste"].Glyph);
        side.AddChild(this.CommandPanel);
        side.AddChild(Label("FRONT DISPATCHES", Violet));
        this._chronicle = RichText(Muted, 13);
        this._chronicle.ScrollActive = true;
        this._chronicle.SizeFlagsVertical = SizeFlags.ExpandFill;
        side.AddChild(this._chronicle);
        return wrapper;
    }

    private HBoxContainer BuildFooter() {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        this.Status = Label("CONNECTING TO KERNEL.", Muted);
        this.Status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(this.Status);
        row.AddChild(Label("SPACE / ENTER", Muted));
        this._advanceButton = new Button { Text = "ADVANCE THE FRONT  →", CustomMinimumSize = new Vector2(230, 44) };
        this._advanceButton.AddThemeFontSizeOverride("font_size", 16);
        StyleButton(this._advanceButton, Gold);
        this._advanceButton.Pressed += () => this.Send(new { command = "advance" });
        row.AddChild(this._advanceButton);
        return row;
    }

    private static VBoxContainer BuildSymbolLegend() {
        VBoxContainer legend = new();
        legend.AddThemeConstantOverride("separation", 4);
        foreach (Dictionary<string, Symbol>? symbols in new[] { TerrainSymbols, ForceSymbols }) {
            bool terrain = ReferenceEquals(symbols, TerrainSymbols);
            HFlowContainer row = new();
            row.AddThemeConstantOverride("h_separation", 14);
            row.AddThemeConstantOverride("v_separation", 4);
            foreach (Symbol symbol in symbols.Values) {
                HBoxContainer item = new();
                if (terrain) {
                    item.AddChild(new ColorRect {
                        CustomMinimumSize = new Vector2(14, 14), SizeFlagsVertical = SizeFlags.ShrinkCenter,
                        Color = PanelRaised.Lerp(symbol.Color, 0.35f)
                    });
                }
                item.AddChild(Label(terrain ? symbol.Label : $"{symbol.Glyph}  {symbol.Label}", symbol.Color, 13));
                row.AddChild(item);
            }
            legend.AddChild(row);
        }
        return legend;
    }

    private void AddCommandButton(string label, string command, Color accent,
        Dictionary<string, object>? extras = null, string symbol = "") {
        Button button = new() {
            Text = symbol.Length == 0 ? label : $"{symbol}   {label}",
            Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 34)
        };
        StyleButton(button, accent);
        button.Pressed += () => {
            Dictionary<string, object> payload = new() {
                ["command"] = command, ["x"] = this.Selected.X, ["y"] = this.Selected.Y
            };
            if (extras is not null) {
                foreach (KeyValuePair<string, object> entry in extras) {
                    payload[entry.Key] = entry.Value;
                }
            }

            this.Send(payload);
        };
        this.CommandPanel.AddChild(button);
    }

    private void StartKernel() {
        string baseDirectory = OS.HasFeature("editor")
            ? ProjectSettings.GlobalizePath("res://")
            : Path.GetDirectoryName(OS.GetExecutablePath())!;
        try {
            this._kernel = new KernelConnection(Path.Combine(baseDirectory, "KernelHost"));
            this.SetStatus("WAR KERNEL ONLINE — awaiting front state", Muted);
        } catch (Exception exception) when (exception is IOException or System.ComponentModel.Win32Exception or InvalidOperationException) {
            this.SetStatus($"KERNEL UNAVAILABLE — {exception.Message}", Rose);
            this._advanceButton.Disabled = true;
        }
    }

    private void Receive(string line) {
        if (string.IsNullOrWhiteSpace(line)) {
            return;
        }

        try {
            KernelResponse? response = JsonSerializer.Deserialize<KernelResponse>(line, KernelProtocol.JsonOptions);
            if (response?.Snapshot is not { Tiles: not null, Forces: not null, Chronicle: not null } snapshot) {
                throw new JsonException("Missing world snapshot.");
            }

            this.Snapshot = snapshot;
            this.LastResponseAccepted = response.Ok;
            this.ResponseCount++;
            this.SetStatus(response.Message, response.Ok ? Mint : Rose);
            this.RenderSnapshot();
        } catch (JsonException) {
            this.SetStatus("KERNEL PROTOCOL ERROR", Rose);
        }
    }

    internal void Send(object payload) {
        if (this._kernel is null) {
            return;
        }

        try {
            this._kernel.Send(payload);
        } catch (Exception exception) when (exception is IOException or InvalidOperationException) {
            this.SetStatus($"KERNEL UNAVAILABLE — {exception.Message}", Rose);
            this._advanceButton.Disabled = true;
        }
    }

    internal void NewWorld() {
        long seed = long.TryParse(this.SeedEdit.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long value)
            ? value : 475023;
        this.Send(new { command = "new", width = 16, height = 12, seed, name = "The Bastion Front" });
    }

    private void RenderSnapshot() {
        if (this.Snapshot is not { } snapshot) {
            return;
        }

        this._titleLabel.Text = snapshot.Name.ToUpperInvariant();
        this._turnLabel.Text = $"TURN {snapshot.Turn:000}";
        this._grid.CustomMinimumSize = new Vector2((snapshot.Width + 0.5f) * HexCell.HexWidth,
            ((snapshot.Height - 1) * HexCell.RowStep) + (2 * HexCell.Radius));
        if (this.Selected.X >= snapshot.Width || this.Selected.Y >= snapshot.Height) {
            this.Selected = Vector2I.Zero;
        }

        foreach (Node? child in this._grid.GetChildren()) {
            this._grid.RemoveChild(child);
            child.QueueFree();
        }
        this.CellButtons.Clear();
        ILookup<CellPosition, ForceSnapshot> forcesByCell = snapshot.Forces.ToLookup(force => force.Position);
        foreach (CellSnapshot? tile in snapshot.Tiles.Take(snapshot.Width * snapshot.Height)) {
            ForceSnapshot[] occupants = forcesByCell[tile.Position].ToArray();
            ForceSnapshot? force = occupants.MaxBy(ForcePriority);
            Color terrainColor = TerrainColor(tile.Terrain);
            Vector2I point = new(tile.Position.X, tile.Position.Y);
            HexCell button = new() {
                Text = force?.Glyph ?? "", TooltipText = CellTooltip(tile, occupants),
                Size = new Vector2(HexCell.HexWidth, 2 * HexCell.Radius),
                Position = HexCell.CellPosition(point.X, point.Y),
                Background = PanelRaised.Lerp(terrainColor, 0.35f), OutlineWidth = this.HexOutlineWidth,
                SymbolColor = force is null ? terrainColor : ForceColor(force.Kind), Selected = point == this.Selected
            };
            if (occupants.Length > 1) {
                AddCornerLabel(button, occupants.Length.ToString(CultureInfo.InvariantCulture));
            }

            button.Pressed += () => this.SelectCell(point);
            this._grid.AddChild(button);
            this.CellButtons.Add(button);
        }
        this.RenderInspector();
        this.RenderChronicle();
    }

    internal void SelectCell(Vector2I point) {
        this.Selected = point;
        this.RenderSnapshot();
    }

    private void RenderInspector() {
        if (this.Snapshot is not { } snapshot) {
            return;
        }

        int index = (this.Selected.Y * snapshot.Width) + this.Selected.X;
        if (index < 0 || index >= snapshot.Tiles.Length) {
            return;
        }

        CellSnapshot tile = snapshot.Tiles[index];
        StringBuilder text = new();
        _ = text.Append(CultureInfo.CurrentCulture, $"[color=#7e6bff][font_size=12]HEX // COLUMN {this.Selected.X:00}, ROW {this.Selected.Y:00}[/font_size][/color]\n");
        _ = text.Append(CultureInfo.CurrentCulture, $"[color=#{TerrainColor(tile.Terrain).ToHtml(false)}][font_size=22]{tile.Glyph}  {Words(tile.Terrain).ToUpperInvariant()}[/font_size][/color]\n");
        _ = text.Append(CultureInfo.CurrentCulture, $"[color=#777f9e]{EscapeMarkup(tile.Description)}[/color]\n");
        ForceSnapshot[] occupants = snapshot.Forces.Where(force => force.Position == tile.Position).ToArray();
        if (occupants.Length == 0) {
            _ = text.Append("\n[color=#777f9e]No detected forces occupy this sector.[/color]");
        } else {
            _ = text.Append("\n[color=#4fe4c1]FORCES[/color]");
            foreach (ForceSnapshot? force in occupants) {
                _ = text.Append(CultureInfo.CurrentCulture, $"\n[color=#{ForceColor(force.Kind).ToHtml(false)}][b]{force.Glyph}  {EscapeMarkup(force.Name)}[/b][/color] · {EscapeMarkup(force.Intent)} · STR {force.Strength}");
            }
        }
        this.Inspector.Text = text.ToString();
    }

    private void RenderChronicle() {
        if (this.Snapshot is not { } snapshot) {
            return;
        }

        if (snapshot.Chronicle.Length == 0) {
            this._chronicle.Text = "[color=#777f9e]No dispatches have reached command.[/color]";
            return;
        }
        this._chronicle.Text = string.Concat(snapshot.Chronicle.Select((line, index) =>
            $"[color={(index == 0 ? "#d8dbef" : "#777f9e")}]{EscapeMarkup(line)}[/color]\n\n"));
    }

    private static string CellTooltip(CellSnapshot tile, ForceSnapshot[] occupants) {
        return $"{tile.Glyph}  {Words(tile.Terrain)}\n{tile.Description}" + string.Concat(occupants.Select(force =>
            $"\n{force.Glyph}  {force.Name} · {force.Intent} · strength {force.Strength}"));
    }

    private static void AddCornerLabel(BaseButton button, string text) {
        Label label = Label(text, Ink, 11);
        label.MouseFilter = MouseFilterEnum.Ignore;
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        label.OffsetLeft = 7;
        label.OffsetRight = -7;
        label.OffsetTop = 8;
        label.OffsetBottom = -8;
        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.VerticalAlignment = VerticalAlignment.Top;
        button.AddChild(label);
    }

    private static Color TerrainColor(string terrain) {
        return TerrainSymbols.GetValueOrDefault(terrain)?.Color ?? Muted;
    }

    private static Color ForceColor(string kind) {
        return ForceSymbols.GetValueOrDefault(kind)?.Color ?? Ink;
    }

    private static int ForcePriority(ForceSnapshot force) {
        return force.Kind switch {
            "bastion" => 5,
            "broodNode" => 4,
            "enclave" => 3,
            "ravener" => 2,
            "soldier" => 1,
            _ => 0
        };
    }

    private static string Words(string camel) {
        return string.Concat(camel.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));
    }

    private static string EscapeMarkup(string text) {
        return text.Replace("[", "[lb]", StringComparison.Ordinal);
    }

    private static string OutlineText(float width) {
        return width.ToString("F2", CultureInfo.InvariantCulture) + " px";
    }

    private void SetStatus(string text, Color color) {
        this.Status.Text = text;
        this.Status.AddThemeColorOverride("font_color", color);
    }

    private static Label Label(string text, Color color, int fontSize = 0) {
        Label label = new() { Text = text };
        label.AddThemeColorOverride("font_color", color);
        if (fontSize > 0) {
            label.AddThemeFontSizeOverride("font_size", fontSize);
        }

        return label;
    }

    private static MarginContainer Margin(int left, int top, int right, int bottom) {
        MarginContainer margin = new();
        margin.AddThemeConstantOverride("margin_left", left);
        margin.AddThemeConstantOverride("margin_top", top);
        margin.AddThemeConstantOverride("margin_right", right);
        margin.AddThemeConstantOverride("margin_bottom", bottom);
        return margin;
    }

    private static PanelContainer Panel(Color background, Color border, int radius) {
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride("panel", PanelStyle(background, border, 1, radius));
        return panel;
    }

    private static RichTextLabel RichText(Color color, int fontSize) {
        RichTextLabel label = new() { BbcodeEnabled = true };
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeColorOverride("default_color", color);
        label.AddThemeStyleboxOverride("normal", PanelStyle(PanelColor, new Color("#282e55"), 1, 8));
        return label;
    }

    private static void StyleButton(BaseButton button, Color accent) {
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", PanelStyle(PanelRaised, accent.Darkened(0.45f), 1, 6));
        button.AddThemeStyleboxOverride("hover", PanelStyle(PanelRaised.Lightened(0.08f), accent, 1, 6));
        button.AddThemeStyleboxOverride("pressed", PanelStyle(PanelRaised.Darkened(0.12f), accent, 2, 6));
    }

    private static StyleBoxFlat PanelStyle(Color background, Color border, int width, int radius) {
        StyleBoxFlat style = new() {
            BgColor = background, BorderColor = border, ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 7, ContentMarginBottom = 7
        };
        style.SetBorderWidthAll(width);
        style.SetCornerRadiusAll(radius);
        return style;
    }
}

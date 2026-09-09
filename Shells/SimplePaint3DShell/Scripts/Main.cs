using System.Globalization;
using System.Text;
using System.Text.Json;
using Godot;

namespace SimplePaint3DShell;

public partial class Main : Control
{
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
    private static readonly Dictionary<string, Symbol> TerrainSymbols = new()
    {
        ["shatteredPlain"] = new("·", "SHATTERED", new Color("#a1adc4")),
        ["ashWaste"] = new(":", "ASH", new Color("#c7aaa0")),
        ["leyChannel"] = new("≈", "LEY", new Color("#9b99ff")),
        ["xenoforest"] = new("^", "XENO", new Color("#72bd8b")),
        ["fortifiedReach"] = new("#", "FORTIFIED", new Color("#65d3cf")),
        ["broodMire"] = new("~", "BROOD", new Color("#d88bac"))
    };
    private static readonly Dictionary<string, Symbol> ForceSymbols = new()
    {
        ["bastion"] = new("B", "BASTION", Gold),
        ["soldier"] = new("S", "SOLDIER", Mint),
        ["ravener"] = new("r", "RAVENER", Rose),
        ["broodNode"] = new("N", "BROOD NODE", Rose),
        ["enclave"] = new("E", "ENCLAVE", Mint)
    };

    private float _hexOutlineWidth = 2.0f;

    /// <summary>Total width of an interior hex outline in board pixels; zero hides outlines.</summary>
    [Export(PropertyHint.Range, "0,12,0.25")]
    public float HexOutlineWidth
    {
        get => _hexOutlineWidth;
        set
        {
            _hexOutlineWidth = Mathf.Clamp(value, 0, 12);
            if (GodotObject.IsInstanceValid(OutlineWidthSlider))
                OutlineWidthSlider.SetValueNoSignal(_hexOutlineWidth);
            if (GodotObject.IsInstanceValid(OutlineWidthValue))
                OutlineWidthValue.Text = OutlineText(_hexOutlineWidth);
            foreach (var cell in CellButtons) cell.OutlineWidth = _hexOutlineWidth;
        }
    }

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

    public override void _Ready()
    {
        // Fractional hex centers must survive rendering without independent Control rounding.
        GetViewport().GuiSnapControlsToPixels = false;
        BuildInterface();
        StartKernel();
        GetWindow().MinSize = new Vector2I(1000, 680);
    }

    public override void _ExitTree()
    {
        _kernel?.Dispose();
        _kernel = null;
    }

    public override void _Process(double delta)
    {
        if (_kernel is null) return;
        while (_kernel.TryRead(out var line)) Receive(line!);
        while (_kernel.TryReadError(out var error)) SetStatus($"KERNEL: {error}", Rose);
        if (_kernel.HasExited)
        {
            SetStatus("KERNEL STOPPED — run Run.cmd to rebuild and restart", Rose);
            _advanceButton.Disabled = true;
            _kernel.Dispose();
            _kernel = null;
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed("advance_turn") && !SeedEdit.HasFocus())
        {
            Send(new { command = "advance" });
            GetViewport().SetInputAsHandled();
        }
    }

    private void BuildInterface()
    {
        var backdrop = new ColorRect { Color = Void };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);
        var margin = Margin(20, 16, 20, 18);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(margin);
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        margin.AddChild(root);
        root.AddChild(BuildHeader());
        var middle = new HSplitContainer { SizeFlagsVertical = SizeFlags.ExpandFill, SplitOffsets = [820] };
        middle.AddThemeConstantOverride("separation", 14);
        root.AddChild(middle);
        middle.AddChild(BuildWorldPanel());
        middle.AddChild(BuildSidePanel());
        root.AddChild(BuildFooter());
    }

    private Control BuildHeader()
    {
        var panel = Panel(PanelColor, Violet, 10);
        var margin = Margin(16, 10, 12, 10);
        panel.AddChild(margin);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        margin.AddChild(row);
        _titleLabel = Label("AUTOMAPOLIS // THE BASTION FRONT", Ink, 24);
        _titleLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(_titleLabel);
        _turnLabel = Label("TURN 000", Gold, 18);
        row.AddChild(_turnLabel);
        SeedEdit = new LineEdit
        {
            Text = "475023", PlaceholderText = "SEED", CustomMinimumSize = new Vector2(100, 0)
        };
        SeedEdit.AddThemeColorOverride("font_color", Ink);
        SeedEdit.AddThemeStyleboxOverride("normal", PanelStyle(PanelRaised, Muted, 1, 6));
        row.AddChild(SeedEdit);
        var forge = new Button
        {
            Text = "OPEN FRONT", TooltipText = "Create a fresh deterministic war front from this seed."
        };
        StyleButton(forge, Mint);
        forge.Pressed += NewWorld;
        row.AddChild(forge);
        return panel;
    }

    private Control BuildWorldPanel()
    {
        var panel = Panel(PanelColor, new Color("#282e55"), 10);
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);
        column.AddChild(BuildSymbolLegend());
        column.AddChild(BuildOutlineControl());
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        column.AddChild(scroll);
        var centering = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill
        };
        scroll.AddChild(centering);
        _grid = new Control();
        centering.AddChild(_grid);
        return panel;
    }

    private Control BuildOutlineControl()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(Label("OUTLINE WIDTH", Muted));
        OutlineWidthSlider = new HSlider
        {
            MinValue = 0, MaxValue = 12, Step = 0.25, Value = HexOutlineWidth,
            CustomMinimumSize = new Vector2(160, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            TooltipText = "Hex outline width. Set to zero to hide outlines."
        };
        OutlineWidthSlider.ValueChanged += value => HexOutlineWidth = (float)value;
        row.AddChild(OutlineWidthSlider);
        OutlineWidthValue = Label(OutlineText(HexOutlineWidth), Ink);
        OutlineWidthValue.CustomMinimumSize = new Vector2(72, 0);
        OutlineWidthValue.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(OutlineWidthValue);
        return row;
    }

    private Control BuildSidePanel()
    {
        var wrapper = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(330, 0),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        var side = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(330, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        side.AddThemeConstantOverride("separation", 10);
        wrapper.AddChild(side);
        Inspector = RichText(Ink, 14);
        Inspector.FitContent = false;
        Inspector.CustomMinimumSize = new Vector2(0, 220);
        side.AddChild(Inspector);
        CommandPanel = new VBoxContainer();
        CommandPanel.AddThemeConstantOverride("separation", 6);
        CommandPanel.AddChild(Label("FIELD COMMAND AUTHORITY", Rose));
        AddCommandButton("CHANNEL +25 RESONANCE", "channel", Violet);
        AddCommandButton("FORTIFY THE REACH", "fortify", Mint,
            new() { ["terrain"] = "fortifiedReach" }, TerrainSymbols["fortifiedReach"].Glyph);
        AddCommandButton("DEPLOY SOLDIER", "deploy", Gold,
            new() { ["kind"] = "soldier" }, ForceSymbols["soldier"].Glyph);
        AddCommandButton("COMMIT BASTION — IF LOST", "deploy", Gold,
            new() { ["kind"] = "bastion" }, ForceSymbols["bastion"].Glyph);
        AddCommandButton("ESTABLISH VIGIL ANNEX", "establish", Mint,
            new() { ["name"] = "Vigil Annex" }, ForceSymbols["enclave"].Glyph);
        AddCommandButton("AUTHORIZE MAGITECH PURGE", "purge", Rose,
            new() { ["radius"] = 1 }, TerrainSymbols["ashWaste"].Glyph);
        side.AddChild(CommandPanel);
        side.AddChild(Label("FRONT DISPATCHES", Violet));
        _chronicle = RichText(Muted, 13);
        _chronicle.ScrollActive = true;
        _chronicle.SizeFlagsVertical = SizeFlags.ExpandFill;
        side.AddChild(_chronicle);
        return wrapper;
    }

    private Control BuildFooter()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        Status = Label("CONNECTING TO KERNEL.", Muted);
        Status.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(Status);
        row.AddChild(Label("SPACE / ENTER", Muted));
        _advanceButton = new Button { Text = "ADVANCE THE FRONT  →", CustomMinimumSize = new Vector2(230, 44) };
        _advanceButton.AddThemeFontSizeOverride("font_size", 16);
        StyleButton(_advanceButton, Gold);
        _advanceButton.Pressed += () => Send(new { command = "advance" });
        row.AddChild(_advanceButton);
        return row;
    }

    private static Control BuildSymbolLegend()
    {
        var legend = new VBoxContainer();
        legend.AddThemeConstantOverride("separation", 4);
        foreach (var symbols in new[] { TerrainSymbols, ForceSymbols })
        {
            var terrain = ReferenceEquals(symbols, TerrainSymbols);
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 14);
            row.AddThemeConstantOverride("v_separation", 4);
            foreach (var symbol in symbols.Values)
            {
                var item = new HBoxContainer();
                if (terrain)
                {
                    item.AddChild(new ColorRect
                    {
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
        Dictionary<string, object>? extras = null, string symbol = "")
    {
        var button = new Button
        {
            Text = symbol.Length == 0 ? label : $"{symbol}   {label}",
            Alignment = HorizontalAlignment.Left, CustomMinimumSize = new Vector2(0, 34)
        };
        StyleButton(button, accent);
        button.Pressed += () =>
        {
            var payload = new Dictionary<string, object>
            {
                ["command"] = command, ["x"] = Selected.X, ["y"] = Selected.Y
            };
            if (extras is not null)
                foreach (var entry in extras) payload[entry.Key] = entry.Value;
            Send(payload);
        };
        CommandPanel.AddChild(button);
    }

    private void StartKernel()
    {
        var baseDirectory = OS.HasFeature("editor")
            ? ProjectSettings.GlobalizePath("res://")
            : Path.GetDirectoryName(OS.GetExecutablePath())!;
        try
        {
            _kernel = new KernelConnection(Path.Combine(baseDirectory, "KernelHost"));
            SetStatus("WAR KERNEL ONLINE — awaiting front state", Muted);
        }
        catch (Exception exception) when (exception is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            SetStatus($"KERNEL UNAVAILABLE — {exception.Message}", Rose);
            _advanceButton.Disabled = true;
        }
    }

    private void Receive(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        try
        {
            var response = JsonSerializer.Deserialize<KernelResponse>(line, KernelProtocol.JsonOptions);
            if (response?.Snapshot is not { Tiles: not null, Forces: not null, Chronicle: not null } snapshot)
                throw new JsonException("Missing world snapshot.");
            Snapshot = snapshot;
            LastResponseAccepted = response.Ok;
            ResponseCount++;
            SetStatus(response.Message, response.Ok ? Mint : Rose);
            RenderSnapshot();
        }
        catch (JsonException)
        {
            SetStatus("KERNEL PROTOCOL ERROR", Rose);
        }
    }

    internal void Send(object payload)
    {
        if (_kernel is null) return;
        try
        {
            _kernel.Send(payload);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            SetStatus($"KERNEL UNAVAILABLE — {exception.Message}", Rose);
            _advanceButton.Disabled = true;
        }
    }

    internal void NewWorld()
    {
        var seed = long.TryParse(SeedEdit.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value : 475023;
        Send(new { command = "new", width = 16, height = 12, seed, name = "The Bastion Front" });
    }

    private void RenderSnapshot()
    {
        if (Snapshot is not { } snapshot) return;
        _titleLabel.Text = snapshot.Name.ToUpperInvariant();
        _turnLabel.Text = $"TURN {snapshot.Turn:000}";
        _grid.CustomMinimumSize = new Vector2((snapshot.Width + 0.5f) * HexCell.HexWidth,
            (snapshot.Height - 1) * HexCell.RowStep + 2 * HexCell.Radius);
        if (Selected.X >= snapshot.Width || Selected.Y >= snapshot.Height) Selected = Vector2I.Zero;
        foreach (var child in _grid.GetChildren())
        {
            _grid.RemoveChild(child);
            child.QueueFree();
        }
        CellButtons.Clear();
        var forcesByCell = snapshot.Forces.ToLookup(force => force.Position);
        foreach (var tile in snapshot.Tiles.Take(snapshot.Width * snapshot.Height))
        {
            var occupants = forcesByCell[tile.Position].ToArray();
            var force = occupants.MaxBy(ForcePriority);
            var terrainColor = TerrainColor(tile.Terrain);
            var point = new Vector2I(tile.Position.X, tile.Position.Y);
            var button = new HexCell
            {
                Text = force?.Glyph ?? "", TooltipText = CellTooltip(tile, occupants),
                Size = new Vector2(HexCell.HexWidth, 2 * HexCell.Radius),
                Position = HexCell.CellPosition(point.X, point.Y),
                Background = PanelRaised.Lerp(terrainColor, 0.35f), OutlineWidth = HexOutlineWidth,
                SymbolColor = force is null ? terrainColor : ForceColor(force.Kind), Selected = point == Selected
            };
            if (occupants.Length > 1) AddCornerLabel(button, occupants.Length.ToString(CultureInfo.InvariantCulture));
            button.Pressed += () => SelectCell(point);
            _grid.AddChild(button);
            CellButtons.Add(button);
        }
        RenderInspector();
        RenderChronicle();
    }

    internal void SelectCell(Vector2I point)
    {
        Selected = point;
        RenderSnapshot();
    }

    private void RenderInspector()
    {
        if (Snapshot is not { } snapshot) return;
        var index = Selected.Y * snapshot.Width + Selected.X;
        if (index < 0 || index >= snapshot.Tiles.Length) return;
        var tile = snapshot.Tiles[index];
        var text = new StringBuilder();
        text.Append($"[color=#7e6bff][font_size=12]HEX // COLUMN {Selected.X:00}, ROW {Selected.Y:00}[/font_size][/color]\n");
        text.Append($"[color=#{TerrainColor(tile.Terrain).ToHtml(false)}][font_size=22]{tile.Glyph}  {Words(tile.Terrain).ToUpperInvariant()}[/font_size][/color]\n");
        text.Append($"[color=#777f9e]{EscapeMarkup(tile.Description)}[/color]\n");
        var occupants = snapshot.Forces.Where(force => force.Position == tile.Position).ToArray();
        if (occupants.Length == 0)
            text.Append("\n[color=#777f9e]No detected forces occupy this sector.[/color]");
        else
        {
            text.Append("\n[color=#4fe4c1]FORCES[/color]");
            foreach (var force in occupants)
                text.Append($"\n[color=#{ForceColor(force.Kind).ToHtml(false)}][b]{force.Glyph}  {EscapeMarkup(force.Name)}[/b][/color] · {EscapeMarkup(force.Intent)} · STR {force.Strength}");
        }
        Inspector.Text = text.ToString();
    }

    private void RenderChronicle()
    {
        if (Snapshot is not { } snapshot) return;
        if (snapshot.Chronicle.Length == 0)
        {
            _chronicle.Text = "[color=#777f9e]No dispatches have reached command.[/color]";
            return;
        }
        _chronicle.Text = string.Concat(snapshot.Chronicle.Select((line, index) =>
            $"[color={(index == 0 ? "#d8dbef" : "#777f9e")}]{EscapeMarkup(line)}[/color]\n\n"));
    }

    private static string CellTooltip(CellSnapshot tile, ForceSnapshot[] occupants) =>
        $"{tile.Glyph}  {Words(tile.Terrain)}\n{tile.Description}" + string.Concat(occupants.Select(force =>
            $"\n{force.Glyph}  {force.Name} · {force.Intent} · strength {force.Strength}"));

    private static void AddCornerLabel(BaseButton button, string text)
    {
        var label = Label(text, Ink, 11);
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

    private static Color TerrainColor(string terrain) => TerrainSymbols.GetValueOrDefault(terrain)?.Color ?? Muted;
    private static Color ForceColor(string kind) => ForceSymbols.GetValueOrDefault(kind)?.Color ?? Ink;
    private static int ForcePriority(ForceSnapshot force) => force.Kind switch
    {
        "bastion" => 5, "broodNode" => 4, "enclave" => 3, "ravener" => 2, "soldier" => 1, _ => 0
    };
    private static string Words(string camel) => string.Concat(camel.Select((character, index) =>
        index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));
    private static string EscapeMarkup(string text) => text.Replace("[", "[lb]", StringComparison.Ordinal);
    private static string OutlineText(float width) => width.ToString("F2", CultureInfo.InvariantCulture) + " px";

    private void SetStatus(string text, Color color)
    {
        Status.Text = text;
        Status.AddThemeColorOverride("font_color", color);
    }

    private static Label Label(string text, Color color, int fontSize = 0)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        if (fontSize > 0) label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    private static MarginContainer Margin(int left, int top, int right, int bottom)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", left);
        margin.AddThemeConstantOverride("margin_top", top);
        margin.AddThemeConstantOverride("margin_right", right);
        margin.AddThemeConstantOverride("margin_bottom", bottom);
        return margin;
    }

    private static PanelContainer Panel(Color background, Color border, int radius)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", PanelStyle(background, border, 1, radius));
        return panel;
    }

    private static RichTextLabel RichText(Color color, int fontSize)
    {
        var label = new RichTextLabel { BbcodeEnabled = true };
        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeColorOverride("default_color", color);
        label.AddThemeStyleboxOverride("normal", PanelStyle(PanelColor, new Color("#282e55"), 1, 8));
        return label;
    }

    private static void StyleButton(BaseButton button, Color accent)
    {
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", PanelStyle(PanelRaised, accent.Darkened(0.45f), 1, 6));
        button.AddThemeStyleboxOverride("hover", PanelStyle(PanelRaised.Lightened(0.08f), accent, 1, 6));
        button.AddThemeStyleboxOverride("pressed", PanelStyle(PanelRaised.Darkened(0.12f), accent, 2, 6));
    }

    private static StyleBoxFlat PanelStyle(Color background, Color border, int width, int radius)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background, BorderColor = border, ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 7, ContentMarginBottom = 7
        };
        style.SetBorderWidthAll(width);
        style.SetCornerRadiusAll(radius);
        return style;
    }
}

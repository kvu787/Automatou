using Automatou.Simulation;
using Godot;
using System.Globalization;

namespace Automatou.UserInterface;

public partial class MainInterface : Control {
    private World world = World.Demonstration();
    private HexBoard board = null!;
    private VBoxContainer toolsPanel = null!;
    private PanelContainer modePillbox = null!;
    private VBoxContainer modePanel = null!;
    private VBoxContainer inspectorPanel = null!;
    private Label turnLabel = null!, statusLabel = null!, eventLabel = null!, populationLabel = null!;
    private Button playButton = null!;
    private readonly List<Unit> unitDesigns = Catalog.Units();
    private string mode = "World";
    private OptionButton? toolChoice;
    private string Tool {
        get;
        set {
            field = value;
            if (this.toolChoice is not null && IsInstanceValid(this.toolChoice)) {
                this.toolChoice.Select(Array.IndexOf(ToolNames, value));
            }
            this.UpdateWorldToolVisibility();
        }
    } = "Inspect";
    private static readonly string[] ToolNames = ["File", "Inspect", "Paint terrain", "Place unit", "Erase entity"];
    private bool running;
    private double elapsed;
    private double turnsPerSecond = 2;
    private int facing;
    private int unitIndex;
    private Faction faction = Faction.Bastions;
    private Terrain terrain = Terrain.Forest;
    private Entity? selected;
    private World checkpoint = null!;
    private readonly SessionWorlds savedWorlds = new();
    private string sessionRoot = "";
    private Label? hoverLabel;
    private LineEdit worldName = null!;
    private OptionButton unitChoice = null!;
    private Control workspaceRoot = null!, menuRoot = null!;
    private HBoxContainer transport = null!;
    private Label screenTitle = null!;
    private Label? menuStatus;
    private bool menuVisible;
    private readonly Color muted = new("93a8b7");
    private readonly Color accent = new("87d9cc");

    public override void _Ready() {
        DisplayServer.WindowSetMinSize(new Vector2I(1100, 700));
        this.sessionRoot = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--session-log=", StringComparison.Ordinal))?[14..]
            ?? Path.Combine(ProjectSettings.GlobalizePath("res://MyLogOutput"), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));
        _ = Directory.CreateDirectory(this.sessionRoot);
        this.Log("Application started. Godot 4.7.2 / .NET 10.");
        this.world.EventRecorded = this.Log;
        foreach (string entry in this.world.Events) {
            this.Log(entry);
        }

        this.Theme = CreateTheme();
        this.BuildInterface();
        this.checkpoint = this.world.Copy();
        this.SwitchMode("World");
        this.Refresh();
        this.ShowMainMenu();
        if (OS.GetCmdlineUserArgs().Contains("--verify-interface")) {
            Callable.From(this.RunInterfaceVerification).CallDeferred();
        }
    }
    private void Log(string message) {
        File.AppendAllText(Path.Combine(this.sessionRoot, "Session.log"), $"{DateTime.Now:HH:mm:ss.fff} {message}{System.Environment.NewLine}");
    }

    private static StyleBoxFlat Box(string background, string border = "263b48", int radius = 6) {
        StyleBoxFlat box = new() { BgColor = new Color(background), BorderColor = new Color(border) };
        box.SetBorderWidthAll(1); box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = 12; box.ContentMarginRight = 12; box.ContentMarginTop = 9; box.ContentMarginBottom = 9;
        return box;
    }
    private static Theme CreateTheme() {
        Theme theme = new() { DefaultFontSize = 14 };
        theme.SetColor("font_color", "Label", new Color("deeaed"));
        theme.SetColor("font_color", "Button", new Color("c5d8dd"));
        foreach (string type in new[] { "Button", "OptionButton" }) {
            theme.SetStylebox("normal", type, Box("182936"));
            theme.SetStylebox("hover", type, Box("29434f", "609787"));
            theme.SetStylebox("pressed", type, Box("315b59", "87d9cc"));
            theme.SetStylebox("focus", type, Box("28454b", "87d9cc"));
        }
        theme.SetStylebox("normal", "LineEdit", Box("0e1c26"));
        theme.SetStylebox("focus", "LineEdit", Box("142c35", "87d9cc"));
        theme.SetStylebox("panel", "PopupMenu", Box("152631"));
        theme.SetConstant("separation", "VBoxContainer", 9);
        theme.SetConstant("separation", "HBoxContainer", 9);
        return theme;
    }
    private static void Clear(Node container) {
        foreach (Node child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }
    }
    private static VBoxContainer Column(Node parent, int separation = 9) {
        VBoxContainer column = new(); column.AddThemeConstantOverride("separation", separation); parent.AddChild(column); return column;
    }
    private static HBoxContainer Row(Node parent) {
        HBoxContainer row = new(); parent.AddChild(row); return row;
    }
    private static Label Label(Node parent, string text, int size = 14, Color? color = null) {
        Label label = new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size);
        if (color is { } tint) {
            label.AddThemeColorOverride("font_color", tint);
        }

        parent.AddChild(label); return label;
    }
    private Button Button(Node parent, string text, Action action, string tooltip = "") {
        Button button = new() { Text = text, TooltipText = tooltip, MouseDefaultCursorShape = CursorShape.PointingHand, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 36) };
        button.Pressed += () => this.Guard(action); parent.AddChild(button); return button;
    }
    private OptionButton Choice(Node parent, IEnumerable<string> entries, int value, Action<int> changed) {
        OptionButton choice = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 36), ClipText = true };
        foreach (string entry in entries) {
            choice.AddItem(entry);
        }

        choice.Select(value); choice.ItemSelected += index => this.Guard(() => changed((int)index)); parent.AddChild(choice); return choice;
    }
    private SpinBox Number(Node parent, string caption, double value, double minimum, double maximum, double step = 1) {
        HBoxContainer row = Row(parent);
        _ = Label(row, caption, 13, this.muted);
        SpinBox spin = new() { MinValue = minimum, MaxValue = maximum, Step = step, Value = value, CustomMinimumSize = new Vector2(100, 36) };
        row.AddChild(spin); return spin;
    }
    private static LineEdit TextField(Node parent, string value, string placeholder) {
        LineEdit field = new() { Text = value, PlaceholderText = placeholder, MaxLength = 60, CustomMinimumSize = new Vector2(0, 36) };
        parent.AddChild(field); return field;
    }
    private Label Heading(Node parent, string text) {
        HSeparator separator = new(); parent.AddChild(separator); return Label(parent, text, 12, this.accent);
    }
    private static VBoxContainer Sidebar(Node parent, int width) {
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(width, 0) };
        panel.AddThemeStyleboxOverride("panel", Box("101e28", "263b48", 8)); parent.AddChild(panel);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddChild(scroll);
        VBoxContainer column = Column(scroll); column.SizeFlagsHorizontal = SizeFlags.ExpandFill; return column;
    }
    private void BuildInterface() {
        MarginContainer margin = new(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) {
            margin.AddThemeConstantOverride("margin_" + side, 18);
        }

        this.AddChild(margin); this.workspaceRoot = margin; VBoxContainer layout = Column(margin, 12);
        HBoxContainer header = Row(layout);
        VBoxContainer brand = Column(header, 1); brand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _ = Label(brand, "A U T O M A T O U", 24, new Color("eef3ec"));
        this.screenTitle = Label(brand, "", 11, this.muted);
        this.Button(header, "Main menu", this.ShowMainMenu).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        this.transport = Row(layout);
        this.playButton = this.Button(this.transport, "▶  Run simulation", this.ToggleRun); this.playButton.CustomMinimumSize = new Vector2(175, 42);
        _ = this.Button(this.transport, "Step  →", () => { this.Pause(); this.Step(); }, "Advance exactly one complete turn. Shortcut: N");
        _ = this.Button(this.transport, "+10 turns", () => this.AdvanceTurns(10), "Pause and advance ten complete turns. Shortcut: B");
        _ = this.Choice(this.transport, ["1 turn / sec", "2 turns / sec", "4 turns / sec", "8 turns / sec"], 1, index => this.turnsPerSecond = Math.Pow(2, index));
        _ = this.Button(this.transport, "Frame world", () => this.board.Fit());
        this.turnLabel = Label(this.transport, "TURN 0000", 19, this.accent); this.turnLabel.HorizontalAlignment = HorizontalAlignment.Right;
        HBoxContainer workspace = Row(layout); workspace.SizeFlagsVertical = SizeFlags.ExpandFill;
        VBoxContainer toolsColumn = Column(workspace);
        this.modePillbox = new PanelContainer();
        this.modePillbox.AddThemeStyleboxOverride("panel", Box("101e28", "263b48", 8));
        toolsColumn.AddChild(this.modePillbox);
        this.modePanel = Column(this.modePillbox);
        this.toolsPanel = Sidebar(toolsColumn, 234);
        ((Control)this.toolsPanel.GetParent().GetParent()).SizeFlagsVertical = SizeFlags.ExpandFill;
        this.board = new HexBoard { World = this.world, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(340, 300) };
        workspace.AddChild(this.board);
        this.board.CellPressed = this.OnCell; this.board.Preview = this.PlacementPreview;
        this.board.HoverChanged = _ => this.RefreshHoverLabel();
        this.inspectorPanel = Sidebar(workspace, 282);
        PanelContainer footer = new(); footer.AddThemeStyleboxOverride("panel", Box("101e28")); layout.AddChild(footer);
        VBoxContainer foot = Column(footer, 4);
        HBoxContainer metrics = Row(foot);
        this.populationLabel = Label(metrics, "", 12, this.accent);
        this.hoverLabel = Label(metrics, "Hover a cell to inspect its coordinates", 12, this.muted); this.hoverLabel.HorizontalAlignment = HorizontalAlignment.Right;
        this.statusLabel = Label(foot, "Simulation paused.", 13, new Color("dce7de"));
        this.eventLabel = Label(foot, "", 12, this.muted); this.eventLabel.CustomMinimumSize = new Vector2(0, 34);
    }
    private void Guard(Action action) {
        try { action(); } catch (Exception exception) { this.Pause(); this.Status(exception.Message); this.Log(exception.ToString()); GD.PushWarning(exception.Message); }
    }
    private void Status(string message) { this.statusLabel.Text = message; if (this.menuStatus is not null && IsInstanceValid(this.menuStatus)) { this.menuStatus.Text = message; } this.Log(message); }
    private void Pause() { this.running = false; this.playButton.Text = "▶  Run simulation"; }
    private void ToggleRun() {
        if (this.mode != "World") {
            this.SwitchMode("World");
        }

        this.running = !this.running; this.elapsed = 0; this.playButton.Text = this.running ? "Ⅱ  Pause" : "▶  Run simulation";
        this.Status(this.running ? "Simulation running." : "Simulation paused.");
        this.BuildInspector();
    }
    private void Step() {
        this.CaptureLiveTuning();
        Entity[] actors = this.world.Entities.ToArray();
        Dictionary<int, (int ShotsFired, int IntentionChanges)> counters = actors.ToDictionary(entity => entity.Id, entity => (entity.ShotsFired, entity.Unit.AutomatonInstance.State.IntentionChanges));
        this.world.Step(); this.board.Flash();
        foreach (Entity entity in actors) {
            AutomatonMemory memory = entity.Unit.AutomatonInstance.State;
            this.experimentShots += entity.ShotsFired - counters[entity.Id].ShotsFired;
            this.experimentChanges += memory.IntentionChanges - counters[entity.Id].IntentionChanges;
            foreach (SensingReceipt call in entity.LastTurn.Sensing) {
                this.Log($"SENSE Turn={this.world.Turn} Id={entity.Id} Call={call.Call} Energy={call.EnergySpent} Remaining={call.RemainingEnergy} Received={call.Succeeded}");
            }
            foreach (ActionOutcome outcome in entity.LastTurn.Outcomes) {
                this.Log($"RESOLVE Turn={this.world.Turn} Id={entity.Id} Request={outcome.Action} Success={outcome.Succeeded} Energy={outcome.EnergySpent} Reason={outcome.Reason}");
            }
            if (memory.History.LastOrDefault() is { } decision && decision.Turn == this.world.Turn) {
                this.Log("DECISION " + $"Turn={this.world.Turn} Id={entity.Id} Name={entity.Name} Health={entity.Health} Heat={entity.Heat} Intention={decision.Intention} Reason={decision.Reason} Destination={decision.Destination}");
            }
        }
        if (this.selected is not null && !this.world.Entities.Contains(this.selected)) {
            this.selected = null;
        }

        this.Refresh();
    }
    public override void _Process(double delta) {
        if (!this.running) {
            return;
        }

        this.elapsed += delta;
        if (this.elapsed >= 1 / this.turnsPerSecond) { this.elapsed = 0; this.Guard(this.Step); }
    }
    public override void _UnhandledKeyInput(InputEvent @event) {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key || this.GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) {
            return;
        }
        if (key.Keycode == Key.Escape) {
            this.ShowMainMenu();
            return;
        }
        if (this.menuVisible) {
            return;
        }

        this.Guard(() => {
            switch (key.Keycode, this.mode) {
            case (Key.Space, "World"): this.ToggleRun(); break;
            case (Key.N, "World"): this.Pause(); this.Step(); break;
            case (Key.B, "World"): this.AdvanceTurns(10); break;
            case (Key.F, _): this.board.Fit(); break;
            case (Key.R, "World creator"): this.RotateSelection(); break;
            case (Key.Delete, "World creator") when this.Tool == "Inspect": this.DeleteSelection(); break;
            default:
                break;
            }
        });
    }
    private void SwitchMode(string value) {
        this.Pause();
        this.mode = value;
        this.menuVisible = false;
        this.menuRoot?.Hide();

        this.workspaceRoot.Show();
        this.screenTitle.Text = value;
        this.transport.Visible = this.mode == "World";
        if (this.mode == "World") {
            this.Tool = "Inspect";
        }

        Clear(this.toolsPanel);
        Clear(this.modePanel);
        this.toolChoice = null;
        this.fileTools = null;
        this.terrainTools = null;
        this.populationTools = null;
        this.modePillbox.Visible = this.mode == "World creator";
        if (this.mode == "World creator") {
            this.BuildWorldTools();
        } else if (this.mode == "World") {
            this.BuildPlaybackTools();
        }

        this.Refresh(); this.board.Fit();
        this.Status(this.mode == "World" ? "Simulation paused." : this.ToolInstruction());
        Callable.From(this.board.Fit).CallDeferred();
    }
    private void Refresh() {
        this.board.World = this.world; this.board.Selected = this.selected; this.board.QueueRedraw();
        this.turnLabel.Text = $"TURN {this.world.Turn:0000}";
        this.populationLabel.Text = $"{this.world.Terrain.Count:N0} CELLS     {this.world.Entities.Count} UNITS" +
            (this.mode == "World" ? $"     {this.world.Casualties} LOST" : "");
        this.RefreshHoverLabel();
        this.eventLabel.Text = string.Join("\n", this.world.Events.TakeLast(2));
        this.eventLabel.Visible = this.eventLabel.Text.Length > 0;
        this.RefreshComparison();
        this.BuildInspector();
    }
    private void RefreshHoverLabel() {
        this.hoverLabel?.Text = this.board.Hovered is { } cell
            ? $"CELL {cell}  ·  {(this.world.Terrain.TryGetValue(cell, out Terrain type) ? Catalog.TerrainNames[(int)type] : "Outside world")}"
            : "Hover a cell to inspect its coordinates";
    }
    private Entity? PlacementPreview(Hex cell) {
        return this.mode == "World creator" && this.Tool == "Place unit"
        ? new() { Position = cell, Facing = this.facing, Faction = this.faction, Unit = this.unitDesigns[this.unitIndex] }
        : null;
    }

    private void OnCell(Hex cell, MouseButton button) {
        this.Guard(() => {
            if (this.menuVisible || this.mode is not ("World" or "World creator")) {
                return;
            }

            if (button == MouseButton.Right || this.Tool == "Inspect") {
                this.Tool = "Inspect"; this.selected = this.world.At(cell); this.Refresh();
                this.Status(this.selected is { } inspected ? $"Inspecting #{inspected.Id} {inspected.Name}." : "No unit at this cell. Select a unit to inspect it.");
                return;
            }
            if (this.Tool == "File") {
                return;
            }
            this.Pause();
            bool changed = false;
            if (this.Tool == "Paint terrain") {
                Terrain? previous = this.world.Terrain.TryGetValue(cell, out Terrain value) ? value : null;
                if (!this.world.Paint(cell, this.terrain)) {
                    this.Status("Cannot paint here: outside world or terrain incompatible with the occupying unit.");
                } else {
                    changed = previous != this.terrain;
                    this.Status($"Painted {Catalog.TerrainNames[(int)this.terrain].ToLowerInvariant()} at {cell}.");
                }
            } else if (this.Tool == "Erase entity") {
                if (this.world.At(cell) is { } entity) {
                    this.world.Remove(entity); if (this.selected == entity) {
                        this.selected = null;
                    }
                    changed = true;
                    this.Status($"Removed {entity.Name} at {cell}.");
                }
            } else if (this.PlacementPreview(cell) is { } entity) {
                entity.Unit = entity.Unit.CreateFresh();
                if (!this.world.Add(entity, out string reason)) {
                    this.Status(reason);
                } else { changed = true; this.selected = entity; this.Status($"Placed {entity.Name} at {cell}."); }
            }
            if (changed) {
                this.RecordWorldEdit();
            } else {
                this.Refresh();
            }
        });
    }
    private void RotateSelection() {
        if (this.mode != "World creator") {
            return;
        }

        if (this.Tool == "Inspect" && this.selected is not null) {
            this.Pause();
            // Regular hexagonal footprints occupy the same cells in every facing.
            this.selected.Facing = (this.selected.Facing + 1) % 6;
            this.Status($"{this.selected.Name} facing: {Hex.DirectionNames[this.selected.Facing]}.");
            this.RecordWorldEdit();
        } else if (this.Tool == "Place unit") {
            this.facing = (this.facing + 1) % 6;
            this.Status($"Placement facing: {Hex.DirectionNames[this.facing]}. R rotates by 60°."); this.Refresh();
        }
    }
    private void DeleteSelection() {
        if (this.selected is null) {
            return;
        }

        this.Pause();
        this.Status($"Removed {this.selected.Name} at {this.selected.Position}.");
        this.world.Remove(this.selected); this.selected = null; this.RecordWorldEdit();
    }
    private void RecordWorldEdit() {
        if (this.world.Turn == 0) {
            this.checkpoint = this.world.Copy();
            this.checkpointShots = this.experimentShots; this.checkpointChanges = this.experimentChanges;
            this.ResetTuningCache();
            this.referenceResult = null;
        }
        this.Refresh();
    }
    private void ReplaceWorld(World replacement, bool capture = true) {
        this.Pause(); this.world = replacement; this.selected = null; this.board.World = this.world;
        this.ResetTuningCache();
        this.world.EventRecorded = this.Log;
        foreach (string entry in this.world.Events) {
            this.Log(entry);
        }

        this.experimentShots = this.world.Entities.Sum(entity => entity.ShotsFired);
        this.experimentChanges = this.world.Entities.Sum(entity => entity.Unit.AutomatonInstance.State.IntentionChanges);
        if (capture) {
            this.checkpoint = this.world.Copy();
            this.checkpointShots = this.experimentShots; this.checkpointChanges = this.experimentChanges;
        }
        this.Refresh(); this.board.Fit(); this.Status("Simulation paused.");
    }
    private void BuildInspector() {
        Clear(this.inspectorPanel);
        if (this.mode == "World" || this.Tool == "Inspect") {
            _ = Label(this.inspectorPanel, "UNIT INSPECTOR", 12, this.accent);
            if (this.selected is null) {
                _ = Label(this.inspectorPanel, "Select a unit on the board to inspect it.", 13, this.muted);
            } else {
                _ = Label(this.inspectorPanel, this.selected.Name, 21, new Color(Catalog.FactionColors[(int)this.selected.Faction]));
                _ = Label(this.inspectorPanel, Catalog.FactionNames[(int)this.selected.Faction], 13, this.muted);
                _ = Label(this.inspectorPanel, $"Health {this.selected.Health} / {this.selected.MaximumHealth}\nHeat {this.selected.Heat} / 100{(this.selected.WeaponLocked ? " · LOCKED" : "")}", 13, this.muted);
                this.BuildDecisionInspector(this.selected);
            }
        }

        _ = this.Heading(this.inspectorPanel, "FACTIONS / LIVE POPULATION");
        for (int i = 0; i < Catalog.FactionNames.Length; i++) {
            int count = this.world.Entities.Count(e => (int)e.Faction == i);
            _ = Label(this.inspectorPanel, $"●  {Catalog.FactionNames[i]}   {count}", 13, new Color(Catalog.FactionColors[i]));
        }
        _ = this.Heading(this.inspectorPanel, "SHORTCUTS");
        string shortcuts = this.mode == "World" ? "Space   Run / pause\nN          Single turn\nB          Ten turns\n" : this.Tool switch {
            "Place unit" => "R          Rotate placement\n",
            "Inspect" when this.selected is not null => "R          Rotate selected unit\nDelete   Remove selected unit\n",
            _ => ""
        };
        Label(this.inspectorPanel, shortcuts + "F          Frame world\nEsc       Main menu\nRight click   Inspect a unit", 12, this.muted).Name = "ShortcutHelp";
    }
}

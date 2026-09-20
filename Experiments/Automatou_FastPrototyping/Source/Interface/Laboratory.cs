using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory : Control
{
    private World world = World.Demonstration();
    private HexBoard board = null!;
    private VBoxContainer toolsPanel = null!;
    private VBoxContainer inspectorPanel = null!;
    private Label turnLabel = null!, statusLabel = null!, eventLabel = null!, populationLabel = null!;
    private Button playButton = null!;
    private readonly List<UnitDesign> unitDesigns = Catalog.Units();
    private readonly List<BuildingDesign> buildingDesigns = [Catalog.Outpost()];
    private string mode = "World";
    private string currentTool = "Inspect";
    private OptionButton? toolChoice;
    private string tool
    {
        get => currentTool;
        set
        {
            currentTool = value;
            if (toolChoice is not null && IsInstanceValid(toolChoice)) toolChoice.Select(Array.IndexOf(ToolNames, value));
        }
    }
    private static readonly string[] ToolNames = ["Inspect", "Paint terrain", "Place unit", "Place building", "Erase entity"];
    private bool running;
    private double elapsed;
    private double turnsPerSecond = 2;
    private int facing;
    private int unitIndex, buildingIndex;
    private Faction faction = Faction.Bastions;
    private Terrain terrain = Terrain.Forest;
    private Entity? selected;
    private string checkpoint = "";
    private string contentRoot = "";
    private string sessionRoot = "";
    private Label? hoverLabel;
    private LineEdit worldName = null!;
    private OptionButton unitChoice = null!, buildingChoice = null!;
    private readonly Dictionary<string, Button> modeButtons = [];
    private readonly Color muted = new("93a8b7");
    private readonly Color accent = new("87d9cc");

    public override void _Ready()
    {
        DisplayServer.WindowSetMinSize(new Vector2I(1100, 700));
        contentRoot = ProjectSettings.GlobalizePath("res://UserContent");
        sessionRoot = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--session-log="))?[14..]
            ?? System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://MyLogOutput"), DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        System.IO.Directory.CreateDirectory(sessionRoot);
        Log("Application started. Godot 4.7.2 / .NET 10.");
        world.EventRecorded = Log;
        foreach (string entry in world.Events) Log(entry);
        LoadLibrary();
        Theme = CreateTheme();
        BuildInterface();
        checkpoint = Storage.Encode(world);
        SwitchMode("World");
        Refresh();
        Callable.From(() => board.Fit()).CallDeferred();
        if (OS.GetCmdlineUserArgs().Contains("--verify-interface")) Callable.From(RunInterfaceVerification).CallDeferred();
    }
    private void Log(string message) => System.IO.File.AppendAllText(System.IO.Path.Combine(sessionRoot, "Session.log"), $"{DateTime.Now:HH:mm:ss.fff} {message}{System.Environment.NewLine}");
    private void LoadLibrary()
    {
        foreach (string kind in new[] { "Units", "Buildings" })
        {
            string directory = System.IO.Path.Combine(contentRoot, kind);
            System.IO.Directory.CreateDirectory(directory);
            foreach (string file in System.IO.Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    if (kind == "Units")
                    {
                        var design = Storage.LoadDesign<UnitDesign>(file); design.Validate();
                        int index = unitDesigns.FindIndex(d => d.Name == design.Name);
                        if (index < 0) unitDesigns.Add(design); else unitDesigns[index] = design;
                    }
                    else
                    {
                        var design = Storage.LoadDesign<BuildingDesign>(file); design.Validate();
                        int index = buildingDesigns.FindIndex(d => d.Name == design.Name);
                        if (index < 0) buildingDesigns.Add(design); else buildingDesigns[index] = design;
                    }
                }
                catch (Exception exception) { Log($"Skipped invalid blueprint {file}: {exception.Message}"); }
        }
    }
    private static StyleBoxFlat Box(string background, string border = "263b48", int radius = 6)
    {
        var box = new StyleBoxFlat { BgColor = new Color(background), BorderColor = new Color(border) };
        box.SetBorderWidthAll(1); box.SetCornerRadiusAll(radius);
        box.ContentMarginLeft = 12; box.ContentMarginRight = 12; box.ContentMarginTop = 9; box.ContentMarginBottom = 9;
        return box;
    }
    private Theme CreateTheme()
    {
        var theme = new Theme { DefaultFontSize = 14 };
        theme.SetColor("font_color", "Label", new Color("deeaed"));
        theme.SetColor("font_color", "Button", new Color("c5d8dd"));
        foreach (string type in new[] { "Button", "OptionButton" })
        {
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
    private static void Clear(Node container)
    {
        foreach (Node child in container.GetChildren()) { container.RemoveChild(child); child.QueueFree(); }
    }
    private static VBoxContainer Column(Node parent, int separation = 9)
    {
        var column = new VBoxContainer(); column.AddThemeConstantOverride("separation", separation); parent.AddChild(column); return column;
    }
    private static HBoxContainer Row(Node parent)
    {
        var row = new HBoxContainer(); parent.AddChild(row); return row;
    }
    private Label Label(Node parent, string text, int size = 14, Color? color = null)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size);
        if (color is { } tint) label.AddThemeColorOverride("font_color", tint);
        parent.AddChild(label); return label;
    }
    private Button Button(Node parent, string text, Action action, string tooltip = "")
    {
        var button = new Button { Text = text, TooltipText = tooltip, MouseDefaultCursorShape = CursorShape.PointingHand, SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 36) };
        button.Pressed += () => Guard(action); parent.AddChild(button); return button;
    }
    private OptionButton Choice(Node parent, IEnumerable<string> entries, int value, Action<int> changed)
    {
        var choice = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 36), ClipText = true };
        foreach (string entry in entries) choice.AddItem(entry);
        choice.Select(value); choice.ItemSelected += index => Guard(() => changed((int)index)); parent.AddChild(choice); return choice;
    }
    private SpinBox Number(Node parent, string caption, double value, double minimum, double maximum)
    {
        var row = Row(parent);
        Label(row, caption, 13, muted);
        var spin = new SpinBox { MinValue = minimum, MaxValue = maximum, Step = 1, Value = value, CustomMinimumSize = new Vector2(100, 36) };
        row.AddChild(spin); return spin;
    }
    private LineEdit TextField(Node parent, string value, string placeholder)
    {
        var field = new LineEdit { Text = value, PlaceholderText = placeholder, MaxLength = 60, CustomMinimumSize = new Vector2(0, 36) };
        parent.AddChild(field); return field;
    }
    private void Heading(Node parent, string text)
    {
        var separator = new HSeparator(); parent.AddChild(separator); Label(parent, text, 12, accent);
    }
    private VBoxContainer Sidebar(Node parent, int width)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(width, 0) };
        panel.AddThemeStyleboxOverride("panel", Box("101e28", "263b48", 8)); parent.AddChild(panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddChild(scroll);
        var column = Column(scroll); column.SizeFlagsHorizontal = SizeFlags.ExpandFill; return column;
    }
    private void BuildInterface()
    {
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 18);
        AddChild(margin); var layout = Column(margin, 12);
        var header = Row(layout);
        var brand = Column(header, 1); brand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Label(brand, "A U T O M A T O U", 24, new Color("eef3ec"));
        Label(brand, "WORLD LABORATORY   /   FAST PROTOTYPING", 11, muted);
        foreach (string name in new[] { "World", "Unit creator", "Building creator" })
        {
            var button = Button(header, name, () => SwitchMode(name)); button.CustomMinimumSize = new Vector2(140, 44); button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            modeButtons[name] = button;
        }
        var transport = Row(layout);
        playButton = Button(transport, "▶  Run automata", ToggleRun); playButton.CustomMinimumSize = new Vector2(175, 42);
        Button(transport, "Step  →", Step, "Advance exactly one complete turn. Shortcut: N");
        Choice(transport, ["1 turn / sec", "2 turns / sec", "4 turns / sec", "8 turns / sec"], 1, index => turnsPerSecond = Math.Pow(2, index));
        Button(transport, "Checkpoint", () => { checkpoint = Storage.Encode(world); Status("Checkpoint captured. Rewind will return here."); });
        Button(transport, "↶  Rewind", () => ReplaceWorld(Storage.Decode(checkpoint), false));
        Button(transport, "Frame world", () => board.Fit());
        turnLabel = Label(transport, "TURN 0000", 19, accent); turnLabel.HorizontalAlignment = HorizontalAlignment.Right;
        var workspace = Row(layout); workspace.SizeFlagsVertical = SizeFlags.ExpandFill;
        toolsPanel = Sidebar(workspace, 246);
        board = new HexBoard { World = world, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(340, 300) };
        workspace.AddChild(board);
        board.CellPressed = OnCell; board.Preview = PlacementPreview;
        board.HoverChanged = cell => { if (hoverLabel is not null) hoverLabel.Text = $"CELL {cell}  ·  {(world.Terrain.TryGetValue(cell, out var type) ? Catalog.TerrainNames[(int)type] : "Outside world")}"; };
        inspectorPanel = Sidebar(workspace, 262);
        var footer = new PanelContainer(); footer.AddThemeStyleboxOverride("panel", Box("101e28")); layout.AddChild(footer);
        var foot = Column(footer, 4);
        var metrics = Row(foot);
        populationLabel = Label(metrics, "", 12, accent);
        hoverLabel = Label(metrics, "Hover a cell to inspect its coordinates", 12, muted); hoverLabel.HorizontalAlignment = HorizontalAlignment.Right;
        statusLabel = Label(foot, "Ready. Shape the world, then start the automata.", 13, new Color("dce7de"));
        eventLabel = Label(foot, "", 12, muted); eventLabel.CustomMinimumSize = new Vector2(0, 34);
    }
    private void Guard(Action action)
    {
        try { action(); }
        catch (Exception exception) { Pause(); Status(exception.Message); Log(exception.ToString()); GD.PushWarning(exception.Message); }
    }
    private void Status(string message) { statusLabel.Text = message; Log(message); }
    private void Pause() { running = false; playButton.Text = "▶  Run automata"; }
    private void ToggleRun()
    {
        if (mode != "World") SwitchMode("World");
        running = !running; elapsed = 0; playButton.Text = running ? "Ⅱ  Pause" : "▶  Run automata";
        Status(running ? "Automata running. Every faction acts independently." : "Paused. You can edit the world.");
    }
    private void Step()
    {
        world.Step(); board.Flash();
        if (selected is not null && !world.Entities.Contains(selected)) selected = null;
        Refresh();
    }
    public override void _Process(double delta)
    {
        if (!running) return;
        elapsed += delta;
        if (elapsed >= 1 / turnsPerSecond) { elapsed = 0; Guard(Step); }
    }
    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        Guard(() =>
        {
            switch (key.Keycode)
            {
                case Key.Space: ToggleRun(); break;
                case Key.N: Pause(); Step(); break;
                case Key.F: board.Fit(); break;
                case Key.R: RotateSelection(); break;
                case Key.Delete: DeleteSelection(); break;
                case Key.Escape: tool = "Inspect"; selected = null; SwitchMode("World"); break;
            }
        });
    }
    private void SwitchMode(string value)
    {
        Pause();
        if (value != mode) RememberDraft();
        mode = value; board.BuildingMode = mode == "Building creator"; board.UnitMode = mode == "Unit creator";
        foreach (var pair in modeButtons) pair.Value.Modulate = pair.Key == value ? accent : Colors.White;
        Clear(toolsPanel);
        if (mode == "World") BuildWorldTools();
        else if (mode == "Unit creator") BuildUnitCreator();
        else BuildBuildingCreator();
        BuildInspector(); board.Fit(); board.QueueRedraw();
    }
    private void Refresh()
    {
        board.World = world; board.Selected = selected; board.QueueRedraw();
        turnLabel.Text = $"TURN {world.Turn:0000}";
        populationLabel.Text = $"{world.Terrain.Count:N0} CELLS     {world.Entities.Count(e => e.Unit is not null)} UNITS     {world.Entities.Count(e => e.Building is not null)} BUILDINGS     {world.Casualties} LOST";
        eventLabel.Text = string.Join("\n", world.Events.TakeLast(2));
        BuildInspector();
    }
    private Entity? PlacementPreview(Hex cell) => mode == "World" && tool is "Place unit" or "Place building"
        ? new() { Position = cell, Facing = facing, Faction = faction, Unit = tool == "Place unit" ? unitDesigns[unitIndex] : null, Building = tool == "Place building" ? buildingDesigns[buildingIndex] : null }
        : null;
    private void OnCell(Hex cell, MouseButton button)
    {
        Guard(() =>
        {
            if (mode == "Building creator") { EditBuilding(cell, button); return; }
            if (mode != "World") return;
            if (button == MouseButton.Right) { tool = "Inspect"; selected = world.At(cell); Refresh(); return; }
            if (tool == "Inspect") { selected = world.At(cell); Refresh(); return; }
            Pause();
            if (tool == "Paint terrain")
            {
                if (!world.Paint(cell, terrain)) Status("Cannot paint here: outside world or terrain incompatible with the occupying unit.");
            }
            else if (tool == "Erase entity")
            {
                if (world.At(cell) is { } entity) { world.Remove(entity); if (selected == entity) selected = null; }
            }
            else if (PlacementPreview(cell) is { } entity)
            {
                entity.Unit = entity.Unit?.Copy(); entity.Building = entity.Building?.Copy();
                if (!world.Add(entity, out string reason)) Status(reason);
                else { selected = entity; Status($"Placed {entity.Name} at {cell}."); }
            }
            if (world.Turn == 0) checkpoint = Storage.Encode(world);
            Refresh();
        });
    }
    private void RotateSelection()
    {
        Pause(); facing = (facing + 1) % 6;
        if (mode == "Unit creator") { UpdateUnitPreview(false); return; }
        if (tool == "Inspect" && selected is not null)
        {
            int rotation = (selected.Facing + 1) % 6;
            if (world.CanOccupy(selected, selected.Position, rotation, out string reason)) { selected.Facing = rotation; world.RebuildOccupancy(); }
            else { Status(reason); return; }
        }
        Status($"Placement facing: {Hex.DirectionNames[facing]}. R rotates by 60°."); Refresh();
    }
    private void DeleteSelection()
    {
        if (selected is null) return;
        Pause(); world.Remove(selected); selected = null; Refresh();
    }
    private void ReplaceWorld(World replacement, bool capture = true)
    {
        Pause(); world = replacement; selected = null; board.World = world;
        world.EventRecorded = Log;
        foreach (string entry in world.Events) Log(entry);
        if (capture) checkpoint = Storage.Encode(world);
        Refresh(); board.Fit(); Status("World ready. Automata paused.");
    }
    private void BuildInspector()
    {
        Clear(inspectorPanel);
        Label(inspectorPanel, mode == "Building creator" ? "DESIGN NOTES" : "WORLD TELEMETRY", 12, accent);
        if (mode == "Unit creator" && board.PreviewUnit?.Unit is { } preview)
        {
            Label(inspectorPanel, "Simple rules.\nComplex encounters.", 21);
            Label(inspectorPanel, "The preview updates as you edit. Unit size determines the complete occupied footprint; the origin is always its center.", 13, muted);
            Heading(inspectorPanel, "DIRECTIONAL COMBAT");
            Label(inspectorPanel, "The arrow marks the facing direction. Gold cells show the forward attack region. Adjacent attacks use melee damage; other attacks use ranged damage.", 13, muted);
            Heading(inspectorPanel, "ACTION ECONOMY");
            Label(inspectorPanel, "Turn 60°: 1 point\nMove forward: 1 point\nRough ground: 2 points\nAttack: 2 points, once per turn", 13);
            Label(inspectorPanel, "Infantry & artillery units cross rough ground for 1 point and lose 2 health. Unused points expire each turn.", 12, muted);
            Heading(inspectorPanel, "DEFENSE BY DIRECTION");
            Label(inspectorPanel, $"Front armor: {preview.Armor}\nFront sides: {preview.Armor * 2 / 3}\nRear & rear sides: {preview.Armor / 4}", 13);
            Heading(inspectorPanel, "EXPERIMENT FREELY");
            Label(inspectorPanel, "Any faction can use any blueprint. Existing units keep their original design when you save changes. A blast radius above zero causes ranged attacks to damage allies as well as enemies.", 12, muted);
            return;
        }
        if (mode == "Building creator")
        {
            Label(inspectorPanel, "A place to take shape.", 21);
            Label(inspectorPanel, "Paint a connected footprint. Right-click to remove cells. The gold cross is the origin used for placement and rotation.", 13, muted);
            Heading(inspectorPanel, "EXPANDING THE WORKSPACE");
            Label(inspectorPanel, "Click a border cell to add a patch on that side. Patch width and height are adjustable below the health field. Opening a design expands the workspace until every cell and its saved pivot fit.", 13, muted);
            Heading(inspectorPanel, "ONE SHARED HEALTH POOL");
            Label(inspectorPanel, "Buildings cover the underlying terrain. Destruction removes the whole footprint and reveals that terrain again.", 13, muted);
            return;
        }
        if (selected is null)
        {
            Label(inspectorPanel, "Watch a world unfold.", 21);
            Label(inspectorPanel, "Choose Inspect and select any unit or building. You are the observer of every faction.", 13, muted);
        }
        else
        {
            Label(inspectorPanel, selected.Name, 21, new Color(Catalog.FactionColors[(int)selected.Faction]));
            Label(inspectorPanel, Catalog.FactionNames[(int)selected.Faction], 13, muted);
            Heading(inspectorPanel, "ENTITY");
            Label(inspectorPanel, $"Origin     {selected.Position}\nFacing    {Hex.DirectionNames[selected.Facing]}\nHealth    {selected.Health} / {selected.MaximumHealth}\nFootprint {selected.OccupiedCells().Count()} cells", 14);
            if (selected.Unit is { } unit)
            {
                Label(inspectorPanel, $"{unit.Automaton} automaton\n{unit.ActionPoints} action points / turn\n{unit.Damage} ranged · {unit.MeleeDamage} melee\n{unit.Range} range · {unit.Armor} front armor\n{unit.Evasion}% evasion · {unit.Mobility}", 13, muted);
                Label(inspectorPanel, "Gold cells show the forward attack region. Rear attacks bypass most armor.", 12, muted);
                Button(inspectorPanel, selected.Stationary ? "Mobilize unit" : "Deploy / hold position", () => { Pause(); selected.Stationary = !selected.Stationary; Refresh(); });
            }
            Button(inspectorPanel, "Rotate 60°   [R]", RotateSelection);
            Button(inspectorPanel, "Remove entity   [Delete]", DeleteSelection);
        }
        Heading(inspectorPanel, "FACTIONS / LIVE POPULATION");
        for (int i = 0; i < Catalog.FactionNames.Length; i++)
        {
            int count = world.Entities.Count(e => (int)e.Faction == i && e.Unit is not null);
            Label(inspectorPanel, $"●  {Catalog.FactionNames[i]}   {count}", 13, new Color(Catalog.FactionColors[i]));
        }
        Heading(inspectorPanel, "LABORATORY CONTROLS");
        Label(inspectorPanel, "Space   Run / pause\nN          Single turn\nR          Rotate selection / placement\nF          Frame world\nEsc       Inspect mode\nRight click   Inspect a cell", 12, muted);
        Heading(inspectorPanel, "AUTOMATA");
        Label(inspectorPanel, "Bastions advance under heavy armor. Travelers strike and retreat. Walkers close to medium range. Clones rush through rough ground at a cost; artillery can hit allies. Prytu swarm weakened targets.", 12, muted);
    }
}

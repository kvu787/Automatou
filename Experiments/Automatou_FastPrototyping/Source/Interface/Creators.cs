using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
    private readonly Dictionary<string, SpinBox> unitNumbers = [];
    private LineEdit unitName = null!, buildingName = null!;
    private OptionButton behaviorChoice = null!, mobilityChoice = null!;
    private SpinBox buildingHealth = null!, patchWidth = null!, patchHeight = null!;
    private bool choosingPivot;
    private Label buildingSummary = null!, unitSummary = null!;
    private UnitDesign? unitDraft;
    private BuildingDesign? buildingDraft;
    private bool buildingOpened;
    private string worldSlotName = "My world";

    private void BuildWorldTools()
    {
        Label(toolsPanel, "WORLD TOOLS", 12, accent);
        Label(toolsPanel, "Set the conditions.", 21);
        toolChoice = Choice(toolsPanel, ToolNames, Array.IndexOf(ToolNames, tool), index =>
        {
            tool = ToolNames[index];
            Status($"{tool} tool selected. Click the world to use it."); board.QueueRedraw();
        });
        Heading(toolsPanel, "TERRAIN BRUSH");
        Choice(toolsPanel, Catalog.TerrainNames, (int)terrain, index => { terrain = (Terrain)index; tool = "Paint terrain"; Status($"Painting {Catalog.TerrainNames[index].ToLowerInvariant()}. Drag across cells."); });
        Heading(toolsPanel, "POPULATE THE WORLD");
        Choice(toolsPanel, Catalog.FactionNames, (int)faction, index => { faction = (Faction)index; board.QueueRedraw(); });
        unitChoice = Choice(toolsPanel, unitDesigns.Select(u => u.Name), unitIndex, index => { unitIndex = index; tool = "Place unit"; board.QueueRedraw(); });
        Button(toolsPanel, "Place selected unit", () => { tool = "Place unit"; Status("Click the world to place units. Red footprints cannot be placed."); });
        buildingChoice = Choice(toolsPanel, buildingDesigns.Select(b => b.Name), buildingIndex, index => { buildingIndex = index; tool = "Place building"; board.QueueRedraw(); });
        Button(toolsPanel, "Place selected building", () => { tool = "Place building"; Status("Click the world to place buildings. R rotates about the saved pivot."); });
        Button(toolsPanel, "Rotate placement   [R]", RotateSelection);
        var coordinates = new CheckButton { Text = "Show cell coordinates", ButtonPressed = board.Coordinates };
        coordinates.Toggled += value => { board.Coordinates = value; board.QueueRedraw(); }; toolsPanel.AddChild(coordinates);
        Heading(toolsPanel, "WORLD FILE");
        worldName = TextField(toolsPanel, worldSlotName, "World name");
        worldName.TextChanged += value => worldSlotName = value;
        var persistence = Row(toolsPanel);
        Button(persistence, "Save", () =>
        {
            string path = WorldPath(); Storage.SaveWorld(path, world); Status($"Saved world: {worldName.Text}. Stored in UserContent / Worlds.");
        });
        Button(persistence, "Load", () => ReplaceWorld(Storage.LoadWorld(WorldPath())));
        string directory = System.IO.Path.Combine(contentRoot, "Worlds");
        System.IO.Directory.CreateDirectory(directory);
        var files = System.IO.Directory.GetFiles(directory, "*.json").Select(System.IO.Path.GetFileNameWithoutExtension).OfType<string>().Order().ToArray();
        if (files.Length > 0) Choice(toolsPanel, ["Saved worlds…", .. files], 0, index => { if (index > 0) { worldName.Text = files[index - 1]; worldSlotName = worldName.Text; } });
        Heading(toolsPanel, "NEW WORLD");
        var shape = Choice(toolsPanel, ["Rectangle", "Hexagon"], 0, _ => { });
        var width = Number(toolsPanel, "Width / size", 34, 1, 200);
        var height = Number(toolsPanel, "Height", 24, 1, 200);
        shape.ItemSelected += index => height.Editable = index == 0;
        var seed = Number(toolsPanel, "Terrain seed", 72491, 1, 999999);
        var newWorld = Row(toolsPanel);
        Button(newWorld, "Blank", () => ReplaceWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, false, (int)seed.Value)));
        Button(newWorld, "Generate", () => ReplaceWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, true, (int)seed.Value)));
        Label(toolsPanel, "New worlds replace this workspace. Save or checkpoint first. Hexagon size 1 is one cell.", 12, muted);
        Button(toolsPanel, "Load five-faction encounter", () => ReplaceWorld(World.Demonstration()));
    }
    private string WorldPath() => System.IO.Path.Combine(contentRoot, "Worlds", Storage.FileName(worldName.Text) + ".json");
    private void BuildUnitCreator()
    {
        UnitDesign design = unitDraft ?? unitDesigns[unitIndex];
        Label(toolsPanel, "UNIT CREATOR", 12, accent);
        Label(toolsPanel, "Give it a purpose.", 21);
        Choice(toolsPanel, unitDesigns.Select(u => u.Name), unitIndex, index => { unitIndex = index; unitDraft = null; SwitchMode("Unit creator"); });
        unitName = TextField(toolsPanel, design.Name, "Unit name");
        Button(toolsPanel, "Save & place in world", () => { SaveUnit(); tool = "Place unit"; SwitchMode("World"); });
        Heading(toolsPanel, "BODY & COMBAT");
        unitNumbers.Clear();
        unitNumbers["Size"] = Number(toolsPanel, "Size", design.Size, 1, 12);
        unitSummary = Label(toolsPanel, $"{1 + 3 * design.Size * (design.Size - 1)} occupied cells", 12, accent);
        unitNumbers["Size"].ValueChanged += value => unitSummary.Text = $"{1 + 3 * value * (value - 1)} occupied cells";
        unitNumbers["Health"] = Number(toolsPanel, "Health", design.Health, 1, 10000);
        unitNumbers["Armor"] = Number(toolsPanel, "Front armor", design.Armor, 0, 1000);
        unitNumbers["Damage"] = Number(toolsPanel, "Ranged damage", design.Damage, 1, 1000);
        unitNumbers["MeleeDamage"] = Number(toolsPanel, "Melee damage", design.MeleeDamage, 1, 1000);
        unitNumbers["Range"] = Number(toolsPanel, "Attack range", design.Range, 1, 30);
        unitNumbers["ActionPoints"] = Number(toolsPanel, "Action points", design.ActionPoints, 1, 20);
        unitNumbers["Evasion"] = Number(toolsPanel, "Evasion %", design.Evasion, 0, 90);
        unitNumbers["BlastRadius"] = Number(toolsPanel, "Blast radius", design.BlastRadius, 0, 3);
        Heading(toolsPanel, "AUTOMATON & MOVEMENT");
        behaviorChoice = Choice(toolsPanel, Enum.GetNames<Automaton>(), (int)design.Automaton, _ => { });
        mobilityChoice = Choice(toolsPanel, Enum.GetNames<Mobility>(), (int)design.Mobility, _ => { });
        Label(toolsPanel, "Advance: close and fight. Skirmish: strike then retreat. Hold: defend current ground. Artillery: maintain distance. Swarm: prefer weakened prey.", 12, muted);
        Button(toolsPanel, "Save unit blueprint", SaveUnit);
        Button(toolsPanel, "Save & place in world", () => { SaveUnit(); tool = "Place unit"; SwitchMode("World"); });
        Label(toolsPanel, "A blueprint is independent of faction. Select its faction when placing. Saving affects future placements.", 12, muted);
        foreach (var spin in unitNumbers.Values) spin.ValueChanged += _ => UpdateUnitPreview();
        unitName.TextChanged += _ => UpdateUnitPreview(false);
        behaviorChoice.ItemSelected += _ => UpdateUnitPreview(false);
        mobilityChoice.ItemSelected += _ => UpdateUnitPreview(false);
        UpdateUnitPreview();
    }
    private UnitDesign ReadUnitDraft()
    {
        int Value(string name) => (int)unitNumbers[name].Value;
        return new UnitDesign
        {
            Name = unitName.Text.Trim(), Size = Value("Size"), Health = Value("Health"), Armor = Value("Armor"), Damage = Value("Damage"), MeleeDamage = Value("MeleeDamage"),
            Range = Value("Range"), ActionPoints = Value("ActionPoints"), Evasion = Value("Evasion"), BlastRadius = Value("BlastRadius"),
            Automaton = (Automaton)behaviorChoice.Selected, Mobility = (Mobility)mobilityChoice.Selected
        };
    }
    private void UpdateUnitPreview(bool fit = true)
    {
        var design = ReadUnitDraft();
        board.PreviewUnit = new Entity { Unit = design, Faction = faction, Facing = facing, Health = design.Health };
        if (fit) board.Fit();
        board.QueueRedraw(); BuildInspector();
    }
    private void SaveUnit()
    {
        var design = ReadUnitDraft();
        design.Validate();
        Storage.SaveDesign(System.IO.Path.Combine(contentRoot, "Units", Storage.FileName(design.Name) + ".json"), design);
        int index = unitDesigns.FindIndex(d => d.Name == design.Name);
        if (index >= 0) { unitDesigns[index] = design; unitIndex = index; }
        else { unitIndex = unitDesigns.Count; unitDesigns.Add(design); }
        unitDraft = design.Copy();
        UpdateUnitPreview(false);
        Status($"Saved {design.Name}. Blueprint ready for placement.");
    }
    private void OpenBuilding(BuildingDesign design)
    {
        buildingOpened = true;
        board.BuildingOrigin = design.EditorOrigin;
        board.BuildingCells = design.Cells.Select(c => c + design.EditorOrigin).ToHashSet();
        int lowX = Math.Min(0, Math.Min(design.EditorOrigin.X, board.BuildingCells.Min(c => c.X)));
        int lowY = Math.Min(0, Math.Min(design.EditorOrigin.Y, board.BuildingCells.Min(c => c.Y)));
        int highX = Math.Max(19, Math.Max(design.EditorOrigin.X, board.BuildingCells.Max(c => c.X)));
        int highY = Math.Max(9, Math.Max(design.EditorOrigin.Y, board.BuildingCells.Max(c => c.Y)));
        int x = (int)Math.Floor((double)lowX / design.PatchWidth) * design.PatchWidth;
        int y = (int)Math.Floor((double)lowY / design.PatchHeight) * design.PatchHeight;
        board.EditorBounds = new Rect2I(x, y, (int)Math.Ceiling((highX - x + 1.0) / design.PatchWidth) * design.PatchWidth, (int)Math.Ceiling((highY - y + 1.0) / design.PatchHeight) * design.PatchHeight);
        board.Fit();
    }
    private void BuildBuildingCreator()
    {
        BuildingDesign design = buildingDraft ?? buildingDesigns[buildingIndex];
        // Retain unsaved drawing when changing tabs; choosing a blueprint explicitly opens it.
        if (!buildingOpened) OpenBuilding(design);
        Label(toolsPanel, "BUILDING CREATOR", 12, accent);
        Label(toolsPanel, "Make your mark.", 21);
        Choice(toolsPanel, buildingDesigns.Select(b => b.Name), buildingIndex, index => { buildingIndex = index; buildingDraft = null; OpenBuilding(buildingDesigns[index]); SwitchMode("Building creator"); });
        buildingName = TextField(toolsPanel, design.Name, "Building name");
        Button(toolsPanel, "Save & place in world", () => { SaveBuilding(); tool = "Place building"; SwitchMode("World"); });
        buildingHealth = Number(toolsPanel, "Shared health", design.Health, 1, 10000);
        patchWidth = Number(toolsPanel, "Patch width", design.PatchWidth, 2, 100);
        patchHeight = Number(toolsPanel, "Patch height", design.PatchHeight, 2, 100);
        Heading(toolsPanel, "FOOTPRINT");
        Button(toolsPanel, "Paint occupied cells", () => { choosingPivot = false; Status("Click or drag to add building cells. Right-click removes cells."); });
        Button(toolsPanel, "Set origin / pivot", () => { choosingPivot = true; Status("Click a cell to choose the building origin."); });
        Button(toolsPanel, "Clear footprint", () => { board.BuildingCells.Clear(); UpdateBuildingSummary(); board.QueueRedraw(); });
        buildingSummary = Label(toolsPanel, "", 13, accent); UpdateBuildingSummary();
        Label(toolsPanel, "The starting grid is 20 × 10. Clicking an edge expands that side by the chosen patch size.", 12, muted);
        Heading(toolsPanel, "SAVE YOUR DESIGN");
        Button(toolsPanel, "Save building blueprint", SaveBuilding);
        Button(toolsPanel, "Save & place in world", () => { SaveBuilding(); tool = "Place building"; SwitchMode("World"); });
        Label(toolsPanel, "All cells must connect. The pivot may sit outside the footprint. Rotate a placed building with R while inspecting it.", 12, muted);
    }
    private void UpdateBuildingSummary()
    {
        if (buildingSummary is null) return;
        buildingSummary.Text = $"{board.BuildingCells.Count} cells  ·  Pivot {board.BuildingOrigin}\nWorkspace {board.EditorBounds.Size.X} × {board.EditorBounds.Size.Y}";
    }
    private void EditBuilding(Hex cell, MouseButton button)
    {
        var bounds = board.EditorBounds;
        if (!bounds.HasPoint(new Vector2I(cell.X, cell.Y))) return;
        if (button == MouseButton.Right) board.BuildingCells.Remove(cell);
        else if (choosingPivot) { board.BuildingOrigin = cell; choosingPivot = false; Status($"Origin moved to {cell}."); }
        else
        {
            int x = bounds.Position.X, y = bounds.Position.Y, width = bounds.Size.X, height = bounds.Size.Y;
            bool edge = false;
            if (cell.X == bounds.Position.X) { x -= (int)patchWidth.Value; width += (int)patchWidth.Value; edge = true; }
            if (cell.X == bounds.End.X - 1) { width += (int)patchWidth.Value; edge = true; }
            if (cell.Y == bounds.Position.Y) { y -= (int)patchHeight.Value; height += (int)patchHeight.Value; edge = true; }
            if (cell.Y == bounds.End.Y - 1) { height += (int)patchHeight.Value; edge = true; }
            if (edge)
            {
                if ((long)width * height > 20000) { Status("Workspace limit: 20,000 cells. Use a smaller patch."); return; }
                board.EditorBounds = new Rect2I(x, y, width, height); board.Fit();
                Status("Grid patch added. Click again to paint within it.");
            }
            else board.BuildingCells.Add(cell);
        }
        UpdateBuildingSummary(); board.QueueRedraw();
    }
    private void SaveBuilding()
    {
        var design = new BuildingDesign
        {
            Name = buildingName.Text.Trim(), Health = (int)buildingHealth.Value, EditorOrigin = board.BuildingOrigin,
            Cells = board.BuildingCells.Select(c => c - board.BuildingOrigin).OrderBy(c => c.R).ThenBy(c => c.Q).ToList(),
            PatchWidth = (int)patchWidth.Value, PatchHeight = (int)patchHeight.Value
        };
        design.Validate();
        Storage.SaveDesign(System.IO.Path.Combine(contentRoot, "Buildings", Storage.FileName(design.Name) + ".json"), design);
        int index = buildingDesigns.FindIndex(b => b.Name == design.Name);
        if (index >= 0) { buildingDesigns[index] = design; buildingIndex = index; }
        else { buildingIndex = buildingDesigns.Count; buildingDesigns.Add(design); }
        Status($"Saved {design.Name}: {design.Cells.Count} connected cells, pivot {design.EditorOrigin}.");
    }
    private void RememberDraft()
    {
        if (mode == "Unit creator" && unitName is not null) unitDraft = ReadUnitDraft();
        if (mode == "Building creator" && buildingName is not null)
            buildingDraft = new BuildingDesign { Name = buildingName.Text, Health = (int)buildingHealth.Value, PatchWidth = (int)patchWidth.Value, PatchHeight = (int)patchHeight.Value };
    }
}

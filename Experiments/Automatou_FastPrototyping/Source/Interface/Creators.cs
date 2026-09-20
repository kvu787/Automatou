using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
    private LineEdit buildingName = null!;
    private SpinBox buildingHealth = null!, patchWidth = null!, patchHeight = null!;
    private bool choosingPivot;
    private Label buildingSummary = null!;
    private BuildingDesign? buildingDraft;
    private bool buildingOpened;
    private string worldSlotName = "My world";

    private void BuildWorldTools()
    {
        Label(toolsPanel, "WORLD CREATOR", 12, accent);
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
        Button(persistence, "Load", () => ShowWorldBrowser("World creator"));
        Button(toolsPanel, "Building creator", () => SwitchMode("Building creator"));
        Button(toolsPanel, "Play this world", () => SwitchMode("World"));
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
    }
    private string WorldPath() => System.IO.Path.Combine(contentRoot, "Worlds", Storage.FileName(worldName.Text) + ".json");
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
        Button(toolsPanel, "Save & place in world", () => { SaveBuilding(); tool = "Place building"; SwitchMode("World creator"); });
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
        Button(toolsPanel, "Save & place in world", () => { SaveBuilding(); tool = "Place building"; SwitchMode("World creator"); });
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
        if (mode == "Building creator" && buildingName is not null)
            buildingDraft = new BuildingDesign { Name = buildingName.Text, Health = (int)buildingHealth.Value, PatchWidth = (int)patchWidth.Value, PatchHeight = (int)patchHeight.Value };
    }
}

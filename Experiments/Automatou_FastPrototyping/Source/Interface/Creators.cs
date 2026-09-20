using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
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
        Button(toolsPanel, "Play this world", () => SwitchMode("World"));
        Heading(toolsPanel, "NEW WORLD");
        var shape = Choice(toolsPanel, ["Rectangle", "Hexagon"], 0, _ => { });
        var width = Number(toolsPanel, "Width / size", 34, 1, 200);
        var height = Number(toolsPanel, "Height", 24, 1, 200);
        shape.ItemSelected += index => height.Editable = index == 0;
        var seed = Number(toolsPanel, "Terrain seed", 72491, 1, 999999);
        var newWorld = Row(toolsPanel);
        Button(newWorld, "Blank", () => CreateWorkingWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, false, (int)seed.Value)));
        Button(newWorld, "Generate", () => CreateWorkingWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, true, (int)seed.Value)));
        Label(toolsPanel, "New worlds replace this workspace. Save or checkpoint first. Hexagon size 1 is one cell.", 12, muted);
    }
    private void CreateWorkingWorld(World replacement)
    {
        experimentName = "Custom world";
        experimentDescription = "Your starting conditions. Place units, tune their behavior, then checkpoint and compare.";
        referenceResult = null;
        ReplaceWorld(replacement);
    }
    private string WorldPath() => System.IO.Path.Combine(contentRoot, "Worlds", Storage.FileName(worldName.Text) + ".json");
}

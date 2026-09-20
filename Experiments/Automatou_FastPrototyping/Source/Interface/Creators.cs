using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class Laboratory {
    private string worldSlotName = "My world";
    private VBoxContainer? terrainTools;
    private VBoxContainer? populationTools;

    private void UpdateWorldToolVisibility() {
        if (this.terrainTools is not null && IsInstanceValid(this.terrainTools)) {
            this.terrainTools.Visible = this.Tool == "Paint terrain";
        }
        if (this.populationTools is not null && IsInstanceValid(this.populationTools)) {
            this.populationTools.Visible = this.Tool == "Place unit";
        }
    }

    private void BuildWorldTools() {
        _ = Label(this.modePanel, "MODE", 12, this.accent);
        this.toolChoice = this.Choice(this.modePanel, ToolNames, Array.IndexOf(ToolNames, this.Tool), index => {
            this.Tool = ToolNames[index];
            this.Status($"{this.Tool} tool selected. Click the world to use it."); this.board.QueueRedraw();
        });
        this.terrainTools = Column(this.toolsPanel);
        _ = Label(this.terrainTools, "TERRAIN BRUSH", 12, this.accent);
        _ = this.Choice(this.terrainTools, Catalog.TerrainNames, (int)this.terrain, index => { this.terrain = (Terrain)index; this.Tool = "Paint terrain"; this.Status($"Painting {Catalog.TerrainNames[index].ToLowerInvariant()}. Drag across cells."); });
        this.populationTools = Column(this.toolsPanel);
        _ = Label(this.populationTools, "POPULATE THE WORLD", 12, this.accent);
        _ = this.Choice(this.populationTools, Catalog.FactionNames, (int)this.faction, index => { this.faction = (Faction)index; this.board.QueueRedraw(); });
        this.unitChoice = this.Choice(this.populationTools, this.unitDesigns.Select(u => u.Name), this.unitIndex, index => { this.unitIndex = index; this.Tool = "Place unit"; this.board.QueueRedraw(); });
        _ = this.Button(this.populationTools, "Place selected unit", () => { this.Tool = "Place unit"; this.Status("Click the world to place units. Red footprints cannot be placed."); });
        _ = this.Button(this.populationTools, "Rotate placement   [R]", this.RotateSelection);
        this.UpdateWorldToolVisibility();
        CheckButton coordinates = new() { Text = "Show cell coordinates", ButtonPressed = this.board.Coordinates };
        coordinates.Toggled += value => { this.board.Coordinates = value; this.board.QueueRedraw(); }; this.toolsPanel.AddChild(coordinates);
        _ = this.Heading(this.toolsPanel, "WORLD FILE");
        this.worldName = TextField(this.toolsPanel, this.worldSlotName, "World name");
        this.worldName.TextChanged += value => this.worldSlotName = value;
        HBoxContainer persistence = Row(this.toolsPanel);
        _ = this.Button(persistence, "Save", () => {
            string path = this.WorldPath(); Storage.SaveWorld(path, this.world); this.Status($"Saved world: {this.worldName.Text}. Stored in UserContent / Worlds.");
        });
        _ = this.Button(persistence, "Load", () => this.ShowWorldBrowser("World creator"));
        _ = this.Button(this.toolsPanel, "Play this world", () => this.SwitchMode("World"));
        _ = this.Heading(this.toolsPanel, "NEW WORLD");
        OptionButton shape = this.Choice(this.toolsPanel, ["Rectangle", "Hexagon"], 0, _ => { });
        SpinBox width = this.Number(this.toolsPanel, "Width / size", 34, 1, 200);
        SpinBox height = this.Number(this.toolsPanel, "Height", 24, 1, 200);
        shape.ItemSelected += index => height.Editable = index == 0;
        SpinBox seed = this.Number(this.toolsPanel, "Terrain seed", 72491, 1, 999999);
        HBoxContainer newWorld = Row(this.toolsPanel);
        _ = this.Button(newWorld, "Blank", () => this.CreateWorkingWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, false, (int)seed.Value)));
        _ = this.Button(newWorld, "Generate", () => this.CreateWorkingWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value, true, (int)seed.Value)));
        _ = Label(this.toolsPanel, "New worlds replace this workspace. Save or checkpoint first. Hexagon size 1 is one cell.", 12, this.muted);
    }
    private void CreateWorkingWorld(World replacement) {
        this.experimentName = "Custom world";
        this.experimentDescription = "";
        this.referenceResult = null;
        this.ReplaceWorld(replacement);
    }
    private string WorldPath() {
        return Path.Combine(this.contentRoot, "Worlds", Storage.FileName(this.worldName.Text) + ".json");
    }
}

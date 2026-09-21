using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class MainInterface {
    private string worldSlotName = "My world";
    private VBoxContainer? fileTools;
    private VBoxContainer? terrainTools;
    private VBoxContainer? populationTools;

    private string ToolInstruction() {
        return this.Tool switch {
            "File" => "Manage session worlds or create a new world.",
            "Inspect" => "Select a unit on the board to inspect it.",
            "Paint terrain" => "Click or drag across cells to paint terrain.",
            "Place unit" => "Click the world to place units. Red footprints cannot be placed.",
            "Erase entity" => "Click or drag across units to remove them.",
            _ => ""
        };
    }
    private void SelectTool(string value) {
        this.Tool = value;
        this.Status(this.ToolInstruction());
        this.BuildInspector();
        this.board.QueueRedraw();
    }

    private void UpdateWorldToolVisibility() {
        if (this.fileTools is not null && IsInstanceValid(this.fileTools)) {
            this.fileTools.Visible = this.Tool == "File";
        }
        if (this.terrainTools is not null && IsInstanceValid(this.terrainTools)) {
            this.terrainTools.Visible = this.Tool == "Paint terrain";
        }
        if (this.populationTools is not null && IsInstanceValid(this.populationTools)) {
            this.populationTools.Visible = this.Tool == "Place unit";
        }
    }

    private void BuildWorldTools() {
        _ = Label(this.modePanel, "MODE", 12, this.accent);
        this.toolChoice = this.Choice(this.modePanel, ToolNames, Array.IndexOf(ToolNames, this.Tool), index => this.SelectTool(ToolNames[index]));
        this.terrainTools = Column(this.toolsPanel);
        _ = Label(this.terrainTools, "Terrain", 12, this.accent);
        _ = this.Choice(this.terrainTools, Catalog.TerrainNames, (int)this.terrain, index => { this.terrain = (Terrain)index; this.SelectTool("Paint terrain"); this.Status($"Painting {Catalog.TerrainNames[index].ToLowerInvariant()}. Drag across cells."); });
        this.populationTools = Column(this.toolsPanel);
        _ = Label(this.populationTools, "Faction", 12, this.accent);
        this.Choice(this.populationTools, Catalog.FactionNames, (int)this.faction, index => {
            this.faction = (Faction)index;
            this.RefreshUnitChoices();
            this.board.QueueRedraw();
        }).Name = "FactionChoice";
        _ = Label(this.populationTools, "Unit", 12, this.accent);
        this.unitChoice = this.Choice(this.populationTools, [], -1, index => {
            this.unitIndex = this.unitChoice.GetItemId(index);
            this.SelectTool("Place unit");
        });
        this.RefreshUnitChoices();
        _ = this.Button(this.populationTools, "Place selected unit", () => this.SelectTool("Place unit"));
        _ = this.Button(this.populationTools, "Rotate placement   [R]", this.RotateSelection);
        this.fileTools = Column(this.toolsPanel);
        _ = Label(this.fileTools, "SESSION WORLDS", 12, this.accent);
        _ = Label(this.fileTools, "Saved worlds stay in memory until you close the app.", 12, this.muted);
        this.worldName = TextField(this.fileTools, this.worldSlotName, "World name");
        this.worldName.TextChanged += value => this.worldSlotName = value;
        HBoxContainer persistence = Row(this.fileTools);
        _ = this.Button(persistence, "Save", () => {
            this.savedWorlds.SaveWorld(this.worldName.Text, this.world); this.Status($"Saved world: {this.worldName.Text.Trim()}. Available only during this session.");
        });
        _ = this.Button(persistence, "Load", () => this.ShowWorldBrowser("World creator"));
        _ = this.Button(this.fileTools, "Play this world", () => this.SwitchMode("World"));
        _ = this.Heading(this.fileTools, "NEW WORLD");
        OptionButton shape = this.Choice(this.fileTools, ["Rectangle", "Hexagon"], 0, _ => { });
        SpinBox width = this.Number(this.fileTools, "Width / size", 34, 1, 200);
        SpinBox height = this.Number(this.fileTools, "Height", 24, 1, 200);
        shape.ItemSelected += index => height.Editable = index == 0;
        _ = this.Button(this.fileTools, "Create world", () => this.CreateWorkingWorld(World.Create(shape.Selected == 1, (int)width.Value, (int)height.Value)));
        _ = Label(this.fileTools, "New worlds replace this workspace and its checkpoint. Save first to keep this world. Hexagon size 1 is one cell.", 12, this.muted);
        this.UpdateWorldToolVisibility();
    }
    private void RefreshUnitChoices() {
        this.unitChoice.Clear();
        for (int index = 0; index < this.unitDesigns.Count; index++) {
            if (Catalog.CanPlace(this.faction, this.unitDesigns[index])) {
                this.unitChoice.AddItem(this.unitDesigns[index].Name, index);
            }
        }
        int selectedIndex = this.unitChoice.GetItemIndex(this.unitIndex);
        this.unitChoice.Select(selectedIndex >= 0 ? selectedIndex : 0);
        this.unitIndex = this.unitChoice.GetSelectedId();
    }
    private void CreateWorkingWorld(World replacement) {
        this.experimentName = "Custom world";
        this.experimentDescription = "";
        this.referenceResult = null;
        this.ReplaceWorld(replacement);
    }
}

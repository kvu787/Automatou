using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class MainInterface {
    private VBoxContainer OpenMenu(string title) {
        this.Pause();
        this.menuVisible = true;
        this.workspaceRoot.Hide();
        if (this.menuRoot is not null) { this.RemoveChild(this.menuRoot); this.menuRoot.QueueFree(); }
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        this.AddChild(center);
        this.menuRoot = center;
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(520, 0) };
        panel.AddThemeStyleboxOverride("panel", Box("101e28"));
        center.AddChild(panel);
        VBoxContainer column = Column(panel, 16);
        _ = Label(column, "A U T O M A T O U", 28, this.accent);
        _ = Label(column, title, 20);
        this.menuStatus = Label(column, "", 13, this.muted);
        return column;
    }

    private void ShowMainMenu() {
        VBoxContainer column = this.OpenMenu("Main menu");
        _ = this.Button(column, "Load world", () => this.ShowWorldBrowser());
        _ = this.Button(column, "World creator", () => this.SwitchMode("World creator"));
    }

    private void ShowWorldBrowser(string destination = "World") {
        VBoxContainer column = this.OpenMenu("Load world");
        ScrollContainer scroll = new() { CustomMinimumSize = new Vector2(0, 410), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        VBoxContainer list = Column(scroll);
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _ = this.Heading(list, "FOCUSED EXPERIMENTS");
        foreach (ExperimentScenario scenario in ScenarioCatalog.All) {
            _ = this.Button(list, scenario.Name, () => this.LoadScenario(scenario, destination));
            _ = Label(list, scenario.Description, 12, this.muted);
        }
        _ = this.Heading(list, "LARGER ENCOUNTER");
        _ = this.Button(list, "Five-faction encounter", () => this.LoadChosenWorld(null, destination));
        _ = this.Heading(list, "PLAYER-CREATED WORLDS");
        string[] names = this.savedWorlds.WorldNames.ToArray();
        if (names.Length == 0) {
            _ = Label(list, "No worlds saved this session. Create and save one in World creator.", 14, this.muted);
        } else {
            foreach (string name in names) {
                Button button = this.Button(list, name, () => this.LoadChosenWorld(name, destination));
                button.ClipText = true;
            }
        }
        _ = this.Button(column, "Back", destination == "World" ? this.ShowMainMenu : () => this.SwitchMode(destination));
    }

    private void LoadChosenWorld(string? name, string destination) {
        World replacement = name is null ? World.Demonstration() : this.savedWorlds.LoadWorld(name);
        this.worldSlotName = name ?? "My world";
        this.experimentName = name is null ? "Five-faction encounter" : this.worldSlotName;
        this.experimentDescription = "";
        this.referenceResult = null;
        this.ReplaceWorld(replacement);
        this.SwitchMode(destination);
    }

    private void LoadScenario(ExperimentScenario scenario, string destination = "World") {
        World replacement = scenario.Create();
        this.experimentName = scenario.Name; this.experimentDescription = scenario.Description;
        this.worldSlotName = scenario.Name; this.referenceResult = null;
        this.ReplaceWorld(replacement);
        this.SwitchMode(destination);
        this.selected = this.world.Entities.FirstOrDefault();
        this.Refresh();
        this.LogResult("SCENARIO", this.Result());
        this.Status("Simulation paused.");
    }

    private void BuildPlaybackTools() {
        _ = Label(this.toolsPanel, "EXPERIMENT", 12, this.accent);
        _ = Label(this.toolsPanel, this.experimentName, 21);
        if (!string.IsNullOrWhiteSpace(this.experimentDescription)) {
            _ = Label(this.toolsPanel, this.experimentDescription, 13, this.muted);
        }
        _ = this.Button(this.toolsPanel, "Load another world", () => this.ShowWorldBrowser());
        this.BuildExperimentTools();
        _ = this.Heading(this.toolsPanel, "WORLD AUTHORING");
        _ = this.Button(this.toolsPanel, "Edit in World creator", () => this.SwitchMode("World creator"));
    }
}

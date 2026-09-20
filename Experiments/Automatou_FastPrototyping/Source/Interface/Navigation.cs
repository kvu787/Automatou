using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
    private VBoxContainer OpenMenu(string title)
    {
        Pause();
        menuVisible = true;
        workspaceRoot.Hide();
        if (menuRoot is not null) { RemoveChild(menuRoot); menuRoot.QueueFree(); }
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        menuRoot = center;
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(520, 0) };
        panel.AddThemeStyleboxOverride("panel", Box("101e28"));
        center.AddChild(panel);
        var column = Column(panel, 16);
        Label(column, "A U T O M A T O U", 28, accent);
        Label(column, title, 20);
        menuStatus = Label(column, "", 13, muted);
        return column;
    }

    private void ShowMainMenu()
    {
        var column = OpenMenu("Main menu");
        Button(column, "Load world", () => ShowWorldBrowser());
        Button(column, "World creator", () => SwitchMode("World creator"));
    }

    private void ShowWorldBrowser(string destination = "World")
    {
        var column = OpenMenu("Load world");
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 410), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        column.AddChild(scroll);
        var list = Column(scroll);
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Heading(list, "FOCUSED EXPERIMENTS");
        foreach (var scenario in ScenarioCatalog.All)
        {
            Button(list, scenario.Name, () => LoadScenario(scenario, destination));
            Label(list, scenario.Description, 12, muted);
        }
        Heading(list, "LARGER ENCOUNTER");
        Button(list, "Five-faction encounter", () => LoadChosenWorld(null, destination));
        Heading(list, "PLAYER-CREATED WORLDS");
        string directory = System.IO.Path.Combine(contentRoot, "Worlds");
        System.IO.Directory.CreateDirectory(directory);
        var files = System.IO.Directory.GetFiles(directory, "*.json")
            .OrderBy(System.IO.Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0) Label(list, "No saved worlds yet. Create and save one in World creator.", 14, muted);
        else
        {
            foreach (string file in files)
            {
                var button = Button(list, System.IO.Path.GetFileNameWithoutExtension(file), () => LoadChosenWorld(file, destination));
                button.ClipText = true;
            }
        }
        Button(column, "Back", destination == "World" ? ShowMainMenu : () => SwitchMode(destination));
    }

    private void LoadChosenWorld(string? path, string destination)
    {
        // Validate first so a broken save leaves the browser and current world intact.
        World replacement = path is null ? World.Demonstration() : Storage.LoadWorld(path);
        worldSlotName = path is null ? "My world" : System.IO.Path.GetFileNameWithoutExtension(path);
        experimentName = path is null ? "Five-faction encounter" : worldSlotName;
        experimentDescription = path is null
            ? "A larger encounter for observing interactions. Select a unit, inspect its reasons, then tune and replay."
            : "A saved world. Select a unit to inspect its intentions and adjust its behavior.";
        referenceResult = null;
        ReplaceWorld(replacement);
        SwitchMode(destination);
    }

    private void LoadScenario(ExperimentScenario scenario, string destination = "World")
    {
        var replacement = scenario.Create();
        experimentName = scenario.Name; experimentDescription = scenario.Description;
        worldSlotName = scenario.Name; referenceResult = null;
        ReplaceWorld(replacement);
        SwitchMode(destination);
        selected = world.Entities.FirstOrDefault();
        Refresh();
        LogResult("SCENARIO", Result());
        Status("Experiment paused at its starting point. Step, inspect, tune, and replay.");
    }

    private void BuildPlaybackTools()
    {
        Label(toolsPanel, "EXPERIMENT", 12, accent);
        Label(toolsPanel, experimentName, 21);
        Label(toolsPanel, experimentDescription, 13, muted);
        Button(toolsPanel, "Load another world", () => ShowWorldBrowser());
        BuildExperimentTools();
        Heading(toolsPanel, "WORLD AUTHORING");
        Button(toolsPanel, "Edit in World creator", () => SwitchMode("World creator"));
    }
}

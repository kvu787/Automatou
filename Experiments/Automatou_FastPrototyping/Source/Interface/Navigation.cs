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
        Heading(column, "BUILT-IN WORLD");
        Button(column, "Five-faction encounter", () => LoadChosenWorld(null, destination));
        Heading(column, "PLAYER-CREATED WORLDS");
        string directory = System.IO.Path.Combine(contentRoot, "Worlds");
        System.IO.Directory.CreateDirectory(directory);
        var files = System.IO.Directory.GetFiles(directory, "*.json")
            .OrderBy(System.IO.Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0) Label(column, "No saved worlds yet. Create and save one in World creator.", 14, muted);
        else
        {
            var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(0, 240), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
            column.AddChild(scroll);
            var list = Column(scroll);
            list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
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
        ReplaceWorld(replacement);
        SwitchMode(destination);
    }

    private void BuildPlaybackTools()
    {
        Label(toolsPanel, "WORLD", 12, accent);
        Label(toolsPanel, "Five factions. One world.", 21);
        Label(toolsPanel, "Run the automata or advance a single turn. Select a unit to inspect it.", 14, muted);
        Button(toolsPanel, "Edit in World creator", () => SwitchMode("World creator"));
        Button(toolsPanel, "Load another world", () => ShowWorldBrowser());
        Button(toolsPanel, "Main menu", ShowMainMenu);
    }
}

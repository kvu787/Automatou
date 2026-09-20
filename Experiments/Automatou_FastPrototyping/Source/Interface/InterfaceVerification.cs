using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
    private async void RunInterfaceVerification()
    {
        try
        {
            // Exercise the same handlers as the controls, then capture the rendered views.
            contentRoot = System.IO.Path.Combine(sessionRoot, "VerificationContent");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!menuVisible || workspaceRoot.Visible) throw new Exception("Startup did not show the main menu.");
            var menuButtons = menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().ToArray();
            if (!menuButtons.Select(button => button.Text).SequenceEqual(new[] { "Load world", "World creator", "Building creator" }))
                throw new Exception("Main menu choices or order are incorrect.");
            await Capture("MainMenu.png");
            foreach (string creator in new[] { "World creator", "Building creator" })
            {
                menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == creator).EmitSignal(Godot.Button.SignalName.Pressed);
                if (menuVisible || mode != creator || transport.Visible) throw new Exception("Creator navigation failed: " + creator);
                ShowMainMenu();
            }
            int initialTurn = world.Turn;
            _UnhandledKeyInput(new InputEventKey { Keycode = Key.N, Pressed = true });
            if (world.Turn != initialTurn) throw new Exception("Menu allowed simulation shortcuts.");
            ShowWorldBrowser();
            await Capture("LoadWorldEmpty.png");
            LoadChosenWorld(null, "World");
            if (menuVisible || !transport.Visible || world.Entities.Count == 0) throw new Exception("Built-in world did not open for play.");
            await Capture("WorldOverview.png");
            tool = "Inspect";
            board._GuiInput(new InputEventMouseButton { Position = board.Screen(world.Entities[0].Position), ButtonIndex = MouseButton.Left, Pressed = true });
            board._GuiInput(new InputEventMouseButton { Position = board.Screen(world.Entities[0].Position), ButtonIndex = MouseButton.Left, Pressed = false });
            if (selected is null) throw new Exception("Inspect did not select a unit.");
            float oldZoom = board.Zoom;
            board._GuiInput(new InputEventMouseButton { Position = board.Size / 2, ButtonIndex = MouseButton.WheelUp, Pressed = true });
            if (board.Zoom <= oldZoom) throw new Exception("Wheel did not zoom.");
            Vector2 oldPosition = board.Screen(selected.Position);
            board._GuiInput(new InputEventMouseButton { Position = board.Size / 2, ButtonIndex = MouseButton.Middle, Pressed = true });
            board._GuiInput(new InputEventMouseMotion { Position = board.Size / 2, Relative = new Vector2(20, 10), ButtonMask = MouseButtonMask.Middle });
            if (board.Screen(selected.Position) != oldPosition + new Vector2(20, 10)) throw new Exception("Middle drag did not pan.");
            board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = false });
            board.Fit();
            RotateSelection(); Step();
            await Capture("WorldInspector.png");
            SwitchMode("Building creator");
            int before = board.EditorBounds.Size.X;
            EditBuilding(Hex.FromOffset(board.EditorBounds.End.X - 1, 5), MouseButton.Left);
            if (board.EditorBounds.Size.X <= before) throw new Exception("Edge click did not add a patch.");
            choosingPivot = true; EditBuilding(Hex.FromOffset(8, 5), MouseButton.Left);
            if (board.BuildingOrigin != Hex.FromOffset(8, 5)) throw new Exception("Pivot was not moved.");
            buildingName.Text = "Verification building"; SaveBuilding();
            var reopened = Storage.LoadDesign<BuildingDesign>(System.IO.Path.Combine(contentRoot, "Buildings", "Verification building.json"));
            OpenBuilding(reopened);
            if (board.BuildingOrigin != Hex.FromOffset(8, 5)) throw new Exception("Saved pivot did not reopen.");
            await Capture("BuildingCreator.png");
            SwitchMode("World creator");
            ReplaceWorld(World.Create(false, 20, 15, false));
            tool = "Place unit"; OnCell(Hex.FromOffset(6, 6), MouseButton.Left);
            if (world.Entities.Single().Unit is not Bastion) throw new Exception("Source-defined unit placement failed.");
            tool = "Place building"; OnCell(Hex.FromOffset(14, 7), MouseButton.Left);
            if (world.Entities.Count != 2) throw new Exception("Custom building placement failed.");
            tool = "Paint terrain"; terrain = Terrain.Desert; OnCell(Hex.FromOffset(1, 1), MouseButton.Left);
            Storage.SaveWorld(System.IO.Path.Combine(contentRoot, "Worlds", "Verification.json"), world);
            ShowMainMenu();
            ShowWorldBrowser();
            await Capture("LoadWorldSaved.png");
            LoadChosenWorld(System.IO.Path.Combine(contentRoot, "Worlds", "Verification.json"), "World");
            if (world.Terrain[Hex.FromOffset(1, 1)] != Terrain.Desert || world.Entities.Count != 2) throw new Exception("Authored world did not restore.");
            ToggleRun(); ShowMainMenu();
            if (running) throw new Exception("Returning to menu did not pause.");
            SwitchMode("World creator");
            if (transport.Visible) throw new Exception("World creator exposes simulation transport.");
            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            await Capture("MinimumWindow.png");
            ReplaceWorld(Storage.Decode(checkpoint));
            Log("PASS: interface, inspect, pan, zoom, rotate, turn, source-defined unit placement, building expand/pivot/save/reopen/place, terrain paint, world file round trip, and minimum window.");
            GetTree().Quit();
        }
        catch (Exception exception) { Log("INTERFACE FAILURE: " + exception); GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }
    private async Task Capture(string name)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = GetViewport().GetTexture().GetImage();
        Error error = image.SavePng(System.IO.Path.Combine(sessionRoot, name));
        if (error != Error.Ok) throw new IOException($"Cannot save screenshot: {error}");
    }
}

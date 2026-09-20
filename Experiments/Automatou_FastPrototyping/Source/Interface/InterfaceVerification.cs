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
            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!menuVisible || workspaceRoot.Visible) throw new Exception("Startup did not show the main menu.");
            var menuButtons = menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().ToArray();
            if (!menuButtons.Select(button => button.Text).SequenceEqual(new[] { "Load world", "World creator" }))
                throw new Exception("Main menu choices or order are incorrect.");
            await Capture("MainMenu.png");
            foreach (string creator in new[] { "World creator" })
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
            foreach (var scenario in ScenarioCatalog.All)
            {
                ShowWorldBrowser();
                menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == scenario.Name).EmitSignal(Godot.Button.SignalName.Pressed);
                if (menuVisible || running || world.Turn != 0 || selected is null || world.Entities.Count == 0 || experimentName != scenario.Name)
                    throw new Exception("Experiment did not load paused: " + scenario.Name);
                Step();
                if (selected.Unit.Brain.TurnsObserved == 0 || string.IsNullOrWhiteSpace(selected.Unit.Brain.State.Reason))
                    throw new Exception("Experiment did not expose decision reasoning: " + scenario.Name);
            }
            var checkpointWorld = Storage.Decode(checkpoint);
            int selectedId = selected!.Id;
            var initialEntity = checkpointWorld.Entities.Single(entity => entity.Id == selectedId);
            double changedAggression = initialEntity.Unit.Brain.Settings.Aggression > .5 ? .2 : .9;
            inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Tuning").EmitSignal(Godot.Button.SignalName.Pressed);
            if (Math.Abs(inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).OfType<SpinBox>().Single().Value - selected.Unit.Brain.Settings.Aggression) > .000001)
                throw new Exception("Tuning display rounded the unit's actual setting.");
            ToggleRun();
            inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).OfType<SpinBox>().Single().Value = changedAggression;
            if (running || selected.Unit.Brain.Settings.Aggression != changedAggression) throw new Exception("Tuning did not pause and update the unit.");
            var peer = world.Entities.FirstOrDefault(entity => entity.Id != selectedId && entity.Faction == selected.Faction);
            if (peer is not null)
            {
                var selector = inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).OfType<OptionButton>().Single();
                int index = Enumerable.Range(1, selector.ItemCount - 1).Single(value => selector.GetItemText(value).StartsWith($"#{peer.Id} "));
                selector.EmitSignal(OptionButton.SignalName.ItemSelected, index);
                if (selected.BondedUnitId != peer.Id) throw new Exception("Bond selector did not update the unit.");
            }
            foreach (string caption in new[] { "Limited perception", "Weapon heat", "Protective bonds" })
            {
                var toggle = toolsPanel.FindChildren("*", "CheckButton", true, false).OfType<CheckButton>().Single(control => control.Text == caption);
                toggle.ButtonPressed = !toggle.ButtonPressed;
            }
            if (world.Settings.LimitedPerception == checkpointWorld.Settings.LimitedPerception || world.Settings.HeatEnabled == checkpointWorld.Settings.HeatEnabled || world.Settings.BondsEnabled == checkpointWorld.Settings.BondsEnabled)
                throw new Exception("Mechanic switches did not update settings.");
            var tunedSettings = world.Settings with { };
            // A casualty must retain its last tuning when the checkpoint restores it.
            world.Remove(selected);
            RewindExperiment(true);
            if (selected is null || selected.Unit.Brain.Settings.Aggression != changedAggression || world.Settings != tunedSettings || selected.Unit.Brain.TurnsObserved != initialEntity.Unit.Brain.TurnsObserved || selected.Heat != initialEntity.Heat || selected.Unit.Brain.State.History.Count != initialEntity.Unit.Brain.State.History.Count || (peer is not null && selected.BondedUnitId != peer.Id))
                throw new Exception("Tuned rewind did not restore the removed unit's tuning and checkpoint state.");
            AdvanceTurns(10);
            if (world.Turn != checkpointWorld.Turn + 10 || referenceResult is null) throw new Exception("Batch turn or comparison failed.");
            RewindExperiment(false);
            if (selected is null || selected.Unit.Brain.Settings.Aggression != initialEntity.Unit.Brain.Settings.Aggression || world.Settings != checkpointWorld.Settings || world.Turn != checkpointWorld.Turn)
                throw new Exception("Exact rewind changed checkpoint values.");
            Step();
            inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Behavior").EmitSignal(Godot.Button.SignalName.Pressed);
            foreach (string caption in new[] { "Show intentions", "Dim unseen enemies" })
            {
                var toggle = toolsPanel.FindChildren("*", "CheckButton", true, false).OfType<CheckButton>().Single(control => control.Text == caption);
                toggle.ButtonPressed = false; toggle.ButtonPressed = true;
            }
            if (!board.DecisionOverlay || !board.DimUnseenEnemies || !inspectorPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(label => label.Text == selected!.Unit.Brain.State.Reason))
                throw new Exception("Decision inspector or observation overlay failed.");
            await Capture("ExperimentMinimumWindow.png");
            if (board.Size.X < 300 || board.GlobalPosition.X + board.Size.X > inspectorPanel.GlobalPosition.X || inspectorPanel.GlobalPosition.X + inspectorPanel.Size.X > GetViewportRect().Size.X)
                throw new Exception("Experiment layout exceeds the minimum window.");
            inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Tuning").EmitSignal(Godot.Button.SignalName.Pressed);
            await Capture("TuningMinimumWindow.png");
            if (inspectorTab != 1 || !inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).Any()) throw new Exception("Tuning tab did not expose settings.");
            inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Unit").EmitSignal(Godot.Button.SignalName.Pressed);
            if (!inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Any(button => button.Text is "Mobilize unit" or "Deploy / hold position")) throw new Exception("Unit tab did not expose physical controls.");
            inspectorTab = 0;
            var inert = world.Entities.FirstOrDefault(entity => entity.Unit is TrainingTarget);
            if (inert is not null)
            {
                selected = inert; inspectorTab = 1; BuildInspector();
                if (inspectorPanel.FindChildren("*", "SpinBox", true, false).Any() || inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).Any())
                    throw new Exception("Inert target exposes ineffective tuning.");
            }
            LoadChosenWorld(null, "World");
            selected = world.Entities.First(entity => entity.Unit is PrytuHunter);
            inspectorTab = 1; BuildInspector();
            if (inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).Any() || !inspectorPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(label => label.Text.Contains("does not use individual bonds")))
                throw new Exception("Prytu exposes ineffective bond tuning.");
            selected = null; inspectorTab = 0; Refresh();
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
            SwitchMode("World creator");
            ReplaceWorld(World.Create(false, 20, 15, false));
            tool = "Place unit"; OnCell(Hex.FromOffset(6, 6), MouseButton.Left);
            if (world.Entities.Single().Unit is not Bastion) throw new Exception("Source-defined unit placement failed.");
            tool = "Paint terrain"; terrain = Terrain.Desert; OnCell(Hex.FromOffset(1, 1), MouseButton.Left);
            Storage.SaveWorld(System.IO.Path.Combine(contentRoot, "Worlds", "Verification.json"), world);
            ShowMainMenu();
            ShowWorldBrowser();
            await Capture("LoadWorldSaved.png");
            LoadChosenWorld(System.IO.Path.Combine(contentRoot, "Worlds", "Verification.json"), "World");
            if (world.Terrain[Hex.FromOffset(1, 1)] != Terrain.Desert || world.Entities.Count != 1) throw new Exception("Authored world did not restore.");
            ToggleRun(); ShowMainMenu();
            if (running) throw new Exception("Returning to menu did not pause.");
            SwitchMode("World creator");
            if (transport.Visible) throw new Exception("World creator exposes simulation transport.");
            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            await Capture("MinimumWindow.png");
            ReplaceWorld(Storage.Decode(checkpoint));
            Log("PASS: interface, scenarios, decision inspector, tuning, bonds, mechanics, tuned/exact rewind, batch turns, overlays, inspect, pan, zoom, rotate, unit placement, terrain paint, world file round trip, and minimum window.");
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

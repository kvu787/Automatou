using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class MainInterface {
    private async void RunInterfaceVerification() {
        try {
            // Exercise the same handlers as the controls, then capture the rendered views.
            if (this.savedWorlds.WorldNames.Any()) {
                throw new InvalidOperationException("Startup restored saved worlds from an earlier session.");
            }
            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            _ = await this.ToSignal(this.GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!this.menuVisible || this.workspaceRoot.Visible) {
                throw new InvalidOperationException("Startup did not show the main menu.");
            }

            Button[] menuButtons = this.menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().ToArray();
            if (!menuButtons.Select(button => button.Text).SequenceEqual(["Load world", "World creator"])) {
                throw new InvalidOperationException("Main menu choices or order are incorrect.");
            }

            await this.Capture("MainMenu.png");
            foreach (string creator in new[] { "World creator" }) {
                _ = this.menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == creator).EmitSignal(BaseButton.SignalName.Pressed);
                if (this.menuVisible || this.mode != creator || this.transport.Visible) {
                    throw new InvalidOperationException("Creator navigation failed: " + creator);
                }

                this.ShowMainMenu();
            }
            int initialTurn = this.world.Turn;
            this._UnhandledKeyInput(new InputEventKey { Keycode = Key.N, Pressed = true });
            if (this.world.Turn != initialTurn) {
                throw new InvalidOperationException("Menu allowed simulation shortcuts.");
            }

            this.ShowWorldBrowser();
            await this.Capture("LoadWorldEmpty.png");
            foreach (ExperimentScenario scenario in ScenarioCatalog.All) {
                this.ShowWorldBrowser();
                _ = this.menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == scenario.Name).EmitSignal(BaseButton.SignalName.Pressed);
                if (this.menuVisible || this.running || this.world.Turn != 0 || this.selected is null || this.world.Entities.Count == 0 || this.experimentName != scenario.Name) {
                    throw new InvalidOperationException("Experiment did not load paused: " + scenario.Name);
                }

                this.Step();
                if (this.selected.Unit.Brain.TurnsObserved == 0 || string.IsNullOrWhiteSpace(this.selected.Unit.Brain.State.Reason)) {
                    throw new InvalidOperationException("Experiment did not expose decision reasoning: " + scenario.Name);
                }
            }
            World checkpointWorld = this.checkpoint.Copy();
            int selectedId = this.selected!.Id;
            Entity initialEntity = checkpointWorld.Entities.Single(entity => entity.Id == selectedId);
            double changedAggression = initialEntity.Unit.Brain.Settings.Aggression > .5 ? .2 : .9;
            _ = this.inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Tuning").EmitSignal(BaseButton.SignalName.Pressed);
            if (Math.Abs(this.inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).OfType<SpinBox>().Single().Value - this.selected.Unit.Brain.Settings.Aggression) > .000001) {
                throw new InvalidOperationException("Tuning display rounded the unit's actual setting.");
            }

            this.ToggleRun();
            this.inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).OfType<SpinBox>().Single().Value = changedAggression;
            if (this.running || this.selected.Unit.Brain.Settings.Aggression != changedAggression) {
                throw new InvalidOperationException("Tuning did not pause and update the unit.");
            }

            Entity? peer = this.world.Entities.FirstOrDefault(entity => entity.Id != selectedId && entity.Faction == this.selected.Faction);
            if (peer is not null) {
                OptionButton selector = this.inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).OfType<OptionButton>().Single();
                int index = Enumerable.Range(1, selector.ItemCount - 1).Single(value => selector.GetItemText(value).StartsWith($"#{peer.Id} ", StringComparison.Ordinal));
                _ = selector.EmitSignal(OptionButton.SignalName.ItemSelected, index);
                if (this.selected.BondedUnitId != peer.Id) {
                    throw new InvalidOperationException("Bond selector did not update the unit.");
                }
            }
            foreach (string caption in new[] { "Limited perception", "Weapon heat", "Protective bonds" }) {
                CheckButton toggle = this.toolsPanel.FindChildren("*", "CheckButton", true, false).OfType<CheckButton>().Single(control => control.Text == caption);
                toggle.ButtonPressed = !toggle.ButtonPressed;
            }
            if (this.world.Settings.LimitedPerception == checkpointWorld.Settings.LimitedPerception || this.world.Settings.HeatEnabled == checkpointWorld.Settings.HeatEnabled || this.world.Settings.BondsEnabled == checkpointWorld.Settings.BondsEnabled) {
                throw new InvalidOperationException("Mechanic switches did not update settings.");
            }

            SimulationSettings tunedSettings = this.world.Settings with { };
            // A casualty must retain its last tuning when the checkpoint restores it.
            this.world.Remove(this.selected);
            this.RewindExperiment(true);
            if (this.selected is null || this.selected.Unit.Brain.Settings.Aggression != changedAggression || this.world.Settings != tunedSettings || this.selected.Unit.Brain.TurnsObserved != initialEntity.Unit.Brain.TurnsObserved || this.selected.Heat != initialEntity.Heat || this.selected.Unit.Brain.State.History.Count != initialEntity.Unit.Brain.State.History.Count || (peer is not null && this.selected.BondedUnitId != peer.Id)) {
                throw new InvalidOperationException("Tuned rewind did not restore the removed unit's tuning and checkpoint state.");
            }

            this.AdvanceTurns(10);
            if (this.world.Turn != checkpointWorld.Turn + 10 || this.referenceResult is null) {
                throw new InvalidOperationException("Batch turn or comparison failed.");
            }

            this.RewindExperiment(false);
            if (this.selected is null || this.selected.Unit.Brain.Settings.Aggression != initialEntity.Unit.Brain.Settings.Aggression || this.world.Settings != checkpointWorld.Settings || this.world.Turn != checkpointWorld.Turn) {
                throw new InvalidOperationException("Exact rewind changed checkpoint values.");
            }

            this.Step();
            _ = this.inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Behavior").EmitSignal(BaseButton.SignalName.Pressed);
            foreach (string caption in new[] { "Show intentions", "Dim unseen enemies" }) {
                CheckButton toggle = this.toolsPanel.FindChildren("*", "CheckButton", true, false).OfType<CheckButton>().Single(control => control.Text == caption);
                toggle.ButtonPressed = false; toggle.ButtonPressed = true;
            }
            if (!this.board.DecisionOverlay || !this.board.DimUnseenEnemies || !this.inspectorPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(label => label.Text == this.selected!.Unit.Brain.State.Reason)) {
                throw new InvalidOperationException("Decision inspector or observation overlay failed.");
            }

            await this.Capture("ExperimentMinimumWindow.png");
            if (this.board.Size.X < 300 || this.board.GlobalPosition.X + this.board.Size.X > this.inspectorPanel.GlobalPosition.X || this.inspectorPanel.GlobalPosition.X + this.inspectorPanel.Size.X > this.GetViewportRect().Size.X) {
                throw new InvalidOperationException("Experiment layout exceeds the minimum window.");
            }

            _ = this.inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Tuning").EmitSignal(BaseButton.SignalName.Pressed);
            await this.Capture("TuningMinimumWindow.png");
            if (this.inspectorTab != 1 || this.inspectorPanel.FindChildren("Aggression", "SpinBox", true, false).Count == 0) {
                throw new InvalidOperationException("Tuning tab did not expose settings.");
            }

            _ = this.inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Unit").EmitSignal(BaseButton.SignalName.Pressed);
            if (!this.inspectorPanel.FindChildren("*", "Button", true, false).OfType<Button>().Any(button => button.Text is "Mobilize unit" or "Deploy / hold position")) {
                throw new InvalidOperationException("Unit tab did not expose physical controls.");
            }

            this.inspectorTab = 0;
            Entity? inert = this.world.Entities.FirstOrDefault(entity => entity.Unit is TrainingTarget);
            if (inert is not null) {
                this.selected = inert; this.inspectorTab = 1; this.BuildInspector();
                if (this.inspectorPanel.FindChildren("*", "SpinBox", true, false).Count != 0 || this.inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).Count != 0) {
                    throw new InvalidOperationException("Inert target exposes ineffective tuning.");
                }
            }
            this.LoadChosenWorld(null, "World");
            this.selected = this.world.Entities.First(entity => entity.Unit is PrytuHunter);
            this.inspectorTab = 1; this.BuildInspector();
            if (this.inspectorPanel.FindChildren("BondChoice", "OptionButton", true, false).Count != 0 || !this.inspectorPanel.FindChildren("*", "Label", true, false).OfType<Label>().Any(label => label.Text.Contains("does not use individual bonds"))) {
                throw new InvalidOperationException("Prytu exposes ineffective bond tuning.");
            }

            this.selected = null; this.inspectorTab = 0; this.Refresh();
            if (this.menuVisible || !this.transport.Visible || this.world.Entities.Count == 0) {
                throw new InvalidOperationException("Built-in world did not open for play.");
            }

            await this.Capture("WorldOverview.png");
            DisplayServer.WindowSetSize(new Vector2I(1440, 900));
            _ = await this.ToSignal(this.GetTree(), SceneTree.SignalName.ProcessFrame);
            while (this.board.Zoom > .12f) {
                this.board._GuiInput(new InputEventMouseButton { Position = this.board.Size / 2, ButtonIndex = MouseButton.WheelDown, Pressed = true });
            }
            foreach (float targetZoom in new[] { .12f, 1f, 3.1f, 4f }) {
                while (this.board.Zoom < targetZoom) {
                    this.board._GuiInput(new InputEventMouseButton { Position = this.board.Size / 2, ButtonIndex = MouseButton.WheelUp, Pressed = true });
                }
                await this.Capture($"EdgesZoom{targetZoom * 100:0}.png");
            }
            this.board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = true });
            this.board._GuiInput(new InputEventMouseMotion { Position = this.board.Size / 2, Relative = new Vector2(.35f, .65f), ButtonMask = MouseButtonMask.Middle });
            this.board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = false });
            await this.Capture("EdgesFractionalPan.png");
            this.board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = true });
            this.board._GuiInput(new InputEventMouseMotion { Position = this.board.Size / 2, Relative = this.board.Size / 2 - this.board.Screen(this.world.Entities[0].Position), ButtonMask = MouseButtonMask.Middle });
            this.board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = false });
            await this.Capture("EdgesUnitCloseup.png");
            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            _ = await this.ToSignal(this.GetTree(), SceneTree.SignalName.ProcessFrame);
            this.board.Fit();
            this.Tool = "Inspect";
            this.board._GuiInput(new InputEventMouseButton { Position = this.board.Screen(this.world.Entities[0].Position), ButtonIndex = MouseButton.Left, Pressed = true });
            this.board._GuiInput(new InputEventMouseButton { Position = this.board.Screen(this.world.Entities[0].Position), ButtonIndex = MouseButton.Left, Pressed = false });
            if (this.selected is null) {
                throw new InvalidOperationException("Inspect did not select a unit.");
            }

            float oldZoom = this.board.Zoom;
            this.board._GuiInput(new InputEventMouseButton { Position = this.board.Size / 2, ButtonIndex = MouseButton.WheelUp, Pressed = true });
            if (this.board.Zoom <= oldZoom) {
                throw new InvalidOperationException("Wheel did not zoom.");
            }

            Vector2 oldPosition = this.board.Screen(this.selected.Position);
            this.board._GuiInput(new InputEventMouseButton { Position = this.board.Size / 2, ButtonIndex = MouseButton.Middle, Pressed = true });
            this.board._GuiInput(new InputEventMouseMotion { Position = this.board.Size / 2, Relative = new Vector2(20, 10), ButtonMask = MouseButtonMask.Middle });
            if (this.board.Screen(this.selected.Position) != oldPosition + new Vector2(20, 10)) {
                throw new InvalidOperationException("Middle drag did not pan.");
            }

            this.board._GuiInput(new InputEventMouseButton { ButtonIndex = MouseButton.Middle, Pressed = false });
            this.board.Fit();
            this.RotateSelection(); this.Step();
            await this.Capture("WorldInspector.png");
            this.SwitchMode("World creator");
            this.Tool = "File";
            OptionButton worldShape = this.toolsPanel.FindChildren("*", "OptionButton", true, false).OfType<OptionButton>().Single(choice => choice.ItemCount == 2 && choice.GetItemText(0) == "Rectangle");
            SpinBox[] dimensions = this.toolsPanel.FindChildren("*", "SpinBox", true, false).OfType<SpinBox>().ToArray();
            Button createWorld = this.toolsPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Create world");
            foreach (int shape in new[] { 1, 0 }) {
                worldShape.Select(shape);
                _ = worldShape.EmitSignal(OptionButton.SignalName.ItemSelected, shape);
                dimensions[0].Value = shape == 1 ? 4 : 20;
                dimensions[1].Value = 15;
                _ = createWorld.EmitSignal(BaseButton.SignalName.Pressed);
                if (this.world.Terrain.Count != (shape == 1 ? 37 : 300) || this.world.Terrain.Values.Any(terrain => terrain != Terrain.Plains) || this.world.Entities.Count != 0 || this.world.Turn != 0 || this.running || this.selected is not null || !this.checkpoint.Terrain.SequenceEqual(this.world.Terrain) || this.checkpoint.Entities.Count != 0 || this.checkpoint.Turn != 0 || dimensions[1].Editable != (shape == 0)) {
                    throw new InvalidOperationException("World creation did not reset the workspace to the requested empty plains map.");
                }
            }
            foreach (string tool in ToolNames) {
                _ = this.toolChoice!.EmitSignal(OptionButton.SignalName.ItemSelected, Array.IndexOf(ToolNames, tool));
                if (this.Tool != tool || this.terrainTools!.IsVisibleInTree() != (tool == "Paint terrain") || this.populationTools!.IsVisibleInTree() != (tool == "Place unit") || this.fileTools!.IsVisibleInTree() != (tool == "File") || this.worldName.IsVisibleInTree() != (tool == "File")) {
                    throw new InvalidOperationException("World creator shows unrelated controls for: " + tool);
                }
                await this.Capture("Creator" + tool.Replace(" ", "") + ".png");
                this.OnCell(Hex.FromOffset(1, 1), MouseButton.Right);
                if (this.Tool != "Inspect" || this.toolChoice.Selected != Array.IndexOf(ToolNames, "Inspect") || this.terrainTools.IsVisibleInTree() || this.populationTools.IsVisibleInTree() || this.fileTools!.IsVisibleInTree()) {
                    throw new InvalidOperationException("Right-click inspect did not hide editing tools.");
                }
            }
            this.Tool = "Place unit"; this.OnCell(Hex.FromOffset(6, 6), MouseButton.Left);
            if (this.world.Entities.Single().Unit is not Bastion) {
                throw new InvalidOperationException("Source-defined unit placement failed.");
            }

            this.Tool = "Paint terrain"; this.terrain = Terrain.ExclusionZone; this.OnCell(Hex.FromOffset(1, 1), MouseButton.Left);
            this.Tool = "File";
            this.worldName.Text = "Verification";
            _ = this.toolsPanel.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Save").EmitSignal(BaseButton.SignalName.Pressed);
            this.ShowMainMenu();
            this.ShowWorldBrowser();
            await this.Capture("LoadWorldSaved.png");
            _ = this.menuRoot.FindChildren("*", "Button", true, false).OfType<Button>().Single(button => button.Text == "Verification").EmitSignal(BaseButton.SignalName.Pressed);
            if (this.world.Terrain[Hex.FromOffset(1, 1)] != Terrain.ExclusionZone || this.world.Entities.Count != 1) {
                throw new InvalidOperationException("Authored world did not restore.");
            }

            this.ToggleRun(); this.ShowMainMenu();
            if (this.running) {
                throw new InvalidOperationException("Returning to menu did not pause.");
            }

            this.SwitchMode("World creator");
            if (this.transport.Visible) {
                throw new InvalidOperationException("World creator exposes simulation transport.");
            }

            DisplayServer.WindowSetSize(new Vector2I(1100, 700));
            await this.Capture("MinimumWindow.png");
            this.ReplaceWorld(this.checkpoint.Copy());
            this.Log("PASS: interface, scenarios, decision inspector, tuning, bonds, mechanics, tuned/exact rewind, batch turns, overlays, inspect, pan, zoom, rotate, world creation, unit placement, terrain paint, in-memory world round trip, and minimum window.");
            this.GetTree().Quit();
        } catch (Exception exception) { this.Log("INTERFACE FAILURE: " + exception); GD.PushError(exception.ToString()); this.GetTree().Quit(1); }
    }
    private async Task Capture(string name) {
        _ = await this.ToSignal(this.GetTree(), SceneTree.SignalName.ProcessFrame);
        _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = this.GetViewport().GetTexture().GetImage();
        Error error = image.SavePng(Path.Combine(this.sessionRoot, name));
        if (error != Error.Ok) {
            throw new IOException($"Cannot save screenshot: {error}");
        }
    }
}

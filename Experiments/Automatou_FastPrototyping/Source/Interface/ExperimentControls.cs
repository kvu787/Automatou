using Automatou.Simulation;
using Godot;

namespace Automatou.UserInterface;

public partial class MainInterface {
    private static readonly string[] InspectorTabNames = ["Behavior", "Tuning", "Unit"];
    private string experimentName = "Five-faction encounter";
    private string experimentDescription = "";
    private Label? comparisonLabel;
    private ExperimentResult? referenceResult;
    private int experimentShots, experimentChanges, checkpointShots, checkpointChanges;
    private int inspectorTab;
    private readonly Dictionary<int, (BehaviorSettings Settings, int? BondedUnitId)> experimentTuning = [];
    private sealed record ExperimentResult(int Turn, int Survivors, int Health, int Shots, int Changes, string Population);

    private CheckButton Toggle(Node parent, string caption, bool value, Action<bool> changed, string tooltip = "") {
        CheckButton control = new() { Name = caption.Replace(" ", ""), Text = caption, ButtonPressed = value, TooltipText = tooltip };
        control.Toggled += enabled => this.Guard(() => changed(enabled));
        parent.AddChild(control);
        return control;
    }

    private void AdvanceTurns(int count) {
        this.Pause();
        for (int turn = 0; turn < count; turn++) {
            this.Step();
        }

        this.LogResult("BATCH", this.Result());
        this.Status($"Advanced {count} turns. Select a unit to read its decision history.");
    }

    private ExperimentResult Result() {
        return new(this.world.Turn, this.world.Entities.Count,
        this.world.Entities.Sum(entity => entity.Health), this.experimentShots, this.experimentChanges,
        string.Join(", ", Catalog.FactionNames.Select((name, index) => $"{name}: {this.world.Entities.Count(entity => (int)entity.Faction == index)}")));
    }

    private void LogResult(string operation, ExperimentResult result) {
        this.Log(operation + " " + $"Experiment={this.experimentName} Result={result} Settings={this.world.Settings}");
    }

    private void RefreshComparison() {
        if (this.comparisonLabel is null || !IsInstanceValid(this.comparisonLabel)) {
            return;
        }

        ExperimentResult result = this.Result();
        this.comparisonLabel.Text = $"Now · turn {result.Turn}\n{result.Survivors} alive · {result.Health} health\n{result.Shots} shots · {result.Changes} intention changes";
        if (this.referenceResult is { } reference) {
            this.comparisonLabel.Text += $"\n\nPrevious · turn {reference.Turn}\n{reference.Survivors} alive · {reference.Health} health\n{reference.Shots} shots · {reference.Changes} intention changes";
            if (result.Turn == reference.Turn) {
                this.comparisonLabel.Text += $"\n\nDifference at same turn\nHealth {result.Health - reference.Health:+0;-0;0} · shots {result.Shots - reference.Shots:+0;-0;0}";
            } else {
                this.comparisonLabel.Text += $"\n\nCompare at turn {reference.Turn}.";
            }
        }
    }

    private void CaptureCheckpoint() {
        this.Pause();
        this.checkpoint = this.world.Copy();
        this.ResetTuningCache();
        this.checkpointShots = this.experimentShots; this.checkpointChanges = this.experimentChanges;
        this.referenceResult = null;
        this.LogResult("CHECKPOINT", this.Result());
        this.Refresh();
        this.Status($"Checkpoint saved at turn {this.world.Turn}. Rewind starts from this state, including memory and random state.");
    }

    private void RewindExperiment(bool preserveTuning) {
        int? selection = this.selected?.Id;
        SimulationSettings settings = this.world.Settings with { };
        this.CaptureLiveTuning();
        Dictionary<int, (BehaviorSettings Settings, int? BondedUnitId)> tuning = this.experimentTuning.ToDictionary(entry => entry.Key,
            entry => (Settings: entry.Value.Settings with { }, entry.Value.BondedUnitId));
        this.referenceResult = this.Result();
        this.LogResult(preserveTuning ? "COMPARE_TUNED" : "COMPARE_EXACT", this.referenceResult);
        World replacement = this.checkpoint.Copy();
        if (preserveTuning) {
            replacement.Settings = settings;
            foreach (Entity entity in replacement.Entities) {
                if (tuning.TryGetValue(entity.Id, out (BehaviorSettings Settings, int? BondedUnitId) value)) {
                    entity.Unit.Brain.Settings = value.Settings;
                    entity.BondedUnitId = value.BondedUnitId;
                }
            }
        }
        this.ReplaceWorld(replacement, false);
        this.experimentShots = this.checkpointShots; this.experimentChanges = this.checkpointChanges;
        this.selected = this.world.Entities.FirstOrDefault(entity => entity.Id == selection);
        Clear(this.toolsPanel); this.BuildPlaybackTools(); this.Refresh();
        this.Status(preserveTuning
            ? "Returned to checkpoint with current tuning. Memory, heat and positions come from the checkpoint."
            : "Returned to exact checkpoint, including its tuning, memory, heat and random state.");
    }

    private void BuildExperimentTools() {
        _ = this.Heading(this.toolsPanel, "REPLAY AND COMPARE");
        _ = this.Button(this.toolsPanel, "Rewind with tuning", () => this.RewindExperiment(true), "Keep each unit's latest settings and bonds, including casualties, then restore checkpoint positions, heat, memory and random state.");
        HBoxContainer replay = Row(this.toolsPanel);
        _ = this.Button(replay, "Exact rewind", () => this.RewindExperiment(false), "Restore every checkpoint value, including tuning.");
        _ = this.Button(replay, "Checkpoint", this.CaptureCheckpoint, "Replace the rewind starting point with this complete state.");
        this.comparisonLabel = Label(this.toolsPanel, "", 12, this.muted);
        this.RefreshComparison();
        _ = this.Heading(this.toolsPanel, "WORLD MECHANICS");
        _ = Label(this.toolsPanel, "Changes pause playback. Replay to compare.", 12, this.muted);
        _ = this.Toggle(this.toolsPanel, "Limited perception", this.world.Settings.LimitedPerception, value => this.ChangeMechanics(() => this.world.Settings = this.world.Settings with { LimitedPerception = value }), "Enemies can disappear beyond sight or behind forest.");
        _ = this.Toggle(this.toolsPanel, "Weapon heat", this.world.Settings.HeatEnabled, value => this.ChangeMechanics(() => this.world.Settings = this.world.Settings with { HeatEnabled = value }), "Weapons build heat; excess heat temporarily locks them.");
        _ = this.Toggle(this.toolsPanel, "Protective bonds", this.world.Settings.BondsEnabled, value => this.ChangeMechanics(() => this.world.Settings = this.world.Settings with { BondsEnabled = value }), "A bond can make a unit protect a particular ally.");
        _ = this.Heading(this.toolsPanel, "OBSERVATION");
        _ = this.Toggle(this.toolsPanel, "Show intentions", this.board.DecisionOverlay, value => { this.board.DecisionOverlay = value; this.board.QueueRedraw(); }, "Cyan: intended destination and remembered contacts. Pink: bonded ally's actual position, shown to the observer.");
        _ = this.Toggle(this.toolsPanel, "Dim unseen enemies", this.board.DimUnseenEnemies, value => { this.board.DimUnseenEnemies = value; this.board.QueueRedraw(); }, "From the selected unit's perspective. All units remain visible to you.");
        _ = Label(this.toolsPanel, "Cyan rings: remembered contacts\nCyan diamond: destination\nPink: bond (actual ally position)\nGold cells: forward attack region", 11, this.muted);
    }

    private void ChangeMechanics(Action change) {
        this.Pause(); change(); this.Refresh();
        this.Log("MECHANICS " + this.world.Settings);
        this.Status("World mechanics changed. Rewind with tuning to replay from the checkpoint.");
    }

    private void BuildDecisionInspector(Entity entity) {
        UnitAutomaton brain = entity.Unit.Brain;
        AutomatonMemory state = brain.State;
        _ = this.Heading(this.inspectorPanel, "CURRENT INTENTION");
        _ = Label(this.inspectorPanel, string.IsNullOrWhiteSpace(state.Intention) ? "Awaiting first turn" : state.Intention, 19, this.accent);
        _ = Label(this.inspectorPanel, string.IsNullOrWhiteSpace(state.Reason) ? "Advance one turn to see this unit's reasoning." : state.Reason, 13);
        HBoxContainer tabs = Row(this.inspectorPanel);
        foreach ((string? name, int index) in InspectorTabNames.Select((name, index) => (name, index))) {
            Button button = this.Button(tabs, name, () => { this.inspectorTab = index; this.BuildInspector(); });
            button.ToggleMode = true; button.ButtonPressed = this.inspectorTab == index;
            button.AddThemeFontSizeOverride("font_size", 12);
        }
        if (this.inspectorTab == 1) { this.BuildTuningControls(entity); return; }
        if (this.inspectorTab == 2) { this.BuildUnitDetails(entity); return; }
        _ = Label(this.inspectorPanel, $"Since turn {state.IntentionSince} · {brain.TurnsObserved} turns observed\nDestination: {state.Destination?.ToString() ?? "none"}\nTarget: {(brain.TargetId is { } target ? "#" + target : "none")}\n{state.ShotsFired} shots · {state.IntentionChanges} intention changes", 12, this.muted);
        if (state.Considerations.Count > 0) {
            _ = this.Heading(this.inspectorPanel, "DECISION SCORES");
            foreach (DecisionConsideration? consideration in state.Considerations.OrderByDescending(value => value.Score)) {
                _ = Label(this.inspectorPanel, $"{consideration.Name}  {consideration.Score:0.00}\n{consideration.Reason}", 12, this.muted);
            }
        }
        _ = this.Heading(this.inspectorPanel, "CONTACT MEMORY");
        _ = Label(this.inspectorPanel, state.Contacts.Count == 0 ? "No remembered enemies." : string.Join("\n", state.Contacts.Select(contact => $"#{contact.Id} at {contact.Position} · last seen T{contact.LastSeenTurn}")), 12, this.muted);
        _ = this.Heading(this.inspectorPanel, "RECENT DECISIONS");
        _ = Label(this.inspectorPanel, state.History.Count == 0 ? "No decisions yet." : string.Join("\n\n", state.History.TakeLast(5).Reverse().Select(decision => $"T{decision.Turn} · {decision.Intention}\n{decision.Reason}")), 12, this.muted);
    }

    private void BuildTuningControls(Entity entity) {
        UnitAutomaton brain = entity.Unit.Brain;
        _ = this.Heading(this.inspectorPanel, "TUNE THIS UNIT");
        if (entity.Unit is TrainingTarget) {
            _ = Label(this.inspectorPanel, "This inert training target holds position. Its policy does not use behavior tuning.", 13, this.muted);
            return;
        }
        _ = Label(this.inspectorPanel, "Changes pause playback. Rewind with tuning keeps these values.", 12, this.muted);
        void Tune(Action edit) {
            this.Pause(); edit();
            this.RememberTuning(entity);
            this.Log("TUNING " + $"Id={entity.Id} Settings={entity.Unit.Brain.Settings} BondedUnitId={entity.BondedUnitId}");
            this.board.QueueRedraw();
            this.Status($"Tuned #{entity.Id} {entity.Name}. Playback paused; Rewind with tuning replays the change.");
        }
        SpinBox Parameter(string name, double value, Action<double> assign, string tooltip, double minimum = 0, double maximum = 1, double step = .05) {
            SpinBox spin = this.Number(this.inspectorPanel, name, value, minimum, maximum, step);
            spin.Name = name.Replace(" ", ""); spin.Step = step; spin.TooltipText = tooltip;
            spin.ValueChanged += updated => this.Guard(() => Tune(() => assign(updated)));
            return spin;
        }
        _ = Parameter("Aggression", brain.Settings.Aggression, value => brain.Settings = brain.Settings with { Aggression = value }, "Higher values favor engagement. At 0.8 or above, firing may overrun the preferred heat ceiling and risk a weapon lock.");
        _ = Parameter("Caution", brain.Settings.Caution, value => brain.Settings = brain.Settings with { Caution = value }, "Higher values favor survival when wounded or threatened.");
        _ = Parameter("Commitment", brain.Settings.Commitment, value => brain.Settings = brain.Settings with { Commitment = value }, "Higher values favor continuing the current intention.");
        if (entity.Unit.HeatPerShot > 0) {
            _ = Parameter("Heat reserve", brain.Settings.HeatReserve, value => brain.Settings = brain.Settings with { HeatReserve = (int)value }, "Preferred heat ceiling. Below 0.8 aggression, next-shot heat is checked before firing. Higher aggression permits exceeding the ceiling.", 40, 100, 5);
        }

        _ = this.Toggle(this.inspectorPanel, "Remember contacts", brain.Settings.RememberContacts, value => Tune(() => brain.Settings = brain.Settings with { RememberContacts = value }));
        if (entity.Unit is PrytuHunter or PrytuManifestation) {
            _ = Label(this.inspectorPanel, "This unit's policy does not use individual bonds.", 12, this.muted);
            return;
        }
        _ = Label(this.inspectorPanel, "Protect a particular ally", 12, this.muted);
        Entity[] allies = this.world.Entities.Where(candidate => candidate != entity && candidate.Faction == entity.Faction).OrderBy(candidate => candidate.Id).ToArray();
        string[] bondNames = ["No bond", .. allies.Select(ally => $"#{ally.Id} {ally.Name}")];
        int bondIndex = Array.FindIndex(allies, ally => ally.Id == entity.BondedUnitId) + 1;
        this.Choice(this.inspectorPanel, bondNames, bondIndex, index => Tune(() => entity.BondedUnitId = index == 0 ? null : allies[index - 1].Id)).Name = "BondChoice";
        if (entity.BondedUnitId is { } bond && !allies.Any(ally => ally.Id == bond)) {
            _ = Label(this.inspectorPanel, $"Bonded unit #{bond} is no longer present.", 12, this.muted);
        }
    }

    private void BuildUnitDetails(Entity entity) {
        _ = this.Heading(this.inspectorPanel, "UNIT STATISTICS");
        Unit unit = entity.Unit;
        _ = Label(this.inspectorPanel, $"Origin {entity.Position} · {Hex.DirectionNames[entity.Facing]}\n{entity.OccupiedCells().Count()} occupied cells\n{unit.ActionPoints} action points / turn\n{unit.Damage} ranged · {unit.MeleeDamage} melee\n{unit.Range} range · {unit.Armor} front armor\n{unit.Evasion}% evasion · {unit.Mobility}", 13, this.muted);
        _ = this.Button(this.inspectorPanel, entity.Stationary ? "Mobilize unit" : "Deploy / hold position", () => {
            this.Pause(); entity.Stationary = !entity.Stationary;
            if (this.mode == "World creator") { this.RecordWorldEdit(); } else { this.Refresh(); }
        });
        if (this.mode == "World creator") {
            _ = this.Button(this.inspectorPanel, "Rotate 60°   [R]", this.RotateSelection);
            _ = this.Button(this.inspectorPanel, "Remove entity   [Delete]", this.DeleteSelection);
        }
    }

    private void RememberTuning(Entity entity) {
        this.experimentTuning[entity.Id] = (entity.Unit.Brain.Settings with { }, entity.BondedUnitId);
    }

    private void CaptureLiveTuning() {
        foreach (Entity entity in this.world.Entities) {
            this.RememberTuning(entity);
        }
    }

    private void ResetTuningCache() {
        this.experimentTuning.Clear();
        this.CaptureLiveTuning();
    }
}

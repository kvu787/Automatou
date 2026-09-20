using Godot;
using Automatou.Simulation;

namespace Automatou.Interface;

public partial class Laboratory
{
    private string experimentName = "Five-faction encounter";
    private string experimentDescription = "A larger encounter for observing interactions. Select a unit, inspect its reasons, then tune and replay.";
    private Label? comparisonLabel;
    private ExperimentResult? referenceResult;
    private int experimentShots, experimentChanges, checkpointShots, checkpointChanges;
    private int inspectorTab;
    private readonly Dictionary<int, (BehaviorSettings Settings, int? BondedUnitId)> experimentTuning = [];
    private sealed record ExperimentResult(int Turn, int Survivors, int Health, int Shots, int Changes, string Population);

    private CheckButton Toggle(Node parent, string caption, bool value, Action<bool> changed, string tooltip = "")
    {
        var control = new CheckButton { Name = caption.Replace(" ", ""), Text = caption, ButtonPressed = value, TooltipText = tooltip };
        control.Toggled += enabled => Guard(() => changed(enabled));
        parent.AddChild(control);
        return control;
    }

    private void AdvanceTurns(int count)
    {
        Pause();
        for (int turn = 0; turn < count; turn++) Step();
        LogResult("BATCH", Result());
        Status($"Advanced {count} turns. Select a unit to read its decision history.");
    }

    private ExperimentResult Result() => new(world.Turn, world.Entities.Count,
        world.Entities.Sum(entity => entity.Health), experimentShots, experimentChanges,
        string.Join(", ", Catalog.FactionNames.Select((name, index) => $"{name}: {world.Entities.Count(entity => (int)entity.Faction == index)}")));

    private void LogResult(string operation, ExperimentResult result) =>
        Log(operation + " " + System.Text.Json.JsonSerializer.Serialize(new { Experiment = experimentName, Result = result, Settings = world.Settings }));

    private void RefreshComparison()
    {
        if (comparisonLabel is null || !IsInstanceValid(comparisonLabel)) return;
        var result = Result();
        comparisonLabel.Text = $"Now · turn {result.Turn}\n{result.Survivors} alive · {result.Health} health\n{result.Shots} shots · {result.Changes} intention changes";
        if (referenceResult is { } reference)
        {
            comparisonLabel.Text += $"\n\nPrevious · turn {reference.Turn}\n{reference.Survivors} alive · {reference.Health} health\n{reference.Shots} shots · {reference.Changes} intention changes";
            if (result.Turn == reference.Turn)
                comparisonLabel.Text += $"\n\nDifference at same turn\nHealth {result.Health - reference.Health:+0;-0;0} · shots {result.Shots - reference.Shots:+0;-0;0}";
            else comparisonLabel.Text += $"\n\nCompare at turn {reference.Turn}.";
        }
    }

    private void CaptureCheckpoint()
    {
        Pause();
        checkpoint = Storage.Encode(world);
        ResetTuningCache();
        checkpointShots = experimentShots; checkpointChanges = experimentChanges;
        referenceResult = null;
        LogResult("CHECKPOINT", Result());
        Refresh();
        Status($"Checkpoint saved at turn {world.Turn}. Rewind starts from this state, including memory and random state.");
    }

    private void RewindExperiment(bool preserveTuning)
    {
        int? selection = selected?.Id;
        var settings = world.Settings with { };
        CaptureLiveTuning();
        var tuning = experimentTuning.ToDictionary(entry => entry.Key,
            entry => (Settings: entry.Value.Settings with { }, entry.Value.BondedUnitId));
        referenceResult = Result();
        LogResult(preserveTuning ? "COMPARE_TUNED" : "COMPARE_EXACT", referenceResult);
        var replacement = Storage.Decode(checkpoint);
        if (preserveTuning)
        {
            replacement.Settings = settings;
            foreach (var entity in replacement.Entities)
                if (tuning.TryGetValue(entity.Id, out var value))
                {
                    entity.Unit.Brain.Settings = value.Settings;
                    entity.BondedUnitId = value.BondedUnitId;
                }
        }
        ReplaceWorld(replacement, false);
        experimentShots = checkpointShots; experimentChanges = checkpointChanges;
        selected = world.Entities.FirstOrDefault(entity => entity.Id == selection);
        Clear(toolsPanel); BuildPlaybackTools(); Refresh();
        Status(preserveTuning
            ? "Returned to checkpoint with current tuning. Memory, heat and positions come from the checkpoint."
            : "Returned to exact checkpoint, including its tuning, memory, heat and random state.");
    }

    private void BuildExperimentTools()
    {
        Heading(toolsPanel, "REPLAY AND COMPARE");
        Button(toolsPanel, "Rewind with tuning", () => RewindExperiment(true), "Keep each unit's latest settings and bonds, including casualties, then restore checkpoint positions, heat, memory and random state.");
        var replay = Row(toolsPanel);
        Button(replay, "Exact rewind", () => RewindExperiment(false), "Restore every checkpoint value, including tuning.");
        Button(replay, "Checkpoint", CaptureCheckpoint, "Replace the rewind starting point with this complete state.");
        comparisonLabel = Label(toolsPanel, "", 12, muted);
        RefreshComparison();
        Heading(toolsPanel, "WORLD MECHANICS");
        Label(toolsPanel, "Changes pause playback. Replay to compare.", 12, muted);
        Toggle(toolsPanel, "Limited perception", world.Settings.LimitedPerception, value => ChangeMechanics(() => world.Settings = world.Settings with { LimitedPerception = value }), "Enemies can disappear beyond sight or behind forest.");
        Toggle(toolsPanel, "Weapon heat", world.Settings.HeatEnabled, value => ChangeMechanics(() => world.Settings = world.Settings with { HeatEnabled = value }), "Weapons build heat; excess heat temporarily locks them.");
        Toggle(toolsPanel, "Protective bonds", world.Settings.BondsEnabled, value => ChangeMechanics(() => world.Settings = world.Settings with { BondsEnabled = value }), "A bond can make a unit protect a particular ally.");
        Heading(toolsPanel, "OBSERVATION");
        Toggle(toolsPanel, "Show intentions", board.DecisionOverlay, value => { board.DecisionOverlay = value; board.QueueRedraw(); }, "Cyan: intended destination and remembered contacts. Pink: bonded ally's actual position, shown to the observer.");
        Toggle(toolsPanel, "Dim unseen enemies", board.DimUnseenEnemies, value => { board.DimUnseenEnemies = value; board.QueueRedraw(); }, "From the selected unit's perspective. All units remain visible to you.");
        Label(toolsPanel, "Cyan rings: remembered contacts\nCyan diamond: destination\nPink: bond (actual ally position)\nGold cells: forward attack region", 11, muted);
    }

    private void ChangeMechanics(Action change)
    {
        Pause(); change(); Refresh();
        Log("MECHANICS " + System.Text.Json.JsonSerializer.Serialize(world.Settings));
        Status("World mechanics changed. Rewind with tuning to replay from the checkpoint.");
    }

    private void BuildDecisionInspector(Entity entity)
    {
        var brain = entity.Unit.Brain;
        var state = brain.State;
        Heading(inspectorPanel, "CURRENT INTENTION");
        Label(inspectorPanel, string.IsNullOrWhiteSpace(state.Intention) ? "Awaiting first turn" : state.Intention, 19, accent);
        Label(inspectorPanel, string.IsNullOrWhiteSpace(state.Reason) ? "Advance one turn to see this unit's reasoning." : state.Reason, 13);
        var tabs = Row(inspectorPanel);
        foreach (var (name, index) in new[] { "Behavior", "Tuning", "Unit" }.Select((name, index) => (name, index)))
        {
            var button = Button(tabs, name, () => { inspectorTab = index; BuildInspector(); });
            button.ToggleMode = true; button.ButtonPressed = inspectorTab == index;
            button.AddThemeFontSizeOverride("font_size", 12);
        }
        if (inspectorTab == 1) { BuildTuningControls(entity); return; }
        if (inspectorTab == 2) { BuildUnitDetails(entity); return; }
        Label(inspectorPanel, $"Since turn {state.IntentionSince} · {brain.TurnsObserved} turns observed\nDestination: {state.Destination?.ToString() ?? "none"}\nTarget: {(brain.TargetId is { } target ? "#" + target : "none")}\n{state.ShotsFired} shots · {state.IntentionChanges} intention changes", 12, muted);
        if (state.Considerations.Count > 0)
        {
            Heading(inspectorPanel, "DECISION SCORES");
            foreach (var consideration in state.Considerations.OrderByDescending(value => value.Score))
                Label(inspectorPanel, $"{consideration.Name}  {consideration.Score:0.00}\n{consideration.Reason}", 12, muted);
        }
        Heading(inspectorPanel, "CONTACT MEMORY");
        Label(inspectorPanel, state.Contacts.Count == 0 ? "No remembered enemies." : string.Join("\n", state.Contacts.Select(contact => $"#{contact.Id} at {contact.Position} · last seen T{contact.LastSeenTurn}")), 12, muted);
        Heading(inspectorPanel, "RECENT DECISIONS");
        Label(inspectorPanel, state.History.Count == 0 ? "No decisions yet." : string.Join("\n\n", state.History.TakeLast(5).Reverse().Select(decision => $"T{decision.Turn} · {decision.Intention}\n{decision.Reason}")), 12, muted);
    }

    private void BuildTuningControls(Entity entity)
    {
        var brain = entity.Unit.Brain;
        Heading(inspectorPanel, "TUNE THIS UNIT");
        if (entity.Unit is TrainingTarget)
        {
            Label(inspectorPanel, "This inert training target holds position. Its policy does not use behavior tuning.", 13, muted);
            return;
        }
        Label(inspectorPanel, "Changes pause playback. Rewind with tuning keeps these values.", 12, muted);
        void Tune(Action edit)
        {
            Pause(); edit();
            RememberTuning(entity);
            Log("TUNING " + System.Text.Json.JsonSerializer.Serialize(new { entity.Id, entity.Unit.Brain.Settings, entity.BondedUnitId }));
            board.QueueRedraw();
            Status($"Tuned #{entity.Id} {entity.Name}. Playback paused; Rewind with tuning replays the change.");
        }
        SpinBox Parameter(string name, double value, Action<double> assign, string tooltip, double minimum = 0, double maximum = 1, double step = .05)
        {
            var spin = Number(inspectorPanel, name, value, minimum, maximum, step);
            spin.Name = name.Replace(" ", ""); spin.Step = step; spin.TooltipText = tooltip;
            spin.ValueChanged += updated => Guard(() => Tune(() => assign(updated)));
            return spin;
        }
        Parameter("Aggression", brain.Settings.Aggression, value => brain.Settings = brain.Settings with { Aggression = value }, "Higher values favor engagement. At 0.8 or above, firing may overrun the preferred heat ceiling and risk a weapon lock.");
        Parameter("Caution", brain.Settings.Caution, value => brain.Settings = brain.Settings with { Caution = value }, "Higher values favor survival when wounded or threatened.");
        Parameter("Commitment", brain.Settings.Commitment, value => brain.Settings = brain.Settings with { Commitment = value }, "Higher values favor continuing the current intention.");
        if (entity.Unit.HeatPerShot > 0)
            Parameter("Heat reserve", brain.Settings.HeatReserve, value => brain.Settings = brain.Settings with { HeatReserve = (int)value }, "Preferred heat ceiling. Below 0.8 aggression, next-shot heat is checked before firing. Higher aggression permits exceeding the ceiling.", 40, 100, 5);
        Toggle(inspectorPanel, "Remember contacts", brain.Settings.RememberContacts, value => Tune(() => brain.Settings = brain.Settings with { RememberContacts = value }));
        if (entity.Unit is PrytuHunter or PrytuManifestation)
        {
            Label(inspectorPanel, "This unit's policy does not use individual bonds.", 12, muted);
            return;
        }
        Label(inspectorPanel, "Protect a particular ally", 12, muted);
        var allies = world.Entities.Where(candidate => candidate != entity && candidate.Faction == entity.Faction).OrderBy(candidate => candidate.Id).ToArray();
        string[] bondNames = ["No bond", .. allies.Select(ally => $"#{ally.Id} {ally.Name}")];
        int bondIndex = Array.FindIndex(allies, ally => ally.Id == entity.BondedUnitId) + 1;
        Choice(inspectorPanel, bondNames, bondIndex, index => Tune(() => entity.BondedUnitId = index == 0 ? null : allies[index - 1].Id)).Name = "BondChoice";
        if (entity.BondedUnitId is { } bond && !allies.Any(ally => ally.Id == bond))
            Label(inspectorPanel, $"Bonded unit #{bond} is no longer present.", 12, muted);
    }

    private void BuildUnitDetails(Entity entity)
    {
        Heading(inspectorPanel, "UNIT STATISTICS");
        var unit = entity.Unit;
        Label(inspectorPanel, $"Origin {entity.Position} · {Hex.DirectionNames[entity.Facing]}\n{entity.OccupiedCells().Count()} occupied cells\n{unit.ActionPoints} action points / turn\n{unit.Damage} ranged · {unit.MeleeDamage} melee\n{unit.Range} range · {unit.Armor} front armor\n{unit.Evasion}% evasion · {unit.Mobility}", 13, muted);
        Button(inspectorPanel, entity.Stationary ? "Mobilize unit" : "Deploy / hold position", () => { Pause(); entity.Stationary = !entity.Stationary; Refresh(); });
        if (mode == "World creator")
        {
            Button(inspectorPanel, "Rotate 60°   [R]", RotateSelection);
            Button(inspectorPanel, "Remove entity   [Delete]", DeleteSelection);
        }
    }

    private void RememberTuning(Entity entity) =>
        experimentTuning[entity.Id] = (entity.Unit.Brain.Settings with { }, entity.BondedUnitId);

    private void CaptureLiveTuning()
    {
        foreach (var entity in world.Entities) RememberTuning(entity);
    }

    private void ResetTuningCache()
    {
        experimentTuning.Clear();
        CaptureLiveTuning();
    }
}

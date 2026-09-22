using Automatou.Simulation;


int passed = 0;
int failed = 0;
string[] systemCallNames = ["get_RemainingEnergy", "ScanVision", "SurveyTerrain"];
string[] spacingPrograms = ["Lone wolf", "Keep your distance"];
string[] specialPrograms = ["Lone wolf", "Keep your distance", "Hunt the weakest"];
bool[] observationOptions = [false, true];
int[] movementGaps = [1, 2];
bool SameMemory(AutomatonMemory first, AutomatonMemory second) {
    return (first.Intention, first.Reason, first.IntentionSince, first.LastUpdatedTurn, first.Destination,
        first.ShotsFired, first.IntentionChanges, first.PatrolIndex) ==
        (second.Intention, second.Reason, second.IntentionSince, second.LastUpdatedTurn, second.Destination,
        second.ShotsFired, second.IntentionChanges, second.PatrolIndex) &&
        first.Contacts.SequenceEqual(second.Contacts) && first.Considerations.SequenceEqual(second.Considerations) &&
        first.History.SequenceEqual(second.History) && first.PreviousOutcomes.SequenceEqual(second.PreviousOutcomes);
}
bool SameWorld(World first, World second) {
    return (first.Turn, first.NextId, first.RandomState, first.Casualties, first.Settings) ==
        (second.Turn, second.NextId, second.RandomState, second.Casualties, second.Settings) &&
        first.Terrain.SequenceEqual(second.Terrain) && first.Events.SequenceEqual(second.Events) && first.Effects.SequenceEqual(second.Effects) &&
        first.Entities.Count == second.Entities.Count && first.Entities.Zip(second.Entities).All(pair => {
            Entity a = pair.First, b = pair.Second;
            return (a.Id, a.Faction, a.Position, a.Facing, a.Health, a.Stationary, a.Heat, a.WeaponLocked, a.BondedUnitId) ==
                (b.Id, b.Faction, b.Position, b.Facing, b.Health, b.Stationary, b.Heat, b.WeaponLocked, b.BondedUnitId) &&
                a.ShotsFired == b.ShotsFired && a.LastTurn.RemainingEnergy == b.LastTurn.RemainingEnergy && a.LastTurn.Submitted.SequenceEqual(b.LastTurn.Submitted) && a.LastTurn.Outcomes.SequenceEqual(b.LastTurn.Outcomes) && a.LastTurn.Sensing.SequenceEqual(b.LastTurn.Sensing) && a.Unit.GetType() == b.Unit.GetType() && a.Unit.Statistics == b.Unit.Statistics &&
                a.Unit.AutomatonInstance.GetType() == b.Unit.AutomatonInstance.GetType() &&
                (a.Unit.AutomatonInstance.TurnsObserved, a.Unit.AutomatonInstance.TargetId, a.Unit.AutomatonInstance.Settings) ==
                (b.Unit.AutomatonInstance.TurnsObserved, b.Unit.AutomatonInstance.TargetId, b.Unit.AutomatonInstance.Settings) &&
                SameMemory(a.Unit.AutomatonInstance.State, b.Unit.AutomatonInstance.State);
        });
}
List<string> report = [];
void Check(bool condition, string message) {
    if (!condition) {
        throw new InvalidOperationException(message);
    }
}
void Test(string name, Action test) {
    // Report assertion failures without an unhandled CLR exception. On Windows an
    // unhandled test exception can open a system error dialog and interrupt the user.
    try {
        test(); passed++; report.Add($"PASS {name}");
    } catch (Exception exception) {
        failed++; report.Add($"FAIL {name}\n{exception}");
    }
    Console.WriteLine(report[^1]);
}
Entity Unit(World world, Hex position, Faction faction = Faction.Bastions, Unit? design = null, int facing = 0) {
    Entity unit = new() { Unit = design ?? new TestUnit(), Position = position, Faction = faction, Facing = facing };
    Check(world.Add(unit, out string reason), reason); return unit;
}
void Reject(Action action) {
    try { action(); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { return; }
    throw new InvalidOperationException("Invalid input was accepted.");
}

Test("Session worlds are independent snapshots and new sessions start empty", () => {
    SessionWorlds session = new();
    World original = ScenarioCatalog.All[0].Create();
    World snapshot = original.Copy();
    session.SaveWorld("  My world  ", original);
    original.Step();
    Check(SameWorld(session.LoadWorld("My world"), snapshot), "Editing after saving leaves the snapshot intact");
    World loaded = session.LoadWorld("My world");
    loaded.Step();
    Check(SameWorld(session.LoadWorld("My world"), snapshot), "Playing a loaded world leaves the snapshot intact");
    session.SaveWorld("my world", loaded);
    Check(session.WorldNames.Count() == 1 && SameWorld(session.LoadWorld("My world"), loaded), "Saving the same name replaces its snapshot");
    Check(!new SessionWorlds().WorldNames.Any(), "A new session has no saved worlds");
    Reject(() => session.SaveWorld("   ", original));
});

Test("World copies detach every mutable collection, automaton, and event subscriber", () => {
    World original = ScenarioCatalog.All[0].Create();
    for (int turn = 0; turn < 5; turn++) { original.Step(); }
    int notifications = 0;
    original.EventRecorded = _ => notifications++;
    World copy = original.Copy();
    Check(SameWorld(original, copy), "All snapshot state is preserved");
    Check(copy.EventRecorded is null, "Live event subscriber is not copied");
    Check(!ReferenceEquals(original.Settings, copy.Settings), "Separate world settings");
    foreach ((Entity source, Entity target) in original.Entities.Zip(copy.Entities)) {
        Check(!ReferenceEquals(source, target) && !ReferenceEquals(source.Unit, target.Unit), "Separate entities and units");
        UnitAutomaton a = source.Unit.AutomatonInstance, b = target.Unit.AutomatonInstance;
        Check(!ReferenceEquals(a, b) && !ReferenceEquals(a.Settings, b.Settings) && !ReferenceEquals(a.State, b.State), "Separate automata, settings, and state");
        Check(!ReferenceEquals(a.State.Contacts, b.State.Contacts) && !ReferenceEquals(a.State.Considerations, b.State.Considerations) && !ReferenceEquals(a.State.History, b.State.History), "Separate memory lists");
        foreach ((ContactMemory first, ContactMemory second) in a.State.Contacts.Zip(b.State.Contacts)) {
            Check(!ReferenceEquals(first, second), "Mutable contacts are copied");
            second.SearchStep++;
            Check(first.SearchStep != second.SearchStep, "Changing a contact leaves the source intact");
        }
        foreach (Hex cell in target.OccupiedCells()) { Check(ReferenceEquals(copy.At(cell), target), "Occupancy uses copied entities"); }
    }
    copy.Terrain.Clear(); copy.Entities.Clear(); copy.Events.Clear(); copy.Effects.Clear();
    copy.Note("Copy only");
    Check(original.Terrain.Count > 0 && original.Entities.Count > 0 && notifications == 0, "World collections and callbacks are independent");
});

Test("Odd rows offset east; public coordinates round-trip including negatives", () => {
    for (int y = -20; y <= 20; y++) {
        for (int x = -20; x <= 20; x++) { Hex cell = Hex.FromOffset(x, y); Check(cell.X == x && cell.Y == y, "Offset conversion"); }
    }

    Check(Hex.FromOffset(0, 0) + Hex.Directions[1] == Hex.FromOffset(0, 1), "Northeast");
    Check(Hex.FromOffset(0, 1) + Hex.Directions[2] == Hex.FromOffset(0, 2), "Northwest");
});
Test("Regular footprints and six rotations preserve distances", () => {
    int[] counts = [1, 7, 19, 37];
    for (int size = 1; size <= 4; size++) {
        Check(Hex.Disk(size).Count() == counts[size - 1], "Footprint count");
    }

    foreach (Hex cell in Hex.Disk(5)) { Check(cell.Rotate(6) == cell, "Full rotation"); Check(cell.Rotate(1).Distance(new()) == cell.Distance(new()), "Distance preserved"); }
});
Test("Rectangle and hexagon authoring start empty on plains and reject invalid map sizes", () => {
    World rectangle = World.Create(false, 17, 9), hexagon = World.Create(true, 4, 1), singleCell = World.Create(true, 1, 1);
    Check(rectangle.Terrain.Count == 153, "Rectangle");
    Check(hexagon.Terrain.Count == 37, "Hexagon");
    Check(singleCell.Terrain.Count == 1 && singleCell.Terrain.ContainsKey(new Hex()), "Size-one hexagon");
    foreach (World world in new[] { rectangle, hexagon, singleCell }) {
        Check(world.Terrain.Values.All(terrain => terrain == Terrain.Plains), "New maps contain only plains");
        Check(world.Entities.Count == 0 && world.Turn == 0, "New maps start empty at turn zero");
    }
    Check(World.Create(false, 200, 100).Terrain.Count == 20000, "Rectangle at cell limit");
    Check(World.Create(true, 82, 1).Terrain.Count == 19927, "Largest allowed hexagon");
    Reject(() => World.Create(false, 0, 1)); Reject(() => World.Create(true, 83, 1));
    Reject(() => World.Create(false, 200, 101));
    Reject(() => World.Create(false, int.MaxValue, int.MaxValue));
    Reject(() => World.Create(true, int.MaxValue, 1));
});
Test("Adding the same entity twice leaves identity, health, and occupancy intact", () => {
    World world = World.Create(false, 10, 10);
    Entity entity = Unit(world, Hex.FromOffset(4, 4));
    entity.Health -= 10;
    World before = world.Copy();
    Check(!world.Add(entity, out string reason) && !string.IsNullOrWhiteSpace(reason), "Duplicate entity is rejected");
    Check(SameWorld(world, before), "Rejected addition does not mutate the world");
    Check(ReferenceEquals(world.At(entity.Position), entity), "Occupancy remains intact");
    Entity neighbor = Unit(world, Hex.FromOffset(5, 4));
    Check(neighbor.Id == before.NextId, "Rejected addition does not consume an identity");
});
Test("Footprints respect borders, occupied cells and movement domains", () => {
    World world = World.Create(false, 20, 20);
    Entity unit = Unit(world, Hex.FromOffset(6, 6), design: new TestUnit { Size = 3 });
    Check(world.At(Hex.FromOffset(6, 6) + new Hex(2, 0)) == unit, "Outer occupancy");
    Check(!world.CanOccupy(unit, Hex.FromOffset(0, 0), out _), "Border rejection");
    Check(!world.Paint(unit.Position, Terrain.Water), "Occupied terrain rejection");
    Check(!world.Add(new() { Unit = new TestUnit(), Position = unit.Position }, out _), "Overlap rejection");
    Check(World.Traversable(new TestUnit() { Mobility = Mobility.Flight }, Terrain.Mountain), "Flight");
    Check(!World.Traversable(new TestUnit() { Mobility = Mobility.Spaceflight }, Terrain.ExclusionZone), "Exclusion");
});
Test("Forward attacks, rear armor and turn energy affects combat", () => {
    World world = World.Create(false, 20, 10);
    Entity attacker = Unit(world, Hex.FromOffset(4, 4), design: new TestUnit() { TurnEnergy = 1, Range = 1 });
    Entity defender = Unit(world, attacker.Position + new Hex(1, 0), Faction.Prytu, new TestUnit() { Armor = 20, Behavior = TestBehavior.Hold, TurnEnergy = 1 });
    Check(world.CanAttack(attacker, defender), "Forward arc"); attacker.Facing = 3;
    Check(!world.CanAttack(attacker, defender), "Rear arc blocked");
    Check(World.ArmorAgainst(defender, defender.Position + new Hex(1, 0)) == 20, "Front armor");
    Check(World.ArmorAgainst(defender, defender.Position - new Hex(1, 0)) == 5, "Rear armor");
    world.Step(); Check(attacker.Health == 80 && defender.Health == 80, "No attack without sufficient energy");
});
Test("Blast damage includes allies and clears destroyed unit footprints", () => {
    World world = World.Create(false, 20, 20);
    Entity attacker = Unit(world, Hex.FromOffset(3, 8), design: new TestUnit() { Damage = 100, Range = 10, BlastRadius = 1 });
    Entity enemy = Unit(world, attacker.Position + new Hex(4, 0), Faction.Prytu, new TestUnit { Health = 20 });
    Entity ally = Unit(world, enemy.Position + new Hex(1, 0), design: new TestUnit() { Health = 20 });
    world.Attack(attacker, enemy);
    Check(!world.Entities.Contains(enemy) && !world.Entities.Contains(ally), "Friendly splash and destruction");
    Check(world.At(enemy.Position) is null && world.At(ally.Position) is null && world.Casualties == 2, "Destroyed footprints cleared");
});
Test("Units route around impassable terrain and engage", () => {
    World world = World.Create(false, 18, 14);
    // This fixture measures route finding, not contact acquisition around an obstacle.

    Entity attacker = Unit(world, Hex.FromOffset(3, 6), design: new TestUnit() { TurnEnergy = 5, Range = 1, Damage = 10, SightRange = 30 });
    Entity defender = Unit(world, Hex.FromOffset(12, 6), Faction.Prytu, new TestUnit() { Behavior = TestBehavior.Hold, Health = 1000, Damage = 1 });
    for (int y = 3; y < 10; y++) {
        world.Terrain[Hex.FromOffset(8, y)] = Terrain.Water;
    }

    for (int i = 0; i < 25; i++) {
        world.Step();
    }

    Check(defender.Health < 1000, "Reached enemy past wall");
    Check(attacker.OccupiedCells().All(c => world.Terrain[c] != Terrain.Water), "Never entered water");
});
Test("Combat measures range to a large unit's occupied edge", () => {
    World world = World.Create(false, 20, 15);
    Entity attacker = Unit(world, Hex.FromOffset(4, 5), design: new TestUnit { Range = 1 });
    Entity defender = Unit(world, attacker.Position + new Hex(3, 0), Faction.Prytu, new TestUnit { Size = 3, Health = 500 });
    Check(World.Separation(attacker, defender) == 1 && world.CanAttack(attacker, defender), "Adjacent footprint is in range");
    world.Attack(attacker, defender);
    Check(defender.Health < 500, "Attack reaches occupied edge");
});
Test("Skirmish, hold and deployed units obey their movement rules", () => {
    World world = World.Create(false, 30, 20);
    Entity skirmisher = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit() { Behavior = TestBehavior.Skirmish, Range = 4, TurnEnergy = 6 });
    Entity holder = Unit(world, Hex.FromOffset(7, 5), Faction.Prytu, new TestUnit() { Behavior = TestBehavior.Hold, Range = 1, TurnEnergy = 1 });
    Entity deployed = Unit(world, Hex.FromOffset(15, 15)); deployed.Stationary = true;
    Hex origin = holder.Position; Hex deployedOrigin = deployed.Position;
    world.Step();
    Check(skirmisher.Position.Distance(origin) > 2, "Cautious skirmisher retreats");
    Check(holder.Position == origin && deployed.Position == deployedOrigin, "Hold and deploy stay put");
});
Test("World saves preserve health, rotations, units and deterministic continuation", () => {
    World original = World.Demonstration();
    for (int i = 0; i < 8; i++) {
        original.Step();
    }

    World restored = original.Copy();
    Check(SameWorld(original, restored), "Exact round trip");
    for (int i = 0; i < 12; i++) { original.Step(); restored.Step(); }
    Check(SameWorld(original, restored), "Deterministic replay");
});
Test("Five-faction encounter remains consistent for 120 turns", () => {
    World world = World.Demonstration();
    Check(world.Entities.Select(e => e.Faction).Distinct().Count() == 5, "All factions present");
    Check(world.Terrain.Count == 34 * 24 && world.Entities.Count == 28, "Original encounter dimensions and population");
    // Fingerprint captured from the encounter before terrain generation was removed.
    string terrainRows = string.Concat(Enumerable.Range(0, 24).SelectMany(y => Enumerable.Range(0, 34).Select(x => "FPMWE"[(int)world.Terrain[Hex.FromOffset(x, y)]])));
    string terrainHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(terrainRows)));
    Check(terrainHash == "6129905157C57A02559D1D1500D4A2C128FDBF089C1B9BDF23933EC97A43FE59", "Exact original encounter terrain, including starting clearings");
    for (int i = 0; i < 120; i++) {
        world.Step();
        Hex[] cells = world.Entities.SelectMany(e => e.OccupiedCells()).ToArray();
        Check(cells.Distinct().Count() == cells.Length, "No overlapping footprints");
        Check(cells.All(world.Terrain.ContainsKey), "No out-of-bounds entity");
        Check(world.Entities.All(e => e.Health > 0), "No destroyed entity retained");
    }
    Check(world.Casualties > 5, "Encounter produces combat");
    report.Add($"Encounter: {world.Turn} turns, {world.Entities.Count} survivors, {world.Casualties} casualties.");
});

Test("Programs depend only on the public contract and never the host", () => {
    System.Reflection.Assembly programs = typeof(StandardAutomaton).Assembly;
    System.Reflection.Assembly contract = typeof(IAutomatonSystemCalls).Assembly;
    Check(programs != contract && contract != typeof(World).Assembly, "Separate assemblies");
    Check(programs.GetReferencedAssemblies().All(reference => reference.Name == contract.GetName().Name || reference.Name!.StartsWith("System", StringComparison.Ordinal)), "No host dependency");
    Check(contract.GetTypes().All(type => type != typeof(World) && type != typeof(Entity) && type != typeof(Unit)), "No world implementation in the contract");
    Check(typeof(IAutomatonSystemCalls).GetMethods().Select(method => method.Name).Order().SequenceEqual(systemCallNames.Order()), "Small explicit syscall surface");
});

Test("Every successful sense costs energy and exhaustion returns no information", () => {
    World world = World.Create(false, 15, 15);
    Entity actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { TurnEnergy = 4 });
    AutomatonKernel kernel = new(world, actor);
    Check(kernel.ScanVision() is not null && kernel.RemainingEnergy == 3, "Base scan costs one");
    Check(kernel.ScanVision() is not null && kernel.RemainingEnergy == 2, "Repeated scan also costs one");
    Check(kernel.SurveyTerrain() is not null && kernel.SurveyTerrain() is not null, "Each terrain call costs one");
    Check(kernel.RemainingEnergy == 0 && kernel.ScanVision() is null && kernel.SurveyTerrain() is null, "No free information after exhaustion");
    Check(kernel.Receipts.Sum(receipt => receipt.EnergySpent) == 4, "Accurate receipts");
    Reject(() => kernel.ScanVision(-1)); Reject(() => kernel.ScanVision(13));
    kernel.Close();
    bool rejected = false;
    try { _ = kernel.ScanVision(); } catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "Retained capabilities expire at submission");
});

Test("Bastion vision has long base range and paid extension obeys concealment", () => {
    World world = World.Create(false, 40, 20);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new Bastion());
    Entity target = Unit(world, actor.Position + new Hex(17, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    Check(actor.Unit.SightRange == 14, "Long baseline sight");
    AutomatonKernel kernel = new(world, actor);
    Check(kernel.ScanVision()!.World.Entities.All(entity => entity.Id != target.Id), "Outside base range");
    Check(kernel.ScanVision(3)!.World.Entities.Any(entity => entity.Id == target.Id), "Pay for extra range");
    Check(kernel.RemainingEnergy == actor.Unit.TurnEnergy - 5, "Extension price is 1 plus extra range");
    world.Terrain[target.Position] = Terrain.Forest;
    AutomatonKernel concealed = new(world, actor);
    Check(concealed.ScanVision(3)!.World.Entities.All(entity => entity.Id != target.Id), "Extra range does not see through forest");
});

Test("Observations contain detached values without memory or hidden occupancy", () => {
    World world = World.Create(false, 25, 15);
    Entity actor = Unit(world, Hex.FromOffset(4, 5), design: new TestUnit { SightRange = 3 });
    Entity visible = Unit(world, actor.Position + new Hex(2, 0), Faction.Prytu);
    Entity hidden = Unit(world, actor.Position + new Hex(9, 0), Faction.Prytu);
    visible.BondedUnitId = hidden.Id;
    AutomatonKernel kernel = new(world, actor);
    VisionObservation vision = kernel.ScanVision()!;
    TerrainObservation terrain = kernel.SurveyTerrain()!;
    Check(vision.World.Entities.Count == 2 && vision.World.Entities.All(entity => entity.Id != hidden.Id), "Only visible entities returned");
    Check(vision.World.Entities.Single(entity => entity.Id == visible.Id).BondedUnitId is null, "Other programs' bonds remain private");
    Check(((ICollection<EntityObservation>)vision.World.Entities).IsReadOnly && ((ICollection<Hex>)vision.World.Self.Cells).IsReadOnly, "Entity arrays are read-only");
    Check(((IDictionary<Hex, Terrain>)terrain.Cells).IsReadOnly, "Terrain dictionary is read-only");
    vision.Settings.HeatEnabled = false; visible.Health--;
    Check(world.Settings.HeatEnabled && vision.World.Entities.Single(entity => entity.Id == visible.Id).Health == visible.MaximumHealth, "Detached settings and entity state");
    ActionPlanning plan = new(vision, terrain, 4);
    Check(plan.CanOccupy(hidden.Position), "User-space route planning cannot query hidden occupants");
});

Test("All programs plan from one snapshot before any request executes", () => {
    World world = World.Create(false, 20, 15);
    Entity first = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit { Actions = _ => new([new MoveForwardAction()]) });
    Hex origin = first.Position;
    Hex? observed = null;
    _ = Unit(world, origin + new Hex(4, 0), design: new TestUnit {
        Actions = system => {
            observed = system.ScanVision()!.World.Entities.Single(entity => entity.Id == first.Id).Position;
            return ActionPlan.Empty;
        }
    });
    world.Step();
    Check(observed == origin && first.Position == origin + Hex.Directions[0], "Later program sees earlier actor's pre-move position");
});

Test("World enforces the shared energy budget even for custom programs", () => {
    World world = World.Create(false, 20, 15);
    Entity actor = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit {
        TurnEnergy = 3, Actions = system => {
            _ = system.ScanVision(); _ = system.SurveyTerrain();
            return new([new MoveForwardAction(), new MoveForwardAction(), new TurnAction(1)]);
        }
    });
    Hex origin = actor.Position;
    world.Step();
    Check(actor.Position == origin + Hex.Directions[0] && actor.LastTurn.RemainingEnergy == 0, "Only one move remains after sensing");
    Check(actor.LastTurn.Outcomes.Select(outcome => outcome.Succeeded).SequenceEqual([true, false, false]), "Rejected request ends plan");
    Check(actor.LastTurn.Outcomes[2].Reason.Contains("Skipped", StringComparison.Ordinal), "Skipped suffix recorded");
});

Test("Attacks require a purchased observation and execute at most once", () => {
    foreach (bool observe in observationOptions) {
        World world = World.Create(false, 20, 15);
        int targetId = 0;
        Entity actor = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit {
            TurnEnergy = 10, Actions = system => {
                if (observe) { _ = system.ScanVision(); }
                return new([new AttackAction(targetId), new AttackAction(targetId)]);
            }
        });
        Entity target = Unit(world, actor.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
        targetId = target.Id;
        world.Step();
        Check(actor.ShotsFired == (observe ? 1 : 0), "No guessed IDs or extra attacks");
        Check(actor.LastTurn.Outcomes.Count == 2 && !actor.LastTurn.Outcomes[1].Succeeded, "Rejected second request reported");
    }
});

Test("Conflicting destinations and swaps are resolved by world systems", () => {
    foreach (int gap in movementGaps) {
        World world = World.Create(false, 20, 15);
        Entity first = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit { Actions = _ => new([new MoveForwardAction()]) });
        Entity second = Unit(world, first.Position + new Hex(gap, 0), design: new TestUnit { Actions = _ => new([new MoveForwardAction()]) }, facing: 3);
        Hex a = first.Position, b = second.Position;
        world.Step();
        Check(first.Position == a && second.Position == b, "Both contenders stay");
        Check(!first.LastTurn.Outcomes[0].Succeeded && !second.LastTurn.Outcomes[0].Succeeded, "Both receive rejections");
        Check(first.LastTurn.RemainingEnergy == first.Unit.TurnEnergy, "Rejected moves spend no action energy");
    }
});

Test("Move conflicts include large footprints with different centers", () => {
    World world = World.Create(false, 25, 20);
    Entity first = Unit(world, Hex.FromOffset(6, 8), design: new TestUnit { Size = 2, Actions = _ => new([new MoveForwardAction()]) });
    Entity second = Unit(world, first.Position + new Hex(4, 0), design: new TestUnit { Size = 2, Actions = _ => new([new MoveForwardAction()]) }, facing: 3);
    Hex origin = first.Position;
    world.Step();
    Check(first.Position == origin && second.Position == origin + new Hex(4, 0), "Overlapping destinations rejected despite distinct centers");
    Check(first.LastTurn.Outcomes[0].Reason.Contains("Conflicting", StringComparison.Ordinal), "Conflict is reported");
});

Test("Hidden physical blockers reject plans without revealing identity", () => {
    World world = World.Create(false, 20, 15);
    Entity actor = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit {
        SightRange = 1, Actions = system => {
            Check(system.ScanVision()!.World.Entities.Count == 1, "Blocker not observed");
            return new([new MoveForwardAction(), new MoveForwardAction()]);
        }
    });
    Entity hidden = Unit(world, actor.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    world.Step();
    Check(World.Separation(actor, hidden) == 1 && actor.LastTurn.Outcomes[0].Succeeded && !actor.LastTurn.Outcomes[1].Succeeded, "World prevents hidden overlap");
    Check(!actor.LastTurn.Outcomes[1].Reason.Contains('#'), "No occupant identity leaked by rejection");
});

Test("Program exceptions and oversized submissions cannot prevent other turns", () => {
    World world = World.Create(false, 20, 15);
    Entity broken = Unit(world, Hex.FromOffset(3, 3), design: new TestUnit { Actions = _ => throw new InvalidOperationException("Example failure") });
    Entity oversized = Unit(world, Hex.FromOffset(3, 7), design: new TestUnit { Actions = _ => new(Enumerable.Repeat<UnitAction>(new TurnAction(1), 33).ToArray()) });
    Entity healthy = Unit(world, Hex.FromOffset(3, 11), design: new TestUnit { Actions = _ => new([new MoveForwardAction()]) });
    Hex start = healthy.Position;
    world.Step(); world.Step();
    Check(broken.LastTurn.Error == "Example failure" && oversized.LastTurn.Error.Contains("oversized", StringComparison.Ordinal), "Invalid programs reported");
    Check(healthy.Position == start + new Hex(2, 0), "Other programs continue");
});

Test("Resolution results arrive through next turn's paid senses", () => {
    World world = World.Create(false, 20, 15);
    int turn = 0;
    Entity actor = Unit(world, Hex.FromOffset(4, 6), design: new TestUnit {
        Actions = system => {
            VisionObservation observation = system.ScanVision()!;
            Check(observation.PreviousOutcomes.Count == turn, "Prior results only");
            if (turn > 0) { Check(observation.PreviousOutcomes[0].Succeeded && observation.PreviousOutcomes[0].Turn == 1, "Correct resolved turn"); }
            turn++;
            return new([new MoveForwardAction()]);
        }
    });
    world.Step(); world.Step();
    Check(actor.LastTurn.Outcomes[0].Turn == 2, "Current report stays host-owned");
});

Test("Lone wolf separates before attacking and resumes combat when safe", () => {
    World world = World.Create(false, 30, 20);
    TestUnit design = new() {
        Range = 8, TurnEnergy = 8, AutomatonInstance = new LoneWolfAutomaton { Settings = new() { Spacing = 3, Aggression = 1, Caution = 0 } }
    };
    Entity actor = Unit(world, Hex.FromOffset(10, 10), design: design, facing: 3);
    Entity ally = Unit(world, actor.Position + new Hex(1, 0), design: new TestUnit { Behavior = TestBehavior.Hold });
    _ = Unit(world, actor.Position + new Hex(-5, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    world.Step();
    Check(World.Separation(actor, ally) >= 3 && actor.ShotsFired == 0, "Separate this turn even with an attack available");
    Check(actor.LastTurn.Submitted.All(action => action is not AttackAction), "No lower-priority attack submitted");
    world.Step();
    Check(actor.ShotsFired == 1 && World.Separation(actor, ally) >= 3, "Standard combat resumes after spacing satisfied");
});

Test("Keep your distance separates from all enemies with a hard priority", () => {
    World world = World.Create(false, 30, 25);
    TestUnit design = new() {
        Range = 7, TurnEnergy = 10, AutomatonInstance = new KeepYourDistanceAutomaton { Settings = new() { Spacing = 4, Aggression = 1, Caution = 0 } }
    };
    Entity actor = Unit(world, Hex.FromOffset(12, 12), design: design, facing: 3);
    Entity first = Unit(world, actor.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    Entity second = Unit(world, actor.Position + new Hex(2, -2), Faction.Travelers, new TestUnit { Behavior = TestBehavior.Hold });
    world.Step();
    Check(World.Separation(actor, first) >= 4 && World.Separation(actor, second) >= 4, "Account for every observed enemy");
    Check(actor.ShotsFired == 0 && actor.LastTurn.Submitted.All(action => action is not AttackAction), "Separation is first priority");
});

Test("Blocked or deployed spacing programs wait without opportunistic attacks", () => {
    foreach (string name in spacingPrograms) {
        World world = World.Create(false, 20, 15);
        Entity actor = Unit(world, Hex.FromOffset(6, 6), design: new TestUnit { Range = 5 });
        AutomatonCatalog.Assign(actor.Unit, name); actor.Unit.AutomatonInstance.Settings.Spacing = 3;
        _ = Unit(world, actor.Position + new Hex(1, 0), name == "Lone wolf" ? actor.Faction : Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
        _ = Unit(world, actor.Position + new Hex(2, 0), Faction.Travelers, new TestUnit { Behavior = TestBehavior.Hold });
        foreach (Hex adjacent in Hex.Directions.Select(direction => actor.Position + direction).Where(cell => world.At(cell) is null)) { world.Terrain[adjacent] = Terrain.Water; }
        Hex start = actor.Position;
        world.Step();
        Check(actor.Position == start && actor.ShotsFired == 0, "Blocked priority cannot fall through to attacking");
        actor.Stationary = true; world.Step();
        Check(actor.ShotsFired == 0, "Deployed priority cannot fall through either");
    }
});

Test("Spacing counts nearest footprint edges and protects already-safe positions", () => {
    World world = World.Create(false, 30, 20);
    TestUnit design = new() {
        Range = 2, TurnEnergy = 10, AutomatonInstance = new LoneWolfAutomaton { Settings = new() { Spacing = 3, Aggression = 1, Caution = 0 } }
    };
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: design);
    Entity ally = Unit(world, actor.Position + new Hex(4, 0), design: new TestUnit { Size = 2, Behavior = TestBehavior.Hold });
    Entity enemy = Unit(world, actor.Position + new Hex(9, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    Check(World.Separation(actor, ally) == 3, "Edge distance differs from center distance");
    world.Step();
    Check(World.Separation(actor, ally) >= 3, "Approaching enemy cannot cross safe spacing");
    Check(enemy.Health == enemy.MaximumHealth || actor.ShotsFired == 1, "Only legitimate target attacked");
});

Test("Keep your distance rejects melee bodies and invalid N", () => {
    TestUnit melee = new() { Range = 1 };
    Reject(() => AutomatonCatalog.Assign(melee, "Keep your distance"));
    TestUnit ranged = new() { Range = 2 };
    AutomatonCatalog.Assign(ranged, "Keep your distance");
    Check(ranged.AutomatonInstance.Settings.Spacing == 1, "Default N adjusted below range");
    ranged.AutomatonInstance.Settings.Spacing = 2; Reject(ranged.Validate);
    ranged.AutomatonInstance.Settings.Spacing = 0; Reject(ranged.Validate);
});

Test("Hunt the weakest ranks health percentage and ignores nearer healthier enemies", () => {
    World world = World.Create(false, 30, 20);
    Entity actor = Unit(world, Hex.FromOffset(5, 8), design: new TestUnit { Range = 1, TurnEnergy = 7 });
    AutomatonCatalog.Assign(actor.Unit, "Hunt the weakest");
    Entity near = Unit(world, actor.Position + new Hex(0, 1), Faction.Prytu, new TestUnit { Health = 30, Behavior = TestBehavior.Hold }); near.Health = 20;
    Entity weak = Unit(world, actor.Position + new Hex(5, 0), Faction.Prytu, new TestUnit { Health = 1000, Behavior = TestBehavior.Hold }); weak.Health = 500;
    world.Step();
    Check(actor.Unit.AutomatonInstance.TargetId == weak.Id, "50% beats 67% despite more absolute health");
    Check(World.Separation(actor, weak) <= actor.Unit.Range && near.Health == 20, "Approach only the weaker enemy");
    Check(actor.LastTurn.Submitted.OfType<AttackAction>().All(action => action.TargetId == weak.Id), "No opportunistic target");
    world.Step();
    Check(weak.Health < 500 && near.Health == 20, "Only chosen enemy takes attacks");
});

Test("Hunt the weakest uses stable ties and never falls back from an unreachable target", () => {
    World world = World.Create(false, 25, 20);
    Entity actor = Unit(world, Hex.FromOffset(5, 8), design: new TestUnit { Range = 1, TurnEnergy = 10 });
    AutomatonCatalog.Assign(actor.Unit, "Hunt the weakest");
    Entity weak = Unit(world, actor.Position + new Hex(6, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold }); weak.Health = 20;
    Entity stronger = Unit(world, actor.Position + new Hex(1, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold }); stronger.Health = 20;
    foreach (Hex cell in Hex.Directions.Select(direction => weak.Position + direction)) { world.Terrain[cell] = Terrain.Water; }
    world.Step();
    Check(actor.Unit.AutomatonInstance.TargetId == weak.Id && actor.ShotsFired == 0, "Lower ID wins equal percentages even when unreachable");
    stronger.Health = 40; world.Step();
    Check(actor.Unit.AutomatonInstance.TargetId == weak.Id && stronger.Health == 40 && actor.ShotsFired == 0, "No stronger fallback");
    weak.Position += new Hex(10, 0); world.RebuildOccupancy();
    world.Step();
    Check(actor.Unit.AutomatonInstance.TargetId == stronger.Id, "Re-evaluate currently visible enemies each turn");
});

Test("Heat and directed bonds still influence standard programs through observations", () => {
    World world = World.Create(false, 25, 20);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new SiegeWalker());
    actor.Heat = 100; actor.WeaponLocked = true;
    _ = Unit(world, actor.Position + new Hex(5, 0), Faction.Prytu, new TestUnit { Behavior = TestBehavior.Hold });
    world.Step();
    Check(actor.ShotsFired == 0 && actor.Heat < 100 && actor.Unit.AutomatonInstance.State.Intention == "Recover", "Paid vision reads physical weapon readiness");
    world.Settings.HeatEnabled = false; world.Step();
    Check(actor.ShotsFired == 1, "Heat mechanic can be disabled without bypassing vision");
    World escort = World.Create(false, 25, 20);
    Entity guard = Unit(escort, Hex.FromOffset(5, 8));
    Entity ward = Unit(escort, guard.Position + new Hex(5, 0), design: new TestUnit { Behavior = TestBehavior.Hold }); ward.Health = 10;
    guard.BondedUnitId = ward.Id;
    escort.Step();
    Check(guard.Unit.AutomatonInstance.State.Intention == "Escort" && World.Separation(guard, ward) <= 2, "Self bond allows observed ally protection");
});

Test("Program memory is bounded, independently copied and remembers resolved outcomes", () => {
    World world = World.Create(false, 25, 20);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new TestUnit { Range = 8, SightRange = 20 });
    AutomatonCatalog.Assign(actor.Unit, "Hunt the weakest");
    _ = Unit(world, actor.Position + new Hex(5, 0), Faction.Prytu, new TestUnit { Health = 1000, Behavior = TestBehavior.Hold });
    for (int turn = 0; turn < 15; turn++) { world.Step(); }
    UnitAutomaton automaton = actor.Unit.AutomatonInstance;
    Check(automaton.State.History.Count == 12 && automaton.State.ShotsFired == actor.ShotsFired - 1, "History bound and delayed outcome memory");
    Check(automaton.State.PreviousOutcomes.Any(outcome => outcome.Action is AttackAction && outcome.Succeeded), "Remembered accepted attacks");
    World copied = world.Copy();
    Check(SameWorld(world, copied), "Complete reports and concrete programs copied");
    copied.Entities[0].Unit.AutomatonInstance.State.PreviousOutcomes.Clear();
    Check(automaton.State.PreviousOutcomes.Count > 0, "Private memory lists detached");
    world.Remove(world.Entities[1]);
    for (int turn = 0; turn < 9; turn++) { world.Step(); }
    Check(automaton.State.Contacts.Count == 0, "Unseen memory expires");
});

Test("Special scenarios visibly distinguish each program from standard", () => {
    foreach (string name in specialPrograms) {
        World world = ScenarioCatalog.All.Single(scenario => scenario.Name == name).Create();
        Entity[] startingEntities = world.Entities.ToArray();
        Entity special = world.Entities.First(entity => entity.Unit.AutomatonInstance.ProgramName == name);
        Entity standard = world.Entities.First(entity => entity.Unit is LongbowArtillery && entity.Unit.AutomatonInstance.ProgramName == "Standard");
        world.Step();
        Check(special.LastTurn.Error.Length == 0 && standard.LastTurn.Error.Length == 0, "Programs run without host rejection");
        if (name == "Hunt the weakest") {
            Check(startingEntities.Single(entity => entity.Id == special.Unit.AutomatonInstance.TargetId).Unit is TrainingTarget, "Special picks farther lower percentage");
            Check(startingEntities.Single(entity => entity.Id == standard.Unit.AutomatonInstance.TargetId).Unit is CloneInfantry, "Standard picks nearer target");
        } else {
            Check(special.ShotsFired == 0 && standard.ShotsFired == 1, "Priority separation versus immediate combat");
        }
        World replay = world.Copy();
        for (int turn = 0; turn < 10; turn++) { world.Step(); replay.Step(); }
        Check(SameWorld(world, replay), "Program comparison replays deterministically");
    }
});

string summary = $"Passed {passed} checks; failed {failed}.";
Console.WriteLine(summary);
Environment.ExitCode = failed == 0 ? 0 : 1;
int outputIndex = Array.IndexOf(args, "--output");
if (outputIndex >= 0 && outputIndex + 1 < args.Length) {
    _ = Directory.CreateDirectory(args[outputIndex + 1]);
    File.WriteAllLines(Path.Combine(args[outputIndex + 1], "SimulationVerification.log"), report.Append(summary));
}

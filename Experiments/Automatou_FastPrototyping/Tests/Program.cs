using Automatou.Simulation;
using System.Text.Json.Nodes;

int passed = 0;
List<string> report = [];
void Check(bool condition, string message) {
    if (!condition) {
        throw new InvalidOperationException(message);
    }
}
void Test(string name, Action test) {
    test(); passed++; report.Add($"PASS {name}"); Console.WriteLine(report[^1]);
}
Entity Unit(World world, Hex position, Faction faction = Faction.Bastions, Unit? design = null, int facing = 0) {
    Entity unit = new() { Unit = design ?? new TestUnit(), Position = position, Faction = faction, Facing = facing };
    Check(world.Add(unit, out string reason), reason); return unit;
}
void Reject(Action action) {
    try { action(); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { return; }
    throw new InvalidOperationException("Invalid input was accepted.");
}

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
Test("Rectangle and hexagon authoring reject invalid map sizes", () => {
    Check(World.Create(false, 17, 9, false).Terrain.Count == 153, "Rectangle");
    Check(World.Create(true, 4, 1, false).Terrain.Count == 37, "Hexagon");
    Reject(() => World.Create(false, 0, 1, false)); Reject(() => World.Create(true, 200, 1, false));
});
Test("Footprints respect borders, occupied cells and movement domains", () => {
    World world = World.Create(false, 20, 20, false);
    Entity unit = Unit(world, Hex.FromOffset(6, 6), design: new TestUnit { Size = 3 });
    Check(world.At(Hex.FromOffset(6, 6) + new Hex(2, 0)) == unit, "Outer occupancy");
    Check(!world.CanOccupy(unit, Hex.FromOffset(0, 0), out _), "Border rejection");
    Check(!world.Paint(unit.Position, Terrain.Water), "Occupied terrain rejection");
    Check(!world.Add(new() { Unit = new TestUnit(), Position = unit.Position }, out _), "Overlap rejection");
    Check(World.Traversable(new TestUnit() { Mobility = Mobility.Flight }, Terrain.Mountain), "Flight");
    Check(!World.Traversable(new TestUnit() { Mobility = Mobility.Spaceflight }, Terrain.ExclusionZone), "Exclusion");
});
Test("Forward attacks, rear armor and action points affect combat", () => {
    World world = World.Create(false, 20, 10, false);
    Entity attacker = Unit(world, Hex.FromOffset(4, 4), design: new TestUnit() { ActionPoints = 1, Range = 1 });
    Entity defender = Unit(world, attacker.Position + new Hex(1, 0), Faction.Prytu, new TestUnit() { Armor = 20, Behavior = TestBehavior.Hold, ActionPoints = 1 });
    Check(world.CanAttack(attacker, defender), "Forward arc"); attacker.Facing = 3;
    Check(!world.CanAttack(attacker, defender), "Rear arc blocked");
    Check(World.ArmorAgainst(defender, defender.Position + new Hex(1, 0)) == 20, "Front armor");
    Check(World.ArmorAgainst(defender, defender.Position - new Hex(1, 0)) == 5, "Rear armor");
    world.Step(); Check(attacker.Health == 80 && defender.Health == 80, "No attack without two points");
});
Test("Blast damage includes allies and clears destroyed unit footprints", () => {
    World world = World.Create(false, 20, 20, false);
    Entity attacker = Unit(world, Hex.FromOffset(3, 8), design: new TestUnit() { Damage = 100, Range = 10, BlastRadius = 1 });
    Entity enemy = Unit(world, attacker.Position + new Hex(4, 0), Faction.Prytu, new TestUnit { Health = 20 });
    Entity ally = Unit(world, enemy.Position + new Hex(1, 0), design: new TestUnit() { Health = 20 });
    world.Attack(attacker, enemy);
    Check(!world.Entities.Contains(enemy) && !world.Entities.Contains(ally), "Friendly splash and destruction");
    Check(world.At(enemy.Position) is null && world.At(ally.Position) is null && world.Casualties == 2, "Destroyed footprints cleared");
});
Test("Units route around impassable terrain and engage", () => {
    World world = World.Create(false, 18, 14, false);
    // This fixture measures route finding, not contact acquisition around an obstacle.
    world.Settings.LimitedPerception = false;
    Entity attacker = Unit(world, Hex.FromOffset(3, 6), design: new TestUnit() { ActionPoints = 5, Range = 1, Damage = 10 });
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
    World world = World.Create(false, 20, 15, false);
    Entity attacker = Unit(world, Hex.FromOffset(4, 5), design: new TestUnit { Range = 1 });
    Entity defender = Unit(world, attacker.Position + new Hex(3, 0), Faction.Prytu, new TestUnit { Size = 3, Health = 500 });
    Check(World.Separation(attacker, defender) == 1 && world.CanAttack(attacker, defender), "Adjacent footprint is in range");
    world.Attack(attacker, defender);
    Check(defender.Health < 500, "Attack reaches occupied edge");
});
Test("Worlds reject entities without a unit", () => {
    World world = World.Demonstration();
    string json = Storage.Encode(world);
    JsonNode data = JsonNode.Parse(json)!;
    data["Entities"]![0]!["Unit"] = null;
    Reject(() => Storage.Decode(data.ToJsonString()));
});
Test("Skirmish, hold and deployed units obey their movement rules", () => {
    World world = World.Create(false, 30, 20, false);
    Entity skirmisher = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit() { Behavior = TestBehavior.Skirmish, Range = 4, ActionPoints = 6 });
    Entity holder = Unit(world, Hex.FromOffset(7, 5), Faction.Prytu, new TestUnit() { Behavior = TestBehavior.Hold, Range = 1, ActionPoints = 1 });
    Entity deployed = Unit(world, Hex.FromOffset(15, 15)); deployed.Stationary = true;
    Hex origin = holder.Position; Hex deployedOrigin = deployed.Position;
    world.Step();
    Check(skirmisher.Position.Distance(origin) > 2, "Skirmisher retreats after strike");
    Check(holder.Position == origin && deployed.Position == deployedOrigin, "Hold and deploy stay put");
});
Test("World saves preserve health, rotations, units and deterministic continuation", () => {
    World original = World.Demonstration();
    for (int i = 0; i < 8; i++) {
        original.Step();
    }

    World restored = Storage.Decode(Storage.Encode(original));
    Check(Storage.Encode(original) == Storage.Encode(restored), "Exact round trip");
    for (int i = 0; i < 12; i++) { original.Step(); restored.Step(); }
    Check(Storage.Encode(original) == Storage.Encode(restored), "Deterministic replay");
});
Test("Five-faction encounter remains consistent for 120 turns", () => {
    World world = World.Demonstration();
    Check(world.Entities.Select(e => e.Faction).Distinct().Count() == 5, "All factions present");
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
Test("Every source unit owns a distinct nested automaton and saves its memory", () => {
    foreach (Unit prototype in Catalog.Units()) {
        World world = World.Create(false, 30, 20, false);
        Entity actor = Unit(world, Hex.FromOffset(7, 10), design: prototype.CreateFresh());
        Entity other = Unit(world, Hex.FromOffset(20, 10), design: prototype.CreateFresh());
        Check(actor.Unit.Brain.GetType().DeclaringType == actor.Unit.GetType(), "Brain defined by its unit class");
        Check(!ReferenceEquals(actor.Unit.Brain, other.Unit.Brain), "Independent brains");
        Type memoryType = actor.Unit.Brain.GetType();
        memoryType.GetProperty("TurnsObserved")!.SetValue(actor.Unit.Brain, 41);
        memoryType.GetProperty("TargetId")!.SetValue(actor.Unit.Brain, 123);
        string json = Storage.Encode(world);
        Check(!json.Contains("Statistics") && !json.Contains("ActionPoints"), "Stats live in source, not saves");
        World restored = Storage.Decode(json);
        UnitAutomaton brain = restored.Entities[0].Unit.Brain;
        Check(brain.GetType() == memoryType && (int)memoryType.GetProperty("TurnsObserved")!.GetValue(brain)! == 41, "Concrete brain and memory restored");
        Check((int)memoryType.GetProperty("TargetId")!.GetValue(brain)! == 123, "Goal memory restored");
        restored.Step();
        Check((int)memoryType.GetProperty("TurnsObserved")!.GetValue(brain)! == 42, "One brain invocation each turn");
        Check((int)memoryType.GetProperty("TurnsObserved")!.GetValue(restored.Entities[1].Unit.Brain)! == 1, "Other memory independent");
    }
});
Test("Sensing is detached and refreshes after each accepted action", () => {
    World world = World.Create(false, 15, 15, false);
    WorldObservation? before = null, after = null;
    IEnumerable<UnitAction> Actions(UnitSenses senses) {
        before = senses.Observe();
        yield return new MoveForwardAction();
        after = senses.Observe();
        Check(senses.RemainingPoints == 3, "Budget reflects actuation");
    }
    Entity actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { Actions = Actions });
    Hex origin = actor.Position;
    world.Step();
    Check(before!.Self.Position == origin && after!.Self.Position == actor.Position && actor.Position != origin, "Snapshot stays unchanged; new observations reflect movement");
    Check(before.Entities is not EntityObservation[] && before.Self.Cells is not Hex[], "No mutable arrays exposed");
});
Test("Actuation rejects invalid requests and enforces per-turn limits", () => {
    foreach (UnitAction request in new UnitAction[] { new TurnAction(6), new AttackAction(9999) }) {
        World world = World.Create(false, 15, 15, false);
        Entity actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { Actions = _ => [request, new MoveForwardAction()] });
        Hex origin = actor.Position;
        world.Step();
        Check(actor.Position == origin && actor.Facing == 0, "Rejected request ends turn");
    }
    IEnumerable<UnitAction> Forever(UnitSenses _) { while (true) { yield return new TurnAction(1); } }
    World turning = World.Create(false, 15, 15, false);
    Entity spinner = Unit(turning, Hex.FromOffset(5, 5), design: new TestUnit { ActionPoints = 4, Actions = Forever });
    turning.Step(); Check(spinner.Facing == 4, "Infinite requests bounded by budget");
    spinner.Stationary = true; turning.Step(); Check(spinner.Facing == 4, "Deployed unit cannot turn");
    World combat = World.Create(false, 15, 15, false);
    Entity defender = Unit(combat, Hex.FromOffset(8, 5), Faction.Prytu, new TestUnit { Actions = _ => [] });
    Entity attacker = Unit(combat, defender.Position - new Hex(1, 0), design: new TestUnit { ActionPoints = 10, Actions = _ => [new AttackAction(defender.Id), new AttackAction(defender.Id)] });
    combat.Step(); Check(combat.Effects.Count == 1, "Only one attack per turn");
    World friendly = World.Create(false, 15, 15, false);
    Entity ally = Unit(friendly, Hex.FromOffset(8, 5), design: new TestUnit { Actions = _ => [] });
    _ = Unit(friendly, ally.Position - new Hex(1, 0), design: new TestUnit { Actions = _ => [new AttackAction(ally.Id)] });
    friendly.Step(); Check(friendly.Effects.Count == 0, "Direct friendly attacks rejected");
    World blocked = World.Create(false, 15, 15, false);
    Entity walker = Unit(blocked, Hex.FromOffset(5, 5), design: new TestUnit { Actions = _ => [new MoveForwardAction()] });
    Hex start = walker.Position;
    blocked.Terrain[start + Hex.Directions[0]] = Terrain.Water;
    blocked.Step(); Check(walker.Position == start, "Brain cannot bypass terrain");
});
Test("Sight radius and forest concealment restrict observations for every faction", () => {
    World world = World.Create(false, 30, 20, false);
    Entity observer = Unit(world, Hex.FromOffset(5, 8), design: new TestUnit { SightRange = 8, Actions = _ => [] });
    Entity concealed = Unit(world, observer.Position + new Hex(6, 0), Faction.Prytu, new TestUnit { Actions = _ => [] });
    Entity distant = Unit(world, observer.Position + new Hex(12, 0), Faction.Prytu, new TestUnit { Actions = _ => [] });
    Entity ally = Unit(world, observer.Position + new Hex(14, 0), design: new TestUnit { Actions = _ => [] });
    UnitSenses senses = new(world, observer);
    Check(senses.Observe().Entities.Any(e => e.Id == concealed.Id), "An exposed enemy inside the sight radius is visible");
    Check(senses.Observe().Entities.All(e => e.Id != distant.Id), "An enemy outside sight is hidden");
    world.Terrain[concealed.Position] = Terrain.Forest;
    WorldObservation observation = senses.Observe();
    Check(observation.Entities.All(e => e.Id != concealed.Id), "Forest conceals the enemy");
    Check(observation.Entities.All(e => e.Id != ally.Id), "Distant allies are not a global information channel");
    Check(observation.Entities.Any(e => e.Id == observer.Id), "The unit always observes itself");
    world.Terrain[concealed.Position] = Terrain.Plains;
    world.Terrain[observer.Position + new Hex(3, 0)] = Terrain.Forest;
    Check(senses.Observe().Entities.All(e => e.Id != concealed.Id), "An intervening forest cell blocks sight of an exposed target");
    concealed.Position = observer.Position + new Hex(2, 0);
    world.Terrain[concealed.Position] = Terrain.Forest;
    world.RebuildOccupancy();
    Check(senses.Observe().Entities.Any(e => e.Id == concealed.Id), "A nearby enemy in forest is detectable");
    senses.Settings.LimitedPerception = false;
    Check(world.Settings.LimitedPerception, "Sensing exposes a detached copy of world settings");
    world.Settings.LimitedPerception = false;
    Check(senses.Observe().Entities.Count == world.Entities.Count, "Global perception restores the comparison baseline");
});
Test("Hidden units cannot leak through target routes, planning occupancy, or attack requests", () => {
    World world = World.Create(false, 24, 18, false);
    Entity? target = null;
    Entity observer = Unit(world, Hex.FromOffset(5, 8), design: new TestUnit {
        SightRange = 10, Range = 10, Actions = _ => [new AttackAction(target!.Id)]
    });
    target = Unit(world, observer.Position + new Hex(6, 0), Faction.Prytu, new TestUnit { Actions = _ => [] });
    world.Terrain[target.Position] = Terrain.Forest;
    UnitSenses senses = new(world, observer);
    Check(senses.FindDirection(target.Id) == -1, "Target routes cannot look up an unseen enemy");
    Check(senses.CanOccupy(target.Position), "Planning occupancy cannot reveal an unseen footprint");
    Check(!world.CanOccupy(observer, target.Position, out _), "Physical collision still rejects the occupied cell");
    Hex destination = target.Position + new Hex(2, 0);
    int directionWithHiddenUnit = senses.FindDirection(destination);
    target.Position += new Hex(0, 3);
    world.Terrain[target.Position] = Terrain.Forest;
    world.RebuildOccupancy();
    Check(senses.FindDirection(destination) == directionWithHiddenUnit, "Destination routes do not change with an unseen unit's position");
    target.Position -= new Hex(0, 3);
    world.RebuildOccupancy();
    int health = target.Health;
    world.Step();
    Check(target.Health == health && world.Effects.Count == 0, "An otherwise legal attack by hidden identity is rejected");
    world.Settings.LimitedPerception = false;
    world.Step();
    Check(target.Health < health && world.Effects.Count == 1, "The same attack succeeds with global perception");
});
Test("Weapon heat is physical, locks at the upper threshold, and recovers at the lower threshold", () => {
    World world = World.Create(false, 20, 15, false);
    bool firing = true;
    Entity? target = null;
    Entity shooter = Unit(world, Hex.FromOffset(5, 6), design: new TestUnit {
        HeatPerShot = 30, CoolingPerTurn = 10, Range = 4,
        Actions = _ => firing ? [new AttackAction(target!.Id)] : []
    });
    target = Unit(world, shooter.Position + new Hex(3, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    shooter.Heat = 90;
    world.Step();
    Check(shooter.Heat == 110 && shooter.WeaponLocked && world.Effects.Count == 1, "One cooling update precedes the shot and the shot triggers lockout");
    int healthAfterShot = target.Health;
    world.Step();
    Check(shooter.Heat == 100 && target.Health == healthAfterShot && world.Effects.Count == 0, "Locked weapon cannot fire even when its automaton requests a shot");
    firing = false;
    for (int i = 0; i < 5; i++) {
        world.Step();
    }

    Check(shooter.Heat == 50 && shooter.WeaponLocked && !world.CanAttack(shooter, target), "Cooling below the lock threshold does not immediately unlock");
    world.Step();
    Check(shooter.Heat == 40 && !shooter.WeaponLocked && world.CanAttack(shooter, target), "Weapon becomes available at the recovery threshold");
    UnitSenses senses = new(world, shooter);
    for (int i = 0; i < 10; i++) {
        _ = senses.Observe();
    }

    Check(shooter.Heat == 40, "Repeated sensing does not update physical heat");
});
Test("Terrain cooling and disabling heat are consistent across sensing and execution", () => {
    foreach (Terrain terrain in new[] { Terrain.Forest, Terrain.Plains, Terrain.Mountain, Terrain.Water }) {
        World world = World.Create(false, 15, 15, false);
        Entity actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { Mobility = Mobility.Flight, CoolingPerTurn = 10, Actions = _ => [] });
        world.Terrain[actor.Position] = terrain;
        actor.Heat = 70;
        UnitSenses senses = new(world, actor);
        Check(senses.Observe().Self.Unit.CoolingPerTurn == 10, "The automaton estimates the same cooling as the world");
        world.Step();
        Check(actor.Heat == 60, "Every terrain uses source-defined cooling once per turn");
    }
    World baseline = World.Create(false, 15, 15, false);
    baseline.Settings.HeatEnabled = false;
    Entity? target = null;
    Entity shooter = Unit(baseline, Hex.FromOffset(5, 5), design: new TestUnit { HeatPerShot = 80, Actions = _ => [new AttackAction(target!.Id)] });
    target = Unit(baseline, shooter.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Actions = _ => [] });
    shooter.Heat = 120; shooter.WeaponLocked = true;
    baseline.Step();
    Check(baseline.Effects.Count == 1 && shooter.Heat == 120, "Disabled heat preserves physical state but bypasses the weapon restriction");
});
Test("Evaded shots still consume the weapon's physical heat budget", () => {
    World world = World.Create(false, 15, 15, false, seed: 1);
    Entity? target = null;
    Entity shooter = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { HeatPerShot = 45, Actions = _ => [new AttackAction(target!.Id)] });
    target = Unit(world, shooter.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Evasion = 90, Actions = _ => [] });
    world.Step();
    Check(world.Effects.Count == 1 && !world.Effects[0].Hit && target.Health == target.MaximumHealth, "The seeded shot was evaded");
    Check(shooter.Heat == 45, "A miss does not refund weapon heat");
});
Test("World saves preserve experiment switches, physical heat, and directed bonds", () => {
    World world = World.Create(false, 30, 20, false);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new SiegeWalker());
    Entity ally = Unit(world, Hex.FromOffset(15, 8), design: new Bastion());
    actor.Heat = 120; actor.WeaponLocked = true; actor.BondedUnitId = ally.Id;
    world.Settings.LimitedPerception = false; world.Settings.HeatEnabled = false; world.Settings.BondsEnabled = false;
    string saved = Storage.Encode(world);
    World restored = Storage.Decode(saved);
    Check(Storage.Encode(restored) == saved, "Experiment setup survives an exact round trip");
    Check(restored.Entities[0].Heat == 120 && restored.Entities[0].WeaponLocked && restored.Entities[0].BondedUnitId == ally.Id, "Physical state and directed attachment are restored");
    Check(!restored.Settings.LimitedPerception && !restored.Settings.HeatEnabled && !restored.Settings.BondsEnabled, "Mechanism comparisons use the saved switches");
    restored.Settings.HeatEnabled = true;
    Check(!world.Settings.HeatEnabled, "Restored world settings are independent");
});
Test("World loading rejects invalid experiment state before it reaches a simulation", () => {
    World world = World.Create(false, 20, 15, false);
    _ = Unit(world, Hex.FromOffset(6, 6), design: new SiegeWalker());
    string saved = Storage.Encode(world);
    foreach (int invalidHeat in new[] { -1, 201 }) {
        JsonNode data = JsonNode.Parse(saved)!;
        data["Entities"]![0]!["Heat"] = invalidHeat;
        Reject(() => Storage.Decode(data.ToJsonString()));
    }
    JsonNode invalidBond = JsonNode.Parse(saved)!;
    invalidBond["Entities"]![0]!["BondedUnitId"] = 0;
    Reject(() => Storage.Decode(invalidBond.ToJsonString()));
    JsonNode missingSettings = JsonNode.Parse(saved)!;
    missingSettings["Settings"] = null;
    Reject(() => Storage.Decode(missingSettings.ToJsonString()));
});
Test("Lost contacts are pursued at their last sighting and expire without remote tracking", () => {
    World world = World.Create(false, 35, 20, false);
    Entity actor = Unit(world, Hex.FromOffset(5, 8), design: new Bastion());
    Entity target = Unit(world, actor.Position + new Hex(6, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    world.Step();
    AutomatonMemory memory = actor.Unit.Brain.State;
    Hex lastSighting = target.Position;
    Check(memory.Contacts.Any(contact => contact.Id == target.Id && contact.Position == lastSighting && contact.LastSeenTurn == 1), "The visible target is recorded");
    target.Position += new Hex(15, 0);
    world.RebuildOccupancy();
    Check(!world.CanObserve(actor, target), "The enemy has left perception");
    world.Step();
    Check(memory.Intention == "Investigate" && memory.Destination == lastSighting && actor.Unit.Brain.TargetId == target.Id, "Pursuit uses the stale location and identity");
    Check(memory.Contacts.Single().Position == lastSighting && memory.Contacts.Single().LastSeenTurn == 1, "Hidden movement does not refresh the memory");
    actor.Stationary = true;
    while (world.Turn < 9) {
        world.Step();
    }

    Check(memory.Contacts.Count == 1, "A contact remains available for eight turns after the sighting");
    world.Step();
    Check(memory.Contacts.Count == 0 && actor.Unit.Brain.TargetId is null, "Stale contacts expire and cease supplying a target");
});
Test("Contact and decision memory stay bounded and remembering can be disabled", () => {
    World world = World.Create(false, 35, 20, false);
    world.Settings.LimitedPerception = false;
    Entity actor = Unit(world, Hex.FromOffset(5, 8), design: new Bastion());
    actor.Stationary = true;
    for (int i = 0; i < 12; i++) {
        _ = Unit(world, Hex.FromOffset(15 + i % 6 * 2, 4 + i / 6 * 8), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    }

    for (int i = 0; i < 16; i++) {
        world.Step();
    }

    UnitAutomaton brain = actor.Unit.Brain;
    Check(brain.State.Contacts.Count == 8 && brain.State.Contacts.Select(contact => contact.Id).Distinct().Count() == 8, "Contact memory is capped without duplicates");
    Check(brain.State.History.Count == 12 && brain.State.History[0].Turn == 5 && brain.State.History[^1].Turn == 16, "The decision trace retains the latest twelve turns");
    brain.Settings.RememberContacts = false;
    world.Step();
    Check(brain.State.Contacts.Count == 0 && brain.State.Intention == "Engage", "Disabling memory clears contacts while retaining current observations");
});
Test("Directed bonds change a healthy escort's choice under pressure", () => {
    (World World, Entity Actor, Entity Ward) Encounter(bool bonds) {
        World world = World.Create(false, 30, 20, false);
        world.Settings.BondsEnabled = bonds;
        Entity actor = Unit(world, Hex.FromOffset(7, 8), design: new TravelerOutrider(), facing: 3);
        Entity ward = Unit(world, actor.Position + new Hex(6, 0), design: new TestUnit { Health = 1000, Actions = _ => [] });
        ward.Health = 100;
        actor.BondedUnitId = ward.Id;
        _ = Unit(world, actor.Position - new Hex(2, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
        _ = Unit(world, ward.Position + new Hex(2, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
        return (world, actor, ward);
    }
    (World World, Entity Actor, Entity Ward) bonded = Encounter(true); (World World, Entity Actor, Entity Ward) independent = Encounter(false);
    int initialDistance = World.Separation(bonded.Actor, bonded.Ward);
    bonded.World.Step(); independent.World.Step();
    Check(bonded.Actor.Unit.Brain.State.Intention == "Escort" && bonded.Actor.Unit.Brain.TargetId == bonded.Ward.Id, "The explicit bond wins over the nearby attack");
    Check(World.Separation(bonded.Actor, bonded.Ward) < initialDistance, "Escort decisions produce movement toward the ward");
    Check(independent.Actor.Unit.Brain.State.Intention == "Engage" && independent.Actor.Unit.Brain.State.ShotsFired == 1, "Disabling bonds restores pressure against the nearby enemy");
});
Test("The same heat-limited machine develops distinct firing rhythms from its policy", () => {
    (World World, Entity Actor) Encounter(double aggression, int reserve) {
        World world = World.Create(false, 22, 16, false);
        Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new SiegeWalker());
        actor.Stationary = true;
        actor.Unit.Brain.Settings.Aggression = aggression;
        actor.Unit.Brain.Settings.HeatReserve = reserve;
        actor.Unit.Brain.Settings.Commitment = 0;
        _ = Unit(world, actor.Position + new Hex(4, 0), Faction.Prytu, new TrainingTarget());
        return (world, actor);
    }
    (World World, Entity Actor) bold = Encounter(1, 100); (World World, Entity Actor) measured = Encounter(.5, 70);
    for (int i = 0; i < 3; i++) { bold.World.Step(); measured.World.Step(); }
    Check(bold.Actor.Unit.Brain.State.ShotsFired == 3 && bold.Actor.WeaponLocked, "Aggressive policy takes three consecutive shots and accepts lockout");
    Check(measured.Actor.Unit.Brain.State.ShotsFired == 2 && !measured.Actor.WeaponLocked, "Measured policy spaces shots to preserve availability");
    Check(measured.Actor.Unit.Brain.State.History.Any(trace => trace.Intention == "Recover"), "The reason for the skipped shot is available in the decision trace");
});
Test("Commitment resists minor score changes but yields to emergency recovery", () => {
    World world = World.Create(false, 15, 15, false);
    Entity actor = Unit(world, Hex.FromOffset(5, 5));
    UnitAutomaton brain = actor.Unit.Brain;
    brain.Settings.Commitment = .2;
    WorldObservation observation = new UnitSenses(world, actor).Observe();
    _ = BehaviorPlanning.Choose(brain, observation with { Turn = 1 }, [new("Engage", .6, "Initial pressure", 2)]);
    _ = BehaviorPlanning.Choose(brain, observation with { Turn = 2 }, [new("Engage", .5, "Still possible", 2), new("Withdraw", .6, "Small advantage", 2)]);
    Check(brain.State.Intention == "Engage" && brain.State.IntentionSince == 1, "A small advantage does not immediately replace the commitment");
    _ = BehaviorPlanning.Choose(brain, observation with { Turn = 3 }, [new("Engage", .9, "Keep pressure", 2), new("Recover", 1, "Weapon unavailable")]);
    Check(brain.State.Intention == "Recover" && brain.State.IntentionSince == 3, "An emergency overrides commitment");
});
Test("A heat reserve smaller than one shot still permits firing from cold", () => {
    World world = World.Create(false, 22, 16, false);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new SiegeWalker());
    actor.Stationary = true;
    actor.Unit.Brain.Settings.Aggression = .5;
    actor.Unit.Brain.Settings.HeatReserve = 40;
    _ = Unit(world, actor.Position + new Hex(4, 0), Faction.Prytu, new TrainingTarget());
    world.Step();
    Check(actor.Unit.Brain.State.ShotsFired == 1 && actor.Heat == actor.Unit.HeatPerShot, "The minimum slider value does not permanently strand a cold weapon in recovery");
});
Test("Focused experiments demonstrate their advertised behavioral differences", () => {
    World heat = ScenarioCatalog.All.Single(scenario => scenario.Name == "Heat and readiness").Create();
    Entity[] walkers = heat.Entities.Where(entity => entity.Unit is SiegeWalker).OrderBy(entity => entity.Unit.Brain.Settings.Aggression).ToArray();
    List<int> measuredRhythm = []; List<int> aggressiveRhythm = [];
    bool aggressiveLocked = false;
    for (int i = 0; i < 12; i++) {
        heat.Step();
        measuredRhythm.Add(walkers[0].Unit.Brain.State.ShotsFired);
        aggressiveRhythm.Add(walkers[1].Unit.Brain.State.ShotsFired);
        Check(!walkers[0].WeaponLocked, "The measured operator preserves weapon availability");
        aggressiveLocked |= walkers[1].WeaponLocked;
    }
    Check(aggressiveLocked && !measuredRhythm.SequenceEqual(aggressiveRhythm), "The two heat policies show different firing rhythms and lockout behavior");
    World forest = ScenarioCatalog.All.Single(scenario => scenario.Name == "Lost in the forest").Create();
    Entity pursuer = forest.Entities.Single(entity => entity.Unit is CloneInfantry);
    bool investigated = false;
    for (int i = 0; i < 12; i++) {
        forest.Step();
        investigated |= pursuer.Unit.Brain.State.Intention == "Investigate";
    }
    Check(investigated, "The forest scenario actually produces investigation of a lost contact");
    ExperimentScenario bonds = ScenarioCatalog.All.Single(scenario => scenario.Name == "Bonds under pressure");
    World lowerWorld = bonds.Create(); World upperWorld = bonds.Create();
    Entity lowerGuard = lowerWorld.Entities.Single(entity => entity.Unit is Bastion);
    Entity upperGuard = upperWorld.Entities.Single(entity => entity.Unit is Bastion);
    upperGuard.BondedUnitId = upperWorld.Entities.First(entity => entity.Unit is TrainingTarget && entity.Id != upperGuard.BondedUnitId).Id;
    for (int i = 0; i < 3; i++) { lowerWorld.Step(); upperWorld.Step(); }
    Check(lowerGuard.Position != upperGuard.Position, "Changing only the bond sends the guardian along a different route");
    Check(lowerGuard.Unit.Brain.State.History.Any(trace => trace.Intention == "Escort") && upperGuard.Unit.Brain.State.History.Any(trace => trace.Intention == "Escort"), "Both routes are explained by escort decisions");
});
Test("Every focused experiment restores rich automaton state and continues exactly", () => {
    foreach (ExperimentScenario scenario in ScenarioCatalog.All) {
        World world = scenario.Create();
        for (int i = 0; i < 5; i++) {
            world.Step();
        }

        Check(world.Entities.Any(entity => entity.Unit.Brain.State.History.Count > 0 && entity.Unit.Brain.State.Considerations.Count > 0), "The saved state includes real decisions");
        string saved = Storage.Encode(world);
        World restored = Storage.Decode(saved);
        Check(Storage.Encode(restored) == saved, $"{scenario.Name}: full state round-trips exactly");
        foreach (Entity actor in world.Entities) {
            Entity copy = restored.Entities.Single(entity => entity.Id == actor.Id);
            Check(!ReferenceEquals(copy.Unit.Brain.Settings, actor.Unit.Brain.Settings) && !ReferenceEquals(copy.Unit.Brain.State, actor.Unit.Brain.State), "Restored preferences and runtime state are independent");
        }
        for (int i = 0; i < 12; i++) {
            world.Step(); restored.Step();
            Check(Storage.Encode(world) == Storage.Encode(restored), $"{scenario.Name}: exact continuation at turn {world.Turn}");
        }
    }
});
Test("Invalid automaton preferences and contact records are rejected on load", () => {
    string saved = Storage.Encode(ScenarioCatalog.All[0].Create());
    JsonNode invalidPreference = JsonNode.Parse(saved)!;
    invalidPreference["Entities"]![0]!["Unit"]!["Memory"]!["Settings"]!["Aggression"] = 1.1;
    Reject(() => Storage.Decode(invalidPreference.ToJsonString()));
    JsonNode missingState = JsonNode.Parse(saved)!;
    missingState["Entities"]![0]!["Unit"]!["Memory"]!["State"] = null;
    Reject(() => Storage.Decode(missingState.ToJsonString()));
    JsonNode invalidContact = JsonNode.Parse(saved)!;
    JsonArray contacts = invalidContact["Entities"]![0]!["Unit"]!["Memory"]!["State"]!["Contacts"]!.AsArray();
    contacts.Add(JsonNode.Parse("{\"Id\":2,\"Faction\":\"Prytu\",\"LastSeenTurn\":0,\"Health\":5,\"MaximumHealth\":5,\"SearchStep\":4}"));
    Reject(() => Storage.Decode(invalidContact.ToJsonString()));
});
Test("Finishing the final search probe abandons the exhausted contact", () => {
    World world = World.Create(false, 20, 16, false);
    Entity actor = Unit(world, Hex.FromOffset(6, 8), design: new Bastion());
    UnitAutomaton brain = actor.Unit.Brain;
    brain.TargetId = 999;
    brain.State.Intention = "Investigate";
    brain.State.Destination = actor.Position;
    brain.State.Contacts.Add(new ContactMemory {
        Id = 999, Faction = Faction.Prytu, Position = actor.Position, LastSeenTurn = 0,
        Health = 100, MaximumHealth = 100, SearchStep = 2
    });
    world.Step();
    Check(brain.State.Contacts.Count == 0 && brain.TargetId is null && brain.State.Intention != "Investigate", "Reaching the third search destination does not schedule a fourth probe");
});
Test("An escort turns toward and attacks a threat while already beside its ward", () => {
    World world = World.Create(false, 24, 18, false);
    Entity guard = Unit(world, Hex.FromOffset(7, 8), design: new Bastion(), facing: 2);
    Entity ward = Unit(world, guard.Position + new Hex(2, 0), design: new TestUnit { Health = 1000, Actions = _ => [] });
    ward.Health = 100; guard.BondedUnitId = ward.Id;
    Entity threat = Unit(world, guard.Position + new Hex(4, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    Check(!world.CanAttack(guard, threat) && World.Separation(guard, ward) <= 2, "The guard is supporting its ward but the threat starts outside its attack arc");
    world.Step();
    Check(guard.Unit.Brain.State.Intention == "Escort" && guard.Facing != 2, "The escort rotates to protect the ward");
    Check(threat.Health < 1000 && guard.Unit.Brain.State.ShotsFired == 1, "Turning and firing completes inside the action budget");
});
Test("Artillery distinguishes safe melee attacks from allied exposure to ranged blasts", () => {
    World world = World.Create(false, 24, 18, false);
    Entity actor = Unit(world, Hex.FromOffset(5, 8), design: new LongbowArtillery());
    Entity meleeTarget = Unit(world, actor.Position + new Hex(1, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    Entity meleeAlly = Unit(world, actor.Position + new Hex(1, -1), design: new TestUnit { Actions = _ => [] });
    Entity rangedTarget = Unit(world, actor.Position + new Hex(4, 0), Faction.Prytu, new TestUnit { Health = 1000, Actions = _ => [] });
    _ = Unit(world, actor.Position + new Hex(5, 0), design: new TestUnit { Actions = _ => [] });
    WorldObservation observation = new UnitSenses(world, actor).Observe();
    EntityObservation close = observation.Entities.Single(entity => entity.Id == meleeTarget.Id);
    EntityObservation far = observation.Entities.Single(entity => entity.Id == rangedTarget.Id);
    UnitAutomaton brain = actor.Unit.Brain;
    Check(BehaviorPlanning.Engage(brain, observation, close, considerBlast: true).Score == BehaviorPlanning.Engage(brain, observation, close).Score,
        "Nearby allies do not reduce the score of a nonsplash melee attack");
    Check(BehaviorPlanning.Engage(brain, observation, far, considerBlast: true).Score < BehaviorPlanning.Engage(brain, observation, far).Score,
        "The same policy still accounts for real ranged blast exposure");
    Check(BehaviorPlanning.Enemy(brain, observation, considerBlast: true)?.Id == meleeTarget.Id, "False splash exposure does not divert artillery from the close target");
    world.Step();
    Check(meleeTarget.Health < 1000 && meleeAlly.Health == meleeAlly.MaximumHealth, "The chosen melee strike damages the enemy without splashing its neighboring ally");
});
Console.WriteLine($"{passed} verification groups passed.");
int outputIndex = Array.IndexOf(args, "--output");
if (outputIndex >= 0 && outputIndex + 1 < args.Length) {
    File.WriteAllLines(Path.Combine(args[outputIndex + 1], "SimulationVerification.log"), report);
}

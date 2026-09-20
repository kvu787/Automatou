using Automatou.Simulation;

int passed = 0;
var report = new List<string>();
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
void Test(string name, Action test)
{
    test(); passed++; report.Add($"PASS {name}"); Console.WriteLine(report[^1]);
}
Entity Unit(World world, Hex position, Faction faction = Faction.Bastions, Automatou.Simulation.Unit? design = null, int facing = 0)
{
    var unit = new Entity { Unit = design ?? new TestUnit(), Position = position, Faction = faction, Facing = facing };
    Check(world.Add(unit, out string reason), reason); return unit;
}
void Reject(Action action)
{
    try { action(); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { return; }
    throw new Exception("Invalid input was accepted.");
}

Test("Odd rows offset east; public coordinates round-trip including negatives", () =>
{
    for (int y = -20; y <= 20; y++) for (int x = -20; x <= 20; x++) { var cell = Hex.FromOffset(x, y); Check(cell.X == x && cell.Y == y, "Offset conversion"); }
    Check(Hex.FromOffset(0, 0) + Hex.Directions[1] == Hex.FromOffset(0, 1), "Northeast");
    Check(Hex.FromOffset(0, 1) + Hex.Directions[2] == Hex.FromOffset(0, 2), "Northwest");
});
Test("Regular footprints and six rotations preserve distances", () =>
{
    int[] counts = [1, 7, 19, 37];
    for (int size = 1; size <= 4; size++) Check(Hex.Disk(size).Count() == counts[size - 1], "Footprint count");
    foreach (var cell in Hex.Disk(5)) { Check(cell.Rotate(6) == cell, "Full rotation"); Check(cell.Rotate(1).Distance(new()) == cell.Distance(new()), "Distance preserved"); }
});
Test("Rectangle and hexagon authoring reject invalid map sizes", () =>
{
    Check(World.Create(false, 17, 9, false).Terrain.Count == 153, "Rectangle");
    Check(World.Create(true, 4, 1, false).Terrain.Count == 37, "Hexagon");
    Reject(() => World.Create(false, 0, 1, false)); Reject(() => World.Create(true, 200, 1, false));
});
Test("Footprints respect borders, occupied cells and movement domains", () =>
{
    var world = World.Create(false, 20, 20, false);
    var unit = Unit(world, Hex.FromOffset(6, 6), design: new TestUnit { Size = 3 });
    Check(world.At(Hex.FromOffset(6, 6) + new Hex(2, 0)) == unit, "Outer occupancy");
    Check(!world.CanOccupy(unit, Hex.FromOffset(0, 0), 0, out _), "Border rejection");
    Check(!world.Paint(unit.Position, Terrain.Water), "Occupied terrain rejection");
    Check(!world.Add(new() { Unit = new TestUnit(), Position = unit.Position }, out _), "Overlap rejection");
    Check(World.Traversable(new TestUnit() { Mobility = Mobility.Flight }, Terrain.Mountain), "Flight");
    Check(!World.Traversable(new TestUnit() { Mobility = Mobility.Spaceflight }, Terrain.ExclusionZone), "Exclusion");
});
Test("Forward attacks, rear armor and action points affect combat", () =>
{
    var world = World.Create(false, 20, 10, false);
    var attacker = Unit(world, Hex.FromOffset(4, 4), design: new TestUnit() { ActionPoints = 1, Range = 1 });
    var defender = Unit(world, attacker.Position + new Hex(1, 0), Faction.Prytu, new TestUnit() { Armor = 20, Behavior = TestBehavior.Hold, ActionPoints = 1 });
    Check(world.CanAttack(attacker, defender), "Forward arc"); attacker.Facing = 3;
    Check(!world.CanAttack(attacker, defender), "Rear arc blocked");
    Check(world.ArmorAgainst(defender, defender.Position + new Hex(1, 0)) == 20, "Front armor");
    Check(world.ArmorAgainst(defender, defender.Position - new Hex(1, 0)) == 5, "Rear armor");
    world.Step(); Check(attacker.Health == 80 && defender.Health == 80, "No attack without two points");
});
Test("Blast damage includes allies and clears destroyed unit footprints", () =>
{
    var world = World.Create(false, 20, 20, false);
    var attacker = Unit(world, Hex.FromOffset(3, 8), design: new TestUnit() { Damage = 100, Range = 10, BlastRadius = 1 });
    var enemy = Unit(world, attacker.Position + new Hex(4, 0), Faction.Prytu, new TestUnit { Health = 20 });
    var ally = Unit(world, enemy.Position + new Hex(1, 0), design: new TestUnit() { Health = 20 });
    world.Attack(attacker, enemy);
    Check(!world.Entities.Contains(enemy) && !world.Entities.Contains(ally), "Friendly splash and destruction");
    Check(world.At(enemy.Position) is null && world.At(ally.Position) is null && world.Casualties == 2, "Destroyed footprints cleared");
});
Test("Automata route around impassable terrain and engage", () =>
{
    var world = World.Create(false, 18, 14, false);
    var attacker = Unit(world, Hex.FromOffset(3, 6), design: new TestUnit() { ActionPoints = 5, Range = 1, Damage = 10 });
    var defender = Unit(world, Hex.FromOffset(12, 6), Faction.Prytu, new TestUnit() { Behavior = TestBehavior.Hold, Health = 1000, Damage = 1 });
    for (int y = 3; y < 10; y++) world.Terrain[Hex.FromOffset(8, y)] = Terrain.Water;
    for (int i = 0; i < 25; i++) world.Step();
    Check(defender.Health < 1000, "Reached enemy past wall");
    Check(attacker.OccupiedCells().All(c => world.Terrain[c] != Terrain.Water), "Never entered water");
});
Test("Combat measures range to a large unit's occupied edge", () =>
{
    var world = World.Create(false, 20, 15, false);
    var attacker = Unit(world, Hex.FromOffset(4, 5), design: new TestUnit { Range = 1 });
    var defender = Unit(world, attacker.Position + new Hex(3, 0), Faction.Prytu, new TestUnit { Size = 3, Health = 500 });
    Check(world.Separation(attacker, defender) == 1 && world.CanAttack(attacker, defender), "Adjacent footprint is in range");
    world.Attack(attacker, defender);
    Check(defender.Health < 500, "Attack reaches occupied edge");
});
Test("Worlds reject entities without a unit", () =>
{
    var world = World.Demonstration();
    string json = Storage.Encode(world);
    var data = System.Text.Json.Nodes.JsonNode.Parse(json)!;
    data["Entities"]![0]!["Unit"] = null;
    Reject(() => Storage.Decode(data.ToJsonString()));
});
Test("Skirmish, hold and deployed automata obey their movement rules", () =>
{
    var world = World.Create(false, 30, 20, false);
    var skirmisher = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit() { Behavior = TestBehavior.Skirmish, Range = 4, ActionPoints = 6 });
    var holder = Unit(world, Hex.FromOffset(7, 5), Faction.Prytu, new TestUnit() { Behavior = TestBehavior.Hold, Range = 1, ActionPoints = 1 });
    var deployed = Unit(world, Hex.FromOffset(15, 15)); deployed.Stationary = true;
    var origin = holder.Position; var deployedOrigin = deployed.Position;
    world.Step();
    Check(skirmisher.Position.Distance(origin) > 2, "Skirmisher retreats after strike");
    Check(holder.Position == origin && deployed.Position == deployedOrigin, "Hold and deploy stay put");
});
Test("World saves preserve health, rotations, units and deterministic continuation", () =>
{
    var original = World.Demonstration();
    for (int i = 0; i < 8; i++) original.Step();
    var restored = Storage.Decode(Storage.Encode(original));
    Check(Storage.Encode(original) == Storage.Encode(restored), "Exact round trip");
    for (int i = 0; i < 12; i++) { original.Step(); restored.Step(); }
    Check(Storage.Encode(original) == Storage.Encode(restored), "Deterministic replay");
});
Test("Five-faction encounter remains consistent for 120 turns", () =>
{
    var world = World.Demonstration();
    Check(world.Entities.Select(e => e.Faction).Distinct().Count() == 5, "All factions present");
    for (int i = 0; i < 120; i++)
    {
        world.Step();
        var cells = world.Entities.SelectMany(e => e.OccupiedCells()).ToArray();
        Check(cells.Distinct().Count() == cells.Length, "No overlapping footprints");
        Check(cells.All(world.Terrain.ContainsKey), "No out-of-bounds entity");
        Check(world.Entities.All(e => e.Health > 0), "No destroyed entity retained");
    }
    Check(world.Casualties > 5, "Encounter produces combat");
    report.Add($"Encounter: {world.Turn} turns, {world.Entities.Count} survivors, {world.Casualties} casualties.");
});
Test("Every source unit owns a distinct nested automaton and saves its memory", () =>
{
    foreach (var prototype in Catalog.Units())
    {
        var world = World.Create(false, 30, 20, false);
        var actor = Unit(world, Hex.FromOffset(7, 10), design: prototype.CreateFresh());
        var other = Unit(world, Hex.FromOffset(20, 10), design: prototype.CreateFresh());
        Check(actor.Unit.Brain.GetType().DeclaringType == actor.Unit.GetType(), "Brain defined by its unit class");
        Check(!ReferenceEquals(actor.Unit.Brain, other.Unit.Brain), "Independent brains");
        var memoryType = actor.Unit.Brain.GetType();
        memoryType.GetProperty("TurnsObserved")!.SetValue(actor.Unit.Brain, 41);
        memoryType.GetProperty("TargetId")!.SetValue(actor.Unit.Brain, 123);
        string json = Storage.Encode(world);
        Check(!json.Contains("Statistics") && !json.Contains("ActionPoints"), "Stats live in source, not saves");
        var restored = Storage.Decode(json);
        var brain = restored.Entities[0].Unit.Brain;
        Check(brain.GetType() == memoryType && (int)memoryType.GetProperty("TurnsObserved")!.GetValue(brain)! == 41, "Concrete brain and memory restored");
        Check((int)memoryType.GetProperty("TargetId")!.GetValue(brain)! == 123, "Goal memory restored");
        restored.Step();
        Check((int)memoryType.GetProperty("TurnsObserved")!.GetValue(brain)! == 42, "One brain invocation each turn");
        Check((int)memoryType.GetProperty("TurnsObserved")!.GetValue(restored.Entities[1].Unit.Brain)! == 1, "Other memory independent");
    }
});
Test("Sensing is detached and refreshes after each accepted action", () =>
{
    var world = World.Create(false, 15, 15, false);
    WorldObservation? before = null, after = null;
    IEnumerable<UnitAction> Actions(UnitSenses senses)
    {
        before = senses.Observe();
        yield return new MoveForwardAction();
        after = senses.Observe();
        Check(senses.RemainingPoints == 3, "Budget reflects actuation");
    }
    var actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { Actions = Actions });
    Hex origin = actor.Position;
    world.Step();
    Check(before!.Self.Position == origin && after!.Self.Position == actor.Position && actor.Position != origin, "Snapshot stays unchanged; new observations reflect movement");
    Check(before.Entities is not EntityObservation[] && before.Self.Cells is not Hex[], "No mutable arrays exposed");
});
Test("Actuation rejects invalid requests and enforces per-turn limits", () =>
{
    foreach (UnitAction request in new UnitAction[] { new TurnAction(6), new AttackAction(9999) })
    {
        var world = World.Create(false, 15, 15, false);
        var actor = Unit(world, Hex.FromOffset(5, 5), design: new TestUnit { Actions = _ => [request, new MoveForwardAction()] });
        Hex origin = actor.Position;
        world.Step();
        Check(actor.Position == origin && actor.Facing == 0, "Rejected request ends turn");
    }
    IEnumerable<UnitAction> Forever(UnitSenses _) { while (true) yield return new TurnAction(1); }
    var turning = World.Create(false, 15, 15, false);
    var spinner = Unit(turning, Hex.FromOffset(5, 5), design: new TestUnit { ActionPoints = 4, Actions = Forever });
    turning.Step(); Check(spinner.Facing == 4, "Infinite requests bounded by budget");
    spinner.Stationary = true; turning.Step(); Check(spinner.Facing == 4, "Deployed unit cannot turn");
    var combat = World.Create(false, 15, 15, false);
    var defender = Unit(combat, Hex.FromOffset(8, 5), Faction.Prytu, new TestUnit { Actions = _ => [] });
    var attacker = Unit(combat, defender.Position - new Hex(1, 0), design: new TestUnit { ActionPoints = 10, Actions = _ => [new AttackAction(defender.Id), new AttackAction(defender.Id)] });
    combat.Step(); Check(combat.Effects.Count == 1, "Only one attack per turn");
    var friendly = World.Create(false, 15, 15, false);
    var ally = Unit(friendly, Hex.FromOffset(8, 5), design: new TestUnit { Actions = _ => [] });
    Unit(friendly, ally.Position - new Hex(1, 0), design: new TestUnit { Actions = _ => [new AttackAction(ally.Id)] });
    friendly.Step(); Check(friendly.Effects.Count == 0, "Direct friendly attacks rejected");
    var blocked = World.Create(false, 15, 15, false);
    var walker = Unit(blocked, Hex.FromOffset(5, 5), design: new TestUnit { Actions = _ => [new MoveForwardAction()] });
    Hex start = walker.Position;
    blocked.Terrain[start + Hex.Directions[0]] = Terrain.Water;
    blocked.Step(); Check(walker.Position == start, "Brain cannot bypass terrain");
});
Console.WriteLine($"{passed} verification groups passed.");
int outputIndex = Array.IndexOf(args, "--output");
if (outputIndex >= 0 && outputIndex + 1 < args.Length) File.WriteAllLines(Path.Combine(args[outputIndex + 1], "SimulationVerification.log"), report);

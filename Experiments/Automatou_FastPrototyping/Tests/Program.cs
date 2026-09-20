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
Entity Unit(World world, Hex position, Faction faction = Faction.Bastions, UnitDesign? design = null, int facing = 0)
{
    var unit = new Entity { Unit = design ?? new UnitDesign(), Position = position, Faction = faction, Facing = facing };
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
    var unit = Unit(world, Hex.FromOffset(6, 6), design: new UnitDesign { Size = 3 });
    Check(world.At(Hex.FromOffset(6, 6) + new Hex(2, 0)) == unit, "Outer occupancy");
    Check(!world.CanOccupy(unit, Hex.FromOffset(0, 0), 0, out _), "Border rejection");
    Check(!world.Paint(unit.Position, Terrain.Water), "Occupied terrain rejection");
    Check(!world.Add(new() { Unit = new(), Position = unit.Position }, out _), "Overlap rejection");
    Check(World.Traversable(new() { Mobility = Mobility.Flight }, Terrain.Mountain), "Flight");
    Check(!World.Traversable(new() { Mobility = Mobility.Spaceflight }, Terrain.ExclusionZone), "Exclusion");
});
Test("Authored buildings rotate around saved pivot and reveal underlying terrain", () =>
{
    var world = World.Create(false, 20, 20, false);
    var design = new BuildingDesign { Cells = [new(1, 0), new(2, 0)], EditorOrigin = Hex.FromOffset(13, 7) };
    design.Validate();
    var building = new Entity { Building = design, Position = Hex.FromOffset(8, 8), Facing = 1 };
    var cell = building.Position + new Hex(0, 1);
    world.Terrain[cell] = Terrain.Water;
    Check(world.Add(building, out _), "Building on water"); Check(world.At(cell) == building, "Rotated occupancy");
    world.Remove(building); Check(world.At(cell) is null && world.Terrain[cell] == Terrain.Water, "Terrain preserved");
    Reject(() => new BuildingDesign { Cells = [new(), new(5, 0)] }.Validate());
});
Test("Forward attacks, rear armor and action points affect combat", () =>
{
    var world = World.Create(false, 20, 10, false);
    var attacker = Unit(world, Hex.FromOffset(4, 4), design: new() { ActionPoints = 1, Range = 1 });
    var defender = Unit(world, attacker.Position + new Hex(1, 0), Faction.Prytu, new() { Armor = 20, Automaton = Automaton.Hold, ActionPoints = 1 });
    Check(world.CanAttack(attacker, defender), "Forward arc"); attacker.Facing = 3;
    Check(!world.CanAttack(attacker, defender), "Rear arc blocked");
    Check(world.ArmorAgainst(defender, defender.Position + new Hex(1, 0)) == 20, "Front armor");
    Check(world.ArmorAgainst(defender, defender.Position - new Hex(1, 0)) == 5, "Rear armor");
    world.Step(); Check(attacker.Health == 80 && defender.Health == 80, "No attack without two points");
});
Test("Blast damage includes allies and removes a building's whole health pool", () =>
{
    var world = World.Create(false, 20, 20, false);
    var attacker = Unit(world, Hex.FromOffset(3, 8), design: new() { Damage = 100, Range = 10, BlastRadius = 1 });
    var enemy = new Entity { Building = new() { Health = 20 }, Faction = Faction.Prytu, Position = attacker.Position + new Hex(4, 0) };
    Check(world.Add(enemy, out _), "Building added");
    var ally = Unit(world, enemy.Position + new Hex(1, 0), design: new() { Health = 20 });
    world.Attack(attacker, enemy);
    Check(!world.Entities.Contains(enemy) && !world.Entities.Contains(ally), "Friendly splash and destruction");
});
Test("Automata route around impassable terrain and engage", () =>
{
    var world = World.Create(false, 18, 14, false);
    var attacker = Unit(world, Hex.FromOffset(3, 6), design: new() { ActionPoints = 5, Range = 1, Damage = 10 });
    var defender = Unit(world, Hex.FromOffset(12, 6), Faction.Prytu, new() { Automaton = Automaton.Hold, Health = 1000, Damage = 1 });
    for (int y = 3; y < 10; y++) world.Terrain[Hex.FromOffset(8, y)] = Terrain.Water;
    for (int i = 0; i < 25; i++) world.Step();
    Check(defender.Health < 1000, "Reached enemy past wall");
    Check(attacker.OccupiedCells().All(c => world.Terrain[c] != Terrain.Water), "Never entered water");
});
Test("Combat targets building cells even when the pivot is outside the footprint", () =>
{
    var world = World.Create(false, 25, 15, false);
    var attacker = Unit(world, Hex.FromOffset(4, 5), design: new() { Range = 1, ActionPoints = 5 });
    var building = new Entity { Building = new() { Cells = [new(-10, 0), new(-11, 0)], Health = 500 }, Position = Hex.FromOffset(20, 5), Faction = Faction.Prytu };
    Check(world.Add(building, out _), "Remote-pivot building added");
    for (int i = 0; i < 12; i++) world.Step();
    Check(building.Health < 500, "Attack occupied cells instead of empty origin");
});
Test("Skirmish, hold and deployed automata obey their movement rules", () =>
{
    var world = World.Create(false, 30, 20, false);
    var skirmisher = Unit(world, Hex.FromOffset(5, 5), design: new() { Automaton = Automaton.Skirmish, Range = 4, ActionPoints = 6 });
    var holder = Unit(world, Hex.FromOffset(7, 5), Faction.Prytu, new() { Automaton = Automaton.Hold, Range = 1, ActionPoints = 1 });
    var deployed = Unit(world, Hex.FromOffset(15, 15)); deployed.Stationary = true;
    var origin = holder.Position; var deployedOrigin = deployed.Position;
    world.Step();
    Check(skirmisher.Position.Distance(origin) > 2, "Skirmisher retreats after strike");
    Check(holder.Position == origin && deployed.Position == deployedOrigin, "Hold and deploy stay put");
});
Test("Blueprint files preserve off-center pivots and custom unit parameters", () =>
{
    string folder = Path.Combine(Path.GetTempPath(), "AutomatouVerification", Guid.NewGuid().ToString("N"));
    var design = new BuildingDesign { Cells = [new(0, 0), new(1, 0)], EditorOrigin = Hex.FromOffset(-20, 32), PatchWidth = 14, PatchHeight = 8 };
    string path = Path.Combine(folder, "Building.json");
    Storage.SaveDesign(path, design);
    var restored = Storage.LoadDesign<BuildingDesign>(path); restored.Validate();
    Check(restored.EditorOrigin == design.EditorOrigin && restored.Cells.SequenceEqual(design.Cells) && restored.PatchWidth == 14, "Building round trip");
    var custom = new UnitDesign { Size = 4, MeleeDamage = 87, Mobility = Mobility.Spaceflight, Automaton = Automaton.Skirmish };
    path = Path.Combine(folder, "Unit.json"); Storage.SaveDesign(path, custom);
    var unit = Storage.LoadDesign<UnitDesign>(path); unit.Validate();
    Check(unit.Size == 4 && unit.MeleeDamage == 87 && unit.Mobility == Mobility.Spaceflight, "Unit round trip");
    File.Delete(path); File.Delete(Path.Combine(folder, "Building.json")); Directory.Delete(folder);
});
Test("World saves preserve health, rotations, blueprints and deterministic continuation", () =>
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
Console.WriteLine($"{passed} verification groups passed.");
int outputIndex = Array.IndexOf(args, "--output");
if (outputIndex >= 0 && outputIndex + 1 < args.Length) File.WriteAllLines(Path.Combine(args[outputIndex + 1], "SimulationVerification.log"), report);

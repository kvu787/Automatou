namespace Automatou.Simulation;

public enum Terrain { Water, Air, Space, Forest, Plains, Mountain, Wetlands, Paved, Desert, Tundra, ExclusionZone }
public enum Faction { Bastions, Travelers, MechAndTank, InfantryAndArtillery, Prytu }
public enum Automaton { Advance, Skirmish, Hold, Artillery, Swarm }
public enum Mobility { Ground, Amphibious, Flight, Spaceflight }

public sealed class UnitDesign
{
    public string Name { get; set; } = "New unit";
    public int Size { get; set; } = 1;
    public int Health { get; set; } = 80;
    public int Armor { get; set; } = 3;
    public int Damage { get; set; } = 18;
    public int MeleeDamage { get; set; } = 18;
    public int Range { get; set; } = 3;
    public int ActionPoints { get; set; } = 4;
    public int Evasion { get; set; }
    public int BlastRadius { get; set; }
    public Automaton Automaton { get; set; } = Automaton.Advance;
    public Mobility Mobility { get; set; } = Mobility.Ground;
    public UnitDesign Copy() => (UnitDesign)MemberwiseClone();
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 60 || Size is < 1 or > 12 || Health is < 1 or > 10000 ||
            Armor is < 0 or > 1000 || Damage is < 1 or > 1000 || MeleeDamage is < 1 or > 1000 || Range is < 1 or > 30 || ActionPoints is < 1 or > 20 ||
            Evasion is < 0 or > 90 || BlastRadius is < 0 or > 3 || !Enum.IsDefined(Automaton) || !Enum.IsDefined(Mobility))
            throw new InvalidDataException("Unit values are outside the supported creator ranges.");
    }
}

public sealed class BuildingDesign
{
    public string Name { get; set; } = "New building";
    public int Health { get; set; } = 400;
    public List<Hex> Cells { get; set; } = [new(0, 0)];
    public Hex EditorOrigin { get; set; } = Hex.FromOffset(10, 5);
    public int PatchWidth { get; set; } = 20;
    public int PatchHeight { get; set; } = 10;
    public BuildingDesign Copy() => new() { Name = Name, Health = Health, Cells = [.. Cells], EditorOrigin = EditorOrigin, PatchWidth = PatchWidth, PatchHeight = PatchHeight };
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 60 || Health is < 1 or > 10000 || Cells.Count is < 1 or > 2000 || Cells.Distinct().Count() != Cells.Count || PatchWidth is < 2 or > 100 || PatchHeight is < 2 or > 100)
            throw new InvalidDataException("Building needs a name, health, and 1–2,000 distinct connected cells.");
        var remaining = Cells.ToHashSet();
        var frontier = new Queue<Hex>();
        frontier.Enqueue(Cells[0]); remaining.Remove(Cells[0]);
        while (frontier.TryDequeue(out var cell))
            foreach (var direction in Hex.Directions)
                if (remaining.Remove(cell + direction)) frontier.Enqueue(cell + direction);
        if (remaining.Count != 0) throw new InvalidDataException("A building must form one connected island of cells.");
    }
}

public sealed class Entity
{
    public int Id { get; set; }
    public Faction Faction { get; set; }
    public Hex Position { get; set; }
    public int Facing { get; set; }
    public int Health { get; set; }
    public UnitDesign? Unit { get; set; }
    public BuildingDesign? Building { get; set; }
    public bool Stationary { get; set; }
    public string Name => Unit?.Name ?? Building?.Name ?? "Unknown";
    public int MaximumHealth => Unit?.Health ?? Building?.Health ?? 1;
    public IEnumerable<Hex> OccupiedCells() => Unit is not null
        ? Hex.Disk(Unit.Size).Select(cell => Position + cell)
        : Building!.Cells.Select(cell => Position + cell.Rotate(Facing));
}

public static class Catalog
{
    public static readonly string[] FactionNames = ["Bastions", "Travelers", "Mech & tank", "Infantry & artillery", "Prytu"];
    public static readonly string[] FactionColors = ["f3c66b", "66d9df", "84b4fb", "f39379", "bd95e9"];
    public static readonly string[] TerrainNames = ["Water", "Air", "Space", "Forest", "Plains", "Mountain", "Wetlands", "Paved", "Desert", "Tundra", "Exclusion zone"];
    public static readonly string[] TerrainColors = ["193e55", "354754", "191f36", "244a3c", "354638", "4a4b50", "345454", "555760", "67563c", "526468", "442c3d"];
    public static List<UnitDesign> Units() =>
    [
        new() { Name = "Bastion", Size = 2, Health = 500, Armor = 22, Damage = 52, MeleeDamage = 75, Range = 4, ActionPoints = 3 },
        new() { Name = "Traveler outrider", Health = 65, Armor = 2, Damage = 22, Range = 2, ActionPoints = 7, Evasion = 30, Automaton = Automaton.Skirmish },
        new() { Name = "H.O.M.E.", Size = 3, Health = 380, Armor = 8, Damage = 15, Range = 2, ActionPoints = 5, Evasion = 15, Automaton = Automaton.Skirmish, Mobility = Mobility.Amphibious },
        new() { Name = "Siege walker", Size = 2, Health = 210, Armor = 12, Damage = 35, Range = 3, ActionPoints = 4 },
        new() { Name = "Clone infantry", Health = 38, Armor = 1, Damage = 12, Range = 2, ActionPoints = 5, Automaton = Automaton.Swarm },
        new() { Name = "Longbow artillery", Health = 90, Armor = 3, Damage = 60, Range = 9, ActionPoints = 3, BlastRadius = 1, Automaton = Automaton.Artillery },
        new() { Name = "Prytu hunter", Health = 75, Armor = 3, Damage = 28, MeleeDamage = 28, Range = 1, ActionPoints = 6, Automaton = Automaton.Swarm },
        new() { Name = "Prytu manifestation", Size = 3, Health = 600, Armor = 10, Damage = 65, MeleeDamage = 65, Range = 2, ActionPoints = 4, Automaton = Automaton.Swarm }
    ];
    // Explicit authored footprint, never procedurally generated.
    public static BuildingDesign Outpost() => new() { Name = "Watch station", Cells = [new(0, 0), new(1, 0), new(0, 1), new(-1, 1), new(-1, 0)], Health = 350 };
}

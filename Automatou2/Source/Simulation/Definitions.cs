namespace Automatou.Simulation;

public sealed class Entity {
    public Entity Copy() {
        return new() {
            Id = this.Id, Faction = this.Faction, Position = this.Position, Facing = this.Facing,
            Health = this.Health, Unit = this.Unit.Copy(), Stationary = this.Stationary,
            Heat = this.Heat, WeaponLocked = this.WeaponLocked, BondedUnitId = this.BondedUnitId,
            ShotsFired = this.ShotsFired, LastTurn = this.LastTurn.Copy()
        };
    }

    public int Id { get; set; }
    public Faction Faction { get; set; }
    public Hex Position { get; set; }
    public int Facing { get; set; }
    public int Health { get; set; }
    public required Unit Unit { get; set; }
    public bool Stationary { get; set; }
    public int Heat { get; set; }
    public bool WeaponLocked { get; set; }
    public int? BondedUnitId { get; set; }
    public int ShotsFired { get; set; }
    public TurnReport LastTurn { get; set; } = new();
    public string Name => this.Unit.Name;
    public int MaximumHealth => this.Unit.Health;
    public IEnumerable<Hex> OccupiedCells() {
        return Hex.Disk(this.Unit.Size).Select(cell => this.Position + cell);
    }
}

public sealed class TurnReport {
    public int Turn { get; set; }
    public int InitialEnergy { get; set; }
    public int RemainingEnergy { get; set; }
    public string Error { get; set; } = "";
    public List<SensingReceipt> Sensing { get; set; } = [];
    public List<UnitAction> Submitted { get; set; } = [];
    public List<ActionOutcome> Outcomes { get; set; } = [];
    public TurnReport Copy() {
        return new() {
            Turn = this.Turn, InitialEnergy = this.InitialEnergy, RemainingEnergy = this.RemainingEnergy,
            Error = this.Error, Sensing = [.. this.Sensing], Submitted = [.. this.Submitted], Outcomes = [.. this.Outcomes]
        };
    }
}

public static class Catalog {
    public static bool CanPlace(Faction faction, Unit unit) {
        return faction switch {
            Faction.Bastions => unit is Bastion,
            Faction.Travelers => unit is TravelerOutrider or Home,
            Faction.MechAndTank => unit is SiegeWalker,
            Faction.InfantryAndArtillery => unit is CloneInfantry or LongbowArtillery,
            Faction.Prytu => unit is PrytuHunter or PrytuManifestation,
            _ => false
        };
    }

    public static readonly string[] FactionNames = ["Bastions", "Travelers", "Mech & tank", "Infantry & artillery", "Prytu"];
    public static readonly string[] FactionColors = ["f3c66b", "66d9df", "84b4fb", "f39379", "bd95e9"];
    public static readonly string[] TerrainNames = ["Forest", "Plains", "Mountains", "Water", "Exclusion zone"];
    public static readonly string[] TerrainColors = ["28543c", "807345", "616773", "245c86", "713b61"];
    public static List<Unit> Units() {
        return [new Bastion(), new TravelerOutrider(), new Home(), new SiegeWalker(), new CloneInfantry(), new LongbowArtillery(), new PrytuHunter(), new PrytuManifestation(), new TrainingTarget()];
    }
}

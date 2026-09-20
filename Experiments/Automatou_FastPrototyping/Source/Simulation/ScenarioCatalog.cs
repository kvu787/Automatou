namespace Automatou.Simulation;

public sealed record ExperimentScenario(string Name, string Description, Func<World> Create);

// Authored, deterministic conditions make a change in policy easy to see and replay.
public static class ScenarioCatalog {
    public static IReadOnlyList<ExperimentScenario> All { get; } = Array.AsReadOnly(new ExperimentScenario[]
    {
        new("Heat and readiness", "Two identical walkers fire at inert targets. The upper walker spaces shots; the lower accepts lockouts. Change aggression or the heat ceiling, then rewind with tuning. Disable machine heat for a control run.", HeatAndReadiness),
        new("Lost in the forest", "A pursuer has a starting sighting of an injured, fast Traveler. Forest breaks contact. Inspect the pursuer's remembered cells and search decisions; compare with memory or limited perception disabled.", LostInTheForest),
        new("Bonds under pressure", "One Bastion can support two vulnerable allies threatened by infantry. Its initial bond favors the lower ally. Change the bond to the upper ally or disable bonds, then rewind with tuning and compare its route.", BondsUnderPressure)
    });

    private static Entity Place(World world, Unit unit, Faction faction, int x, int y, int facing = 0, bool stationary = false) {
        Entity entity = new() { Unit = unit, Faction = faction, Position = Hex.FromOffset(x, y), Facing = facing, Stationary = stationary };
        return !world.Add(entity, out string reason)
            ? throw new InvalidOperationException("Invalid experiment placement: " + reason)
            : entity;
    }

    private static World HeatAndReadiness() {
        World world = World.Create(false, 22, 20, false);
        Entity measured = Place(world, new SiegeWalker(), Faction.MechAndTank, 5, 14);
        measured.Unit.Brain.Settings = new() { Aggression = .55, Caution = .4, Commitment = .1, HeatReserve = 70 };
        _ = Place(world, new TrainingTarget(), Faction.Prytu, 10, 14, 3, true);
        Entity aggressive = Place(world, new SiegeWalker(), Faction.MechAndTank, 5, 5);
        aggressive.Unit.Brain.Settings = new() { Aggression = .95, Caution = .4, Commitment = .1, HeatReserve = 100 };
        _ = Place(world, new TrainingTarget(), Faction.Prytu, 10, 5, 3, true);
        for (int x = 1; x < 21; x++) {
            world.Terrain[Hex.FromOffset(x, 10)] = Terrain.Paved;
        }

        world.Note("Experiment: heat and readiness. Upper walker is measured; lower walker accepts overheating.");
        return world;
    }

    private static World LostInTheForest() {
        World world = World.Create(false, 24, 16, false);
        Entity pursuer = Place(world, new CloneInfantry(), Faction.InfantryAndArtillery, 5, 7);
        Entity runner = Place(world, new TravelerOutrider(), Faction.Travelers, 9, 7);
        runner.Health = 24;
        runner.Unit.Brain.Settings = new() { Aggression = .1, Caution = 1, Commitment = .05, RememberContacts = false };
        for (int y = 2; y <= 13; y++) {
            for (int x = 11; x <= 13; x++) {
                world.Terrain[Hex.FromOffset(x, y)] = Terrain.Forest;
            }
        }
        // A real initial sighting, before the first acting unit rotates. No hidden position is seeded.
        if (world.CanObserve(pursuer, runner)) {
            pursuer.Unit.Brain.State.Contacts.Add(new ContactMemory {
                Id = runner.Id, Faction = runner.Faction, Position = runner.Position,
                LastSeenTurn = 0, Health = runner.Health, MaximumHealth = runner.MaximumHealth
            });
        }

        pursuer.Unit.Brain.State.Reason = "The Traveler was observed at the start; follow its last sighting if contact is lost.";
        world.Note("Experiment: lost in the forest. Select the infantry pursuer to see remembered sightings.");
        return world;
    }

    private static World BondsUnderPressure() {
        World world = World.Create(false, 25, 21, false);
        Entity guard = Place(world, new Bastion(), Faction.Bastions, 5, 10);
        Entity lower = Place(world, new TrainingTarget(), Faction.Bastions, 10, 6, 0, true);
        Entity upper = Place(world, new TrainingTarget(), Faction.Bastions, 10, 14, 0, true);
        lower.Health = 350; upper.Health = 350;
        guard.BondedUnitId = lower.Id;
        guard.Unit.Brain.Settings = new() { Aggression = .6, Caution = .7, Commitment = .2 };
        foreach (int y in new[] { 6, 14 }) {
            for (int i = 0; i < 3; i++) {
                _ = Place(world, new CloneInfantry(), Faction.InfantryAndArtillery, 14 + (i * 2), y, 3);
            }
        }

        world.Note("Experiment: bonds under pressure. The Bastion initially favors the lower ally; compare a different bond.");
        return world;
    }
}

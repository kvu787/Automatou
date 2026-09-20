namespace Automatou.Simulation;

public sealed record BehaviorOption(string Name, double Score, string Reason, int? TargetId = null, Hex? Destination = null);

// Small, inspectable building blocks. Each nested automaton chooses its own candidates.
public static class BehaviorPlanning
{
    public const int ContactLifetime = 8;

    public static WorldObservation Begin(UnitAutomaton brain, UnitSenses senses)
    {
        brain.TurnsObserved++;
        var observation = Observe(brain, senses);
        if (brain.State.Intention == "Investigate" && brain.State.Destination is { } destination &&
            observation.Self.Position.Distance(destination) <= 1 &&
            brain.State.Contacts.FirstOrDefault(contact => contact.Id == brain.TargetId) is { } searched && searched.LastSeenTurn < observation.Turn)
        {
            searched.SearchStep = Math.Min(3, searched.SearchStep + 1);
            if (searched.SearchStep == 3) brain.State.Contacts.Remove(searched);
        }
        brain.State.LastUpdatedTurn = observation.Turn;
        return observation;
    }

    private static WorldObservation Observe(UnitAutomaton brain, UnitSenses senses)
    {
        var observation = senses.Observe();
        if (!brain.Settings.RememberContacts) brain.State.Contacts.Clear();
        else
        {
            foreach (var enemy in observation.Entities.Where(entity => entity.Faction != observation.Self.Faction))
            {
                brain.State.Contacts.RemoveAll(contact => contact.Id == enemy.Id);
                brain.State.Contacts.Add(new ContactMemory { Id = enemy.Id, Faction = enemy.Faction, Position = enemy.Position,
                    LastSeenTurn = observation.Turn, Health = enemy.Health, MaximumHealth = enemy.MaximumHealth });
            }
            brain.State.Contacts = brain.State.Contacts.Where(contact => observation.Turn - contact.LastSeenTurn <= ContactLifetime && contact.SearchStep < 3)
                .OrderByDescending(contact => contact.LastSeenTurn).ThenBy(contact => contact.Position.Distance(observation.Self.Position))
                .ThenBy(contact => contact.Id).Take(8).ToList();
        }
        return observation;
    }

    public static int Distance(EntityObservation a, EntityObservation b) => a.Cells.Min(cell => b.Cells.Min(cell.Distance));

    public static EntityObservation? Enemy(UnitAutomaton brain, WorldObservation observation, bool preferWounded = false, bool considerBlast = false)
    {
        var self = observation.Self;
        return observation.Entities.Where(entity => entity.Faction != self.Faction)
            .OrderBy(entity => Distance(self, entity) - (preferWounded ? 4 * (1 - (double)entity.Health / entity.MaximumHealth) : 0)
                - (entity.Id == brain.TargetId ? brain.Settings.Commitment * 4 : 0)
                + (considerBlast ? FriendlyBlastCost(observation, entity) * (2 + 6 * brain.Settings.Caution) : 0))
            .ThenBy(entity => entity.Id).FirstOrDefault();
    }

    private static int FriendlyBlastCost(WorldObservation observation, EntityObservation target)
    {
        if (observation.Self.Unit.BlastRadius == 0 || Distance(observation.Self, target) <= 1) return 0;
        var impact = target.Cells.MinBy(cell => cell.Distance(observation.Self.Position));
        return observation.Entities.Count(entity => entity.Faction == observation.Self.Faction &&
            entity.Cells.Any(cell => cell.Distance(impact) <= observation.Self.Unit.BlastRadius));
    }

    public static BehaviorOption Engage(UnitAutomaton brain, WorldObservation observation, EntityObservation target, bool considerBlast = false)
    {
        double opportunity = Distance(observation.Self, target) <= observation.Self.Unit.Range ? .12 : 0;
        int allies = considerBlast ? FriendlyBlastCost(observation, target) : 0;
        double score = .35 + .4 * brain.Settings.Aggression + opportunity - allies * .18 * brain.Settings.Caution;
        return new("Engage", Math.Clamp(score, 0, 1), allies > 0
            ? $"Target #{target.Id}; {allies} allied footprint(s) in blast area reduce this choice."
            : $"Target #{target.Id} is visible; aggression {brain.Settings.Aggression:0.00} favors pressure.", target.Id, target.Position);
    }

    public static BehaviorOption Withdraw(UnitAutomaton brain, WorldObservation observation, EntityObservation target, bool skirmish = false)
    {
        double wounded = 1 - (double)observation.Self.Health / observation.Self.MaximumHealth;
        int nearby = observation.Entities.Count(entity => entity.Faction != observation.Self.Faction && Distance(observation.Self, entity) <= 4);
        double close = skirmish && Distance(observation.Self, target) < Math.Max(2, observation.Self.Unit.Range - 1) ? .42 : 0;
        double score = brain.Settings.Caution * (.1 + wounded * .85 + Math.Min(nearby, 3) * .08) + close;
        return new("Withdraw", Math.Clamp(score, 0, 1), $"Health {observation.Self.Health}/{observation.Self.MaximumHealth}; {nearby} nearby threats; caution {brain.Settings.Caution:0.00}.", target.Id, target.Position);
    }

    private static bool NeedsCooling(UnitAutomaton brain, EntityObservation self) => self.WeaponLocked || self.Heat >= brain.Settings.HeatReserve ||
        (brain.Settings.Aggression < .8 && self.Heat > 0 && self.Heat + self.Unit.HeatPerShot > brain.Settings.HeatReserve);

    public static BehaviorOption Recover(UnitAutomaton brain, UnitSenses senses, WorldObservation observation)
    {
        var self = observation.Self;
        bool relevant = senses.Settings.HeatEnabled && self.Unit.HeatPerShot > 0;
        double score = relevant && NeedsCooling(brain, self) ? 1 : 0;
        return new("Recover", score, self.WeaponLocked ? $"Weapon locked at heat {self.Heat}; unlocks at 40."
            : $"Heat {self.Heat}; preferred ceiling {brain.Settings.HeatReserve}; next shot adds {self.Unit.HeatPerShot}.");
    }

    public static BehaviorOption? Escort(UnitAutomaton brain, UnitSenses senses, WorldObservation observation)
    {
        if (!senses.Settings.BondsEnabled || observation.Self.BondedUnitId is not { } id) return null;
        var ward = observation.Entities.FirstOrDefault(entity => entity.Id == id && entity.Faction == observation.Self.Faction);
        if (ward is null) return null;
        double need = 1 - (double)ward.Health / ward.MaximumHealth;
        double selfWounded = 1 - (double)observation.Self.Health / observation.Self.MaximumHealth;
        bool threatened = observation.Entities.Any(entity => entity.Faction != ward.Faction && Distance(ward, entity) <= 5);
        double score = .4 + .25 * need + (threatened ? .25 : 0) + (Distance(observation.Self, ward) > 2 ? .12 : 0)
            - selfWounded * brain.Settings.Caution * .5;
        return new("Escort", Math.Clamp(score, 0, .98), $"Bond to #{id}; ward health {ward.Health}/{ward.MaximumHealth}" +
            (threatened ? "; a visible threat is near the ward." : "; staying within supporting distance."), id, ward.Position);
    }

    public static BehaviorOption Explore(UnitAutomaton brain, UnitSenses senses, WorldObservation observation)
    {
        var self = observation.Self;
        var remembered = brain.State.Contacts.Where(contact => !observation.Entities.Any(entity => entity.Id == contact.Id))
            .OrderByDescending(contact => contact.Id == brain.TargetId).ThenByDescending(contact => contact.LastSeenTurn).ThenBy(contact => contact.Id).FirstOrDefault();
        if (remembered is not null && !self.Stationary)
        {
            Hex destination = remembered.Position;
            if (remembered.SearchStep > 0)
            {
                Hex probe = destination + Hex.Directions[(remembered.Id + remembered.SearchStep) % 6];
                if (senses.TerrainAt(probe) is not null) destination = probe;
            }
            return new("Investigate", .55, $"Last saw #{remembered.Id} at {remembered.Position}, {observation.Turn - remembered.LastSeenTurn} turn(s) ago; search {remembered.SearchStep + 1}/3.", remembered.Id, destination);
        }
        if (self.Stationary) return new("Hold", .1, "Deployed: observing without moving.");
        if (brain.State.Intention == "Patrol" && brain.State.Destination is { } existing && self.Position.Distance(existing) > 1 &&
            observation.Turn - brain.State.IntentionSince < 6)
            return new("Patrol", .12, "Continuing toward a known map location; no enemy location is supplied.", Destination: existing);
        var cells = senses.KnownCells;
        if (cells.Count == 0) return new("Hold", .1, "No known destination.");
        var center = new Hex((int)cells.Average(cell => cell.Q), (int)cells.Average(cell => cell.R));
        int heading = (self.Id + brain.State.PatrolIndex++) % 6;
        Hex desired = center + new Hex(Hex.Directions[heading].Q * 4, Hex.Directions[heading].R * 4);
        Hex? chosen = cells.Where(cell => self.Position.Distance(cell) > 1)
            .OrderBy(cell => cell.Distance(desired)).ThenBy(cell => cell.Q).ThenBy(cell => cell.R)
            .Take(80).Where(cell => senses.CanOccupy(cell, self.Facing)).Select(cell => (Hex?)cell).FirstOrDefault();
        return chosen is { } destinationCell ? new("Patrol", .12, "Explore a known map location to find contacts.", Destination: destinationCell)
            : new("Hold", .1, "No usable patrol destination.");
    }

    public static BehaviorOption Choose(UnitAutomaton brain, WorldObservation observation, IEnumerable<BehaviorOption?> choices)
    {
        var options = choices.OfType<BehaviorOption>().OrderByDescending(option => option.Score).ThenBy(option => option.Name, StringComparer.Ordinal).ToArray();
        if (options.Length == 0) options = [new("Hold", .1, "No available intention.")];
        brain.State.Considerations = options.Take(10).Select(option => new DecisionConsideration(option.Name, option.Score, option.Reason)).ToList();
        var chosen = options[0];
        var current = options.FirstOrDefault(option => option.Name == brain.State.Intention && option.TargetId == brain.TargetId);
        bool retained = current is not null && current.Score > 0 && chosen.Score < 1 &&
            observation.Turn - brain.State.IntentionSince < 4 && chosen.Score - current.Score < brain.Settings.Commitment;
        if (retained) chosen = current!;
        bool changed = chosen.Name != brain.State.Intention || chosen.TargetId != brain.TargetId;
        if (changed)
        {
            brain.State.IntentionChanges++;
            brain.State.IntentionSince = observation.Turn;
        }
        // Reaching a patrol waypoint starts a fresh commitment even with the same intention name.
        else if (chosen.Name == "Patrol" && chosen.Destination != brain.State.Destination) brain.State.IntentionSince = observation.Turn;
        brain.State.Intention = chosen.Name;
        brain.State.Reason = chosen.Reason + (retained && chosen != options[0] ? " Keeping the existing commitment." : "");
        brain.State.Destination = chosen.Destination;
        brain.TargetId = chosen.TargetId;
        brain.State.History.Add(new(observation.Turn, chosen.Name, brain.State.Reason, chosen.Destination));
        if (brain.State.History.Count > 12) brain.State.History.RemoveAt(0);
        return chosen;
    }

    public static IEnumerable<UnitAction> Execute(UnitAutomaton brain, UnitSenses senses, BehaviorOption intention, bool keepDistance = false)
    {
        while (senses.RemainingPoints > 0)
        {
            var observation = Observe(brain, senses);
            var self = observation.Self;
            var enemy = observation.Entities.FirstOrDefault(entity => entity.Id == intention.TargetId && entity.Faction != self.Faction);
            UnitAction? action = null;
            if (intention.Name == "Engage")
            {
                if (enemy is null) { brain.State.Reason = "Contact lost during the turn; reconsidering next turn."; yield break; }
                if (senses.Settings.HeatEnabled && self.Unit.HeatPerShot > 0 && NeedsCooling(brain, self))
                { brain.State.Reason = "Firing would exceed the chosen heat policy; waiting for the next decision."; yield break; }
                action = TacticalPlanning.Engage(senses, observation, enemy, keepDistance);
            }
            else if (intention.Name == "Withdraw")
            {
                if (enemy is null) yield break;
                action = Retreat(senses, observation, enemy);
                if (action is null && senses.CanAttack(enemy.Id)) action = new AttackAction(enemy.Id);
            }
            else if (intention.Name == "Recover")
            {
                // Improve cooling only when an adjacent cell is actually better. Cooling while
                // waiting is useful too, and prevents a recovery routine from wandering forever.
                var better = Enumerable.Range(0, 6).Where(direction => senses.CanOccupy(self.Position + Hex.Directions[direction], direction))
                    .Where(direction => senses.CoolingAt(self.Position + Hex.Directions[direction]) > senses.CoolingAt(self.Position))
                    .OrderByDescending(direction => senses.CoolingAt(self.Position + Hex.Directions[direction]))
                    .ThenBy(direction => Hex.TurnDistance(self.Facing, direction)).ToArray();
                if (better.Length > 0) action = DirectionAction(senses, self, better[0]);
            }
            else if (intention.Name == "Escort")
            {
                var ward = observation.Entities.FirstOrDefault(entity => entity.Id == intention.TargetId && entity.Faction == self.Faction);
                if (ward is null) { brain.State.Reason = "Bonded unit is not currently observed; no hidden location is supplied."; yield break; }
                brain.State.Destination = ward.Position;
                // Supporting a ward means proximity and attacking its threats, not intercepting bullets.
                var threat = observation.Entities.Where(entity => entity.Faction != self.Faction && Distance(entity, ward) <= 5)
                    .OrderBy(entity => Distance(self, entity) > self.Unit.Range).ThenBy(entity => Distance(entity, ward)).ThenBy(entity => entity.Id).FirstOrDefault();
                if (threat is not null && Distance(self, threat) <= self.Unit.Range)
                    action = TacticalPlanning.Engage(senses, observation, threat, keepDistance: false);
                if (action is null && Distance(self, ward) > 2)
                    action = MoveToward(senses, self, ward.Position, self.Unit.Size + ward.Unit.Size);
            }
            else if (intention.Name is "Investigate" or "Patrol" && intention.Destination is { } destination)
            {
                if (Enemy(brain, observation) is not null)
                { brain.State.Reason = "A new enemy is visible; reconsidering at the next turn boundary."; yield break; }
                action = MoveToward(senses, self, destination, intention.Name == "Patrol" ? 1 : 0);
            }
            if (action is null) yield break;
            yield return action;
            // A successful final action can exhaust the turn budget before the world resumes us.
            // World records successful attacks; do not infer success from yielding a request here.
        }
    }

    private static UnitAction? Retreat(UnitSenses senses, WorldObservation observation, EntityObservation enemy)
    {
        var self = observation.Self;
        int present = self.Position.Distance(enemy.Position);
        var escape = Enumerable.Range(0, 6).Where(direction => senses.CanOccupy(self.Position + Hex.Directions[direction], direction))
            .Where(direction => (self.Position + Hex.Directions[direction]).Distance(enemy.Position) > present)
            .OrderByDescending(direction => (self.Position + Hex.Directions[direction]).Distance(enemy.Position))
            .ThenBy(direction => Hex.TurnDistance(self.Facing, direction)).ToArray();
        return escape.Length == 0 ? null : DirectionAction(senses, self, escape[0]);
    }

    private static UnitAction? MoveToward(UnitSenses senses, EntityObservation self, Hex destination, int stoppingDistance)
    {
        if (self.Position.Distance(destination) <= stoppingDistance) return null;
        return DirectionAction(senses, self, senses.FindDirection(destination, stoppingDistance));
    }

    private static UnitAction? DirectionAction(UnitSenses senses, EntityObservation self, int direction)
    {
        if (self.Stationary || direction < 0) return null;
        if (self.Facing != direction) return new TurnAction((direction - self.Facing + 6) % 6 <= 3 ? 1 : -1);
        return senses.RemainingPoints >= senses.MovementCost(self.Position + Hex.Directions[direction]) ? new MoveForwardAction() : null;
    }
}

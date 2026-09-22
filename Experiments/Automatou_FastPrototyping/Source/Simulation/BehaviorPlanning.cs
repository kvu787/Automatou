namespace Automatou.Simulation;

public sealed record BehaviorOption(string Name, double Score, string Reason, int? TargetId = null, Hex? Destination = null);

// Small, inspectable building blocks. Each nested automaton chooses its own candidates.
public static class BehaviorPlanning {
    public const int ContactLifetime = 8;

    public static WorldObservation Begin(UnitAutomaton automaton, UnitSenses senses) {
        // Called once by Act. Search progress advances only at turn boundaries;
        // Observe below can refresh contacts many times during the same turn.
        automaton.TurnsObserved++;
        WorldObservation observation = Observe(automaton, senses);
        if (automaton.State.Intention == "Investigate" && automaton.State.Destination is { } destination &&
            observation.Self.Position.Distance(destination) <= 1 &&
            automaton.State.Contacts.FirstOrDefault(contact => contact.Id == automaton.TargetId) is { } searched && searched.LastSeenTurn < observation.Turn) {
            searched.SearchStep = Math.Min(3, searched.SearchStep + 1);
            if (searched.SearchStep == 3) {
                _ = automaton.State.Contacts.Remove(searched);
            }
        }
        automaton.State.LastUpdatedTurn = observation.Turn;
        return observation;
    }

    private static WorldObservation Observe(UnitAutomaton automaton, UnitSenses senses) {
        // Visible enemies replace their old sightings. Unseen enemies retain only
        // last-seen facts, including enemies that may since have died out of sight.
        WorldObservation observation = senses.Observe();
        if (!automaton.Settings.RememberContacts) {
            automaton.State.Contacts.Clear();
        } else {
            foreach (EntityObservation? enemy in observation.Entities.Where(entity => entity.Faction != observation.Self.Faction)) {
                _ = automaton.State.Contacts.RemoveAll(contact => contact.Id == enemy.Id);
                automaton.State.Contacts.Add(new ContactMemory {
                    Id = enemy.Id, Faction = enemy.Faction, Position = enemy.Position,
                    LastSeenTurn = observation.Turn, Health = enemy.Health, MaximumHealth = enemy.MaximumHealth
                });
            }
            automaton.State.Contacts = [.. automaton.State.Contacts.Where(contact => observation.Turn - contact.LastSeenTurn <= ContactLifetime && contact.SearchStep < 3)
                .OrderByDescending(contact => contact.LastSeenTurn).ThenBy(contact => contact.Position.Distance(observation.Self.Position))
                .ThenBy(contact => contact.Id).Take(8)];
        }
        return observation;
    }

    public static int Distance(EntityObservation a, EntityObservation b) {
        return a.Cells.Min(cell => b.Cells.Min(cell.Distance));
    }

    public static EntityObservation? Enemy(UnitAutomaton automaton, WorldObservation observation, bool preferWounded = false, bool considerBlast = false) {
        // Lower rank wins: footprint distance minus wounded/commitment bonuses,
        // plus allied blast exposure. This selects a target before intentions compete.
        EntityObservation self = observation.Self;
        return observation.Entities.Where(entity => entity.Faction != self.Faction)
            .OrderBy(entity => Distance(self, entity) - (preferWounded ? 4 * (1 - ((double)entity.Health / entity.MaximumHealth)) : 0)
                - (entity.Id == automaton.TargetId ? automaton.Settings.Commitment * 4 : 0)
                + (considerBlast ? FriendlyBlastCost(observation, entity) * (2 + (6 * automaton.Settings.Caution)) : 0))
            .ThenBy(entity => entity.Id).FirstOrDefault();
    }

    private static int FriendlyBlastCost(WorldObservation observation, EntityObservation target) {
        if (observation.Self.Unit.BlastRadius == 0 || Distance(observation.Self, target) <= 1) {
            return 0;
        }

        Hex impact = target.Cells.MinBy(cell => cell.Distance(observation.Self.Position));
        return observation.Entities.Count(entity => entity.Faction == observation.Self.Faction &&
            entity.Cells.Any(cell => cell.Distance(impact) <= observation.Self.Unit.BlastRadius));
    }

    public static BehaviorOption Engage(UnitAutomaton automaton, WorldObservation observation, EntityObservation target, bool considerBlast = false) {
        // Scores are relative preferences, not probabilities or guarantees of a legal
        // action. Facing, visibility and the remaining budget are checked at execution.
        double opportunity = Distance(observation.Self, target) <= observation.Self.Unit.Range ? .12 : 0;
        int allies = considerBlast ? FriendlyBlastCost(observation, target) : 0;
        double score = .35 + (.4 * automaton.Settings.Aggression) + opportunity - (allies * .18 * automaton.Settings.Caution);
        return new("Engage", Math.Clamp(score, 0, 1), allies > 0
            ? $"Target #{target.Id}; {allies} allied footprint(s) in blast area reduce this choice."
            : $"Target #{target.Id} is visible; aggression {automaton.Settings.Aggression:0.00} favors pressure.", target.Id, target.Position);
    }

    public static BehaviorOption Withdraw(UnitAutomaton automaton, WorldObservation observation, EntityObservation target, bool skirmish = false) {
        double wounded = 1 - ((double)observation.Self.Health / observation.Self.MaximumHealth);
        int nearby = observation.Entities.Count(entity => entity.Faction != observation.Self.Faction && Distance(observation.Self, entity) <= 4);
        double close = skirmish && Distance(observation.Self, target) < Math.Max(2, observation.Self.Unit.Range - 1) ? .42 : 0;
        double score = (automaton.Settings.Caution * (.1 + (wounded * .85) + (Math.Min(nearby, 3) * .08))) + close;
        return new("Withdraw", Math.Clamp(score, 0, 1), $"Health {observation.Self.Health}/{observation.Self.MaximumHealth}; {nearby} nearby threats; caution {automaton.Settings.Caution:0.00}.", target.Id, target.Position);
    }

    private static bool NeedsCooling(UnitAutomaton automaton, EntityObservation self) {
        return self.WeaponLocked || self.Heat >= automaton.Settings.HeatReserve ||
        (automaton.Settings.Aggression < .8 && self.Heat > 0 && self.Heat + self.Unit.HeatPerShot > automaton.Settings.HeatReserve);
    }

    public static BehaviorOption Recover(UnitAutomaton automaton, UnitSenses senses, WorldObservation observation) {
        // Recovery scores 1 when needed, bypassing commitment to a lower-scoring
        // intention. Waiting adds no cooling bonus; World.Step cools every unit.
        EntityObservation self = observation.Self;
        bool relevant = senses.Settings.HeatEnabled && self.Unit.HeatPerShot > 0;
        double score = relevant && NeedsCooling(automaton, self) ? 1 : 0;
        return new("Recover", score, self.WeaponLocked ? $"Weapon locked at heat {self.Heat}; unlocks at 40."
            : $"Heat {self.Heat}; preferred ceiling {automaton.Settings.HeatReserve}; next shot adds {self.Unit.HeatPerShot}.");
    }

    public static BehaviorOption? Escort(UnitAutomaton automaton, UnitSenses senses, WorldObservation observation) {
        if (!senses.Settings.BondsEnabled || observation.Self.BondedUnitId is not { } id) {
            return null;
        }

        EntityObservation? ward = observation.Entities.FirstOrDefault(entity => entity.Id == id && entity.Faction == observation.Self.Faction);
        if (ward is null) {
            return null;
        }

        double need = 1 - ((double)ward.Health / ward.MaximumHealth);
        double selfWounded = 1 - ((double)observation.Self.Health / observation.Self.MaximumHealth);
        bool threatened = observation.Entities.Any(entity => entity.Faction != ward.Faction && Distance(ward, entity) <= 5);
        double score = .4 + (.25 * need) + (threatened ? .25 : 0) + (Distance(observation.Self, ward) > 2 ? .12 : 0)
            - (selfWounded * automaton.Settings.Caution * .5);
        return new("Escort", Math.Clamp(score, 0, .98), $"Bond to #{id}; ward health {ward.Health}/{ward.MaximumHealth}" +
            (threatened ? "; a visible threat is near the ward." : "; staying within supporting distance."), id, ward.Position);
    }

    public static BehaviorOption Explore(UnitAutomaton automaton, UnitSenses senses, WorldObservation observation) {
        // Prefer a lost contact's last known location; otherwise choose a deterministic
        // patrol waypoint from known terrain. Neither path reveals hidden enemy positions.
        EntityObservation self = observation.Self;
        ContactMemory? remembered = automaton.State.Contacts.Where(contact => !observation.Entities.Any(entity => entity.Id == contact.Id))
            .OrderByDescending(contact => contact.Id == automaton.TargetId).ThenByDescending(contact => contact.LastSeenTurn).ThenBy(contact => contact.Id).FirstOrDefault();
        if (remembered is not null && !self.Stationary) {
            Hex destination = remembered.Position;
            if (remembered.SearchStep > 0) {
                Hex probe = destination + Hex.Directions[(remembered.Id + remembered.SearchStep) % 6];
                if (senses.TerrainAt(probe) is not null) {
                    destination = probe;
                }
            }
            return new("Investigate", .55, $"Last saw #{remembered.Id} at {remembered.Position}, {observation.Turn - remembered.LastSeenTurn} turn(s) ago; search {remembered.SearchStep + 1}/3.", remembered.Id, destination);
        }
        if (self.Stationary) {
            return new("Hold", .1, "Deployed: observing without moving.");
        }

        if (automaton.State.Intention == "Patrol" && automaton.State.Destination is { } existing && self.Position.Distance(existing) > 1 &&
            observation.Turn - automaton.State.IntentionSince < 6) {
            return new("Patrol", .12, "Continuing toward a known map location; no enemy location is supplied.", Destination: existing);
        }

        IReadOnlyList<Hex> cells = senses.KnownCells;
        if (cells.Count == 0) {
            return new("Hold", .1, "No known destination.");
        }

        Hex center = new((int)cells.Average(cell => cell.Q), (int)cells.Average(cell => cell.R));
        int heading = (self.Id + automaton.State.PatrolIndex++) % 6;
        Hex desired = center + new Hex(Hex.Directions[heading].Q * 4, Hex.Directions[heading].R * 4);
        Hex? chosen = cells.Where(cell => self.Position.Distance(cell) > 1)
            .OrderBy(cell => cell.Distance(desired)).ThenBy(cell => cell.Q).ThenBy(cell => cell.R)
            .Take(80).Where(senses.CanOccupy).Select(cell => (Hex?)cell).FirstOrDefault();
        return chosen is { } destinationCell ? new("Patrol", .12, "Explore a known map location to find contacts.", Destination: destinationCell)
            : new("Hold", .1, "No usable patrol destination.");
    }

    public static BehaviorOption Choose(UnitAutomaton automaton, WorldObservation observation, IEnumerable<BehaviorOption?> choices) {
        // Highest score wins, with ordinal name order breaking ties. A recent intention
        // can survive a small score deficit for fewer than four elapsed turns; the
        // same intention must still be offered for the same target with a positive score.
        BehaviorOption[] options = choices.OfType<BehaviorOption>().OrderByDescending(option => option.Score).ThenBy(option => option.Name, StringComparer.Ordinal).ToArray();
        if (options.Length == 0) {
            options = [new("Hold", .1, "No available intention.")];
        }

        automaton.State.Considerations = [.. options.Take(10).Select(option => new DecisionConsideration(option.Name, option.Score, option.Reason))];
        // Inspector scores stay in raw rank order even if commitment keeps another option.
        BehaviorOption chosen = options[0];
        BehaviorOption? current = options.FirstOrDefault(option => option.Name == automaton.State.Intention && option.TargetId == automaton.TargetId);
        bool retained = current is not null && current.Score > 0 && chosen.Score < 1 &&
            observation.Turn - automaton.State.IntentionSince < 4 && chosen.Score - current.Score < automaton.Settings.Commitment;
        if (retained) {
            chosen = current!;
        }

        bool changed = chosen.Name != automaton.State.Intention || chosen.TargetId != automaton.TargetId;
        if (changed) {
            automaton.State.IntentionChanges++;
            automaton.State.IntentionSince = observation.Turn;
        }
        // Reaching a patrol waypoint starts a fresh commitment even with the same intention name.
        else if (chosen.Name == "Patrol" && chosen.Destination != automaton.State.Destination) {
            automaton.State.IntentionSince = observation.Turn;
        }

        automaton.State.Intention = chosen.Name;
        automaton.State.Reason = chosen.Reason + (retained && chosen != options[0] ? " Keeping the existing commitment." : "");
        automaton.State.Destination = chosen.Destination;
        automaton.TargetId = chosen.TargetId;
        automaton.State.History.Add(new(observation.Turn, chosen.Name, automaton.State.Reason, chosen.Destination));
        if (automaton.State.History.Count > 12) {
            automaton.State.History.RemoveAt(0);
        }

        return chosen;
    }

    public static IEnumerable<UnitAction> Execute(UnitAutomaton automaton, UnitSenses senses, BehaviorOption intention, bool keepDistance = false) {
        // The intention stays fixed, but positions/health/contacts are freshly observed
        // each iteration. yield return hands control to World.ApplyAction; on resumption
        // the world and senses budget already reflect that action. If no action is
        // available, execution stops even when points remain.
        while (senses.RemainingPoints > 0) {
            WorldObservation observation = Observe(automaton, senses);
            EntityObservation self = observation.Self;
            EntityObservation? enemy = observation.Entities.FirstOrDefault(entity => entity.Id == intention.TargetId && entity.Faction != self.Faction);
            UnitAction? action = null;
            if (intention.Name == "Engage") {
                if (enemy is null) { automaton.State.Reason = "Contact lost during the turn; reconsidering next turn."; yield break; }
                if (senses.Settings.HeatEnabled && self.Unit.HeatPerShot > 0 && NeedsCooling(automaton, self)) { automaton.State.Reason = "Firing would exceed the chosen heat policy; waiting for the next decision."; yield break; }
                action = TacticalPlanning.Engage(senses, observation, enemy, keepDistance);
            } else if (intention.Name == "Withdraw") {
                if (enemy is null) {
                    yield break;
                }

                action = Retreat(senses, observation, enemy);
                if (action is null && senses.CanAttack(enemy.Id)) {
                    action = new AttackAction(enemy.Id);
                }
            } else if (intention.Name == "Recover") {
                // Cooling is identical on every terrain; recover in place.
                yield break;
            } else if (intention.Name == "Escort") {
                EntityObservation? ward = observation.Entities.FirstOrDefault(entity => entity.Id == intention.TargetId && entity.Faction == self.Faction);
                if (ward is null) { automaton.State.Reason = "Bonded unit is not currently observed; no hidden location is supplied."; yield break; }
                automaton.State.Destination = ward.Position;
                // Supporting a ward means proximity and attacking its threats, not intercepting bullets.
                EntityObservation? threat = observation.Entities.Where(entity => entity.Faction != self.Faction && Distance(entity, ward) <= 5)
                    .OrderBy(entity => Distance(self, entity) > self.Unit.Range).ThenBy(entity => Distance(entity, ward)).ThenBy(entity => entity.Id).FirstOrDefault();
                if (threat is not null && Distance(self, threat) <= self.Unit.Range) {
                    action = TacticalPlanning.Engage(senses, observation, threat, keepDistance: false);
                }

                if (action is null && Distance(self, ward) > 2) {
                    action = MoveToward(senses, self, ward.Position, self.Unit.Size + ward.Unit.Size);
                }
            } else if (intention.Name is "Investigate" or "Patrol" && intention.Destination is { } destination) {
                if (Enemy(automaton, observation) is not null) { automaton.State.Reason = "A new enemy is visible; reconsidering at the next turn boundary."; yield break; }
                action = MoveToward(senses, self, destination, intention.Name == "Patrol" ? 1 : 0);
            }
            if (action is null) {
                yield break;
            }

            yield return action;
            // A successful final action can exhaust the turn budget before the world resumes us.
            // World records successful attacks; do not infer success from yielding a request here.
        }
    }

    private static UnitAction? Retreat(UnitSenses senses, WorldObservation observation, EntityObservation enemy) {
        EntityObservation self = observation.Self;
        int present = self.Position.Distance(enemy.Position);
        int[] escape = Enumerable.Range(0, 6).Where(direction => senses.CanOccupy(self.Position + Hex.Directions[direction]))
            .Where(direction => (self.Position + Hex.Directions[direction]).Distance(enemy.Position) > present)
            .OrderByDescending(direction => (self.Position + Hex.Directions[direction]).Distance(enemy.Position))
            .ThenBy(direction => Hex.TurnDistance(self.Facing, direction)).ToArray();
        return escape.Length == 0 ? null : DirectionAction(senses, self, escape[0]);
    }

    private static UnitAction? MoveToward(UnitSenses senses, EntityObservation self, Hex destination, int stoppingDistance) {
        return self.Position.Distance(destination) <= stoppingDistance
            ? null
            : DirectionAction(senses, self, senses.FindDirection(destination, stoppingDistance));
    }

    private static UnitAction? DirectionAction(UnitSenses senses, EntityObservation self, int direction) {
        if (self.Stationary || direction < 0) {
            return null;
        }

        return self.Facing != direction
            ? new TurnAction((direction - self.Facing + 6) % 6 <= 3 ? 1 : -1)
            : senses.RemainingPoints >= senses.MovementCost(self.Position + Hex.Directions[direction]) ? new MoveForwardAction() : null;
    }
}

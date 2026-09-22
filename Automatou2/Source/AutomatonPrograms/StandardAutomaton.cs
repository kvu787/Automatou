namespace Automatou.Simulation;

public class StandardAutomaton : UnitAutomaton {
    public override string ProgramName => "Standard";
    protected override UnitAutomaton CreateInstance() {
        return new StandardAutomaton();
    }

    protected override IReadOnlyList<UnitAction> Think(VisionObservation vision, TerrainObservation terrain, int energy) {
        ActionPlanning plan = new(vision, terrain, energy);
        this.PlanStandard(vision, terrain, plan);
        return plan.Actions;
    }

    // Shared policy is optional. Special programs can use any subset or replace it.
    protected void PlanStandard(VisionObservation vision, TerrainObservation terrain, ActionPlanning plan) {
        EntityObservation self = plan.Self;
        EntityObservation? enemy = plan.Enemies.OrderBy(target => ActionPlanning.Distance(self, target)
            - (this.Settings.PreferWounded ? 4 * (1 - (double)target.Health / target.MaximumHealth) : 0)
            - (target.Id == this.TargetId ? this.Settings.Commitment * 4 : 0)
            + (this.Settings.ConsiderBlast ? BlastExposure(plan, target) * (2 + 6 * this.Settings.Caution) : 0)).ThenBy(target => target.Id).FirstOrDefault();
        List<BehaviorOption> choices = [];
        if (enemy is not null) {
            double engage = .35 + .4 * this.Settings.Aggression + (ActionPlanning.Distance(self, enemy) <= self.Unit.Range ? .12 : 0)
                - (this.Settings.ConsiderBlast ? BlastExposure(plan, enemy) * .18 * this.Settings.Caution : 0);
            double retreat = this.Settings.Caution * (.1 + .85 * (1 - (double)self.Health / self.MaximumHealth) + .08 * Math.Min(3, plan.Enemies.Count(target => ActionPlanning.Distance(self, target) <= 4)))
                + (this.Settings.KeepDistance && ActionPlanning.Distance(self, enemy) < Math.Max(2, self.Unit.Range - 1) ? .42 : 0);
            choices.Add(new("Engage", Math.Clamp(engage, 0, 1), $"Engage observed enemy #{enemy.Id}.", enemy.Id, enemy.Position));
            choices.Add(new("Withdraw", Math.Clamp(retreat, 0, 1), "Health and nearby threats favor withdrawal.", enemy.Id, enemy.Position));
        } else { choices.Add(this.Explore(vision, terrain, plan)); }
        if (vision.Settings.HeatEnabled && self.Unit.HeatPerShot > 0 && this.NeedsCooling(self)) {
            choices.Add(new("Recover", 1, "Conserve energy and let the weapon cool."));
        }
        if (this.Settings.UseBonds && vision.Settings.BondsEnabled && self.BondedUnitId is { } bond &&
            plan.Entities.FirstOrDefault(entity => entity.Id == bond && entity.Faction == self.Faction) is { } ward) {
            double score = .4 + .25 * (1 - (double)ward.Health / ward.MaximumHealth)
                + (plan.Enemies.Any(target => ActionPlanning.Distance(ward, target) <= 5) ? .25 : 0)
                + (ActionPlanning.Distance(self, ward) > 2 ? .12 : 0) - (1 - (double)self.Health / self.MaximumHealth) * this.Settings.Caution * .5;
            choices.Add(new("Escort", Math.Clamp(score, 0, .98), $"Support observed ally #{ward.Id}.", ward.Id, ward.Position));
        }
        BehaviorOption option = this.Choose(vision.World.Turn, choices);
        if (option.Name == "Engage" && enemy is not null) { plan.Engage(enemy); } else if (option.Name == "Withdraw" && enemy is not null) {
            int distance = ActionPlanning.Distance(self, enemy);
            plan.MoveTo(position => plan.DistanceFrom(position, enemy) >= distance + 2);
        } else if (option.Name == "Escort" && plan.Entities.FirstOrDefault(entity => entity.Id == option.TargetId) is { } ally) {
            EntityObservation? threat = plan.Enemies.Where(target => ActionPlanning.Distance(ally, target) <= 5)
                .OrderBy(target => ActionPlanning.Distance(self, target)).ThenBy(target => target.Id).FirstOrDefault();
            if (threat is not null && ActionPlanning.Distance(self, threat) <= self.Unit.Range) { plan.Engage(threat); } else { plan.MoveTo(position => plan.DistanceFrom(position, ally) <= 2); }
        } else if (option.Name is "Patrol" or "Investigate" && option.Destination is { } destination) { plan.MoveTo(destination, 1); }
    }

    protected bool NeedsCooling(EntityObservation self) {
        return self.WeaponLocked || self.Heat >= this.Settings.HeatReserve ||
        (this.Settings.Aggression < .8 && self.Heat > 0 && self.Heat + self.Unit.HeatPerShot > this.Settings.HeatReserve);
    }

    private static int BlastExposure(ActionPlanning plan, EntityObservation enemy) {
        if (plan.Self.Unit.BlastRadius == 0 || ActionPlanning.Distance(plan.Self, enemy) <= 1) { return 0; }
        Hex impact = enemy.Cells.MinBy(cell => cell.Distance(plan.Self.Position));
        return plan.Entities.Count(entity => entity.Faction == plan.Self.Faction && entity.Cells.Any(cell => cell.Distance(impact) <= plan.Self.Unit.BlastRadius));
    }
    private BehaviorOption Explore(VisionObservation vision, TerrainObservation terrain, ActionPlanning plan) {
        if (plan.Self.Stationary) { return new("Hold", .1, "Deployed in place."); }
        ContactMemory? contact = this.State.Contacts.OrderByDescending(value => value.LastSeenTurn).ThenBy(value => value.Id).FirstOrDefault();
        if (contact is not null) {
            if (plan.Self.Position.Distance(contact.Position) <= 1) { _ = this.State.Contacts.Remove(contact); } else { return new("Investigate", .55, $"Search the last sighting of #{contact.Id}; no current location is known.", contact.Id, contact.Position); }
        }
        if (this.State.Intention == "Patrol" && this.State.Destination is { } previous && plan.Self.Position.Distance(previous) > 1 &&
            vision.World.Turn - this.State.IntentionSince < 6) { return new("Patrol", .12, "Continue to the surveyed waypoint.", Destination: previous); }
        Hex[] cells = terrain.Cells.Keys.OrderBy(cell => cell.Q).ThenBy(cell => cell.R).ToArray();
        if (cells.Length == 0) { return new("Hold", .1, "No surveyed terrain."); }
        Hex desired = cells[(int)(((long)plan.Self.Id * 37 + this.State.PatrolIndex++ * 83L) % cells.Length)];
        Hex? chosen = cells.Where(cell => plan.Self.Position.Distance(cell) > 1 && plan.CanOccupy(cell)).OrderBy(cell => cell.Distance(desired)).Select(cell => (Hex?)cell).FirstOrDefault();
        return chosen is { } waypoint ? new("Patrol", .12, "Explore a surveyed location.", Destination: waypoint) : new("Hold", .1, "No usable waypoint.");
    }
    protected void Explain(int turn, string intention, string reason, int? target = null, Hex? destination = null) {
        _ = this.Choose(turn, [new(intention, 1, reason, target, destination)]);
    }

    private BehaviorOption Choose(int turn, IEnumerable<BehaviorOption> options) {
        BehaviorOption[] choices = options.OrderByDescending(option => option.Score).ThenBy(option => option.Name, StringComparer.Ordinal).ToArray();
        BehaviorOption chosen = choices[0];
        BehaviorOption? current = choices.FirstOrDefault(option => option.Name == this.State.Intention && option.TargetId == this.TargetId);
        if (current is not null && current.Score > 0 && chosen.Score < 1 && turn - this.State.IntentionSince < 4 && chosen.Score - current.Score < this.Settings.Commitment) { chosen = current; }
        if (this.State.Intention != chosen.Name || this.TargetId != chosen.TargetId || this.State.Destination != chosen.Destination) {
            this.State.IntentionChanges++; this.State.IntentionSince = turn;
        }
        this.State.Considerations = [.. choices.Select(option => new DecisionConsideration(option.Name, option.Score, option.Reason))];
        this.State.Intention = chosen.Name; this.State.Reason = chosen.Reason; this.TargetId = chosen.TargetId; this.State.Destination = chosen.Destination;
        this.State.History.Add(new(turn, chosen.Name, chosen.Reason, chosen.Destination));
        if (this.State.History.Count > 12) { this.State.History.RemoveAt(0); }
        return chosen;
    }
    private sealed record BehaviorOption(string Name, double Score, string Reason, int? TargetId = null, Hex? Destination = null);
}

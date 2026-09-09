using Automapolis.Kernel;

namespace Automapolis.TerminalShell;

internal static class TerminalRenderer
{
    public static void Map(TextWriter output, WorldSnapshot snapshot)
    {
        // Preserve the Kernel's authoritative glyphs and force display priority.
        using var rendered = new StringReader(TextWorldRenderer.Render(snapshot));
        output.WriteLine(rendered.ReadLine());
        output.WriteLine(rendered.ReadLine());
        output.Write("    ");
        for (var column = 0; column < snapshot.Width; column++)
            output.Write($"{column:00}  ");
        output.WriteLine("X");
        output.Write(rendered.ReadToEnd());
        output.WriteLine("Use inspect X Y to see terrain and every force sharing a sector.");
    }

    public static void Status(TextWriter output, WorldSnapshot snapshot)
    {
        var metrics = snapshot.Metrics;
        output.WriteLine($"{snapshot.Name} | Turn {snapshot.Turn} | Seed {snapshot.Seed}");
        output.WriteLine($"Grid: {snapshot.Width} x {snapshot.Height} | {snapshot.Topology} / {snapshot.Coordinates}");
        output.WriteLine($"Human forces: {metrics.HumanForces} | Alien forces: {metrics.AlienForces}");
        output.WriteLine($"Bastions: {metrics.Bastions} | Enclaves: {metrics.Enclaves} | Human population: {metrics.HumanPopulation}");
        output.WriteLine($"Resonance: {metrics.TotalResonance} | Biomass: {metrics.TotalBiomass} | Theater integrity: {metrics.TheaterIntegrity}%");
    }

    public static void Inspect(TextWriter output, WorldSnapshot snapshot, GridPoint position)
    {
        var tile = snapshot.Tiles.FirstOrDefault(tile => tile.Position == position)
            ?? throw new ArgumentException($"Position {position} is outside the grid. X: 0..{snapshot.Width - 1}; Y: 0..{snapshot.Height - 1}.");
        output.WriteLine($"Sector {position} | {tile.Terrain} [{tile.Glyph}]");
        output.WriteLine(tile.Description);
        Forces(output, snapshot.Forces.Where(force => force.Position == position));
    }

    public static void Forces(TextWriter output, IEnumerable<ForceSnapshot> forces)
    {
        var count = 0;
        foreach (var force in forces)
        {
            count++;
            output.WriteLine($"#{force.Id} [{force.Glyph}] {force.Name} | {force.Kind} | Sector {force.Position}");
            output.WriteLine($"  Strength: {force.Strength} | Population: {force.Population} | Service turns: {force.ServiceTurns}");
            output.WriteLine($"  Intent: {force.Intent}");
        }
        if (count == 0)
            output.WriteLine("No forces found.");
    }

    public static void Legend(TextWriter output, WorldSnapshot snapshot)
    {
        output.WriteLine("Terrain present in this world:");
        foreach (var tile in snapshot.Tiles.DistinctBy(tile => tile.Terrain).OrderBy(tile => tile.Terrain))
            output.WriteLine($"  [{tile.Glyph}] {tile.Terrain}");
        output.WriteLine("Forces present in this world (a force glyph covers the terrain):");
        foreach (var force in snapshot.Forces.DistinctBy(force => force.Kind).OrderBy(force => force.Kind))
            output.WriteLine($"  [{force.Glyph}] {force.Kind}");
        output.WriteLine("Coordinates are zero-based X (column), Y (row); odd rows shift right.");
    }
}

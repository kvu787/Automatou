namespace Automatou.Simulation;

public sealed class SessionWorlds {
    private readonly Dictionary<string, World> worlds = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> WorldNames => this.worlds.Keys.Order(StringComparer.OrdinalIgnoreCase);

    public void SaveWorld(string name, World world) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.worlds[name.Trim()] = world.Copy();
    }

    public World LoadWorld(string name) {
        return this.worlds[name.Trim()].Copy();
    }
}

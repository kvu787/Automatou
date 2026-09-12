namespace Automatou.Kernel;

internal static class DeterministicNoise
{
    public static ulong At(long seed, int turn, int x, int y, int channel = 0)
    {
        var value = unchecked((ulong)seed);
        value ^= unchecked((ulong)(turn * 0x45d9f3b));
        value ^= unchecked((ulong)(x * 0x27d4eb2d));
        value ^= unchecked((ulong)(y * 0x165667b1));
        value ^= unchecked((ulong)(channel * 0x9e3779b9));
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }

    public static int Range(long seed, int turn, int x, int y, int channel, int exclusiveMax) =>
        (int)(At(seed, turn, x, y, channel) % (uint)exclusiveMax);
}

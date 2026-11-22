namespace L3Lint.FixtureGen.Corpus;

/// <summary>
/// SplitMix64: a tiny deterministic generator, so the committed corpus never depends on the runtime's
/// <see cref="Random"/> implementation. The seed is fixed (Test and delivery tab: "generated once with a fixed seed").
/// </summary>
internal sealed class Prng(ulong seed)
{
    private ulong _state = seed;

    public ulong Next()
    {
        var z = _state += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Returns an integer in [min, max).</summary>
    public int Between(int min, int max) => min + (int)(Next() % (ulong)(max - min));

    public T Pick<T>(IReadOnlyList<T> items) => items[Between(0, items.Count)];
}

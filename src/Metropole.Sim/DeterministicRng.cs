namespace Metropole.Sim;

public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(long seed)
    {
        _state = unchecked((ulong)seed) ^ 0x9E3779B97F4A7C15UL;
        if (_state == 0) _state = 0xA0761D6478BD642FUL;
        for (var i = 0; i < 8; i++) NextUInt64();
    }

    public ulong NextUInt64()
    {
        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * 2685821657736338717UL;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        var range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt64() % range);
    }

    public decimal NextDecimal(decimal minInclusive, decimal maxInclusive)
    {
        var unit = (decimal)((NextUInt64() >> 11) * (1.0 / (1UL << 53)));
        return minInclusive + ((maxInclusive - minInclusive) * unit);
    }

    public bool Chance(double probability)
    {
        if (probability <= 0) return false;
        if (probability >= 1) return true;
        var unit = (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        return unit < probability;
    }

    public T Pick<T>(IReadOnlyList<T> values)
    {
        if (values.Count == 0) throw new InvalidOperationException("Lista vazia.");
        return values[NextInt(0, values.Count)];
    }
}

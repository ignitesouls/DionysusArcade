// SPDX-License-Identifier: GPL-3.0-only
namespace Dionysus.Core.Randomization;

public static class RandomPick
{
    // A seeded pick of up to `count` different entries
    public static List<T> From<T>(IReadOnlyList<T> pool, int count, Random rng)
    {
        var copy = new List<T>(pool);
        int take = Math.Min(count, copy.Count);
        for (int i = 0; i < take; i++)
        {
            int j = rng.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, take);
    }
}
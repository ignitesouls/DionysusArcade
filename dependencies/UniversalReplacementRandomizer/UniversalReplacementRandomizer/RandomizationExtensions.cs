// SPDX-License-Identifier: GPL-3.0-only
namespace UniversalReplacementRandomizer;

public static class ListRandomizationExtensions
{
    // FisherYates shuffle is a uniformly random shuffle algorithm: any permutation is equally likely
    public static void FisherYatesShuffle<T>(this IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);                 // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // same thing, but start from the 0-index of the array instead of the other end.
    public static void ReverseFisherYatesShuffle<T>(this IList<T> list, Random rng)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = rng.Next(i, list.Count);         // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // same thing, but start from the 0-index of the array instead of the other end.
    public static void ReverseFisherYatesShuffleN<T>(this IList<T> list, int n, Random rng)
    {
        if (n > list.Count - 1 || n <= 0)
        {
            throw new Exception($"Invalid choice of n: {n}. Expected value in range [1, {list.Count - 1}]");
        }
        for (int i = 0; i < n; i++)
        {
            int j = rng.Next(i, list.Count);         // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // a generator that performs the FisherYates shuffle one step at a time.
    public static IEnumerable<(int, T)> LazyFisherYatesShuffle<T>(this IList<T> list, Random rng)
    {
        // the last value is implicitly chosen (i > 0)
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);                 // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return (i, list[i]);               // yield the current index and its finalized replacement
        }
        yield return (0, list[0]);                   // yield the final index and its finalized replacement
    }

    // a generator that performs the FisherYates shuffle one step at a time, in reverse.
    public static IEnumerable<(int, T)> LazyReverseFisherYatesShuffle<T>(this IList<T> list, Random rng)
    {
        // the last value is implicitly chosen (i < list.Count - 1)
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = rng.Next(i, list.Count);         // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return (i, list[i]);               // yield the current index and its finalized replacement
        }
        yield return (list.Count - 1, list[list.Count - 1]); // yield the final index and its finalized replacement
    }
}

public static class ArrayRandomizationExtensions
{
    // FisherYates shuffle is a uniformly random shuffle algorithm: any permutation is equally likely
    public static void FisherYatesShuffle(this int[] list, Random rng)
    {
        for (int i = list.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);                 // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // same thing, but start from the 0-index of the array instead of the other end.
    public static void ReverseFisherYatesShuffle(this int[] list, Random rng)
    {
        for (int i = 0; i < list.Length - 1; i++)
        {
            int j = rng.Next(i, list.Length);         // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // same thing, but start from the 0-index of the array instead of the other end.
    public static void ReverseFisherYatesShuffleN(this int[] list, int n, Random rng)
    {
        if (n > list.Length - 1 || n <= 0)
        {
            throw new Exception($"Invalid choice of n: {n}. Expected value in range [1, {list.Length -1}]");
        }
        for (int i = 0; i < n; i++)
        {
            int j = rng.Next(i, list.Length);         // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
        }
    }

    // a generator that performs the FisherYates shuffle one step at a time.
    public static IEnumerable<(int, int)> LazyFisherYatesShuffleWithIndex(this int[] list, Random rng)
    {
        // the last value is implicitly chosen (i > 0)
        for (int i = list.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);                 // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return (i, list[i]);               // yield the current index and its finalized replacement
        }
        yield return (0, list[0]);                   // yield the final index and its finalized replacement
    }

    // a generator that performs the FisherYates shuffle one step at a time, in reverse.
    public static IEnumerable<(int, int)> LazyReverseFisherYatesShuffleWithIndex(this int[] list, Random rng)
    {
        // the last value is implicitly chosen (i < list.Count - 1)
        for (int i = 0; i < list.Length - 1; i++)
        {
            int j = rng.Next(i, list.Length);        // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return (i, list[i]);               // yield the current index and its finalized replacement
        }
        yield return (list.Length - 1, list[list.Length - 1]); // yield the final index and its finalized replacement
    }

    // a generator that performs the FisherYates shuffle one step at a time.
    public static IEnumerable<int> LazyFisherYatesShuffle(this int[] list, Random rng)
    {
        // the last value is implicitly chosen (i > 0)
        for (int i = list.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);                 // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return list[i];                    // yield the current replacement
        }
        yield return list[0];                        // yield the final replacement
    }

    // a generator that performs the FisherYates shuffle one step at a time, in reverse.
    public static IEnumerable<int> LazyReverseFisherYatesShuffle(this int[] list, Random rng)
    {
        // the last value is implicitly chosen (i < list.Count - 1)
        for (int i = 0; i < list.Length - 1; i++)
        {
            int j = rng.Next(i, list.Length);        // choose a random index to replace the value at the current position of the array
            (list[i], list[j]) = (list[j], list[i]); // swap the values
            yield return list[i];                    // yield the current replacement
        }
        yield return list[list.Length - 1];          // yield the final replacement
    }
}
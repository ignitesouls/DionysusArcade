using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UniversalReplacementRandomizer;

public class OptimizedRandomizationGroup
{
    public readonly int M;
    public readonly int N;
    private readonly IReplacementValidator? Validator;

    public OptimizedRandomizationGroup(int m, int n, IReplacementValidator? validator = null)
    {
        if (m <= 0 || n <= 0)
        {
            throw new ArgumentException("M and N must be positive.");
        }

        M = m;
        N = n;
        Validator = validator;
    }

    public int[] Randomize(Random rng)
    {
        int[] result = GenerateDistribution(rng);

        int n = M < N ? M : M - 1;
        result.ReverseFisherYatesShuffleN(n, rng);

        return result.Length > M ? result[..M] : result;
    }

    public int[] RetryingValidatedRandomize(Random rng, int? maxAttempts = 100)
    {
        if (Validator == null)
        {
            throw new Exception("ReplacementValidator not implemented!");
        }
        
        int[] cache = Array.Empty<int>();
        // The duplicates will be chosen once, when this result array is initialized.
        // Otherwise, bias could be introduced to the randomness.
        int[] result = GenerateDistribution(rng);

        // track attempts
        int iterations = 0;
    Retry:
        if (iterations++ < maxAttempts)
        {
            if (cache.Length == 0)
            {
                // Initialize cache from unrandomized result array
                cache = new int[result.Length];
                Array.Copy(result, cache, result.Length);
            }
            else
            {
                // Reset result array
                Array.Copy(cache, result, result.Length);
            }

            // Now run the shuffle step-wise, validating after each step.
            foreach ((int i, int replacementIndex) in result.LazyReverseFisherYatesShuffleWithIndex(rng))
            {
                if (i >= M) // finished generating replacements. they were all valid.
                {
                    return result[..M];
                }
                if (!Validator.Validate(i, replacementIndex))
                {
                    goto Retry;
                }
            }

            return result;
        }
        else
        {
            throw new Exception($"Randomization failed to generate a valid result after {maxAttempts} attempts.");
        }
    }

    private int[] GenerateDistribution(Random rng)
    {
        int[] result;
        // First, fill the result array
        if (M <= N)
        {
            result = new int[N];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = i;
            }
        }
        else // M > N
        {
            result = new int[M];

            int fullCycles = M / N;
            int remainder = M % N;

            // Fill evenly
            for (int cycle = 0; cycle < fullCycles; cycle++)
            {
                for (int i = 0; i < N; i++)
                {
                    result[cycle * N + i] = i;
                }
            }

            // Fill the remaining M % N values with unique, random choices
            int[] choices = new int[N];
            for (int i = 0; i < N; i++)
            {
                choices[i] = i;
            }
            using IEnumerator<int> enumerator = choices.LazyFisherYatesShuffle(rng).GetEnumerator();
            for (int i = 0; i < remainder; i++)
            {
                enumerator.MoveNext();
                result[fullCycles * N + i] = enumerator.Current;
            }
        }

        return result;
    }
}

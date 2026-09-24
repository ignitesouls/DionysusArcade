using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalReplacementRandomizer;

public class RandomizationGroup
{
    private List<int> Targets;      // integers to be replaced
    private List<int> Replacements; // integers that can potentially replace any location in the targets array

    private readonly IReplacementValidator? ReplacementValidator;

    public List<int> GetTargets() { return Targets; }
    public List<int> GetReplacements() { return Replacements; }

    public RandomizationGroup(List<int> targets, List<int> replacements, IReplacementValidator? replacementValidator = null)
    {
        Targets = targets;
        Replacements = replacements;
        ReplacementValidator = replacementValidator;
    }

    public Dictionary<int, int> Randomize(Random rng)
    {
        // FY shuffles arrays in-place. copy the replacements before randomizing.
        List<int> replacements = GenerateDistribution(rng);

        int n = Targets.Count < Replacements.Count ? Targets.Count : Targets.Count - 1;
        replacements.ReverseFisherYatesShuffleN(n, rng);

        // format results as a dictionary mapping target => replacement
        Dictionary<int, int> targetsToReplacements = new();
        for (int i = 0; i < Targets.Count; i++)
        {
            targetsToReplacements[Targets[i]] = replacements[i % replacements.Count];
        }
        return targetsToReplacements;
    }

    public Dictionary<int, int> ValidatedRandomize(Random rng)
    {
        if (ReplacementValidator == null)
        {
            throw new Exception("ReplacementValidator not implemented!");
        }

        // FY shuffles arrays in-place. copy the replacements before randomizing.
        List<int> replacements = GenerateDistribution(rng);

        // iterate through the replacements, validating after each finalized replacement.
        foreach ((int i, int replacement) in replacements.LazyReverseFisherYatesShuffle(rng))
        {
            if (i >= Targets.Count)
            {
                break; // finished generating the replacements. they were all valid.
            }
            if (!ReplacementValidator.Validate(Targets[i], replacement))
            {
                throw new Exception("Randomization failed to generate a valid result.");
            }
        }

        // format results as a dictionary mapping target => replacement
        Dictionary<int, int> targetsToReplacements = new Dictionary<int, int>();
        for (int i = 0; i < Targets.Count; i++)
        {
            targetsToReplacements[Targets[i]] = replacements[i % replacements.Count];
        }
        return targetsToReplacements;
    }

    public Dictionary<int, int> RetryingValidatedRandomize(Random rng, int? maxAttempts = 100)
    {
        if (ReplacementValidator == null)
        {
            throw new Exception("ReplacementValidator not implemented!");
        }

        // track attempts
        int iterations = 0;
        List<int> cache = new();
        List<int> replacements = GenerateDistribution(rng);

    Retry:
        if (iterations++ < maxAttempts)
        {
            if (cache.Count == 0)
            {
                // Initialize cache from unrandomized replacements array
                cache = new(replacements);
            }
            else
            {
                // Reset result array
                replacements = new(cache);
            }

            foreach ((int i, int replacement) in replacements.LazyReverseFisherYatesShuffle(rng))
            {
                if (i >= Targets.Count)
                { 
                    break;      // finished generating the replacements. they were all valid.
                }
                if (!ReplacementValidator.Validate(Targets[i], replacement))
                { 
                    goto Retry; // invalid replacement encountered, go to next loop
                }
            }

            // format results as a dictionary mapping target => replacement
            Dictionary<int, int> targetsToReplacements = new Dictionary<int, int>();
            for (int i = 0; i < Targets.Count; i++)
            {
                targetsToReplacements[Targets[i]] = replacements[i % replacements.Count];
            }
            return targetsToReplacements;
        }
        else
        {
            throw new Exception($"Randomization failed to generate a valid result after {maxAttempts} attempts.");
        }
    }

    private List<int> GenerateDistribution(Random rng)
    {
        List<int> result;
        // First, fill the result array
        if (Targets.Count <= Replacements.Count)
        {
            result = new(Replacements);
        }
        else // M > N
        {
            int fullCycles = Targets.Count / Replacements.Count;
            int remainder = Targets.Count % Replacements.Count;

            // Fill evenly
            result = new();
            for (int cycle = 0; cycle < fullCycles; cycle++)
            {
                result = result.Concat(Replacements).ToList();
            }

            // Fill the remaining M % N values with unique, random choices
            List<int> choices = new(Replacements);
            using IEnumerator<(int, int)> enumerator = choices.LazyFisherYatesShuffle(rng).GetEnumerator();
            for (int i = 0; i < remainder; i++)
            {
                enumerator.MoveNext();
                (int ri, int replacement) = enumerator.Current;
                result.Add(replacement);
            }
        }
        return result;
    }
}

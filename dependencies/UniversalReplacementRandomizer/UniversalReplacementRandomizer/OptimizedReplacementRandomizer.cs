using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalReplacementRandomizer;

public class OptimizedReplacementRandomizer
{
    // managed deterministic randomness. namespaced random number generators. don't leave home without it
    private readonly SeedManager SeedManager;

    // SeedManager key => OptimizedRandomizationGroup
    private readonly Dictionary<string, OptimizedRandomizationGroup> Groups;

    // necessary for deterministic randomization
    public int GetBaseSeed() { return SeedManager.GetBaseSeed(); }

    // caveat: could influence randomization if used outside of the OptimizedReplacementRandomizer
    public SeedManager GetSeedManager() { return SeedManager; }

    public OptimizedReplacementRandomizer(string? prefix = null, int? baseSeed = null)
    {
        Groups = new Dictionary<string, OptimizedRandomizationGroup>();
        SeedManager = new SeedManager(prefix, baseSeed);
    }

    public Dictionary<string, int[]> RandomizeAllGroups()
    {
        // shuffle every group in the Groups dictionary by iterating through all keys present
        List<string> keys = Groups.Keys.ToList();

        // format results as a dictionary of dictionaries mapping key => replacementsDictionary
        Dictionary<string, int[]> result = new();

        foreach (string key in keys)
        {
            Random rng = SeedManager.GetRandomByKey(key);
            result[key] = Groups[key].Randomize(rng);
        }

        return result;
    }

    public int[] RandomizeGroup(string key)
    {
        Random rng = SeedManager.GetRandomByKey(key);
        if (!Groups.TryGetValue(key, out OptimizedRandomizationGroup? group))
        {
            throw new Exception("Unrecognized key");
        }

        return group.Randomize(rng);
    }

    public int[] RetryingRandomizeGroup(string key, int? maxAttempts = null)
    {
        Random rng = SeedManager.GetRandomByKey(key);
        if (!Groups.TryGetValue(key, out OptimizedRandomizationGroup? group))
        {
            throw new Exception("Unrecognized key");
        }

        return group.RetryingValidatedRandomize(rng, maxAttempts);
    }

    public void AddGroup(string key, OptimizedRandomizationGroup group)
    {
        // add a new randomization group
        if (Groups.ContainsKey(key))
        {
            throw new Exception($"Dictionary {nameof(Groups)} already contains key: {key}");
        }

        Groups.Add(key, group);
    }
}

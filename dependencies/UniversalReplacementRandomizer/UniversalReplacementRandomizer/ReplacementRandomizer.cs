// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;

namespace UniversalReplacementRandomizer;

public class ReplacementRandomizer
{
    private readonly SeedManager SeedManager;
    private readonly Dictionary<string, RandomizationGroup> Groups; // SeedManager key => RandomizationGroup

    public int GetBaseSeed() { return SeedManager.GetBaseSeed(); } // necessary for deterministic randomization
    public SeedManager GetSeedManager() { return SeedManager; }

    public ReplacementRandomizer(string? prefix = null, int? baseSeed = null)
    {
        Groups = new Dictionary<string, RandomizationGroup>();
        SeedManager = new SeedManager(prefix,baseSeed);
    }

    public Dictionary<string, Dictionary<int, int>> RandomizeAllGroups()
    {
        // shuffle every group in the Groups dictionary by iterating through all keys present
        List<string> keys = Groups.Keys.ToList();

        // format results as a dictionary of dictionaries mapping key => replacementsDictionary
        Dictionary<string, Dictionary<int, int>> result = new();

        foreach (string key in keys)
        {
            Random rng = SeedManager.GetRandomByKey(key);  // retrieve the namespaced randomness generator
            result[key] = Groups[key].Randomize(rng); // direct access by key, since it's generated from the dictionary
        }

        return result;
    }

    public Dictionary<int, int> RandomizeGroup(string key)
    {
        Random rng = SeedManager.GetRandomByKey(key);
        if (!Groups.TryGetValue(key, out RandomizationGroup? group))
        {
            throw new Exception("Unrecognized key");
        }
        return group.Randomize(rng);
    }

    public Dictionary<int, int> RetryingRandomizeGroup(string key, int? maxAttempts = null)
    {
        Random rng = SeedManager.GetRandomByKey(key);
        if (!Groups.TryGetValue(key, out RandomizationGroup? group))
        {
            throw new Exception("Unrecognized key");
        }

        return group.RetryingValidatedRandomize(rng, maxAttempts);
    }

    public void AddGroup(string key, RandomizationGroup group)
    {
        // add a new randomization group
        if (Groups.ContainsKey(key))
        {
            throw new Exception($"Dictionary {nameof(Groups)} already contains key: {key}");
        }
        Groups.Add(key, group);
    }
}

public class EncodedBitmapValidator : IReplacementValidator
{
    private readonly Dictionary<int, int> TargetEncodedBitmaps;
    private readonly Dictionary<int, int> ReplacementEncodedBitmaps;

    public Dictionary<string, Func<int, int, bool>>? ValidatorsByKey { get; set; }

    public EncodedBitmapValidator(Dictionary<int, int> targetEncodedBitmaps, Dictionary<int, int> replacementEncodedBitmaps)
    {
        TargetEncodedBitmaps = targetEncodedBitmaps;
        ReplacementEncodedBitmaps = replacementEncodedBitmaps;
    }

    public bool Validate(int target, int replacement)
    {
        if (!TargetEncodedBitmaps.TryGetValue(target, out int targetEncodedBitmap))
            return true;                                              // the target doesn't have any conflicts
        if (!ReplacementEncodedBitmaps.TryGetValue(replacement, out int replacementEncodedBitmap))
            return true;                                              // the replacement doesn't have any conflicts
        return (targetEncodedBitmap & replacementEncodedBitmap) == 0; // we have to check for conflicts between the two
    }
}

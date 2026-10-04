// SPDX-License-Identifier: GPL-3.0-only
// Based on the Athena Randomizer's implementation (https://github.com/ignitesouls/AthenaRandomizer).
using Dionysus.Core.Data;
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

public sealed class StartingGracesModule : IModule
{
    public string Id => "graces";
    public string DisplayName => "Starting Graces";
    public string Category => "Graces";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("graces.enabled", "Randomize starting graces", false,
            "Unlocks a random grace from each regional pool at the start of the game.")
    };

    public bool IsActive(OptionSet options) => options.GetBool("graces.enabled");

    public void Apply(BuildContext context)
    {
        Dictionary<string, List<GracePoolModel>> pools =
            CsvReaderUtils.Read<GracePoolModel>(context.Resource("Data", "StartingGraces", "grace_pools.csv"))
                .GroupBy(g => g.GraceRegion)
                .ToDictionary(g => g.Key, g => g.ToList());

        List<string> selected = new();

        // Limgrave: one grace from Limgrave1 and one from Limgrave2
        {
            Random rng = context.Rng("grace_Limgrave");
            PickPair(context, rng, pools["Limgrave1"], pools["Limgrave2"], selected);
        }

        // Liurnia: a pair from North + South, or from East + West
        {
            Random rng = context.Rng("grace_Liurnia");
            bool useNorthSouth = rng.Next(2) == 0;
            PickPair(context, rng,
                useNorthSouth ? pools["Liurnia_North"] : pools["Liurnia_East"],
                useNorthSouth ? pools["Liurnia_South"] : pools["Liurnia_West"],
                selected);
        }

        // Every other pool: one grace each
        foreach (var (poolName, pool) in pools)
        {
            if (poolName is "Limgrave1" or "Limgrave2" || poolName.StartsWith("Liurnia_")) continue;

            Random rng = context.Rng($"grace_{poolName}");
            GracePoolModel grace = pool[rng.Next(pool.Count)];
            context.Params.SetGraceEventFlagId(grace.ID);
            selected.Add(grace.GraceName);
        }

        context.Report["Starting Graces"] = selected;
    }

    // Picks one grace from each pool, making sure the two aren't the same grace.
    private static void PickPair(BuildContext context, Random rng,
        List<GracePoolModel> firstPool, List<GracePoolModel> secondPool, List<string> selected)
    {
        GracePoolModel first = firstPool[rng.Next(firstPool.Count)];
        GracePoolModel second;
        int guard = 0;
        do
        {
            second = secondPool[rng.Next(secondPool.Count)];
            guard++;
        }
        while (second.ID == first.ID && guard < 100);

        context.Params.SetGraceEventFlagId(first.ID);
        context.Params.SetGraceEventFlagId(second.ID);
        selected.Add(first.GraceName);
        selected.Add(second.GraceName);
    }
}
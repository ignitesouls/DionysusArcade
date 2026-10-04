// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Modules;

namespace Dionysus.Core.Pipeline;

public static class ModuleRegistry
{
    // Modules run in this order
    public static IReadOnlyList<IModule> All { get; } = new List<IModule>
    {
        new PatchModule(),
        new StartingGracesModule(),
        new StartingClassesModule(),
        new ItemShuffleModule(),
        new TweaksModule(),
        new TalismanShopModule(),
        new PackModule(),
        new ModEngineModule(),
        new RandomizerModule(),
    };
}
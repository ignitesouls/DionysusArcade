// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Modules;

namespace Dionysus.Core.Pipeline;

public static class ModuleRegistry
{
    // Modules run in this order. For parity with the old app, keep its order:
    // graces, starting classes, item shuffle, tweaks, talisman shop, then packs.
    public static IReadOnlyList<IModule> All { get; } = new List<IModule>
    {
        new StartingGracesModule(),
        // Starting Classes goes here
        // Item Shuffle goes here
        new TweaksModule(),
        // Talisman Shop goes here
        new IncursionPackModule(),
    };
}
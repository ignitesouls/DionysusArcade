// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Adds folders from Resources/Packs to the game. Each option switches on one pack.
public sealed class PackModule : IModule
{
    public string Id => "packs";
    public string DisplayName => "Packs";
    public string Category => "Packs";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("packs.incursion", "Incursion", false,
            "Battleship's Incursion event and map files."),
        new BoolOption("packs.cluedo", "Cluedo", false,
            "Cluedo's event and map files."),
    };

    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    public void Apply(BuildContext context) { }

    public IEnumerable<string> Packs(OptionSet options)
    {
        if (options.GetBool("packs.incursion")) yield return "Incursion";
        if (options.GetBool("packs.cluedo")) yield return "Cluedo";
    }
}
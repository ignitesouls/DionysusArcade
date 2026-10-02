// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Applies patch files from Resources/ParamPatches and Resources/TextPatches.
// Each option switches on one set of patches, which can include both kinds.
public sealed class PatchModule : IModule
{
    public string Id => "patches";
    public string DisplayName => "Patches";
    public string Category => "Patches";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("patches.battleship_base", "Battleship base changes", false,
            "The param and text changes that used to be baked into the Battleship regulation and menu files.")
    };

    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    public void Apply(BuildContext context) { }

    public IEnumerable<string> ParamPatches(OptionSet options)
    {
        if (options.GetBool("patches.battleship_base"))
        {
            yield return "battleship_base.csv";
        }
    }

    public IEnumerable<string> TextPatches(OptionSet options)
    {
        if (options.GetBool("patches.battleship_base"))
        {
            yield return "battleship_base.csv";
        }
    }
}
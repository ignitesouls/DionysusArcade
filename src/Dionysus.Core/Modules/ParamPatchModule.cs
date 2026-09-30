// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Applies param patch files from Resources/ParamPatches
public sealed class ParamPatchModule : IModule
{
    public string Id => "param_patches";
    public string DisplayName => "Param Patches";
    public string Category => "Patches";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("param_patches.battleship_base", "Battleship base changes", true,
            "Applies battleship_base.csv: the changes that used to be baked into the Battleship regulation.")
    };

    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    public void Apply(BuildContext context) { }

    public IEnumerable<string> ParamPatches(OptionSet options)
    {
        if (options.GetBool("param_patches.battleship_base"))
        {
            yield return "battleship_base.csv";
        }
    }
}
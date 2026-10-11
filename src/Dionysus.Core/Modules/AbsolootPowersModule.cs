// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Absoloot Powers: replaces common.emevd.dcx with the version in Resources/Packs/AbsolootPowers.
// Its pack is added after every other pack, so its file wins. With the option off, the mode's own file
// is used again, because the package is rebuilt from scratch on every Assemble.
public sealed class AbsolootPowersModule : IModule
{
    public string Id => "absoloot";
    public string DisplayName => "Absoloot Powers";
    public string Category => "Packs";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("absoloot.enabled", "Absoloot Powers", false,
            "Replaces the game's common event file (common.emevd.dcx) with the Absoloot Powers version."),
    };

    public bool IsActive(OptionSet options) => options.GetBool("absoloot.enabled");

    public void Apply(BuildContext context) { }

    public IEnumerable<string> Packs(OptionSet options) => new[] { "AbsolootPowers" };
}
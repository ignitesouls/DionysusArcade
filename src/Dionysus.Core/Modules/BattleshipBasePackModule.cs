// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// TEMPORARY: the hand-edited Battleship regulation and menu text, used in place of vanilla.
// Delete this module and its pack once all of its edits exist as patch data.
public sealed class BattleshipBasePackModule : IModule
{
    public string Id => "battleship_base";
    public string DisplayName => "Battleship Base Files (temporary)";
    public string Category => "Packs";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("battleship_base.enabled", "Use the pre-edited Battleship files", true,
            "Uses the hand-edited Battleship regulation and menu text instead of vanilla.")
    };

    public bool IsActive(OptionSet options) => options.GetBool("battleship_base.enabled");

    public void Apply(BuildContext context) { }

    public IEnumerable<string> Packs(OptionSet options) => new[] { "BattleshipBase" };
}
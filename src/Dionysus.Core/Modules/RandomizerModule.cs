// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;
using Dionysus.Core.Rando;

namespace Dionysus.Core.Modules;

// Prepares a run of Matt's Item and Enemy Randomizer: the chosen template, with boss arenas
// shuffled by the Boss Arena Randomizer (BAR). The game is then launched from Matt's randomizer.
public sealed class RandomizerModule : IModule
{
    public string Id => "randomizer";
    public string DisplayName => "Item and Enemy Randomizer";
    public string Category => "Randomizer";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("randomizer.enabled", "Play with Matt's Item and Enemy Randomizer", false,
            "Creates an options file for Matt's randomizer with boss arenas shuffled by the Boss Arena Randomizer. The game is launched from Matt's randomizer instead of Dionysus."),
        new ChoiceOption("randomizer.template", "Randomizer template", "Battleship_Boss_Shuffle_Mode",
            new[] { "Battleship_Boss_Shuffle_Mode" },
            "An options file made in Matt's randomizer.", RandomizerTemplates.Names),
        new ChoiceOption("randomizer.bar_config", "BAR configuration", "Battleship_Boss_Shuffle",
            new[] { "Battleship_Boss_Shuffle" },
            "Which arenas and bosses the Boss Arena Randomizer uses, and how they can be paired."),
    };

    // Pack files that break Matt's randomizer, so they're left out of randomizer builds
    private static readonly string[] FilesThatBreakTheRandomizer =
    {
        "map/mapstudio/m60_50_56_00.msb.dcx",
        "event/m14_00_00_00.emevd.dcx",
    };

    public IEnumerable<string> ExcludedFiles(OptionSet options) => FilesThatBreakTheRandomizer;

    public bool IsActive(OptionSet options) => options.GetBool("randomizer.enabled");

    public void Apply(BuildContext context)
    {
        string templateName = context.Options.GetChoice("randomizer.template");
        string templatePath = RandomizerTemplates.PathFor(templateName)
            ?? throw new FileNotFoundException($"Randomizer template '{templateName}' wasn't found.");

        // Only the latest output is kept
        string outputFolder = context.Resource("RandomizerOutput");
        if (Directory.Exists(outputFolder))
        {
            foreach (string old in Directory.GetFiles(outputFolder))
            {
                File.Delete(old);
            }
        }

        // BAR's seed comes from the Dionysus seed, through its own random stream
        int barSeed = context.Rng("bar").Next(1, int.MaxValue);
        string output = BarRunner.Run(context.ResourcesRoot,
            context.Options.GetChoice("randomizer.bar_config"), templatePath, outputFolder, barSeed);

        context.Outputs["randomizeopt"] = output;
        context.Report["Boss Arena Randomizer"] = new List<string> { $"BAR seed {barSeed}" };
    }
}
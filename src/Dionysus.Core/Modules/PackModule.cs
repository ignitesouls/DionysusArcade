// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Adds folders from Resources/Packs to the game. Each option switches on one pack.
public sealed class PackModule : IModule
{
    public string Id => "packs";
    public string DisplayName => "Packs";
    public string Category => "Packs";

    // Not offered in the Custom tab for now; its values come from the chosen mode
    public bool ShowInCustom => false;

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("packs.incursion", "Incursion", false,
            "Battleship's Incursion event and map files."),
        new BoolOption("packs.cluedo", "Cluedo", false,
                        "Cluedo's event and map files, its overlay and RandomizerHelper."),
        new BoolOption("packs.leonine_locked", "Leonine Locked", false,
            "Legacy Leonine Locked's game files."),
        new BoolOption("packs.stormvault", "Stormvault", false,
            "Legacy Stormvault's game files."),
        new BoolOption("packs.academy_keyed", "Academy Keyed", false,
            "Legacy Academy Keyed's game files."),
    };

    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    // Option keys of the Legacy formats
    private static readonly string[] LegacyPacks = { "packs.leonine_locked", "packs.stormvault", "packs.academy_keyed" };

    public void Apply(BuildContext context)
    {
        // Legacy builds point players to the community and the how-to-play guide.
        // Outputs starting with "link:" are shown as clickable links after the build.
        if (LegacyPacks.Any(key => context.Options.GetBool(key)))
        {
            context.Outputs["link:Ignite Discord"] = "https://discord.gg/ignitesouls";
            context.Outputs["link:How to play (Discord forum post)"] =
                "https://discord.com/channels/1365789384628244611/1504264233402241055";
        }
    }

    public IEnumerable<string> Packs(OptionSet options)
    {
        if (options.GetBool("packs.incursion")) yield return "Incursion";
        if (options.GetBool("packs.cluedo")) yield return "Cluedo";
        if (options.GetBool("packs.leonine_locked")) yield return "LeonineLocked";
        if (options.GetBool("packs.stormvault")) yield return "Stormvault";
        if (options.GetBool("packs.academy_keyed")) yield return "AcademyKeyed";
    }

    // Native DLLs, relative to the Mod Engine 3 folder, that come with each pack
    public IEnumerable<string> Natives(OptionSet options)
    {
        if (options.GetBool("packs.cluedo"))
        {
            yield return "CluedoOverlay/er_overlay.dll";
            yield return "RandomizerHelper/RandomizerHelper.dll";
        }
    }
}
// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

public sealed class IncursionPackModule : IModule
{
    public string Id => "incursion";
    public string DisplayName => "Incursion Pack";
    public string Category => "Battleship";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("incursion.enabled", "Include Incursion files", true,
            "Copies the prebuilt Incursion map and event files into the mod package.")
    };

    public bool IsActive(OptionSet options) => options.GetBool("incursion.enabled");

    public void Apply(BuildContext context) =>
        context.ResourcePacks.Add(context.Resource("ModeFolders", "full_incursion"));
}
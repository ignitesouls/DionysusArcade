// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

// Native DLLs loaded through Mod Engine 3
public sealed class ModEngineModule : IModule
{
    public string Id => "me3";
    public string DisplayName => "Mod Engine";
    public string Category => "Setup";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("me3.stutter_fix", "StutterFix", true,
            "Loads StutterFix (DINPUT8.DLL) through Mod Engine 3."),
        new BoolOption("me3.crash_fix", "RandomizerCrashFix", true,
            "Loads RandomizerCrashFix.dll through Mod Engine 3."),
        new BoolOption("me3.overlay", "EROverlay", true,
            "Loads er_overlay.dll through Mod Engine 3."),
    };

    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    public void Apply(BuildContext context) { }

    public IEnumerable<string> Natives(OptionSet options)
    {
        if (options.GetBool("me3.stutter_fix")) yield return "StutterFix/DINPUT8.DLL";
        if (options.GetBool("me3.crash_fix")) yield return "CrashFix/RandomizerCrashFix.dll";
        if (options.GetBool("me3.overlay")) yield return "EROverlay/er_overlay.dll";
    }
}
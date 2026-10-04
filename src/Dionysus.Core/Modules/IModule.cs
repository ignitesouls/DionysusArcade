// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;

namespace Dionysus.Core.Modules;

public interface IModule
{
    string Id { get; }
    string DisplayName { get; }
    string Category { get; }
    IReadOnlyList<OptionDefinition> Options { get; }
    bool IsActive(OptionSet options);
    void Apply(BuildContext context);

    // Folders under Resources/Packs that this module adds to the game while active.
    // Most modules add none, so this has a default and they don't need to mention it.
    IEnumerable<string> Packs(OptionSet options) => Array.Empty<string>();


    // Patch files under Resources/ParamPatches that this module applies while active.
    IEnumerable<string> ParamPatches(OptionSet options) => Array.Empty<string>();


    // Patch files under Resources/TextPatches that this module applies while active.
    IEnumerable<string> TextPatches(OptionSet options) => Array.Empty<string>();


    // Native DLLs, relative to the Mod Engine 3 folder, that this module loads while active.
    IEnumerable<string> Natives(OptionSet options) => Array.Empty<string>();


    // Game paths (e.g. "map/mapstudio/m60_50_56_00.msb.dcx") that must not be copied from any pack
    // while this module is active, for example files that break another tool.
    IEnumerable<string> ExcludedFiles(OptionSet options) => Array.Empty<string>();
}
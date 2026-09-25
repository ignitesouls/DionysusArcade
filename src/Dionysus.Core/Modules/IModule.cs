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
}
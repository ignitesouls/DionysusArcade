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
}
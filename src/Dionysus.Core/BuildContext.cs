// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using Dionysus.Core.Options;
using Dionysus.Core.Workspace;
using EldenRingParamsEditor;
using UniversalReplacementRandomizer;

namespace Dionysus.Core;

public sealed class BuildContext
{
    public required GameFiles Files { get; init; }
    public required OptimizedReplacementRandomizer Randomizer { get; init; }
    public required OptionSet Options { get; init; }
    public required string ResourcesRoot { get; init; }

    public Dictionary<string, List<string>> Report { get; } = new();

    // Shortcuts for the most used files. Each opens on first use.
    public ParamsEditor Params => Files.Params;
    public MenuBndEditorService MenuText => Files.MenuText;

    public Random Rng(string key) => Randomizer.GetSeedManager().GetRandomByKey(key);
    public string Resource(params string[] parts) => Path.Combine(ResourcesRoot, Path.Combine(parts));
}
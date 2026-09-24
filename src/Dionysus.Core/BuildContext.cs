// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using Dionysus.Core.Options;
using EldenRingParamsEditor;
using UniversalReplacementRandomizer;

namespace Dionysus.Core;

public sealed class BuildContext
{
    public required ParamsEditor Params { get; init; }
    public required MenuBndEditorService MenuText { get; init; }
    public required OptimizedReplacementRandomizer Randomizer { get; init; }
    public required OptionSet Options { get; init; }
    public required string ResourcesRoot { get; init; }

    public Dictionary<string, List<string>> Report { get; } = new();

    public Random Rng(string key) => Randomizer.GetSeedManager().GetRandomByKey(key);
    public string Resource(params string[] parts) => Path.Combine(ResourcesRoot, Path.Combine(parts));
}
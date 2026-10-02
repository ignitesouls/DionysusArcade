// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json.Serialization;

namespace Dionysus.Core.Presets;

// A named set of option values: a built-in mode, or a profile the player saved
public sealed class Preset
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order { get; set; } = 100;
    public Dictionary<string, object?> Options { get; set; } = new();

    // Not saved in the file; filled in when loading
    [JsonIgnore] public bool IsBuiltIn { get; set; }
    [JsonIgnore] public string FilePath { get; set; } = "";
    [JsonIgnore] public string DisplayName => IsBuiltIn ? Name : $"{Name} (profile)";
}
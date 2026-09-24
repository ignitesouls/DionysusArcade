// SPDX-License-Identifier: GPL-3.0-only
namespace Dionysus.Core.Options;

public sealed class OptionSet
{
    private readonly Dictionary<string, object> _values = new();

    public OptionSet(IEnumerable<OptionDefinition> definitions, IReadOnlyDictionary<string, object?> overrides)
    {
        foreach (OptionDefinition def in definitions)
        {
            overrides.TryGetValue(def.Key, out object? raw);
            _values[def.Key] = raw is null ? def.DefaultValue : def.Normalize(raw);
        }
    }

    public IReadOnlyDictionary<string, object> Values => _values;
    public bool GetBool(string key) => (bool)_values[key];
    public int GetInt(string key) => (int)_values[key];
    public string GetChoice(string key) => (string)_values[key];
}
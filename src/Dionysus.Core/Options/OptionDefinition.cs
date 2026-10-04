// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;

namespace Dionysus.Core.Options;

public abstract record OptionDefinition(string Key, string Label, string Description)
{
    public abstract object DefaultValue { get; }
    public abstract object Normalize(object? value);
}

public sealed record BoolOption(string Key, string Label, bool Default, string Description = "")
    : OptionDefinition(Key, Label, Description)
{
    public override object DefaultValue => Default;
    public override object Normalize(object? value) => value switch
    {
        bool b => b,
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.False } => false,
        _ => Default
    };
}

public sealed record IntOption(string Key, string Label, int Default, int Min, int Max, string Description = "")
    : OptionDefinition(Key, Label, Description)
{
    public override object DefaultValue => Default;
    public override object Normalize(object? value) => value switch
    {
        int i => Math.Clamp(i, Min, Max),
        JsonElement { ValueKind: JsonValueKind.Number } e when e.TryGetInt32(out int j) => Math.Clamp(j, Min, Max),
        _ => Default
    };
}

public sealed record ChoiceOption(string Key, string Label, string Default, IReadOnlyList<string> Choices,
    string Description = "", Func<IReadOnlyList<string>>? ChoicesSource = null)
    : OptionDefinition(Key, Label, Description)
{
    // The choices right now: from ChoicesSource if there is one (e.g. files that can be added later),
    // otherwise the fixed list
    public IReadOnlyList<string> CurrentChoices => ChoicesSource?.Invoke() ?? Choices;

    public override object DefaultValue => Default;
    public override object Normalize(object? value)
    {
        IReadOnlyList<string> choices = CurrentChoices;
        return value switch
        {
            string s when choices.Contains(s) => s,
            JsonElement { ValueKind: JsonValueKind.String } e when choices.Contains(e.GetString()!) => e.GetString()!,
            _ => Default
        };
    }
}
// SPDX-License-Identifier: GPL-3.0-only
using System.IO.Compression;
using System.Text.Json;

namespace Dionysus.Core.Presets;

// Packs option values into a short string players can copy and paste, and unpacks them again
public static class OptionsString
{
    private const string Prefix = "DNY1:";

    public static string Encode(IReadOnlyDictionary<string, object?> options)
    {
        // Sorted keys, so the same options always produce the same string
        var sorted = options.OrderBy(kv => kv.Key, StringComparer.Ordinal).ToDictionary(kv => kv.Key, kv => kv.Value);
        byte[] json = JsonSerializer.SerializeToUtf8Bytes(sorted);

        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.SmallestSize))
        {
            deflate.Write(json);
        }
        return Prefix + Convert.ToBase64String(output.ToArray());
    }

    public static Dictionary<string, object?> Decode(string text)
    {
        text = text.Trim();
        if (!text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new FormatException("This isn't a Dionysus options string.");
        }

        byte[] compressed = Convert.FromBase64String(text[Prefix.Length..]);
        using var input = new DeflateStream(new MemoryStream(compressed), CompressionMode.Decompress);
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(input) ?? new();
    }
}
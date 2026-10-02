// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;

namespace Dionysus.Core.Presets;

// Loads built-in modes and the player's profiles, and saves and deletes profiles
public static class PresetStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    // Built-in modes ship in Resources/Modes and are read-only
    public static List<Preset> LoadBuiltIn(string resourcesRoot) =>
        Load(Path.Combine(resourcesRoot, "Modes"), builtIn: true);

    // Profiles belong to the player and live outside the app folder, so they survive updates
    public static List<Preset> LoadProfiles(string profilesFolder) =>
        Load(profilesFolder, builtIn: false);

    // Saves (or overwrites) a profile, storing every option's value so it never depends on defaults
    public static Preset SaveProfile(string profilesFolder, string name, IReadOnlyDictionary<string, object?> options)
    {
        Directory.CreateDirectory(profilesFolder);
        var preset = new Preset
        {
            Name = name,
            Options = options.ToDictionary(kv => kv.Key, kv => kv.Value),
            FilePath = Path.Combine(profilesFolder, SafeFileName(name) + ".json")
        };
        File.WriteAllText(preset.FilePath, JsonSerializer.Serialize(preset, Json));
        return preset;
    }

    public static void DeleteProfile(Preset profile)
    {
        if (!profile.IsBuiltIn && File.Exists(profile.FilePath))
        {
            File.Delete(profile.FilePath);
        }
    }

    // Renames a profile by saving it under the new name, then removing the old file
    public static Preset RenameProfile(string profilesFolder, Preset profile, string newName)
    {
        Preset renamed = SaveProfile(profilesFolder, newName, profile.Options);

        // Windows file names ignore capitals, so renaming "test" to "Test" is still the same file.
        // Deleting the "old" file in that case would delete the one just saved.
        if (!string.Equals(renamed.FilePath, profile.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            DeleteProfile(profile);
        }
        return renamed;
    }

    private static List<Preset> Load(string folder, bool builtIn)
    {
        var presets = new List<Preset>();
        if (!Directory.Exists(folder))
        {
            return presets;
        }

        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                Preset? preset = JsonSerializer.Deserialize<Preset>(File.ReadAllText(path), Json);
                if (preset == null) continue;
                preset.IsBuiltIn = builtIn;
                preset.FilePath = path;
                presets.Add(preset);
            }
            catch (JsonException)
            {
                // A damaged file shouldn't stop the others from loading
            }
        }

        return presets.OrderBy(p => p.Order).ThenBy(p => p.Name).ToList();
    }

    private static string SafeFileName(string name) =>
        string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
}
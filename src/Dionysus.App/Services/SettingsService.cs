// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Text.Json;

namespace Dionysus.App.Services;

public sealed class AppSettings
{
    public int SettingsVersion { get; set; } = 1;
    public int? LastBuiltSeed { get; set; }
    public Dictionary<string, object?> LastBuiltOptions { get; set; } = new();
}

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DionysusArcade");

    private static readonly string SettingsFile = Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile)) ?? new AppSettings();
            }
        }
        catch (JsonException)
        {
            // A damaged settings file shouldn't stop the app from starting
        }

        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }
}
// SPDX-License-Identifier: GPL-3.0-only
using System.IO;
using System.Text.RegularExpressions;
using Dionysus.Core.Pipeline;

namespace Dionysus.App.Services;

// Keeps the Battleship token in EROverlay's config file in sync with the app's settings
public static class OverlayConfigService
{
    private static string ConfigPath =>
        Path.Combine(AppPaths.ResourcesRoot, BuildPipeline.Me3Folder, "EROverlay", "overlay_config.toml");

    // Replaces only the token line in the [ingest] section; the rest of the file stays exactly as it is.
    // Returns false if the file or the token line wasn't found.
    public static bool WriteToken(string token)
    {
        if (!File.Exists(ConfigPath))
        {
            return false;
        }

        string[] lines = File.ReadAllLines(ConfigPath);
        string section = "";
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith('['))
            {
                section = trimmed;
            }
            else if (section == "[ingest]" && Regex.IsMatch(trimmed, @"^token\s*="))
            {
                // Backslashes and quotes must be escaped inside a TOML string
                string escaped = token.Replace("\\", "\\\\").Replace("\"", "\\\"");
                lines[i] = $"token = \"{escaped}\"";
                File.WriteAllLines(ConfigPath, lines);
                return true;
            }
        }
        return false;
    }
}
// SPDX-License-Identifier: GPL-3.0-only
namespace Dionysus.Core.Rando;

// Templates are options files made in Matt's randomizer. Built-in ones ship with Dionysus;
// players import their own into their data folder, so they survive updates.
public static class RandomizerTemplates
{
    public const string Extension = ".randomizeopt";

    // Set by the app at startup
    public static string BuiltInFolder { get; set; } = "";
    public static string UserFolder { get; set; } = "";

    public static IReadOnlyList<string> Names() =>
        new[] { BuiltInFolder, UserFolder }
            .Where(folder => folder.Length > 0 && Directory.Exists(folder))
            .SelectMany(folder => Directory.GetFiles(folder, "*" + Extension))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // A player's own template wins over a built-in one with the same name
    public static string? PathFor(string name) =>
        new[] { UserFolder, BuiltInFolder }
            .Where(folder => folder.Length > 0)
            .Select(folder => Path.Combine(folder, name + Extension))
            .FirstOrDefault(File.Exists);

    // Copies a template into the player's folder and returns its name
    public static string Import(string sourcePath)
    {
        Directory.CreateDirectory(UserFolder);
        string destination = Path.Combine(UserFolder, Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destination, overwrite: true);
        return Path.GetFileNameWithoutExtension(destination);
    }
}
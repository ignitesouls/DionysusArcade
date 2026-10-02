// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using Dionysus.Core.Modules;
using Dionysus.Core.Options;
using Dionysus.Core.Workspace;
using UniversalReplacementRandomizer;
using System.Text;

namespace Dionysus.Core.Pipeline;

public sealed record BuildResult(int Seed, Dictionary<string, List<string>> Report);

public static class BuildPipeline
{
    // The same seed with the same settings gives the same result within one app version.
    // Update this with each release.
    private const string SeedPrefix = "dionysus-v0.4";


    public const string Me3Folder = "me3-v0.8.0";
    public const string ProfileFile = "dionysus.me3";
    private const string PackageFolder = "package";

    public static BuildResult Run(string resourcesRoot, int? seed, IReadOnlyDictionary<string, object?> overrides)
    {
        string me3Dir = Path.Combine(resourcesRoot, Me3Folder);
        string packageDir = Path.Combine(me3Dir, PackageFolder);

        var options = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), overrides);
        List<IModule> active = ModuleRegistry.All.Where(m => m.IsActive(options)).ToList();

        // Packs requested by the active modules, in module order.
        // When two packs contain the same file, the later one wins.
        List<string> packs = active
            .SelectMany(m => m.Packs(options))
            .Select(name => Path.Combine(resourcesRoot, "Packs", name))
            .ToList();

        // Param patch files requested by the active modules, applied in module order
        List<string> paramPatches = active
            .SelectMany(m => m.ParamPatches(options))
            .Select(name => Path.Combine(resourcesRoot, "ParamPatches", name))
            .ToList();

        // Text patch files requested by the active modules, applied in module order
        List<string> textPatches = active
            .SelectMany(m => m.TextPatches(options))
            .Select(name => Path.Combine(resourcesRoot, "TextPatches", name))
            .ToList();

        // Native DLLs requested by the active modules
        List<string> natives = active.SelectMany(m => m.Natives(options)).Distinct().ToList();

        // File sources, highest priority first: packs (last one first), then the bundled vanilla files
        var sources = new List<string>();
        for (int i = packs.Count - 1; i >= 0; i--)
        {
            sources.Add(packs[i]);
        }
        sources.Add(Path.Combine(resourcesRoot, "Vanilla"));

        var context = new BuildContext
        {
            Files = new GameFiles(sources, paramPatches, textPatches, Path.Combine(resourcesRoot, "Defs")),
            Randomizer = new OptimizedReplacementRandomizer(SeedPrefix, seed),
            Options = options,
            ResourcesRoot = resourcesRoot
        };

        // Open the regulation now if there are patches, so they reach the package even when no module edits params
        if (paramPatches.Count > 0)
        {
            _ = context.Params;
        }

        if (textPatches.Count > 0)
        {
            _ = context.MenuText;
        }

        foreach (IModule module in active)
        {
            module.Apply(context);
        }

        // Output: clear the package, copy the packs, then write the edited files on top
        ClearFolder(packageDir);
        foreach (string pack in packs)
        {
            CopyFolder(pack, packageDir);
        }
        context.Files.WriteAll(packageDir);

        WriteMe3Profile(me3Dir, natives);

        return new BuildResult(context.Randomizer.GetBaseSeed(), context.Report);
    }

    // Writes the Mod Engine 3 profile for this build: the package folder plus the requested natives
    private static void WriteMe3Profile(string me3Dir, IReadOnlyList<string> natives)
    {
        var profile = new StringBuilder();
        profile.AppendLine("profileVersion = \"v1\"");
        profile.AppendLine();
        profile.AppendLine("[[supports]]");
        profile.AppendLine("game = \"eldenring\"");
        profile.AppendLine();
        profile.AppendLine("[[package]]");
        profile.AppendLine("id = \"dionysus\"");
        profile.AppendLine($"path = \"{PackageFolder}\"");

        foreach (string native in natives)
        {
            profile.AppendLine();
            profile.AppendLine("[[natives]]");
            profile.AppendLine($"path = '{native}'");
        }

        File.WriteAllText(Path.Combine(me3Dir, ProfileFile), profile.ToString());
    }

    private static void ClearFolder(string folder)
    {
        Directory.CreateDirectory(folder);

        foreach (string file in Directory.GetFiles(folder))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in Directory.GetDirectories(folder))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static void CopyFolder(string source, string target)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Resource pack not found: {source}");
        }

        foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }
}
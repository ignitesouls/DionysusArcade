// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Modules;
using Dionysus.Core.Options;
using Dionysus.Core.Workspace;
using UniversalReplacementRandomizer;

namespace Dionysus.Core.Pipeline;

public sealed record BuildResult(int Seed, Dictionary<string, List<string>> Report);

public static class BuildPipeline
{
    // The same seed with the same settings gives the same result within one app version.
    // Update this with each release.
    private const string SeedPrefix = "dionysus-v0.3";

    public static BuildResult Run(string resourcesRoot, int? seed, IReadOnlyDictionary<string, object?> overrides)
    {
        string packageDir = Path.Combine(resourcesRoot, "me3-v0.8.0", "basedlc");

        var options = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), overrides);
        List<IModule> active = ModuleRegistry.All.Where(m => m.IsActive(options)).ToList();

        // Packs requested by the active modules, in module order.
        // When two packs contain the same file, the later one wins.
        List<string> packs = active
            .SelectMany(m => m.Packs(options))
            .Select(name => Path.Combine(resourcesRoot, "Packs", name))
            .ToList();

        // File sources, highest priority first: packs (last one first), then the bundled vanilla files
        var sources = new List<string>();
        for (int i = packs.Count - 1; i >= 0; i--)
        {
            sources.Add(packs[i]);
        }
        sources.Add(Path.Combine(resourcesRoot, "Vanilla"));

        var context = new BuildContext
        {
            Files = new GameFiles(sources),
            Randomizer = new OptimizedReplacementRandomizer(SeedPrefix, seed),
            Options = options,
            ResourcesRoot = resourcesRoot
        };

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

        return new BuildResult(context.Randomizer.GetBaseSeed(), context.Report);
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
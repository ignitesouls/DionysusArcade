// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.FormatEditors;
using Dionysus.Core.Modules;
using Dionysus.Core.Options;
using EldenRingParamsEditor;
using UniversalReplacementRandomizer;

namespace Dionysus.Core.Pipeline;

public sealed record BuildResult(int Seed, Dictionary<string, List<string>> Report);

public static class BuildPipeline
{
    // Same prefix as the old app, so the same seed gives the same result.
    private const string SeedPrefix = "basedlcv0.2_incursion";

    public static BuildResult Run(string resourcesRoot, int? seed, IReadOnlyDictionary<string, object?> overrides)
    {
        // Battleship inputs for now. Templates will choose these later.
        string regulationIn = Path.Combine(resourcesRoot, "Regulation", "battleship", "regulation.bin");
        string menuIn = Path.Combine(resourcesRoot, "Bnd", "basedlc", "menu_dlc02.msgbnd.dcx");
        string packageDir = Path.Combine(resourcesRoot, "me3-v0.8.0", "basedlc");

        var context = new BuildContext
        {
            Params = ParamsEditor.ReadFromRegulationPath(regulationIn),
            MenuText = MenuBndEditorService.ReadFromMenuBndFilePath(menuIn),
            Randomizer = new OptimizedReplacementRandomizer(SeedPrefix, seed),
            Options = new OptionSet(ModuleRegistry.All.SelectMany(m => m.Options), overrides),
            ResourcesRoot = resourcesRoot
        };

        // Run every active module, in order
        foreach (IModule module in ModuleRegistry.All)
        {
            if (module.IsActive(context.Options))
            {
                module.Apply(context);
            }
        }

        // Output: clear the package, copy resource packs, then write the generated files on top
        ClearFolder(packageDir);
        foreach (string pack in context.ResourcePacks)
        {
            CopyFolder(pack, packageDir);
        }

        context.Params.WriteToRegulationPath(Path.Combine(packageDir, "regulation.bin"));
        context.MenuText.WriteToMenuBndFilePath(Path.Combine(packageDir, "msg", "engus", "menu_dlc02.msgbnd.dcx"));

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
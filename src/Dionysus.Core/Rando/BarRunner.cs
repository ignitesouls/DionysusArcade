// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using BossArenaRandomizer.Services;

namespace Dionysus.Core.Rando;

// Runs the Boss Arena Randomizer (BAR) with a BAR configuration shipped with Dionysus
public static class BarRunner
{
    // Returns the path of the new .randomizeopt
    public static string Run(string resourcesRoot, string configName, string templatePath, string outputFolder, int seed)
    {
        // BAR's Data folder sits next to the app, where BAR's own code also looks for it
        string barRoot = AppContext.BaseDirectory;

        // Dionysus's BAR configurations and presets
        string barFiles = Path.Combine(resourcesRoot, "BAR");

        PresetConfiguration config = JsonSerializer.Deserialize<PresetConfiguration>(
            File.ReadAllText(Path.Combine(barFiles, "Configurations", configName + ".json")))
            ?? throw new InvalidDataException($"BAR configuration '{configName}' is empty.");

        var (arenas, bosses) = new DataRepository(barRoot).LoadAllArenaBossDatabase();

        // Like BAR's own window, only use preset IDs that exist in the database
        var arenaIdsInDatabase = arenas.Values.Select(a => a.id).ToHashSet();
        var bossIdsInDatabase = bosses.Values.Select(b => b.id).ToHashSet();
        List<string> arenaIds = ReadIdList(Path.Combine(barFiles, "Presets", "Arenas", config.ArenaPreset))
            .Where(arenaIdsInDatabase.Contains).ToList();
        List<string> bossIds = ReadIdList(Path.Combine(barFiles, "Presets", "Bosses", config.BossPreset))
            .Where(bossIdsInDatabase.Contains).ToList();

        var service = new GenerationService(
            _ => new DionysusBarPaths(templatePath, Path.Combine(barRoot, "Data", "Pairings")),
            new PairingPresetFileLoader(),
            new RandomizeOptionsAssignmentWriter(),
            new UniformityReporter(),
            new GenerationDisplayBuilder());

        GenerationResult result = service.Generate(new GenerationRequest
        {
            Arenas = arenas,
            Bosses = bosses,
            SelectedArenaIds = arenaIds,
            SelectedBossIds = bossIds,
            BasePath = barRoot,
            OutputFolderPath = outputFolder,
            SelectedOptionsPreset = Path.GetFileNameWithoutExtension(templatePath),
            SelectedPairingPreset = config.PairingPreset,
            ClearArenasEnabled = config.ClearArenasEnabled ?? false,
            ClearArenaReplacementId = config.ClearArenaReplacementId,
            SeedCount = 1,
            ReplaySeed = seed
        });

        BatchSeedResult? batch = result.BatchResults.FirstOrDefault(b => b.Success);
        if (!result.Success || batch == null)
        {
            string details = string.Join(" ", result.ValidationLines.Take(3));
            throw new InvalidOperationException($"The Boss Arena Randomizer failed: {result.ErrorMessage} {details}".Trim());
        }
        return batch.OutputPath;
    }

    private static List<string> ReadIdList(string path) =>
        JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? new List<string>();

    // Tells BAR where Dionysus keeps the template, the pairing presets and the output
    private sealed class DionysusBarPaths : IProjectPaths
    {
        private readonly string _templatePath;
        private readonly string _pairingFolder;

        public DionysusBarPaths(string templatePath, string pairingFolder)
        {
            _templatePath = templatePath;
            _pairingFolder = pairingFolder;
        }

        public string OptionsPresetPath(string presetName) => _templatePath;

        public string PairingPresetPath(string presetFileName) => Path.Combine(_pairingFolder, presetFileName);

        public string BuildBatchOutputPath(string outputFolderPath, string fileNamePattern,
            string selectedOptionsPreset, int index, int seed) =>
            Path.Combine(outputFolderPath, $"{selectedOptionsPreset}_BAR_{seed}.randomizeopt");
    }
}
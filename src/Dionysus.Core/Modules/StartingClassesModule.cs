// SPDX-License-Identifier: GPL-3.0-only
// Based on the Athena Randomizer's implementation (https://github.com/ignitesouls/AthenaRandomizer).
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

public sealed class StartingClassesModule : IModule
{
    private const string StatModeVigor20 = "Vigor 20, other stats total 80";
    private const string StatModeTotal88 = "All stats total 88";
    private const string StatModeRuneLevel1 = "Rune level 1 (all stats 10)";

    private const string EquipmentRandomized = "Randomized";
    private const string EquipmentNone = "None";

    private const int GlintstoneStaffId = 33000000;
    private const int FingerSealId = 34000000;
    private const int AstrologerIndex = 4;
    private const int ProphetIndex = 5;

    public string Id => "starting_classes";
    public string DisplayName => "Starting Classes";
    public string Category => "Starting Classes";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("starting_classes.enabled", "Change starting classes", false,
            "Changes every class's stats, equipment and starting runes."),
        new ChoiceOption("starting_classes.stat_mode", "Stat spread", StatModeVigor20,
            new[] { StatModeVigor20, StatModeTotal88, StatModeRuneLevel1 }),
        new ChoiceOption("starting_classes.equipment", "Starting equipment", EquipmentRandomized,
            new[] { EquipmentRandomized, EquipmentNone },
            "Randomized gives every class random armor and weapons. None starts every class with nothing."),
        new BoolOption("starting_classes.caster_kits", "Guaranteed caster kits", true,
            "With randomized equipment: Astrologer gets a Glintstone Staff and a random sorcery, Prophet a Finger Seal and a random incantation."),
        new IntOption("starting_classes.starting_runes", "Starting runes", 10_000, 0, 10_000_000),
    };

    public bool IsActive(OptionSet options) => options.GetBool("starting_classes.enabled");

    public void Apply(BuildContext context)
    {
        ParamsEditor editor = context.Params;
        var generator = new StartingClassGenerator();
        string dataDir = context.Resource("Data", "StartingClasses");

        string statMode = context.Options.GetChoice("starting_classes.stat_mode");
        bool runeLevel1 = statMode == StatModeRuneLevel1;
        bool randomEquipment = context.Options.GetChoice("starting_classes.equipment") == EquipmentRandomized;
        bool casterKits = context.Options.GetBool("starting_classes.caster_kits");
        int startingRunes = context.Options.GetInt("starting_classes.starting_runes");

        // Stats: rune level 1 is 10 in all eight attributes; otherwise they're generated as before
        List<StartingClassGenerator.ClassStatAllocation> stats = runeLevel1
            ? Enumerable.Range(0, ParamsEditor.TotalStartingClasses)
                .Select(_ => new StartingClassGenerator.ClassStatAllocation(Enumerable.Repeat(10, 8).ToArray()))
                .ToList()
            : generator.GenerateAllClassStats(context.Randomizer, fixedVigor20WithOtherStatsTotal80: statMode == StatModeVigor20);

        // Equipment is only generated when randomized. Each has its own random stream,
        // so skipping it doesn't change anything else a seed produces.
        var armorSets = randomEquipment ? generator.GenerateArmorSets(context.Randomizer, dataDir) : null;
        var weaponSets = randomEquipment ? generator.GenerateWeaponSets(context.Randomizer, dataDir) : null;

        for (int i = 0; i < ParamsEditor.TotalStartingClasses; i++)
        {
            int charaInitId = ParamsEditor.VagabondCharaInitId + i;

            generator.ClearLoadout(editor, charaInitId);
            generator.ApplyStatAllocation(editor, charaInitId, stats[i]);
            editor.SetInitialRuneLevel(charaInitId, (short)(runeLevel1 ? 1 : 9));

            string description;
            if (randomEquipment)
            {
                generator.ApplyArmorSet(editor, charaInitId, armorSets![i]);
                generator.ApplyWeaponSet(editor, charaInitId, weaponSets![i]);
                description = generator.GenerateClassDescription(editor, armorSets[i], weaponSets[i], stats[i]);
            }
            else
            {
                description = runeLevel1
                    ? "Rune level 1, with 10 in every attribute. Starts with nothing."
                    : "Starts with nothing.";
            }

            // Caster kits are part of a randomized loadout, so they only apply with randomized equipment
            if (casterKits && randomEquipment && i == AstrologerIndex)
            {
                GameItemModel sorcery = PickSpell(context, Path.Combine(dataDir, "sorceries.csv"), "starting_sorceries");
                editor.SetInitialEquipSpell(charaInitId, 0, sorcery.ID);
                editor.SetInitialEquipWepLeft(charaInitId, 1, GlintstoneStaffId);
                description += $", Glintstone Staff, {sorcery.Name}";
            }

            if (casterKits && randomEquipment && i == ProphetIndex)
            {
                GameItemModel incantation = PickSpell(context, Path.Combine(dataDir, "incantations.csv"), "starting_incantations");
                editor.SetInitialEquipSpell(charaInitId, 0, incantation.ID);
                editor.SetInitialEquipWepLeft(charaInitId, 1, FingerSealId);
                description += $", Finger Seal, {incantation.Name}";
            }

            editor.SetInitialRunes(charaInitId, startingRunes);
            context.MenuText.SetClassDescription(i, description);
        }
    }

    private static GameItemModel PickSpell(BuildContext context, string csvPath, string rngKey)
    {
        List<GameItemModel> spells = CsvReaderUtils.Read<GameItemModel>(csvPath);
        Random rng = context.Rng(rngKey);
        return spells[rng.Next(spells.Count)];
    }
}
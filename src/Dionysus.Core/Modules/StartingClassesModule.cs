// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

public sealed class StartingClassesModule : IModule
{
    private const string StatModeVigor20 = "Vigor 20, other stats total 80";
    private const string StatModeTotal88 = "All stats total 88";

    private const int GlintstoneStaffId = 33000000;
    private const int FingerSealId = 34000000;
    private const int AstrologerIndex = 4;
    private const int ProphetIndex = 5;

    public string Id => "starting_classes";
    public string DisplayName => "Starting Classes";
    public string Category => "Starting Classes";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("starting_classes.enabled", "Randomize starting classes", true,
            "Gives every class random stats, armor and weapons."),
        new ChoiceOption("starting_classes.stat_mode", "Stat spread", StatModeVigor20,
            new[] { StatModeVigor20, StatModeTotal88 }),
        new BoolOption("starting_classes.caster_kits", "Guaranteed caster kits", true,
            "Astrologer gets a Glintstone Staff and a random sorcery. Prophet gets a Finger Seal and a random incantation."),
        new IntOption("starting_classes.starting_runes", "Starting runes", 10_000, 0, 10_000_000),
    };

    public bool IsActive(OptionSet options) => options.GetBool("starting_classes.enabled");

    public void Apply(BuildContext context)
    {
        ParamsEditor editor = context.Params;
        var generator = new StartingClassGenerator();
        string dataDir = context.Resource("Data", "StartingClasses");

        bool vigor20 = context.Options.GetChoice("starting_classes.stat_mode") == StatModeVigor20;
        bool casterKits = context.Options.GetBool("starting_classes.caster_kits");
        int startingRunes = context.Options.GetInt("starting_classes.starting_runes");

        // Generate everything first, in the same order as the old app
        var stats = generator.GenerateAllClassStats(context.Randomizer, fixedVigor20WithOtherStatsTotal80: vigor20);
        var armorSets = generator.GenerateArmorSets(context.Randomizer, dataDir);
        var weaponSets = generator.GenerateWeaponSets(context.Randomizer, dataDir);

        for (int i = 0; i < ParamsEditor.TotalStartingClasses; i++)
        {
            int charaInitId = ParamsEditor.VagabondCharaInitId + i;

            generator.ClearLoadout(editor, charaInitId);
            generator.ApplyStatAllocation(editor, charaInitId, stats[i]);
            editor.SetInitialRuneLevel(charaInitId, 9);
            generator.ApplyArmorSet(editor, charaInitId, armorSets[i]);
            generator.ApplyWeaponSet(editor, charaInitId, weaponSets[i]);

            string description = generator.GenerateClassDescription(editor, armorSets[i], weaponSets[i], stats[i]);

            if (casterKits && i == AstrologerIndex)
            {
                GameItemModel sorcery = PickSpell(context, Path.Combine(dataDir, "sorceries.csv"), "starting_sorceries");
                editor.SetInitialEquipSpell(charaInitId, 0, sorcery.ID);
                editor.SetInitialEquipWepLeft(charaInitId, 1, GlintstoneStaffId);
                description += $", Glintstone Staff, {sorcery.Name}";
            }

            if (casterKits && i == ProphetIndex)
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
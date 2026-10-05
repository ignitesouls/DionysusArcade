// SPDX-License-Identifier: GPL-3.0-only
// Based on the Athena Randomizer's Starlight Shop (https://github.com/ignitesouls/AthenaRandomizer).
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

// A seeded Starlight Shard shop: weapons filtered by how far they are from wieldable at rune level 1,
// talismans and physick tears from curated per-format lists, plus free armor sets and free items
// in the runes shop. The pack's talk script opens these shop ranges.
public sealed class StarlightShopModule : IModule
{
    // Shop rows the packs' talk scripts open (the same ranges as Athena)
    private const int RunesShopFirstRow = 9100000;
    private const int StarlightWeaponsFirstRow = 9200000;
    private const int StarlightTearsFirstRow = 9201000;
    private const int StarlightTalismansFirstRow = 9202000;
    private const int StarlightMenuTitleTextId = 508000;
    private const byte StarlightShardCostType = 2;
    private const byte RunesCostType = 0;

    // Stock flags reserved for these shops, 10 apart
    private const uint RunesShopFirstFlag = 1056447000;
    private const uint StarlightFirstFlag = 1056457000;
    private const uint FlagStep = 10;

    // "Wieldable" is measured against rune level 1: 10 in every attribute
    private const int ReferenceStat = 10;

    private static readonly string[] Pools = { "LeonineLocked", "Stormvault", "AcademyKeyed" };

    public string Id => "starlight";
    public string DisplayName => "Starlight Shop";
    public string Category => "Shops";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("starlight.enabled", "Starlight Shop", false,
            "A seeded shop selling weapons, talismans and physick tears for Starlight Shards, plus free armor and items."),
        new ChoiceOption("starlight.pool", "Format lists", "LeonineLocked", Pools,
            "Which format's talisman, physick tear, armor and free item lists to use (folders in Data/StarlightShop)."),
        new IntOption("starlight.weapon_count", "Weapons", 3, 0, 10),
        new IntOption("starlight.weapon_cost", "Weapon price (shards)", 5, 0, 99),
        new IntOption("starlight.max_levels_off", "Max levels from wieldable", 8, 1, 99,
            "Only weapons fewer than this many levels from wieldable at rune level 1 can appear."),
        new IntOption("starlight.talisman_count", "Talismans", 2, 0, 20),
        new IntOption("starlight.physick_count", "Physick tears", 2, 0, 20),
        new IntOption("starlight.armor_sets", "Free armor sets", 2, 0, 10),
        new BoolOption("starlight.free_items", "Free items", true,
            "The format's free items (its free_items.csv), free in the runes shop."),
    };

    public bool IsActive(OptionSet options) => options.GetBool("starlight.enabled");

    public void Apply(BuildContext context)
    {
        ParamsEditor editor = context.Params;
        OptionSet o = context.Options;
        string shopData = context.Resource("Data", "StarlightShop");
        string formatData = Path.Combine(shopData, o.GetChoice("starlight.pool"));
        var report = new List<string>();

        // ---- Starlight Shard shop ----

        // Weapons: every weapon close enough to wieldable, then a seeded pick
        int maxLevelsOff = o.GetInt("starlight.max_levels_off");
        List<GameItemModel> wieldable = CsvReaderUtils.Read<GameItemModel>(Path.Combine(shopData, "AllWeapons.csv"))
            .Where(w => !IsAmmunition(w.ID)) // delete this line to allow arrows and bolts
            .Where(w => LevelsFromWieldable(editor, w.ID) is int levels && levels < maxLevelsOff)
            .ToList();

        int row = StarlightWeaponsFirstRow;
        uint flag = StarlightFirstFlag;
        foreach (GameItemModel weapon in PickRandom(wieldable, o.GetInt("starlight.weapon_count"), context.Rng("starlight_weapons")))
        {
            AddRow(editor, row++, $"[Starlight Shop - Weapon] {weapon.Name}", weapon.ID, weapon.EquipType,
                StarlightShardCostType, o.GetInt("starlight.weapon_cost"), flag, StarlightMenuTitleTextId);
            flag += FlagStep;
            report.Add(weapon.Name);
        }

        // Physick tears and talismans: a seeded pick from the format's curated lists
        flag = AddStarlightItems(editor, StarlightTearsFirstRow, flag,
            CsvReaderUtils.Read<ShopItemModel>(Path.Combine(formatData, "physick_tears.csv")),
            o.GetInt("starlight.physick_count"), context.Rng("starlight_tears"), report);
        AddStarlightItems(editor, StarlightTalismansFirstRow, flag,
            CsvReaderUtils.Read<ShopItemModel>(Path.Combine(formatData, "talismans.csv")),
            o.GetInt("starlight.talisman_count"), context.Rng("starlight_talismans"), report);

        // ---- Runes shop: free armor sets and the format's free items ----

        row = RunesShopFirstRow;
        flag = RunesShopFirstFlag;
        List<ArmorSetModel> armorSets = CsvReaderUtils.Read<ArmorSetModel>(Path.Combine(formatData, "armor_sets.csv"));
        foreach (ArmorSetModel set in PickRandom(armorSets, o.GetInt("starlight.armor_sets"), context.Rng("starlight_armor")))
        {
            foreach (var (piece, id) in new[] { ("Helm", set.HelmID), ("Torso", set.TorsoID), ("Gauntlets", set.GauntletsID), ("Greaves", set.GreavesID) })
            {
                if (id is not int armorId) continue;
                AddRow(editor, row++, $"[Starlight Shop - Armor] {set.Name} {piece}", armorId, equipType: 1,
                    RunesCostType, price: 0, flag, menuTextId: null);
                flag += FlagStep;
            }
            report.Add($"{set.Name} (armor)");
        }

        if (o.GetBool("starlight.free_items"))
        {
            foreach (ShopItemModel item in CsvReaderUtils.Read<ShopItemModel>(Path.Combine(formatData, "free_items.csv")))
            {
                uint stockFlag = item.EventFlagID ?? flag;
                if (item.EventFlagID == null) flag += FlagStep;
                AddRow(editor, row++, $"[Starlight Shop - {item.Type}] {item.Name}", item.ID, item.EquipType,
                    RunesCostType, price: 0, stockFlag, menuTextId: null, item.SellQuantity);
            }
        }

        context.Report["Starlight Shop"] = report;
    }

    // Arrows (50xxxxxx), greatarrows (51xxxxxx), bolts (52xxxxxx) and greatbolts (53xxxxxx).
    // They have no stat requirements, so they'd always count as wieldable.
    private static bool IsAmmunition(int weaponId) => weaponId >= 50000000 && weaponId < 54000000;

    // Same rule as the class descriptions: the total missing points across the five requirement stats,
    // with two-handing counted for Strength. Null if the weapon isn't in this regulation.
    private static int? LevelsFromWieldable(ParamsEditor editor, int weaponId)
    {
        try
        {
            int missing = 0;
            for (int stat = 0; stat < 5; stat++) // Strength, Dexterity, Intelligence, Faith, Arcane
            {
                int requirement = editor.GetEquipWeaponProperStat(weaponId, stat);
                if (stat == 0)
                {
                    // Two-handing multiplies Strength by 1.5, so the requirement is effectively 2/3 of it, rounded up
                    requirement = (requirement * 2 + 2) / 3;
                }
                missing += Math.Max(0, requirement - ReferenceStat);
            }
            return missing;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static uint AddStarlightItems(ParamsEditor editor, int firstRow, uint flag, List<ShopItemModel> pool,
        int count, Random rng, List<string> report)
    {
        int row = firstRow;
        foreach (ShopItemModel item in PickRandom(pool, count, rng))
        {
            uint stockFlag = item.EventFlagID ?? flag;
            if (item.EventFlagID == null) flag += FlagStep;
            AddRow(editor, row++, $"[Starlight Shop - {item.Type}] {item.Name}", item.ID, item.EquipType,
                StarlightShardCostType, ShardPrice(item.Cost), stockFlag, StarlightMenuTitleTextId, item.SellQuantity);
            report.Add(item.Name);
        }
        return flag;
    }

    // Athena's pricing: an item's usual rune price decides whether it costs 1, 2 or 3 shards
    private static int ShardPrice(int runeCost) => runeCost > 40000 ? 3 : runeCost > 20000 ? 2 : 1;

    private static void AddRow(ParamsEditor editor, int row, string name, int equipId, byte equipType,
        byte costType, int price, uint stockFlag, int? menuTextId, short sellQuantity = 1)
    {
        editor.CreateNewShopLineupRow(row, name);
        editor.SetShopLineupEquipId(row, equipId);
        editor.SetShopLineupEquipType(row, equipType);
        editor.SetShopLineupCostType(row, costType);
        editor.SetShopLineupSellPrice(row, price);
        editor.SetShopLineupEventFlagForStock(row, stockFlag);
        editor.SetShopLineupSellQuantity(row, sellQuantity);
        editor.SetShopLineupNumSold(row, 1);
        if (menuTextId is int textId)
        {
            editor.SetShopLineupMenuTextId(row, textId);
        }
    }

    // A seeded pick of up to `count` different entries
    private static List<T> PickRandom<T>(List<T> pool, int count, Random rng)
    {
        var copy = new List<T>(pool);
        int take = Math.Min(count, copy.Count);
        for (int i = 0; i < take; i++)
        {
            int j = rng.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, take);
    }
}
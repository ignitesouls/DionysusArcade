// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using Dionysus.Core.Randomization;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

public sealed class ItemShuffleModule : IModule
{
    public string Id => "item_shuffle";
    public string DisplayName => "Item Shuffle";
    public string Category => "Items";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("item_shuffle.spells", "Shuffle spells", true,
            "Sorceries and incantations, shuffled within their groups."),
        new BoolOption("item_shuffle.chance_weapons", "Shuffle chance weapon drops", true,
            "Chance-based enemy weapon drops, shuffled within weapon classes."),
        new BoolOption("item_shuffle.map_weapons", "Shuffle map and guaranteed weapons", true,
            "Weapons found in the world and guaranteed drops, shuffled within weapon classes."),
        new BoolOption("item_shuffle.remembrances", "Shuffle remembrance rewards", true,
            "Weapons, sorceries and incantations traded for remembrances."),
        new BoolOption("item_shuffle.shop_weapons", "Shuffle shop weapons", true),
        new BoolOption("item_shuffle.perfume_bottles", "Shuffle perfume bottles", true),
    };

    // Active if at least one shuffle is turned on
    public bool IsActive(OptionSet options) => Options.Any(o => options.GetBool(o.Key));

    public void Apply(BuildContext context)
    {
        ParamsEditor editor = context.Params;
        var urr = context.Randomizer;
        OptionSet o = context.Options;
        string groups = context.Resource("RandomizationGroups", "battleship");

        // Look up where every item is placed, once, before anything is shuffled
        var weaponIdsToItemLotMap = editor.GetWeaponIdsToItemLotMap();
        var weaponIdsToItemLotEnemy = editor.GetWeaponIdsToItemLotEnemy();
        var weaponIdsToShopLineup = editor.GetWeaponIdsToShopLineup();

        var goodsIdsToItemLotMap = editor.GetGoodsIdsToItemLotMap();
        var goodsIdsToItemLotEnemy = editor.GetGoodsIdsToItemLotEnemy();
        var goodsIdsToShopLineup = editor.GetGoodsIdsToShopLineup();

        if (o.GetBool("item_shuffle.spells"))
        {
            ReplacementUtils.Randomize<GameItemModel>(editor, urr, Path.Combine(groups, "spells"),
                goodsIdsToItemLotMap, goodsIdsToItemLotEnemy, goodsIdsToShopLineup);
        }

        if (o.GetBool("item_shuffle.chance_weapons"))
        {
            ReplacementUtils.RandomizeItemLotEnemy<WeaponModel>(editor, urr, Path.Combine(groups, "chance_weapons"),
                weaponIdsToItemLotEnemy);
        }

        if (o.GetBool("item_shuffle.map_weapons"))
        {
            ReplacementUtils.Randomize<GameItemModel>(editor, urr, Path.Combine(groups, "map_guaranteed_weapons"),
                weaponIdsToItemLotMap, weaponIdsToItemLotEnemy, weaponIdsToShopLineup);
        }

        if (o.GetBool("item_shuffle.remembrances"))
        {
            ReplacementUtils.RandomizeAndReplaceShopLineupFile<WeaponModel>(editor, urr,
                Path.Combine(groups, "remembrances", "weapons.csv"), weaponIdsToShopLineup);
            ReplacementUtils.RandomizeAndReplaceShopLineupFile<WeaponModel>(editor, urr,
                Path.Combine(groups, "remembrances", "sorceries.csv"), goodsIdsToShopLineup);
            ReplacementUtils.RandomizeAndReplaceShopLineupFile<WeaponModel>(editor, urr,
                Path.Combine(groups, "remembrances", "incantations.csv"), goodsIdsToShopLineup);
        }

        if (o.GetBool("item_shuffle.shop_weapons"))
        {
            ReplacementUtils.RandomizeAndReplaceShopLineupDir<WeaponModel>(editor, urr,
                Path.Combine(groups, "shop_weapons"), weaponIdsToShopLineup);
        }

        if (o.GetBool("item_shuffle.perfume_bottles"))
        {
            ReplacementUtils.Randomize<GameItemModel>(editor, urr, Path.Combine(groups, "perfume_bottles"),
                weaponIdsToItemLotMap, weaponIdsToItemLotEnemy, weaponIdsToShopLineup);
        }
    }
}
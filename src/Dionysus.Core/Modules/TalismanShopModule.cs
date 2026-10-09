// SPDX-License-Identifier: GPL-3.0-only
// Based on the Athena Randomizer's implementation (https://github.com/ignitesouls/AthenaRandomizer).
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using Dionysus.Core.Randomization;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

// Bernie Bingo's shop. Either the classic fixed list, or a seeded selection where some items are always for sale.
public sealed class TalismanShopModule : IModule
{
    private const int FirstShopLineupId = 9300000;
    private const uint FirstEventFlagId = 1056448000;
    private const uint EventFlagStep = 10;

    public string Id => "talisman_shop";
    public string DisplayName => "Talisman Shop";
    public string Category => "Shops";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("talisman_shop.enabled", "Talisman shop", false,
            "Bernie Bingo's shop of talismans, physick tears and whetblades."),
        new BoolOption("talisman_shop.randomized", "Randomized shop", false,
            "Sells a random selection for each seed. The items in Randomized/fixed.csv are always for sale."),
        new IntOption("talisman_shop.talismans", "Random talismans", 10, 0, 50,
            "Talismans picked at random, on top of the ones that are always for sale."),
        new IntOption("talisman_shop.physick_tears", "Random physick tears", 4, 0, 30),
        new IntOption("talisman_shop.whetblades", "Random whetblades", 3, 0, 10),
    };

    public bool IsActive(OptionSet options) => options.GetBool("talisman_shop.enabled");

    public void Apply(BuildContext context)
    {
        if (!context.Options.GetBool("talisman_shop.randomized"))
        {
            // The classic shop: the whole list, exactly as before
            AddRows(context.Params, CsvReaderUtils.Read<ShopItemModel>(
                context.Resource("Data", "TalismanShop", "shop_talismans.csv")));
            return;
        }

        string dir = context.Resource("Data", "TalismanShop", "Randomized");
        OptionSet o = context.Options;

        List<ShopItemModel> fixedItems = CsvReaderUtils.Read<ShopItemModel>(Path.Combine(dir, "fixed.csv"));
        HashSet<int> fixedIds = fixedItems.Select(item => item.ID).ToHashSet();

        // A pool never offers an item that's already always for sale
        List<ShopItemModel> Pool(string file) =>
            CsvReaderUtils.Read<ShopItemModel>(Path.Combine(dir, file)).Where(item => !fixedIds.Contains(item.ID)).ToList();

        List<ShopItemModel> randomItems = new();
        randomItems.AddRange(RandomPick.From(Pool("talismans.csv"), o.GetInt("talisman_shop.talismans"), context.Rng("talisman_shop_talismans")));
        randomItems.AddRange(RandomPick.From(Pool("physick_tears.csv"), o.GetInt("talisman_shop.physick_tears"), context.Rng("talisman_shop_tears")));
        randomItems.AddRange(RandomPick.From(Pool("whetblades.csv"), o.GetInt("talisman_shop.whetblades"), context.Rng("talisman_shop_whetblades")));

        AddRows(context.Params, fixedItems.Concat(randomItems).ToList());
        context.Report["Talisman Shop"] = randomItems.Select(item => item.Name).ToList();
    }

    // One shop row per item, starting at 9300000. Items with their own flag use it (so buying one removes
    // its overworld counterpart); the rest get the next flag from the reserved range.
    private static void AddRows(ParamsEditor editor, List<ShopItemModel> items)
    {
        int shopLineupId = FirstShopLineupId;
        uint nextEventFlagId = FirstEventFlagId;

        foreach (ShopItemModel item in items)
        {
            uint eventFlag;
            if (item.EventFlagID is uint ownFlag)
            {
                eventFlag = ownFlag;
            }
            else
            {
                eventFlag = nextEventFlagId;
                nextEventFlagId += EventFlagStep;
            }

            string name = $"[Bernie Bingo - {item.Type}] {item.Name}";

            editor.CreateNewShopLineupRow(shopLineupId, name);
            editor.SetShopLineupEquipId(shopLineupId, item.ID);
            editor.SetShopLineupEquipType(shopLineupId, item.EquipType);
            editor.SetShopLineupSellPrice(shopLineupId, item.Cost);
            editor.SetShopLineupEventFlagForStock(shopLineupId, eventFlag);
            editor.SetShopLineupSellQuantity(shopLineupId, item.SellQuantity);

            shopLineupId++;
        }
    }
}
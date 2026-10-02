// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Data;
using Dionysus.Core.Options;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

public sealed class TalismanShopModule : IModule
{
    public string Id => "talisman_shop";
    public string DisplayName => "Talisman Shop";
    public string Category => "Shops";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("talisman_shop.enabled", "Talisman shop", true,
            "Adds a shop selling the talismans listed in shop_talismans.csv.")
    };

    public bool IsActive(OptionSet options) => options.GetBool("talisman_shop.enabled");

    public void Apply(BuildContext context)
    {
        const int firstShopLineupId = 9300000;
        const uint firstEventFlagId = 1056448000;
        const uint eventFlagStep = 10;

        List<ShopItemModel> items = CsvReaderUtils.Read<ShopItemModel>(
            context.Resource("Data", "TalismanShop", "shop_talismans.csv"));

        ParamsEditor editor = context.Params;
        int shopLineupId = firstShopLineupId;
        uint nextEventFlagId = firstEventFlagId;

        foreach (ShopItemModel item in items)
        {
            // Use the talisman's own flag if the CSV has one, otherwise take the next reserved flag
            uint eventFlag;
            if (item.EventFlagID is uint ownFlag)
            {
                eventFlag = ownFlag;
            }
            else
            {
                eventFlag = nextEventFlagId;
                nextEventFlagId += eventFlagStep;
            }

            // Row name kept from the old app so the parity check matches. Rename after parity passes.
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
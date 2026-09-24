// SPDX-License-Identifier: GPL-3.0-only
using Dionysus.Core.Options;
using EldenRingParamsEditor;

namespace Dionysus.Core.Modules;

public sealed class TweaksModule : IModule
{
    public string Id => "tweaks";
    public string DisplayName => "Tweaks";
    public string Category => "Tweaks";

    public IReadOnlyList<OptionDefinition> Options { get; } = new List<OptionDefinition>
    {
        new BoolOption("tweaks.remove_roundtable_fist_check", "Remove Roundtable fist check", true,
            "Removes the Cipher Pata pickup in Roundtable Hold."),
        new BoolOption("tweaks.seluvis_scorpion_charm", "Magic Scorpion Charm at Seluvis", true,
            "Replaces Pidia's Old Fang in Seluvis's shop with the Magic Scorpion Charm."),
        new BoolOption("tweaks.lock_serpent_hunter", "Serpent Hunter can't be upgraded", true,
            "Makes the Serpent Hunter a non-upgradable weapon."),
    };

    public bool IsActive(OptionSet options) =>
        options.GetBool("tweaks.remove_roundtable_fist_check")
        || options.GetBool("tweaks.seluvis_scorpion_charm")
        || options.GetBool("tweaks.lock_serpent_hunter");

    public void Apply(BuildContext context)
    {
        if (context.Options.GetBool("tweaks.remove_roundtable_fist_check"))
            RemoveRoundtableFistCheck(context.Params);

        if (context.Options.GetBool("tweaks.seluvis_scorpion_charm"))
            AddScorpionCharmToSeluvis(context.Params);

        if (context.Options.GetBool("tweaks.lock_serpent_hunter"))
            LockSerpentHunter(context.Params);
    }

    private static void RemoveRoundtableFistCheck(ParamsEditor editor)
    {
        const int cipherPataLotId = 11100000;
        editor.SetItemLotMapLotItemId(cipherPataLotId, 0, 0);
        editor.SetItemLotMapCategory(cipherPataLotId, 0, 0);
        editor.SetItemLotMapItemNum(cipherPataLotId, 0, 0);
    }

    private static void AddScorpionCharmToSeluvis(ParamsEditor editor)
    {
        const int pidiaOldFangShopId = 100329;
        const int magicScorpionCharmId = 2000;
        const byte talismanEquipType = 2;
        const uint acquisitionFlag = 400141;
        const short sellQuantity = 1;
        const int sellPrice = 5000;

        editor.SetShopLineupEquipId(pidiaOldFangShopId, magicScorpionCharmId);
        editor.SetShopLineupEquipType(pidiaOldFangShopId, talismanEquipType);
        editor.SetShopLineupEventFlagForStock(pidiaOldFangShopId, acquisitionFlag);
        editor.SetShopLineupSellQuantity(pidiaOldFangShopId, sellQuantity);
        editor.SetShopLineupSellPrice(pidiaOldFangShopId, sellPrice);
    }

    private static void LockSerpentHunter(ParamsEditor editor)
    {
        const int serpentHunterId = 17030000;
        editor.SetEquipWeaponIsCustom(serpentHunterId, 0);
        editor.SetEquipWeaponMaterialSetId(serpentHunterId, 0);
        editor.SetEquipWeaponReinforceTypeId(serpentHunterId, 3000);
        editor.SetEquipWeaponReinforceShopCategory(serpentHunterId, 0);
    }
}
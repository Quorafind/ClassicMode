using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ClassicModeMod;

[HarmonyPatch(typeof(Nunchaku), nameof(RelicModel.Rarity), MethodType.Getter)]
internal static class NunchakuClassicOnlyRarityPatch
{
    static bool Prefix(ref RelicRarity __result)
    {
        if (!ClassicConfig.OnlyClassicRelics)
            return true;

        __result = RelicRarity.Common;
        return false;
    }
}

[HarmonyPatch(typeof(LuckyFysh), nameof(RelicModel.Rarity), MethodType.Getter)]
internal static class LuckyFyshClassicOnlyRarityPatch
{
    static bool Prefix(ref RelicRarity __result)
    {
        if (!ClassicConfig.OnlyClassicRelics)
            return true;

        __result = RelicRarity.Common;
        return false;
    }
}

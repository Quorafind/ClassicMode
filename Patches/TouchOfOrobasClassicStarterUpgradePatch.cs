using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ClassicModeMod;

[HarmonyPatch(typeof(TouchOfOrobas), nameof(TouchOfOrobas.GetUpgradedStarterRelic))]
internal static class TouchOfOrobasClassicStarterUpgradePatch
{
    static bool Prefix(RelicModel starterRelic, ref RelicModel __result)
    {
        if (!ClassicConfig.OnlyClassicRelics)
            return true;

        if (starterRelic.Id == ModelDb.Relic<RingOfTheSnake>().Id)
        {
            __result = ModelDb.Relic<RingOfTheSerpent>();
            return false;
        }

        if (starterRelic.Id == ModelDb.Relic<CrackedCore>().Id)
        {
            __result = ModelDb.Relic<FrozenCore>();
            return false;
        }

        return true;
    }
}


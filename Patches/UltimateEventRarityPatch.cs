using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ClassicModeMod;

[HarmonyPatch(typeof(CardModel), "get_Rarity")]
internal static class UltimateEventRarityPatch
{
    static void Postfix(CardModel __instance, ref CardRarity __result)
    {
        if (!(ClassicConfig.ClassicColorless && !ClassicConfig.ClassicColorlessHybrid))
            return;

        if (__instance.Id == ModelDb.Card<UltimateStrike>().Id
            || __instance.Id == ModelDb.Card<UltimateDefend>().Id)
        {
            __result = CardRarity.Event;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace ClassicModeMod;

[HarmonyPatch(typeof(RestSiteOption), nameof(RestSiteOption.Generate))]
internal static class PeacePipeRestSiteOptionPatch
{
    static void Postfix(Player player, ref List<RestSiteOption> __result)
    {
        if (__result.Any(o => o is PeacePipeTokeRestSiteOption))
            return;

        if (player.Relics.Any(r => r is PeacePipeRelic))
        {
            __result.Add(new PeacePipeTokeRestSiteOption(player));
        }
    }
}

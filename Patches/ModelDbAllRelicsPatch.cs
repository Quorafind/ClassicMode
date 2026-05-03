using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

[HarmonyPatch(typeof(ModelDb), "get_AllRelics")]
internal static class ModelDbAllRelicsPatch
{
    static void Postfix(ref IEnumerable<RelicModel> __result)
    {
        if (__result == null)
            return;

        var includeClassicPools = ClassicConfig.AddClassicRelics || ClassicConfig.OnlyClassicRelics;
        var includeDarvOnlySet = ClassicConfig.ReplaceAncientsWithDarv && !includeClassicPools;
        if (!includeClassicPools && !includeDarvOnlySet)
            return;

        IEnumerable<RelicModel> relics = __result;

        if (includeClassicPools)
        {
            relics = relics.Concat(ModelDb.RelicPool<ClassicSharedRelicPool>().AllRelics);
        }
        else if (includeDarvOnlySet)
        {
            relics = relics.Concat(
            [
                ModelDb.Relic<BustedCrownRelic>(),
                ModelDb.Relic<CoffeeDripperRelic>(),
                ModelDb.Relic<CursedKeyRelic>(),
                ModelDb.Relic<FusionHammerRelic>(),
                ModelDb.Relic<RunicCubeRelic>(),
                ModelDb.Relic<RunicDomeRelic>(),
                ModelDb.Relic<SacredBarkRelic>(),
                ModelDb.Relic<SlaversCollarRelic>(),
                ModelDb.Relic<TinyHouseRelic>()
            ]);
        }

        __result = relics
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .OrderBy(r => r.Id.Entry);
    }
}

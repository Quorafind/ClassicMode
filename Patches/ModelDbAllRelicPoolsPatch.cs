using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

[HarmonyPatch(typeof(ModelDb), "get_AllRelicPools")]
internal static class ModelDbAllRelicPoolsPatch
{
    static void Postfix(ref IEnumerable<RelicPoolModel> __result)
    {
        if (__result == null)
            return;

        // Even without classic relic pool modes, ReplaceAncientsWithDarv can grant
        // classic boss relics; they still need an owning pool for tooltip/prefix logic.
        if (!ClassicConfig.AddClassicRelics && !ClassicConfig.OnlyClassicRelics && !ClassicConfig.ReplaceAncientsWithDarv)
            return;

        List<RelicPoolModel> pools = __result.ToList();
        var classicPools = new RelicPoolModel[]
        {
            ModelDb.RelicPool<ClassicSharedRelicPool>(),
            ModelDb.RelicPool<ClassicIroncladRelicPool>(),
            ModelDb.RelicPool<ClassicSilentRelicPool>(),
            ModelDb.RelicPool<ClassicDefectRelicPool>(),
        };

        foreach (var pool in classicPools)
        {
            if (pools.Any(p => p.Id == pool.Id))
                continue;
            pools.Add(pool);
        }

        __result = pools;
    }
}

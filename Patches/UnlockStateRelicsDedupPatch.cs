using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Unlocks;

namespace ClassicModeMod;

// Keep first-seen order while removing duplicate relic IDs from UnlockState aggregation.
[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.Relics), MethodType.Getter)]
internal static class UnlockStateRelicsDedupPatch
{
    static void Postfix(ref IEnumerable<RelicModel> __result)
    {
        if (__result == null)
            return;

        IEnumerable<RelicModel> relics = __result;

        // In classic relic modes, ensure encyclopedia aggregation includes both
        // vanilla shared relics and classic shared relics, then dedupe by id.
        if (ClassicConfig.AddClassicRelics || ClassicConfig.OnlyClassicRelics)
        {
            relics = relics
                .Concat(ModelDb.RelicPool<SharedRelicPool>().AllRelics)
                .Concat(ModelDb.RelicPool<ClassicSharedRelicPool>().AllRelics)
                // Starter-upgrade classics used by Touch of Orobas in classic-only mode.
                .Concat(
                [
                    ModelDb.Relic<RingOfTheSerpent>(),
                    ModelDb.Relic<FrozenCore>(),
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
        else if (ClassicConfig.ReplaceAncientsWithDarv)
        {
            // Darv replacement can award these relics even when classic relic pool
            // modes are off; include them so encyclopedia/inspect treats them as unlocked.
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

        var seen = new HashSet<ModelId>();
        var unique = new List<RelicModel>();

        foreach (RelicModel relic in relics)
        {
            if (relic == null)
                continue;
            if (!seen.Add(relic.Id))
                continue;

            unique.Add(relic);
        }

        __result = unique;
    }
}

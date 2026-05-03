using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace ClassicModeMod;

// Deduplicate relics at the encyclopedia collection ingress to avoid cross-pool triple entries.
[HarmonyPatch(typeof(NRelicCollection), "AddRelics")]
internal static class RelicCollectionAddRelicsDedupPatch
{
    static void Prefix(NRelicCollection __instance, ref IEnumerable<RelicModel> relics)
    {
        if (relics == null)
            return;

        HashSet<ModelId> seenIds = __instance.Relics
            .Select(r => r.Id)
            .ToHashSet();

        List<RelicModel> deduped = new();
        foreach (RelicModel relic in relics)
        {
            if (relic == null)
                continue;
            if (!seenIds.Add(relic.Id))
                continue;

            deduped.Add(relic);
        }

        relics = deduped;
    }
}

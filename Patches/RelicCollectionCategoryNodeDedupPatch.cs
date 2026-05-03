using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;

namespace ClassicModeMod;

// NRelicCollectionCategory.LoadRelics can include the same relic once per character pool.
// Deduplicate by model id right before nodes are created to avoid repeated encyclopedia entries.
[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelicNodes")]
internal static class RelicCollectionCategoryNodeDedupPatch
{
    static void Prefix(ref IEnumerable<RelicModel> relics)
    {
        if (relics == null)
            return;

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

        relics = unique;
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ClassicModeMod;

internal static class DarvRelicDescriptionHelper
{
    internal static LocString BuildSafeDynamicDescription(RelicModel relic)
    {
        var description = new LocString("relics", $"{relic.Id.Entry}.description");
        relic.DynamicVars.AddTo(description);
        description.Add("energyPrefix", "");
        description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
        return description;
    }
}

[HarmonyPatch(typeof(Hive), nameof(Hive.GetUnlockedAncients))]
internal static class HiveAncientReplacePatch
{
    static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        if (!ClassicConfig.ReplaceAncientsWithDarv) return;
        __result = [ModelDb.AncientEvent<Darv>()];
    }
}

[HarmonyPatch(typeof(Glory), nameof(Glory.GetUnlockedAncients))]
internal static class GloryAncientReplacePatch
{
    static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        if (!ClassicConfig.ReplaceAncientsWithDarv) return;
        __result = [ModelDb.AncientEvent<Darv>()];
    }
}

[HarmonyPatch(typeof(Darv), "GenerateInitialOptions")]
internal static class DarvClassicBossOptionsPatch
{
    private static readonly MethodInfo? AncientDoneMethod =
        AccessTools.Method(typeof(AncientEventModel), "Done", []);

    static bool Prefix(Darv __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!ClassicConfig.ReplaceAncientsWithDarv || AncientDoneMethod == null)
            return true;

        var owner = __instance.Owner;
        if (owner == null)
            return true;

        var pool = GetClassicBossRelicPool();
        var owned = owner.Relics.Select(r => r.Id).ToHashSet();
        var candidates = pool.Where(r => !owned.Contains(r.Id)).ToList();
        if (candidates.Count < 3)
            candidates = pool.ToList();

        owner.PlayerRng.Rewards.Shuffle(candidates);
        var picks = candidates.Take(3).DistinctBy(r => r.Id).ToList();
        if (picks.Count < 3)
            return true;

        var options = new List<EventOption>(3);
        foreach (var relic in picks)
        {
            options.Add(CreateSafeRelicOption(__instance, relic));
        }

        if (options.Count == 3)
        {
            __result = options;
            return false;
        }

        return true;
    }

    private static EventOption CreateSafeRelicOption(Darv darv, RelicModel relicModel)
    {
        var relic = relicModel.ToMutable();
        var textKey = $"DARV.pages.INITIAL.options.{relic.Id.Entry}";
        var title = new LocString("relics", $"{relic.Id.Entry}.title");
        var description = DarvRelicDescriptionHelper.BuildSafeDynamicDescription(relic);

        Task OnChosen()
        {
            return ObtainAndFinish(darv, relic);
        }

        return new EventOption(darv, OnChosen, title, description, textKey, Enumerable.Empty<IHoverTip>())
            .WithRelic(relic);
    }

    private static async Task ObtainAndFinish(Darv darv, RelicModel relic)
    {
        if (darv.Owner != null)
            await RelicCmd.Obtain(relic, darv.Owner);
        AncientDoneMethod?.Invoke(darv, null);
    }

    private static IReadOnlyList<RelicModel> GetClassicBossRelicPool()
    {
        return
        [
            ModelDb.Relic<Astrolabe>(),
            ModelDb.Relic<BlackBlood>(),
            ModelDb.Relic<BlackStar>(),
            ModelDb.Relic<BustedCrownRelic>(),
            ModelDb.Relic<CallingBell>(),
            ModelDb.Relic<CoffeeDripperRelic>(),
            ModelDb.Relic<CursedKeyRelic>(),
            ModelDb.Relic<Ectoplasm>(),
            ModelDb.Relic<EmptyCage>(),
            ModelDb.Relic<FusionHammerRelic>(),
            ModelDb.Relic<HoveringKite>(),
            ModelDb.Relic<Inserter>(),
            ModelDb.Relic<MarkOfPain>(),
            ModelDb.Relic<NuclearBattery>(),
            ModelDb.Relic<PandorasBox>(),
            ModelDb.Relic<PhilosophersStone>(),
            ModelDb.Relic<RunicCubeRelic>(),
            ModelDb.Relic<RunicDomeRelic>(),
            ModelDb.Relic<RunicPyramid>(),
            ModelDb.Relic<SacredBarkRelic>(),
            ModelDb.Relic<SneckoEye>(),
            ModelDb.Relic<Sozu>(),
            ModelDb.Relic<SlaversCollarRelic>(),
            ModelDb.Relic<TinyHouseRelic>(),
            ModelDb.Relic<TouchOfOrobas>(),
            ModelDb.Relic<VelvetChoker>(),
            ModelDb.Relic<WristBlade>()
        ];
    }
}

[HarmonyPatch(typeof(Darv), nameof(Darv.AllPossibleOptions), MethodType.Getter)]
internal static class DarvAllPossibleOptionsPatch
{
    static void Postfix(Darv __instance, ref IEnumerable<EventOption> __result)
    {
        // Ensure relic encyclopedia's Darv column can discover all classic boss relic options.
        var baseOptions = (__result ?? Enumerable.Empty<EventOption>()).ToList();
        var existingRelicIds = baseOptions
            .Where(o => o?.Relic != null)
            .Select(o => o.Relic!.Id)
            .ToHashSet();

        foreach (var relic in DarvClassicBossOptionsPatch_GetBossRelicsForCollection())
        {
            if (!existingRelicIds.Add(relic.Id))
                continue;

            string textKey = $"DARV.pages.INITIAL.options.{relic.Id.Entry}";
            var title = new LocString("relics", $"{relic.Id.Entry}.title");
            var description = DarvRelicDescriptionHelper.BuildSafeDynamicDescription(relic.ToMutable());
            var option = new EventOption(__instance, onChosen: null, title, description, textKey, Enumerable.Empty<IHoverTip>())
                .WithRelic(relic.ToMutable());
            baseOptions.Add(option);
        }

        __result = baseOptions;
    }

    private static IReadOnlyList<RelicModel> DarvClassicBossOptionsPatch_GetBossRelicsForCollection()
    {
        return
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
        ];
    }
}

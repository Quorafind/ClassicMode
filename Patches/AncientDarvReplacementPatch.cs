using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
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

internal static class ClassicDarvRelicPoolHelper
{
    private static readonly IReadOnlySet<ModelId> DarvExcludedRelicIds = new HashSet<ModelId>
    {
        ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.BlackBlood>().Id,
        ModelDb.Relic<RingOfTheSerpent>().Id,
        ModelDb.Relic<FrozenCore>().Id,
    };

    private static IEnumerable<RelicModel> ApplyDarvPoolExclusions(IEnumerable<RelicModel> source)
    {
        return source.Where(r => !DarvExcludedRelicIds.Contains(r.Id));
    }

    private static IEnumerable<RelicModel> ApplyDarvGlobalInclusions(IEnumerable<RelicModel> source)
    {
        // Keep Touch of Orobas available to every character in Darv options.
        return source.Concat(new[] { ModelDb.Relic<TouchOfOrobas>() });
    }

    private static IReadOnlyList<RelicModel> NormalizeDarvPool(IEnumerable<RelicModel> source)
    {
        return ApplyDarvGlobalInclusions(ApplyDarvPoolExclusions(source))
            .GroupBy(r => r.Id)
            .Select(g => g.First())
            .ToList();
    }

    internal static IReadOnlyList<RelicModel> GetCandidatesForPlayer(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        if (IsIroncladLike(owner))
        {
            var allowed = GetIroncladAllowedIds();
            return NormalizeDarvPool(GetIroncladDarvBossCandidates().Where(r => allowed.Contains(r.Id)));
        }

        string colorKey = ResolveCharacterColorKey(owner);
        IEnumerable<RelicModel> sharedAncients = ModelDb.RelicPool<ClassicSharedRelicPool>().AllRelics
            .Where(r => r.Rarity == RelicRarity.Ancient);

        IEnumerable<RelicModel> characterAncients = GetCharacterAncients(colorKey);

        return NormalizeDarvPool(sharedAncients.Concat(characterAncients));
    }

    internal static IReadOnlyList<RelicModel> GetCandidatesForUnknownOwner()
    {
        return NormalizeDarvPool(ModelDb.RelicPool<ClassicSharedRelicPool>().AllRelics
            .Where(r => r.Rarity == RelicRarity.Ancient));
    }

    internal static IReadOnlyList<RelicModel> GetAllCandidatesForCollection()
    {
        return NormalizeDarvPool(ModelDb.RelicPool<ClassicSharedRelicPool>().AllRelics
            .Concat(ModelDb.RelicPool<ClassicIroncladRelicPool>().AllRelics)
            .Concat(ModelDb.RelicPool<ClassicSilentRelicPool>().AllRelics)
            .Concat(ModelDb.RelicPool<ClassicDefectRelicPool>().AllRelics)
            .Where(r => r.Rarity == RelicRarity.Ancient));
    }

    private static IEnumerable<RelicModel> GetCharacterAncients(string colorKey)
    {
        IEnumerable<RelicModel> pool = colorKey switch
        {
            "ironclad" => ModelDb.RelicPool<ClassicIroncladRelicPool>().AllRelics,
            "silent" => ModelDb.RelicPool<ClassicSilentRelicPool>().AllRelics,
            "defect" => ModelDb.RelicPool<ClassicDefectRelicPool>().AllRelics,
            _ => Enumerable.Empty<RelicModel>()
        };

        return pool.Where(r => r.Rarity == RelicRarity.Ancient);
    }

    private static string ResolveCharacterColorKey(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        string? byPool = owner.Character?.RelicPool?.EnergyColorName;
        if (!string.IsNullOrWhiteSpace(byPool))
            return byPool.Trim().ToLowerInvariant();

        string? id = owner.Character?.Id.Entry;
        if (string.Equals(id, "Ironclad", System.StringComparison.OrdinalIgnoreCase)) return "ironclad";
        if (string.Equals(id, "Silent", System.StringComparison.OrdinalIgnoreCase)) return "silent";
        if (string.Equals(id, "Defect", System.StringComparison.OrdinalIgnoreCase)) return "defect";
        return string.Empty;
    }

    private static IEnumerable<RelicModel> GetIroncladDarvBossCandidates()
    {
        // STS1 rule target for Ironclad: Boss relics with color empty or Red.
        return new RelicModel[]
        {
            // Colorless Boss relics (empty color in sts1export)
            ModelDb.Relic<Astrolabe>(),
            ModelDb.Relic<BlackStar>(),
            ModelDb.Relic<CallingBell>(),
            ModelDb.Relic<CoffeeDripperRelic>(),
            ModelDb.Relic<CursedKeyRelic>(),
            ModelDb.Relic<Ectoplasm>(),
            ModelDb.Relic<EmptyCage>(),
            ModelDb.Relic<FusionHammerRelic>(),
            ModelDb.Relic<PandorasBox>(),
            ModelDb.Relic<PhilosophersStone>(),
            ModelDb.Relic<RunicDomeRelic>(),
            ModelDb.Relic<RunicPyramid>(),
            ModelDb.Relic<SacredBarkRelic>(),
            ModelDb.Relic<SlaversCollarRelic>(),
            ModelDb.Relic<SneckoEye>(),
            ModelDb.Relic<Sozu>(),
            ModelDb.Relic<TinyHouseRelic>(),
            ModelDb.Relic<VelvetChoker>(),
            ModelDb.Relic<BustedCrownRelic>(),
            ModelDb.Relic<TouchOfOrobas>(),

            // Red Boss relics
            ModelDb.Relic<MarkOfPain>(),
            ModelDb.Relic<RunicCubeRelic>(),
        };
    }

    internal static bool IsIroncladLike(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        if (owner.Character is Ironclad)
            return true;

        if (owner.Character?.Id == ModelDb.Character<Ironclad>().Id)
            return true;

        return string.Equals(owner.Character?.RelicPool?.EnergyColorName, "ironclad", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static IReadOnlySet<ModelId> GetIroncladAllowedIds()
    {
        return GetIroncladDarvBossCandidates().Select(r => r.Id).ToHashSet();
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
        if (!ClassicConfig.ReplaceAncientsWithDarv)
            return true;

        var owner = __instance.Owner;
        List<RelicModel> pool = owner == null
            ? ClassicDarvRelicPoolHelper.GetCandidatesForUnknownOwner().ToList()
            : ClassicDarvRelicPoolHelper.GetCandidatesForPlayer(owner).ToList();

        // Uniform sample without replacement from the full eligible set.
        if (owner != null)
            owner.PlayerRng.Rewards.Shuffle(pool);

        var picks = pool.Take(3).DistinctBy(r => r.Id).ToList();

        var options = new List<EventOption>(picks.Count);
        foreach (var relic in picks)
        {
            options.Add(CreateSafeRelicOption(__instance, relic));
        }

        __result = options;
        return false;
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

        foreach (var relic in ClassicDarvRelicPoolHelper.GetAllCandidatesForCollection())
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
}

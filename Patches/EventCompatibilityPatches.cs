using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ClassicModeMod;

/// <summary>
/// Fixes event option generation when Classic/Hybrid pools replace base card pools.
/// </summary>
[HarmonyPatch(typeof(ColorfulPhilosophers), "GenerateInitialOptions")]
internal static class ColorfulPhilosophersClassicPoolFixPatch
{
    private static readonly MethodInfo? OfferRewardsMethod =
        AccessTools.Method(typeof(ColorfulPhilosophers), "OfferRewards");

    static bool Prefix(ColorfulPhilosophers __instance, ref IReadOnlyList<EventOption> __result)
    {
        if (!ClassicConfig.ClassicCards && !ClassicConfig.ClassicHybrid)
            return true;

        if (OfferRewardsMethod == null)
            return true;

        var owner = __instance.Owner;
        if (owner?.Character == null)
            return true;

        var options = new List<EventOption>();
        var ownerCharacterId = owner.Character.Id;

        foreach (var character in owner.UnlockState.Characters)
        {
            if (character.Id == ownerCharacterId)
                continue;

            var pool = character.CardPool;
            options.Add(new EventOption(
                __instance,
                () => InvokeOfferRewards(__instance, pool),
                "COLORFUL_PHILOSOPHERS.pages.INITIAL.options." + pool.EnergyColorName.ToUpperInvariant()));
        }

        var optionCount = Math.Min(3, options.Count);
        while (options.Count > optionCount)
            options.RemoveAt(__instance.Rng.NextInt(options.Count));

        __result = options;
        return false;
    }

    private static Task InvokeOfferRewards(ColorfulPhilosophers instance, CardPoolModel pool)
    {
        var task = OfferRewardsMethod?.Invoke(instance, new object[] { pool }) as Task;
        return task ?? Task.CompletedTask;
    }
}

/// <summary>
/// Makes Archaic Tooth recognize STS1 classic starter cards and map them to the
/// same transcendence outcomes as their base STS2 counterparts.
/// </summary>
[HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceStarterCard")]
internal static class ArchaicToothClassicStarterDetectPatch
{
    static void Postfix(MegaCrit.Sts2.Core.Entities.Players.Player player, ref CardModel? __result)
    {
        if (__result != null || !ClassicConfig.ClassicCards)
            return;

        try
        {
            __result = player.Deck.Cards.FirstOrDefault(c =>
                c is Bash_C or Neutralize_C or Dualcast_C);
        }
        catch
        {
            // Keep base behavior if anything unexpected happens at runtime.
        }
    }
}

[HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")]
internal static class ArchaicToothClassicStarterTransformPatch
{
    static bool Prefix(CardModel starterCard, ref CardModel __result)
    {
        if (!ClassicConfig.ClassicCards)
            return true;

        var target = GetClassicTranscendenceTargetByType(starterCard);
        if (target == null)
            return true;

        var owner = starterCard.Owner;
        if (owner == null)
            return true;

        try
        {
            var transformed = owner.RunState.CreateCard(target, owner);

            if (starterCard.IsUpgraded)
                CardCmd.Upgrade(transformed);

            if (starterCard.Enchantment != null)
            {
                var enchantment = (EnchantmentModel)starterCard.Enchantment.MutableClone();
                CardCmd.Enchant(enchantment, transformed, enchantment.Amount);
            }

            __result = transformed;
            return false;
        }
        catch
        {
            // Fall back to vanilla method if custom mapping fails.
            return true;
        }
    }

    private static CardModel? GetClassicTranscendenceTargetByType(CardModel starterCard)
    {
        if (starterCard is Bash_C)
            return ModelDb.Card<Break>();

        if (starterCard is Neutralize_C)
            return ModelDb.Card<Suppress>();

        if (starterCard is Dualcast_C)
            return ModelDb.Card<Quadcast>();

        return null;
    }
}
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Runs;

namespace ClassicModeMod;

/// <summary>
/// Preserve reward filter semantics when CharacterCards augments card rewards.
/// Vanilla appends the entire target pool and can drop event constraints such as
/// "rare power" when modifiers are chained.
/// </summary>
[HarmonyPatch(typeof(CharacterCards), nameof(CharacterCards.ModifyCardRewardCreationOptions))]
internal static class CharacterCardsEventRewardPatch
{
    static bool Prefix(CharacterCards __instance, Player player, CardCreationOptions options, ref CardCreationOptions __result)
    {
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
        {
            __result = options;
            return false;
        }

        var baseCards = options.GetPossibleCards(player).ToArray();
        var targetCharacterCards = ModelDb.GetById<CharacterModel>(__instance.CharacterModel)
            .CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);

        if (options.CardPoolFilter != null)
        {
            targetCharacterCards = targetCharacterCards.Where(options.CardPoolFilter);
        }
        else if (baseCards.Length > 0)
        {
            // If an earlier modifier has already materialized a custom pool,
            // infer constraints from that pool to keep event intent intact.
            var allowedRarities = baseCards.Select(c => c.Rarity).Distinct().ToHashSet();
            var allowedTypes = baseCards.Select(c => c.Type).Distinct().ToHashSet();

            targetCharacterCards = targetCharacterCards.Where(c =>
                allowedRarities.Contains(c.Rarity) &&
                allowedTypes.Contains(c.Type));
        }

        var merged = baseCards
            .Concat(targetCharacterCards)
            .DistinctBy(c => c.Id)
            .ToArray();

        if (merged.Length == 0)
        {
            __result = options;
            return false;
        }

        var singleRarity = merged.Select(c => c.Rarity).Distinct().Count() == 1;
        var rarityOdds = singleRarity ? CardRarityOddsType.Uniform : options.RarityOdds;

        __result = options.WithCustomPool(merged, rarityOdds);
        return false;
    }
}

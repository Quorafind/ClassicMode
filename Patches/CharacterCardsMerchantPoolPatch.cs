using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Modifiers;

namespace ClassicModeMod;

/// <summary>
/// CharacterCards merchant merge in vanilla relies on strict pool reference equality.
/// With classic/hybrid pool swaps, that check can fail and skip the merge entirely.
/// </summary>
[HarmonyPatch(typeof(CharacterCards), nameof(CharacterCards.ModifyMerchantCardPool))]
internal static class CharacterCardsMerchantPoolPatch
{
    static void Postfix(CharacterCards __instance, Player player, ref IEnumerable<CardModel> __result)
    {
        if (!ClassicConfig.ClassicCards && !ClassicConfig.ClassicHybrid)
            return;

        var source = (__result ?? Enumerable.Empty<CardModel>()).ToList();
        if (source.Count == 0)
            return;

        // Do not touch pure colorless candidate lists. Mixed lists should still
        // receive CharacterCards additions for their non-colorless portion.
        if (source.All(c => c.Pool is ColorlessCardPool))
            return;

        var targetCharacter = ModelDb.GetById<CharacterModel>(__instance.CharacterModel);
        var targetCards = targetCharacter.CardPool.GetUnlockedCards(
            player.UnlockState,
            player.RunState.CardMultiplayerConstraint);

        __result = source
            .Concat(targetCards)
            .DistinctBy(c => c.Id)
            .ToArray();
    }
}

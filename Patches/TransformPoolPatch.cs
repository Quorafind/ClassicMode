using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

/// <summary>
/// Preserve card UI identity (base pool colors) while letting transforms follow
/// the active configured character pool for classic cards.
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDefaultTransformationOptions))]
internal static class ClassicTransformPoolPatch
{
    static void Postfix(CardModel original, bool isInCombat, ref IEnumerable<CardModel> __result)
    {
        if (original is not (ClassicIroncladCard or ClassicSilentCard or ClassicDefectCard))
            return;

        var owner = original.Owner;
        if (owner?.Character == null)
            return;

        var pool = owner.Character.CardPool;
        var options = pool.GetUnlockedCards(owner.UnlockState, original.RunState.CardMultiplayerConstraint);

        // Match base transform filtering behavior for normal cards.
        if (original.Rarity != CardRarity.Event && original.Rarity != CardRarity.Ancient)
        {
            options = options.Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare);
        }

        if (isInCombat)
        {
            options = options.Where(c => c.CanBeGeneratedInCombat);
        }

        var filtered = options.Where(c => c.Id != original.Id).ToArray();
        if (filtered.Length > 0)
        {
            __result = filtered;
        }
    }
}

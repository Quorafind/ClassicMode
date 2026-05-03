using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Runs;

namespace ClassicModeMod;

internal static class FrozenEyeDrawPileOrderHelper
{
    private static readonly AccessTools.FieldRef<NCardPileScreen, NCardGrid> GridRef =
        AccessTools.FieldRefAccess<NCardPileScreen, NCardGrid>("_grid");

    public static bool ShouldUseOrderedDrawPile(CardPile pile)
    {
        if (pile.Type != PileType.Draw)
            return false;

        Player? owner = GetOwner(pile);
        return owner?.GetRelic<FrozenEyeRelic>() != null;
    }

    public static void ApplyOrderedDrawPile(NCardPileScreen screen)
    {
        if (!ShouldUseOrderedDrawPile(screen.Pile))
            return;

        NCardGrid grid = GridRef.Invoke(screen);
        if (grid == null)
            return;

        List<CardModel> cards = screen.Pile.Cards.ToList();
        grid.SetCards(cards, PileType.Draw, [SortingOrders.Ascending]);
    }

    public static IReadOnlyList<CardModel> GetOrderedDrawPileCards(CardPile pile)
    {
        return pile.Cards.ToList();
    }

    private static Player? GetOwner(CardPile pile)
    {
        RunState? state = RunManager.Instance.DebugOnlyGetState();
        if (state == null)
            return null;

        foreach (Player player in state.Players)
        {
            CardPile? drawPile = PileType.Draw.GetPile(player);
            if (ReferenceEquals(drawPile, pile))
                return player;
        }

        return null;
    }
}

[HarmonyPatch(typeof(NCardPileScreen), "OnPileContentsChanged")]
internal static class FrozenEyeOrderedDrawPilePatch
{
    [HarmonyPrefix]
    static bool Prefix(NCardPileScreen __instance)
    {
        try
        {
            if (!FrozenEyeDrawPileOrderHelper.ShouldUseOrderedDrawPile(__instance.Pile))
                return true;

            FrozenEyeDrawPileOrderHelper.ApplyOrderedDrawPile(__instance);
            return false;
        }
        catch
        {
            // Fall back to base behavior if anything goes wrong.
            return true;
        }
    }
}

[HarmonyPatch(typeof(NCardPileScreen), "_Ready")]
internal static class FrozenEyeOrderedDrawPileReadyPatch
{
    [HarmonyPostfix]
    static void Postfix(NCardPileScreen __instance)
    {
        try
        {
            FrozenEyeDrawPileOrderHelper.ApplyOrderedDrawPile(__instance);
        }
        catch
        {
            // Ignore and keep default UI behavior.
        }
    }
}

[HarmonyPatch(typeof(NCardPileScreen), "ShowScreen")]
internal static class FrozenEyeOrderedDrawPileShowScreenPatch
{
    [HarmonyPostfix]
    static void Postfix(CardPile pile, NCardPileScreen __result)
    {
        if (__result == null)
            return;

        try
        {
            if (!FrozenEyeDrawPileOrderHelper.ShouldUseOrderedDrawPile(pile))
                return;

            FrozenEyeDrawPileOrderHelper.ApplyOrderedDrawPile(__result);
        }
        catch
        {
            // Ignore and keep default UI behavior.
        }
    }
}

[HarmonyPatch(typeof(NCardGrid), "SetCards")]
internal static class FrozenEyeOrderedDrawPileGridPatch
{
    [HarmonyPriority(0)]
    [HarmonyPrefix]
    static void Prefix(ref IReadOnlyList<CardModel> cardsToDisplay, PileType pileType, ref List<SortingOrders> sortingPriority)
    {
        try
        {
            if (pileType != PileType.Draw)
                return;

            if (NCapstoneContainer.Instance?.CurrentCapstoneScreen is not NCardPileScreen pileScreen)
                return;

            if (!FrozenEyeDrawPileOrderHelper.ShouldUseOrderedDrawPile(pileScreen.Pile))
                return;

            cardsToDisplay = FrozenEyeDrawPileOrderHelper.GetOrderedDrawPileCards(pileScreen.Pile);
            sortingPriority = [SortingOrders.Ascending];
        }
        catch
        {
            // Ignore and keep default UI behavior.
        }
    }
}

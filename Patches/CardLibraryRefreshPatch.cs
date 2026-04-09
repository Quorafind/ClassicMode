using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace ClassicModeMod;

/// <summary>
/// Forces the encyclopedia / card library grid to rebuild its local card
/// list every time it's opened.
///
/// Why this is needed:
///   NCardLibraryGrid populates its private `_allCards` list ONCE in _Ready()
///   from `ModelDb.AllCards`. After that point, the grid never re-reads
///   ModelDb. Toggling Hybrid / Classic in the character-select screen swaps
///   the active pool and (via HybridPoolCache) invalidates ModelDb's lazy
///   `_allCards` cache, but the grid's local copy is frozen at boot.
///
///   Without this patch, the encyclopedia keeps showing whatever pool was
///   active when the game first booted, even though gameplay correctly uses
///   the new pool.
///
/// Approach: **Prefix** on `NCardLibrary.OnSubmenuOpened`. Must run before
/// the base method, because the base ends with `UpdateFilter()` → `DisplayCards`
/// → `_grid.FilterCards(_filter, _sortingPriority)`, which reads the grid's
/// `_allCards` list. If we refresh after, the grid has already rendered the
/// stale set — user sees no change. Prefix runs first, then the base's
/// filter/display sees the rebuilt list.
/// </summary>
[HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary.OnSubmenuOpened))]
internal static class CardLibraryRefreshPatch
{
    private static readonly FieldInfo? _gridField =
        typeof(NCardLibrary).GetField("_grid", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? _allCardsField =
        typeof(NCardLibraryGrid).GetField("_allCards", BindingFlags.Instance | BindingFlags.NonPublic);

    static void Prefix(NCardLibrary __instance)
    {
        try
        {
            // Ensure ModelDb card-pool snapshots are rebuilt for current toggles
            // before we repopulate the grid-local caches.
            HybridPoolCache.InvalidateAll();

            if (_gridField?.GetValue(__instance) is not NCardLibraryGrid grid)
            {
                Log.Warn("[ClassicMode] CardLibrary refresh: grid field not found");
                return;
            }
            if (_allCardsField?.GetValue(grid) is not List<CardModel> allCards)
            {
                Log.Warn("[ClassicMode] CardLibrary refresh: grid._allCards field not found");
                return;
            }

            int before = allCards.Count;
            allCards.Clear();
            foreach (var card in ModelDb.AllCards)
            {
                if (card.ShouldShowInCardLibrary)
                    allCards.Add(card);
            }

            // Match the original InitialSorter intent loosely: rarity first,
            // then ID. The user's chosen sort priority takes over after this
            // initial pass anyway.
            allCards.Sort((x, y) =>
            {
                int cmp = x.Rarity.CompareTo(y.Rarity);
                if (cmp != 0) return cmp;
                return x.Id.CompareTo(y.Id);
            });

            // Keep lock/seen state aligned with the rebuilt pool list.
            grid.RefreshVisibility();

            Log.Info($"[ClassicMode] CardLibrary refreshed: {before} → {allCards.Count} cards");
        }
        catch (Exception ex)
        {
            Log.Error($"[ClassicMode] Failed to refresh card library grid: {ex.Message}");
        }
    }
}

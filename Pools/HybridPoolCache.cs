using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

/// <summary>
/// Drops the lazy caches that go stale when a pool-affecting toggle changes.
///
/// Two layers need clearing:
///
/// 1. **Hybrid pool instances** — `CardPoolModel._allCards` / `_allCardIds`
///    and `RelicPoolModel._relics` / `_allRelicIds` cache the merged set on
///    first access. Cleared so that the next access remerges with the new
///    HybridDedupe setting.
///
/// 2. **Static `ModelDb` caches** — `_allCards`, `_allCardPools`,
///    `_allCharacterCardPools`, `_allRelics`, `_allCharacterRelicPools`. These
///    snapshot what `Ironclad.CardPool` / `Silent.CardPool` / `Defect.CardPool`
///    returned at game-boot. Without clearing them, toggling Hybrid (or
///    Classic) in the character-select screen leaves the encyclopedia and any
///    other consumer of `ModelDb.AllCards` looking at the old set.
///
/// CardPoolModel.AllCards is virtual but we deliberately don't override it —
/// instead we clear the base class's private cache fields via reflection so
/// both card and relic pools (whose AllRelics is non-virtual) share one
/// invalidation path.
/// </summary>
internal static class HybridPoolCache
{
    // ── Hybrid-pool instance caches ──
    private static readonly FieldInfo? _allCardsField =
        typeof(CardPoolModel).GetField("_allCards", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _allCardIdsField =
        typeof(CardPoolModel).GetField("_allCardIds", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _relicsField =
        typeof(RelicPoolModel).GetField("_relics", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? _allRelicIdsField =
        typeof(RelicPoolModel).GetField("_allRelicIds", BindingFlags.Instance | BindingFlags.NonPublic);

    // ── Static ModelDb caches ──
    private static readonly FieldInfo? _modelDbAllCardsField =
        typeof(ModelDb).GetField("_allCards", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? _modelDbAllCardPoolsField =
        typeof(ModelDb).GetField("_allCardPools", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? _modelDbAllCharacterCardPoolsField =
        typeof(ModelDb).GetField("_allCharacterCardPools", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? _modelDbAllRelicsField =
        typeof(ModelDb).GetField("_allRelics", BindingFlags.Static | BindingFlags.NonPublic);
    private static readonly FieldInfo? _modelDbAllCharacterRelicPoolsField =
        typeof(ModelDb).GetField("_allCharacterRelicPools", BindingFlags.Static | BindingFlags.NonPublic);

    internal static void InvalidateAll()
    {
        try
        {
            // Hybrid card pool instances
            ClearCardPool(ModelDb.CardPool<HybridIroncladCardPool>());
            ClearCardPool(ModelDb.CardPool<HybridSilentCardPool>());
            ClearCardPool(ModelDb.CardPool<HybridDefectCardPool>());
            ClearCardPool(ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.ColorlessCardPool>());

            // Hybrid relic pool instances
            ClearRelicPool(ModelDb.RelicPool<HybridIroncladRelicPool>());
            ClearRelicPool(ModelDb.RelicPool<HybridSilentRelicPool>());
            ClearRelicPool(ModelDb.RelicPool<HybridDefectRelicPool>());

            // Static ModelDb caches — these freeze at boot, so without clearing
            // them the encyclopedia / card library shows stale data after any
            // pool-affecting toggle.
            _modelDbAllCardsField?.SetValue(null, null);
            _modelDbAllCardPoolsField?.SetValue(null, null);
            _modelDbAllCharacterCardPoolsField?.SetValue(null, null);
            _modelDbAllRelicsField?.SetValue(null, null);
            _modelDbAllCharacterRelicPoolsField?.SetValue(null, null);
        }
        catch (Exception ex)
        {
            Log.Error($"[ClassicMode] Failed to invalidate pool caches: {ex.Message}");
        }
    }

    private static void ClearCardPool(CardPoolModel pool)
    {
        _allCardsField?.SetValue(pool, null);
        _allCardIdsField?.SetValue(pool, null);
    }

    private static void ClearRelicPool(RelicPoolModel pool)
    {
        _relicsField?.SetValue(pool, null);
        _allRelicIdsField?.SetValue(pool, null);
    }
}

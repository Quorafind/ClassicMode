using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace ClassicModeMod;

// ── Starting Deck Patches ──

[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingDeck), MethodType.Getter)]
internal static class IroncladStartingDeckPatch
{
    static bool Prefix(ref IEnumerable<CardModel> __result)
    {
        if (!ClassicConfig.ClassicCards) return true;
        // STS1 Ironclad: 5x Strike, 4x Defend, 1x Bash
        __result = new CardModel[]
        {
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<DefendIronclad_C>(),
            ModelDb.Card<DefendIronclad_C>(),
            ModelDb.Card<DefendIronclad_C>(),
            ModelDb.Card<DefendIronclad_C>(),
            ModelDb.Card<Bash_C>()
        };
        return false;
    }
}

[HarmonyPatch(typeof(Silent), nameof(Silent.StartingDeck), MethodType.Getter)]
internal static class SilentStartingDeckPatch
{
    static bool Prefix(ref IEnumerable<CardModel> __result)
    {
        if (!ClassicConfig.ClassicCards) return true;
        // STS1 Silent: 5x Strike, 5x Defend, 1x Neutralize, 1x Survivor
        __result = new CardModel[]
        {
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<StrikeSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<DefendSilent_C>(),
            ModelDb.Card<Neutralize_C>(),
            ModelDb.Card<Survivor_C>()
        };
        return false;
    }
}

[HarmonyPatch(typeof(Defect), nameof(Defect.StartingDeck), MethodType.Getter)]
internal static class DefectStartingDeckPatch
{
    static bool Prefix(ref IEnumerable<CardModel> __result)
    {
        if (!ClassicConfig.ClassicCards) return true;
        // STS1 Defect: 4x Strike, 4x Defend, 1x Zap, 1x Dualcast
        __result = new CardModel[]
        {
            ModelDb.Card<StrikeDefect_C>(),
            ModelDb.Card<StrikeDefect_C>(),
            ModelDb.Card<StrikeDefect_C>(),
            ModelDb.Card<StrikeDefect_C>(),
            ModelDb.Card<DefendDefect_C>(),
            ModelDb.Card<DefendDefect_C>(),
            ModelDb.Card<DefendDefect_C>(),
            ModelDb.Card<DefendDefect_C>(),
            ModelDb.Card<Zap_C>(),
            ModelDb.Card<Dualcast_C>()
        };
        return false;
    }
}

// ── Starting Relic Patches ──

[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.StartingRelics), MethodType.Getter)]
internal static class IroncladStartingRelicsPatch
{
    static bool Prefix(ref IReadOnlyList<RelicModel> __result)
    {
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = new[] { ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.BurningBlood>() };
        return false;
    }
}

[HarmonyPatch(typeof(Silent), nameof(Silent.StartingRelics), MethodType.Getter)]
internal static class SilentStartingRelicsPatch
{
    static bool Prefix(ref IReadOnlyList<RelicModel> __result)
    {
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = new[] { ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.RingOfTheSnake>() };
        return false;
    }
}

[HarmonyPatch(typeof(Defect), nameof(Defect.StartingRelics), MethodType.Getter)]
internal static class DefectStartingRelicsPatch
{
    static bool Prefix(ref IReadOnlyList<RelicModel> __result)
    {
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = new[] { ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.CrackedCore>() };
        return false;
    }
}

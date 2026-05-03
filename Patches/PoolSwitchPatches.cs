using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Unlocks;

namespace ClassicModeMod;

// ── Card Pool Patches ──
// Hybrid takes precedence over Classic. If neither is on, the base getter runs.

[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.CardPool), MethodType.Getter)]
internal static class IroncladCardPoolPatch
{
    static bool Prefix(ref CardPoolModel __result)
    {
        if (ClassicConfig.ClassicHybrid)
        {
            __result = ModelDb.CardPool<HybridIroncladCardPool>();
            return false;
        }
        if (!ClassicConfig.ClassicCards) return true;
        __result = ModelDb.CardPool<ClassicIroncladCardPool>();
        return false;
    }
}

[HarmonyPatch(typeof(Silent), nameof(Silent.CardPool), MethodType.Getter)]
internal static class SilentCardPoolPatch
{
    static bool Prefix(ref CardPoolModel __result)
    {
        if (ClassicConfig.ClassicHybrid)
        {
            __result = ModelDb.CardPool<HybridSilentCardPool>();
            return false;
        }
        if (!ClassicConfig.ClassicCards) return true;
        __result = ModelDb.CardPool<ClassicSilentCardPool>();
        return false;
    }
}

[HarmonyPatch(typeof(Defect), nameof(Defect.CardPool), MethodType.Getter)]
internal static class DefectCardPoolPatch
{
    static bool Prefix(ref CardPoolModel __result)
    {
        if (ClassicConfig.ClassicHybrid)
        {
            __result = ModelDb.CardPool<HybridDefectCardPool>();
            return false;
        }
        if (!ClassicConfig.ClassicCards) return true;
        __result = ModelDb.CardPool<ClassicDefectCardPool>();
        return false;
    }
}

// ── Relic Pool Patches ──

[HarmonyPatch(typeof(Ironclad), nameof(Ironclad.RelicPool), MethodType.Getter)]
internal static class IroncladRelicPoolPatch
{
    static bool Prefix(ref RelicPoolModel __result)
    {
        if (ClassicConfig.AddClassicRelics)
        {
            __result = ModelDb.RelicPool<HybridIroncladRelicPool>();
            return false;
        }
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = ModelDb.RelicPool<ClassicIroncladRelicPool>();
        return false;
    }
}

[HarmonyPatch(typeof(Silent), nameof(Silent.RelicPool), MethodType.Getter)]
internal static class SilentRelicPoolPatch
{
    static bool Prefix(ref RelicPoolModel __result)
    {
        if (ClassicConfig.AddClassicRelics)
        {
            __result = ModelDb.RelicPool<HybridSilentRelicPool>();
            return false;
        }
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = ModelDb.RelicPool<ClassicSilentRelicPool>();
        return false;
    }
}

[HarmonyPatch(typeof(Defect), nameof(Defect.RelicPool), MethodType.Getter)]
internal static class DefectRelicPoolPatch
{
    static bool Prefix(ref RelicPoolModel __result)
    {
        if (ClassicConfig.AddClassicRelics)
        {
            __result = ModelDb.RelicPool<HybridDefectRelicPool>();
            return false;
        }
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = ModelDb.RelicPool<ClassicDefectRelicPool>();
        return false;
    }
}

[HarmonyPatch(typeof(SharedRelicPool), nameof(SharedRelicPool.GetUnlockedRelics))]
internal static class SharedRelicPoolPatch
{
    static bool Prefix(UnlockState unlockState, ref IEnumerable<RelicModel> __result)
    {
        if (ClassicConfig.AddClassicRelics)
        {
            __result = ModelDb.RelicPool<HybridSharedRelicPool>().GetUnlockedRelics(unlockState);
            return false;
        }
        if (!ClassicConfig.OnlyClassicRelics) return true;
        __result = ModelDb.RelicPool<ClassicSharedRelicPool>().GetUnlockedRelics(unlockState);
        return false;
    }
}

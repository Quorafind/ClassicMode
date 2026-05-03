using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ClassicModeMod;

internal static class PowerIconPathHelper
{
    internal static bool TryGetCustomPowerIconPath(PowerModel power, out string path)
    {
        path = string.Empty;
        if (power?.Id == null)
            return false;

        if (power is ClassicPlatedArmorPower_C)
        {
            path = ModelDb.Power<PlatingPower>().IconPath;
            return true;
        }

        // Flex can be represented as a temporary strength power instance.
        // Keep this compatibility branch to avoid regressions from pure ID-based lookup.
        if (power is FlexPower || (power is TemporaryStrengthPower tsp && tsp.OriginModel is Flex_C))
        {
            var flexPath = ImageHelper.GetImagePath("powers/flex_power.tres");
            if (ResourceLoader.Exists(flexPath, ""))
            {
                path = flexPath;
                return true;
            }
        }

        // prepare_assets.py emits power icons as images/powers/<power_id_lower>.tres
        var slug = power.Id.Entry.ToLowerInvariant();
        var candidate = ImageHelper.GetImagePath($"powers/{slug}.tres");
        if (!ResourceLoader.Exists(candidate, ""))
            return false;

        path = candidate;
        return true;
    }

    internal static bool TryGetCustomPowerBigIconPath(PowerModel power, out string path)
    {
        path = string.Empty;
        if (power?.Id == null)
            return false;

        if (power is ClassicPlatedArmorPower_C)
        {
            path = ModelDb.Power<PlatingPower>().ResolvedBigIconPath;
            return true;
        }

        var slug = power.Id.Entry.ToLowerInvariant();
        var bigTres = ImageHelper.GetImagePath($"powers/{slug}_big.tres");
        if (ResourceLoader.Exists(bigTres, ""))
        {
            path = bigTres;
            return true;
        }

        var smallTres = ImageHelper.GetImagePath($"powers/{slug}.tres");
        if (ResourceLoader.Exists(smallTres, ""))
        {
            path = smallTres;
            return true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_PackedIconPath")]
internal static class PowerIconPathPatch
{
    static void Postfix(PowerModel __instance, ref string __result)
    {
        if (PowerIconPathHelper.TryGetCustomPowerIconPath(__instance, out var path))
        {
            __result = path;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_IconPath")]
internal static class PowerIconPathDirectPatch
{
    static void Postfix(PowerModel __instance, ref string __result)
    {
        if (PowerIconPathHelper.TryGetCustomPowerIconPath(__instance, out var path))
        {
            __result = path;
        }
    }
}

[HarmonyPatch(typeof(PowerModel), "get_Icon")]
internal static class PowerIconDirectPatch
{
    static bool Prefix(PowerModel __instance, ref Texture2D __result)
    {
        if (PowerIconPathHelper.TryGetCustomPowerIconPath(__instance, out var path))
        {
            var icon = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
            if (icon != null)
            {
                __result = icon;
                return false;
            }
        }

        return true;
    }
}

[HarmonyPatch(typeof(PowerModel), "get_ResolvedBigIconPath")]
internal static class PowerBigIconPathPatch
{
    static void Postfix(PowerModel __instance, ref string __result)
    {
        if (PowerIconPathHelper.TryGetCustomPowerBigIconPath(__instance, out var path))
        {
            __result = path;
        }
    }
}

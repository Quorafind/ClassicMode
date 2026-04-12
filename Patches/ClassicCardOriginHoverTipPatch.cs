using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

/// <summary>
/// Optionally adds a small origin hover tip to all STS1 cards so they are
/// easy to identify in mixed/hybrid card pools.
/// </summary>
[HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
internal static class ClassicCardOriginHoverTipPatch
{
    private const string ClassicOriginTipId = "classic_mode.card_origin.sts1";

    static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        try
        {
            if (!ClassicConfig.MarkClassicCardOrigin)
                return;

            if (__instance is not (ClassicIroncladCard or ClassicSilentCard or ClassicDefectCard or ClassicColorlessCard))
                return;

            var descLoc = new LocString("cards", "CLASSIC_ORIGIN_HOVERTIP.description");
            var titleLoc = new LocString("cards", "CLASSIC_ORIGIN_HOVERTIP.title");
            var isZh = LocManager.Instance != null && LocManager.Instance.Language == "zhs";
            var desc = descLoc.Exists()
                ? descLoc.GetFormattedText()
                : (isZh ? "这张卡来自《杀戮尖塔》一代。" : "This card comes from Slay the Spire 1.");

            // If dedicated title loc is missing, use card title as fallback title.
            var tip = titleLoc.Exists() ? new HoverTip(titleLoc, desc) : new HoverTip(__instance.TitleLocString, desc);
            tip.Id = ClassicOriginTipId;

            __result = (__result ?? Enumerable.Empty<IHoverTip>()).Append(tip);
        }
        catch
        {
            // Keep card rendering resilient even if tip localization/resources are invalid.
        }
    }
}

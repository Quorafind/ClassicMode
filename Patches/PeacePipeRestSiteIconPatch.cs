using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace ClassicModeMod;

[HarmonyPatch(typeof(NRestSiteButton), "Reload")]
internal static class PeacePipeRestSiteIconPatch
{
    private static readonly AccessTools.FieldRef<NRestSiteButton, Godot.TextureRect> IconRef =
        AccessTools.FieldRefAccess<NRestSiteButton, Godot.TextureRect>("_icon");

    static void Postfix(NRestSiteButton __instance)
    {
        if (__instance.Option is not PeacePipeTokeRestSiteOption)
            return;

        var icon = IconRef(__instance);
        if (icon == null)
            return;

        icon.Texture = PeacePipeTokeRestSiteOption.GetCookOptionIcon();
    }
}


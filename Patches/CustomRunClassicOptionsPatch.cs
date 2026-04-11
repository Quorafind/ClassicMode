using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ClassicModeMod;

[HarmonyPatch(typeof(NCustomRunModifiersList), "GetAllModifiers")]
internal static class CustomRunAddClassicOptionsPatch
{
    static void Postfix(ref IEnumerable<ModifierModel> __result)
    {
        var list = (__result ?? Enumerable.Empty<ModifierModel>()).ToList();

        AppendIfMissing<ClassicCardsCustomModeModifier>(list);
        AppendIfMissing<ClassicRelicsCustomModeModifier>(list);
        AppendIfMissing<ClassicHybridCustomModeModifier>(list);
        AppendIfMissing<ClassicHybridDedupeCustomModeModifier>(list);

        __result = list;
    }

    private static void AppendIfMissing<T>(List<ModifierModel> list) where T : ModifierModel
    {
        if (list.Any(m => m.GetType() == typeof(T)))
            return;

        list.Add(ModelDb.Modifier<T>().ToMutable());
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), "_Ready")]
internal static class CustomRunClassicOptionsInitPatch
{
    static void Postfix(NCustomRunModifiersList __instance)
    {
        // Seed custom-run toggles from current config values for consistency.
        var tickboxes = CustomRunClassicOptionsRules.GetTickboxes(__instance);
        if (tickboxes.Count == 0)
            return;

        CustomRunClassicOptionsRules.Find<ClassicCardsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicCards);
        CustomRunClassicOptionsRules.Find<ClassicRelicsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicRelics);
        CustomRunClassicOptionsRules.Find<ClassicHybridCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicHybrid);
        CustomRunClassicOptionsRules.Find<ClassicHybridDedupeCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.HybridDedupe);

        CustomRunClassicOptionsRules.Apply(__instance, changed: null);
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), "AfterModifiersChanged")]
internal static class CustomRunClassicOptionsAfterChangedPatch
{
    static void Postfix(NCustomRunModifiersList __instance, NRunModifierTickbox tickbox)
    {
        CustomRunClassicOptionsRules.Apply(__instance, tickbox);
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), nameof(NCustomRunModifiersList.SyncModifierList))]
internal static class CustomRunClassicOptionsSyncPatch
{
    static void Postfix(NCustomRunModifiersList __instance)
    {
        CustomRunClassicOptionsRules.Apply(__instance, changed: null);
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), nameof(NCustomRunModifiersList.GetModifiersTickedOn))]
internal static class CustomRunClassicOptionsNormalizeSelectionPatch
{
    static void Postfix(ref List<ModifierModel> __result)
    {
        __result = CustomRunClassicOptionsRules.Normalize(__result);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.BeginRun))]
internal static class CustomRunClassicOptionsApplyToConfigPatch
{
    static void Prefix(NCustomRunScreen __instance, IReadOnlyList<ModifierModel> modifiers)
    {
        var normalized = CustomRunClassicOptionsRules.Normalize(modifiers);

        // Always apply from the synchronized modifier list; in multiplayer,
        // host is authoritative and clients follow the host's selections.
        ClassicConfig.ClassicCards = normalized.Any(m => m is ClassicCardsCustomModeModifier);
        ClassicConfig.ClassicRelics = normalized.Any(m => m is ClassicRelicsCustomModeModifier);
        ClassicConfig.ClassicHybrid = normalized.Any(m => m is ClassicHybridCustomModeModifier);
        ClassicConfig.HybridDedupe = normalized.Any(m => m is ClassicHybridDedupeCustomModeModifier);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.SelectCharacter))]
internal static class CustomRunClassicOptionsCharacterChangedPatch
{
    static void Postfix(NCustomRunScreen __instance, CharacterModel characterModel)
    {
        CustomRunClassicOptionsRules.ApplyCharacterGate(__instance, characterModel);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.OnSubmenuOpened))]
internal static class CustomRunClassicOptionsOpenedPatch
{
    static void Postfix(NCustomRunScreen __instance)
    {
        CustomRunClassicOptionsRules.ApplyCharacterGate(__instance, __instance.Lobby.LocalPlayer.character);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.PlayerChanged))]
internal static class CustomRunClassicOptionsPlayerChangedPatch
{
    static void Postfix(NCustomRunScreen __instance)
    {
        CustomRunClassicOptionsRules.ApplyCharacterGate(__instance, __instance.Lobby.LocalPlayer.character);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.PlayerConnected))]
internal static class CustomRunClassicOptionsPlayerConnectedPatch
{
    static void Postfix(NCustomRunScreen __instance)
    {
        CustomRunClassicOptionsRules.ApplyCharacterGate(__instance, __instance.Lobby.LocalPlayer.character);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.RemotePlayerDisconnected))]
internal static class CustomRunClassicOptionsRemotePlayerDisconnectedPatch
{
    static void Postfix(NCustomRunScreen __instance)
    {
        CustomRunClassicOptionsRules.ApplyCharacterGate(__instance, __instance.Lobby.LocalPlayer.character);
    }
}

internal static class CustomRunClassicOptionsRules
{
    private static readonly AccessTools.FieldRef<NCustomRunModifiersList, List<NRunModifierTickbox>> TickboxesRef =
        AccessTools.FieldRefAccess<NCustomRunModifiersList, List<NRunModifierTickbox>>("_modifierTickboxes");

    private static readonly AccessTools.FieldRef<NCustomRunModifiersList, MultiplayerUiMode> ModeRef =
        AccessTools.FieldRefAccess<NCustomRunModifiersList, MultiplayerUiMode>("_mode");

    private static readonly AccessTools.FieldRef<NCustomRunScreen, NCustomRunModifiersList> ModifiersListRef =
        AccessTools.FieldRefAccess<NCustomRunScreen, NCustomRunModifiersList>("_modifiersList");

    public static List<NRunModifierTickbox> GetTickboxes(NCustomRunModifiersList list)
    {
        return TickboxesRef(list) ?? [];
    }

    public static NRunModifierTickbox? Find<T>(IEnumerable<NRunModifierTickbox> tickboxes) where T : ModifierModel
    {
        return tickboxes.FirstOrDefault(t => t.Modifier?.GetType() == typeof(T));
    }

    public static void SetValue(this NRunModifierTickbox tickbox, bool value)
    {
        tickbox.IsTicked = value;
    }

    public static void Apply(NCustomRunModifiersList list, NRunModifierTickbox? changed)
    {
        var tickboxes = GetTickboxes(list);
        if (tickboxes.Count == 0)
            return;

        var localCanEdit = CanLocalPlayerEdit(list);

        var cards = Find<ClassicCardsCustomModeModifier>(tickboxes);
        var hybrid = Find<ClassicHybridCustomModeModifier>(tickboxes);
        var dedupe = Find<ClassicHybridDedupeCustomModeModifier>(tickboxes);

        // Keep behavior aligned with character-select toggles:
        // the newly toggled master mode wins, and dedupe depends on Hybrid.
        if (changed?.Modifier is ClassicHybridCustomModeModifier && (hybrid?.IsTicked ?? false))
        {
            if (cards != null) cards.IsTicked = false;
        }
        else if (changed?.Modifier is ClassicCardsCustomModeModifier && (cards?.IsTicked ?? false))
        {
            if (hybrid != null) hybrid.IsTicked = false;
        }

        // Safety net for externally synced lists: Hybrid takes precedence.
        if ((hybrid?.IsTicked ?? false) && (cards?.IsTicked ?? false))
        {
            cards!.IsTicked = false;
        }

        var hybridOn = hybrid?.IsTicked ?? false;
        if (dedupe != null)
        {
            if (!hybridOn)
            {
                dedupe.IsTicked = false;
                dedupe.Disable();
            }
            else if (!localCanEdit)
            {
                dedupe.Disable();
            }
            else
            {
                dedupe.Enable();
            }
        }
    }

    public static bool IsClassicEligibleForLobby(NCustomRunScreen screen, CharacterModel character)
    {
        _ = screen;
        _ = character;
        // Keep classic options visible for all characters.
        return true;
    }

    public static void ApplyCharacterGate(NCustomRunScreen screen, CharacterModel character)
    {
        var modifiersList = ModifiersListRef(screen);
        if (modifiersList == null)
            return;

        var tickboxes = GetTickboxes(modifiersList);
        if (tickboxes.Count == 0)
            return;

        var cards = Find<ClassicCardsCustomModeModifier>(tickboxes);
        var relics = Find<ClassicRelicsCustomModeModifier>(tickboxes);
        var hybrid = Find<ClassicHybridCustomModeModifier>(tickboxes);
        var dedupe = Find<ClassicHybridDedupeCustomModeModifier>(tickboxes);

        // Re-apply dependency/mutex states after re-enabling.
        Apply(modifiersList, changed: null);

        // Always show classic options for all characters, but only allow editing
        // from singleplayer/host. Clients stay read-only and follow host sync.
        var localCanEdit = CanLocalPlayerEdit(modifiersList);
        foreach (var t in new[] { cards, relics, hybrid, dedupe })
        {
            if (t == null) continue;
            if (localCanEdit)
                t.Enable();
            else
                t.Disable();
        }
    }

    private static bool CanLocalPlayerEdit(NCustomRunModifiersList list)
    {
        var mode = ModeRef(list);
        return mode is MultiplayerUiMode.Singleplayer or MultiplayerUiMode.Host;
    }

    public static List<ModifierModel> Normalize(IEnumerable<ModifierModel> source)
    {
        var list = (source ?? Enumerable.Empty<ModifierModel>()).ToList();

        var hasHybrid = list.Any(m => m is ClassicHybridCustomModeModifier);
        var hasCards = list.Any(m => m is ClassicCardsCustomModeModifier);

        if (hasHybrid && hasCards)
        {
            list.RemoveAll(m => m is ClassicCardsCustomModeModifier);
        }

        if (!list.Any(m => m is ClassicHybridCustomModeModifier))
        {
            list.RemoveAll(m => m is ClassicHybridDedupeCustomModeModifier);
        }

        return list;
    }
}

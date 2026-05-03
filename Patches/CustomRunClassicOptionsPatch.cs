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
        AppendIfMissing<ClassicHybridCustomModeModifier>(list);
        AppendIfMissing<ClassicHybridDedupeCustomModeModifier>(list);
        AppendIfMissing<ClassicColorlessCustomModeModifier>(list);
        AppendIfMissing<ClassicColorlessHybridCustomModeModifier>(list);
        AppendIfMissing<ClassicColorlessDedupeCustomModeModifier>(list);
        AppendIfMissing<ColorlessCardRewardsCustomModeModifier>(list);
        AppendIfMissing<ReplaceAncientsWithDarvCustomModeModifier>(list);
        AppendIfMissing<AddClassicRelicsCustomModeModifier>(list);
        AppendIfMissing<OnlyClassicRelicsCustomModeModifier>(list);

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
    static void Prefix(NCustomRunScreen __instance, ref IReadOnlyList<ModifierModel> modifiers)
    {
        var modifiersList = CustomRunClassicOptionsRules.GetModifiersList(__instance);
        if (modifiersList != null)
        {
            var snapshot = modifiersList.GetModifiersTickedOn();
            if (snapshot.Count > 0)
            {
                modifiers = snapshot;
            }
        }

        var normalized = CustomRunClassicOptionsRules.Normalize(modifiers);
        modifiers = normalized;

        // Always apply from the synchronized modifier list; in multiplayer,
        // host is authoritative and clients follow the host's selections.
        ClassicConfig.ClassicCards = normalized.Any(m => m is ClassicCardsCustomModeModifier);
        ClassicConfig.AddClassicRelics = normalized.Any(m => m is AddClassicRelicsCustomModeModifier);
        ClassicConfig.OnlyClassicRelics = normalized.Any(m => m is OnlyClassicRelicsCustomModeModifier)
            || normalized.Any(m => m is ClassicRelicsCustomModeModifier);
        ClassicConfig.ClassicHybrid = normalized.Any(m => m is ClassicHybridCustomModeModifier);
        ClassicConfig.HybridDedupe = normalized.Any(m => m is ClassicHybridDedupeCustomModeModifier);
        ClassicConfig.ClassicColorless = normalized.Any(m => m is ClassicColorlessCustomModeModifier);
        ClassicConfig.ClassicColorlessHybrid = normalized.Any(m => m is ClassicColorlessHybridCustomModeModifier);
        ClassicConfig.ClassicColorlessDedupe = normalized.Any(m => m is ClassicColorlessDedupeCustomModeModifier);
        ClassicConfig.ColorlessCardRewards = normalized.Any(m => m is ColorlessCardRewardsCustomModeModifier);
        ClassicConfig.ReplaceAncientsWithDarv = normalized.Any(m => m is ReplaceAncientsWithDarvCustomModeModifier);
    }
}

[HarmonyPatch(typeof(NCustomRunScreen), "OnEmbarkPressed")]
internal static class CustomRunClassicOptionsEmbarkSyncPatch
{
    static void Prefix(NCustomRunScreen __instance)
    {
        var modifiersList = CustomRunClassicOptionsRules.GetModifiersList(__instance);
        if (modifiersList == null)
            return;

        CustomRunClassicOptionsRules.SyncLobbyFromTickboxesIfEditable(__instance, modifiersList);
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
        var modifiersList = CustomRunClassicOptionsRules.GetModifiersList(__instance);
        if (modifiersList != null)
        {
            CustomRunClassicOptionsRules.Apply(modifiersList, changed: null);
            CustomRunClassicOptionsRules.SyncLobbyFromTickboxesIfEditable(__instance, modifiersList);
        }

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

    public static NCustomRunModifiersList? GetModifiersList(NCustomRunScreen screen)
    {
        return ModifiersListRef(screen);
    }

    public static NRunModifierTickbox? Find<T>(IEnumerable<NRunModifierTickbox> tickboxes) where T : ModifierModel
    {
        return tickboxes.FirstOrDefault(t => t.Modifier?.GetType() == typeof(T));
    }

    public static void SetValue(this NRunModifierTickbox tickbox, bool value)
    {
        tickbox.IsTicked = value;
    }

    public static void RefreshFromConfigIfEditable(NCustomRunModifiersList list)
    {
        if (!CanLocalPlayerEdit(list))
            return;

        var tickboxes = GetTickboxes(list);
        if (tickboxes.Count == 0)
            return;

        Find<ClassicCardsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicCards);
        Find<AddClassicRelicsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.AddClassicRelics);
        Find<OnlyClassicRelicsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.OnlyClassicRelics);
        Find<ClassicHybridCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicHybrid);
        Find<ClassicHybridDedupeCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.HybridDedupe);
        Find<ClassicColorlessCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicColorless);
        Find<ClassicColorlessHybridCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicColorlessHybrid);
        Find<ClassicColorlessDedupeCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ClassicColorlessDedupe);
        Find<ColorlessCardRewardsCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ColorlessCardRewards);
        Find<ReplaceAncientsWithDarvCustomModeModifier>(tickboxes)?.SetValue(ClassicConfig.ReplaceAncientsWithDarv);
    }

    public static void SyncLobbyFromTickboxesIfEditable(NCustomRunScreen screen, NCustomRunModifiersList list)
    {
        if (!CanLocalPlayerEdit(list))
            return;

        screen.Lobby.SetModifiers(list.GetModifiersTickedOn());
    }

    public static void Apply(NCustomRunModifiersList list, NRunModifierTickbox? changed)
    {
        var tickboxes = GetTickboxes(list);
        if (tickboxes.Count == 0)
            return;

        var localCanEdit = CanLocalPlayerEdit(list);

        var cards = Find<ClassicCardsCustomModeModifier>(tickboxes);
        var addRelics = Find<AddClassicRelicsCustomModeModifier>(tickboxes);
        var onlyRelics = Find<OnlyClassicRelicsCustomModeModifier>(tickboxes);
        var hybrid = Find<ClassicHybridCustomModeModifier>(tickboxes);
        var dedupe = Find<ClassicHybridDedupeCustomModeModifier>(tickboxes);
        var colorless = Find<ClassicColorlessCustomModeModifier>(tickboxes);
        var colorlessHybrid = Find<ClassicColorlessHybridCustomModeModifier>(tickboxes);
        var colorlessDedupe = Find<ClassicColorlessDedupeCustomModeModifier>(tickboxes);
        var colorlessRewards = Find<ColorlessCardRewardsCustomModeModifier>(tickboxes);
        var replaceAncients = Find<ReplaceAncientsWithDarvCustomModeModifier>(tickboxes);

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

        if (changed?.Modifier is AddClassicRelicsCustomModeModifier && (addRelics?.IsTicked ?? false))
        {
            if (onlyRelics != null) onlyRelics.IsTicked = false;
        }
        else if ((changed?.Modifier is OnlyClassicRelicsCustomModeModifier || changed?.Modifier is ClassicRelicsCustomModeModifier)
            && (onlyRelics?.IsTicked ?? false))
        {
            if (addRelics != null) addRelics.IsTicked = false;
        }

        if ((addRelics?.IsTicked ?? false) && (onlyRelics?.IsTicked ?? false))
        {
            onlyRelics!.IsTicked = false;
        }

        if (changed?.Modifier is ClassicColorlessCustomModeModifier && (colorless?.IsTicked ?? false))
        {
            if (colorlessHybrid != null) colorlessHybrid.IsTicked = false;
        }
        else if (changed?.Modifier is ClassicColorlessHybridCustomModeModifier && (colorlessHybrid?.IsTicked ?? false))
        {
            if (colorless != null) colorless.IsTicked = false;
        }

        // Safety net for externally synced lists: mixed colorless takes precedence.
        if ((colorlessHybrid?.IsTicked ?? false) && (colorless?.IsTicked ?? false))
        {
            colorless!.IsTicked = false;
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

        var colorlessHybridOn = colorlessHybrid?.IsTicked ?? false;
        if (colorlessDedupe != null)
        {
            if (!colorlessHybridOn)
            {
                colorlessDedupe.IsTicked = false;
                colorlessDedupe.Disable();
            }
            else if (!localCanEdit)
            {
                colorlessDedupe.Disable();
            }
            else
            {
                colorlessDedupe.Enable();
            }
        }

        if (colorlessRewards != null)
        {
            if (!localCanEdit)
                colorlessRewards.Disable();
            else
                colorlessRewards.Enable();
        }
        if (replaceAncients != null)
        {
            if (!localCanEdit)
                replaceAncients.Disable();
            else
                replaceAncients.Enable();
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
        var relicsAdd = Find<AddClassicRelicsCustomModeModifier>(tickboxes);
        var relicsOnly = Find<OnlyClassicRelicsCustomModeModifier>(tickboxes);
        var hybrid = Find<ClassicHybridCustomModeModifier>(tickboxes);
        var dedupe = Find<ClassicHybridDedupeCustomModeModifier>(tickboxes);
        var colorless = Find<ClassicColorlessCustomModeModifier>(tickboxes);
        var colorlessHybrid = Find<ClassicColorlessHybridCustomModeModifier>(tickboxes);
        var colorlessDedupe = Find<ClassicColorlessDedupeCustomModeModifier>(tickboxes);
        var colorlessRewards = Find<ColorlessCardRewardsCustomModeModifier>(tickboxes);
        var replaceAncients = Find<ReplaceAncientsWithDarvCustomModeModifier>(tickboxes);

        // Always show classic options for all characters, but only allow editing
        // from singleplayer/host. Clients stay read-only and follow host sync.
        var localCanEdit = CanLocalPlayerEdit(modifiersList);
        foreach (var t in new[] { cards, relicsAdd, relicsOnly, hybrid, dedupe, colorless, colorlessHybrid, colorlessDedupe, colorlessRewards, replaceAncients })
        {
            if (t == null) continue;
            if (localCanEdit)
                t.Enable();
            else
                t.Disable();
        }

        // Re-apply dependency/mutex states after (re)setting base editability,
        // so child toggles keep their required disabled state.
        Apply(modifiersList, changed: null);
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

        var hasAddRelics = list.Any(m => m is AddClassicRelicsCustomModeModifier);
        var hasOnlyRelics = list.Any(m => m is OnlyClassicRelicsCustomModeModifier || m is ClassicRelicsCustomModeModifier);
        if (hasAddRelics && hasOnlyRelics)
        {
            list.RemoveAll(m => m is OnlyClassicRelicsCustomModeModifier || m is ClassicRelicsCustomModeModifier);
        }

        if (!list.Any(m => m is ClassicHybridCustomModeModifier))
        {
            list.RemoveAll(m => m is ClassicHybridDedupeCustomModeModifier);
        }

        if (list.Any(m => m is ClassicColorlessCustomModeModifier)
            && list.Any(m => m is ClassicColorlessHybridCustomModeModifier))
        {
            list.RemoveAll(m => m is ClassicColorlessCustomModeModifier);
        }

        if (!list.Any(m => m is ClassicColorlessHybridCustomModeModifier))
        {
            list.RemoveAll(m => m is ClassicColorlessDedupeCustomModeModifier);
        }

        return list;
    }
}

using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace ClassicModeMod;

/// <summary>
/// Injects Classic Mode toggle panel on the character select screen.
/// Always visible regardless of selected character.
/// Mimics the game's NTickbox styling with hover/press animations and SFX.
/// </summary>
internal static class ClassicModePanel
{
    private static PanelContainer? _root;
    private static TickboxRow? _cardsRow;
    private static TickboxRow? _relicsRow;
    private static TickboxRow? _hybridRow;
    private static TickboxRow? _dedupeRow;
    private static TickboxRow? _colorlessRow;
    private static TickboxRow? _colorlessHybridRow;
    private static TickboxRow? _colorlessDedupeRow;
    private static StartRunLobby? _lobby;
    private static bool _suppressLobbySync;

    internal static bool Exists => _root != null && GodotObject.IsInstanceValid(_root);

    internal static void Inject(Control screen)
    {
        if (Exists) return;
        _root = BuildPanel();
        screen.AddChild(_root);
        UpdatePanelPlacement();
        _root.Visible = false;
    }

    internal static void Remove()
    {
        if (_root != null && GodotObject.IsInstanceValid(_root))
            _root.QueueFree();
        _root = null;
        _cardsRow = null;
        _relicsRow = null;
        _hybridRow = null;
        _dedupeRow = null;
        _colorlessRow = null;
        _colorlessHybridRow = null;
        _colorlessDedupeRow = null;
        _lobby = null;
        _suppressLobbySync = false;
    }

    internal static void OnCharacterSelected(CharacterModel? character)
    {
        _ = character;
        if (!Exists || _root == null) return;
        _root.Visible = true;
    }

    internal static void ConfigureLobby(StartRunLobby lobby)
    {
        _lobby = lobby;
        UpdatePanelPlacement();
        var canEdit = CanLocalEditLobby();
        ApplyEditability(canEdit);
        SyncFromLobbyModifiers(lobby.Modifiers, canEdit);
    }

    private static void UpdatePanelPlacement()
    {
        if (!Exists || _root == null)
            return;

        var multiplayer = _lobby != null && _lobby.NetService.Type != NetGameType.Singleplayer;
        if (multiplayer)
        {
            _root.AnchorLeft = 0.76f;
            _root.AnchorRight = 0.98f;
            _root.GrowHorizontal = Control.GrowDirection.Begin;
        }
        else
        {
            _root.AnchorLeft = 0.02f;
            _root.AnchorRight = 0.24f;
            _root.GrowHorizontal = Control.GrowDirection.End;
        }

        _root.AnchorTop = 0.05f;
        _root.AnchorBottom = 0.05f;
        _root.GrowVertical = Control.GrowDirection.End;
    }

    internal static void SyncFromLobbyModifiers(IReadOnlyList<ModifierModel> modifiers, bool canEdit)
    {
        if (!Exists)
            return;

        // If no synced state is present (singleplayer/default), keep local config.
        if (modifiers == null || modifiers.Count == 0)
        {
            // In multiplayer client mode, an empty list means host has all classic
            // toggles off. Do not keep stale local values on clients.
            if (!canEdit)
            {
                ClassicConfig.ClassicCards = false;
                ClassicConfig.ClassicRelics = false;
                ClassicConfig.ClassicHybrid = false;
                ClassicConfig.HybridDedupe = false;
                ClassicConfig.ClassicColorless = false;
                ClassicConfig.ClassicColorlessHybrid = false;
                ClassicConfig.ClassicColorlessDedupe = false;
            }

            ApplyEditability(canEdit);
            RefreshCardToggleRows();
            return;
        }

        _suppressLobbySync = true;
        try
        {
            ClassicConfig.ClassicCards = modifiers.Any(m => m is ClassicCardsCustomModeModifier);
            ClassicConfig.ClassicRelics = modifiers.Any(m => m is ClassicRelicsCustomModeModifier);
            ClassicConfig.ClassicHybrid = modifiers.Any(m => m is ClassicHybridCustomModeModifier);
            ClassicConfig.HybridDedupe = modifiers.Any(m => m is ClassicHybridDedupeCustomModeModifier);
            ClassicConfig.ClassicColorless = modifiers.Any(m => m is ClassicColorlessCustomModeModifier);
            ClassicConfig.ClassicColorlessHybrid = modifiers.Any(m => m is ClassicColorlessHybridCustomModeModifier);
            ClassicConfig.ClassicColorlessDedupe = modifiers.Any(m => m is ClassicColorlessDedupeCustomModeModifier);
            RefreshCardToggleRows();
            ApplyEditability(canEdit);
        }
        finally
        {
            _suppressLobbySync = false;
        }
    }

    internal static void SyncLobbyFromLocalConfig()
    {
        if (_suppressLobbySync || _lobby == null)
            return;

        if (!CanLocalEditLobby())
            return;

        var modifiers = new List<ModifierModel>();
        if (ClassicConfig.ClassicCards)
            modifiers.Add(ModelDb.Modifier<ClassicCardsCustomModeModifier>().ToMutable());
        if (ClassicConfig.ClassicRelics)
            modifiers.Add(ModelDb.Modifier<ClassicRelicsCustomModeModifier>().ToMutable());
        if (ClassicConfig.ClassicHybrid)
            modifiers.Add(ModelDb.Modifier<ClassicHybridCustomModeModifier>().ToMutable());
        if (ClassicConfig.HybridDedupe)
            modifiers.Add(ModelDb.Modifier<ClassicHybridDedupeCustomModeModifier>().ToMutable());
        if (ClassicConfig.ClassicColorless)
            modifiers.Add(ModelDb.Modifier<ClassicColorlessCustomModeModifier>().ToMutable());
        if (ClassicConfig.ClassicColorlessHybrid)
            modifiers.Add(ModelDb.Modifier<ClassicColorlessHybridCustomModeModifier>().ToMutable());
        if (ClassicConfig.ClassicColorlessDedupe)
            modifiers.Add(ModelDb.Modifier<ClassicColorlessDedupeCustomModeModifier>().ToMutable());

        _lobby.SetModifiers(modifiers);
    }

    private static bool CanLocalEditLobby()
    {
        return _lobby == null || _lobby.NetService.Type != NetGameType.Client;
    }

    private static void ApplyEditability(bool canEdit)
    {
        _cardsRow?.SetEnabled(canEdit);
        _relicsRow?.SetEnabled(canEdit);
        _hybridRow?.SetEnabled(canEdit);
        // Dedupe still depends on Hybrid. Keep disabled if Hybrid is off.
        _dedupeRow?.SetEnabled(canEdit && ClassicConfig.ClassicHybrid);
        _colorlessRow?.SetEnabled(canEdit);
        _colorlessHybridRow?.SetEnabled(canEdit);
        _colorlessDedupeRow?.SetEnabled(canEdit && ClassicConfig.ClassicColorlessHybrid);
    }

    private static void RefreshCardToggleRows()
    {
        if (_cardsRow == null || _hybridRow == null || _dedupeRow == null || _colorlessRow == null || _colorlessHybridRow == null || _colorlessDedupeRow == null)
            return;

        // Hybrid is mutually exclusive with pure classic card mode.
        if (ClassicConfig.ClassicHybrid && ClassicConfig.ClassicCards)
        {
            ClassicConfig.ClassicCards = false;
        }

        if (ClassicConfig.ClassicColorlessHybrid && ClassicConfig.ClassicColorless)
        {
            ClassicConfig.ClassicColorless = false;
        }

        _cardsRow.SetValueSilently(ClassicConfig.ClassicCards);
        _hybridRow.SetValueSilently(ClassicConfig.ClassicHybrid);
        _dedupeRow.SetValueSilently(ClassicConfig.HybridDedupe);
        _colorlessRow.SetValueSilently(ClassicConfig.ClassicColorless);
        _colorlessHybridRow.SetValueSilently(ClassicConfig.ClassicColorlessHybrid);
        _colorlessDedupeRow.SetValueSilently(ClassicConfig.ClassicColorlessDedupe);

        // Dedupe only has effect under Hybrid mode.
        _dedupeRow.SetEnabled(CanLocalEditLobby() && ClassicConfig.ClassicHybrid);
        _colorlessDedupeRow.SetEnabled(CanLocalEditLobby() && ClassicConfig.ClassicColorlessHybrid);
    }

    private static PanelContainer BuildPanel()
    {
        var font = GD.Load<Font>("res://themes/kreon_bold_glyph_space_two.tres");

        var root = new PanelContainer();
        root.Name = "ClassicModePanel";
        root.MouseFilter = Control.MouseFilterEnum.Ignore;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.06f, 0.05f, 0.09f, 0.85f);
        style.BorderColor = new Color(0.55f, 0.42f, 0.18f, 0.7f);
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(8);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        root.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        root.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "CLASSIC MODE";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", StsColors.gold);
        title.AddThemeColorOverride("font_outline_color", new Color(0.08f, 0.06f, 0.1f, 1f));
        title.AddThemeConstantOverride("outline_size", 4);
        if (font != null) title.AddThemeFontOverride("font", font);
        vbox.AddChild(title);

        // Separator
        var sep = new HSeparator();
        sep.AddThemeConstantOverride("separation", 2);
        sep.AddThemeStyleboxOverride("separator", new StyleBoxLine
        {
            Color = new Color(0.55f, 0.42f, 0.18f, 0.4f),
            Thickness = 1
        });
        vbox.AddChild(sep);

        // Card toggle
        _cardsRow = new TickboxRow("Classic Character Cards", "\u7ecf\u5178\u89d2\u8272\u5361\u724c", font,
            ClassicConfig.ClassicCards, on =>
            {
                if (on && ClassicConfig.ClassicHybrid)
                    ClassicConfig.ClassicHybrid = false;
                ClassicConfig.ClassicCards = on;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Classic Cards: {on}");
            });
        vbox.AddChild(_cardsRow);

        // Hybrid (STS1 + STS2 merged pools) master toggle
        _hybridRow = new TickboxRow("Hybrid Character Cards", "\u6df7\u5408\u89d2\u8272\u5361\u724c", font,
            ClassicConfig.ClassicHybrid, on =>
            {
                ClassicConfig.ClassicHybrid = on;
                if (on && ClassicConfig.ClassicCards)
                    ClassicConfig.ClassicCards = false;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Hybrid Mode: {on}");
            });
        vbox.AddChild(_hybridRow);

        // Hybrid sub-option: merge same-named cards/relics so STS2 wins
        _dedupeRow = new TickboxRow("Dedupe Overlaps", "\u540c\u540d\u53bb\u91cd\uff08\u7559\u4e8c\u4ee3\uff09", font,
            ClassicConfig.HybridDedupe, on =>
            {
                if (!ClassicConfig.ClassicHybrid)
                {
                    RefreshCardToggleRows();
                    return;
                }
                ClassicConfig.HybridDedupe = on;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Hybrid Dedupe: {on}");
            });
        vbox.AddChild(_dedupeRow);

        // Divider between character cards and colorless cards
        var sep2 = new HSeparator();
        sep2.AddThemeConstantOverride("separation", 2);
        sep2.AddThemeStyleboxOverride("separator", new StyleBoxLine
        {
            Color = new Color(0.55f, 0.42f, 0.18f, 0.4f),
            Thickness = 1
        });
        vbox.AddChild(sep2);

        _colorlessRow = new TickboxRow("Classic Colorless", "\u7ecf\u5178\u65e0\u8272\u724c", font,
            ClassicConfig.ClassicColorless, on =>
            {
                ClassicConfig.ClassicColorless = on;
                if (on && ClassicConfig.ClassicColorlessHybrid)
                    ClassicConfig.ClassicColorlessHybrid = false;
                if (!on && !ClassicConfig.ClassicColorlessHybrid)
                    ClassicConfig.ClassicColorlessDedupe = false;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Classic Colorless Only: {on}");
            });
        vbox.AddChild(_colorlessRow);

        _colorlessHybridRow = new TickboxRow("Hybrid Colorless", "\u6df7\u5408\u65e0\u8272\u724c", font,
            ClassicConfig.ClassicColorlessHybrid, on =>
            {
                ClassicConfig.ClassicColorlessHybrid = on;
                if (on && ClassicConfig.ClassicColorless)
                    ClassicConfig.ClassicColorless = false;
                if (!on)
                    ClassicConfig.ClassicColorlessDedupe = false;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Hybrid Colorless: {on}");
            });
        vbox.AddChild(_colorlessHybridRow);

        _colorlessDedupeRow = new TickboxRow("Colorless Dedupe", "\u65e0\u8272\u540c\u540d\u53bb\u91cd", font,
            ClassicConfig.ClassicColorlessDedupe, on =>
            {
                if (!ClassicConfig.ClassicColorlessHybrid)
                {
                    RefreshCardToggleRows();
                    return;
                }
                ClassicConfig.ClassicColorlessDedupe = on;
                RefreshCardToggleRows();
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Classic Colorless Dedupe: {on}");
            });
        vbox.AddChild(_colorlessDedupeRow);

        // Divider between colorless cards and relics
        var sep3 = new HSeparator();
        sep3.AddThemeConstantOverride("separation", 2);
        sep3.AddThemeStyleboxOverride("separator", new StyleBoxLine
        {
            Color = new Color(0.55f, 0.42f, 0.18f, 0.4f),
            Thickness = 1
        });
        vbox.AddChild(sep3);

        // Relic toggle
        _relicsRow = new TickboxRow("Classic Relics", "\u7ecf\u5178\u9057\u7269", font,
            ClassicConfig.ClassicRelics, on =>
            {
                ClassicConfig.ClassicRelics = on;
                SyncLobbyFromLocalConfig();
                Log.Info($"[ClassicMode] Classic Relics: {on}");
            });
        vbox.AddChild(_relicsRow);

        RefreshCardToggleRows();

        return root;
    }
}

/// <summary>
/// A single toggle row that mimics STS2's NTickbox visual style.
/// Uses a colored square with checkmark + label, hover/press tween, and SFX.
/// </summary>
internal class TickboxRow : HBoxContainer
{
    private readonly Control _box;
    private readonly Label _checkMark;
    private readonly Label _label;
    private readonly Action<bool> _onToggled;
    private bool _ticked;
    private bool _enabled = true;
    private Tween? _tween;
    private readonly Vector2 _baseScale = Vector2.One;

    public TickboxRow(string engText, string zhsText, Font? font, bool initial, Action<bool> onToggled)
    {
        _ticked = initial;
        _onToggled = onToggled;

        AddThemeConstantOverride("separation", 10);
        MouseFilter = MouseFilterEnum.Stop;

        // Tick box visual.
        // CRITICAL: every child Control must have MouseFilter = Ignore so mouse
        // events pass through to this HBoxContainer's GuiInput handler. The
        // default MouseFilter for Panel/Label is Stop, which silently eats the
        // click and the row appears unresponsive. (See STS1Replica TickboxRow.)
        var boxOuter = new Panel();
        boxOuter.CustomMinimumSize = new Vector2(28, 28);
        boxOuter.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.10f, 0.16f, 0.9f),
            BorderColor = _ticked ? StsColors.gold : new Color(0.4f, 0.35f, 0.25f, 0.8f),
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
        boxOuter.MouseFilter = MouseFilterEnum.Ignore;
        _box = boxOuter;
        AddChild(boxOuter);

        // Checkmark
        _checkMark = new Label();
        _checkMark.Text = "\u2714";
        _checkMark.HorizontalAlignment = HorizontalAlignment.Center;
        _checkMark.VerticalAlignment = VerticalAlignment.Center;
        _checkMark.AddThemeFontSizeOverride("font_size", 18);
        _checkMark.AddThemeColorOverride("font_color", StsColors.gold);
        _checkMark.AnchorRight = 1;
        _checkMark.AnchorBottom = 1;
        _checkMark.Visible = _ticked;
        _checkMark.MouseFilter = MouseFilterEnum.Ignore;
        boxOuter.AddChild(_checkMark);

        // Label
        var label = new Label();
        label.Text = $"{engText}  {zhsText}";
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", _ticked ? StsColors.cream : new Color(0.65f, 0.60f, 0.50f));
        label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.04f, 0.08f, 0.8f));
        label.AddThemeConstantOverride("outline_size", 2);
        if (font != null) label.AddThemeFontOverride("font", font);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        label.MouseFilter = MouseFilterEnum.Ignore;
        _label = label;
        AddChild(label);

        // Input handling
        GuiInput += HandleInput;
        MouseEntered += OnHover;
        MouseExited += OnUnhover;

        ApplyVisualState();
    }

    public void SetValueSilently(bool value)
    {
        _ticked = value;
        ApplyVisualState();
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        MouseFilter = enabled ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
        if (!enabled)
        {
            _tween?.Kill();
            _box.Scale = _baseScale;
        }
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        _checkMark.Visible = _ticked;

        var boxStyle = (StyleBoxFlat)_box.GetThemeStylebox("panel");
        if (_enabled)
        {
            boxStyle.BorderColor = _ticked ? StsColors.gold : new Color(0.4f, 0.35f, 0.25f, 0.8f);
            _checkMark.Modulate = Colors.White;
            _label.AddThemeColorOverride("font_color", _ticked ? StsColors.cream : new Color(0.65f, 0.60f, 0.50f));
        }
        else
        {
            boxStyle.BorderColor = new Color(0.28f, 0.24f, 0.20f, 0.6f);
            _checkMark.Modulate = new Color(1f, 1f, 1f, 0.55f);
            _label.AddThemeColorOverride("font_color", new Color(0.47f, 0.45f, 0.42f, 0.95f));
        }
    }

    private void HandleInput(InputEvent e)
    {
        if (!_enabled)
            return;

        if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            _ticked = !_ticked;
            ApplyVisualState();

            // SFX
            SfxCmd.Play(_ticked ? "event:/sfx/ui/clicks/ui_checkbox_on" : "event:/sfx/ui/clicks/ui_checkbox_off");

            // Press animation
            _tween?.Kill();
            _tween = CreateTween();
            _tween.TweenProperty(_box, "scale", _baseScale * 0.9f, 0.05);
            _tween.TweenProperty(_box, "scale", _baseScale, 0.15)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);

            _onToggled(_ticked);
            AcceptEvent();
        }
    }

    private void OnHover()
    {
        if (!_enabled)
            return;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_box, "scale", _baseScale * 1.08f, 0.08)
            .SetEase(Tween.EaseType.Out);
    }

    private void OnUnhover()
    {
        if (!_enabled)
            return;

        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_box, "scale", _baseScale, 0.3)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
    }
}

// ── Harmony patches ──

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeSingleplayer))]
internal static class ClassicModeCharSelectSingleplayerPatch
{
    static void Postfix(NCharacterSelectScreen __instance)
    {
        ClassicModePanel.Remove();
        ClassicModePanel.Inject(__instance);
        ClassicModePanel.ConfigureLobby(__instance.Lobby);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsHost))]
internal static class ClassicModeCharSelectHostPatch
{
    static void Postfix(NCharacterSelectScreen __instance)
    {
        ClassicModePanel.Remove();
        ClassicModePanel.Inject(__instance);
        ClassicModePanel.ConfigureLobby(__instance.Lobby);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.InitializeMultiplayerAsClient))]
internal static class ClassicModeCharSelectClientPatch
{
    static void Postfix(NCharacterSelectScreen __instance)
    {
        ClassicModePanel.Remove();
        ClassicModePanel.Inject(__instance);
        ClassicModePanel.ConfigureLobby(__instance.Lobby);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class ClassicModeCharSelectVisibilityPatch
{
    static void Postfix(CharacterModel characterModel)
    {
        ClassicModePanel.OnCharacterSelected(characterModel);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.ModifiersChanged))]
internal static class ClassicModeCharSelectModifiersChangedPatch
{
    static bool Prefix(NCharacterSelectScreen __instance)
    {
        ClassicModePanel.ConfigureLobby(__instance.Lobby);
        return false;
    }
}

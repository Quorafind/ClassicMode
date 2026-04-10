using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using Godot;

namespace ClassicModeMod;

public abstract class ClassicCustomModeModifierBase : ModifierModel
{
    // Reuse the same icon shown by CharacterCards (e.g., "战士卡牌").
    // Different game builds may use slightly different file naming.
    protected override string IconPath
    {
        get
        {
            string[] candidates =
            [
                ImageHelper.GetImagePath("packed/modifiers/charactercards.png"),
                ImageHelper.GetImagePath("packed/modifiers/character_cards.png"),
                ImageHelper.GetImagePath("packed/modifiers/ironcladcards.png"),
                ImageHelper.GetImagePath("packed/modifiers/ironclad_cards.png")
            ];

            foreach (var path in candidates)
            {
                if (ResourceLoader.Exists(path, ""))
                    return path;
            }

            return base.IconPath;
        }
    }
}

public sealed class ClassicCardsCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_CARDS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_CARDS.description");
}

public sealed class ClassicRelicsCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_RELICS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_RELICS.description");
}

public sealed class ClassicHybridCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_HYBRID.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_HYBRID.description");
}

public sealed class ClassicHybridDedupeCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_HYBRID_DEDUPE.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_HYBRID_DEDUPE.description");
}

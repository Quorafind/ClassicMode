using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;
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

public sealed class AddClassicRelicsCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_ADD_RELICS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_ADD_RELICS.description");
}

public class OnlyClassicRelicsCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_ONLY_RELICS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_ONLY_RELICS.description");
}

// Legacy compatibility with previous runs/modifier snapshots.
public sealed class ClassicRelicsCustomModeModifier : OnlyClassicRelicsCustomModeModifier
{
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

public sealed class ClassicColorlessCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_COLORLESS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_COLORLESS.description");
}

public sealed class ClassicColorlessHybridCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_HYBRID.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_HYBRID.description");
}

public sealed class ClassicColorlessDedupeCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_DEDUPE.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_DEDUPE.description");
}

public sealed class ColorlessCardRewardsCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_REWARDS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_COLORLESS_REWARDS.description");

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        return MergeColorlessPool(player, options);
    }

    public override CardCreationOptions ModifyCardRewardCreationOptionsLate(Player player, CardCreationOptions options)
    {
        return MergeColorlessPool(player, options);
    }

    private static CardCreationOptions MergeColorlessPool(Player player, CardCreationOptions options)
    {
        if (options.Source != CardCreationSource.Encounter)
            return options;

        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return options;

        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();

        if (options.CustomCardPool != null)
        {
            if (options.CustomCardPool.Any(c => c.Pool is ColorlessCardPool))
                return options;

            var colorlessCards = colorlessPool.GetUnlockedCards(
                player.UnlockState,
                player.RunState.CardMultiplayerConstraint);

            return options.WithCustomPool(
                options.CustomCardPool
                    .Concat(colorlessCards)
                    .DistinctBy(c => c.Id),
                options.RarityOdds);
        }

        if (options.CardPools.Any(p => p is ColorlessCardPool))
            return options;

        var mergedPools = options.CardPools.ToList();
        mergedPools.Add(colorlessPool);

        return options.WithCardPools(mergedPools, options.CardPoolFilter);
    }
}

public sealed class ReplaceAncientsWithDarvCustomModeModifier : ClassicCustomModeModifierBase
{
    public override LocString Title => new("modifiers", "CLASSIC_CUSTOM_REPLACE_ANCIENTS.title");

    public override LocString Description => new("modifiers", "CLASSIC_CUSTOM_REPLACE_ANCIENTS.description");
}

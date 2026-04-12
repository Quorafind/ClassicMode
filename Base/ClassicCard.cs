using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ClassicModeMod;

/// <summary>Base card for Classic Ironclad cards.</summary>
public abstract class ClassicIroncladCard : CardModel
{
    private readonly string _portraitName;

    protected ClassicIroncladCard(
        string portraitName,
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        _portraitName = portraitName;
    }

    // Keep class identity stable for UI coloring/filtering.
    public override CardPoolModel Pool => ModelDb.CardPool<IroncladCardPool>();

    public override string PortraitPath =>
        $"res://images/packed/card_portraits/classic/ironclad/{_portraitName}.png";

    public override string BetaPortraitPath =>
        $"res://images/packed/card_portraits/classic/ironclad/beta/{_portraitName}.png";

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];
}

/// <summary>Base card for Classic Silent cards.</summary>
public abstract class ClassicSilentCard : CardModel
{
    private readonly string _portraitName;

    protected ClassicSilentCard(
        string portraitName,
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        _portraitName = portraitName;
    }

    public override CardPoolModel Pool => ModelDb.CardPool<SilentCardPool>();

    public override string PortraitPath =>
        $"res://images/packed/card_portraits/classic/silent/{_portraitName}.png";

    public override string BetaPortraitPath =>
        $"res://images/packed/card_portraits/classic/silent/beta/{_portraitName}.png";

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];
}

/// <summary>Base card for Classic Defect cards.</summary>
public abstract class ClassicDefectCard : CardModel
{
    private readonly string _portraitName;

    protected ClassicDefectCard(
        string portraitName,
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        _portraitName = portraitName;
    }

    public override CardPoolModel Pool => ModelDb.CardPool<DefectCardPool>();

    public override string PortraitPath =>
        $"res://images/packed/card_portraits/classic/defect/{_portraitName}.png";

    public override string BetaPortraitPath =>
        $"res://images/packed/card_portraits/classic/defect/beta/{_portraitName}.png";

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];
}

/// <summary>Base card for Classic Colorless cards.</summary>
public abstract class ClassicColorlessCard : CardModel
{
    private readonly string _portraitName;

    protected ClassicColorlessCard(
        string portraitName,
        int canonicalEnergyCost,
        CardType type,
        CardRarity rarity,
        TargetType targetType,
        bool shouldShowInCardLibrary = true)
        : base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
        _portraitName = portraitName;
    }

    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();

    public override string PortraitPath =>
        $"res://images/packed/card_portraits/classic/colorless/{_portraitName}.png";

    public override string BetaPortraitPath =>
        $"res://images/packed/card_portraits/classic/colorless/beta/{_portraitName}.png";

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];
}

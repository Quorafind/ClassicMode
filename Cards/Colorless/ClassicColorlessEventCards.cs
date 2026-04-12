using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

public sealed class UltimateStrikeEvent_C : CardModel
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move)];

    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();

    public override string PortraitPath => ModelDb.Card<UltimateStrike>().PortraitPath;

    public override string BetaPortraitPath => ModelDb.Card<UltimateStrike>().BetaPortraitPath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];

    public UltimateStrikeEvent_C()
        : base(1, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, nameof(cardPlay.Target));

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(6m);
    }
}

public sealed class UltimateDefendEvent_C : CardModel
{
    public override bool GainsBlock => true;

    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(11m, ValueProp.Move)];

    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();

    public override string PortraitPath => ModelDb.Card<UltimateDefend>().PortraitPath;

    public override string BetaPortraitPath => ModelDb.Card<UltimateDefend>().BetaPortraitPath;

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, BetaPortraitPath];

    public UltimateDefendEvent_C()
        : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Block.UpgradeValueBy(4m);
    }
}

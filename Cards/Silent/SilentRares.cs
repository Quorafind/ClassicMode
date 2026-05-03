using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// SILENT RARE ATTACKS (4)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 1. DieDieDie (no STS2 collision)
//    1 cost, Attack, AllEnemies, 13 dmg ALL (17 upg). Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class DieDieDie_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(13m, ValueProp.Move)];

    public DieDieDie_C()
        : base("die_die_die", 1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 2. GrandFinale (STS2 collision -> GrandFinale_C)
//    0 cost, Attack, AllEnemies, 50 dmg ALL (60 upg).
//    Only playable if draw pile is empty.
// ────────────────────────────────────────────────────────────────────────────
public sealed class GrandFinale_C : ClassicSilentCard
{
    protected override bool ShouldGlowGoldInternal => IsPlayable;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(50m, ValueProp.Move)];

    protected override bool IsPlayable =>
        PileType.Draw.GetPile(Owner).Cards.Count == 0;

    public GrandFinale_C()
        : base("grand_finale", 0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "heavy_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 3. Unload (no STS2 collision)
//    1 cost, Attack, AnyEnemy, 14 dmg (18 upg). Exhaust all non-Attack cards in hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Unload_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move)];

    public Unload_C()
        : base("unload", 1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        // Discard all non-Attack cards in hand (STS1 behavior)
        List<CardModel> toDiscard = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Type != CardType.Attack).ToList();
        foreach (CardModel card in toDiscard)
        {
            await CardCmd.Discard(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 4. FanOfKnives (STS2 collision -> FanOfKnives_C)
//    1 cost, Attack, AllEnemies, 4 dmg ALL (7 upg). Draw 1.
//    (STS1: not a Shiv card, it's AoE damage + draw)
// ────────────────────────────────────────────────────────────────────────────
// NOTE: In STS1 this is actually an Uncommon, not Rare. Placed here for completeness.
// We'll put it in pool as uncommon via the pool.
public sealed class FanOfKnives_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new CardsVar(1)
    ];

    public FanOfKnives_C()
        : base("fan_of_knives", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_dagger_throw", null, "dagger_throw.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

}


// ═══════════════════════════════════════════════════════════════════
// SILENT RARE SKILLS (7)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 5. Adrenaline (STS2 collision -> Adrenaline_C)
//    0 cost, Skill, Self. Gain 1 (2) energy. Draw 2. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Adrenaline_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(2)
    ];

    public Adrenaline_C()
        : base("adrenaline", 0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 6. Alchemize (STS2 collision -> Alchemize_C)
//    1 cost, Skill, Self. Obtain a random potion. Exhaust. Upgrade: cost 0.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Alchemize_C : ClassicSilentCard
{
    public override bool CanBeGeneratedInCombat => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Alchemize_C()
        : base("alchemize", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PotionCmd.TryToProcure(
            PotionFactory.CreateRandomPotionInCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable(),
            Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 7. BulletTime (STS2 collision -> BulletTime_C)
//    3 cost, Skill, Self. All cards in hand cost 0 this turn. Can't draw. Upg: cost 2.
// ────────────────────────────────────────────────────────────────────────────
public sealed class BulletTime_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<NoDrawPower>()];

    public BulletTime_C()
        : base("bullet_time", 3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (!card.EnergyCost.CostsX)
            {
                card.SetToFreeThisTurn();
            }
        }
        await PowerCmd.Apply<NoDrawPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 8. Burst (STS2 collision -> Burst_C)
//    1 cost, Skill, Self. Next Skill is played twice (next 2 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class Burst_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Skills", 1m)];

    public Burst_C()
        : base("burst", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BurstPower>(Owner.Creature, DynamicVars["Skills"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Skills"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 9. Doppelganger (no STS2 collision)
//    X cost, Skill, Self. Next turn draw X (X+1) and gain X (X+1) energy. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Doppelganger_C : ClassicSilentCard
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>(),
        HoverTipFactory.FromPower<EnergyNextTurnPower>()
    ];

    public Doppelganger_C()
        : base("doppelganger", -1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        int amount = IsUpgraded ? x + 1 : x;
        if (amount > 0)
        {
            await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, amount, Owner.Creature, this);
            await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 10. Malaise (STS2 collision -> Malaise_C)
//     X cost, Skill, AnyEnemy. Enemy loses X (X+1) Strength. Apply X (X+1) Weak. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Malaise_C : ClassicSilentCard
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override TargetType TargetType => TargetType.AnyEnemy;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public Malaise_C()
        : base("malaise", -1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int x = ResolveEnergyXValue();
        int amount = IsUpgraded ? x + 1 : x;
        if (amount <= 0)
        {
            return;
        }
        await PowerCmd.Apply<StrengthPower>(cardPlay.Target, -amount, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 11. Nightmare (STS2 collision -> Nightmare_C)
//     3 cost, Skill, Self. Choose a card. Next turn add 3 copies to hand. Exhaust. Upg: cost 2.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Nightmare_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Nightmare_C()
        : base("nightmare", 3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext, player: Owner, filter: null, source: this)).FirstOrDefault();
        if (selected != null)
        {
            (await PowerCmd.Apply<NightmarePower>(Owner.Creature, 3m, Owner.Creature, this))
                .SetSelectedCard(selected);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 12. StormOfSteel (STS2 collision -> StormOfSteel_C)
//     1 cost, Skill, Self. Discard hand. Add 1 Shiv per discarded card. Upg: Shivs are upgraded.
// ────────────────────────────────────────────────────────────────────────────
public sealed class StormOfSteel_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shiv>(IsUpgraded)];

    public StormOfSteel_C()
        : base("storm_of_steel", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        int handSize = hand.Count;
        await CardCmd.Discard(choiceContext, hand);
        await Cmd.CustomScaledWait(0f, 0.25f);
        var shivs = await Shiv.CreateInHand(Owner, handSize, CombatState);
        if (IsUpgraded)
        {
            foreach (CardModel shiv in shivs)
            {
                CardCmd.Upgrade(shiv);
            }
        }
    }

    protected override void OnUpgrade()
    {
    }
}

// ═══════════════════════════════════════════════════════════════════
// SILENT RARE POWERS (3)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 13. AThousandCuts (no STS2 collision)
//     2 cost, Power, Self. Whenever you play a card, deal 1 (2) damage to ALL enemies.
// ────────────────────────────────────────────────────────────────────────────
public sealed class AThousandCuts_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("CutDamage", 1m)];

    public AThousandCuts_C()
        : base("a_thousand_cuts", 2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AThousandCutsPower>(Owner.Creature, DynamicVars["CutDamage"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CutDamage"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 14. AfterImage (STS2 collision -> Afterimage_C)
//     1 cost, Power, Self. Whenever you play a card, gain 1 block. Upg: Innate.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Afterimage_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<AfterimagePower>(1m)];

    public Afterimage_C()
        : base("after_image", 1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AfterimagePower>(Owner.Creature, DynamicVars["AfterimagePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 15. ToolsOfTheTrade (STS2 collision -> ToolsOfTheTrade_C)
//     1 cost, Power, Self. At start of turn, draw 1, discard 1. Upg: cost 0.
// ────────────────────────────────────────────────────────────────────────────
public sealed class ToolsOfTheTrade_C : ClassicSilentCard
{
    public ToolsOfTheTrade_C()
        : base("tools_of_the_trade", 1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ToolsOfTheTradePower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 16. WraithForm (STS2 collision -> WraithForm_C)
//     3 cost, Power, Self. Gain 2 (3) Intangible. At start of each turn, lose 1 Dexterity.
// ────────────────────────────────────────────────────────────────────────────
public sealed class WraithForm_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<IntangiblePower>(2m),
        new PowerVar<WraithFormPower>(1m)
    ];

    public WraithForm_C()
        : base("wraith_form", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IntangiblePower>(Owner.Creature, DynamicVars["IntangiblePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WraithFormPower>(Owner.Creature, DynamicVars["WraithFormPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["IntangiblePower"].UpgradeValueBy(1m);
    }
}

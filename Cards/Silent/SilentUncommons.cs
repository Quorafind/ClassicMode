using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
// SILENT UNCOMMON ATTACKS (13)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 1. AllOutAttack (no STS2 collision)
//    1 cost, Attack, AllEnemies, 10 dmg (14 upg). Random discard 1.
// ────────────────────────────────────────────────────────────────────────────
public sealed class AllOutAttack_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10m, ValueProp.Move)];

    public AllOutAttack_C()
        : base("all_out_attack", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
        // Random discard
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count > 0)
        {
            var rng = Owner.RunState.Rng.CombatTargets;
            var card = rng.NextItem(hand);
            if (card != null)
                await CardCmd.Discard(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 2. Backstab (STS2 collision -> Backstab_C)
//    0 cost, Attack, AnyEnemy, 11 dmg (15 upg). Innate. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Backstab_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Innate];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(11m, ValueProp.Move)];

    public Backstab_C()
        : base("backstab", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 3. ChokeHold (no STS2 collision)
//    2 cost, Attack, AnyEnemy, 12 dmg (16 upg). Apply 3 (5) Choke to enemy.
//    (Enemy takes that damage whenever player plays a card this turn.)
// ────────────────────────────────────────────────────────────────────────────
public sealed class ChokeHold_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new DynamicVar("Choke", 3m)
    ];

    public ChokeHold_C()
        : base("choke", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<ChokeHoldPower>(cardPlay.Target, DynamicVars["Choke"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["Choke"].UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 4. Dash (STS2 collision -> Dash_C)
//    2 cost, Attack, AnyEnemy, 10 dmg (13 upg). Gain 10 (13) block.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Dash_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new BlockVar(10m, ValueProp.Move)
    ];

    public Dash_C()
        : base("dash", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 5. EndlessAgony (no STS2 collision)
//    0 cost, Attack, AnyEnemy, 4 dmg (6 upg). Exhaust. When drawn, add copy to hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class EndlessAgony_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move)];

    public EndlessAgony_C()
        : base("endless_agony", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .Execute(choiceContext);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // When THIS card is drawn, add a copy to your hand
        if (card != this) return;
        var copy = CreateClone();
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, addedByPlayer: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 6. Eviscerate (no STS2 collision)
//    3 cost, Attack, AnyEnemy, 7x3 dmg (9x3 upg). Costs 1 less per card discarded this turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Eviscerate_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    private int DiscardsThisTurn =>
        CombatManager.Instance.History.Entries.OfType<CardDiscardedEntry>()
            .Count(e => e.HappenedThisTurn(CombatState) && e.Card.Owner == Owner);

    public Eviscerate_C()
        : base("eviscerate", 3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card != this) return false;
        int discards = DiscardsThisTurn;
        if (discards <= 0) return false;
        modifiedCost = Math.Max(0m, originalCost - discards);
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitCount(3)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 7. Finisher (STS2 collision -> Finisher_C)
//    1 cost, Attack, AnyEnemy, 6 dmg (8 upg) per Attack played this turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Finisher_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits")
            .WithMultiplier((CardModel card, Creature? _) =>
                CombatManager.Instance.History.CardPlaysFinished.Count(
                    (CardPlayFinishedEntry e) =>
                        e.HappenedThisTurn(card.CombatState) &&
                        e.CardPlay.Card.Type == CardType.Attack &&
                        e.CardPlay.Card.Owner == card.Owner))
    ];

    public Finisher_C()
        : base("finisher", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 8. Flechettes (STS2 collision -> Flechettes_C)
//    1 cost, Attack, AnyEnemy, 4 dmg (6 upg) per Skill in hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Flechettes_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits")
            .WithMultiplier((CardModel card, Creature? _) =>
                PileType.Hand.GetPile(card.Owner).Cards.Count(c => c.Type == CardType.Skill))
    ];

    public Flechettes_C()
        : base("flechettes", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_dagger_throw", null, "dagger_throw.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 9. GlassKnife (no STS2 collision)
//    1 cost, Attack, AnyEnemy, 8x2 dmg (12x2 upg). Loses 2 damage each time played.
// ────────────────────────────────────────────────────────────────────────────
public sealed class GlassKnife_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    public GlassKnife_C()
        : base("glass_knife", 1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        // Lose 2 damage each time played
        DynamicVars.Damage.BaseValue = Math.Max(0m, DynamicVars.Damage.BaseValue - 2m);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 10. HeelHook (no STS2 collision)
//     1 cost, Attack, AnyEnemy, 5 dmg (8 upg). If enemy has Weak, gain 1 energy, draw 1.
// ────────────────────────────────────────────────────────────────────────────
public sealed class HeelHook_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    protected override bool ShouldGlowGoldInternal =>
        CombatState?.HittableEnemies.Any(e => e.HasPower<WeakPower>()) ?? false;

    public HeelHook_C()
        : base("heel_hook", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        if (cardPlay.Target.HasPower<WeakPower>())
        {
            await PlayerCmd.GainEnergy(1, Owner);
            await CardPileCmd.Draw(choiceContext, 1m, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 11. MasterfulStab (no STS2 collision)
//     0 cost, Attack, AnyEnemy, 12 dmg (16 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class MasterfulStab_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    public MasterfulStab_C()
        : base("masterful_stab", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner.Creature && result.UnblockedDamage > 0)
        {
            EnergyCost.AddThisCombat(1);
        }
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 12. Predator (STS2 collision -> Predator_C)
//     2 cost, Attack, AnyEnemy, 15 dmg (20 upg). Draw 2 next turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Predator_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DrawCardsNextTurnPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(15m, ValueProp.Move),
        new DynamicVar("DrawNext", 2m)
    ];

    public Predator_C()
        : base("predator", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, DynamicVars["DrawNext"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 13. RiddleWithHoles (no STS2 collision)
//     2 cost, Attack, AnyEnemy, 3x5 dmg (3x7 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class RiddleWithHoles_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(5)
    ];

    public RiddleWithHoles_C()
        : base("riddle_with_holes", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 14. Skewer (STS2 collision -> Skewer_C)
//     X cost, Attack, AnyEnemy, 7 dmg X times (10 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class Skewer_C : ClassicSilentCard
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    public Skewer_C()
        : base("skewer", -1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int x = ResolveEnergyXValue();
        if (x > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(x).FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SILENT UNCOMMON SKILLS (18)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 15. Blur (STS2 collision -> Blur_C)
//     1 cost, Skill, Self. Gain 5 (8) block. Block not removed next turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Blur_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BlurPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move)];

    public Blur_C()
        : base("blur", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<BlurPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 16. BouncingFlask (STS2 collision -> BouncingFlask_C)
//     2 cost, Skill, RandomEnemy. Apply 3 Poison 3 (4 upg) times to random enemies.
// ────────────────────────────────────────────────────────────────────────────
public sealed class BouncingFlask_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PoisonPower>(3m),
        new RepeatVar(3)
    ];

    public BouncingFlask_C()
        : base("bouncing_flask", 2, CardType.Skill, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            Creature enemy = Owner.RunState.Rng.CombatTargets.NextItem(CombatState.HittableEnemies);
            if (enemy != null)
            {
                await PowerCmd.Apply<PoisonPower>(enemy, DynamicVars.Poison.BaseValue, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 17. CalculatedGamble (STS2 collision -> CalculatedGamble_C)
//     0 cost, Skill, Self. Discard hand, draw that many cards. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class CalculatedGamble_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public CalculatedGamble_C()
        : base("calculated_gamble", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> cards = PileType.Hand.GetPile(Owner).Cards;
        int cardsToDraw = cards.Count();
        await CardCmd.DiscardAndDraw(choiceContext, cards, cardsToDraw);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 18. Catalyst (no STS2 card file in decomp -- but check)
//     1 cost, Skill, AnyEnemy. Double (triple upg) a target's Poison. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Catalyst_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Multiplier", 2m)];

    public Catalyst_C()
        : base("catalyst", 1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        var poison = cardPlay.Target.GetPower<PoisonPower>();
        if (poison != null)
        {
            int currentPoison = poison.Amount;
            int multiplier = (int)DynamicVars["Multiplier"].BaseValue;
            int extraPoison = currentPoison * (multiplier - 1);
            await PowerCmd.Apply<PoisonPower>(cardPlay.Target, extraPoison, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Multiplier"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 19. Concentrate (no STS2 collision)
//     0 cost, Skill, Self. Discard 3 (2 upg) cards. Gain 2 energy.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Concentrate_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Discard", 3m),
        new EnergyVar(2)
    ];

    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Count > DynamicVars["Discard"].IntValue;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    public Concentrate_C()
        : base("concentrate", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int discardCount = (int)DynamicVars["Discard"].BaseValue;
        var discarded = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, discardCount),
            null, this)).ToList();
        foreach (var card in discarded)
            await CardCmd.Discard(choiceContext, card);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Discard"].UpgradeValueBy(-1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 20. CorpseExplosion (no STS2 collision)
//     2 cost, Skill, AnyEnemy. Apply Poison equal to 6 (9 upg).
//     When that enemy dies, deal damage to all enemies equal to its Max HP.
// ────────────────────────────────────────────────────────────────────────────
public sealed class CorpseExplosion_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PoisonPower>(6m)];

    public CorpseExplosion_C()
        : base("corpse_explosion", 2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<PoisonPower>(cardPlay.Target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<CorpseExplosionPower>(cardPlay.Target, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Poison.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 21. CripplingPoison (no STS2 collision -- STS1: Crippling Cloud)
//     2 cost, Skill, AllEnemies. Apply 4 (7) Poison and 2 (2) Weak to ALL enemies. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class CripplingPoison_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<PoisonPower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PoisonPower>(4m),
        new PowerVar<WeakPower>(2m)
    ];

    public CripplingPoison_C()
        : base("crippling_poison", 2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<PoisonPower>(enemy, DynamicVars.Poison.BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Poison.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 22. Distraction (STS2 collision -> Distraction_C)
//     1 cost, Skill, Self. Add random Skill from your card pool to hand. It costs 0. Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Distraction_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Distraction_C()
        : base("distraction", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = CardFactory.GetDistinctForCombat(
            Owner,
            from c in Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            where c.Type == CardType.Skill
            select c,
            1,
            Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (card != null)
        {
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, addedByPlayer: true);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 23. EscapePlan (STS2 collision -> EscapePlan_C)
//     0 cost, Skill, Self. Draw 1. If it's a Skill, gain 3 (5) block.
// ────────────────────────────────────────────────────────────────────────────
public sealed class EscapePlan_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3m, ValueProp.Move)];

    public EscapePlan_C()
        : base("escape_plan", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawn = (await CardPileCmd.Draw(choiceContext, 1m, Owner)).FirstOrDefault();
        if (drawn != null && drawn.Type == CardType.Skill)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 24. Expertise (STS2 collision -> Expertise_C)
//     1 cost, Skill, Self. Draw cards until you have 6 (7 upg) in hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Expertise_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("HandSize", 6m)];

    public Expertise_C()
        : base("expertise", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int targetSize = (int)DynamicVars["HandSize"].BaseValue;
        int currentHand = PileType.Hand.GetPile(Owner).Cards.Count;
        int toDraw = Math.Max(0, targetSize - currentHand);
        if (toDraw > 0)
            await CardPileCmd.Draw(choiceContext, toDraw, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HandSize"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 25. LegSweep (STS2 collision -> LegSweep_C)
//     2 cost, Skill, AnyEnemy. Apply 2 (3) Weak. Gain 11 (14) block.
// ────────────────────────────────────────────────────────────────────────────
public sealed class LegSweep_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(11m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    ];

    public LegSweep_C()
        : base("leg_sweep", 2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 26. PiercingWail (STS2 collision -> PiercingWail_C)
//     1 cost, Skill, AllEnemies. Reduce ALL enemies Strength by 6 (8). Exhaust.
// ────────────────────────────────────────────────────────────────────────────
public sealed class PiercingWail_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("StrLoss", 6m)];

    public PiercingWail_C()
        : base("piercing_wail", 1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<PiercingWailPower>(enemy, DynamicVars["StrLoss"].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrLoss"].UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 27. PhantasmalKiller (no STS2 collision)
//     1 cost, Skill, Self. Next turn, your attacks deal double damage.
// ────────────────────────────────────────────────────────────────────────────
public sealed class PhantasmalKiller_C : ClassicSilentCard
{
    public PhantasmalKiller_C()
        : base("phantasmal_killer", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PhantasmalKillerPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 28. Reflex (STS2 collision -> Reflex_C)
//     Unplayable. When discarded from hand, draw 2 (3 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class Reflex_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    protected override bool IsPlayable => false;

    public Reflex_C()
        : base("reflex", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, shouldShowInCardLibrary: true)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card != this)
            return;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 29. Setup (no STS2 collision)
//     1 cost, Skill, Self. Choose a card in hand. It costs 0 next turn. Upgrade: costs 0.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Setup_C : ClassicSilentCard
{
    public Setup_C()
        : base("setup", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
            context: choiceContext, player: Owner, filter: null, source: this)).FirstOrDefault();
        if (selected != null)
        {
            selected.EnergyCost.SetUntilPlayed(0, reduceOnly: true);
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 30. Tactician (STS2 collision -> Tactician_C)
//     Unplayable. When discarded from hand, gain 1 (2 upg) energy.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Tactician_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override bool IsPlayable => false;

    public Tactician_C()
        : base("tactician", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self, shouldShowInCardLibrary: true)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card != this)
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 31. Terror (no STS2 collision)
//     1 cost, Skill, AnyEnemy. Apply 99 Vulnerable. Exhaust. Upg: cost 0.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Terror_C : ClassicSilentCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<VulnerablePower>(99m)];

    public Terror_C()
        : base("terror", 1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SILENT UNCOMMON POWERS (7)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 32. Accuracy (STS2 collision -> Accuracy_C)
//     1 cost, Power, Self. Shivs deal 4 (6) more damage.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Accuracy_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shiv>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<AccuracyPower>(4m)];

    public Accuracy_C()
        : base("accuracy", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AccuracyPower>(Owner.Creature, DynamicVars["AccuracyPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["AccuracyPower"].UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 33. Caltrops (STS2 collision -> Caltrops_C)
//     1 cost, Power, Self. Gain 3 (5) Thorns.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Caltrops_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsPower>(3m)];

    public Caltrops_C()
        : base("caltrops", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ThornsPower>(Owner.Creature, DynamicVars["ThornsPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["ThornsPower"].UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 34. Envenom (STS2 collision -> Envenom_C)
//     2 cost, Power, Self. Whenever you deal unblocked attack damage, apply 1 (2) Poison.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Envenom_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<EnvenomPower>(1m)];

    public Envenom_C()
        : base("envenom", 2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnvenomPower>(Owner.Creature, DynamicVars["EnvenomPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["EnvenomPower"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 35. FootworkSilent (Footwork STS2 collision -> Footwork_C)
//     1 cost, Power, Self. Gain 2 (3) Dexterity.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Footwork_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<DexterityPower>(2m)];

    public Footwork_C()
        : base("footwork", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Dexterity.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 36. InfiniteBlades (STS2 collision -> InfiniteBlades_C)
//     1 cost, Power, Self. At start of turn, add a Shiv to hand. Upg: Innate.
// ────────────────────────────────────────────────────────────────────────────
public sealed class InfiniteBlades_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shiv>()];

    public InfiniteBlades_C()
        : base("infinite_blades", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InfiniteBladesPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 37. NoxiousFumes (STS2 collision -> NoxiousFumes_C)
//     1 cost, Power, Self. At start of turn, apply 2 (3) Poison to ALL enemies.
// ────────────────────────────────────────────────────────────────────────────
public sealed class NoxiousFumes_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("PoisonPerTurn", 2m)];

    public NoxiousFumes_C()
        : base("noxious_fumes", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NoxiousFumesPower>(Owner.Creature, DynamicVars["PoisonPerTurn"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PoisonPerTurn"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 38. WellLaidPlans (STS2 collision -> WellLaidPlans_C)
//     1 cost, Power, Self. At end of turn, retain up to 1 (2 upg) cards.
// ────────────────────────────────────────────────────────────────────────────
public sealed class WellLaidPlans_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("RetainAmount", 1m)];

    public WellLaidPlans_C()
        : base("well_laid_plans", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WellLaidPlansPower>(Owner.Creature, DynamicVars["RetainAmount"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RetainAmount"].UpgradeValueBy(1m);
    }
}

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// SILENT COMMON ATTACKS (10)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 1. Bane (no STS2 collision)
//    1 cost, Attack, AnyEnemy, 7 dmg (10 upg). If enemy has Poison, deal 7 (10) again.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Bane_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    public Bane_C()
        : base("bane", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (cardPlay.Target.HasPower<PoisonPower>())
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 2. DaggerSpray (STS2 collision -> DaggerSpray_C)
//    1 cost, Attack, AllEnemies, 4x2 dmg (6x2 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class DaggerSpray_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move)];

    public DaggerSpray_C()
        : base("dagger_spray", 1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        for (int i = 0; i < 2; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_dagger_spray", null, "dagger_throw.mp3")
                .SpawningHitVfxOnEachCreature()
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 3. DaggerThrow (STS2 collision -> DaggerThrow_C)
//    1 cost, Attack, AnyEnemy, 9 dmg (12 upg). Draw 1, discard 1.
// ────────────────────────────────────────────────────────────────────────────
public sealed class DaggerThrow_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    public DaggerThrow_C()
        : base("dagger_throw", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dagger_throw", null, "dagger_throw.mp3")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
        var discarded = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            null, this)).FirstOrDefault();
        if (discarded != null)
            await CardCmd.Discard(choiceContext, discarded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 4. FlyingKnee (no STS2 collision)
//    1 cost, Attack, AnyEnemy, 8 dmg (11 upg). Gain 1 energy next turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class FlyingKnee_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<EnergyNextTurnPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    public FlyingKnee_C()
        : base("flying_knee", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 5. PoisonedStab (STS2 collision -> PoisonedStab_C)
//    1 cost, Attack, AnyEnemy, 6 dmg (8 upg). Apply 3 (4) Poison.
// ────────────────────────────────────────────────────────────────────────────
public sealed class PoisonedStab_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new PowerVar<PoisonPower>(3m)
    ];

    public PoisonedStab_C()
        : base("poisoned_stab", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<PoisonPower>(cardPlay.Target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Poison.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 6. QuickSlash (no STS2 collision)
//    1 cost, Attack, AnyEnemy, 8 dmg (12 upg). Draw 1.
// ────────────────────────────────────────────────────────────────────────────
public sealed class QuickSlash_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new CardsVar(1)
    ];

    public QuickSlash_C()
        : base("quick_slash", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 7. Slice (STS2 collision -> Slice_C)
//    0 cost, Attack, AnyEnemy, 6 dmg (9 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class Slice_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    public Slice_C()
        : base("slice", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 8. SneakyStrike (no STS2 collision)
//    2 cost, Attack, AnyEnemy, 12 dmg (16 upg). If you discarded a card this turn, costs 0.
// ────────────────────────────────────────────────────────────────────────────
public sealed class SneakyStrike_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    protected override bool ShouldGlowGoldInternal => HasDiscardedThisTurn;

    private bool HasDiscardedThisTurn =>
        CombatManager.Instance.History.Entries.OfType<CardDiscardedEntry>()
            .Any(e => e.HappenedThisTurn(CombatState) && e.Card.Owner == Owner);

    public SneakyStrike_C()
        : base("sneaky_strike", 2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        if (HasDiscardedThisTurn)
            await PlayerCmd.GainEnergy(2m, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 9. SuckerPunch (STS2 collision -> SuckerPunch_C)
//    1 cost, Attack, AnyEnemy, 7 dmg (9 upg). Apply 1 (2) Weak.
// ────────────────────────────────────────────────────────────────────────────
public sealed class SuckerPunch_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new PowerVar<WeakPower>(1m)
    ];

    public SuckerPunch_C()
        : base("sucker_punch", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars.Weak.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SILENT COMMON SKILLS (8)
// ═══════════════════════════════════════════════════════════════════

// ────────────────────────────────────────────────────────────────────────────
// 10. Acrobatics (STS2 collision -> Acrobatics_C)
//     1 cost, Skill, Self. Draw 3 (4 upg), discard 1.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Acrobatics_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(3)];

    public Acrobatics_C()
        : base("acrobatics", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        var discarded = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1),
            null, this)).FirstOrDefault();
        if (discarded != null)
            await CardCmd.Discard(choiceContext, discarded);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 11. Backflip (STS2 collision -> Backflip_C)
//     1 cost, Skill, Self. Gain 5 (8) block. Draw 2.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Backflip_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new CardsVar(2)
    ];

    public Backflip_C()
        : base("backflip", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 12. BladeDance (STS2 collision -> BladeDance_C)
//     1 cost, Skill, Self. Add 3 (4 upg) Shivs to hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class BladeDance_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shiv>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(3)];

    public BladeDance_C()
        : base("blade_dance", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await Shiv.CreateInHand(Owner, CombatState);
            await Cmd.Wait(0.1f);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 13. CloakAndDagger (STS2 collision -> CloakAndDagger_C)
//     1 cost, Skill, Self. Gain 6 block. Add 1 (2 upg) Shiv to hand.
// ────────────────────────────────────────────────────────────────────────────
public sealed class CloakAndDagger_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Shiv>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
        new CardsVar(1)
    ];

    public CloakAndDagger_C()
        : base("cloak_and_dagger", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            await Shiv.CreateInHand(Owner, CombatState);
            await Cmd.Wait(0.1f);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 14. DeadlyPoison (STS2 collision -> DeadlyPoison_C)
//     1 cost, Skill, AnyEnemy. Apply 5 (7) Poison.
// ────────────────────────────────────────────────────────────────────────────
public sealed class DeadlyPoison_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<PoisonPower>(5m)];

    public DeadlyPoison_C()
        : base("deadly_poison", 1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<PoisonPower>(cardPlay.Target, DynamicVars.Poison.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Poison.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 15. Deflect (STS2 collision -> Deflect_C)
//     0 cost, Skill, Self. Gain 4 (7) block.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Deflect_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4m, ValueProp.Move)];

    public Deflect_C()
        : base("deflect", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 16. DodgeAndRoll (STS2 collision -> DodgeAndRoll_C)
//     1 cost, Skill, Self. Gain 4 (6) block. Next turn, gain 4 (6) block.
// ────────────────────────────────────────────────────────────────────────────
public sealed class DodgeAndRoll_C : ClassicSilentCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BlockNextTurnPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(4m, ValueProp.Move)];

    public DodgeAndRoll_C()
        : base("dodge_and_roll", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<BlockNextTurnPower>(Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 17. Outmaneuver (STS2 collision -> Outmaneuver_C)
//     1 cost, Skill, Self. Gain 2 (3 upg) energy next turn.
// ────────────────────────────────────────────────────────────────────────────
public sealed class Outmaneuver_C : ClassicSilentCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<EnergyNextTurnPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Energy", 2m)];

    public Outmaneuver_C()
        : base("outmaneuver", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, DynamicVars["Energy"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 18. Prepared (STS2 collision -> Prepared_C)
//     0 cost, Skill, Self. Draw 1 (2 upg), discard 1 (2 upg).
// ────────────────────────────────────────────────────────────────────────────
public sealed class Prepared_C : ClassicSilentCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public Prepared_C()
        : base("prepared", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars.Cards.IntValue;
        await CardPileCmd.Draw(choiceContext, count, Owner);
        var discarded = (await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count),
            null, this)).ToList();
        foreach (var card in discarded)
            await CardCmd.Discard(choiceContext, card);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

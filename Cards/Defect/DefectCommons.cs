using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// DEFECT COMMON ATTACKS (8)
// ═══════════════════════════════════════════════════════════════════

// STS1 Ball Lightning: 1 energy, 7 damage (10 upgraded). Channel 1 Lightning.
public sealed class BallLightning_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
    ];

    public BallLightning_C()
        : base("ball_lightning", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_lightning", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// STS1 Barrage: 1 energy, deal 4 damage per channeled Orb (6 upgraded).
public sealed class Barrage_C : ClassicDefectCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Channeling)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("CalculatedHits")
            .WithMultiplier((CardModel card, Creature? _) =>
                card.Owner.PlayerCombatState?.OrbQueue?.Orbs.Count ?? 0)
    ];

    public Barrage_C()
        : base("barrage", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        int hits = (int)((CalculatedVar)DynamicVars["CalculatedHits"]).Calculate(cardPlay.Target);
        if (hits > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hits)
                .FromCard(this).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// STS1 Beam Cell: 0 energy, 3 damage (4 upgraded). Apply 1 Vulnerable (2 upgraded).
public sealed class BeamCell_C : ClassicDefectCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m)
    ];

    public BeamCell_C()
        : base("beam_cell", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_lightning", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DynamicVars.Vulnerable.BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}

// STS1 Claw: 0 energy, 3 damage (5 upgraded). Increase damage of ALL Claw cards by 2.
public sealed class Claw_C : ClassicDefectCard
{
    private const string IncreaseKey = "Increase";
    private decimal _extraDamageFromClawPlays;

    private decimal ExtraDamageFromClawPlays
    {
        get => _extraDamageFromClawPlays;
        set
        {
            AssertMutable();
            _extraDamageFromClawPlays = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new DynamicVar(IncreaseKey, 2m)
    ];

    public Claw_C()
        : base("claw", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_scratch")
            .Execute(choiceContext);
        decimal increase = DynamicVars[IncreaseKey].BaseValue;
        foreach (Claw_C claw in Owner.PlayerCombatState.AllCards.OfType<Claw_C>())
        {
            claw.BuffFromClawPlay(increase);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamageFromClawPlays;
    }

    private void BuffFromClawPlay(decimal extraDamage)
    {
        DynamicVars.Damage.BaseValue += extraDamage;
        ExtraDamageFromClawPlays += extraDamage;
    }
}

// STS1 Cold Snap: 1 energy, 6 damage (9 upgraded). Channel 1 Frost.
public sealed class ColdSnap_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<FrostOrb>()
    ];

    public ColdSnap_C()
        : base("cold_snap", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// STS1 Compiled Driver: 1 energy, 7 damage (10 upgraded). Draw 1 card per unique Orb type.
public sealed class CompiledDriver_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Channeling)];

    public CompiledDriver_C()
        : base("compile_driver", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        var orbQueue = Owner.PlayerCombatState?.OrbQueue;
        int uniqueOrbs = orbQueue?.Orbs
            .Select(o => o.GetType()).Distinct().Count() ?? 0;
        if (uniqueOrbs > 0)
        {
            await CardPileCmd.Draw(choiceContext, uniqueOrbs, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// STS1 Go for the Eyes: 0 energy, 3 damage (4 upgraded). Apply 1 Weak (2 upgraded) if enemy intends to attack.
public sealed class GoForTheEyes_C : ClassicDefectCard
{
    protected override bool ShouldGlowGoldInternal =>
        CombatState?.HittableEnemies.Any(e => e.Monster?.IntendsToAttack ?? false) ?? false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new PowerVar<WeakPower>(1m)
    ];

    public GoForTheEyes_C()
        : base("go_for_the_eyes", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        if (cardPlay.Target.Monster != null && cardPlay.Target.Monster.IntendsToAttack)
        {
            await PowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars.Weak.BaseValue,
                Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

// STS1 Streamline: 2 energy, 15 damage (20 upgraded). Costs 1 less each time played.
public sealed class Streamline_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(15m, ValueProp.Move)];

    public Streamline_C()
        : base("streamline", 2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
        EnergyCost.AddThisCombat(-1, reduceOnly: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

// STS1 Sweeping Beam: 1 energy, deal 6 damage (9 upgraded) to ALL. Draw 1 card.
public sealed class SweepingBeam_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new CardsVar(1)
    ];

    public SweepingBeam_C()
        : base("sweeping_beam", 1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_lightning")
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
// DEFECT COMMON SKILLS (6)
// ═══════════════════════════════════════════════════════════════════

// STS1 Coolheaded: 1 energy, Channel 1 Frost, Draw 1 card (2 upgraded).
public sealed class Coolheaded_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<FrostOrb>()
    ];

    public Coolheaded_C()
        : base("coolheaded", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Leap: 1 energy, gain 9 Block (12 upgraded).
public sealed class Leap_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(9m, ValueProp.Move)];

    public Leap_C()
        : base("leap", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
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

// STS1 Reboot: 0 energy, shuffle hand into draw pile, draw 4 cards (6 upgraded). Exhaust.
public sealed class Reboot_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(4)];

    public Reboot_C()
        : base("reboot", 0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards.ToList())
        {
            await CardPileCmd.Add(card, PileType.Draw);
        }
        await CardPileCmd.Shuffle(choiceContext, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2m);
    }
}

// STS1 Recursion: 1 energy (0 upgraded), Evoke front orb, Channel front orb type.
public sealed class Recursion_C : ClassicDefectCard
{
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.Front;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Evoke),
        HoverTipFactory.Static(StaticHoverTip.Channeling)
    ];

    public Recursion_C()
        : base("recursion", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var orbQueue = Owner.PlayerCombatState?.OrbQueue;
        if (orbQueue?.Orbs.Count > 0)
        {
            var firstOrb = orbQueue.Orbs[0];
            var orbType = firstOrb.GetType();
            await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
            await OrbCmd.EvokeNext(choiceContext, Owner);
            // Channel same orb type
            if (orbType == typeof(LightningOrb))
                await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
            else if (orbType == typeof(FrostOrb))
                await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
            else if (orbType == typeof(DarkOrb))
                await OrbCmd.Channel<DarkOrb>(choiceContext, Owner);
            else if (orbType == typeof(PlasmaOrb))
                await OrbCmd.Channel<PlasmaOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Skim: 1 energy, Draw 3 cards (4 upgraded).
public sealed class Skim_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(3)];

    public Skim_C()
        : base("skim", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Steam Barrier: 0 energy, gain 6 Block (8 upgraded). Block decreases by 1 each play.
public sealed class SteamBarrier_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6m, ValueProp.Move)];

    public SteamBarrier_C()
        : base("steam_barrier", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (DynamicVars.Block.BaseValue > 0)
        {
            DynamicVars.Block.BaseValue -= 1m;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// STS1 Turbo: 0 energy, gain 2 energy (3 upgraded). Add a Void to discard.
public sealed class Turbo_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Void>()
    ];

    public Turbo_C()
        : base("turbo", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        ArgumentNullException.ThrowIfNull(CombatState);
        CardModel voidCard = CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(voidCard, PileType.Discard, Owner));
        await Cmd.Wait(0.5f);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

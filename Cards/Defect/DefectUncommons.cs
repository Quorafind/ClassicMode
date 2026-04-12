using System.Linq;
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
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// DEFECT UNCOMMON ATTACKS (9)
// ═══════════════════════════════════════════════════════════════════

// STS1 Blizzard: 1 energy, deal damage equal to 2x (3x upgraded) number of Frost channeled this combat.
public sealed class Blizzard_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
                CombatManager.Instance.History.Entries.OfType<OrbChanneledEntry>()
                    .Count(e => e.Actor.Player == card.Owner && e.Orb is FrostOrb))
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromOrb<FrostOrb>()];

    public Blizzard_C()
        : base("blizzard", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}

// STS1 Doom and Gloom: 2 energy, 10 damage (14 upgraded) to ALL. Channel 1 Dark.
public sealed class DoomAndGloom_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<DarkOrb>()
    ];

    public DoomAndGloom_C()
        : base("doom_and_gloom", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
        await OrbCmd.Channel<DarkOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// STS1 FTL: 0 energy, 5 damage (6 upgraded). Draw 1 card if < 3 cards played this turn (4 upgraded).
public sealed class Ftl_C : ClassicDefectCard
{
    private const string PlayMaxKey = "PlayMax";

    protected override bool ShouldGlowGoldInternal => CanDrawCard;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new IntVar(PlayMaxKey, 3m),
        new CardsVar(1)
    ];

    private bool CanDrawCard
    {
        get
        {
            int plays = CombatManager.Instance.History.CardPlaysFinished
                .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Owner == Owner);
            return plays < DynamicVars[PlayMaxKey].IntValue;
        }
    }

    public Ftl_C()
        : base("ftl", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        if (CanDrawCard)
        {
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars[PlayMaxKey].UpgradeValueBy(1m);
    }
}

// STS1 Melter: 1 energy, 10 damage (14 upgraded). Remove target's Block.
public sealed class Melter_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    public Melter_C()
        : base("melter", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // Remove all Block from target before dealing damage
        if (cardPlay.Target.Block > 0)
        {
            await CreatureCmd.LoseBlock(cardPlay.Target, cardPlay.Target.Block);
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// STS1 Rip and Tear: 1 energy, deal 7 damage (9 upgraded) to a random enemy 2 times.
public sealed class RipAndTear_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    public RipAndTear_C()
        : base("rip_and_tear", 1, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(2)
            .FromCard(this).TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_scratch")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// STS1 Scrape: 1 energy, 7 damage (10 upgraded). Draw 4 (5 upgraded) cards, discard non-0-cost cards drawn.
public sealed class Scrape_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new CardsVar(4)
    ];

    public Scrape_C()
        : base("scrape", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_scratch")
            .Execute(choiceContext);
        IEnumerable<CardModel> drawn = await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        var toDiscard = drawn.Where(c =>
            c.EnergyCost.GetWithModifiers(CostModifiers.Local) != 0 ||
            c.EnergyCost.CostsX);
        await CardCmd.Discard(choiceContext, toDiscard);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Sunder: 3 energy, 24 damage (32 upgraded). If fatal, gain 3 energy.
public sealed class Sunder_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24m, ValueProp.Move),
        new EnergyVar(3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Energy)];

    public Sunder_C()
        : base("sunder", 3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        AttackCommand result = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
            .Execute(choiceContext);
        if (result.Results.Any(r => r.WasTargetKilled))
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}

// STS1 Lock-On: 1 energy, 9 damage (12 upgraded). Apply 1 (2 upgraded) Lock-On.
public sealed class Lockdown_C : ClassicDefectCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<LockOnPower_C>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new PowerVar<LockOnPower_C>(1m)
    ];

    public Lockdown_C()
        : base("lock_on", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<LockOnPower_C>(cardPlay.Target, DynamicVars["LockOnPower_C"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["LockOnPower_C"].UpgradeValueBy(1m);
    }
}

// STS1 Rebound: 1 energy, 9 damage (12 upgraded). Next card played goes on top of draw pile.
public sealed class Rebound_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    public Rebound_C()
        : base("rebound", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await PowerCmd.Apply<ReboundPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// DEFECT UNCOMMON SKILLS (19)
// ═══════════════════════════════════════════════════════════════════

// STS1 Aggregate: 1 energy, gain 1 energy per 4 (3 upgraded) cards in draw pile.
public sealed class Aggregate_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Divisor", 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Energy)];

    public Aggregate_C()
        : base("aggregate", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int drawCount = PileType.Draw.GetPile(Owner).Cards.Count();
        int divisor = DynamicVars["Divisor"].IntValue;
        int energyGain = drawCount / divisor;
        if (energyGain > 0)
        {
            await PlayerCmd.GainEnergy(energyGain, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Divisor"].UpgradeValueBy(-1m);
    }
}

// STS1 Auto-Shields: 1 energy, gain 11 Block (15 upgraded). Only if you have no Block.
public sealed class AutoShields_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override bool ShouldGlowGoldInternal => Owner.Creature.Block == 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(11m, ValueProp.Move)];

    public AutoShields_C()
        : base("auto_shields", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.Block == 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}

// STS1 Boot Sequence: 0 energy, gain 10 Block (13 upgraded). Innate, Exhaust.
public sealed class BootSequence_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Innate, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(10m, ValueProp.Move)];

    public BootSequence_C()
        : base("boot_sequence", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

// STS1 Chaos: 1 energy, channel 1 random Orb (2 upgraded).
public sealed class Chaos_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new RepeatVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Channeling)];

    public Chaos_C()
        : base("chaos", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await OrbCmd.Channel(choiceContext,
                OrbModel.GetRandomOrb(Owner.RunState.Rng.CombatOrbGeneration).ToMutable(), Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// STS1 Charge Battery: 1 energy, gain 7 Block (10 upgraded). Gain 1 energy next turn.
public sealed class ChargeBattery_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Energy)];

    public ChargeBattery_C()
        : base("charge_battery", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, DynamicVars.Energy.BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// STS1 Chill: 0 energy, channel 1 Frost per enemy. Exhaust (upgraded: no exhaust).
public sealed class Chill_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<FrostOrb>()
    ];

    public Chill_C()
        : base("chill", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

// STS1 Consume: 2 energy, gain 2 Focus (3 upgraded), lose 1 Orb slot.
public sealed class Consume_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FocusPower>(2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FocusPower>()];

    public Consume_C()
        : base("consume", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue,
            Owner.Creature, this);
        OrbCmd.RemoveSlots(Owner, 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }
}

// STS1 Darkness: 1 energy, channel 1 Dark. Trigger passive of all Dark orbs 1 (2 upgraded) times.
public sealed class Darkness_C : ClassicDefectCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<DarkOrb>()
    ];

    public Darkness_C()
        : base("darkness", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.Channel<DarkOrb>(choiceContext, Owner);
        var orbQueue = Owner.PlayerCombatState?.OrbQueue;
        if (orbQueue == null)
            return;

        int triggerCount = IsUpgraded ? 2 : 1;
        foreach (OrbModel orb in orbQueue.Orbs.Where(o => o is DarkOrb))
        {
            for (int i = 0; i < triggerCount; i++)
            {
                await OrbCmd.Passive(choiceContext, orb, null);
            }
        }
    }
}

// STS1 Equilibrium: 2 energy, gain 13 Block (16 upgraded). Retain your hand this turn.
public sealed class Equilibrium_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(13m, ValueProp.Move),
        new DynamicVar("Equilibrium", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Retain)];

    public Equilibrium_C()
        : base("equilibrium", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<RetainHandPower>(Owner.Creature, DynamicVars["Equilibrium"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// STS1 Fission: 0 energy, remove all Orbs. Gain 1 energy and draw 1 card per Orb removed. Exhaust.
public sealed class Fission_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Fission_C()
        : base("fission", 0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var orbQueue = Owner.PlayerCombatState?.OrbQueue;
        if (orbQueue == null || orbQueue.Orbs.Count <= 0)
            return;

        int orbCount = orbQueue.Orbs.Count;
        var orbManager = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.OrbManager;
        foreach (OrbModel orb in orbQueue.Orbs.ToList())
        {
            if (!orbQueue.Remove(orb))
                continue;

            // Visual-only removal; does not call orb.Evoke() and thus does not trigger orb effects.
            orbManager?.EvokeOrbAnim(orb);
            orb.RemoveInternal();
        }

        await PlayerCmd.GainEnergy(orbCount, Owner);
        await CardPileCmd.Draw(choiceContext, orbCount, Owner);
    }
}

// STS1 Force Field: 4 energy, gain 12 Block (16 upgraded). Costs 1 less per Power played this combat.
public sealed class Forcefield_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(12m, ValueProp.Move)];

    public Forcefield_C()
        : base("forcefield", 4, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner && cardPlay.Card.Type == CardType.Power)
        {
            EnergyCost.AddThisCombat(-1, reduceOnly: true);
        }
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}

// STS1 Fusion: 2 energy (1 upgraded), channel 1 Plasma.
public sealed class Fusion_C : ClassicDefectCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<PlasmaOrb>()
    ];

    public Fusion_C()
        : base("fusion", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.Channel<PlasmaOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Genetic Algorithm: 1 energy, 1 Block (1 upgraded). Block increases by 2 (3 upgraded) each play. Exhaust.
public sealed class GeneticAlgorithm_C : ClassicDefectCard
{
    private int _currentBlock = 1;
    private int _increasedBlock;

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    [SavedProperty]
    public int CurrentBlock
    {
        get => _currentBlock;
        set
        {
            AssertMutable();
            _currentBlock = value;
            DynamicVars.Block.BaseValue = _currentBlock;
        }
    }

    [SavedProperty]
    public int IncreasedBlock
    {
        get => _increasedBlock;
        set
        {
            AssertMutable();
            _increasedBlock = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(CurrentBlock, ValueProp.Move),
        new IntVar("Increase", 2m)
    ];

    public GeneticAlgorithm_C()
        : base("genetic_algorithm", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        int intValue = DynamicVars["Increase"].IntValue;
        BuffFromPlay(intValue);
        (DeckVersion as GeneticAlgorithm_C)?.BuffFromPlay(intValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1m);
    }

    protected override void AfterDowngraded()
    {
        UpdateBlock();
    }

    private void BuffFromPlay(int extraBlock)
    {
        IncreasedBlock += extraBlock;
        UpdateBlock();
    }

    private void UpdateBlock()
    {
        CurrentBlock = 1 + IncreasedBlock;
    }
}

// STS1 Glacier: 2 energy, gain 7 Block (10 upgraded). Channel 2 Frost.
public sealed class Glacier_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<FrostOrb>()
    ];

    public Glacier_C()
        : base("glacier", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        for (int i = 0; i < 2; i++)
        {
            await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}

// STS1 Hologram: 1 energy, gain 3 Block (5 upgraded). Pick 1 card from discard to put in hand. Exhaust (upgraded: no exhaust).
public sealed class Hologram_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(3m, ValueProp.Move)];

    public Hologram_C()
        : base("hologram", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        CardPile pile = PileType.Discard.GetPile(Owner);
        int discardCount = pile.Cards.Count();
        if (discardCount == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        CardModel card = (await CardSelectCmd.FromSimpleGrid(choiceContext, pile.Cards, Owner, prefs))
            .FirstOrDefault();
        if (card != null)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

// STS1 Overclock: 0 energy, draw 2 cards (3 upgraded). Add a Burn to discard.
public sealed class Overclock_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Burn>()];

    public Overclock_C()
        : base("overclock", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        ArgumentNullException.ThrowIfNull(CombatState);
        CardModel burn = CombatState.CreateCard<Burn>(Owner);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Discard, addedByPlayer: true));
        await Cmd.Wait(0.5f);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Rainbow: 2 energy, channel 1 Lightning, 1 Frost, 1 Dark. Exhaust (upgraded: no exhaust).
public sealed class Rainbow_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>(),
        HoverTipFactory.FromOrb<FrostOrb>(),
        HoverTipFactory.FromOrb<DarkOrb>()
    ];

    public Rainbow_C()
        : base("rainbow", 2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
        await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
        await OrbCmd.Channel<DarkOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}

// STS1 Reinforced Body: 1 energy (X-cost), gain 7 Block X times (9 upgraded).
public sealed class ReinforcedBody_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7m, ValueProp.Move)];

    public ReinforcedBody_C()
        : base("reinforced_body", -1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        for (int i = 0; i < x; i++)
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// STS1 Reprogram: 1 energy, lose 1 Focus (2 upgraded loses 1). Gain 1 Strength (2 upgraded) and 1 Dexterity (2 upgraded).
public sealed class Reprogram_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FocusPower>(1m),
        new PowerVar<StrengthPower>(1m),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public Reprogram_C()
        : base("reprogram", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FocusPower>(Owner.Creature, -DynamicVars["FocusPower"].BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<DexterityPower>(Owner.Creature, DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
        DynamicVars["DexterityPower"].UpgradeValueBy(1m);
    }
}

// STS1 Seek: 0 energy, choose 1 card (2 upgraded) from draw pile and put in hand. Exhaust.
public sealed class Seek_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public Seek_C()
        : base("seek", 0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        CardPile pile = PileType.Draw.GetPile(Owner);
        int availableCards = pile.Cards.Count();
        if (availableCards == 0)
        {
            return;
        }

        int requestedCards = Math.Min(DynamicVars.Cards.IntValue, availableCards);
        CardSelectorPrefs prefs = new CardSelectorPrefs(SelectionScreenPrompt, requestedCards);
        var chosen = await CardSelectCmd.FromSimpleGrid(choiceContext, pile.Cards, Owner, prefs);
        foreach (CardModel card in chosen)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Stack: 1 energy, gain Block equal to discarded cards count (+ 3 upgraded).
public sealed class Stack_C : ClassicDefectCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedBlockVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
                PileType.Discard.GetPile(card.Owner).Cards.Count())
    ];

    public Stack_C()
        : base("stack", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature,
            DynamicVars.CalculatedBlock.Calculate(cardPlay.Target),
            DynamicVars.CalculatedBlock.Props, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.CalculationBase.UpgradeValueBy(3m);
    }
}

// STS1 Tempest: 0 energy (X-cost), channel X (X+1 upgraded) Lightning. Exhaust.
public sealed class Tempest_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
    ];

    public Tempest_C()
        : base("tempest", -1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int numOrbs = ResolveEnergyXValue();
        if (IsUpgraded)
            numOrbs++;
        for (int i = 0; i < numOrbs; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
        }
    }
}

// STS1 White Noise: 1 energy (0 upgraded), add a random Power card to hand. It costs 0 this turn. Exhaust.
public sealed class WhiteNoise_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public WhiteNoise_C()
        : base("white_noise", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        CardModel card = CardFactory.GetDistinctForCombat(
            Owner,
            from c in Owner.Character.CardPool.GetUnlockedCards(
                Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            where c.Type == CardType.Power
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

// ═══════════════════════════════════════════════════════════════════
// DEFECT UNCOMMON POWERS (8)
// ═══════════════════════════════════════════════════════════════════

// STS1 Buffer: 2 energy, gain 1 Buffer (2 upgraded). (Prevent the next time you lose HP.)
public sealed class Buffer_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<BufferPower>(1m)];

    public Buffer_C()
        : base("buffer", 2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["BufferPower"].UpgradeValueBy(1m);
    }
}

// STS1 Capacitor: 1 energy, gain 2 (3 upgraded) Orb slots.
public sealed class Capacitor_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new RepeatVar(2)];

    public Capacitor_C()
        : base("capacitor", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await OrbCmd.AddSlots(Owner, DynamicVars.Repeat.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// STS1 Defragment: 1 energy, gain 1 Focus (2 upgraded).
public sealed class Defragment_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<FocusPower>(1m)];

    public Defragment_C()
        : base("defragment", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }
}

// STS1 Heatsinks: 1 energy, whenever you play a Power card, draw 1 (2 upgraded) card.
public sealed class Heatsinks_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Heatsinks", 1m)];

    public Heatsinks_C()
        : base("heatsinks", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HeatsinksPower_C>(Owner.Creature, DynamicVars["Heatsinks"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heatsinks"].UpgradeValueBy(1m);
    }
}

// STS1 Hello World: 1 energy, at the start of each turn add a random Common card to hand. Innate (upgraded).
public sealed class HelloWorld_C : ClassicDefectCard
{
    public HelloWorld_C()
        : base("hello_world", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HelloWorldPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

// STS1 Loop: 1 energy, at the start of each turn, trigger the passive of your first Orb 1 (2 upgraded) times.
public sealed class Loop_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Loop", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Channeling)];

    public Loop_C()
        : base("loop", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<LoopPower>(Owner.Creature, DynamicVars["Loop"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Loop"].UpgradeValueBy(1m);
    }
}

// STS1 Storm: 1 energy, whenever you play a Power card, channel 1 (2 upgraded) Lightning.
public sealed class Storm_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Storm", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
    ];

    public Storm_C()
        : base("storm", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<StormPower>(Owner.Creature, DynamicVars["Storm"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Storm"].UpgradeValueBy(1m);
    }
}

// STS1 Static Discharge: 1 energy, whenever you take unblocked attack damage, channel 1 (2 upgraded) Lightning.
public sealed class StaticDischarge_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("StaticDischarge", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
    ];

    public StaticDischarge_C()
        : base("static_discharge", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<StaticDischargePower_C>(Owner.Creature,
            DynamicVars["StaticDischarge"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StaticDischarge"].UpgradeValueBy(1m);
    }
}

using System.Linq;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
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
// DEFECT RARE ATTACKS (5)
// ═══════════════════════════════════════════════════════════════════

// STS1 All For One: 2 energy, 10 damage (14 upgraded). Put all 0-cost cards from discard into hand.
public sealed class AllForOne_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(10m, ValueProp.Move)];

    public AllForOne_C()
        : base("all_for_one", 2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        var zeroCostCards = PileType.Discard.GetPile(Owner).Cards
            .Where(c => c.EnergyCost.GetWithModifiers(CostModifiers.All) == 0
                        && !c.EnergyCost.CostsX)
            .ToList();
        foreach (CardModel card in zeroCostCards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// STS1 Core Surge: 1 energy, 11 damage (15 upgraded). Gain 1 Artifact. Exhaust.
public sealed class CoreSurge_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(11m, ValueProp.Move),
        new PowerVar<ArtifactPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ArtifactPower>()];

    public CoreSurge_C()
        : base("core_surge", 1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_lightning", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// STS1 Hyper Beam: 2 energy, deal 26 damage (34 upgraded) to ALL. Lose 3 Focus.
public sealed class HyperBeam_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(26m, ValueProp.Move),
        new PowerVar<FocusPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FocusPower>()];

    public HyperBeam_C()
        : base("hyper_beam", 2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_lightning", null, "heavy_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
        await PowerCmd.Apply<FocusPower>(Owner.Creature, -DynamicVars["FocusPower"].BaseValue,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}

// STS1 Meteor Strike: 5 energy, 24 damage (30 upgraded). Channel 3 Plasma.
public sealed class MeteorStrike_C : ClassicDefectCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(24m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<PlasmaOrb>()
    ];

    public MeteorStrike_C()
        : base("meteor_strike", 5, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        for (int i = 0; i < 3; i++)
        {
            await OrbCmd.Channel<PlasmaOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}

// STS1 Thunder Strike: 3 energy, deal 7 damage (9 upgraded) to a random enemy for each Lightning channeled this combat.
public sealed class ThunderStrike_C : ClassicDefectCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromOrb<LightningOrb>()];

    public ThunderStrike_C()
        : base("thunder_strike", 3, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int lightningCount = CombatManager.Instance.History.Entries.OfType<OrbChanneledEntry>()
            .Count(e => e.Actor.Player == Owner && e.Orb is LightningOrb);
        if (lightningCount > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(lightningCount)
                .FromCard(this).TargetingRandomOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_lightning")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// DEFECT RARE SKILLS (5)
// ═══════════════════════════════════════════════════════════════════

// STS1 Amplify: 1 energy (0 upgraded), your next Power is played twice this turn.
public sealed class Amplify_C : ClassicDefectCard
{
    public Amplify_C()
        : base("amplify", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<AmplifyPower_C>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Double Energy: 1 energy (0 upgraded), double your energy. Exhaust.
// Portrait: double_energy.png
public sealed class DoubleEnergy_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Energy)];

    public DoubleEnergy_C()
        : base("double_energy", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int currentEnergy = Owner.PlayerCombatState.Energy;
        if (currentEnergy > 0)
        {
            await PlayerCmd.GainEnergy(currentEnergy, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Multi-Cast: X energy, evoke front Orb X times (X+1 upgraded).
public sealed class MultiCast_C : ClassicDefectCard
{
    protected override bool HasEnergyCostX => true;

    public override OrbEvokeType OrbEvokeType => OrbEvokeType.Front;

    public MultiCast_C()
        : base("multicast", -1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        int evokeCount = ResolveEnergyXValue();
        if (IsUpgraded)
            evokeCount++;
        for (int i = 0; i < evokeCount; i++)
        {
            bool dequeue = i == evokeCount - 1;
            await OrbCmd.EvokeNext(choiceContext, Owner, dequeue);
            await Cmd.Wait(0.25f);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

// STS1 Rainbow: moved to uncommon (matching STS1 rarity).
// Already implemented above in Uncommons.

// STS1 Recycle: 1 energy (0 upgraded), Exhaust a card from hand. Gain energy equal to its cost.
public sealed class Recycle_C : ClassicDefectCard
{
    public Recycle_C()
        : base("recycle", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        CardPile pile = PileType.Hand.GetPile(Owner);
        int handCount = pile.Cards.Count();
        if (handCount == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        CardModel card = (await CardSelectCmd.FromSimpleGrid(choiceContext, pile.Cards, Owner, prefs))
            .FirstOrDefault();
        if (card != null)
        {
            int cost = card.EnergyCost.GetWithModifiers(CostModifiers.All);
            if (card.EnergyCost.CostsX)
                cost = 0;
            await CardCmd.Exhaust(choiceContext, card);
            if (cost > 0)
            {
                await PlayerCmd.GainEnergy(cost, Owner);
            }
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Reboot: already in Commons (STS1 had it as Common, rarity varies by version).
// If it was actually Rare in STS1, it's already implemented as Reboot_C in Commons.

// ═══════════════════════════════════════════════════════════════════
// DEFECT RARE POWERS (8)
// ═══════════════════════════════════════════════════════════════════

// STS1 Biased Cognition: 1 energy, gain 4 Focus (5 upgraded). Lose 1 Focus at end of each turn.
public sealed class BiasedCognition_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FocusPower>(4m),
        new PowerVar<BiasedCognitionPower>(1m)
    ];

    public BiasedCognition_C()
        : base("biased_cognition", 1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<FocusPower>(Owner.Creature, DynamicVars["FocusPower"].BaseValue,
            Owner.Creature, this);
        await PowerCmd.Apply<BiasedCognitionPower>(Owner.Creature,
            DynamicVars["BiasedCognitionPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["FocusPower"].UpgradeValueBy(1m);
    }
}

// STS1 Creative AI: 3 energy (2 upgraded), at start of turn generate a random Power card.
public sealed class CreativeAi_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("CreativeAi", 1m)];

    public CreativeAi_C()
        : base("creative_ai", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<CreativeAiPower>(Owner.Creature,
            DynamicVars["CreativeAi"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Echo Form: 3 energy, Ethereal. First card each turn is played twice.
public sealed class EchoForm_C : ClassicDefectCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("EchoForm", 1m)];

    public EchoForm_C()
        : base("echo_form", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<EchoFormPower>(Owner.Creature,
            DynamicVars["EchoForm"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}

// STS1 Electrodynamics: 2 energy, channel 2 Lightning (3 upgraded). Lightning hits ALL enemies.
public sealed class Electrodynamics_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new RepeatVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<LightningOrb>()
    ];

    public Electrodynamics_C()
        : base("electrodynamics", 2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<ElectrodynamicsPower_C>(Owner.Creature, 1m, Owner.Creature, this);
        for (int i = 0; i < DynamicVars.Repeat.IntValue; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// STS1 Machine Learning: 1 energy, at the start of each turn draw 1 extra card. Innate (upgraded).
public sealed class MachineLearning_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public MachineLearning_C()
        : base("machine_learning", 1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<MachineLearningPower>(Owner.Creature,
            DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}

// STS1 Self Repair: 1 energy, at end of combat heal 7 HP (10 upgraded).
public sealed class SelfRepair_C : ClassicDefectCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Heal", 7m)];

    public SelfRepair_C()
        : base("self_repair", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<SelfRepairPower_C>(Owner.Creature,
            DynamicVars["Heal"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(3m);
    }
}

// STS1 Storm: moved to Uncommon in this implementation (STS1 had it as Uncommon).
// Already implemented above in Uncommons as Storm_C.

// STS1 Lightning Mastery: portrait exists but this is a STS2-original card concept.
// Not an STS1 card -- skip.

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// ATTACKS (6)
// ═══════════════════════════════════════════════════════════════════

// STS1 Bludgeon: 3 energy, 32 damage (42 upgraded)
public sealed class Bludgeon_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(32m, ValueProp.Move)];

    public Bludgeon_C()
        : base("bludgeon", 3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(10m);
    }
}

// STS1 Feed: 1 energy, 10 damage (12 upgraded). If kill, gain 3 (4) Max HP. Exhaust.
public sealed class Feed_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DynamicVar("MaxHp", 3m)
    ];

    public Feed_C()
        : base("feed", 1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool shouldTriggerFatal = cardPlay.Target.Powers
            .All(p => p.ShouldOwnerDeathTriggerFatal());
        AttackCommand result = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bite", null, "blunt_attack.mp3")
            .Execute(choiceContext);

        if (shouldTriggerFatal && result.Results.Any(r => r.WasTargetKilled))
        {
            decimal maxHpGain = DynamicVars["MaxHp"].BaseValue;
            await CreatureCmd.GainMaxHp(Owner.Creature, maxHpGain);
            await CreatureCmd.Heal(Owner.Creature, maxHpGain);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars["MaxHp"].UpgradeValueBy(1m);
    }
}

// STS1 Fiend Fire: 2 energy, exhaust entire hand, deal 7 damage per card (10 upgraded). Exhaust.
public sealed class FiendFire_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    public FiendFire_C()
        : base("fiend_fire", 2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        List<CardModel> handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        int exhaustedCount = 0;
        foreach (CardModel card in handCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
            exhaustedCount++;
        }

        if (exhaustedCount > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitCount(exhaustedCount)
                .WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// STS1 Immolate: 2 energy, deal 21 damage to ALL (28 upgraded). Add a Burn to discard.
public sealed class Immolate_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<Burn>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(21m, ValueProp.Move)];

    public Immolate_C()
        : base("immolate", 2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
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

        CardModel burn = CombatState.CreateCard<Burn>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(burn, PileType.Discard, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(7m);
    }
}

// STS1 Reaper: 2 energy, deal 4 damage to ALL (5 upgraded). Heal for unblocked damage. Exhaust.
public sealed class Reaper_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(4m, ValueProp.Move)];

    public Reaper_C()
        : base("reaper", 2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        AttackCommand result = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_giant_horizontal_slash", null, "slash_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);

        int totalHeal = result.Results.Sum(r => r.UnblockedDamage);
        if (totalHeal > 0)
        {
            await CreatureCmd.Heal(Owner.Creature, totalHeal);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}

// STS1 Whirlwind: X energy, deal 5 damage to ALL X times (8 upgraded).
public sealed class Whirlwind_C : ClassicIroncladCard
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    public Whirlwind_C()
        : base("whirlwind", -1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(CombatState);
        int x = ResolveEnergyXValue();
        for (int i = 0; i < x; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
                .TargetingAllOpponents(CombatState)
                .WithHitFx("vfx/vfx_giant_horizontal_slash")
                .SpawningHitVfxOnEachCreature()
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// SKILLS (5)
// ═══════════════════════════════════════════════════════════════════

// STS1 Double Tap: 1 energy, next Attack played twice (next 2 upgraded).
public sealed class DoubleTap_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DuplicationPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 1m)];

    public DoubleTap_C()
        : base("double_tap", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DuplicationPower>(
            Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1m);
    }
}

// STS1 Exhume: 1 energy (0 upgraded), put a card from Exhaust into hand. Exhaust.
public sealed class Exhume_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Exhume_C()
        : base("exhume", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> exhaustCards = PileType.Exhaust.GetPile(Owner).Cards.ToList();
        if (exhaustCards.Count == 0)
        {
            return;
        }

        CardSelectorPrefs prefs = new(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
        IEnumerable<CardModel> selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, exhaustCards, Owner, prefs);

        CardModel? chosenCard = selected.FirstOrDefault();
        if (chosenCard != null)
        {
            await CardPileCmd.Add(chosenCard, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Impervious: 2 energy, gain 30 block (40 upgraded). Exhaust.
public sealed class Impervious_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(30m, ValueProp.Move)];

    public Impervious_C()
        : base("impervious", 2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(10m);
    }
}

// STS1 Limit Break: 1 energy, double your Strength. Exhaust (no exhaust upgraded).
public sealed class LimitBreak_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [] : [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public LimitBreak_C()
        : base("limit_break", 1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal currentStrength = Owner.Creature.GetPowerAmount<StrengthPower>();
        if (currentStrength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(
                Owner.Creature, currentStrength, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // Exhaust keyword removed via CanonicalKeywords check
    }
}

// STS1 Offering: 0 energy, lose 6 HP, gain 2 energy, draw 3 cards (5 upgraded). Exhaust.
public sealed class Offering_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Draw", 3m)];

    public Offering_C()
        : base("offering", 0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 6m,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await PlayerCmd.GainEnergy(2m, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(2m);
    }
}

// ═══════════════════════════════════════════════════════════════════
// POWERS (4)
// ═══════════════════════════════════════════════════════════════════

// STS1 Barricade: 3 energy (2 upgraded). Block not removed at start of turn.
public sealed class Barricade_C : ClassicIroncladCard
{
    public Barricade_C()
        : base("barricade", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BarricadePower>(
            Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Brutality: 0 energy. At start of turn, lose 1 HP and draw 1 card. Innate when upgraded.
public sealed class Brutality_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [CardKeyword.Innate] : [];

    public Brutality_C()
        : base("brutality", 0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BrutalityPower_C>(
            Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // Innate keyword added via CanonicalKeywords check
    }
}

// STS1 Corruption: 3 energy (2 upgraded). Skills cost 0. Playing a Skill exhausts it.
public sealed class Corruption_C : ClassicIroncladCard
{
    public Corruption_C()
        : base("corruption", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CorruptionPower>(
            Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Demon Form: 3 energy. At start of turn, gain 2 Strength (3 upgraded).
public sealed class DemonForm_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Strength", 2m)];

    public DemonForm_C()
        : base("demon_form", 3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DemonFormPower>(
            Owner.Creature, DynamicVars["Strength"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1m);
    }
}

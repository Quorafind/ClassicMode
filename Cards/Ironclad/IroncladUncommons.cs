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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// =============================================================================
// IRONCLAD UNCOMMON ATTACKS (11)
// =============================================================================

// STS1 Blood for Blood: 4 energy, 18 damage (22 upgraded).
// Costs 1 less energy for each time you lose HP this combat.
public sealed class BloodForBlood_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(18m, ValueProp.Move)];

    public BloodForBlood_C()
        : base("blood_for_blood", 4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override void AfterCreated()
    {
        base.AfterCreated();

        if (!CombatManager.Instance.IsInProgress)
        {
            return;
        }

        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        var priorLosses = CombatManager.Instance.History.Entries
            .OfType<DamageReceivedEntry>()
            .Count(e => e.Receiver == Owner.Creature && e.Result.UnblockedDamage > 0);

        if (priorLosses > 0)
        {
            EnergyCost.AddThisCombat(-priorLosses, reduceOnly: true);
        }
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _ = choiceContext;
        _ = props;
        _ = dealer;
        _ = cardSource;

        if (!IsInCombat)
        {
            return Task.CompletedTask;
        }

        if (target == Owner.Creature && result.UnblockedDamage > 0)
        {
            EnergyCost.AddThisCombat(-1, reduceOnly: true);
        }

        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// STS1 Carnage: 2 energy, Ethereal, 20 damage (28 upgraded).
public sealed class Carnage_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(20m, ValueProp.Move)];

    public Carnage_C()
        : base("carnage", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}

// STS1 Dropkick: 1 energy, 5 damage (8 upgraded).
// If enemy has Vulnerable, gain 1 energy and draw 1 card.
public sealed class Dropkick_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    protected override bool ShouldGlowGoldInternal =>
        CombatState?.HittableEnemies.Any(e => e.HasPower<VulnerablePower>()) ?? false;

    public Dropkick_C()
        : base("drop_kick", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        bool hadVulnerable = cardPlay.Target.HasPower<VulnerablePower>();
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        if (hadVulnerable)
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

// STS1 Heavy Blade: 2 energy, 14 damage (14 upgraded).
// Strength affects this card 3 times (5 upgraded).
public sealed class HeavyBlade_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(14m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
                card.Owner.Creature.GetPowerAmount<StrengthPower>()
                * (card.DynamicVars["StrengthMultiplier"].IntValue - 1)),
        new DynamicVar("StrengthMultiplier", 3m)
    ];

    public HeavyBlade_C()
        : base("heavy_blade", 2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthMultiplier"].UpgradeValueBy(2m);
    }
}

// STS1 Hemokinesis: 1 energy, lose 2 HP, deal 15 damage (20 upgraded). COLLISION with STS2.
public sealed class Hemokinesis_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(2m),
        new DamageVar(15m, ValueProp.Move)
    ];

    public Hemokinesis_C()
        : base("hemokinesis", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_bloody_impact")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

// STS1 Pummel: 1 energy, deal 2 damage 4 times (5 upgraded). Exhaust.
public sealed class Pummel_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new RepeatVar(4)
    ];

    public Pummel_C()
        : base("pummel", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// STS1 Rampage: 1 energy, deal 8 damage. Increase damage by 5 each play (8 upgraded). COLLISION.
public sealed class Rampage_C : ClassicIroncladCard
{
    private const string IncreaseKey = "Increase";
    private decimal _extraDamageFromPlays;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new DynamicVar(IncreaseKey, 5m)
    ];

    private decimal ExtraDamageFromPlays
    {
        get => _extraDamageFromPlays;
        set
        {
            AssertMutable();
            _extraDamageFromPlays = value;
        }
    }

    public Rampage_C()
        : base("rampage", 1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        DynamicVars.Damage.BaseValue += DynamicVars[IncreaseKey].BaseValue;
        ExtraDamageFromPlays += DynamicVars[IncreaseKey].BaseValue;
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        DynamicVars.Damage.BaseValue += ExtraDamageFromPlays;
    }

    protected override void OnUpgrade()
    {
        DynamicVars[IncreaseKey].UpgradeValueBy(3m);
    }
}

// STS1 Reckless Charge: 0 energy, 7 damage (10 upgraded). Shuffle a Dazed into draw pile.
public sealed class RecklessCharge_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(7m, ValueProp.Move)];

    public RecklessCharge_C()
        : base("reckless_charge", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        var dazed = CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(dazed, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// STS1 Searing Blow: 2 energy, 12 damage. Can be upgraded any number of times.
public sealed class SearingBlow_C : ClassicIroncladCard
{
    public override int MaxUpgradeLevel => int.MaxValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    public SearingBlow_C()
        : base("searing_blow", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // STS1 upgrades add 4 + timesUpgraded damage before incrementing the card's displayed +N.
        // In this API, CurrentUpgradeLevel has already been incremented before OnUpgrade runs,
        // so the equivalent increment is 3 + CurrentUpgradeLevel.
        DynamicVars.Damage.UpgradeValueBy(3m + CurrentUpgradeLevel);
    }
}

// STS1 Sever Soul: 2 energy, 16 damage (22 upgraded). Exhaust all non-Attack cards in hand.
public sealed class SeverSoul_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(16m, ValueProp.Move)];

    public SeverSoul_C()
        : base("sever_soul", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // Exhaust all non-Attack cards in hand first
        var nonAttacks = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Type != CardType.Attack).ToList();
        foreach (var card in nonAttacks)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab", null, "slash_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
    }
}

// STS1 Uppercut: 2 energy, 13 damage (18 upgraded). Apply 1 Weak + 1 Vulnerable (2 each upgraded). COLLISION.
public sealed class Uppercut_C : ClassicIroncladCard
{
    private const string PowerKey = "Power";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13m, ValueProp.Move),
        new DynamicVar(PowerKey, 1m)
    ];

    public Uppercut_C()
        : base("uppercut", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        int amount = DynamicVars[PowerKey].IntValue;
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, amount, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars[PowerKey].UpgradeValueBy(1m);
    }
}

// =============================================================================
// IRONCLAD UNCOMMON SKILLS (17)
// =============================================================================

// STS1 Battle Trance: 0 energy, draw 3 cards (4 upgraded). Cannot draw additional cards this turn. COLLISION.
public sealed class BattleTrance_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(3)];

    public BattleTrance_C()
        : base("battle_trance", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PowerCmd.Apply<NoDrawPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Bloodletting: 0 energy, lose 3 HP, gain 2 energy (3 upgraded). COLLISION.
public sealed class Bloodletting_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(3m),
        new EnergyVar(2)
    ];

    public Bloodletting_C()
        : base("bloodletting", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

// STS1 Burning Pact: 1 energy, exhaust 1 card, draw 2 cards (3 upgraded). COLLISION.
public sealed class BurningPact_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(2)];

    public BurningPact_C()
        : base("burning_pact", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            context: choiceContext, player: Owner, filter: null, source: this)).FirstOrDefault();
        if (selected != null)
        {
            await CardCmd.Exhaust(choiceContext, selected);
        }
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Disarm: 1 energy, enemy loses 2 Strength (3 upgraded). Exhaust.
public sealed class Disarm_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(2m)];

    public Disarm_C()
        : base("disarm", 1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<StrengthPower>(cardPlay.Target, -DynamicVars.Strength.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}

// STS1 Dual Wield: 1 energy, copy an Attack or Power card in hand (2 copies upgraded). COLLISION.
public sealed class DualWield_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public DualWield_C()
        : base("dual_wield", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // STS1 Dual Wield semantics:
        //   0 eligible cards in hand → no selection screen, no effect
        //   1 eligible card → auto-pick without showing the screen
        //   2+ eligible cards → show CardSelect screen as normal
        var eligible = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this && (c.Type == CardType.Attack || c.Type == CardType.Power))
            .ToList();

        if (eligible.Count == 0)
            return;

        CardModel? selection;
        if (eligible.Count == 1)
        {
            selection = eligible[0];
        }
        else
        {
            selection = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext, player: Owner,
                filter: c => c.Type == CardType.Attack || c.Type == CardType.Power,
                source: this)).FirstOrDefault();
        }

        if (selection == null)
            return;

        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
        {
            var clone = selection.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Entrench: 2 energy (1 upgraded), double current Block. COLLISION.
public sealed class Entrench_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    public Entrench_C()
        : base("entrench", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, Owner.Creature.Block,
            ValueProp.Unpowered | ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Flame Barrier: 2 energy, gain 12 block (16 upgraded), deal 4 damage back (6 upgraded). COLLISION.
public sealed class FlameBarrier_C : ClassicIroncladCard
{
    private const string DamageBackKey = "DamageBack";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(12m, ValueProp.Move),
        new DynamicVar(DamageBackKey, 4m)
    ];

    public FlameBarrier_C()
        : base("flame_barrier", 2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<FlameBarrierPower>(Owner.Creature, DynamicVars[DamageBackKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars[DamageBackKey].UpgradeValueBy(2m);
    }
}

// STS1 Ghostly Armor: 1 energy, Ethereal, gain 10 block (13 upgraded).
public sealed class GhostlyArmor_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(10m, ValueProp.Move)];

    public GhostlyArmor_C()
        : base("ghostly_armor", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

// STS1 Infernal Blade: 1 energy (0 upgraded), add a random Attack to hand costing 0 this turn. Exhaust. COLLISION.
public sealed class InfernalBlade_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    public InfernalBlade_C()
        : base("infernal_blade", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var generated = CardFactory.GetDistinctForCombat(
            Owner,
            from c in Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            where c.Type == CardType.Attack
            select c,
            1,
            Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (generated != null)
        {
            generated.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Intimidate: 0 energy, apply 1 Weak to ALL enemies (2 upgraded). Exhaust.
public sealed class Intimidate_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(1m)];

    public Intimidate_C()
        : base("intimidate", 0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars.Weak.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
    }
}

// STS1 Power Through: 1 energy, add 2 Wounds to hand, gain 15 block (20 upgraded).
public sealed class PowerThrough_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromCard<MegaCrit.Sts2.Core.Models.Cards.Wound>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(15m, ValueProp.Move)];

    public PowerThrough_C()
        : base("power_through", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        for (int i = 0; i < 2; i++)
        {
            var wound = CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Hand, Owner);
        }
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}

// STS1 Rage: 0 energy, whenever you play an Attack this turn, gain 3 Block (5 upgraded). COLLISION.
public sealed class Rage_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<RagePower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<RagePower>(3m)];

    public Rage_C()
        : base("rage", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RagePower>(Owner.Creature, DynamicVars["RagePower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RagePower"].UpgradeValueBy(2m);
    }
}

// STS1 Second Wind: 1 energy, exhaust all non-Attack cards in hand, gain 5 Block (7 upgraded) for each. COLLISION.
public sealed class SecondWind_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move)];

    public SecondWind_C()
        : base("second_wind", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var nonAttacks = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.Type != CardType.Attack).ToList();
        foreach (var card in nonAttacks)
        {
            await CardCmd.Exhaust(choiceContext, card);
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// STS1 Seeing Red: 1 energy (0 upgraded), gain 2 energy. Exhaust.
public sealed class SeeingRed_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(2)];

    public SeeingRed_C()
        : base("seeing_red", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Sentinel: 1 energy, gain 5 block (8 upgraded).
// If this card is Exhausted, gain 2 energy (3 upgraded).
public sealed class Sentinel_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [EnergyHoverTip];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new EnergyVar(2)
    ];

    public Sentinel_C()
        : base("sentinel", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card != this)
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}

// STS1 Shockwave: 2 energy, apply 3 Weak and 3 Vulnerable to ALL enemies (5 upgraded). Exhaust. COLLISION.
public sealed class Shockwave_C : ClassicIroncladCard
{
    private const string PowerKey = "Power";

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<VulnerablePower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(PowerKey, 3m)];

    public Shockwave_C()
        : base("shockwave", 2, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = DynamicVars[PowerKey].IntValue;
        foreach (Creature enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<WeakPower>(enemy, amount, Owner.Creature, this);
            await PowerCmd.Apply<VulnerablePower>(enemy, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[PowerKey].UpgradeValueBy(2m);
    }
}

// STS1 Spot Weakness: 1 energy, if enemy intends to attack, gain 3 Strength (4 upgraded).
public sealed class SpotWeakness_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(3m)];

    // Glow gold when target enemy intends to attack
    protected override bool ShouldGlowGoldInternal =>
        CombatState?.HittableEnemies.Any(e => e.Monster?.IntendsToAttack ?? false) ?? false;

    public SpotWeakness_C()
        : base("spot_weakness", 1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        // Check if the target enemy intends to attack
        bool isAttacking = cardPlay.Target.Monster?.IntendsToAttack ?? false;
        if (isAttacking)
        {
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}

// =============================================================================
// IRONCLAD UNCOMMON POWERS (10)
// =============================================================================

// STS1 Berserk: 0 energy, gain 2 Vulnerable to self (1 upgraded). At start of each turn, gain 1 energy.
public sealed class Berserk_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VulnerablePower>(2m),
        new EnergyVar(1)
    ];

    public Berserk_C()
        : base("berserk", 0, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VulnerablePower>(Owner.Creature, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<BerserkPower_C>(Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Vulnerable.UpgradeValueBy(-1m);
    }
}

// STS1 Combust: 1 energy, at end of each turn, lose 1 HP and deal 5 damage to ALL enemies (7 upgraded).
public sealed class Combust_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 5m)];

    public Combust_C()
        : base("combust", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CombustPower_C>(Owner.Creature, DynamicVars["MagicNumber"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(2m);
    }
}

// STS1 Dark Embrace: 2 energy (1 upgraded), whenever a card is Exhausted, draw 1 card. COLLISION.
public sealed class DarkEmbrace_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public DarkEmbrace_C()
        : base("dark_embrace", 2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DarkEmbracePower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// STS1 Evolve: 1 energy, whenever you draw a Status card, draw 1 card (2 upgraded).
public sealed class Evolve_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public Evolve_C()
        : base("evolve", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<EvolvePower_C>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// STS1 Fire Breathing: 1 energy, whenever you draw a Status or Curse, deal 6 damage to ALL enemies (10 upgraded).
public sealed class FireBreathing_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 6m)];

    public FireBreathing_C()
        : base("fire_breathing", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FireBreathingPower_C>(Owner.Creature, DynamicVars["MagicNumber"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(4m);
    }
}

// STS1 Inflame: 1 energy, gain 2 Strength (3 upgraded). COLLISION.
public sealed class Inflame_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(2m)];

    public Inflame_C()
        : base("inflame", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(1m);
    }
}

// STS1 Juggernaut: 2 energy, whenever you gain Block, deal 5 damage to random enemy (7 upgraded). COLLISION.
public sealed class Juggernaut_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<JuggernautPower>(5m)];

    public Juggernaut_C()
        : base("juggernaut", 2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<JuggernautPower>(Owner.Creature, DynamicVars["JuggernautPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["JuggernautPower"].UpgradeValueBy(2m);
    }
}

// STS1 Metallicize: 1 energy, at end of turn gain 3 Block (4 upgraded).
public sealed class Metallicize_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MagicNumber", 3m)];

    public Metallicize_C()
        : base("metallicize", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<MetallicizePower_C>(Owner.Creature, DynamicVars["MagicNumber"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["MagicNumber"].UpgradeValueBy(1m);
    }
}

// STS1 Rupture: 1 energy, whenever you lose HP from a card, gain 1 Strength (2 upgraded). COLLISION.
public sealed class Rupture_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<StrengthPower>(1m)];

    public Rupture_C()
        : base("rupture", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RupturePower_C>(Owner.Creature, DynamicVars.Strength.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
    }
}

// STS1 Feel No Pain: 1 energy, whenever a card is Exhausted, gain 3 Block (4 upgraded). COLLISION.
public sealed class FeelNoPain_C : ClassicIroncladCard
{
    private const string PowerVarName = "Power";

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.Static(StaticHoverTip.Block)
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar(PowerVarName, 3m)];

    public FeelNoPain_C()
        : base("feel_no_pain", 1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FeelNoPainPower>(Owner.Creature, DynamicVars[PowerVarName].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[PowerVarName].UpgradeValueBy(1m);
    }
}

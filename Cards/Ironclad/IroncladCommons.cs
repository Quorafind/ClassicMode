using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ────────────────────────────────────────────────────────────────────────────
// Flex temporary-strength power (required because TemporaryStrengthPower is abstract)
// ────────────────────────────────────────────────────────────────────────────

public class FlexPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<Flex_C>();
}

// ────────────────────────────────────────────────────────────────────────────
// 1. Anger  (STS2 collision → Anger_C)
//    0 cost · Attack · AnyEnemy · 6 dmg (upg 8) · Add copy to discard
// ────────────────────────────────────────────────────────────────────────────

public sealed class Anger_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(6m, ValueProp.Move)];

    public Anger_C()
        : base("anger", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        CardModel card = CreateClone();
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Discard, Owner), 2.2f);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 2. Armaments  (STS2 collision → Armaments_C)
//    1 cost · Skill · Self · 5 block (upg 5) · Upgrade 1 card in hand (upg ALL)
// ────────────────────────────────────────────────────────────────────────────

public sealed class Armaments_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(5m, ValueProp.Move)];

    public Armaments_C()
        : base("armaments", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (IsUpgraded)
        {
            foreach (var c in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable))
                CardCmd.Upgrade(c);
        }
        else
        {
            var card = await CardSelectCmd.FromHandForUpgrade(choiceContext, Owner, this);
            if (card != null)
                CardCmd.Upgrade(card);
        }
    }

    protected override void OnUpgrade()
    {
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 3. BodySlam  (STS2 collision → BodySlam_C)
//    1 cost · Attack · AnyEnemy · Dmg = current Block · Upgrade: cost → 0
// ────────────────────────────────────────────────────────────────────────────

public sealed class BodySlam_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new ExtraDamageVar(1m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) => card.Owner.Creature.Block)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Block)];

    public BodySlam_C()
        : base("body_slam", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt", null, "heavy_attack.mp3")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 4. Clash  (STS2 collision → Clash_C)
//    0 cost · Attack · AnyEnemy · 14 dmg (upg 18) · Only playable if all hand cards are Attacks
// ────────────────────────────────────────────────────────────────────────────

public sealed class Clash_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(14m, ValueProp.Move)];

    protected override bool IsPlayable =>
        CardPile.GetCards(Owner, PileType.Hand).All(c => c.Type == CardType.Attack);

    protected override bool ShouldGlowGoldInternal => IsPlayable;

    public Clash_C()
        : base("clash", 0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 5. Cleave  (no STS2 collision → bare name)
//    1 cost · Attack · AllEnemies · 8 dmg ALL (upg 11)
// ────────────────────────────────────────────────────────────────────────────

public sealed class Cleave_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(8m, ValueProp.Move)];

    public Cleave_C()
        : base("cleave", 1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_giant_horizontal_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 6. Clothesline  (no STS2 collision → bare name)
//    2 cost · Attack · AnyEnemy · 12 dmg (upg 14) + 2 Weak (upg 3)
// ────────────────────────────────────────────────────────────────────────────

public sealed class Clothesline_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<WeakPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    public Clothesline_C()
        : base("clothesline", 2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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

// ────────────────────────────────────────────────────────────────────────────
// 7. Flex  (no STS2 collision → bare name)
//    0 cost · Skill · Self · Gain 2 Str (upg 4), lose it at end of turn
//    Uses FlexPower (TemporaryStrengthPower subclass) which auto-grants
//    StrengthPower on apply and removes both at turn end.
// ────────────────────────────────────────────────────────────────────────────

public sealed class Flex_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Strength", 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public Flex_C()
        : base("flex", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var amount = DynamicVars["Strength"].BaseValue;
        await PowerCmd.Apply<FlexPower>(Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 8. Havoc  (STS2 collision → Havoc_C)
//    1 cost · Skill · Self · Auto-play top of draw pile and exhaust it · Upg: cost → 0
// ────────────────────────────────────────────────────────────────────────────

public sealed class Havoc_C : ClassicIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public Havoc_C()
        : base("havoc", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner, 1, CardPilePosition.Top, forceExhaust: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 9. Headbutt  (STS2 collision → Headbutt_C)
//    1 cost · Attack · AnyEnemy · 9 dmg (upg 12) · Put a card from discard on top of draw
// ────────────────────────────────────────────────────────────────────────────

public sealed class Headbutt_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(9m, ValueProp.Move)];

    public Headbutt_C()
        : base("headbutt", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var pile = PileType.Discard.GetPile(Owner);
        var card = (await CardSelectCmd.FromSimpleGrid(choiceContext, pile.Cards, Owner, prefs)).FirstOrDefault();
        if (card != null)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 10. IronWave  (STS2 collision → IronWave_C)
//     1 cost · Attack · AnyEnemy · 5 dmg + 5 block (upg both 7)
// ────────────────────────────────────────────────────────────────────────────

public sealed class IronWave_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new BlockVar(5m, ValueProp.Move)
    ];

    public IronWave_C()
        : base("iron_wave", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 11. PerfectedStrike  (STS2 collision → PerfectedStrike_C)
//     2 cost · Attack · AnyEnemy · 6 base + 2 per Strike card (upg 3 per)
// ────────────────────────────────────────────────────────────────────────────

public sealed class PerfectedStrike_C : ClassicIroncladCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    private static bool CountsAsStrikeNamedCard(CardModel card)
    {
        string localizedTitle = card.TitleLocString.GetFormattedText();
        return localizedTitle.Contains("Strike", StringComparison.OrdinalIgnoreCase)
            || card.Id.Entry.Contains("strike", StringComparison.OrdinalIgnoreCase);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(6m),
        new ExtraDamageVar(2m),
        new CalculatedDamageVar(ValueProp.Move)
            .WithMultiplier((CardModel card, Creature? _) =>
                card.Owner?.PlayerCombatState?.AllCards.Count(CountsAsStrikeNamedCard) ?? 0)
    ];

    public PerfectedStrike_C()
        : base("perfected_strike", 2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.CalculatedDamage).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.ExtraDamage.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 12. PommelStrike  (STS2 collision → PommelStrike_C)
//     1 cost · Attack · AnyEnemy · 9 dmg (upg 10) + Draw 1 (upg 2)
// ────────────────────────────────────────────────────────────────────────────

public sealed class PommelStrike_C : ClassicIroncladCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new CardsVar(1)
    ];

    public PommelStrike_C()
        : base("pommel_strike", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 13. ShrugItOff  (STS2 collision → ShrugItOff_C)
//     1 cost · Skill · Self · 8 block (upg 11) + Draw 1
// ────────────────────────────────────────────────────────────────────────────

public sealed class ShrugItOff_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new CardsVar(1)
    ];

    public ShrugItOff_C()
        : base("shrug_it_off", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
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
// 14. SwordBoomerang  (STS2 collision → SwordBoomerang_C)
//     1 cost · Attack · RandomEnemy · 3 dmg × 3 hits (upg 4 hits)
// ────────────────────────────────────────────────────────────────────────────

public sealed class SwordBoomerang_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new RepeatVar(3)
    ];

    public SwordBoomerang_C()
        : base("sword_boomerang", 1, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this)
            .TargetingRandomOpponents(CombatState)
            .WithHitFx("vfx/vfx_flying_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Repeat.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 15. ThunderClap  (STS2 has "Thunderclap" but our name is "ThunderClap_C" → no collision)
//     1 cost · Attack · AllEnemies · 4 dmg ALL (upg 7) + 1 Vuln to ALL
// ────────────────────────────────────────────────────────────────────────────

public sealed class ThunderClap_C : ClassicIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new PowerVar<VulnerablePower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<VulnerablePower>()];

    public ThunderClap_C()
        : base("thunder_clap", 1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
            .Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(
            CombatState.HittableEnemies, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 16. TrueGrit  (STS2 collision → TrueGrit_C)
//     1 cost · Skill · Self · 7 block (upg 9) · Exhaust random card (upg: choose)
// ────────────────────────────────────────────────────────────────────────────

public sealed class TrueGrit_C : ClassicIroncladCard
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(7m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    public TrueGrit_C()
        : base("true_grit", 1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (IsUpgraded)
        {
            var card = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
                context: choiceContext,
                player: Owner,
                filter: null,
                source: this)).FirstOrDefault();
            if (card != null)
                await CardCmd.Exhaust(choiceContext, card);
        }
        else
        {
            var pile = PileType.Hand.GetPile(Owner);
            var card = Owner.RunState.Rng.CombatCardSelection.NextItem(pile.Cards);
            if (card != null)
                await CardCmd.Exhaust(choiceContext, card);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 17. TwinStrike  (STS2 collision → TwinStrike_C)
//     1 cost · Attack · AnyEnemy · 5 dmg × 2 (upg 7)
// ────────────────────────────────────────────────────────────────────────────

public sealed class TwinStrike_C : ClassicIroncladCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(5m, ValueProp.Move)];

    public TwinStrike_C()
        : base("twin_strike", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).WithHitCount(2).FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 18. Warcry  (no STS2 collision → bare name)
//     0 cost · Skill · Self · Draw 1 (upg 2) · Put a card from hand on top of draw · Exhaust
// ────────────────────────────────────────────────────────────────────────────

public sealed class Warcry_C : ClassicIroncladCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(1)];

    public Warcry_C()
        : base("warcry", 0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        // STS1 Warcry semantics:
        //   0 cards in hand (after draw, excluding self) → no selection screen
        //   1 card → auto-pick without showing the screen
        //   2+ cards → show CardSelect as normal
        var hand = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (hand.Count == 0)
            return;

        CardModel? card;
        if (hand.Count == 1)
        {
            card = hand[0];
        }
        else
        {
            card = (await CardSelectCmd.FromHand(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                player: Owner,
                filter: null,
                source: this)).FirstOrDefault();
        }

        if (card != null)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}

// ────────────────────────────────────────────────────────────────────────────
// 19. WildStrike  (no STS2 collision → bare name)
//     1 cost · Attack · AnyEnemy · 12 dmg (upg 17) · Shuffle a Wound into draw pile
// ────────────────────────────────────────────────────────────────────────────

public sealed class WildStrike_C : ClassicIroncladCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(12m, ValueProp.Move)];

    public WildStrike_C()
        : base("wild_strike", 1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.AddGeneratedCardToCombat(
            CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner),
            PileType.Draw,
            Owner,
            CardPilePosition.Random);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}

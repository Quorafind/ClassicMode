using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

public sealed class Panacea_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ArtifactPower>(1m)];

    public Panacea_C() : base("panacea", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<ArtifactPower>(Owner.Creature, DynamicVars["ArtifactPower"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["ArtifactPower"].UpgradeValueBy(1m);
}

public sealed class SwiftStrike_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7m, ValueProp.Move)];

    public SwiftStrike_C() : base("swift_strike", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class GoodInstincts_C : ClassicColorlessCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    public GoodInstincts_C() : base("good_instincts", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class Purity_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3m)];

    public Purity_C() : base("purity", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0)
            return;

        int max = DynamicVars["Cards"].IntValue;
        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, max),
            null,
            this);

        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(2m);
}

public sealed class BandageUp_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Heal", 4m)];

    public BandageUp_C() : base("bandage_up", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].IntValue);

    protected override void OnUpgrade() => DynamicVars["Heal"].UpgradeValueBy(2m);
}

public sealed class Discovery_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    public Discovery_C() : base("discovery", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var options = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();

        if (options.Count == 0)
            return;

        var chosen = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            options,
            Owner,
            canSkip: true);

        if (chosen == null)
            return;

        chosen.EnergyCost.SetThisTurnOrUntilPlayed(0);
        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, addedByPlayer: true);
    }

    protected override void OnUpgrade()
    {
    }
}

public sealed class Finesse_C : ClassicColorlessCard
{
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];

    public Finesse_C() : base("finesse", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}

public sealed class PanicButton_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(30m, ValueProp.Move)];

    public PanicButton_C() : base("panic_button", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<NoBlockPower>(Owner.Creature, 2, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(10m);
}

public sealed class Enlightenment_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Enlightenment_C() : base("enlightenment", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var c in PileType.Hand.GetPile(Owner).Cards)
        {
            if (c.EnergyCost.GetResolved() <= 1)
                continue;

            if (IsUpgraded)
                c.EnergyCost.SetThisCombat(1);
            else
                c.EnergyCost.SetThisTurnOrUntilPlayed(1);
        }

        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
    }
}

public sealed class MindBlast_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate];

    public MindBlast_C() : base("mind_blast", 2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        decimal dmg = PileType.Draw.GetPile(Owner).Cards.Count;
        await DamageCmd.Attack(dmg).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Impatience_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 2m)];

    public Impatience_C() : base("impatience", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (PileType.Hand.GetPile(Owner).Cards.Any(c => c.Type == CardType.Attack))
            return;

        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class DeepBreath_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 1m)];

    public DeepBreath_C() : base("deep_breath", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Shuffle(choiceContext, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class Madness_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Madness_C() : base("madness", 1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
        if (hand.Count == 0)
            return;

        var selected = Owner.RunState.Rng.CombatCardSelection.NextItem(hand);
        selected?.SetToFreeThisCombat();
        await Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Trip_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Vulnerable", 2m)];

    public Trip_C() : base("trip", 0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<VulnerablePower>(enemy, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
            return;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DynamicVars["Vulnerable"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }
}

public sealed class Blind_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Weak", 2m)];

    public Blind_C() : base("blind", 0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (IsUpgraded)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<WeakPower>(enemy, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
            return;
        }

        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<WeakPower>(cardPlay.Target, DynamicVars["Weak"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() { }
}

public sealed class JackOfAllTrades_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 1m)];

    public JackOfAllTrades_C() : base("jack_of_all_trades", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars["Cards"].IntValue;
        var pool = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Id != Id)
            .ToList();
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            var card = rng.NextItem(pool);
            if (card == null) continue;
            await CardPileCmd.AddGeneratedCardToCombat(card.CreateClone(), PileType.Hand, addedByPlayer: true);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class FlashOfSteel_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3m, ValueProp.Move)];

    public FlashOfSteel_C() : base("flash_of_steel", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}

public sealed class DramaticEntrance_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    public DramaticEntrance_C() : base("dramatic_entrance", 0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}

public sealed class Forethought_C : ClassicColorlessCard
{
    public Forethought_C() : base("forethought", 0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0)
            return;

        if (IsUpgraded)
        {
            var selectedAll = await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, hand.Count),
                null,
                this);

            foreach (var c in selectedAll)
            {
                c.SetToFreeThisCombat();
                await CardPileCmd.Add(c, PileType.Draw, CardPilePosition.Bottom);
            }
            return;
        }

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();

        if (selected == null)
            return;

        selected.SetToFreeThisCombat();
        await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade() { }
}

public sealed class DarkShackles_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("StrengthLoss", 9m)];

    public DarkShackles_C() : base("dark_shackles", 0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await PowerCmd.Apply<DarkShacklesPower>(cardPlay.Target, DynamicVars["StrengthLoss"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthLoss"].UpgradeValueBy(6m);
}

public sealed class Mayhem_C : ClassicColorlessCard
{
    public Mayhem_C() : base("mayhem", 2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<MayhemPower>(Owner.Creature, 1, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class MasterOfStrategy_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3m)];

    public MasterOfStrategy_C() : base("master_of_strategy", 0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class Violence_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3m)];

    public Violence_C() : base("violence", 0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars["Cards"].IntValue;
        var attacks = PileType.Draw.GetPile(Owner).Cards.Where(c => c.Type == CardType.Attack).ToList();
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        for (int i = 0; i < count && attacks.Count > 0; i++)
        {
            var chosen = rng.NextItem(attacks);
            if (chosen == null) break;
            attacks.Remove(chosen);
            await CardPileCmd.Add(chosen, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(1m);
}

public sealed class SadisticNature_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 5m)];

    public SadisticNature_C() : base("sadistic_nature", 0, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<SadisticNaturePower_C>(Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(2m);
}

public sealed class ThinkingAhead_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    public ThinkingAhead_C() : base("thinking_ahead", 0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 2, Owner);

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1),
            null,
            this)).FirstOrDefault();

        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Draw, CardPilePosition.Top);
    }

    protected override void OnUpgrade() { }
}

public sealed class TheBomb_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(40m, ValueProp.Move)];

    public TheBomb_C() : base("the_bomb", 2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<TheBombPower>(Owner.Creature, 3, Owner.Creature, this);
        power?.SetDamage(DynamicVars.Damage.BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10m);
}

public sealed class Magnetism_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1m)];

    public Magnetism_C() : base("magnetism", 2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<MagnetismPower_C>(Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Apotheosis_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Apotheosis_C() : base("apotheosis", 2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var c in Owner.PlayerCombatState.AllCards.Where(c => c != this && c.IsUpgradable))
            CardCmd.Upgrade(c);
        await Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Panache_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 10m)];

    public Panache_C() : base("panache", 0, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<PanachePower>(Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, this);

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(4m);
}

public sealed class SecretTechnique_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    public SecretTechnique_C() : base("secret_technique", 0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pile = PileType.Draw.GetPile(Owner);
        var options = pile.Cards.Where(c => c.Type == CardType.Skill).ToList();
        if (options.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();

        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Hand);
    }

    protected override void OnUpgrade() { }
}

public sealed class SecretWeapon_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded ? [] : [CardKeyword.Exhaust];

    public SecretWeapon_C() : base("secret_weapon", 0, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pile = PileType.Draw.GetPile(Owner);
        var options = pile.Cards.Where(c => c.Type == CardType.Attack).ToList();
        if (options.Count == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            options,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();

        if (selected != null)
            await CardPileCmd.Add(selected, PileType.Hand);
    }

    protected override void OnUpgrade() { }
}

public sealed class Chrysalis_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3m)];

    public Chrysalis_C() : base("chrysalis", 2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars["Cards"].IntValue;
        var generated = CardFactory.GetForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Type == CardType.Skill),
            count,
            Owner.RunState.Rng.CombatCardGeneration);

        foreach (var card in generated)
        {
            card.SetToFreeThisCombat();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
        }
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(2m);
}

public sealed class Metamorphosis_C : ClassicColorlessCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Cards", 3m)];

    public Metamorphosis_C() : base("metamorphosis", 2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars["Cards"].IntValue;
        var generated = CardFactory.GetForCombat(
            Owner,
            Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Type == CardType.Attack),
            count,
            Owner.RunState.Rng.CombatCardGeneration);

        foreach (var card in generated)
        {
            card.SetToFreeThisCombat();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
        }
    }

    protected override void OnUpgrade() => DynamicVars["Cards"].UpgradeValueBy(2m);
}

public sealed class HandOfGreed_C : ClassicColorlessCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(20m, ValueProp.Move),
        new DynamicVar("Gold", 20m)
    ];

    public HandOfGreed_C() : base("hand_of_greed", 2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_dramatic_stab")
            .Execute(choiceContext);

        if (!cardPlay.Target.IsAlive)
            await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
        DynamicVars["Gold"].UpgradeValueBy(5m);
    }
}

public sealed class Transmutation_C : ClassicColorlessCard
{
    protected override bool HasEnergyCostX => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public Transmutation_C() : base("transmutation", -1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = ResolveEnergyXValue();
        if (x <= 0)
            return;

        var cards = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity != CardRarity.Basic)
            .ToList();

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        for (int i = 0; i < x; i++)
        {
            var chosen = rng.NextItem(cards);
            if (chosen == null) continue;

            var generated = chosen.CreateClone();
            if (IsUpgraded)
                CardCmd.Upgrade(generated);
            generated.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, addedByPlayer: true);
        }
    }
}

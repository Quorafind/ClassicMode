using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

public sealed class BustedCrownRelic : ClassicRelic
{
    public BustedCrownRelic() : base("bustedCrown") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new DynamicVar("CardChoices", 1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1m : amount;
    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (player != Owner || creationOptions.Source != CardCreationSource.Encounter || options.Count <= 1) return false;
        while (options.Count > DynamicVars["CardChoices"].IntValue)
            options.RemoveAt(options.Count - 1);
        return true;
    }
}

public sealed class CoffeeDripperRelic : ClassicRelic
{
    public CoffeeDripperRelic() : base("coffeeDripper") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1m : amount;
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;

        var removed = false;
        removed |= options.Remove(options.FirstOrDefault(o => o is HealRestSiteOption));
        removed |= options.Remove(options.FirstOrDefault(o => o is MendRestSiteOption));
        return removed;
    }
}

public sealed class CursedKeyRelic : ClassicRelic
{
    public CursedKeyRelic() : base("cursedKey") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1m : amount;
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room.RoomType != RoomType.Treasure || Owner == null) return;
        var curses = ModelDb.CardPool<CurseCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.CanBeGeneratedByModifiers).ToList();
        if (curses.Count == 0) return;
        var curse = Owner.RunState.CreateCard(Owner.RunState.Rng.Niche.NextItem(curses), Owner);
        Flash();
        var addResult = await CardPileCmd.Add(curse, PileType.Deck);
        CardCmd.PreviewCardPileAdd(addResult, 1.2f);
    }
}

public sealed class FusionHammerRelic : ClassicRelic
{
    public FusionHammerRelic() : base("fusionHammer") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1m : amount;
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        var smith = options.FirstOrDefault(o => o.GetType().Name.Contains("Smith", System.StringComparison.OrdinalIgnoreCase));
        if (smith != null) options.Remove(smith);
        return smith != null;
    }
}

public sealed class RunicCubeRelic : ClassicRelic
{
    public RunicCubeRelic() : base("runicCube") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!CombatManager.Instance.IsInProgress || target != Owner.Creature || result.UnblockedDamage <= 0) return;
        Flash();
        for (int i = 0; i < DynamicVars.Cards.IntValue; i++)
            await CardPileCmd.Draw(choiceContext, Owner);
    }
}

public sealed class RunicDomeRelic : ClassicRelic
{
    public RunicDomeRelic() : base("runicDome") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount) => player == Owner ? amount + 1m : amount;
}

public sealed class SacredBarkRelic : ClassicRelic
{
    private bool _isApplyingPotionEffects;
    private bool _isDuplicatingGeneratedCard;
    private bool _isCardChoicePotion;

    public SacredBarkRelic() : base("sacredBark") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;

    private bool IsApplyingPotionEffects
    {
        get => _isApplyingPotionEffects;
        set
        {
            AssertMutable();
            _isApplyingPotionEffects = value;
        }
    }

    private bool IsDuplicatingGeneratedCard
    {
        get => _isDuplicatingGeneratedCard;
        set
        {
            AssertMutable();
            _isDuplicatingGeneratedCard = value;
        }
    }

    private bool IsCardChoicePotion
    {
        get => _isCardChoicePotion;
        set
        {
            AssertMutable();
            _isCardChoicePotion = value;
        }
    }

    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner)
        {
            IsApplyingPotionEffects = true;
            IsCardChoicePotion = potion is AttackPotion or SkillPotion or PowerPotion or ColorlessPotion;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner == Owner)
        {
            IsApplyingPotionEffects = false;
            IsCardChoicePotion = false;
        }
        return Task.CompletedTask;
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (!IsApplyingPotionEffects) return amount;
        if (player != Owner) return amount;
        if (amount <= 0m) return amount;
        return amount * 2m;
    }

    public override decimal ModifyPowerAmountGiven(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!IsApplyingPotionEffects) return amount;
        if (giver != Owner.Creature) return amount;
        if (amount <= 0m) return amount;
        return amount * 2m;
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (!IsApplyingPotionEffects || !IsCardChoicePotion) return;
        if (creator != Owner) return;
        if (card.Owner != Owner || card.Pile?.Type != PileType.Hand) return;
        if (IsDuplicatingGeneratedCard) return;

        IsDuplicatingGeneratedCard = true;
        try
        {
            var dupe = card.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(dupe, PileType.Hand, Owner);
        }
        finally
        {
            IsDuplicatingGeneratedCard = false;
        }
    }
}

public sealed class SlaversCollarRelic : ClassicRelic
{
    public SlaversCollarRelic() : base("slaversCollar") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner) return amount;
        var room = player.RunState.CurrentRoom;
        if (room?.RoomType is RoomType.Elite or RoomType.Boss) return amount + 1m;
        return amount;
    }
}

public sealed class TinyHouseRelic : ClassicRelic
{
    public TinyHouseRelic() : base("tinyHouse") { }
    public override RelicRarity Rarity => RelicRarity.Ancient;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(5m), new DynamicVar("Gold", 50m)];
    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.BaseValue);
        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
        var potion = PotionFactory.CreateRandomPotionOutOfCombat(Owner, Owner.RunState.Rng.CombatPotionGeneration).ToMutable();
        await PotionCmd.TryToProcure(potion, Owner);
        var card = CardFactory.CreateForReward(Owner, 1, CardCreationOptions.ForRoom(Owner, RoomType.Monster)).FirstOrDefault()?.Card;
        if (card != null)
            await CardPileCmd.Add(card, PileType.Deck);
        var upgradable = Owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        if (upgradable.Count > 0)
            CardCmd.Upgrade(Owner.RunState.Rng.Niche.NextItem(upgradable));
    }
}

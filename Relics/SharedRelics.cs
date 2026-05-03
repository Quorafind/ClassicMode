using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

public sealed class ClassicBronzeScales : ClassicRelic
{
    public ClassicBronzeScales() : base("bronzeScales") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<ThornsPower>(3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ThornsPower>()];

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            Flash();
            await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["ThornsPower"].BaseValue, Owner.Creature, null);
        }
    }
}

public sealed class OmamoriRelic : ClassicRelic
{
    private int _remainingNegates;

    public OmamoriRelic() : base("omamori") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShowCounter => true;
    public override int DisplayAmount => RemainingNegates;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Negates", 2m)];

    [SavedProperty]
    public int RemainingNegates
    {
        get => _remainingNegates;
        set
        {
            AssertMutable();
            _remainingNegates = value;
            decimal negates = Math.Max(0, value);
            DynamicVars["Negates"].BaseValue = negates;
            if (CanonicalInstance != null && !ReferenceEquals(CanonicalInstance, this))
                CanonicalInstance.DynamicVars["Negates"].BaseValue = negates;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterObtained()
    {
        if (RemainingNegates <= 0)
            RemainingNegates = 2;
        return Task.CompletedTask;
    }

    public override bool ShouldAddToDeck(CardModel card)
    {
        if (card.Owner != Owner || card.Type != CardType.Curse || RemainingNegates <= 0)
            return true;

        RemainingNegates--;
        Flash();
        return false;
    }
}

public sealed class SmilingMaskRelic : ClassicRelic
{
    public SmilingMaskRelic() : base("merchantMask") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != Owner)
            return originalPrice;
        if (entry is not MerchantCardRemovalEntry)
            return originalPrice;
        return 50m;
    }
}

public sealed class TinyChestRelic : ClassicRelic
{
    private int _eventRoomsSeen;

    public TinyChestRelic() : base("tinyChest") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShowCounter => true;
    public override int DisplayAmount => (EventRoomsSeen % 4) + 1;

    [SavedProperty]
    public int EventRoomsSeen
    {
        get => _eventRoomsSeen;
        set
        {
            AssertMutable();
            _eventRoomsSeen = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        bool eventRoom = room.RoomType == RoomType.Event;
        bool forcedTreasure = room.RoomType == RoomType.Treasure && ((EventRoomsSeen + 1) % 4) == 0;
        if (eventRoom || forcedTreasure)
            EventRoomsSeen++;
        return Task.CompletedTask;
    }

    public override IReadOnlySet<RoomType> ModifyUnknownMapPointRoomTypes(IReadOnlySet<RoomType> roomTypes)
    {
        if (((EventRoomsSeen + 1) % 4) != 0)
            return roomTypes;

        return new HashSet<RoomType> { RoomType.Treasure };
    }
}

public sealed class ClassicNunchaku : ClassicRelic
{
    private bool _isActivating;
    private int _attacksPlayed;

    public ClassicNunchaku() : base("nunchaku") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShowCounter => true;

    public override int DisplayAmount
    {
        get
        {
            if (IsActivating)
                return DynamicVars.Cards.IntValue;
            return AttacksPlayed % DynamicVars.Cards.IntValue;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(10),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this)];

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            AssertMutable();
            _isActivating = value;
            UpdateDisplay();
        }
    }

    [SavedProperty]
    public int AttacksPlayed
    {
        get => _attacksPlayed;
        set
        {
            AssertMutable();
            _attacksPlayed = value;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        int threshold = DynamicVars.Cards.IntValue;
        Status = IsActivating
            ? RelicStatus.Normal
            : (AttacksPlayed % threshold == threshold - 1 ? RelicStatus.Active : RelicStatus.Normal);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return;

        AttacksPlayed++;
        int threshold = DynamicVars.Cards.IntValue;
        if (CombatManager.Instance.IsInProgress && AttacksPlayed % threshold == 0)
        {
            _ = TaskHelper.RunSafely(DoActivateVisuals());
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        }
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }
}

public sealed class PreservedInsectRelic : ClassicRelic
{
    private bool _appliedInCombat;

    public PreservedInsectRelic() : base("insect") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("HpReductionPercent", 25m)];

    public override async Task BeforeCombatStart()
    {
        if (_appliedInCombat)
            return;

        if (Owner.RunState.CurrentRoom is not CombatRoom combatRoom || combatRoom.RoomType != RoomType.Elite)
            return;

        _appliedInCombat = true;
        Flash();

        foreach (Creature enemy in combatRoom.CombatState.Enemies.Where(e => e.IsAlive))
        {
            decimal loseAmount = Math.Ceiling(enemy.MaxHp * (DynamicVars["HpReductionPercent"].BaseValue / 100m));
            if (loseAmount > 0)
                await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), enemy, loseAmount, isFromCard: false);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _appliedInCombat = false;
        return Task.CompletedTask;
    }
}

public sealed class CeramicFishRelic : ClassicRelic
{
    public CeramicFishRelic() : base("ceramic_fish") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Gold", 9m)];

    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != Owner || reward is not CardReward)
            return;

        Flash();
        await PlayerCmd.GainGold(DynamicVars["Gold"].BaseValue, Owner);
    }
}

public sealed class ToyOrnithopterRelic : ClassicRelic
{
    public ToyOrnithopterRelic() : base("ornithopter") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Heal", 5m)];

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue, playAnim: true);
    }
}

public sealed class BlueCandleRelic : ClassicRelic
{
    public BlueCandleRelic() : base("blueCandle") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner == Owner && card.Type == CardType.Curse)
            return true;
        return base.ShouldPlay(card, autoPlayType);
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        TryEnableBlueCandleCard(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        TryEnableBlueCandleCard(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        TryEnableBlueCandleCard(card);
        return Task.CompletedTask;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (card.Owner == Owner && card.Type == CardType.Curse)
            return (PileType.Exhaust, position);
        return (pileType, position);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Curse)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, Owner.Creature, 1m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    private void TryEnableBlueCandleCard(CardModel card)
    {
        if (card.Owner != Owner || card.Type != CardType.Curse)
            return;
        if (card.Pile?.Type != PileType.Hand)
            return;
        if (card.Keywords.Contains(CardKeyword.Unplayable))
            card.RemoveKeyword(CardKeyword.Unplayable);
    }
}

public abstract class ClassicBottleRelic : ClassicRelic
{
    private readonly CardType _bottleType;

    protected ClassicBottleRelic(string assetName, CardType bottleType) : base(assetName)
    {
        _bottleType = bottleType;
    }

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new StringVar("Enchantment", ModelDb.Enchantment<BottledEnchantment>().Title.GetFormattedText())];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<BottledEnchantment>();

    public override bool IsAllowed(IRunState runState)
    {
        if (!base.IsAllowed(runState))
            return false;

        EnchantmentModel bottled = ModelDb.Enchantment<BottledEnchantment>();
        return runState.Players.Any(p => p.Deck.Cards.Any(c => CanBottle(c, bottled)));
    }

    public override async Task AfterObtained()
    {
        EnchantmentModel bottled = ModelDb.Enchantment<BottledEnchantment>();
        List<CardModel> candidates = Owner.Deck.Cards
            .Where(c => CanBottle(c, bottled))
            .ToList();
        if (candidates.Count == 0)
            return;

        CardModel? selected = (await CardSelectCmd.FromDeckForEnchantment(
            cards: candidates.UnstableShuffle(Owner.RunState.Rng.Niche).ToList(),
            enchantment: bottled,
            amount: 1,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1)))
            .FirstOrDefault();
        if (selected == null)
            return;

        Flash();
        CardCmd.Enchant<BottledEnchantment>(selected, 1m);

        NCardEnchantVfx? vfx = NCardEnchantVfx.Create(selected);
        if (vfx != null)
            NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
    }

    private bool CanBottle(CardModel card, EnchantmentModel bottled)
    {
        return card.Type == _bottleType && bottled.CanEnchant(card);
    }
}

public sealed class BottledFlameRelic : ClassicBottleRelic
{
    public BottledFlameRelic() : base("bottledFlame", CardType.Attack) { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class BottledLightningRelic : ClassicBottleRelic
{
    public BottledLightningRelic() : base("bottledLightning", CardType.Skill) { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class BottledTornadoRelic : ClassicBottleRelic
{
    public BottledTornadoRelic() : base("bottledTornado", CardType.Power) { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class MatryoshkaRelic : ClassicRelic
{
    private int _remainingChests;

    public MatryoshkaRelic() : base("matryoshka") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;
    public override int DisplayAmount => RemainingChests;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ChestsLeft", 2m)];

    [SavedProperty]
    public int RemainingChests
    {
        get => _remainingChests;
        set
        {
            AssertMutable();
            _remainingChests = value;
            decimal chestsLeft = Math.Max(0, value);
            DynamicVars["ChestsLeft"].BaseValue = chestsLeft;
            if (CanonicalInstance != null && !ReferenceEquals(CanonicalInstance, this))
                CanonicalInstance.DynamicVars["ChestsLeft"].BaseValue = chestsLeft;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterObtained()
    {
        if (RemainingChests <= 0)
            RemainingChests = 2;
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || RemainingChests <= 0)
            return false;
        if (room is not TreasureRoom)
            return false;

        rewards.Add(new RelicReward(player));
        RemainingChests--;
        Flash();
        return true;
    }
}

public sealed class QuestionCardRelic : ClassicRelic
{
    public QuestionCardRelic() : base("questionCard") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> options, CardCreationOptions creationOptions)
    {
        if (player != Owner)
            return false;
        if (!creationOptions.Flags.HasFlag(CardCreationFlags.IsCardReward))
            return false;

        HashSet<ModelId> existingIds = options.Select(o => o.originalCard.Id).ToHashSet();
        IEnumerable<CardModel> candidates = creationOptions.GetPossibleCards(player)
            .Where(c => !existingIds.Contains(c.Id));

        CardCreationOptions extraOptionSettings = new CardCreationOptions(
            candidates,
            CardCreationSource.Other,
            creationOptions.RarityOdds)
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications | CardCreationFlags.NoUpgradeRoll);

        CardCreationResult? extra = CardFactory.CreateForReward(player, 1, extraOptionSettings).FirstOrDefault();
        if (extra == null)
            return false;

        options.Add(extra);
        Flash();
        return true;
    }
}

public sealed class SingingBowlRelic : ClassicRelic
{
    public const string GainMaxHpAlternativeKey = "GAIN_MAX_HP";

    public SingingBowlRelic() : base("singingBowl") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (player != Owner)
            return false;

        alternatives.Add(new CardRewardAlternative(
            GainMaxHpAlternativeKey,
            async () =>
            {
                Flash();
                await CreatureCmd.GainMaxHp(player.Creature, 2m);
            },
            PostAlternateCardRewardAction.DismissScreenAndRemoveReward));

        return true;
    }
}

public sealed class SundialRelic : ClassicRelic
{
    private bool _isActivating;
    private int _shuffleCount;

    public SundialRelic() : base("sundial") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;
    public override int DisplayAmount => IsActivating ? DynamicVars["Shuffles"].IntValue : (ShuffleCount % DynamicVars["Shuffles"].IntValue);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Shuffles", 3m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this)];

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            AssertMutable();
            _isActivating = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int ShuffleCount
    {
        get => _shuffleCount;
        set
        {
            AssertMutable();
            _shuffleCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != Owner)
            return;

        ShuffleCount++;
        int threshold = DynamicVars["Shuffles"].IntValue;
        if (ShuffleCount % threshold != 0)
            return;

        _ = TaskHelper.RunSafely(DoActivateVisuals());
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }
}

public sealed class InkBottleRelic : ClassicRelic
{
    private bool _isActivating;
    private int _cardsPlayed;

    public InkBottleRelic() : base("inkBottle") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;
    public override int DisplayAmount => IsActivating ? DynamicVars["Cards"].IntValue : (CardsPlayed % DynamicVars["Cards"].IntValue);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(10),
        new DynamicVar("Draw", 1m)
    ];

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            AssertMutable();
            _isActivating = value;
            InvokeDisplayAmountChanged();
        }
    }

    [SavedProperty]
    public int CardsPlayed
    {
        get => _cardsPlayed;
        set
        {
            AssertMutable();
            _cardsPlayed = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;

        CardsPlayed++;
        int threshold = DynamicVars["Cards"].IntValue;
        if (CardsPlayed % threshold != 0)
            return;

        _ = TaskHelper.RunSafely(DoActivateVisuals());
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }
}

public sealed class IncenseBurnerRelic : ClassicRelic
{
    private int _turnsElapsed;

    public IncenseBurnerRelic() : base("incenseBurner") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;
    public override int DisplayAmount => DynamicVars["Turns"].IntValue - (TurnsElapsed % DynamicVars["Turns"].IntValue);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Turns", 6m),
        new DynamicVar("Amount", 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<IntangiblePower>()];

    [SavedProperty]
    public int TurnsElapsed
    {
        get => _turnsElapsed;
        set
        {
            AssertMutable();
            _turnsElapsed = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side && combatState.RoundNumber <= 1)
            TurnsElapsed = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side)
            return;

        TurnsElapsed++;
        if (TurnsElapsed % DynamicVars["Turns"].IntValue != 0)
            return;

        Flash();
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, null);
    }
}

public sealed class FossilizedHelixRelic : ClassicRelic
{
    public FossilizedHelixRelic() : base("helix") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Amount", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<BufferPower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<BufferPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Amount"].BaseValue, Owner.Creature, null);
    }
}

public sealed class BirdFacedUrnRelic : ClassicRelic
{
    public BirdFacedUrnRelic() : base("bird_urn") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Heal", 2m)];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Power)
            return;

        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["Heal"].BaseValue, playAnim: true);
    }
}

public sealed class CalipersRelic : ClassicRelic
{
    public CalipersRelic() : base("calipers") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("BlockLoss", 15m)];

    public override bool ShouldClearBlock(Creature creature)
    {
        if (creature == Owner.Creature)
            return false;
        return base.ShouldClearBlock(creature);
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side)
            return;

        int block = Owner.Creature.Block;
        if (block <= 0)
            return;

        decimal loss = Math.Min(block, DynamicVars["BlockLoss"].BaseValue);
        if (loss > 0)
            await CreatureCmd.LoseBlock(Owner.Creature, loss);
    }
}

public sealed class DuVuDollRelic : ClassicRelic
{
    public DuVuDollRelic() : base("duvuDoll") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task BeforeCombatStart()
    {
        int curses = Owner.Deck.Cards.Count(c => c.Type == CardType.Curse);
        if (curses <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, curses, Owner.Creature, null);
    }
}

public sealed class DeadBranchRelic : ClassicRelic
{
    private int _pendingEtherealTriggers;

    public DeadBranchRelic() : base("deadBranch") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner)
            return;

        if (causedByEthereal)
        {
            _pendingEtherealTriggers++;
            return;
        }

        await AddRandomCardToHand();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side || _pendingEtherealTriggers <= 0)
            return;

        int count = _pendingEtherealTriggers;
        _pendingEtherealTriggers = 0;
        for (int i = 0; i < count; i++)
            await AddRandomCardToHand();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _pendingEtherealTriggers = 0;
        return Task.CompletedTask;
    }

    private async Task AddRandomCardToHand()
    {
        CardCreationOptions options = CardCreationOptions.ForNonCombatWithDefaultOdds([Owner.Character.CardPool])
            .WithFlags(CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications | CardCreationFlags.NoUpgradeRoll);
        CardCreationResult? generated = CardFactory.CreateForReward(Owner, 1, options).FirstOrDefault();
        if (generated == null)
            return;

        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(generated.Card, PileType.Hand, Owner);
    }
}

public sealed class GingerRelic : ClassicRelic
{
    public GingerRelic() : base("ginger") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (target == Owner.Creature && amount > 0 && canonicalPower is WeakPower)
        {
            modifiedAmount = 0m;
            Flash();
            return true;
        }

        modifiedAmount = amount;
        return false;
    }
}

public sealed class MagicFlowerRelic : ClassicRelic
{
    public MagicFlowerRelic() : base("magicFlower") { }

    public override RelicRarity Rarity => RelicRarity.Rare;
}

public sealed class PeacePipeRelic : ClassicRelic
{
    public PeacePipeRelic() : base("peacePipe") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    // STS2 has no built-in rest-site remove option model; keep as passive marker relic for now.
}

public sealed class TurnipRelic : ClassicRelic
{
    public TurnipRelic() : base("turnip") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<FrailPower>()];

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
    {
        if (target == Owner.Creature && amount > 0 && canonicalPower is FrailPower)
        {
            modifiedAmount = 0m;
            Flash();
            return true;
        }

        modifiedAmount = amount;
        return false;
    }
}

public sealed class ThreadAndNeedleRelic : ClassicRelic
{
    public ThreadAndNeedleRelic() : base("threadAndNeedle") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("PlatedArmor", 4m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ClassicPlatedArmorPower_C>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<ClassicPlatedArmorPower_C>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["PlatedArmor"].BaseValue,
            Owner.Creature,
            null);
    }
}

public sealed class ToriiRelic : ClassicRelic
{
    public ToriiRelic() : base("torii") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature)
            return amount;
        if (!props.HasFlag(ValueProp.Move))
            return amount;
        if (amount <= 0 || amount > 5m)
            return amount;

        Flash();
        return 1m;
    }
}

public sealed class FrozenEyeRelic : ClassicRelic
{
    public FrozenEyeRelic() : base("frozenEye") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    // Draw-pile ordering display is UI-side; STS2 already exposes draw pile order in pile view.
}

public sealed class MedicalKitRelic : ClassicRelic
{
    public MedicalKitRelic() : base("medicalKit") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner == Owner && card.Type == CardType.Status)
            return true;
        return base.ShouldPlay(card, autoPlayType);
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        TryEnableMedicalKitCard(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        TryEnableMedicalKitCard(card);
        return Task.CompletedTask;
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        TryEnableMedicalKitCard(card);
        return Task.CompletedTask;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (card.Owner == Owner && card.Type == CardType.Status)
            return (PileType.Exhaust, position);
        return (pileType, position);
    }

    private void TryEnableMedicalKitCard(CardModel card)
    {
        if (card.Owner != Owner || card.Type != CardType.Status)
            return;
        if (card.Pile?.Type != PileType.Hand)
            return;
        if (card.Keywords.Contains(CardKeyword.Unplayable))
            card.RemoveKeyword(CardKeyword.Unplayable);
    }
}

public sealed class StrangeSpoonRelic : ClassicRelic
{
    public StrangeSpoonRelic() : base("bigSpoon") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Chance", 50m)];

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (card.Owner != Owner || pileType != PileType.Exhaust)
            return (pileType, position);

        int roll = Owner.RunState.Rng.CombatTargets.NextInt(100);
        if (roll >= DynamicVars["Chance"].IntValue)
            return (pileType, position);

        Flash();
        return (PileType.Discard, position);
    }
}

public sealed class ClockworkSouvenirRelic : ClassicRelic
{
    public ClockworkSouvenirRelic() : base("clockwork") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Artifact", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ArtifactPower>()];

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Artifact"].BaseValue, Owner.Creature, null);
    }
}

public sealed class OrangePelletsRelic : ClassicRelic
{
    private bool _playedAttack;
    private bool _playedSkill;
    private bool _playedPower;

    public OrangePelletsRelic() : base("bluePill") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side)
            return Task.CompletedTask;

        _playedAttack = false;
        _playedSkill = false;
        _playedPower = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;

        if (cardPlay.Card.Type == CardType.Attack) _playedAttack = true;
        if (cardPlay.Card.Type == CardType.Skill) _playedSkill = true;
        if (cardPlay.Card.Type == CardType.Power) _playedPower = true;

        if (!(_playedAttack && _playedSkill && _playedPower))
            return;

        List<PowerModel> debuffs = Owner.Creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
        if (debuffs.Count == 0)
            return;

        Flash();
        foreach (PowerModel debuff in debuffs)
            await PowerCmd.Remove(debuff);

        _playedAttack = false;
        _playedSkill = false;
        _playedPower = false;
    }
}

public sealed class PrismaticShardRelic : ClassicRelic
{
    public PrismaticShardRelic() : base("prism") { }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (Owner != player)
            return options;
        if (options.Flags.HasFlag(CardCreationFlags.NoCardPoolModifications))
            return options;
        if (!options.Flags.HasFlag(CardCreationFlags.IsCardReward))
            return options;
        if (options.CustomCardPool != null)
            return options;

        IEnumerable<CardPoolModel> pools = player.UnlockState.CharacterCardPools
            .Union(options.CardPools)
            .Append(ModelDb.CardPool<ColorlessCardPool>());

        return options.WithCardPools(pools.Distinct(), options.CardPoolFilter);
    }
}

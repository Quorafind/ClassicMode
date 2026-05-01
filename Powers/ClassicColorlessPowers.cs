using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

public sealed class SadisticNaturePower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = choiceContext;
        _ = cardSource;
        if (applier != Owner)
            return;
        if (amount <= 0m)
            return;
        if (power.Type != PowerType.Debuff)
            return;
        if (power.Owner.Side == Owner.Side)
            return;

        Flash();
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), power.Owner, Amount, ValueProp.Unpowered, Owner, null);
    }
}

public sealed class MagnetismPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        _ = combatState;
        if (side != Owner.Side)
            return;
        var player = Owner.Player;
        if (player == null)
            return;

        var card = CardFactory.GetDistinctForCombat(
            player,
            from c in ModelDb.CardPool<ColorlessCardPool>()
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            where c.Rarity != CardRarity.Basic
            select c,
            1,
            player.RunState.Rng.CombatCardGeneration).FirstOrDefault();

        if (card == null)
            return;

        Flash();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner.Player);
    }
}

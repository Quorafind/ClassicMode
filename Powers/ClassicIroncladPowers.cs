using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

/// <summary>
/// At end of turn, lose 1 HP and deal Amount damage to ALL enemies.
/// STS1: Combust (5 base / 7 upgraded damage).
/// </summary>
public sealed class CombustPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Side)
            return;

        Flash();
        // Lose 1 HP (unblockable self-damage)
        await CreatureCmd.Damage(choiceContext, base.Owner, 1m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        // Deal Amount damage to all enemies
        await CreatureCmd.Damage(choiceContext, base.CombatState.HittableEnemies, base.Amount, ValueProp.Unpowered, base.Owner, null);
    }
}

/// <summary>
/// Whenever you draw a Status card, draw Amount additional cards.
/// STS1: Evolve (1 base / 2 upgraded draw).
/// </summary>
public sealed class EvolvePower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature == base.Owner && card.Type == CardType.Status)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
        }
    }
}

/// <summary>
/// Whenever you draw a Status or Curse card, deal Amount damage to ALL enemies.
/// STS1: Fire Breathing (6 base / 10 upgraded damage).
/// </summary>
public sealed class FireBreathingPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature == base.Owner && (card.Type == CardType.Status || card.Type == CardType.Curse))
        {
            Flash();
            await CreatureCmd.Damage(choiceContext, base.CombatState.HittableEnemies, base.Amount, ValueProp.Unpowered, base.Owner, null);
        }
    }
}

/// <summary>
/// At end of turn, gain Amount Block.
/// STS1: Metallicize (3 base / 4 upgraded block).
/// </summary>
public sealed class MetallicizePower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            Flash();
            await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
        }
    }
}

/// <summary>
/// At the start of each turn, gain Amount energy.
/// STS1: Berserk (1 energy per turn).
/// </summary>
public sealed class BerserkPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == base.Owner.Player)
        {
            Flash();
            await PlayerCmd.GainEnergy(base.Amount, player);
        }
    }
}

/// <summary>
/// At the start of each turn, lose 1 HP and draw Amount cards.
/// STS1: Brutality (1 draw per turn).
/// </summary>
public sealed class BrutalityPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        Flash();
        // STS1 stacking behavior: both HP loss and card draw scale with Amount.
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner, base.Amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        // Draw Amount cards
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), base.Amount, base.Owner.Player);
    }
}

/// <summary>
/// STS1 Rupture: Whenever you lose HP from one of your own cards, gain Amount Strength.
/// </summary>
public sealed class RupturePower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
            return;
        if (result.UnblockedDamage <= 0)
            return;

        // STS1 behavior: only self HP loss originating from the owner's cards counts.
        if (cardSource?.Owner?.Creature != base.Owner)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner, base.Amount, base.Owner, null);
    }
}

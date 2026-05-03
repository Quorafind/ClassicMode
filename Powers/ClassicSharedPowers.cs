using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

/// <summary>
/// STS1-style Plated Armor:
/// - End of owner's turn: gain Block equal to Amount.
/// - After taking unblocked attack damage: lose 1 stack.
/// </summary>
public sealed class ClassicPlatedArmorPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side || Amount <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0)
            return;
        if (result.UnblockedDamage <= 0)
            return;
        if (!props.HasFlag(ValueProp.Move))
            return;

        Flash();
        await PowerCmd.Decrement(this);
    }
}

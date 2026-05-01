using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// STS1 Silent-specific powers (classic implementations)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// Whenever you play a card, deal Amount damage to ALL enemies.
/// STS1: A Thousand Cuts (1 base / 2 upgraded).
/// </summary>
public sealed class AThousandCutsPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner)
        {
            Flash();
            await CreatureCmd.Damage(context, base.CombatState.HittableEnemies, base.Amount, ValueProp.Unpowered, base.Owner, null);
        }
    }
}

/// <summary>
/// Whenever you deal unblocked attack damage, apply Amount of Choke damage.
/// The enemy takes Amount damage whenever they play a card this turn.
/// STS1: Choke (3 base / 5 upgraded).
/// NOTE: In STS1, Choke wasn't a power - it was applied as damage per card played.
/// We implement it as a debuff on the enemy that triggers when they act.
/// </summary>
public sealed class ChokeHoldPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        // When the player who applied this plays a card, deal damage to this enemy
        if (cardPlay.Card.Owner.Creature != base.Owner && base.Amount > 0)
        {
            Flash();
            await CreatureCmd.Damage(context, base.Owner, base.Amount, ValueProp.Unpowered, cardPlay.Card.Owner.Creature, null);
        }
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // Choke wears off at end of player turn (like STS1)
        if (side != base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

/// <summary>
/// When this creature dies, deal damage equal to its Max HP to ALL enemies.
/// STS1: Corpse Explosion.
/// </summary>
public sealed class CorpseExplosionPower : PowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // CRITICAL: PowerModel default removes powers from the dying creature BEFORE
    // AfterDeath hooks fire on it (Creature.RemoveAllPowersAfterDeath uses
    // ShouldPowerBeRemovedAfterOwnerDeath()). Without this override the explosion
    // never triggers. STS2's SteamEruptionPower has the same pattern.
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (target != base.Owner) return;
        if (wasRemovalPrevented) return;

        Flash();
        decimal maxHp = target.MaxHp;
        // Corpse Explosion should hit the dead target's teammates (other enemies),
        // not the opposing side.
        var enemies = base.CombatState.GetTeammatesOf(target)
            .Where(c => c.IsAlive && c != target).ToList();
        if (enemies.Count > 0)
        {
            await CreatureCmd.Damage(choiceContext, enemies, maxHp, ValueProp.Unpowered, (Creature?)null, null);
        }
    }
}

/// <summary>
/// Next turn, your attacks deal double damage.
/// STS1: Phantasmal Killer.
/// </summary>
public sealed class PhantasmalKillerPower : PowerModel
{
    private bool _isActive;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side || _isActive)
            return;

        Flash();
        _isActive = true;
        await Task.CompletedTask;
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? card)
    {
        if (!_isActive) return 1m;
        if (dealer != base.Owner) return 1m;
        if (!props.HasFlag(ValueProp.Move)) return 1m;
        return 2m;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side && _isActive)
        {
            await PowerCmd.Remove(this);
        }
    }
}

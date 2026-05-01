using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// STS1 Silent-specific relics (new implementations, not in STS2)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// STS1 Wrist Blade: Attacks that cost 0 deal 4 additional damage.
/// </summary>
public sealed class WristBlade : ClassicRelic
{
    public WristBlade() : base("wBlade") { }

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ExtraDamage", 4m)];

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // IsPoweredAttack is internal; inline the check: Move flag + not Unpowered
        if (!props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return 0m;
        if (cardSource == null) return 0m;
        if (dealer != Owner.Creature && cardSource.Owner != Owner) return 0m;
        if (cardSource.EnergyCost.Canonical != 0) return 0m;
        return DynamicVars["ExtraDamage"].BaseValue;
    }
}

/// <summary>
/// STS1 Hovering Kite: The first time you discard a card each turn, gain 1 Energy.
/// </summary>
public sealed class HoveringKite : ClassicRelic
{
    private bool _triggeredThisTurn;

    public HoveringKite() : base("kite") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.ForEnergy(this)];

    private bool TriggeredThisTurn
    {
        get => _triggeredThisTurn;
        set { AssertMutable(); _triggeredThisTurn = value; }
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side)
            TriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card.Owner != Owner) return;
        if (TriggeredThisTurn) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;
        if (Owner.Creature.Side != combatState.CurrentSide) return;

        TriggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }
}

/// <summary>
/// STS1 The Specimen: When an enemy dies with Poison, transfer its Poison to a random enemy.
/// </summary>
public sealed class TheSpecimen : ClassicRelic
{
    public TheSpecimen() : base("the_specimen") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>()];

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature target, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (target.Side == Owner.Creature.Side) return;

        var poisonPower = target.GetPower<PoisonPower>();
        if (poisonPower == null || poisonPower.Amount <= 0) return;

        var aliveEnemies = target.CombatState.GetOpponentsOf(Owner.Creature)
            .Where(c => c.IsAlive && c != target)
            .ToList();
        if (aliveEnemies.Count == 0) return;

        Flash();
        Creature transferTarget = Owner.RunState.Rng.CombatTargets.NextItem(aliveEnemies);
        await PowerCmd.Apply<PoisonPower>(choiceContext, transferTarget, poisonPower.Amount, Owner.Creature, null);
    }
}

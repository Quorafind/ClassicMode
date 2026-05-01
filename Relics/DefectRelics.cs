using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// STS1 Defect-specific relics (new implementations, not in STS2)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// STS1 Frozen Core: If you end your turn with empty Orb slots, Channel 1 Frost.
/// Boss Relic (upgrade of Cracked Core).
/// </summary>
public sealed class FrozenCore : ClassicRelic
{
    public FrozenCore() : base("frozenCore") { }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<FrostOrb>()
    ];

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Creature.Side) return;

        var orbQueue = Owner.PlayerCombatState?.OrbQueue;
        if (orbQueue == null)
            return;

        if (orbQueue.Orbs.Count < orbQueue.Capacity)
        {
            Flash();
            await OrbCmd.Channel<FrostOrb>(new BlockingPlayerChoiceContext(), Owner);
        }
    }
}

/// <summary>
/// STS1 Inserter: Every 2 turns, gain 1 Orb slot.
/// </summary>
public sealed class Inserter : ClassicRelic
{
    private int _turnsElapsed;

    public Inserter() : base("inserter") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;
    public override int DisplayAmount => DynamicVars["Turns"].IntValue - (_turnsElapsed % DynamicVars["Turns"].IntValue);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("Turns", 2m)];

    private int TurnsElapsed
    {
        get => _turnsElapsed;
        set { AssertMutable(); _turnsElapsed = value; }
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

        TurnsElapsed++;
        int interval = DynamicVars["Turns"].IntValue;
        if (TurnsElapsed % interval == 0)
        {
            Flash();
            await OrbCmd.AddSlots(Owner, 1);
        }
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side && combatState.RoundNumber <= 1)
            TurnsElapsed = 0;
        return Task.CompletedTask;
    }
}

/// <summary>
/// STS1 Nuclear Battery: At the start of combat, Channel 1 Plasma Orb.
/// </summary>
public sealed class NuclearBattery : ClassicRelic
{
    public NuclearBattery() : base("battery") { }

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<PlasmaOrb>()
    ];

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side && combatState.RoundNumber <= 1)
        {
            Flash();
            await OrbCmd.Channel<PlasmaOrb>(new BlockingPlayerChoiceContext(), Owner);
        }
    }
}

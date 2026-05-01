using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

// ═══════════════════════════════════════════════════════════════════
// STS1 Ironclad-specific relics (new implementations, not in STS2)
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// STS1 Mark of Pain: Gain 1 Energy at the start of each turn.
/// Start each combat with 2 Wounds in your draw pile.
/// Boss Energy Relic.
/// </summary>
public sealed class MarkOfPain : ClassicRelic
{
    public MarkOfPain() : base("mark_of_pain") { }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("Wounds", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this),
        HoverTipFactory.FromCard<Wound>()
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != Owner) return amount;
        return amount + (decimal)DynamicVars.Energy.IntValue;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner || combatState.RoundNumber > 1) return;

        Flash();
        int woundCount = DynamicVars["Wounds"].IntValue;
        for (int i = 0; i < woundCount; i++)
        {
            CardModel wound = combatState.CreateCard<Wound>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(wound, PileType.Draw, null);
        }
    }
}

/// <summary>
/// STS1 Champion Belt: Whenever you apply Vulnerable, also apply 1 Weak.
/// </summary>
public sealed class ChampionBelt : ClassicRelic
{
    public ChampionBelt() : base("championBelt") { }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("WeakAmount", 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>()
    ];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        _ = choiceContext;
        if (power is VulnerablePower
            && applier == Owner.Creature
            && power.Owner != Owner.Creature
            && amount > 0)
        {
            Flash();
            await PowerCmd.Apply<WeakPower>(choiceContext, power.Owner, 1m, Owner.Creature, null);
        }
    }
}

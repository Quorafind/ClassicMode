using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClassicModeMod;

internal static class OrbDamageScope
{
    private static readonly AsyncLocal<Stack<OrbModel>?> CurrentOrbs = new();

    public static OrbModel? CurrentOrb =>
        CurrentOrbs.Value is { Count: > 0 } stack ? stack.Peek() : null;

    public static void Push(OrbModel orb)
    {
        (CurrentOrbs.Value ??= new Stack<OrbModel>()).Push(orb);
    }

    public static async Task Wrap(Task task, OrbModel orb)
    {
        try
        {
            await task;
        }
        finally
        {
            Stack<OrbModel>? stack = CurrentOrbs.Value;
            if (stack != null && stack.Count > 0 && ReferenceEquals(stack.Peek(), orb))
            {
                stack.Pop();
                if (stack.Count == 0)
                    CurrentOrbs.Value = null;
            }
        }
    }
}

[HarmonyPatch(typeof(OrbCmd), "Passive")]
internal static class OrbPassiveScopePatch
{
    static void Prefix(OrbModel orb)
    {
        OrbDamageScope.Push(orb);
    }

    static void Postfix(OrbModel orb, ref Task __result)
    {
        __result = OrbDamageScope.Wrap(__result, orb);
    }
}

[HarmonyPatch(typeof(OrbCmd), "Evoke")]
internal static class OrbEvokeScopePatch
{
    static void Prefix(OrbModel evokedOrb)
    {
        OrbDamageScope.Push(evokedOrb);
    }

    static void Postfix(OrbModel evokedOrb, ref Task __result)
    {
        __result = OrbDamageScope.Wrap(__result, evokedOrb);
    }
}

// ═══════════════════════════════════════════════════════════════════
// CLASSIC DEFECT POWERS
// ═══════════════════════════════════════════════════════════════════

/// <summary>
/// STS1 Lock-On: the target receives 50% more damage from the applier's Orbs.
/// </summary>
public sealed class LockOnPower_C : PowerModel
{
    private const decimal OrbDamageMultiplier = 1.5m;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
            return 1m;
        if (!props.HasFlag(ValueProp.Unpowered))
            return 1m;

        OrbModel? orb = OrbDamageScope.CurrentOrb;
        if (orb == null || orb.Owner.Creature != dealer)
            return 1m;
        if (base.Applier?.Player != null && orb.Owner != base.Applier.Player)
            return 1m;

        return OrbDamageMultiplier;
    }

    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Enemy)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }
}

/// <summary>
/// STS1 Electrodynamics: Lightning hits ALL enemies.
/// After a Lightning orb evokes, deal the same damage to all enemies not already hit.
/// </summary>
public sealed class ElectrodynamicsPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}

[HarmonyPatch(typeof(LightningOrb), "ApplyLightningDamage")]
internal static class ElectrodynamicsLightningPatch
{
    static void Postfix(
        LightningOrb __instance,
        decimal value,
        Creature? target,
        PlayerChoiceContext choiceContext,
        ref Task<IEnumerable<Creature>> __result)
    {
        __result = Wrap(__instance, value, choiceContext, __result);
    }

    private static async Task<IEnumerable<Creature>> Wrap(
        LightningOrb orb,
        decimal damageValue,
        PlayerChoiceContext choiceContext,
        Task<IEnumerable<Creature>> originalTask)
    {
        var hitTargets = (await originalTask)?.ToHashSet() ?? [];

        var ownerCreature = orb.Owner?.Creature;
        if (ownerCreature == null)
            return hitTargets;

        var electrodynamics = ownerCreature.GetPower<ElectrodynamicsPower_C>();
        if (electrodynamics == null)
            return hitTargets;

        var remaining = orb.CombatState
            .GetOpponentsOf(ownerCreature)
            .Where(c => c.IsHittable && !hitTargets.Contains(c))
            .ToList();

        if (remaining.Count == 0)
            return hitTargets;

        foreach (var enemy in remaining)
        {
            VfxCmd.PlayOnCreature(enemy, "vfx/vfx_attack_lightning");
        }

        await CreatureCmd.Damage(choiceContext, remaining, damageValue, ValueProp.Unpowered, ownerCreature, null);
        foreach (var enemy in remaining)
            hitTargets.Add(enemy);

        return hitTargets;
    }
}

/// <summary>
/// STS1 Static Discharge: Whenever you take unblocked attack damage, channel Amount Lightning.
/// </summary>
public sealed class StaticDischargePower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
            return;

        // Match the powered-attack damage pattern (see TheGambitPower/FlameBarrierPower).
        if (result.UnblockedDamage <= 0)
            return;
        if (dealer == null || dealer.Side == base.Owner.Side)
            return;
        if (!props.IsPoweredAttack())
            return;

        Flash();
        for (int i = 0; i < (int)base.Amount; i++)
        {
            await OrbCmd.Channel<LightningOrb>(choiceContext, base.Owner.Player);
        }
    }
}

/// <summary>
/// STS1 Creative AI: At the start of each turn, add Amount random Power card to hand.
/// </summary>
public sealed class CreativeAiPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        Flash();
        for (int i = 0; i < (int)base.Amount; i++)
        {
            CardModel card = CardFactory.GetDistinctForCombat(
                base.Owner.Player,
                from c in base.Owner.Player.Character.CardPool.GetUnlockedCards(
                    base.Owner.Player.UnlockState,
                    base.Owner.Player.RunState.CardMultiplayerConstraint)
                where c.Type == CardType.Power
                select c,
                1,
                base.Owner.Player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (card != null)
            {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, base.Owner.Player);
            }
        }
    }
}

/// <summary>
/// STS1 Hello World: At the start of each turn, add a random Common card to hand.
/// </summary>
public sealed class HelloWorldPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        Flash();
        for (int i = 0; i < (int)base.Amount; i++)
        {
            CardModel card = CardFactory.GetDistinctForCombat(
                base.Owner.Player,
                from c in base.Owner.Player.Character.CardPool.GetUnlockedCards(
                    base.Owner.Player.UnlockState,
                    base.Owner.Player.RunState.CardMultiplayerConstraint)
                where c.Rarity == CardRarity.Common
                select c,
                1,
                base.Owner.Player.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (card != null)
            {
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, base.Owner.Player);
            }
        }
    }
}

/// <summary>
/// STS1 Storm: When you play a Power card, channel Amount Lightning.
/// </summary>
public sealed class StormPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner && cardPlay.Card.Type == CardType.Power)
        {
            Flash();
            for (int i = 0; i < (int)base.Amount; i++)
            {
                await OrbCmd.Channel<LightningOrb>(choiceContext, base.Owner.Player);
            }
        }
    }
}

/// <summary>
/// STS1 Machine Learning: At the start of each turn, draw Amount extra cards.
/// </summary>
public sealed class MachineLearningPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        Flash();
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), base.Amount, base.Owner.Player);
    }
}

/// <summary>
/// STS1 Self Repair: At end of combat, heal Amount HP.
/// Use AfterCombatEnd timing so the power is still present when hooks fire.
/// </summary>
public sealed class SelfRepairPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (base.Owner.Player != null && !base.Owner.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(base.Owner, base.Amount);
        }
    }
}

/// <summary>
/// STS1 Loop: At the start of each turn, trigger the passive of your first Orb Amount time(s).
/// </summary>
public sealed class LoopPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != base.Owner.Side)
            return;

        var orbQueue = base.Owner.Player?.PlayerCombatState?.OrbQueue;
        if (orbQueue?.Orbs.Count > 0)
        {
            Flash();
            var firstOrb = orbQueue.Orbs[0];
            for (int i = 0; i < (int)base.Amount; i++)
            {
                await OrbCmd.Passive(new ThrowingPlayerChoiceContext(), firstOrb, null);
            }
        }
    }
}

/// <summary>
/// STS1 Heatsinks: Whenever you play a Power card, draw Amount card(s).
/// </summary>
public sealed class HeatsinksPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == base.Owner
            && cardPlay.Card.Type == CardType.Power
            && cardPlay.Card is not Heatsinks_C)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, base.Amount, base.Owner.Player);
        }
    }
}

/// <summary>
/// STS1 Amplify: Next Power card played this turn is played twice.
/// Follows the BurstPower pattern (type-filtered duplication).
/// </summary>
public sealed class AmplifyPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != base.Owner) return playCount;
        if (card.Type != CardType.Power) return playCount;
        return playCount + 1;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner.Creature == base.Owner && card.Type == CardType.Power)
        {
            await PowerCmd.Decrement(this);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == base.Owner.Side)
        {
            await PowerCmd.Remove(this);
        }
    }
}

/// <summary>
/// STS1 Echo Form: First card each turn is played twice.
/// Uses ModifyCardPlayCount hook (same pattern as DuplicationPower).
/// Amount = number of cards to double per turn.
/// </summary>
public sealed class EchoFormPower_C : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _doublesRemaining;

    private int DoublesRemaining
    {
        get => _doublesRemaining;
        set { AssertMutable(); _doublesRemaining = value; }
    }

    public override Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == base.Owner.Side)
        {
            DoublesRemaining = (int)base.Amount;
        }
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != base.Owner) return playCount;
        if (DoublesRemaining <= 0) return playCount;
        return playCount + 1;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner.Creature == base.Owner && DoublesRemaining > 0)
        {
            Flash();
            DoublesRemaining--;
        }
        return Task.CompletedTask;
    }
}

using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ClassicModeMod;

[HarmonyPatch(typeof(NIntent), nameof(NIntent.UpdateIntent))]
internal static class RunicDomeIntentPatch
{
    static void Prefix(ref AbstractIntent intent, Creature owner)
    {
        if (intent is UnknownIntent)
            return;

        if (!ShouldMaskIntent(owner))
            return;

        intent = new UnknownIntent();
    }

    private static bool ShouldMaskIntent(Creature owner)
    {
        if (owner.Monster == null)
            return false;

        var combatState = owner.CombatState;
        if (combatState == null)
            return false;

        return combatState.Players.Any(p => p.Relics.Any(r => r is RunicDomeRelic));
    }
}

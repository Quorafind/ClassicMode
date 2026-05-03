using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ClassicModeMod;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal), typeof(Creature), typeof(decimal), typeof(bool))]
internal static class MagicFlowerHealPatch
{
    static void Prefix(Creature creature, ref decimal amount)
    {
        if (amount <= 0 || creature?.CombatState == null)
            return;

        MagicFlowerRelic? relic = creature.Player?.GetRelic<MagicFlowerRelic>();
        if (relic == null)
            return;

        decimal bonus = decimal.Ceiling(amount * 0.5m);
        if (bonus <= 0)
            return;

        relic.Flash();
        amount += bonus;
    }
}

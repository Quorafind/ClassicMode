using Godot;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ClassicModeMod;

public sealed class ClassicIroncladCardPool : CardPoolModel
{
    public override string Title => "ironclad";

    public override string EnergyColorName => "ironclad";

    public override string CardFrameMaterialPath => "card_frame_red";

    public override Color DeckEntryCardColor => new("FF6450");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        var classicCards = new CardModel[]
        {
            // ── Basic ──
            ModelDb.Card<StrikeIronclad_C>(),
            ModelDb.Card<DefendIronclad_C>(),
            ModelDb.Card<Bash_C>(),

            // ── Common ──
            ModelDb.Card<Anger_C>(),
            ModelDb.Card<Armaments_C>(),
            ModelDb.Card<BodySlam_C>(),
            ModelDb.Card<Clash_C>(),
            ModelDb.Card<Cleave_C>(),
            ModelDb.Card<Clothesline_C>(),
            ModelDb.Card<Flex_C>(),
            ModelDb.Card<Havoc_C>(),
            ModelDb.Card<Headbutt_C>(),
            ModelDb.Card<IronWave_C>(),
            ModelDb.Card<PerfectedStrike_C>(),
            ModelDb.Card<PommelStrike_C>(),
            ModelDb.Card<ShrugItOff_C>(),
            ModelDb.Card<SwordBoomerang_C>(),
            ModelDb.Card<ThunderClap_C>(),
            ModelDb.Card<TrueGrit_C>(),
            ModelDb.Card<TwinStrike_C>(),
            ModelDb.Card<Warcry_C>(),
            ModelDb.Card<WildStrike_C>(),

            // ── Uncommon ──
            ModelDb.Card<BloodForBlood_C>(),
            ModelDb.Card<Carnage_C>(),
            ModelDb.Card<Dropkick_C>(),
            ModelDb.Card<HeavyBlade_C>(),
            ModelDb.Card<Hemokinesis_C>(),
            ModelDb.Card<Pummel_C>(),
            ModelDb.Card<Rampage_C>(),
            ModelDb.Card<RecklessCharge_C>(),
            ModelDb.Card<SearingBlow_C>(),
            ModelDb.Card<SeverSoul_C>(),
            ModelDb.Card<Uppercut_C>(),
            ModelDb.Card<BattleTrance_C>(),
            ModelDb.Card<Bloodletting_C>(),
            ModelDb.Card<BurningPact_C>(),
            ModelDb.Card<Disarm_C>(),
            ModelDb.Card<DualWield_C>(),
            ModelDb.Card<Entrench_C>(),
            ModelDb.Card<FlameBarrier_C>(),
            ModelDb.Card<GhostlyArmor_C>(),
            ModelDb.Card<InfernalBlade_C>(),
            ModelDb.Card<Intimidate_C>(),
            ModelDb.Card<PowerThrough_C>(),
            ModelDb.Card<Rage_C>(),
            ModelDb.Card<SecondWind_C>(),
            ModelDb.Card<SeeingRed_C>(),
            ModelDb.Card<Sentinel_C>(),
            ModelDb.Card<Shockwave_C>(),
            ModelDb.Card<SpotWeakness_C>(),
            ModelDb.Card<Berserk_C>(),
            ModelDb.Card<Combust_C>(),
            ModelDb.Card<DarkEmbrace_C>(),
            ModelDb.Card<Evolve_C>(),
            ModelDb.Card<FireBreathing_C>(),
            ModelDb.Card<Inflame_C>(),
            ModelDb.Card<Juggernaut_C>(),
            ModelDb.Card<Metallicize_C>(),
            ModelDb.Card<Rupture_C>(),
            ModelDb.Card<FeelNoPain_C>(),

            // ── Rare ──
            ModelDb.Card<Bludgeon_C>(),
            ModelDb.Card<Feed_C>(),
            ModelDb.Card<FiendFire_C>(),
            ModelDb.Card<Immolate_C>(),
            ModelDb.Card<Reaper_C>(),
            ModelDb.Card<Whirlwind_C>(),
            ModelDb.Card<DoubleTap_C>(),
            ModelDb.Card<Exhume_C>(),
            ModelDb.Card<Impervious_C>(),
            ModelDb.Card<LimitBreak_C>(),
            ModelDb.Card<Offering_C>(),
            ModelDb.Card<Barricade_C>(),
            ModelDb.Card<Brutality_C>(),
            ModelDb.Card<Corruption_C>(),
            ModelDb.Card<DemonForm_C>(),
        };

        return classicCards
        .Concat(ModelDb.CardPool<IroncladCardPool>().AllCards.Where(c => c.Rarity == CardRarity.Ancient))
        .GroupBy(c => c.Id)
        .Select(g => g.First())
        .ToArray();
    }
}

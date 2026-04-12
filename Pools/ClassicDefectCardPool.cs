using Godot;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ClassicModeMod;

public sealed class ClassicDefectCardPool : CardPoolModel
{
    public override string Title => "defect";

    public override string EnergyColorName => "defect";

    public override string CardFrameMaterialPath => "card_frame_blue";

    public override Color DeckEntryCardColor => new("5CC1FF");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards()
    {
        var classicCards = new CardModel[]
        {
            // ── Basic ──
            ModelDb.Card<StrikeDefect_C>(),
            ModelDb.Card<DefendDefect_C>(),
            ModelDb.Card<Zap_C>(),
            ModelDb.Card<Dualcast_C>(),

            // ── Common Attacks ──
            ModelDb.Card<BallLightning_C>(),
            ModelDb.Card<Barrage_C>(),
            ModelDb.Card<BeamCell_C>(),
            ModelDb.Card<Claw_C>(),
            ModelDb.Card<ColdSnap_C>(),
            ModelDb.Card<CompiledDriver_C>(),
            ModelDb.Card<GoForTheEyes_C>(),
            ModelDb.Card<Streamline_C>(),
            ModelDb.Card<SweepingBeam_C>(),

            // ── Common Skills ──
            ModelDb.Card<Coolheaded_C>(),
            ModelDb.Card<Leap_C>(),
            ModelDb.Card<Reboot_C>(),
            ModelDb.Card<Recursion_C>(),
            ModelDb.Card<Skim_C>(),
            ModelDb.Card<SteamBarrier_C>(),
            ModelDb.Card<Turbo_C>(),

            // ── Uncommon Attacks ──
            ModelDb.Card<Blizzard_C>(),
            ModelDb.Card<DoomAndGloom_C>(),
            ModelDb.Card<Ftl_C>(),
            ModelDb.Card<Lockdown_C>(),
            ModelDb.Card<Melter_C>(),
            ModelDb.Card<Rebound_C>(),
            ModelDb.Card<RipAndTear_C>(),
            ModelDb.Card<Scrape_C>(),
            ModelDb.Card<Sunder_C>(),

            // ── Uncommon Skills ──
            ModelDb.Card<Aggregate_C>(),
            ModelDb.Card<AutoShields_C>(),
            ModelDb.Card<BootSequence_C>(),
            ModelDb.Card<Chaos_C>(),
            ModelDb.Card<ChargeBattery_C>(),
            ModelDb.Card<Chill_C>(),
            ModelDb.Card<Consume_C>(),
            ModelDb.Card<Darkness_C>(),
            ModelDb.Card<Equilibrium_C>(),
            ModelDb.Card<Fission_C>(),
            ModelDb.Card<Forcefield_C>(),
            ModelDb.Card<Fusion_C>(),
            ModelDb.Card<GeneticAlgorithm_C>(),
            ModelDb.Card<Glacier_C>(),
            ModelDb.Card<Hologram_C>(),
            ModelDb.Card<Overclock_C>(),
            ModelDb.Card<Rainbow_C>(),
            ModelDb.Card<ReinforcedBody_C>(),
            ModelDb.Card<Reprogram_C>(),
            ModelDb.Card<Seek_C>(),
            ModelDb.Card<Stack_C>(),
            ModelDb.Card<Tempest_C>(),
            ModelDb.Card<WhiteNoise_C>(),

            // ── Uncommon Powers ──
            ModelDb.Card<Buffer_C>(),
            ModelDb.Card<Capacitor_C>(),
            ModelDb.Card<Defragment_C>(),
            ModelDb.Card<Heatsinks_C>(),
            ModelDb.Card<HelloWorld_C>(),
            ModelDb.Card<Loop_C>(),
            ModelDb.Card<StaticDischarge_C>(),
            ModelDb.Card<Storm_C>(),

            // ── Rare Attacks ──
            ModelDb.Card<AllForOne_C>(),
            ModelDb.Card<CoreSurge_C>(),
            ModelDb.Card<HyperBeam_C>(),
            ModelDb.Card<MeteorStrike_C>(),
            ModelDb.Card<ThunderStrike_C>(),

            // ── Rare Skills ──
            ModelDb.Card<Amplify_C>(),
            ModelDb.Card<DoubleEnergy_C>(),
            ModelDb.Card<MultiCast_C>(),
            ModelDb.Card<Recycle_C>(),

            // ── Rare Powers ──
            ModelDb.Card<BiasedCognition_C>(),
            ModelDb.Card<CreativeAi_C>(),
            ModelDb.Card<EchoForm_C>(),
            ModelDb.Card<Electrodynamics_C>(),
            ModelDb.Card<MachineLearning_C>(),
            ModelDb.Card<SelfRepair_C>(),
        };

        return classicCards
        .Concat(ModelDb.CardPool<DefectCardPool>().AllCards.Where(c => c.Rarity == CardRarity.Ancient))
        .GroupBy(c => c.Id)
        .Select(g => g.First())
        .ToArray();
    }
}

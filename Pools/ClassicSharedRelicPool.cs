using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace ClassicModeMod;

public sealed class ClassicSharedRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            // Common
            ModelDb.Relic<OmamoriRelic>(),
            ModelDb.Relic<SmilingMaskRelic>(),
            ModelDb.Relic<TinyChestRelic>(),
            ModelDb.Relic<PreservedInsectRelic>(),
            ModelDb.Relic<ToyOrnithopterRelic>(),
            ModelDb.Relic<Nunchaku>(),
            ModelDb.Relic<LuckyFysh>(),
            ModelDb.Relic<BronzeScales>(),

            // Uncommon
            ModelDb.Relic<BlueCandleRelic>(),
            ModelDb.Relic<BottledFlameRelic>(),
            ModelDb.Relic<BottledLightningRelic>(),
            ModelDb.Relic<BottledTornadoRelic>(),
            ModelDb.Relic<MatryoshkaRelic>(),
            ModelDb.Relic<InkBottleRelic>(),
            ModelDb.Relic<QuestionCardRelic>(),
            ModelDb.Relic<SingingBowlRelic>(),
            ModelDb.Relic<SundialRelic>(),

            // Rare
            ModelDb.Relic<IncenseBurnerRelic>(),
            ModelDb.Relic<FossilizedHelixRelic>(),
            ModelDb.Relic<BirdFacedUrnRelic>(),
            ModelDb.Relic<CalipersRelic>(),
            ModelDb.Relic<DuVuDollRelic>(),
            ModelDb.Relic<DeadBranchRelic>(),
            ModelDb.Relic<GingerRelic>(),
            ModelDb.Relic<PeacePipeRelic>(),
            ModelDb.Relic<TurnipRelic>(),
            ModelDb.Relic<ThreadAndNeedleRelic>(),
            ModelDb.Relic<ToriiRelic>(),
            ModelDb.Relic<SacredBarkRelic>(),
            ModelDb.Relic<SlaversCollarRelic>(),

            // Shop
            ModelDb.Relic<FrozenEyeRelic>(),
            ModelDb.Relic<MedicalKitRelic>(),
            ModelDb.Relic<StrangeSpoonRelic>(),
            ModelDb.Relic<ClockworkSouvenirRelic>(),
            ModelDb.Relic<OrangePelletsRelic>(),
            ModelDb.Relic<PrismaticShardRelic>(),
            ModelDb.Relic<CoffeeDripperRelic>(),
            ModelDb.Relic<CursedKeyRelic>(),
            ModelDb.Relic<FusionHammerRelic>(),
            ModelDb.Relic<BustedCrownRelic>(),
            ModelDb.Relic<RunicDomeRelic>(),
            ModelDb.Relic<TinyHouseRelic>(),
        ];
    }
}

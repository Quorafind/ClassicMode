using Godot;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

public sealed class ClassicSilentRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "silent";

    public override Color LabOutlineColor => new("70CC83");

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            // Starter
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.RingOfTheSnake>(),
            ModelDb.Relic<RingOfTheSerpent>(),
            // Uncommon
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Tingsha>(),
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.OrnamentalFan>(),
            // Rare
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.ToughBandages>(),
            ModelDb.Relic<TheSpecimen>(),
            // Shop
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.NinjaScroll>(),
        ];
    }
}

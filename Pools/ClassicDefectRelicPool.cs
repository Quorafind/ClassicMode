using Godot;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

public sealed class ClassicDefectRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "defect";

    public override Color LabOutlineColor => new("5CC1FF");

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            // Starter
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.CrackedCore>(),
            ModelDb.Relic<FrozenCore>(),
            // Common
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.DataDisk>(),
            // Uncommon
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.SymbioticVirus>(),
            // Rare
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.EmotionChip>(),
            // Shop
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.RunicCapacitor>(),
        ];
    }
}

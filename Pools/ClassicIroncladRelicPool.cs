using Godot;
using MegaCrit.Sts2.Core.Models;

namespace ClassicModeMod;

public sealed class ClassicIroncladRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";

    public override Color LabOutlineColor => new("FF6450");

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            // Starter
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.BurningBlood>(),
            // Starter upgrade (boss relic swap)
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.BlackBlood>(),
            // Common
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.RedSkull>(),
            // Uncommon
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.SelfFormingClay>(),
            // Rare
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.PaperKrane>(),
            ModelDb.Relic<ChampionBelt>(),
            ModelDb.Relic<MagicFlowerRelic>(),
            ModelDb.Relic<RunicCubeRelic>(),
            // Shop
            ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Brimstone>(),
        ];
    }
}

using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ClassicModeMod;

public sealed class HybridIroncladRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";
    public override Color LabOutlineColor => StsColors.red;

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        HybridPoolHelper.MergeRelics(
            ModelDb.RelicPool<IroncladRelicPool>(),
            ModelDb.RelicPool<ClassicIroncladRelicPool>());
}

public sealed class HybridSilentRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "silent";
    public override Color LabOutlineColor => new("70CC83");

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        HybridPoolHelper.MergeRelics(
            ModelDb.RelicPool<SilentRelicPool>(),
            ModelDb.RelicPool<ClassicSilentRelicPool>());
}

public sealed class HybridDefectRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "defect";
    public override Color LabOutlineColor => new("5CC1FF");

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        HybridPoolHelper.MergeRelics(
            ModelDb.RelicPool<DefectRelicPool>(),
            ModelDb.RelicPool<ClassicDefectRelicPool>());
}

public sealed class HybridSharedRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics() =>
        HybridPoolHelper.MergeRelics(
            ModelDb.RelicPool<SharedRelicPool>(),
            ModelDb.RelicPool<ClassicSharedRelicPool>());
}

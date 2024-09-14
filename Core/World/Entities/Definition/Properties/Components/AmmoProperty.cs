using Helion.Maps.Shared;

namespace Helion.World.Entities.Definition.Properties.Components;

public struct AmmoProperty
{
    const int Skills = 5;

    public int BackpackAmount;
    public int BackpackMaxAmount;
    public int? DropAmount;
    public int? DropBackpackAmmo;
    public double[]? SkillMultiplier;

    public readonly double? GetSkillMultiplier(SkillLevel level)
    {
        if (SkillMultiplier == null)
            return null;

        int index = (int)level - 1;
        if (index < 0 || index >= Skills)
            return 1;

        return SkillMultiplier[index];
    }

    public void SetSkillMultiplier(SkillLevel level, double value)
    {
        SkillMultiplier ??= new double[Skills];

        int index = (int)level - 1;
        if (index < 0 || index >= Skills)
            return;

        SkillMultiplier[index] = value;
    }
}

using Helion.World.Entities;

namespace Helion.World.Special
{
    public interface ISpecial
    {
        SpecialTickStatus Tick();
        void Use(Entity entity);
        void ResetInterpolation() { }
        SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Default;
    }
}
using Helion.Worlds.Entities;

namespace Helion.Worlds.Special
{
    public interface ISpecial
    {
        SpecialTickStatus Tick();
        void Use(Entity entity);
        SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Default;
    }
}
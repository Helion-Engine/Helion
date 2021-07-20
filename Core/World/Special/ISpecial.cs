using Helion.Models;
using Helion.World.Entities;

namespace Helion.World.Special
{
    public interface ISpecial
    {
        SpecialTickStatus Tick();
        bool Use(Entity entity);
        void ResetInterpolation() { }
        SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Default;
        ISpecialModel? ToSpecialModel() => null;
    }
}
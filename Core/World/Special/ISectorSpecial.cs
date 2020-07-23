using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special
{
    public interface ISectorSpecial : ISpecial
    {
        Sector Sector { get; }
        void FinalizeDestroy();
    }
}
using Helion.World.Geometry.Sectors;

namespace Helion.World.Special
{
    public interface ISectorSpecial : ISpecial
    {
        Sector Sector { get; }
    }
}
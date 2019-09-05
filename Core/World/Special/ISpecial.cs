using Helion.World.Geometry.Sectors;

namespace Helion.World.Special
{
    public interface ISpecial
    {
        Sector? Sector { get; }
        SpecialTickStatus Tick(long gametic);
        void Use();
    }
}
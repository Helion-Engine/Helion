using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;

namespace Helion.Maps.Special
{
    public enum SpecialTickStatus
    {
        Continue,
        Destroy,
    }

    public interface ISpecial
    {
        Sector? Sector { get; }
        SpecialTickStatus Tick();
        void Use();
    }
}

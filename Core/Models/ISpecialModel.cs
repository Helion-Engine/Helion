using Helion.World;
using Helion.World.Special;

namespace Helion.Models
{
    public interface ISpecialModel
    {
        ISpecial? ToWorldSpecial(IWorld world);
    }
}

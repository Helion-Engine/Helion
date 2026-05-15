using Helion.World;
using Helion.World.Entities;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public struct QuakeSpecialModel : ISpecialModel
{
    public double Intensity { get; set; }
    public int DamageRadius { get; set; }
    public int TremorRadius { get; set; }
    public WeakEntity Location { get; set; }
    public string Sound { get; set; }
    public int Duration { get; set; }
    public int EntityId { get; set; }

    public readonly ISpecial? ToWorldSpecial(IWorld world)
    {
        var entity = world.EntityManager.FindById(EntityId);
        if (entity == null)
            return null;

        return new QuakeSpecial(world, Intensity, Duration, DamageRadius, TremorRadius, entity, Sound);
    }
}

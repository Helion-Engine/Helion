using Helion.Geometry.Vectors;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Special;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class PushSpecialModel : ISpecialModel
{
    public int Type { get; set; }
    public int SectorId { get; set; }
    public double PushX { get; set; }
    public double PushY { get; set; }
    public double Magnitude { get; set; }
    public int? PusherEntityId { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(SectorId))
            return null;

        Entity? pusher = null;
        if (PusherEntityId.HasValue)
            pusher = world.EntityManager.FindById(PusherEntityId.Value);

        return new PushSpecial((PushType)Type, world, world.Sectors[SectorId], new Vec2D(PushX, PushY), pusher);
    }
}


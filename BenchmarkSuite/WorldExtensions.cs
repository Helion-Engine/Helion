using Helion.World;
using Helion.World.Entities;
using System.Collections.Generic;

namespace BenchmarkSuite;

public static class WorldExtensions
{
    public static IEnumerable<Entity> GetMonstersNoCloset(this IWorld world)
    {
        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (!entity.Flags.CountKill() || (entity.ClosetFlags & ClosetFlags.MonsterCloset) != 0)
                continue;

            yield return entity;
        }
    }
}

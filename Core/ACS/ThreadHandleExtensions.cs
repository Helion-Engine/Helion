using Helion.World;
using Helion.World.Entities;
using HelionACS;

namespace Helion.ACS;

public static class ThreadHandleExtensions
{
    public static Entity? GetActivator(this ThreadHandle thread, IWorld world)
    {
        var activator = thread.GetThreadInfo().Activator;
        if (activator < 0)
            return null;
        return world.EntityManager.FindById(activator);
    }
}

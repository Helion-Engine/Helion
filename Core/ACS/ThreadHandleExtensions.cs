using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
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

    public static Line? GetLine(this ThreadHandle thread, IWorld world)
    {
        var lineId = thread.GetThreadInfo().Line;
        if (lineId < 0 || lineId >= world.Lines.Count)
            return null;
        return world.Lines[lineId];
    }

    public static Side? GetSide(this ThreadHandle thread, IWorld world)
    {
        var line = thread.GetLine(world);
        if (line == null)
            return null;

        var side = thread.GetThreadInfo().Side;
        if (side < 0)
            return null;

        return side == 0 ? line.Front : line.Back;
    }
}

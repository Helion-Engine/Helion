using Helion.Maps.Specials.ZDoom;
using Helion.World.Geometry.Lines;

namespace Helion.World.Special;

public static class LockSpecialUtil
{
    public static bool IsLockSpecial(Line line, out int key)
    {
        if (line.Special.LineSpecialType == ZDoomLineSpecialType.DoorLockedRaise)
        {
            key = line.Args.Arg3;
            return true;
        }

        if (line.Special.LineSpecialType == ZDoomLineSpecialType.DoorGeneric)
        {
            key = line.Args.Arg4;
            return key != 0;
        }

        key = -1;
        return false;
    }
}

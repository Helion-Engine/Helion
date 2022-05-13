using Helion.World;
using Helion.World.Special;
using Helion.World.Special.Specials;
using System;
using System.Collections.Generic;

namespace Helion.Models;

public class StairSpecialModel : ISpecialModel
{
    public readonly int Delay;
    public readonly double StartZ;
    public readonly int Destroy;
    public readonly int DelayTics;
    public readonly int ResetTics;
    public readonly bool Crush;
    public readonly IList<int> SectorIds;
    public readonly IList<int> Heights;
    public readonly SectorMoveSpecialModel MoveSpecial;

    public StairSpecialModel(int delay, double startZ, int destroy, int delayTics, int resetTics, bool crush, 
        IList<int> sectorIds, IList<int> heights, SectorMoveSpecialModel moveSpecial)
    {
        Delay = delay;
        StartZ = startZ;
        Destroy = destroy;
        DelayTics = delayTics;
        ResetTics = resetTics;
        Crush = crush;
        SectorIds = sectorIds;
        Heights = heights;
        MoveSpecial = moveSpecial;
    }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        if (!world.IsSectorIdValid(MoveSpecial.SectorId))
            return null;

        return new StairSpecial(world, world.Sectors[MoveSpecial.SectorId], this);
    }
}

using Helion.Models;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials;

public class DoorOpenCloseSpecial : SectorMoveSpecial
{
    public int Key { get; private set; }

    public DoorOpenCloseSpecial(IWorld world, Sector sector, double dest, double speed, int delay, int key = -1, int lightTag = 0)
        : base(world, sector, sector.Floor.Z, dest,
              new SectorMoveData(SectorPlaneFace.Ceiling, MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay, 
                flags: SectorMoveFlags.Door, lightTag: lightTag), SpecialManager.GetDoorSound(speed))
    {
        Key = key;
    }

    public DoorOpenCloseSpecial(IWorld world, Sector sector, DoorOpenCloseSpecialModel model)
        : base (world, sector, model)
    {
        Key = model.Key;
    }

    public override bool Use(Entity entity)
    {
        if (MoveData.MoveRepetition == MoveRepetition.None || !entity.IsPlayer)
            return false;

        // If the delay is zero then flip the door direction. Otherwise we
        // are in the wait delay and setting the delay back to 0 will
        // immediately bring it back down. Either way we need to set delay
        // to 0, because this effect needs to work immediately.
        if (DelayTics == 0)
            FlipMovementDirection(false);
        DelayTics = 0;
        return true;
    }
}

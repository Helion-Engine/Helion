using Helion.Models;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class DoorOpenCloseSpecial : SectorMoveSpecial
    {
        public int Key { get; private set; }

        public DoorOpenCloseSpecial(IWorld world, Sector sector, double dest, double speed, int delay, int key = -1)
            : base(world, sector, sector.Floor.Z, dest, 
                  new SectorMoveData(SectorPlaneType.Ceiling, MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay), 
                  SpecialManager.GetDoorSound(speed))
        {
            Key = key;
        }

        public DoorOpenCloseSpecial(IWorld world, Sector sector, DoorOpenCloseSpecialModel model)
            : base (world, sector, model)
        {
            Key = model.Key;
        }

        public override void Use(Entity entity)
        {
            if (MoveData.MoveRepetition == MoveRepetition.None || !(entity is Player))
                return;

            // If the delay is zero then flip the door direction. Otherwise we
            // are in the wait delay and setting the delay back to 0 will
            // immediately bring it back down. Either way we need to set delay
            // to 0, because this effect needs to work immediately.
            if (DelayTics == 0)
                FlipMovementDirection(false);
            DelayTics = 0;
        }
    }
}

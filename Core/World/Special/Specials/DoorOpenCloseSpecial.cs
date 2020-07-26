using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.World.Special.Specials
{
    public class DoorOpenCloseSpecial : SectorMoveSpecial
    {
        public int Key { get; private set; }

        public DoorOpenCloseSpecial(WorldBase world, Sector sector, double dest, double speed, int delay, int key = -1)
            : base(world, sector, sector.Floor.Z, dest, 
                  new SectorMoveData(SectorPlaneType.Ceiling, MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay), 
                  SpecialManager.GetDoorSound(speed))
        {
            Key = key;
        }

        public override void Use()
        {
            if (MoveData.MoveRepetition == MoveRepetition.None)
                return;

            // If the delay is zero then flip the door direction. Otherwise we
            // are in the wait delay and setting the delay back to 0 will
            // immediately bring it back down. Either way we need to set delay
            // to 0, because this effect needs to work immediately.
            if (DelayTics == 0)
                FlipMovementDirection();
            DelayTics = 0;
        }
    }
}

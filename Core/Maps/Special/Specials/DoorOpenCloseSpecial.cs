using Helion.Maps.Geometry;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class DoorOpenCloseSpecial : SectorMoveSpecial
    {
        public DoorOpenCloseSpecial(PhysicsManager physicsManager, Sector sector, double dest, double speed, int delay)
            : base(physicsManager, sector, sector.Floor.Z, dest, new SectorMoveData(SectorMoveType.Ceiling, MoveDirection.Up, MoveRepetition.DelayReturn, speed, delay))
        {
        }

        public override void Use()
        {
            // If the delay is zero then flip the door direction
            // Otherwise we are in the wait delay and setting the delay back to 0 will immediately bring it back down
            // Either way we need to set delay to 0, because this effect needs to work immediately
            if (DelayTics == 0)
                FlipMovementDirection();
            DelayTics = 0;
        }
    }
}

using Helion.Worlds.Entities;
using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Special.SectorMovement;

namespace Helion.Worlds.Special.Specials
{
    public class DoorOpenCloseSpecial : SectorMoveSpecial
    {
        public int Key { get; }

        public DoorOpenCloseSpecial(World world, Sector sector, double dest, double speed, int delay, int key = -1)
            : base(world, sector, sector.Floor.Z, dest,
                  new SectorMoveData(SectorPlaneType.Ceiling, MoveDirection.Up, delay > 0 ? MoveRepetition.DelayReturn : MoveRepetition.None, speed, delay),
                  SpecialManager.GetDoorSound(speed))
        {
            Key = key;
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

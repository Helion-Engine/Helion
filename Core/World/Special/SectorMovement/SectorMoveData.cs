using Helion.Util;

namespace Helion.World.Special.SectorMovement
{
    public class SectorMoveData
    {
        public readonly SectorMoveType SectorMoveType;
        public readonly MoveRepetition MoveRepetition;
        public readonly double Speed;
        public readonly int Delay;
        public readonly CrushData? Crush;
        public readonly CIString? FloorChangeTexture;
        public MoveDirection StartDirection;
        
        public SectorMoveData(SectorMoveType moveType, MoveDirection startDirection, MoveRepetition repetition, 
            double speed, int delay, CrushData? crush = null, CIString? floorChangeTexture = null)
        {
            SectorMoveType = moveType;
            StartDirection = startDirection;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
            Crush = crush;
            FloorChangeTexture = floorChangeTexture;
        }
    }
}

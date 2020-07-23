namespace Helion.World.Special.SectorMovement
{
    public class SectorMoveData
    {
        public readonly SectorPlaneType SectorMoveType;
        public readonly MoveRepetition MoveRepetition;
        public readonly double Speed;
        public readonly int Delay;
        public readonly CrushData? Crush;
        public readonly int? FloorChangeTextureHandle;
        public readonly MoveDirection StartDirection;
        
        public SectorMoveData(SectorPlaneType moveType, MoveDirection startDirection, MoveRepetition repetition, 
            double speed, int delay, CrushData? crush = null, int? floorChangeTextureHandle = null)
        {
            SectorMoveType = moveType;
            StartDirection = startDirection;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
            Crush = crush;
            FloorChangeTextureHandle = floorChangeTextureHandle;
        }
    }
}

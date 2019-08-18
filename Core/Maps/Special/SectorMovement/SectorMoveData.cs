using Helion.Util;

namespace Helion.Maps.Special
{
    public class SectorMoveData
    {
        public SectorMoveData(SectorMoveType moveType, MoveDirection startDirection,
           MoveRepetition repetition, double speed, int delay, CrushData? crush = null, CIString? floorChangeTexture = null)
        {
            SectorMoveType = moveType;
            StartDirection = startDirection;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
            Crush = crush;
            FloorChangeTexture = floorChangeTexture;
        }

        public SectorMoveType SectorMoveType { get; private set; }
        public MoveDirection StartDirection { get; private set; }
        public MoveRepetition MoveRepetition { get; private set; }
        public double Speed { get; private set; }
        public int Delay { get; private set; }
        public CrushData? Crush { get; private set; }
        public CIString? FloorChangeTexture { get; private set; }
    }
}

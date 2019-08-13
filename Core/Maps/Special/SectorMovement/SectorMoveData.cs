namespace Helion.Maps.Special
{
    public class SectorMoveData
    {
        public SectorMoveData(SectorMoveType moveType, MoveDirection startDirection,
            MoveRepetition repetition, double speed, int delay)
        {
            SectorMoveType = moveType;
            StartDirection = startDirection;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
        }

        public SectorMoveType SectorMoveType { get; private set; }
        public MoveDirection StartDirection { get; private set; }
        public MoveRepetition MoveRepetition { get; private set; }
        public double Speed { get; private set; }
        public int Delay { get; private set; }
    }
}

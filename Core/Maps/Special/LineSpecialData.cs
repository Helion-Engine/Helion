namespace Helion.Maps.Special
{
    public class LineSpecialData
    {
        public LineSpecialData(LineSpecialType type, ActivationType activationType, SectorMoveType moveType, MoveDirection startDirection,
            SectorDest dest, MoveRepetition repetition, double speed, int delay)
        {
            LineSpecialType = type;
            ActivationType = activationType;
            SectorMoveType = moveType;
            StartDirection = startDirection;
            SectorDestination = dest;
            MoveRepetition = repetition;
            Speed = speed;
            Delay = delay;
        }

        public LineSpecialType LineSpecialType { get; private set; }
        public ActivationType ActivationType { get; private set; }
        public SectorMoveType SectorMoveType { get; private set; }
        public MoveDirection StartDirection { get; private set; }
        public SectorDest SectorDestination { get; private set; }
        public MoveRepetition MoveRepetition { get; private set; }
        public double Speed { get; private set; }
        public int Delay { get; private set; }
    }
}

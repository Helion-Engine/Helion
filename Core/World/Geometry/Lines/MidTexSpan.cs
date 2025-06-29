namespace Helion.World.Geometry.Lines;

public readonly struct MidTexSpan(double bottom, double top, double prevBottom, double prevTop)
{
    public readonly double BottomZ = bottom;
    public readonly double TopZ = top;
    public readonly double PrevBottomZ = prevBottom;
    public readonly double PrevTopZ = prevTop;
}

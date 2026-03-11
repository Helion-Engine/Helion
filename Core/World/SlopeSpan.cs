namespace Helion.World;

public struct SlopeSpan
{
    public double Top;
    public double Bottom;
    public readonly override string ToString() => $"Top={{{Top}}} Bottom={{{Bottom}}}";
}

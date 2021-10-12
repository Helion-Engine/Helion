namespace Helion.Maps.Components.GL;

public class GLSubsector
{
    public readonly int Count;
    public readonly int FirstSegmentIndex;

    public GLSubsector(int count, int firstSegmentIndex)
    {
        Count = count;
        FirstSegmentIndex = firstSegmentIndex;
    }
}


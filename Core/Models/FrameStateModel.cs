namespace Helion.Models;

public class FrameStateModel
{
    public static readonly FrameStateModel Default = new();

    public int FrameIndex;
    public int Tics;
    public bool Destroy;
}

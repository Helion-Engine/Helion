namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public interface IBspHeuristics
{
    public float SubsectorVisibility { get; }
    public float SegVisibility { get; }
    public int SubsectorCount { get; }
    public int SegCount { get; }
    public int LineCount { get; }
    public int LastProcessedId { get; }
    public int Microseconds { get; }
}

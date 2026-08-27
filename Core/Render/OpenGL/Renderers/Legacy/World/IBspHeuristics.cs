namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public interface IBspHeuristics
{
    public int SubsectorCount { get; }
    public int SegCount { get; }
    public int LineCount { get; }
    public int LastProcessedId { get; }
    public int Microseconds { get; }
    public long LastProcessedTimeStamp { get; }
    public bool UseBsp { get; set; }
}

namespace Helion.Render.OpenGL.Renderers.Legacy.World;

public class BspHeuristicInfo
{
    public bool UseBsp { get; set; }
    public int GameTick { get; set; }
    public int SmoothTime { get; set; }
    public int AboveThresholdCount { get; set; }
    public int BelowThresholdCount { get; set; }
    public int SegCount { get; set; }
}

public interface IBspHeuristics
{
    public bool Valid { get; }
    public int SubsectorCount { get; }
    public int SegCount { get; }
    public int LineCount { get; }
    public int LastProcessedId { get; }
    public int Microseconds { get; }
    public long LastProcessedTimeStamp { get; }
    public BspHeuristicInfo Info { get; }
}

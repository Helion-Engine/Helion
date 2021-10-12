namespace Helion.World.Entities.Definition;

public class FrameSet
{
    public string VanillActorName { get; set; } = string.Empty;
    public int StartFrameIndex { get; set; }
    public int Count { get; set; }

    public override string ToString() => $"name={VanillActorName} start={StartFrameIndex} count={Count}";
}

using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;

namespace Helion.Resources.Definitions.Intermission;

public class IntermissionSpot
{
    public string MapName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public Box2I Box { get; set; }

    public Vec2I Vector => (X, Y);
}

using Helion.Geometry.Vectors;
using Helion.Maps.Components;

namespace Helion.Maps.Udmf.Components;

public class UdmfVertex : IVertex
{
    public UdmfVertex(int id, Vec2D position)
    {
        Id = id;
        Position = position;
    }

    public int Id { get; set; }

    public Vec2D Position { get; set; }
}

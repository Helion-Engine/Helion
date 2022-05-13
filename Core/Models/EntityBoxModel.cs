using Helion.Geometry.Vectors;

namespace Helion.Models;

public class EntityBoxModel
{
    public static readonly EntityBoxModel Default = new();

    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public double Radius;
    public double Height;

    public Vec3D GetCenter() => (CenterX, CenterY, CenterZ);
}

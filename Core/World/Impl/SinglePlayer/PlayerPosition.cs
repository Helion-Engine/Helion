using Helion.Geometry.Vectors;

namespace Helion.World.Impl.SinglePlayer;

public readonly struct PlayerPosition(Vec3D position, Vec3D viewDirection, double angleRadians, double pitchRadians, int id)
{
    public readonly Vec3D Position = position;
    public readonly Vec3D ViewDirection = viewDirection;
    public readonly double AngleRadians = angleRadians;
    public readonly double PitchRadians = pitchRadians;
    public readonly int Id = id;
}

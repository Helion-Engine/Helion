using Helion.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Entities.Players;

public class CameraPlayer : Player
{
    public override bool IsCamera => true;

    public CameraPlayer(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world)
        : base (id, thingId, definition, position, angleRadians, sector, world, short.MaxValue)
    {

    }

    public override bool DrawFullBright() => WorldStatic.World.Config.Render.Fullbright;

    public override void Tick()
    {
        PrevPosition = Position;
        m_interpolateAngle = ShouldInterpolate();

        PrevAngle = AngleRadians;
        m_prevPitch = PitchRadians;
        m_prevViewZ = m_viewZ;
    }
}

using Helion.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;

namespace Helion.World.Entities.Players;

public class CameraPlayer : Player
{
    public const int CameraPlayerId = int.MaxValue;
    public override bool IsCamera => true;

    public CameraPlayer(int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world)
    {
        Set(CameraPlayerId, CameraPlayerId, thingId, definition, position, angleRadians, sector, world, CameraPlayerId);
    }

    public override bool DrawFullBright() => WorldStatic.World.Config.Render.Fullbright;

    public override void Tick()
    {

    }
}

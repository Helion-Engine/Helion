namespace Helion.World.Geometry.Walls;

public class SectorWall3D(Wall controlWall, WallLocation location) : Wall(0, location)
{
    private readonly Wall m_controlWall = controlWall;

    public override int TextureHandle
    {
        get => m_controlWall.TextureHandle;
        set { }
    }
}

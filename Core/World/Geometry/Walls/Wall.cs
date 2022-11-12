using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Static;

namespace Helion.World.Geometry.Walls;

public class Wall
{
    public readonly int Id;
    public readonly WallLocation Location;
    public Side Side { get; internal set; }
    public int TextureHandle { get; private set; }

    public Line Line => Side.Line;
    public SectorDynamic Dynamic;
    public bool IsDynamic => Dynamic != SectorDynamic.None;

    public Wall(int id, int textureHandle, WallLocation location)
    {
        Id = id;
        TextureHandle = textureHandle;
        Location = location;

        // We are okay with things blowing up violently if someone forgets
        // to assign it, because that is such a critical error on the part
        // of the developer if this ever happens that it's deserved. Fixing
        // this would lead to some very messy logic, and when this is added
        // to a parent object, it will add itself for us. If this can be
        // fixed in the future with non-messy code, go for it.
        Side = null !;
    }

    public void SetTexture(int texture, SideDataTypes type)
    {
        TextureHandle = texture;
        Side.Line.DataChanges |= LineDataTypes.Texture;
        Side.DataChanges |= type;
    }
}

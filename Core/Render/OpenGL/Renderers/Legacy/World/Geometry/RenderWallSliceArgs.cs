using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;
public struct RenderWallSliceArgs
{
    public Side Side;
    public Side OtherSide;
    public bool IsFrontSide;
    public bool RenderSkySide;
    public bool AllowAlpha;
    public RenderDataStyle Style;
    public Sector WallSector;
    public Sector LightSector;
    public Sector FacingSector;
    public Sector OtherSector;
    public Side? OffsetSide;
}
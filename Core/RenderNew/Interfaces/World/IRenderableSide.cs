using Helion.Geometry.Vectors;

namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableSide
{
    int GetIndex();
    IRenderableWall? GetUpper();
    IRenderableWall? GetMiddle();
    IRenderableWall? GetLower();
    IRenderableSector GetSector();
    Vec2F GetScroll();
    int GetLightLevel();
}
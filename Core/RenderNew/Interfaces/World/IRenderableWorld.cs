using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.RenderNew.Interfaces.World;

public interface IRenderableWorld
{
    int Gametick { get; }
    
    IEnumerable<IRenderableSector> GetRenderableSectors();
    IEnumerable<IRenderableSubsector> GetRenderableSubsectors();
    IEnumerable<IRenderableLine> GetRenderableLines();
    IEnumerable<IRenderableSide> GetRenderableSides();
    IEnumerable<IRenderableWall> GetRenderableWalls();
    IEnumerable<IRenderableEntity> GetRenderableEntities();
    bool TryGetRenderablePlayer(int playerNumber, [NotNullWhen(true)] out IRenderablePlayer? player);
}

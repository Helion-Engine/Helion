using Helion.Geometry.Vectors;
using Helion.Render.Legacy.Shared;
using Helion.World;
using Helion.World.Entities;

namespace Helion.Render.Legacy.Commands.Types;

public record DrawWorldCommand : IRenderCommand
{
    public readonly WorldBase World;
    public readonly Camera Camera;
    public readonly int Gametick;
    public readonly float GametickFraction;
    public readonly Entity ViewerEntity;
    public readonly bool DrawAutomap;
    public readonly Vec2I AutomapOffset;
    public readonly double AutomapScale;

    public DrawWorldCommand(WorldBase world, Camera camera, int gametick, float gametickFraction,
        Entity viewerEntity, bool drawAutomap, Vec2I automapOffset, double automapScale)
    {
        World = world;
        Camera = camera;
        Gametick = gametick;
        GametickFraction = gametickFraction;
        ViewerEntity = viewerEntity;
        DrawAutomap = drawAutomap;
        AutomapOffset = automapOffset;
        AutomapScale = automapScale;
    }
}


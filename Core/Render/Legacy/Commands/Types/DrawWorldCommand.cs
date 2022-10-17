using Helion.Geometry.Vectors;
using Helion.Render.Legacy.Shared;
using Helion.World;
using Helion.World.Entities;
using System.Runtime.InteropServices;

namespace Helion.Render.Legacy.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawWorldCommand
{
    public readonly bool DrawAutomap;
    public readonly int Gametick;
    public readonly float GametickFraction;
    public readonly double AutomapScale;
    public readonly Vec2I AutomapOffset;
    public readonly IWorld World;
    public readonly Camera Camera;
    public readonly Entity ViewerEntity;

    public DrawWorldCommand(IWorld world, Camera camera, int gametick, float gametickFraction,
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

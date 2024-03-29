using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Shared;
using Helion.World;
using Helion.World.Entities;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawWorldCommand
{
    public readonly bool DrawAutomap;
    public readonly int Gametick;
    public readonly float GametickFraction;
    public readonly double AutomapScale;
    public readonly Vec2I AutomapOffset;
    public readonly IWorld World;
    public readonly OldCamera Camera;
    public readonly Entity ViewerEntity;

    public DrawWorldCommand(IWorld world, OldCamera camera, int gametick, float gametickFraction,
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

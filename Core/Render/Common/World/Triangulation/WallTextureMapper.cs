using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Textures;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;

namespace Helion.Render.Common.World.Triangulation;

/// <summary>
/// A helper to take texture geometry and turn it into UV coordinates.
/// </summary>
public static class WallTextureMapper
{
    public static Box2F OneSidedWallUV(Line line, Side side, float length, float spanZ, IRenderableTextureHandle textureHandle)
    {
        Vec2F uvInverse = textureHandle.UV.Sides.Inverse();
        Vec2F offsetUV = side.Offset.Float * uvInverse;
        if (side.ScrollData != null)
        {
            // TODO
            // offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.MiddlePosition, textureUVInverse);
        }
            
        float wallSpanU = length * uvInverse.U;
        float spanV = spanZ * uvInverse.V;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            bottomV = 1.0f + offsetUV.V;
            topV = bottomV - spanV;
        }
        else
        {
            topV = offsetUV.V;
            bottomV = offsetUV.V + spanV;
        }

        return new Box2F((leftU, topV), (rightU, bottomV));
    }
}
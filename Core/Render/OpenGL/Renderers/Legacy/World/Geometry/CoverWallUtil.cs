using Helion.World;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class CoverWallUtil
{
    readonly record struct Heights(float AddTop, float SubBottom);
    const int ProjectHeight = 8192;

    public static unsafe void SetCoverWallVertices(Side side, DynamicVertex[] vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (DynamicVertex* startVertex = &vertices[index])
        {
            DynamicVertex* v = startVertex;
            v->Z += heights.AddTop;
            v->PrevZ += heights.AddTop;
            v++;

            v->Z -= heights.SubBottom;
            v->PrevZ -= heights.SubBottom;
            v++;

            v->Z += heights.AddTop;
            v->PrevZ += heights.AddTop;
            v++;

            v->Z -= heights.SubBottom;
            v->PrevZ -= heights.SubBottom;
            v++;

            v->Z += heights.AddTop;
            v->PrevZ += heights.AddTop;
            v++;

            v->Z -= heights.SubBottom;
            v->PrevZ -= heights.SubBottom;
        }
    }

    public static unsafe void CopyCoverWallVertices(Side side, StaticVertex[] staticVertices, Span<DynamicVertex> vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (DynamicVertex* startVertex = &vertices[0])
        {
            DynamicVertex* v = startVertex;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.AddTop, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.SubBottom, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.AddTop, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.SubBottom, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.AddTop, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.SubBottom, v->U, v->V,
                v->SurfaceOptions, v->LightLevelAdd, 0);
        }
    }

    private static Heights GetProjectHeights(Side side, WallLocation location)
    {
        if (location == WallLocation.Middle3D)
            return new Heights(0, 0);

        // Treat two-sided lines that block rendering as one-sided cover to prevent sprites from bleeding through.
        if (side.PartnerSide == null || RenderBlock.IsBlocked(side.Line, side == side.Line.Front))
            return new Heights(ProjectHeight, ProjectHeight);

        // Do not add to upper portion of lower textures, or upper portion of lower textures
        // Adjust cover wall offsets to not block extra pixels from the the backside
        return location switch
        {
            WallLocation.Upper => new Heights(ProjectHeight, -(float)WorldStatic.LineVertexGapBottomZ),
            WallLocation.Lower => new Heights(-(float)WorldStatic.LineVertexGapTopZ, ProjectHeight),
            _ => new(ProjectHeight, ProjectHeight),
        };
    }
}

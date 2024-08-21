using Helion.Util.Container;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class CoverWallUtil
{
    readonly record struct Heights(int Add, int Sub);
    const int ProjectHeight = 8192;

    public static unsafe void SetCoverWallVertices(Side side, DynamicVertex[] vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (DynamicVertex* startVertex = &vertices[index])
        {
            DynamicVertex* v = startVertex;
            v->Z += heights.Add;
            v->PrevZ += heights.Add;
            v++;

            v->Z -= heights.Sub;
            v->PrevZ -= heights.Sub;
            v++;

            v->Z += heights.Add;
            v->PrevZ += heights.Add;
            v++;

            v->Z += heights.Add;
            v->PrevZ += heights.Add;
            v++;

            v->Z -= heights.Sub;
            v->PrevZ -= heights.Sub;
            v++;

            v->Z -= heights.Sub;
            v->PrevZ -= heights.Sub;
        }
    }

    public static unsafe void AddCoverWallVertices(Side side, DynamicArray<StaticVertex> staticVertices, DynamicVertex[] vertices, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        staticVertices.EnsureCapacity(staticVertices.Length + 6);
        int staticStartIndex = staticVertices.Length;
        fixed (DynamicVertex* startVertex = &vertices[0])
        {
            DynamicVertex* v = startVertex;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);

            staticVertices.SetLength(staticVertices.Length + 6);
        }
    }

    public static unsafe void CopyCoverWallVertices(Side side, StaticVertex[] staticVertices, DynamicVertex[] vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (DynamicVertex* startVertex = &vertices[0])
        {
            DynamicVertex* v = startVertex;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Options, v->LightLevelAdd, 0);
        }
    }

    private static Heights GetProjectHeights(Side side, WallLocation location)
    {
        // Treat two-sided lines that block rendering as one-sided cover to prevent sprites from bleeding through.
        if (side.PartnerSide == null || LineOpening.IsRenderingBlocked(side.Line))
            return new Heights(ProjectHeight, ProjectHeight);

        // Do not add to upper portion of lower textures, or upper portion of lower textures
        return new Heights
        (
            location == WallLocation.Lower ? 0 : ProjectHeight,
            location == WallLocation.Upper ? 0 : ProjectHeight
        );
    }
}

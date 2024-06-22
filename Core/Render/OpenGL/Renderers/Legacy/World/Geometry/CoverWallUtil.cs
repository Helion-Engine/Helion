using Helion.Util.Container;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public class CoverWallUtil
{
    readonly record struct Heights(int Add, int Sub);
    const int ProjectHeight = 8192;

    public static unsafe void SetCoverWallVertices(Side side, LegacyVertex[] vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (LegacyVertex* startVertex = &vertices[index])
        {
            LegacyVertex* v = startVertex;
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

    public static unsafe void AddCoverWallVertices(Side side, DynamicArray<StaticVertex> staticVertices, LegacyVertex[] vertices, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        staticVertices.EnsureCapacity(staticVertices.Length + 6);
        int staticStartIndex = staticVertices.Length;
        fixed (LegacyVertex* startVertex = &vertices[0])
        {
            LegacyVertex* v = startVertex;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices.Data[staticStartIndex++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);

            staticVertices.SetLength(staticVertices.Length + 6);
        }
    }

    public static unsafe void CopyCoverWallVertices(Side side, StaticVertex[] staticVertices, LegacyVertex[] vertices, int index, WallLocation location)
    {
        var heights = GetProjectHeights(side, location);
        fixed (LegacyVertex* startVertex = &vertices[0])
        {
            LegacyVertex* v = startVertex;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z + heights.Add, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
            v++;
            staticVertices[index++] = new StaticVertex(v->X, v->Y, v->Z - heights.Sub, v->U, v->V,
                v->Alpha, v->AddAlpha, v->LightLevelBufferIndex, v->LightLevelAdd);
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

using System;
using System.Runtime.CompilerServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.Common.Shared.World;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shared.World;

public static class WorldTriangulator
{
    public const double NoOverride = double.MaxValue;

    public static void HandleOneSided(Side side, Side offsetSide, SectorPlane floor, SectorPlane ceiling, in Vec2F textureUVInverse, ref WallVertices wall,
        double overrideFloor = NoOverride, double overrideCeiling = NoOverride, bool isFront = true, bool calculateUV = true)
    {
        Line line = side.Line;
        GetLeftRightVertices(isFront, line, out var left, out var right);

        double topZ, bottomZ, prevTopZ, prevBottomZ;
        if (overrideCeiling == NoOverride)
        {
            topZ = ceiling.Z + WorldStatic.LineVertexGapTopZ;
            prevTopZ = ceiling.PrevZ + WorldStatic.LineVertexGapTopZ;
        }
        else
        {
            topZ = overrideCeiling;
            prevTopZ = overrideCeiling;
        }

        if (overrideFloor == NoOverride)
        {
            bottomZ = floor.Z - WorldStatic.LineVertexGapBottomZ;
            prevBottomZ = floor.PrevZ - WorldStatic.LineVertexGapBottomZ;
        }
        else
        {
            bottomZ = overrideFloor;
            prevBottomZ = overrideFloor;
        }

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv;
        WallUV prevUV;

        if (calculateUV)
        {
            uv = CalculateOneSidedWallUV(line, side, offsetSide, length, textureUVInverse, spanZ, previous: false);
            prevUV = CalculateOneSidedWallUV(line, side, offsetSide, length, textureUVInverse, prevSpanZ, previous: true);
        }
        else
        {
            uv = default;
            prevUV = default;
        }

        wall.TopLeft.X = left.X;
        wall.TopLeft.Y = left.Y;
        wall.TopLeft.Z = (float)topZ;
        wall.TopLeft.PrevZ = (float)prevTopZ;
        wall.TopLeft.U = uv.TopLeft.X;
        wall.TopLeft.V = uv.TopLeft.Y;
        wall.TopLeft.PrevU = prevUV.TopLeft.X;
        wall.TopLeft.PrevV = prevUV.TopLeft.Y;

        wall.BottomRight.X = right.X;
        wall.BottomRight.Y = right.Y;
        wall.BottomRight.Z = (float)bottomZ;
        wall.BottomRight.PrevZ = (float)prevBottomZ;
        wall.BottomRight.U = uv.BottomRight.X;
        wall.BottomRight.V = uv.BottomRight.Y;
        wall.BottomRight.PrevU = prevUV.BottomRight.X;
        wall.BottomRight.PrevV = prevUV.BottomRight.Y;

        wall.PrevTopZ = (float)prevTopZ;
        wall.PrevBottomZ = (float)prevBottomZ;
    }

    public static void HandleTwoSidedLower(Side facingSide, SectorPlane topFlat, SectorPlane bottomFlat,
        in Vec2F textureUVInverse, bool isFrontSide, ref WallVertices wall, bool calculateUV = true)
    {
        Line line = facingSide.Line;
        GetLeftRightVertices(isFrontSide, line, out var left, out var right);

        double topZ = topFlat.Z + WorldStatic.LineVertexGapTopZ;
        double bottomZ = bottomFlat.Z - WorldStatic.LineVertexGapBottomZ;
        double prevTopZ = topFlat.PrevZ + WorldStatic.LineVertexGapTopZ;
        double prevBottomZ = bottomFlat.PrevZ - WorldStatic.LineVertexGapBottomZ;

        double length = line.GetLength();
        WallUV uv, prevUV;
        if (calculateUV)
        {
            uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ, previous: false);
            prevUV = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, prevTopZ, prevBottomZ, previous: true);
        }
        else
        {
            uv = default;
            prevUV = default;
        }

        wall.TopLeft.X = left.X;
        wall.TopLeft.Y = left.Y;
        wall.TopLeft.Z = (float)topZ;
        wall.TopLeft.PrevZ = (float)prevTopZ;
        wall.TopLeft.U = uv.TopLeft.X;
        wall.TopLeft.V = uv.TopLeft.Y;
        wall.TopLeft.PrevU = prevUV.TopLeft.X;
        wall.TopLeft.PrevV = prevUV.TopLeft.Y;

        wall.BottomRight.X = right.X;
        wall.BottomRight.Y = right.Y;
        wall.BottomRight.Z = (float)bottomZ;
        wall.BottomRight.PrevZ = (float)prevBottomZ;
        wall.BottomRight.U = uv.BottomRight.X;
        wall.BottomRight.V = uv.BottomRight.Y;
        wall.BottomRight.PrevU = prevUV.BottomRight.X;
        wall.BottomRight.PrevV = prevUV.BottomRight.Y;

        wall.PrevTopZ = (float)prevTopZ;
        wall.PrevBottomZ = (float)prevBottomZ;
    }

    public static void HandleTwoSidedMiddle(Side facingSide,
        in Dimension textureDimension, in Vec2F textureUVInverse, in MidTexOpening opening, in MidTexOpening prevOpening,
        bool isFrontSide, ref WallVertices wall, out bool nothingVisible, double offset = 0, double prevOffset = 0, 
        SectorPlanes clipPlanes = SectorPlanes.Floor | SectorPlanes.Ceiling, bool vertexGap = true, MidTexSpan? restrictSpan = null)
    {
        if (RenderBlock.IsBlocked(facingSide.Line, isFrontSide))
        {
            nothingVisible = true;
            return;
        }

        Line line = facingSide.Line;
        // Set offset according to the scroll Y offset. The doom renderer would push the entire texture up/down.
        if (facingSide.ScrollData != null)
        {
            offset += facingSide.ScrollData.Offset(WallLocation.Middle, ScrollOffsetType.Current).Y / facingSide.Middle.Scale.Y;
            prevOffset += facingSide.ScrollData.Offset(WallLocation.Middle, ScrollOffsetType.Previous).Y / facingSide.Middle.Scale.Y;
        }

        var drawSpan = CalculateMiddleDrawSpan(line, facingSide, opening, prevOpening, textureDimension, offset, prevOffset, clipPlanes, vertexGap, restrictSpan);
        if (drawSpan.NotVisible())
        {
            nothingVisible = true;
            return;
        }

        GetLeftRightVertices(isFrontSide, line, out var left, out var right);

        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan.TopZ, drawSpan.BottomZ, 
            drawSpan.VisibleTopZ, drawSpan.VisibleBottomZ, textureUVInverse, previous: false);
        WallUV prevUV = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan.PrevTopZ, drawSpan.PrevBottomZ, 
            drawSpan.PrevVisibleTopZ, drawSpan.PrevVisibleBottomZ, textureUVInverse, previous: true);

        wall.TopLeft.X = left.X;
        wall.TopLeft.Y = left.Y;
        wall.TopLeft.Z = (float)drawSpan.VisibleTopZ;
        wall.TopLeft.PrevZ = (float)drawSpan.PrevVisibleTopZ;
        wall.TopLeft.U = uv.TopLeft.X;
        wall.TopLeft.V = uv.TopLeft.Y;
        wall.TopLeft.PrevU = prevUV.TopLeft.X;
        wall.TopLeft.PrevV = prevUV.TopLeft.Y;

        wall.BottomRight.X = right.X;
        wall.BottomRight.Y = right.Y;
        wall.BottomRight.Z = (float)drawSpan.VisibleBottomZ;
        wall.BottomRight.PrevZ = (float)drawSpan.PrevVisibleBottomZ;
        wall.BottomRight.U = uv.BottomRight.X;
        wall.BottomRight.V = uv.BottomRight.Y;
        wall.BottomRight.PrevU = prevUV.BottomRight.X;
        wall.BottomRight.PrevV = prevUV.BottomRight.Y;

        wall.PrevTopZ = (float)drawSpan.PrevVisibleTopZ;
        wall.PrevBottomZ = (float)drawSpan.PrevVisibleBottomZ;
        nothingVisible = false;
    }

    public static void HandleTwoSidedUpper(Side facingSide, SectorPlane topPlane, SectorPlane bottomPlane, in Vec2F textureUVInverse,
        bool isFrontSide, ref WallVertices wall, double overrideTopZ = NoOverride, bool calculateUV = true)
    {
        Line line = facingSide.Line;
        GetLeftRightVertices(isFrontSide, line, out var left, out var right);

        double topZ, prevTopZ;
        if (overrideTopZ == NoOverride)
        {
            topZ = topPlane.Z + WorldStatic.LineVertexGapTopZ;
            prevTopZ = topPlane.PrevZ + WorldStatic.LineVertexGapTopZ;
        }
        else
        {
            topZ = overrideTopZ;
            prevTopZ = overrideTopZ;
        }

        var bottomZ = bottomPlane.Z - WorldStatic.LineVertexGapBottomZ;
        var prevBottomZ = bottomPlane.PrevZ - WorldStatic.LineVertexGapBottomZ;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv;
        WallUV prevUV;

        if (calculateUV)
        {
            uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ, previous: false);
            prevUV = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, prevSpanZ, previous: true);
        }
        else
        {
            uv = default;
            prevUV = default;
        }

        wall.TopLeft.X = left.X;
        wall.TopLeft.Y = left.Y;
        wall.TopLeft.Z = (float)topZ;
        wall.TopLeft.PrevZ = (float)prevTopZ;
        wall.TopLeft.U = uv.TopLeft.X;
        wall.TopLeft.V = uv.TopLeft.Y;
        wall.TopLeft.PrevU = prevUV.TopLeft.X;
        wall.TopLeft.PrevV = prevUV.TopLeft.Y;

        wall.BottomRight.X = right.X;
        wall.BottomRight.Y = right.Y;
        wall.BottomRight.Z = (float)bottomZ;
        wall.BottomRight.PrevZ = (float)prevBottomZ;
        wall.BottomRight.U = uv.BottomRight.X;
        wall.BottomRight.V = uv.BottomRight.Y;
        wall.BottomRight.PrevU = prevUV.BottomRight.X;
        wall.BottomRight.PrevV = prevUV.BottomRight.Y;

        wall.PrevTopZ = (float)prevTopZ;
        wall.PrevBottomZ = (float)prevBottomZ;
    }

    public static unsafe void HandleSubsector(CompactBspTree bspTree, Subsector subsector, SectorPlane sectorPlane, bool floor, in Vec2F textureVector,
        DynamicArray<TriangulatedWorldVertex> verticesToPopulate, double overrideZ = int.MaxValue)
    {
        Precondition(subsector.SegCount >= 3, "Cannot render subsector when it's degenerate (should have 3+ edges)");

        var edges = bspTree.Segments.Data;
        int index = subsector.SegIndex;
        int length = index + subsector.SegCount;
        verticesToPopulate.EnsureCapacity(subsector.SegCount);
        verticesToPopulate.SetLength(subsector.SegCount);

        double z, prevZ;
        if (overrideZ == int.MaxValue)
        {
            z = sectorPlane.Z;
            prevZ = sectorPlane.PrevZ;
        }
        else
        {
            z = overrideZ;
            prevZ = overrideZ;
        }

        Vec2D uv = default;
        Vec2D prevUV = default;
        ref var offset = ref sectorPlane.RenderOffsets.Offset;
        ref var lastOffset = ref sectorPlane.RenderOffsets.LastOffset;

        int edgeIndex = index;
        int count = length - index;
        int add = 1;

        if (floor)
        {
            edgeIndex = length - 1;
            add = -1;
        }

        fixed (TriangulatedWorldVertex* startVertex = &verticesToPopulate.Data[0])
        {
            TriangulatedWorldVertex* worldVertex = startVertex;
            for (int i = 0; i < count; i++)
            {
                var vertex = edges[edgeIndex].Start;
                if (sectorPlane.RenderOffsets.Rotate == 0)
                {
                    uv.X = vertex.X / textureVector.X;
                    uv.Y = -(vertex.Y / textureVector.Y);
                    prevUV = uv;

                    uv.X += offset.X / textureVector.X;
                    uv.Y += offset.Y / textureVector.Y;
                    prevUV.X += lastOffset.X / textureVector.X;
                    prevUV.Y += lastOffset.Y / textureVector.Y;
                }
                else
                {
                    var uvVertex = vertex;
                    if (sectorPlane.FlatTransformMethod == FlatTransformMethod.OffsetThenRotate)
                    {
                        uvVertex.X += offset.X;
                        uvVertex.Y -= offset.Y;
                        uvVertex = uvVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
                    }
                    else
                    {
                        uvVertex = uvVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
                        uvVertex.X += offset.X;
                        uvVertex.Y -= offset.Y;
                    }
                    uv.X = uvVertex.X / textureVector.X;
                    uv.Y = -(uvVertex.Y / textureVector.Y);

                    var prevUVVertex = vertex;
                    if (sectorPlane.FlatTransformMethod == FlatTransformMethod.OffsetThenRotate)
                    {
                        prevUVVertex.X += lastOffset.X;
                        prevUVVertex.Y -= lastOffset.Y;
                        prevUVVertex = prevUVVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
                    }
                    else
                    {
                        prevUVVertex = prevUVVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
                        prevUVVertex.X += lastOffset.X;
                        prevUVVertex.Y -= lastOffset.Y;
                    }
                    prevUV.X = uvVertex.X / textureVector.X;
                    prevUV.Y = -(uvVertex.Y / textureVector.Y);
                }

                if (sectorPlane.RenderOffsets.Scale.X != 0)
                {
                    uv.X *= sectorPlane.RenderOffsets.Scale.X;
                    prevUV.X *= sectorPlane.RenderOffsets.Scale.X;
                }

                if (sectorPlane.RenderOffsets.Scale.Y != 0)
                {
                    uv.Y *= sectorPlane.RenderOffsets.Scale.Y;
                    prevUV.Y *= sectorPlane.RenderOffsets.Scale.Y;
                }

                worldVertex->X = (float)vertex.X;
                worldVertex->Y = (float)vertex.Y;
                worldVertex->Z = (float)z;
                worldVertex->PrevZ = (float)prevZ;
                worldVertex->U = (float)uv.X;
                worldVertex->V = (float)uv.Y;
                worldVertex->PrevU = (float)prevUV.X;
                worldVertex->PrevV = (float)prevUV.Y;
                worldVertex++;
                edgeIndex += add;
            }
        }
    }

    private static MiddleDrawSpan CalculateMiddleDrawSpan(Line line, Side facingSide, in MidTexOpening opening, in MidTexOpening prevOpening, 
        in Dimension textureDimension, double offset, double prevOffset, SectorPlanes clipPlanes, bool vertexGap, MidTexSpan? restrictSpan)
    {
        if (facingSide.Flags.WrapMidTex)
            return new(opening.BottomZ, opening.TopZ, opening.BottomZ, opening.TopZ, prevOpening.BottomZ, prevOpening.TopZ, prevOpening.BottomZ, prevOpening.TopZ);

        var textureHeight = textureDimension.Height / Math.Abs(facingSide.Middle.Scale.Y);
        // Default rendering top down. Unpegged.Lower renders bottom up
        // TopZ is the top of the texture to render and BottomZ is the bottom
        // MaxTopZ and MinBottomZ are the min/max areas to render with Y offset. (e.g. a middle texture can render over a missing lower texture)
        double topZ = opening.TopZ;
        double bottomZ = topZ - textureHeight;
        double prevTopZ = prevOpening.TopZ;
        double prevBottomZ = prevTopZ - textureHeight;

        if (line.Flags.Unpegged.Lower)
        {
            bottomZ = opening.BottomZ;
            topZ = bottomZ + textureHeight;
            prevBottomZ = prevOpening.BottomZ;
            prevTopZ = prevBottomZ + textureHeight;
        }

        var offsetY = (facingSide.Offset.Y + facingSide.Middle.Offset.Y) / facingSide.Middle.Scale.Y;
        topZ += offsetY + offset;
        bottomZ += offsetY + offset;
        prevTopZ += offsetY + prevOffset;
        prevBottomZ += offsetY + prevOffset;

        // Check clipping to min/max floor/ceiling. Typically ignored for skies or mid-texture hacks.
        var visibleTopZ = (clipPlanes & SectorPlanes.Ceiling) == 0 ? topZ : Math.Min(topZ, opening.MaxTopZ);
        var visiblePrevTopZ = (clipPlanes & SectorPlanes.Ceiling) == 0 ? prevTopZ : Math.Min(prevTopZ, prevOpening.MaxTopZ);

        var visibleBottomZ = (clipPlanes & SectorPlanes.Floor) == 0 ? bottomZ : Math.Max(bottomZ, opening.MinBottomZ);
        var visiblePrevBottomZ = (clipPlanes & SectorPlanes.Floor) == 0 ? prevBottomZ : Math.Max(prevBottomZ, prevOpening.MinBottomZ);

        if (restrictSpan.HasValue)
        {
            if (visibleTopZ > restrictSpan.Value.TopZ)
                visibleTopZ = restrictSpan.Value.TopZ;
            if (visiblePrevTopZ > restrictSpan.Value.PrevTopZ)
                visiblePrevTopZ = restrictSpan.Value.PrevTopZ;
            if (visibleBottomZ < restrictSpan.Value.BottomZ)
                visibleBottomZ = restrictSpan.Value.BottomZ;
            if (visiblePrevBottomZ < restrictSpan.Value.PrevBottomZ)
                visiblePrevBottomZ = restrictSpan.Value.PrevBottomZ;
        }

        if (vertexGap)
        {
            return new(bottomZ - WorldStatic.LineVertexGapBottomZ, topZ + WorldStatic.LineVertexGapTopZ, visibleBottomZ - WorldStatic.LineVertexGapBottomZ, visibleTopZ + WorldStatic.LineVertexGapTopZ,
                prevBottomZ - WorldStatic.LineVertexGapBottomZ, prevTopZ + WorldStatic.LineVertexGapTopZ, visiblePrevBottomZ - WorldStatic.LineVertexGapBottomZ, visiblePrevTopZ + WorldStatic.LineVertexGapTopZ);
        }

        return new(bottomZ, topZ , visibleBottomZ, visibleTopZ ,
            prevBottomZ, prevTopZ, visiblePrevBottomZ, visiblePrevTopZ);
    }

    public static WallUV CalculateOneSidedWallUV(Line line, Side side, Side offsetSide, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        var absScaleX = Math.Abs(side.Middle.Scale.X);
        var absScaleY = Math.Abs(side.Middle.Scale.Y);
        var offsetU = (offsetSide.Offset.X + offsetSide.Middle.Offset.X) * textureUVInverse.X / absScaleX + (WorldStatic.LineVertexOffset * textureUVInverse.X);
        var offsetV = (offsetSide.Offset.Y + offsetSide.Middle.Offset.Y) * textureUVInverse.Y / absScaleY + (WorldStatic.LineVertexOffset * textureUVInverse.Y);
        if (offsetSide.ScrollData != null)
        {
            offsetU += (float)offsetSide.ScrollData.Offset(WallLocation.Middle, previous).X * textureUVInverse.U / absScaleX;
            offsetV += (float)offsetSide.ScrollData.Offset(WallLocation.Middle, previous).Y * textureUVInverse.V / absScaleY;
        }

        float wallSpanU = (float)length * textureUVInverse.U;
        float spanV = (float)spanZ * textureUVInverse.V;

        float leftU = offsetU;
        float rightU = offsetU + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            bottomV = (1.0f / absScaleY) + offsetV;
            topV = bottomV - spanV;
        }
        else
        {
            topV = offsetV;
            bottomV = offsetV + spanV;
        }

        return new WallUV(new(leftU * side.Middle.Scale.X, topV * side.Middle.Scale.Y), new(rightU * side.Middle.Scale.X, bottomV * side.Middle.Scale.Y));
    }

    public static WallUV CalculateTwoSidedLowerWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double topZ, double bottomZ, bool previous)
    {
        var absScaleX = Math.Abs(side.Lower.Scale.X);
        var absScaleY = Math.Abs(side.Lower.Scale.Y);
        var offsetU = (side.Offset.X + side.Lower.Offset.X) * textureUVInverse.X / absScaleX + (WorldStatic.LineVertexOffset * textureUVInverse.X);
        var offsetV = (side.Offset.Y + side.Lower.Offset.Y) * textureUVInverse.Y / absScaleY + (WorldStatic.LineVertexOffset * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            offsetU += (float)side.ScrollData.Offset(WallLocation.Lower, previous).X * textureUVInverse.X / absScaleX;
            offsetV += (float)side.ScrollData.Offset(WallLocation.Lower, previous).Y * textureUVInverse.Y / absScaleY;
        }       

        float wallSpanU = (float)length * textureUVInverse.X;
        float leftU = offsetU;
        float rightU = offsetU + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            double ceilZ = previous ? side.Sector.Ceiling.PrevZ : side.Sector.Ceiling.Z;
            float topDistFromCeil = (float)(ceilZ - topZ);
            float bottomDistFromCeil = (float)(ceilZ - bottomZ);

            topV = offsetV + (topDistFromCeil * textureUVInverse.Y);
            bottomV = offsetV + (bottomDistFromCeil * textureUVInverse.Y);
        }
        else
        {
            topV = offsetV;
            bottomV = offsetV + (float)(topZ - bottomZ) * textureUVInverse.Y;
        }

        return new WallUV(new(leftU * side.Lower.Scale.X, topV * side.Lower.Scale.Y), new(rightU * side.Lower.Scale.X, bottomV * side.Lower.Scale.Y));
    }

    private static WallUV CalculateTwoSidedMiddleWallUV(Side side, double length, double topZ, double bottomZ, 
        double visibleTopZ, double visibleBottomZ, in Vec2F textureUVInverse, bool previous)
    {
        if (side.Flags.WrapMidTex)
            return CalculateOneSidedWallUV(side.Line, side, side, length, textureUVInverse, visibleTopZ - visibleBottomZ, previous);

        var absScaleX = Math.Abs(side.Middle.Scale.X);
        var offsetU = (side.Offset.X + side.Middle.Offset.X) * textureUVInverse.X / absScaleX + (WorldStatic.LineVertexOffset * textureUVInverse.X);
        if (side.ScrollData != null)
            offsetU += (float)side.ScrollData.Offset(WallLocation.Middle, previous).X * textureUVInverse.X / absScaleX;

        float wallSpanU = (float)length * textureUVInverse.X;
        float leftU = offsetU;
        float rightU = offsetU + wallSpanU;

        // Since we only draw one of the texture, all we need to do is find
        // out where the texture is clamped by and find that value between
        // [0.0, 1.0]. For example if a texture height of 10 only has two
        // pixels available between 6 -> 7 for the line opening, then
        // the top V would be 0.6 and the bottom V would be 0.7.
        double textureHeight = topZ - bottomZ;
        float topV = 1.0f - (float)((visibleTopZ - bottomZ) / textureHeight);
        float bottomV = 1.0f - (float)((visibleBottomZ - bottomZ) / textureHeight);

        if (side.Middle.Scale.Y < 0)
        {
            topV = 1.0f - topV;
            bottomV = 1.0f - bottomV;
        }

        return new WallUV(new Vec2F(leftU * side.Middle.Scale.X, topV), new Vec2F(rightU * side.Middle.Scale.X, bottomV));
    }

    public static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        var absScaleX = Math.Abs(side.Upper.Scale.X);
        var absScaleY = Math.Abs(side.Upper.Scale.Y);
        var offsetU = (side.Offset.X + side.Upper.Offset.X) * textureUVInverse.X / absScaleX + (WorldStatic.LineVertexOffset * textureUVInverse.X);
        var offsetV = (side.Offset.Y + side.Upper.Offset.Y) * textureUVInverse.Y / absScaleY + (WorldStatic.LineVertexOffset * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            offsetU += (float)side.ScrollData.Offset(WallLocation.Upper, previous).X * textureUVInverse.U / absScaleX;
            offsetV += (float)side.ScrollData.Offset(WallLocation.Upper, previous).Y * textureUVInverse.V / absScaleY;
        }

        float wallSpanU = (float)length * textureUVInverse.U;
        float spanV = (float)spanZ * textureUVInverse.V;

        float leftU = offsetU;
        float rightU = offsetU + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Upper)
        {
            topV = offsetV;
            bottomV = topV + spanV;
        }
        else
        {
            bottomV = 1.0f + offsetV;
            topV = bottomV - spanV;
        }

        return new WallUV(new(leftU * side.Upper.Scale.X, topV * side.Upper.Scale.Y), new(rightU * side.Upper.Scale.X, bottomV * side.Upper.Scale.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetLeftRightVertices(bool isFront, Line line, out Vec2F left, out Vec2F right)
    {
        if (isFront)
        {
            left = new((float)line.RenderSegStart.X, (float)line.RenderSegStart.Y);
            right = new((float)line.RenderSegEnd.X, (float)line.RenderSegEnd.Y);
        }
        else
        {
            left = new((float)line.RenderSegEnd.X, (float)line.RenderSegEnd.Y);
            right = new((float)line.RenderSegStart.X, (float)line.RenderSegStart.Y);
        }
    }

}

using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Shared.World;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shared.World;

public static class WorldTriangulator
{
    public const double NoOverride = double.MaxValue;

    public static void HandleOneSided(Side side, SectorPlane floor, SectorPlane ceiling, in Vec2F textureUVInverse, ref WallVertices wall,
        double overrideFloor = NoOverride, double overrideCeiling = NoOverride, bool isFront = true)
    {
        Line line = side.Line;

        Vec2F left = isFront ? new((float)line.Segment.Start.X, (float)line.Segment.Start.Y) : new((float)line.Segment.End.X, (float)line.Segment.End.Y);
        Vec2F right = isFront ? new((float)line.Segment.End.X, (float)line.Segment.End.Y) : new((float)line.Segment.Start.X, (float)line.Segment.Start.Y);
        double topZ = overrideCeiling == NoOverride ? ceiling.Z : overrideCeiling;
        double bottomZ = overrideFloor == NoOverride ? floor.Z : overrideFloor;
        double prevTopZ = overrideCeiling == NoOverride ? ceiling.PrevZ : overrideCeiling;
        double prevBottomZ = overrideFloor == NoOverride ? floor.PrevZ : overrideFloor;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv = CalculateOneSidedWallUV(line, side, length, textureUVInverse, spanZ, previous: false);
        WallUV prevUV = CalculateOneSidedWallUV(line, side, length, textureUVInverse, prevSpanZ, previous: true);

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
        in Vec2F textureUVInverse, bool isFrontSide, ref WallVertices wall)
    {
        Line line = facingSide.Line;

        Vec2F left = isFrontSide ? new((float)line.Segment.Start.X, (float)line.Segment.Start.Y) : new((float)line.Segment.End.X, (float)line.Segment.End.Y);
        Vec2F right = isFrontSide ? new((float)line.Segment.End.X, (float)line.Segment.End.Y) : new((float)line.Segment.Start.X, (float)line.Segment.Start.Y);
        double topZ = topFlat.Z;
        double bottomZ = bottomFlat.Z;
        double prevTopZ = topFlat.PrevZ;
        double prevBottomZ = bottomFlat.PrevZ;

        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ, previous: false);
        WallUV prevUV = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, prevTopZ, prevBottomZ, previous: true);

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
        SectorPlanes clipPlanes = SectorPlanes.Floor | SectorPlanes.Ceiling)
    {
        if (LineOpening.IsRenderingBlocked(facingSide.Line))
        {
            nothingVisible = true;
            return;
        }

        Line line = facingSide.Line;
        // Set offset according to the scroll Y offset. The doom renderer would push the entire texture up/down.
        if (facingSide.ScrollData != null)
        {
            offset += facingSide.ScrollData.OffsetMiddle.Y;
            prevOffset += facingSide.ScrollData.LastOffsetMiddle.Y;
        }
        MiddleDrawSpan drawSpan = CalculateMiddleDrawSpan(line, facingSide, opening, prevOpening, textureDimension, offset, prevOffset, clipPlanes);
        if (drawSpan.NotVisible())
        {
            nothingVisible = true;
            return;
        }

        Vec2F left = isFrontSide ? new((float)line.Segment.Start.X, (float)line.Segment.Start.Y) : new((float)line.Segment.End.X, (float)line.Segment.End.Y);
        Vec2F right = isFrontSide ? new((float)line.Segment.End.X, (float)line.Segment.End.Y) : new((float)line.Segment.Start.X, (float)line.Segment.Start.Y);
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
        bool isFrontSide, ref WallVertices wall, double overrideTopZ = NoOverride)
    {
        Line line = facingSide.Line;

        Vec2F left = isFrontSide ? new((float)line.Segment.Start.X, (float)line.Segment.Start.Y) : new((float)line.Segment.End.X, (float)line.Segment.End.Y);
        Vec2F right = isFrontSide ? new((float)line.Segment.End.X, (float)line.Segment.End.Y) : new((float)line.Segment.Start.X, (float)line.Segment.Start.Y);
        double topZ = overrideTopZ == NoOverride ? topPlane.Z : overrideTopZ;
        double bottomZ = bottomPlane.Z;
        double prevTopZ = overrideTopZ == NoOverride ? topPlane.PrevZ : overrideTopZ;
        double prevBottomZ = bottomPlane.PrevZ;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ, previous: false);
        WallUV prevUV = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, prevSpanZ, previous: true);
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

    public static unsafe void HandleSubsector(CompactBspTree bspTree, Subsector subsector, SectorPlane sectorPlane, in Vec2F textureVector,
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
        Vec2D offset = sectorPlane.RenderOffsets.Offset;
        Vec2D lastOffset = sectorPlane.RenderOffsets.LastOffset;

        int edgeIndex = index;
        int count = length - index;
        int add = 1;

        if (sectorPlane.Facing == SectorPlaneFace.Floor)
        {
            edgeIndex = length - 1;
            add = -1;
        }

        fixed (TriangulatedWorldVertex* startVertex = &verticesToPopulate.Data[0])
        {
            TriangulatedWorldVertex* worldVertex = startVertex;
            for (int i = 0; i < count; i++)
            {
                Vec2D vertex = edges[edgeIndex].Start;
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
                    uvVertex.X += offset.X;
                    uvVertex.Y -= offset.Y;
                    uvVertex = uvVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
                    uv.X = uvVertex.X / textureVector.X;
                    uv.Y = -(uvVertex.Y / textureVector.Y);

                    var prevUVVertex = vertex;
                    prevUVVertex.X += lastOffset.X;
                    prevUVVertex.Y -= lastOffset.Y;
                    prevUVVertex = prevUVVertex.Rotate(sectorPlane.RenderOffsets.Rotate);
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
        in Dimension textureDimension, double offset, double prevOffset, SectorPlanes clipPlanes)
    {
        // Default rendering top down. Unpegged.Lower renders bottom up
        // TopZ is the top of the texture to render and BottomZ is the bottom
        // MaxTopZ and MinBottomZ are the min/max areas to render with Y offset. (e.g. a middle texture can render over a missing lower texture)
        double topZ = opening.TopZ;
        double bottomZ = topZ - textureDimension.Height;
        double prevTopZ = prevOpening.TopZ;
        double prevBottomZ = prevTopZ - textureDimension.Height;
        if (line.Flags.Unpegged.Lower)
        {
            bottomZ = opening.BottomZ;
            topZ = bottomZ + textureDimension.Height;
            prevBottomZ = prevOpening.BottomZ;
            prevTopZ = prevBottomZ + textureDimension.Height;
        }

        topZ += facingSide.OffsetMiddle.Y + offset;
        bottomZ += facingSide.OffsetMiddle.Y + offset;
        prevTopZ += facingSide.OffsetMiddle.Y + prevOffset;
        prevBottomZ += facingSide.OffsetMiddle.Y + prevOffset;

        // Check clipping to min/max floor/ceiling. Typically ignored for skies or mid-texture hacks.
        var visibleTopZ = (clipPlanes & SectorPlanes.Ceiling) == 0 ? topZ : Math.Min(topZ, opening.MaxTopZ);
        var visiblePrevTopZ = (clipPlanes & SectorPlanes.Ceiling) == 0 ? prevTopZ : Math.Min(prevTopZ, prevOpening.MaxTopZ);

        var visibleBottomZ = (clipPlanes & SectorPlanes.Floor) == 0 ? bottomZ : Math.Max(bottomZ, opening.MinBottomZ);
        var visiblePrevBottomZ = (clipPlanes & SectorPlanes.Floor) == 0 ? prevBottomZ : Math.Max(prevBottomZ, prevOpening.MinBottomZ);

        return new MiddleDrawSpan(bottomZ, topZ, visibleBottomZ, visibleTopZ, prevBottomZ, prevTopZ, visiblePrevBottomZ, visiblePrevTopZ);
    }

    public static WallUV CalculateOneSidedWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        Vec2F offsetUV = new(side.OffsetMiddle.X * textureUVInverse.X, side.OffsetMiddle.Y * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            if (previous)
            {
                offsetUV.X += (float)side.ScrollData.LastOffsetMiddle.X * textureUVInverse.U;
                offsetUV.Y += (float)side.ScrollData.LastOffsetMiddle.Y * textureUVInverse.V;
            }
            else
            {
                offsetUV.X += (float)side.ScrollData.LastOffsetMiddle.X * textureUVInverse.U;
                offsetUV.Y += (float)side.ScrollData.LastOffsetMiddle.Y * textureUVInverse.V;
            }
        }

        float wallSpanU = (float)length * textureUVInverse.U;
        float spanV = (float)spanZ * textureUVInverse.V;

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

        return new WallUV(new(leftU * side.ScaleMiddle.X, topV * side.ScaleMiddle.Y), new(rightU * side.ScaleMiddle.X, bottomV * side.ScaleMiddle.Y));
    }

    public static WallUV CalculateTwoSidedLowerWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double topZ, double bottomZ, bool previous)
    {
        Vec2F offsetUV = new(side.OffsetLower.X * textureUVInverse.X, side.OffsetLower.Y * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            if (previous)
            {
                offsetUV.X += (float)side.ScrollData.LastOffsetLower.X * textureUVInverse.X;
                offsetUV.Y += (float)side.ScrollData.LastOffsetLower.Y * textureUVInverse.Y;
            }
            else
            {
                offsetUV.X += (float)side.ScrollData.OffsetLower.X * textureUVInverse.X;
                offsetUV.Y += (float)side.ScrollData.OffsetLower.Y * textureUVInverse.Y;
            }
        }       

        float wallSpanU = (float)length * textureUVInverse.X;
        float leftU = offsetUV.X;
        float rightU = offsetUV.X + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            double ceilZ = previous ? side.Sector.Ceiling.PrevZ : side.Sector.Ceiling.Z;
            float topDistFromCeil = (float)(ceilZ - topZ);
            float bottomDistFromCeil = (float)(ceilZ - bottomZ);

            topV = offsetUV.Y + (topDistFromCeil * textureUVInverse.Y);
            bottomV = offsetUV.Y + (bottomDistFromCeil * textureUVInverse.Y);
        }
        else
        {
            topV = offsetUV.Y;
            bottomV = offsetUV.Y + (float)(topZ - bottomZ) * textureUVInverse.Y;
        }

        return new WallUV(new(leftU * side.ScaleLower.X, topV * side.ScaleLower.Y), new(rightU * side.ScaleLower.X, bottomV * side.ScaleLower.Y));
    }

    private static WallUV CalculateTwoSidedMiddleWallUV(Side side, double length, double topZ, double bottomZ, 
        double visibleTopZ, double visibleBottomZ, in Vec2F textureUVInverse, bool previous)
    {
        Vec2F offsetUV = new(side.OffsetMiddle.X * textureUVInverse.X, side.OffsetMiddle.Y * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            if (previous)
            {
                offsetUV.X += (float)side.ScrollData.LastOffsetMiddle.X * textureUVInverse.X;
                offsetUV.Y += (float)side.ScrollData.LastOffsetMiddle.Y * textureUVInverse.Y;
            }
            else
            {
                offsetUV.X += (float)side.ScrollData.OffsetMiddle.X * textureUVInverse.X;
                offsetUV.Y += (float)side.ScrollData.OffsetMiddle.Y * textureUVInverse.Y;
            }
        }

        float wallSpanU = (float)length * textureUVInverse.X;
        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;

        // Since we only draw one of the texture, all we need to do is find
        // out where the texture is clamped by and find that value between
        // [0.0, 1.0]. For example if a texture height of 10 only has two
        // pixels available between 6 -> 7 for the line opening, then
        // the top V would be 0.6 and the bottom V would be 0.7.
        double textureHeight = topZ - bottomZ;
        float topV = 1.0f - (float)((visibleTopZ - bottomZ) / textureHeight);
        float bottomV = 1.0f - (float)((visibleBottomZ - bottomZ) / textureHeight);

        return new WallUV(new Vec2F(leftU * side.ScaleMiddle.X, topV * side.ScaleMiddle.Y), new Vec2F(rightU * side.ScaleMiddle.X, bottomV * side.ScaleMiddle.Y));
    }

    public static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        Vec2F offsetUV = new(side.OffsetUpper.X * textureUVInverse.X, side.OffsetUpper.Y * textureUVInverse.Y);
        if (side.ScrollData != null)
        {
            if (previous)
            {
                offsetUV.X += (float)side.ScrollData.LastOffsetUpper.X * textureUVInverse.U;
                offsetUV.Y += (float)side.ScrollData.LastOffsetUpper.Y * textureUVInverse.V;
            }
            else
            {
                offsetUV.X += (float)side.ScrollData.OffsetUpper.X * textureUVInverse.U;
                offsetUV.Y += (float)side.ScrollData.OffsetUpper.Y * textureUVInverse.V;
            }
        }
        float wallSpanU = (float)length * textureUVInverse.U;
        float spanV = (float)spanZ * textureUVInverse.V;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Upper)
        {
            topV = offsetUV.V;
            bottomV = topV + spanV;
        }
        else
        {
            bottomV = 1.0f + offsetUV.V;
            topV = bottomV - spanV;
        }

        return new WallUV(new(leftU * side.ScaleUpper.X, topV * side.ScaleUpper.Y), new(rightU * side.ScaleUpper.Y, bottomV * side.ScaleUpper.Y));
    }
}

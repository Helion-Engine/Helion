using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using SixLabors.ImageSharp.Processing;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shared.World;

public static class WorldTriangulator
{
    public const double NoOverride = double.MaxValue;

    public static WallVertices HandleOneSided(Side side, SectorPlane floor, SectorPlane ceiling, in Vec2F textureUVInverse,
        double overrideFloor = NoOverride, double overrideCeiling = NoOverride, bool isFront = true)
    {
        Line line = side.Line;

        Vec2D left = isFront ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFront ? line.Segment.End : line.Segment.Start;
        double topZ = overrideCeiling == NoOverride ? ceiling.Z : overrideCeiling;
        double bottomZ = overrideFloor == NoOverride ? floor.Z : overrideFloor;
        double prevTopZ = overrideCeiling == NoOverride ? ceiling.PrevZ : overrideCeiling;
        double prevBottomZ = overrideFloor == NoOverride ? floor.PrevZ : overrideFloor;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv = CalculateOneSidedWallUV(line, side, length, textureUVInverse, spanZ, previous: false);
        WallUV prevUV = CalculateOneSidedWallUV(line, side, length, textureUVInverse, prevSpanZ, previous: true);

        TriangulatedWorldVertex topLeft = new(left.X, left.Y, topZ, prevTopZ, 
            uv.TopLeft.X, uv.TopLeft.Y, prevUV.TopLeft.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex topRight = new(right.X, right.Y, topZ, prevTopZ,
            uv.BottomRight.X, uv.TopLeft.Y, prevUV.BottomRight.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new(left.X, left.Y, bottomZ, prevBottomZ, 
            uv.TopLeft.X, uv.BottomRight.Y, prevUV.TopLeft.X, prevUV.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new(right.X, right.Y, bottomZ, prevBottomZ, 
            uv.BottomRight.X, uv.BottomRight.Y, prevUV.BottomRight.X, prevUV.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, prevTopZ, prevBottomZ);
    }

    public static WallVertices HandleTwoSidedLower(Side facingSide, SectorPlane topFlat, SectorPlane bottomFlat,
        in Vec2F textureUVInverse, bool isFrontSide)
    {
        Line line = facingSide.Line;

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double topZ = topFlat.Z;
        double bottomZ = bottomFlat.Z;
        double prevTopZ = topFlat.PrevZ;
        double prevBottomZ = bottomFlat.PrevZ;

        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ, previous: false);
        WallUV prevUV = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, prevTopZ, prevBottomZ, previous: true);

        TriangulatedWorldVertex topLeft = new(left.X, left.Y, topZ, prevTopZ, 
            uv.TopLeft.X, uv.TopLeft.Y, prevUV.TopLeft.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex topRight = new(right.X, right.Y, topZ, prevTopZ, 
            uv.BottomRight.X, uv.TopLeft.Y, prevUV.BottomRight.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new(left.X, left.Y, bottomZ, prevBottomZ, 
            uv.TopLeft.X, uv.BottomRight.Y, prevUV.TopLeft.X, prevUV.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new(right.X, right.Y, bottomZ, prevBottomZ, 
            uv.BottomRight.X, uv.BottomRight.Y, prevUV.BottomRight.X, prevUV.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, prevTopZ, prevBottomZ);
    }

    public static WallVertices HandleTwoSidedMiddle(Side facingSide,
        in Dimension textureDimension, in Vec2F textureUVInverse, double bottomOpeningZ, double topOpeningZ, double prevBottomZ, double prevTopZ,
        bool isFrontSide, out bool nothingVisible, double offset = 0, double prevOffset = 0)
    {
        if (LineOpening.IsRenderingBlocked(facingSide.Line))
        {
            nothingVisible = true;
            return default;
        }

        Line line = facingSide.Line;
        MiddleDrawSpan drawSpan = CalculateMiddleDrawSpan(line, facingSide, bottomOpeningZ, topOpeningZ, prevBottomZ, prevTopZ, textureDimension, offset, prevOffset);
        if (drawSpan.NotVisible())
        {
            nothingVisible = true;
            return default;
        }

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan.TopZ, drawSpan.BottomZ, 
            drawSpan.VisibleTopZ, drawSpan.VisibleBottomZ, textureUVInverse, previous: false);
        WallUV prevUV = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan.PrevTopZ, drawSpan.PrevBottomZ, 
            drawSpan.PrevVisibleTopZ, drawSpan.PrevVisibleBottomZ, textureUVInverse, previous: true);

        TriangulatedWorldVertex topLeft = new(left.X, left.Y, drawSpan.VisibleTopZ, drawSpan.PrevVisibleTopZ, 
            uv.TopLeft.X, uv.TopLeft.Y, prevUV.TopLeft.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex topRight = new(right.X, right.Y, drawSpan.VisibleTopZ, drawSpan.PrevVisibleTopZ, 
            uv.BottomRight.X, uv.TopLeft.Y, prevUV.BottomRight.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new(left.X, left.Y, drawSpan.VisibleBottomZ, drawSpan.PrevVisibleBottomZ, 
            uv.TopLeft.X, uv.BottomRight.Y, prevUV.TopLeft.X, prevUV.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new(right.X, right.Y, drawSpan.VisibleBottomZ, drawSpan.PrevVisibleBottomZ, 
            uv.BottomRight.X, uv.BottomRight.Y, prevUV.BottomRight.X, prevUV.BottomRight.Y);

        nothingVisible = false;
        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, drawSpan.PrevVisibleTopZ, drawSpan.PrevVisibleBottomZ);
    }

    public static WallVertices HandleTwoSidedUpper(Side facingSide, SectorPlane topPlane, SectorPlane bottomPlane, in Vec2F textureUVInverse,
        bool isFrontSide, double overrideTopZ = NoOverride)
    {
        Line line = facingSide.Line;

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double topZ = overrideTopZ == NoOverride ? topPlane.Z : overrideTopZ;
        double bottomZ = bottomPlane.Z;
        double prevTopZ = overrideTopZ == NoOverride ? topPlane.PrevZ : overrideTopZ;
        double prevBottomZ = bottomPlane.PrevZ;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        double prevSpanZ = prevTopZ - prevBottomZ;
        WallUV uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ, previous: false);
        WallUV prevUV = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, prevSpanZ, previous: true);

        TriangulatedWorldVertex topLeft = new(left.X, left.Y, topZ, prevTopZ, 
            uv.TopLeft.X, uv.TopLeft.Y, prevUV.TopLeft.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex topRight = new(right.X, right.Y, topZ, prevTopZ, 
            uv.BottomRight.X, uv.TopLeft.Y, prevUV.BottomRight.X, prevUV.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new(left.X, left.Y, bottomZ, prevBottomZ, 
            uv.TopLeft.X, uv.BottomRight.Y, prevUV.TopLeft.X, prevUV.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new(right.X, right.Y, bottomZ, prevBottomZ, 
            uv.BottomRight.X, uv.BottomRight.Y, prevUV.BottomRight.X, prevUV.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, prevTopZ, prevBottomZ);
    }

    public static unsafe void HandleSubsector(Subsector subsector, SectorPlane sectorPlane, in Vec2F textureVector,
        DynamicArray<TriangulatedWorldVertex> verticesToPopulate, double overrideZ = int.MaxValue)
    {
        Precondition(subsector.ClockwiseEdges.Count >= 3, "Cannot render subsector when it's degenerate (should have 3+ edges)");

        List<SubsectorSegment> edges = subsector.ClockwiseEdges;
        verticesToPopulate.EnsureCapacity(edges.Count);
        verticesToPopulate.SetLength(edges.Count);

        double z = overrideZ == int.MaxValue ? sectorPlane.Z : overrideZ;
        double prevZ = sectorPlane.PrevZ;
        Vec2D uv = default;
        Vec2D prevUV = default;
        if (sectorPlane.Facing == SectorPlaneFace.Ceiling)
        {
            fixed (TriangulatedWorldVertex* startVertex = &verticesToPopulate.Data[0])
            {
                TriangulatedWorldVertex* worldVertex = startVertex;
                for (int i = 0; i < edges.Count; i++)
                {
                    Vec2D vertex = edges[i].Start;
                    if (sectorPlane.SectorScrollData == null)
                    {
                        uv.X = vertex.X / textureVector.X;
                        uv.Y = -(vertex.Y / textureVector.Y);
                        prevUV = uv;
                    }
                    else
                    {
                        uv.X = vertex.X / textureVector.X;
                        uv.Y = -(vertex.Y / textureVector.Y);
                        prevUV = uv;

                        uv.X += sectorPlane.SectorScrollData.Offset.X;
                        uv.Y += sectorPlane.SectorScrollData.Offset.Y;
                        prevUV.X += sectorPlane.SectorScrollData.LastOffset.X;
                        prevUV.Y += sectorPlane.SectorScrollData.LastOffset.Y;
                    }

                    worldVertex->X = (float)vertex.X;
                    worldVertex->Y = (float)vertex.Y;
                    worldVertex->Z = (float)z;
                    worldVertex->PrevZ = (float)prevZ;
                    worldVertex->U = (float)uv.U;
                    worldVertex->V = (float)uv.V;
                    worldVertex->PrevU = (float)prevUV.U;
                    worldVertex->PrevV = (float)prevUV.V;
                    worldVertex++;
                }
            }
        }
        else
        {
            fixed (TriangulatedWorldVertex* startVertex = &verticesToPopulate.Data[0])
            {
                TriangulatedWorldVertex* worldVertex = startVertex;
                // Because the floor is looked at downwards and because it is
                // clockwise, to get counter-clockwise vertices we reverse the
                // iteration order and go from the end vertex.
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    Vec2D vertex = edges[i].End;
                    if (sectorPlane.SectorScrollData == null)
                    {
                        uv.X = vertex.X / textureVector.X;
                        uv.Y = -(vertex.Y / textureVector.Y);
                        prevUV = uv;
                    }
                    else
                    {
                        uv.X = vertex.X / textureVector.X;
                        uv.Y = -(vertex.Y / textureVector.Y);
                        prevUV = uv;

                        uv.X += sectorPlane.SectorScrollData.Offset.X;
                        uv.Y += sectorPlane.SectorScrollData.Offset.Y;
                        prevUV.X += sectorPlane.SectorScrollData.LastOffset.X;
                        prevUV.Y += sectorPlane.SectorScrollData.LastOffset.Y;
                    }

                    worldVertex->X = (float)vertex.X;
                    worldVertex->Y = (float)vertex.Y;
                    worldVertex->Z = (float)z;
                    worldVertex->PrevZ = (float)prevZ;
                    worldVertex->U = (float)uv.U;
                    worldVertex->V = (float)uv.V;
                    worldVertex->PrevU = (float)prevUV.U;
                    worldVertex->PrevV = (float)prevUV.V;
                    worldVertex++;
                }
            }
        }
    }

    private static MiddleDrawSpan CalculateMiddleDrawSpan(Line line, Side facingSide, double bottomOpeningZ,
        double topOpeningZ, double prevBottomOpeningZ, double prevTopOpeningZ, in Dimension textureDimension, double offset, double prevOffset)
    {
        double topZ = topOpeningZ;
        double bottomZ = topZ - textureDimension.Height;
        double prevTopZ = prevTopOpeningZ;
        double prevBottomZ = prevTopZ - textureDimension.Height;
        if (line.Flags.Unpegged.Lower)
        {
            bottomZ = bottomOpeningZ;
            topZ = bottomZ + textureDimension.Height;
            prevBottomZ = prevBottomOpeningZ;
            prevTopZ = prevBottomZ + textureDimension.Height;
        }

        topZ += facingSide.Offset.Y + offset;
        bottomZ += facingSide.Offset.Y + offset;
        prevTopZ += facingSide.Offset.Y + prevOffset;
        prevBottomZ += facingSide.Offset.Y + prevOffset;

        // Check if the lower/upper textures are set. If not then then the middle can be drawn through.
        double visibleTopZ = topZ;
        double visiblePrevTopZ = prevTopZ;
        if (facingSide.Upper.TextureHandle != Constants.NoTextureIndex)
        {
            visibleTopZ = Math.Min(topZ, topOpeningZ);
            visiblePrevTopZ = Math.Min(prevTopZ, prevTopOpeningZ);
        }
        double visibleBottomZ = bottomZ;
        double visiblePrevBottomZ = prevBottomZ;
        if (facingSide.Lower.TextureHandle != Constants.NoTextureIndex)
        {
            visibleBottomZ = Math.Max(bottomZ, bottomOpeningZ);
            visiblePrevBottomZ = Math.Max(prevBottomZ, prevBottomOpeningZ);
        }

        return new MiddleDrawSpan(bottomZ, topZ, visibleBottomZ, visibleTopZ, prevBottomZ, prevTopZ, visiblePrevBottomZ, visiblePrevTopZ);
    }

    public static WallUV CalculateOneSidedWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;

        if (side.ScrollData != null)
            offsetUV += previous ? side.ScrollData.LastOffsetMiddle.Float * textureUVInverse : 
                side.ScrollData.OffsetMiddle.Float * textureUVInverse;
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

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }

    public static WallUV CalculateTwoSidedLowerWallUV(Line line, Side facingSide, double length,
        in Vec2F textureUVInverse, double topZ, double bottomZ, bool previous)
    {
        Vec2F offsetUV = facingSide.Offset.Float * textureUVInverse;
        if (facingSide.ScrollData != null)
            offsetUV += previous ? facingSide.ScrollData.LastOffsetLower.Float * textureUVInverse : 
                facingSide.ScrollData.OffsetLower.Float * textureUVInverse;
        float wallSpanU = (float)length * textureUVInverse.U;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            double ceilZ = previous ? facingSide.Sector.Ceiling.PrevZ : facingSide.Sector.Ceiling.Z;
            float topDistFromCeil = (float)(ceilZ - topZ);
            float bottomDistFromCeil = (float)(ceilZ - bottomZ);

            topV = offsetUV.V + (topDistFromCeil * textureUVInverse.V);
            bottomV = offsetUV.V + (bottomDistFromCeil * textureUVInverse.V);
        }
        else
        {
            float spanZ = (float)(topZ - bottomZ);
            float spanV = spanZ * textureUVInverse.V;

            topV = offsetUV.V;
            bottomV = offsetUV.V + spanV;
        }

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }

    private static WallUV CalculateTwoSidedMiddleWallUV(Side side, double length, double topZ, double bottomZ, 
        double visibleTopZ, double visibleBottomZ, in Vec2F textureUVInverse, bool previous)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += previous ? side.ScrollData.LastOffsetMiddle.Float * textureUVInverse :
                side.ScrollData.OffsetMiddle.Float * textureUVInverse;
        float wallSpanU = (float)length * textureUVInverse.U;

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

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }

    public static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, bool previous)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += previous ? side.ScrollData.LastOffsetUpper.Float * textureUVInverse :
                side.ScrollData.OffsetUpper.Float * textureUVInverse;
        float wallSpanU = (float)length * textureUVInverse.U;
        float spanV = (float)spanZ * textureUVInverse.V;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U+ wallSpanU;
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

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }
}

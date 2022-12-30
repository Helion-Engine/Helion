using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
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

    public static WallVertices HandleOneSided(Side side, SectorPlane floor, SectorPlane ceiling, in Vec2F textureUVInverse,
        double overrideFloor = NoOverride, double overrideCeiling = NoOverride, bool isFront = true)
    {
        Line line = side.Line;

        Vec2D left = isFront ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFront ? line.Segment.End : line.Segment.Start;
        double topZ = overrideCeiling == NoOverride ? ceiling.Z : overrideCeiling;
        double bottomZ = overrideFloor == NoOverride ? floor.Z : overrideFloor;
        double prevTopZ = topZ;
        double prevBottomZ = bottomZ;
        double checkPrevTopZ = overrideCeiling == NoOverride ? ceiling.PrevZ : overrideCeiling;

        if (!line.Flags.Unpegged.Lower && topZ != checkPrevTopZ)
        {
            prevTopZ = checkPrevTopZ;
            prevBottomZ = overrideFloor == NoOverride ? floor.PrevZ : overrideFloor;
        }

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        WallUV uv = CalculateOneSidedWallUV(line, side, length, textureUVInverse, spanZ);

        TriangulatedWorldVertex topLeft = new TriangulatedWorldVertex(left.X, left.Y, topZ, prevTopZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedWorldVertex topRight = new TriangulatedWorldVertex(right.X, right.Y, topZ, prevTopZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new TriangulatedWorldVertex(left.X, left.Y, bottomZ, prevBottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new TriangulatedWorldVertex(right.X, right.Y, bottomZ, prevBottomZ, uv.BottomRight.X, uv.BottomRight.Y);

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
        double prevTopZ = topZ;
        double prevBottomZ = bottomZ;

        if (!line.Flags.Unpegged.Lower && topZ != topFlat.PrevZ)
        {
            prevTopZ = topFlat.PrevZ;
            prevBottomZ = bottomFlat.PrevZ;
        }

        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ);

        TriangulatedWorldVertex topLeft = new TriangulatedWorldVertex(left.X, left.Y, topZ, prevTopZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedWorldVertex topRight = new TriangulatedWorldVertex(right.X, right.Y, topZ, prevTopZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new TriangulatedWorldVertex(left.X, left.Y, bottomZ, prevBottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new TriangulatedWorldVertex(right.X, right.Y, bottomZ, prevBottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, prevTopZ, prevBottomZ);
    }

    public static WallVertices HandleTwoSidedMiddle(Side facingSide,
        in Dimension textureDimension, in Vec2F textureUVInverse, double bottomOpeningZ, double topOpeningZ, double prevBottomZ, double prevTopZ,
        bool isFrontSide, out bool nothingVisible, double offset = 0)
    {
        if (LineOpening.IsRenderingBlocked(facingSide.Line))
        {
            nothingVisible = true;
            return default;
        }

        Line line = facingSide.Line;
        MiddleDrawSpan drawSpan = CalculateMiddleDrawSpan(line, facingSide, bottomOpeningZ, topOpeningZ, prevBottomZ, prevTopZ, textureDimension, offset);
        if (drawSpan.NotVisible())
        {
            nothingVisible = true;
            return default;
        }

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan, textureUVInverse);

        TriangulatedWorldVertex topLeft = new TriangulatedWorldVertex(left.X, left.Y, drawSpan.VisibleTopZ, drawSpan.PrevVisibleTopZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedWorldVertex topRight = new TriangulatedWorldVertex(right.X, right.Y, drawSpan.VisibleTopZ, drawSpan.PrevVisibleTopZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new TriangulatedWorldVertex(left.X, left.Y, drawSpan.VisibleBottomZ, drawSpan.PrevVisibleBottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new TriangulatedWorldVertex(right.X, right.Y, drawSpan.VisibleBottomZ, drawSpan.PrevVisibleBottomZ, uv.BottomRight.X, uv.BottomRight.Y);

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
        WallUV uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ);

        TriangulatedWorldVertex topLeft = new TriangulatedWorldVertex(left.X, left.Y, topZ, prevTopZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedWorldVertex topRight = new TriangulatedWorldVertex(right.X, right.Y, topZ, prevTopZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedWorldVertex bottomLeft = new TriangulatedWorldVertex(left.X, left.Y, bottomZ, prevBottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedWorldVertex bottomRight = new TriangulatedWorldVertex(right.X, right.Y, bottomZ, prevBottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight, prevTopZ, prevBottomZ);
    }

    public static void HandleSubsector(Subsector subsector, SectorPlane sectorPlane, in Dimension textureDimension,
        DynamicArray<TriangulatedWorldVertex> verticesToPopulate, double overrideZ = int.MaxValue)
    {
        Precondition(subsector.ClockwiseEdges.Count >= 3, "Cannot render subsector when it's degenerate (should have 3+ edges)");

        List<SubsectorSegment> edges = subsector.ClockwiseEdges;
        verticesToPopulate.Clear();

        if (sectorPlane.Facing == SectorPlaneFace.Ceiling)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                Vec2D vertex = edges[i].Start;

                // TODO: Interpolation and slopes needs a slight change in
                //       how we store sector flat plane information.
                double z = sectorPlane.Z;
                double prevZ = sectorPlane.PrevZ;
                if (overrideZ != int.MaxValue)
                    z = overrideZ;

                Vec3F position = ((float)vertex.X, (float)vertex.Y, (float)z);
                Vec2F uv = CalculateFlatUV(sectorPlane.SectorScrollData, vertex, textureDimension);

                verticesToPopulate.Add(new TriangulatedWorldVertex(position, (float)prevZ, uv));
            }
        }
        else
        {
            // Because the floor is looked at downwards and because it is
            // clockwise, to get counter-clockwise vertices we reverse the
            // iteration order and go from the end vertex.
            for (int i = edges.Count - 1; i >= 0; i--)
            {
                Vec2D vertex = edges[i].End;

                // TODO: Interpolation and slopes needs a slight change in
                //       how we store sector flat plane information.
                double z = sectorPlane.Z;
                double prevZ = sectorPlane.PrevZ;
                if (overrideZ != int.MaxValue)
                    z = overrideZ;

                Vec3F position = ((float)vertex.X, (float)vertex.Y, (float)z);
                Vec2F uv = CalculateFlatUV(sectorPlane.SectorScrollData, vertex, textureDimension);

                verticesToPopulate.Add(new TriangulatedWorldVertex(position, (float)prevZ, uv));
            }
        }
    }

    private static MiddleDrawSpan CalculateMiddleDrawSpan(Line line, Side facingSide, double bottomOpeningZ,
        double topOpeningZ, double prevBottomOpeningZ, double prevTopOpeningZ, in Dimension textureDimension, double offset)
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
        prevTopZ += facingSide.Offset.Y + offset;
        prevBottomZ += facingSide.Offset.Y + offset;

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

        return new MiddleDrawSpan(bottomZ, topZ, visibleBottomZ, visibleTopZ, prevTopZ, prevBottomZ, visiblePrevBottomZ, visiblePrevTopZ);
    }

    public static WallUV CalculateOneSidedWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;

        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.MiddlePosition, textureUVInverse);
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
        in Vec2F textureUVInverse, double topZ, double bottomZ)
    {
        Vec2F offsetUV = facingSide.Offset.Float * textureUVInverse;
        if (facingSide.ScrollData != null)
            offsetUV += GetScrollOffset(facingSide.ScrollData, SideScrollData.LowerPosition, textureUVInverse);
        float wallSpanU = (float)length * textureUVInverse.U;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            double ceilZ = facingSide.Sector.Ceiling.Z;
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

    private static WallUV CalculateTwoSidedMiddleWallUV(Side side, double length, in MiddleDrawSpan drawSpan,
        in Vec2F textureUVInverse)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.MiddlePosition, textureUVInverse);
        float wallSpanU = (float)length * textureUVInverse.U;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;

        // Since we only draw one of the texture, all we need to do is find
        // out where the texture is clamped by and find that value between
        // [0.0, 1.0]. For example if a texture height of 10 only has two
        // pixels available between 6 -> 7 for the line opening, then
        // the top V would be 0.6 and the bottom V would be 0.7.
        double textureHeight = drawSpan.TopZ - drawSpan.BottomZ;
        float topV = 1.0f - (float)((drawSpan.VisibleTopZ - drawSpan.BottomZ) / textureHeight);
        float bottomV = 1.0f - (float)((drawSpan.VisibleBottomZ - drawSpan.BottomZ) / textureHeight);

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }

    public static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.UpperPosition, textureUVInverse);
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

    private static Vec2F GetScrollOffset(SideScrollData scrollData, int position, in Vec2F textureUVInverse)
    {
        //Vec2F scrollAmount = scrollData.LastOffset[position].Interpolate(scrollData.Offset[position], tickFraction).Float;
        Vec2F scrollAmount = scrollData.Offset[position].Float;
        return scrollAmount * textureUVInverse;
    }

    private static Vec2F CalculateFlatUV(SectorScrollData? scrollData, in Vec2D vertex, in Dimension textureDimension)
    {
        Vec2F uv = vertex.Float / textureDimension.Vector.Float;
        if (scrollData != null)
        {
            // TODO scrolling
            Vec2F scrollAmount = scrollData.Offset.Float;
            uv.X += scrollAmount.X;
            uv.Y -= scrollAmount.Y;
        }

        // When we map coordinates to their texture coordinates, because
        // we do division above, a coordinate with Y values of 16 to 32
        // for a 64-dimension texture gets mapped onto 0.25 and 0.5.
        // However the textures are drawn from the top down in vanilla
        // (and all the other ports), which means 16 is effectively 0.75
        // and 32 is 0.5.
        //
        // This means our drawing is inverted along the Y axis, and this is
        // trivially fixed by inverting letting the shader take care of the
        // rest when it clamps it to the image.
        uv.Y = -uv.Y;
        return uv;
    }
}

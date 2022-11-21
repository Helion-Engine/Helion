using System;
using System.Collections.Generic;
using Helion;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Render;
using Helion.Render.Common.Shared.World;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Common.Shared.World;

public static class WorldTriangulator
{
    public const double NoOverride = double.MaxValue;

    public static WallVertices HandleOneSided(Side side, SectorPlane floor, SectorPlane ceiling, in Vec2F textureUVInverse, double tickFraction,
        double overrideFloor = NoOverride, double overrideCeiling = NoOverride, bool isFront = true)
    {
        Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick interpolation out of unit range");

        Line line = side.Line;

        Vec2D left = isFront ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFront ? line.Segment.End : line.Segment.Start;
        double topZ = overrideCeiling == NoOverride ? ceiling.PrevZ.Interpolate(ceiling.Z, tickFraction) : overrideCeiling;
        double bottomZ = overrideFloor == NoOverride ? floor.PrevZ.Interpolate(floor.Z, tickFraction) : overrideFloor;

        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        WallUV uv = CalculateOneSidedWallUV(line, side, length, textureUVInverse, spanZ, tickFraction);

        TriangulatedVertex topLeft = new TriangulatedVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedVertex topRight = new TriangulatedVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedVertex bottomLeft = new TriangulatedVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedVertex bottomRight = new TriangulatedVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    public static WallVertices HandleTwoSidedLower(Side facingSide, SectorPlane topFlat, SectorPlane bottomFlat,
        in Vec2F textureUVInverse, bool isFrontSide, double tickFraction)
    {
        Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick interpolation out of unit range");

        Line line = facingSide.Line;

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double topZ = topFlat.PrevZ.Interpolate(topFlat.Z, tickFraction);
        double bottomZ = bottomFlat.PrevZ.Interpolate(bottomFlat.Z, tickFraction);

        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ, tickFraction);

        TriangulatedVertex topLeft = new TriangulatedVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedVertex topRight = new TriangulatedVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedVertex bottomLeft = new TriangulatedVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedVertex bottomRight = new TriangulatedVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    public static WallVertices HandleTwoSidedMiddle(Side facingSide,
        in Dimension textureDimension, in Vec2F textureUVInverse, double bottomOpeningZ, double topOpeningZ,
        bool isFrontSide, out bool nothingVisible, double tickFraction, double offset = 0)
    {
        if (LineOpening.IsRenderingBlocked(facingSide.Line))
        {
            nothingVisible = true;
            return default;
        }

        Line line = facingSide.Line;
        MiddleDrawSpan drawSpan = CalculateMiddleDrawSpan(line, facingSide, bottomOpeningZ, topOpeningZ, textureDimension, offset);
        if (drawSpan.NotVisible())
        {
            nothingVisible = true;
            return default;
        }

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double length = line.GetLength();
        WallUV uv = CalculateTwoSidedMiddleWallUV(facingSide, length, drawSpan, textureUVInverse, tickFraction);

        TriangulatedVertex topLeft = new TriangulatedVertex(left.X, left.Y, drawSpan.VisibleTopZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedVertex topRight = new TriangulatedVertex(right.X, right.Y, drawSpan.VisibleTopZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedVertex bottomLeft = new TriangulatedVertex(left.X, left.Y, drawSpan.VisibleBottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedVertex bottomRight = new TriangulatedVertex(right.X, right.Y, drawSpan.VisibleBottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        nothingVisible = false;
        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    public static WallVertices HandleTwoSidedUpper(Side facingSide, SectorPlane topPlane, SectorPlane bottomPlane, in Vec2F textureUVInverse,
        bool isFrontSide, double tickFraction, double overrideTopZ = NoOverride)
    {
        Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick interpolation out of unit range");

        Line line = facingSide.Line;

        Vec2D left = isFrontSide ? line.Segment.Start : line.Segment.End;
        Vec2D right = isFrontSide ? line.Segment.End : line.Segment.Start;
        double topZ = overrideTopZ == NoOverride ? topPlane.PrevZ.Interpolate(topPlane.Z, tickFraction) : overrideTopZ;
        double bottomZ = bottomPlane.PrevZ.Interpolate(bottomPlane.Z, tickFraction);

        // TODO: If unchanging, we can pre-calculate the length.
        double length = line.GetLength();
        double spanZ = topZ - bottomZ;
        WallUV uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ, tickFraction);

        TriangulatedVertex topLeft = new TriangulatedVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
        TriangulatedVertex topRight = new TriangulatedVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
        TriangulatedVertex bottomLeft = new TriangulatedVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
        TriangulatedVertex bottomRight = new TriangulatedVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);

        return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
    }

    public static void HandleSubsector(Subsector subsector, SectorPlane sectorPlane, in Dimension textureDimension,
        double tickFraction, DynamicArray<TriangulatedVertex> verticesToPopulate, double overrideZ = int.MaxValue)
    {
        Precondition(tickFraction >= 0.0 && tickFraction <= 1.0, "Tick interpolation out of unit range");
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
                double z = sectorPlane.PrevZ.Interpolate(sectorPlane.Z, tickFraction);
                if (overrideZ != int.MaxValue)
                    z = overrideZ;

                Vec3F position = ((float)vertex.X, (float)vertex.Y, (float)z);
                Vec2F uv = CalculateFlatUV(sectorPlane.SectorScrollData, vertex, textureDimension, tickFraction);

                verticesToPopulate.Add(new TriangulatedVertex(position, uv));
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
                double z = sectorPlane.PrevZ.Interpolate(sectorPlane.Z, tickFraction);
                if (overrideZ != int.MaxValue)
                    z = overrideZ;

                Vec3F position = ((float)vertex.X, (float)vertex.Y, (float)z);
                Vec2F uv = CalculateFlatUV(sectorPlane.SectorScrollData, vertex, textureDimension, tickFraction);

                verticesToPopulate.Add(new TriangulatedVertex(position, uv));
            }
        }
    }

    private static MiddleDrawSpan CalculateMiddleDrawSpan(Line line, Side facingSide, double bottomOpeningZ,
        double topOpeningZ, in Dimension textureDimension, double offset)
    {
        double topZ = topOpeningZ;
        double bottomZ = topZ - textureDimension.Height;
        if (line.Flags.Unpegged.Lower)
        {
            bottomZ = bottomOpeningZ;
            topZ = bottomZ + textureDimension.Height;
        }

        topZ += facingSide.Offset.Y + offset;
        bottomZ += facingSide.Offset.Y + offset;

        // Check if the lower/upper textures are set. If not then then the middle can be drawn through.
        double visibleTopZ = topZ;
        if (facingSide.Upper.TextureHandle != Constants.NoTextureIndex)
            visibleTopZ = Math.Min(topZ, topOpeningZ);
        double visibleBottomZ = bottomZ;
        if (facingSide.Lower.TextureHandle != Constants.NoTextureIndex)
            visibleBottomZ = Math.Max(bottomZ, bottomOpeningZ);

        return new MiddleDrawSpan(bottomZ, topZ, visibleBottomZ, visibleTopZ);
    }

    private static WallUV CalculateOneSidedWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, double tickFraction)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.MiddlePosition, textureUVInverse, tickFraction);
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

    private static WallUV CalculateTwoSidedLowerWallUV(Line line, Side facingSide, double length,
        in Vec2F textureUVInverse, double topZ, double bottomZ, double tickFraction)
    {
        Vec2F offsetUV = facingSide.Offset.Float * textureUVInverse;
        if (facingSide.ScrollData != null)
            offsetUV += GetScrollOffset(facingSide.ScrollData, SideScrollData.LowerPosition, textureUVInverse, tickFraction);
        float wallSpanU = (float)length * textureUVInverse.U;

        float leftU = offsetUV.U;
        float rightU = offsetUV.U + wallSpanU;
        float topV;
        float bottomV;

        if (line.Flags.Unpegged.Lower)
        {
            double ceilZ = facingSide.Sector.Ceiling.PrevZ.Interpolate(facingSide.Sector.Ceiling.Z, tickFraction);
            float topDistFromCeil = (float)(ceilZ - topZ);
            float bottomDistFromCeil = (float)(ceilZ - bottomZ);

            topV = offsetUV.V + topDistFromCeil * textureUVInverse.V;
            bottomV = offsetUV.V + bottomDistFromCeil * textureUVInverse.V;
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
        in Vec2F textureUVInverse, double tickFraction)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.MiddlePosition, textureUVInverse, tickFraction);
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

    private static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length,
        in Vec2F textureUVInverse, double spanZ, double tickFraction)
    {
        Vec2F offsetUV = side.Offset.Float * textureUVInverse;
        if (side.ScrollData != null)
            offsetUV += GetScrollOffset(side.ScrollData, SideScrollData.UpperPosition, textureUVInverse, tickFraction);
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

        return new WallUV(new Vec2F(leftU, topV), new Vec2F(rightU, bottomV));
    }

    private static Vec2F GetScrollOffset(SideScrollData scrollData, int position, in Vec2F textureUVInverse, double tickFraction)
    {
        Vec2F scrollAmount = scrollData.LastOffset[position].Interpolate(scrollData.Offset[position], tickFraction).Float;
        return scrollAmount * textureUVInverse;
    }

    private static Vec2F CalculateFlatUV(SectorScrollData? scrollData, in Vec2D vertex, in Dimension textureDimension,
        double tickFraction)
    {
        Vec2F uv = vertex.Float / textureDimension.Vector.Float;
        if (scrollData != null)
        {
            Vec2F scrollAmount = scrollData.LastOffset.Interpolate(scrollData.Offset, tickFraction).Float;
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

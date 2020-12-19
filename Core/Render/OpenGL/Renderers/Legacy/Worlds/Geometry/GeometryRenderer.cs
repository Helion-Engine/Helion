using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.Worlds.Data;
using Helion.Render.OpenGL.Renderers.Legacy.Worlds.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.Worlds.Sky.Sphere;
using Helion.Render.OpenGL.Textures.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.Worlds;
using Helion.Render.Shared.Worlds.ViewClipping;
using Helion.Resource;
using Helion.Util.Configuration;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Subsectors;
using Helion.Worlds.Geometry.Walls;
using Helion.Worlds.Physics;
using Helion.Worlds.Textures;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Worlds.Geometry
{
    public class GeometryRenderer : IDisposable
    {
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly LineDrawnTracker m_lineDrawnTracker = new LineDrawnTracker();
        private readonly DynamicArray<WorldVertex> m_subsectorVertices = new DynamicArray<WorldVertex>();
        private readonly ViewClipper m_viewClipper;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly LegacySkyRenderer m_skyRenderer;
        private double m_tickFraction;

        private LegacyVertex[][] m_vertexLookup = new LegacyVertex[0][];
        private LegacyVertex[][] m_vertexLowerLookup = new LegacyVertex[0][];
        private LegacyVertex[][] m_vertexUpperLookup = new LegacyVertex[0][];
        private SkyGeometryVertex[][] m_skyWallVertexLookup = new SkyGeometryVertex[0][];
        private LegacyVertex[][] m_vertexFloorLookup = new LegacyVertex[0][];
        private LegacyVertex[][] m_vertexCeilingLookup = new LegacyVertex[0][];
        private SkyGeometryVertex[][] m_skyFloorVertexLookup = new SkyGeometryVertex[0][];
        private SkyGeometryVertex[][] m_skyCeilingVertexLookup = new SkyGeometryVertex[0][];

        public GeometryRenderer(Config config, Resources resources, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager, ViewClipper viewClipper,
            RenderWorldDataManager worldDataManager)
        {
            m_textureManager = textureManager;
            m_worldDataManager = worldDataManager;
            m_viewClipper = viewClipper;
            m_skyRenderer = new LegacySkyRenderer(config, resources, capabilities, functions, textureManager);
        }

        ~GeometryRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void UpdateTo(World world)
        {
            // TODO: We should create a new one from the ground up when making new sky renderers.
            m_skyRenderer.Clear();
            m_lineDrawnTracker.UpdateToWorld(world);

            m_vertexLookup = new LegacyVertex[world.Sides.Count][];
            m_vertexLowerLookup = new LegacyVertex[world.Sides.Count][];
            m_vertexUpperLookup = new LegacyVertex[world.Sides.Count][];
            m_skyWallVertexLookup = new SkyGeometryVertex[world.Sides.Count][];
            m_skyFloorVertexLookup = new SkyGeometryVertex[world.BspTree.Subsectors.Length][];
            m_skyCeilingVertexLookup = new SkyGeometryVertex[world.BspTree.Subsectors.Length][];
            m_vertexFloorLookup = new LegacyVertex[world.BspTree.Subsectors.Length][];
            m_vertexCeilingLookup = new LegacyVertex[world.BspTree.Subsectors.Length][];
        }

        public void Clear(double tickFraction)
        {
            m_tickFraction = tickFraction;
            m_skyRenderer.Clear();
            m_lineDrawnTracker.ClearDrawnLines();
        }

        public void Render(RenderInfo renderInfo)
        {
            m_skyRenderer.Render(renderInfo);
        }

        public void RenderSubsector(Subsector subsector, in Vec2D position)
        {
            RenderWalls(subsector, position);
            RenderFlat(subsector, subsector.Sector.Floor, true);
            RenderFlat(subsector, subsector.Sector.Ceiling, false);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void RenderWalls(Subsector subsector, in Vec2D position)
        {
            List<SubsectorSegment> edges = subsector.ClockwiseEdges;
            for (int i = 0; i < edges.Count; i++)
            {
                SubsectorSegment edge = edges[i];
                if (edge.Line == null)
                    continue;

                if (m_lineDrawnTracker.HasDrawn(edge.Line))
                {
                    AddLineClip(edge, edge.Line);
                    continue;
                }

                bool onFrontSide = edge.Line.Segment.OnRight(position);
                if (!onFrontSide && edge.Line.OneSided)
                    continue;

                Side? side = onFrontSide ? edge.Line.Front : edge.Line.Back;
                if (side == null)
                    throw new NullReferenceException("Trying to draw the wrong side of a one sided line (or a miniseg)");

                RenderSide(side, onFrontSide);

                m_lineDrawnTracker.MarkDrawn(edge.Line);
                AddLineClip(edge, edge.Line);
            }
        }

        private void AddLineClip(SubsectorSegment edge, Line line)
        {
            if (line.OneSided)
                m_viewClipper.AddLine(edge.Start, edge.End);
            else if (LineOpening.GetOpeningHeight(line) <= 0)
                m_viewClipper.AddLine(edge.Start, edge.End);
        }

        private void RenderSide(Side side, bool isFrontSide)
        {
            if (side.Line.OneSided)
                RenderOneSided(side);
            else
                RenderTwoSided(side, isFrontSide);
        }

        private void RenderOneSided(Side side)
        {
            Wall middle = side.Middle!;

            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            GLLegacyTexture texture = m_textureManager.GetTexture(middle.Texture);
            LegacyVertex[] data = m_vertexLookup[side.Id];

            if (side.Sector.DataChanged)
            {
                WallVertices wall = WorldTriangulator.HandleOneSided(side, texture.UVInverse, m_tickFraction);
                data = GetWallVertices(wall, side.Sector.LightLevel / 256.0f);
                m_vertexLookup[side.Id] = data;
            }
            else if (side.Sector.LightingChanged)
            {
                SetLightToVertices(data, side.Sector.LightLevel / 256.0f);
            }

            RenderWorldData renderData = m_worldDataManager[texture];
            renderData.Vbo.Add(data);
        }

        private void SetLightToVertices(LegacyVertex[] data, float lightLevel)
        {
            for (int i = 0; i < data.Length; i++)
                data[i].LightLevelUnit = lightLevel;
        }

        private void RenderTwoSided(Side facingSide, bool isFrontSide)
        {
            Side otherSide = facingSide.PartnerSide!;
            Sector facingSector = facingSide.Sector;
            Sector otherSector = otherSide.Sector;

            if (LowerIsVisible(facingSector, otherSector))
                RenderTwoSidedLower(facingSide, otherSide, isFrontSide);
            if (facingSide.Middle?.Texture.IsMissing ?? false)
                RenderTwoSidedMiddle(facingSide, otherSide, isFrontSide);
            if (UpperIsVisible(facingSector, otherSector))
                RenderTwoSidedUpper(facingSide, otherSide, isFrontSide);
        }

        private bool LowerIsVisible(Sector facingSector, Sector otherSector)
        {
            double facingZ = facingSector.Floor.PrevZ.Interpolate(facingSector.Floor.Z, m_tickFraction);
            double otherZ = otherSector.Floor.PrevZ.Interpolate(otherSector.Floor.Z, m_tickFraction);
            return facingZ < otherZ;
        }

        private bool UpperIsVisible(Sector facingSector, Sector otherSector)
        {
            double facingZ = facingSector.Ceiling.PrevZ.Interpolate(facingSector.Ceiling.Z, m_tickFraction);
            double otherZ = otherSector.Ceiling.PrevZ.Interpolate(otherSector.Ceiling.Z, m_tickFraction);
            return facingZ > otherZ;
        }

        private void RenderTwoSidedLower(Side facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            SectorPlane plane = otherSide.Sector.Floor;
            bool isSky = plane.Texture.IsSky;
            Wall lowerWall = facingSide.Lower!;

            IWorldTexture worldTexture = lowerWall.Texture.IsMissing ? plane.Texture : lowerWall.Texture;
            GLLegacyTexture texture = m_textureManager.GetTexture(worldTexture);
            RenderWorldData renderData = m_worldDataManager[texture];

            if (isSky)
            {
                SkyGeometryVertex[] data = m_skyWallVertexLookup[facingSide.Id];

                if (facingSide.Sector.DataChanged || otherSide.Sector.DataChanged)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction);
                    data = CreateSkyWallVertices(wall);
                    m_skyWallVertexLookup[facingSide.Id] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[] data = m_vertexLowerLookup[facingSide.Id];

                if (facingSide.Sector.DataChanged || otherSide.Sector.DataChanged)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction);
                    data = GetWallVertices(wall, facingSide.Sector.LightLevel / 256.0f);
                    m_vertexLowerLookup[facingSide.Id] = data;
                }
                else if (facingSide.Sector.LightingChanged)
                {
                    SetLightToVertices(data, facingSide.Sector.LightLevel / 256.0f);
                }

                // See RenderOneSided() for an ASCII image of why we do this.
                renderData.Vbo.Add(data);
            }
        }

        private void RenderTwoSidedUpper(Side facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            SectorPlane plane = otherSide.Sector.Ceiling;
            bool isSky = plane.Texture.IsSky;
            Wall upperWall = facingSide.Upper!;

            IWorldTexture worldTexture = upperWall.Texture.IsMissing ? plane.Texture : upperWall.Texture;
            GLLegacyTexture texture = m_textureManager.GetTexture(worldTexture);
            RenderWorldData renderData = m_worldDataManager[texture];

            if (isSky)
            {
                SkyGeometryVertex[] data = m_skyWallVertexLookup[facingSide.Id];

                if (facingSide.Sector.DataChanged || otherSide.Sector.DataChanged)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction);
                    data = CreateSkyWallVertices(wall);
                    m_skyWallVertexLookup[facingSide.Id] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[] data = m_vertexUpperLookup[facingSide.Id];

                if (facingSide.Sector.DataChanged || otherSide.Sector.DataChanged)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction);
                    data = GetWallVertices(wall, facingSide.Sector.LightLevel / 256.0f);
                    m_vertexUpperLookup[facingSide.Id] = data;
                }
                else if (facingSide.Sector.LightingChanged)
                {
                    SetLightToVertices(data, facingSide.Sector.LightLevel / 256.0f);
                }

                // See RenderOneSided() for an ASCII image of why we do this.
                renderData.Vbo.Add(data);
            }
        }

        private void RenderTwoSidedMiddle(Side facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            Wall middleWall = facingSide.Middle!;
            GLLegacyTexture texture = m_textureManager.GetTexture(middleWall.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];
            LegacyVertex[] data = m_vertexLookup[facingSide.Id];

            if (facingSide.Sector.DataChanged)
            {
                (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSide.Sector, otherSide.Sector);
                WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide, otherSide,
                    texture.Dimension, texture.UVInverse, bottomZ, topZ, isFrontSide, out bool nothingVisible, m_tickFraction);

                // If the texture can't be drawn because the level has offsets that
                // are messed up (ex: offset causes it to be completely missing) we
                // can exit early since nothing can be drawn.
                float lightLevel = facingSide.Sector.LightLevel / 256.0f;
                data = nothingVisible ? Array.Empty<LegacyVertex>() : GetWallVertices(wall, lightLevel);

                m_vertexLookup[facingSide.Id] = data;
            }
            else if (facingSide.Sector.LightingChanged)
            {
                SetLightToVertices(data, facingSide.Sector.LightLevel / 256.0f);
            }

            // See RenderOneSided() for an ASCII image of why we do this.
            renderData.Vbo.Add(data);
        }

        private (double bottomZ, double topZ) FindOpeningFlatsInterpolated(Sector facingSector, Sector otherSector)
        {
            SectorPlane facingFloor = facingSector.Floor;
            SectorPlane facingCeiling = facingSector.Ceiling;
            SectorPlane otherFloor = otherSector.Floor;
            SectorPlane otherCeiling = otherSector.Ceiling;

            double facingFloorZ = facingFloor.PrevZ.Interpolate(facingFloor.Z, m_tickFraction);
            double facingCeilingZ = facingCeiling.PrevZ.Interpolate(facingCeiling.Z, m_tickFraction);
            double otherFloorZ = otherFloor.PrevZ.Interpolate(otherFloor.Z, m_tickFraction);
            double otherCeilingZ = otherCeiling.PrevZ.Interpolate(otherCeiling.Z, m_tickFraction);

            double bottomZ = facingFloorZ;
            double topZ = facingCeilingZ;
            if (otherFloorZ > facingFloorZ)
                bottomZ = otherFloorZ;
            if (otherCeilingZ < facingCeilingZ)
                topZ = otherCeilingZ;

            return (bottomZ, topZ);
        }

        private void RenderFlat(Subsector subsector, SectorPlane flat, bool floor)
        {
            // TODO: If we can't see it (dot product the plane) then exit.
            bool isSky = flat.Texture.IsSky;
            GLLegacyTexture texture = m_textureManager.GetTexture(flat.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];

            if (isSky)
            {
                SkyGeometryVertex[] data = floor ? m_skyFloorVertexLookup[subsector.Index] : m_skyCeilingVertexLookup[subsector.Index];

                if (flat.Sector.DataChanged)
                {
                    // TODO: A lot of calculations aren't needed for sky coordinates, waste of computation.
                    // Note that the subsector triangulator is supposed to realize when
                    // we're passing it a floor or ceiling and order the vertices for
                    // us such that it's always in counter-clockwise order.
                    WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices);
                    WorldVertex root = m_subsectorVertices[0];
                    List<SkyGeometryVertex> subData = new();
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        WorldVertex second = m_subsectorVertices[i];
                        WorldVertex third = m_subsectorVertices[i + 1];
                        subData.AddRange(CreateSkyFlatVertices(root, second, third));
                    }

                    data = subData.ToArray();
                    if (floor)
                        m_skyFloorVertexLookup[subsector.Index] = data;
                    else
                        m_skyCeilingVertexLookup[subsector.Index] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[] data = floor ? m_vertexFloorLookup[subsector.Index] : m_vertexCeilingLookup[subsector.Index];

                if (flat.Sector.DataChanged)
                {
                    WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices);
                    WorldVertex root = m_subsectorVertices[0];
                    List<LegacyVertex> subData = new List<LegacyVertex>();
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        WorldVertex second = m_subsectorVertices[i];
                        WorldVertex third = m_subsectorVertices[i + 1];
                        subData.AddRange(GetFlatVertices(ref root, ref second, ref third, flat.LightLevel / 256.0f));
                    }

                    data = subData.ToArray();
                    if (floor)
                        m_vertexFloorLookup[subsector.Index] = data;
                    else
                        m_vertexCeilingLookup[subsector.Index] = data;
                }
                else if (flat.Sector.LightingChanged)
                {
                    SetLightToVertices(data, flat.Sector.LightLevel / 256.0f);
                }

                renderData.Vbo.Add(data);
            }
        }

        private SkyGeometryVertex[] CreateSkyWallVertices(in WallVertices wv)
        {
            SkyGeometryVertex[] data = new SkyGeometryVertex[6];
            data[0].X = wv.TopLeft.X;
            data[0].Y = wv.TopLeft.Y;
            data[0].Z = wv.TopLeft.Z;

            data[1].X = wv.BottomLeft.X;
            data[1].Y = wv.BottomLeft.Y;
            data[1].Z = wv.BottomLeft.Z;

            data[2].X = wv.TopRight.X;
            data[2].Y = wv.TopRight.Y;
            data[2].Z = wv.TopRight.Z;

            data[3].X = wv.TopRight.X;
            data[3].Y = wv.TopRight.Y;
            data[3].Z = wv.TopRight.Z;

            data[4].X = wv.BottomLeft.X;
            data[4].Y = wv.BottomLeft.Y;
            data[4].Z = wv.BottomLeft.Z;

            data[5].X = wv.BottomRight.X;
            data[5].Y = wv.BottomRight.Y;
            data[5].Z = wv.BottomRight.Z;

            return data;
        }

        private SkyGeometryVertex[] CreateSkyFlatVertices(in WorldVertex root, in WorldVertex second, in WorldVertex third)
        {
            SkyGeometryVertex[] data = new SkyGeometryVertex[3];
            data[0].X = root.X;
            data[0].Y = root.Y;
            data[0].Z = root.Z;

            data[1].X = second.X;
            data[1].Y = second.Y;
            data[1].Z = second.Z;

            data[2].X = third.X;
            data[2].Y = third.Y;
            data[2].Z = third.Z;

            return data;
        }

        private LegacyVertex[] GetWallVertices(in WallVertices wv, float lightLevel)
        {
            LegacyVertex[] data = new LegacyVertex[6];
            // Our triangle is added like:
            //    0--2
            //    | /  3
            //    |/  /|
            //    1  / |
            //      4--5
            data[0].LightLevelUnit = lightLevel;
            data[0].X = wv.TopLeft.X;
            data[0].Y = wv.TopLeft.Y;
            data[0].Z = wv.TopLeft.Z;
            data[0].U = wv.TopLeft.U;
            data[0].V = wv.TopLeft.V;

            data[1].LightLevelUnit = lightLevel;
            data[1].X = wv.BottomLeft.X;
            data[1].Y = wv.BottomLeft.Y;
            data[1].Z = wv.BottomLeft.Z;
            data[1].U = wv.BottomLeft.U;
            data[1].V = wv.BottomLeft.V;

            data[2].LightLevelUnit = lightLevel;
            data[2].X = wv.TopRight.X;
            data[2].Y = wv.TopRight.Y;
            data[2].Z = wv.TopRight.Z;
            data[2].U = wv.TopRight.U;
            data[2].V = wv.TopRight.V;

            data[3].LightLevelUnit = lightLevel;
            data[3].X = wv.TopRight.X;
            data[3].Y = wv.TopRight.Y;
            data[3].Z = wv.TopRight.Z;
            data[3].U = wv.TopRight.U;
            data[3].V = wv.TopRight.V;

            data[4].LightLevelUnit = lightLevel;
            data[4].X = wv.BottomLeft.X;
            data[4].Y = wv.BottomLeft.Y;
            data[4].Z = wv.BottomLeft.Z;
            data[4].U = wv.BottomLeft.U;
            data[4].V = wv.BottomLeft.V;

            data[5].LightLevelUnit = lightLevel;
            data[5].X = wv.BottomRight.X;
            data[5].Y = wv.BottomRight.Y;
            data[5].Z = wv.BottomRight.Z;
            data[5].U = wv.BottomRight.U;
            data[5].V = wv.BottomRight.V;

            return data;
        }

        private LegacyVertex[] GetFlatVertices(ref WorldVertex root, ref WorldVertex second, ref WorldVertex third, float lightLevel)
        {
            LegacyVertex[] data = new LegacyVertex[3];

            data[0].LightLevelUnit = lightLevel;
            data[0].X = root.X;
            data[0].Y = root.Y;
            data[0].Z = root.Z;
            data[0].U = root.U;
            data[0].V = root.V;

            data[1].LightLevelUnit = lightLevel;
            data[1].X = second.X;
            data[1].Y = second.Y;
            data[1].Z = second.Z;
            data[1].U = second.U;
            data[1].V = second.V;

            data[2].LightLevelUnit = lightLevel;
            data[2].X = third.X;
            data[2].Y = third.Y;
            data[2].Z = third.Z;
            data[2].U = third.U;
            data[2].V = third.V;

            return data;
        }

        private void ReleaseUnmanagedResources()
        {
            m_skyRenderer.Dispose();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using Helion.World.Geometry.Walls;
using Helion.World.Physics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry
{
    public class GeometryRenderer : IDisposable
    {
        private const double MaxSky = 65536;

        private readonly LegacyGLTextureManager m_textureManager;
        private readonly LineDrawnTracker m_lineDrawnTracker = new LineDrawnTracker();
        private readonly DynamicArray<WorldVertex> m_subsectorVertices = new DynamicArray<WorldVertex>();
        private readonly ViewClipper m_viewClipper;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly LegacySkyRenderer m_skyRenderer;
        private double m_tickFraction;
        private bool m_skyOverride;

        private LegacyVertex[][] m_vertexLookup = Array.Empty<LegacyVertex[]>();
        private LegacyVertex[][] m_vertexLowerLookup = Array.Empty<LegacyVertex[]>();
        private LegacyVertex[][] m_vertexUpperLookup = Array.Empty<LegacyVertex[]>();
        private SkyGeometryVertex[][] m_skyWallVertexLowerLookup = new SkyGeometryVertex[0][];
        private SkyGeometryVertex[][] m_skyWallVertexUpperLookup = new SkyGeometryVertex[0][];
        private LegacyVertex[][] m_vertexFloorLookup = Array.Empty<LegacyVertex[]>();
        private LegacyVertex[][] m_vertexCeilingLookup = Array.Empty<LegacyVertex[]>();
        private SkyGeometryVertex[][] m_skyFloorVertexLookup = Array.Empty<SkyGeometryVertex[]>();
        private SkyGeometryVertex[][] m_skyCeilingVertexLookup = Array.Empty<SkyGeometryVertex[]>();

        public GeometryRenderer(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager, ViewClipper viewClipper,
            RenderWorldDataManager worldDataManager)
        {
            m_textureManager = textureManager;
            m_worldDataManager = worldDataManager;
            m_viewClipper = viewClipper;
            m_skyRenderer = new LegacySkyRenderer(config, archiveCollection, capabilities, functions, textureManager);
        }

        ~GeometryRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void UpdateTo(WorldBase world)
        {
            // TODO: We should create a new one from the ground up when making new sky renderers.
            m_skyRenderer.Clear();
            m_lineDrawnTracker.UpdateToWorld(world);
            PreloadAllTextures(world);

            m_vertexLookup = new LegacyVertex[world.Sides.Count][];
            m_vertexLowerLookup = new LegacyVertex[world.Sides.Count][];
            m_vertexUpperLookup = new LegacyVertex[world.Sides.Count][];
            m_skyWallVertexLowerLookup = new SkyGeometryVertex[world.Sides.Count][];
            m_skyWallVertexUpperLookup = new SkyGeometryVertex[world.Sides.Count][];
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

        private static void PreloadAllTextures(IWorld world)
        {
            var textures = world.Lines.SelectMany(line => line.Sides)
                .SelectMany(side => side.Walls)
                .Select(wall => wall.TextureHandle)
                .Distinct().ToList();

            var flatTextures = world.Sectors.SelectMany(sector => new[] { sector.Ceiling, sector.Floor })
                .Select(flat => flat.TextureHandle)
                .Distinct();
            textures.AddRange(flatTextures);

            TextureManager.Instance.LoadTextureImages(textures);
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
                    if (!edge.Line.Sky)
                        AddLineClip(edge);
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

                edge.Line.Sky = m_skyOverride;
                if (!m_skyOverride)
                    AddLineClip(edge);
            }
        }

        private void AddLineClip(SubsectorSegment edge)
        {
            if (edge.Line.OneSided)
                m_viewClipper.AddLine(edge.Start, edge.End);
            else if (LineOpening.IsRenderingBlocked(edge.Line))
                m_viewClipper.AddLine(edge.Start, edge.End);
        }

        private void RenderSide(Side side, bool isFrontSide)
        {
            m_skyOverride = false;
            if (!(side is TwoSided twoSided))
                RenderOneSided(side);
            else
                RenderTwoSided(twoSided, isFrontSide);
        }

        private void RenderOneSided(Side side)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            GLLegacyTexture texture = m_textureManager.GetTexture(side.Middle.TextureHandle);
            LegacyVertex[]? data = m_vertexLookup[side.Id];

            RenderSkySide(side, null, texture);

            if (side.OffsetChanged || side.Sector.DataChanged || data == null)
            {
                WallVertices wall = WorldTriangulator.HandleOneSided(side, texture.UVInverse, m_tickFraction);
                data = GetWallVertices(wall, side.Sector.LightLevel / 256.0f);
                m_vertexLookup[side.Id] = data;
            }
            else if (side.Sector.LightingChanged)
            {
                SetLightToVertices(data, side.Sector.LightLevel / 256.0f);
            }

            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);
            renderData.Vbo.Add(data);
        }

        private void SetLightToVertices(LegacyVertex[] data, float lightLevel)
        {
            for (int i = 0; i < data.Length; i++)
                data[i].LightLevelUnit = lightLevel;
        }

        private void RenderTwoSided(TwoSided facingSide, bool isFrontSide)
        {
            TwoSided otherSide = facingSide.PartnerSide;
            Sector facingSector = facingSide.Sector;
            Sector otherSector = otherSide.Sector;

            if (LowerIsVisible(facingSector, otherSector))
                RenderTwoSidedLower(facingSide, otherSide, isFrontSide);
            if (facingSide.Middle.TextureHandle != Constants.NoTextureIndex)
                RenderTwoSidedMiddle(facingSide, otherSide, isFrontSide);
            if (UpperIsVisible(facingSide, facingSector, otherSector))
                RenderTwoSidedUpper(facingSide, otherSide, isFrontSide);
        }

        private bool LowerIsVisible(Sector facingSector, Sector otherSector)
        {
            double facingZ = facingSector.Floor.PrevZ.Interpolate(facingSector.Floor.Z, m_tickFraction);
            double otherZ = otherSector.Floor.PrevZ.Interpolate(otherSector.Floor.Z, m_tickFraction);
            return facingZ < otherZ;
        }

        private bool UpperIsVisible(TwoSided facingSide, Sector facingSector, Sector otherSector)
        {
            if (TextureManager.Instance.IsSkyTexture(facingSector.Ceiling.TextureHandle))
            {
                if (TextureManager.Instance.IsSkyTexture(otherSector.Ceiling.TextureHandle))
                {
                    // The sky is only drawn if there is no opening height
                    // Otherwise ignore this line for sky effects
                    return LineOpening.GetOpeningHeight(facingSide.Line) <= 0;
                }
                // Assume upper is visible for sky rendering hacks
                return true;
            }

            double facingZ = facingSector.Ceiling.PrevZ.Interpolate(facingSector.Ceiling.Z, m_tickFraction);
            double otherZ = otherSector.Ceiling.PrevZ.Interpolate(otherSector.Ceiling.Z, m_tickFraction);
            return facingZ > otherZ;
        }

        private void RenderTwoSidedLower(TwoSided facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            SectorPlane plane = otherSide.Sector.Floor;
            bool isSky = TextureManager.Instance.IsSkyTexture(plane.TextureHandle);
            Wall lowerWall = facingSide.Lower;

            bool skyRender = isSky && TextureManager.Instance.IsSkyTexture(otherSide.Sector.Floor.TextureHandle);
            if (lowerWall.TextureHandle == Constants.NoTextureIndex && !skyRender)
                return;

            GLLegacyTexture texture = m_textureManager.GetTexture(lowerWall.TextureHandle);
            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

            if (isSky)
            {
                SkyGeometryVertex[]? data = m_skyWallVertexLowerLookup[facingSide.Id];

                if (facingSide.OffsetChanged || facingSide.Sector.DataChanged || otherSide.Sector.DataChanged || data == null)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction);
                    data = CreateSkyWallVertices(wall);
                    m_skyWallVertexLowerLookup[facingSide.Id] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[]? data = m_vertexLowerLookup[facingSide.Id];

                if (facingSide.OffsetChanged || facingSide.Sector.DataChanged || otherSide.Sector.DataChanged || data == null)
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

        private void RenderTwoSidedUpper(TwoSided facingSide, TwoSided otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            SectorPlane plane = otherSide.Sector.Ceiling;
            bool isSky = TextureManager.Instance.IsSkyTexture(plane.TextureHandle);
            Wall upperWall = facingSide.Upper;

            if (!TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle) && upperWall.TextureHandle == Constants.NoTextureIndex)
                return;

            GLLegacyTexture texture = m_textureManager.GetTexture(upperWall.TextureHandle);
            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

            RenderSkySide(facingSide, facingSide, texture);

            if (isSky)
            {
                SkyGeometryVertex[]? data = m_skyWallVertexUpperLookup[facingSide.Id];

                if (TextureManager.Instance.IsSkyTexture(otherSide.Sector.Ceiling.TextureHandle))
                {
                    m_skyOverride = true;
                    return;
                }

                if (facingSide.OffsetChanged || facingSide.OffsetChanged || facingSide.Sector.DataChanged || otherSide.Sector.DataChanged || data == null)
                {
                    WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, otherSide, texture.UVInverse,
                        isFrontSide, m_tickFraction, MaxSky);
                    data = CreateSkyWallVertices(wall);
                    m_skyWallVertexUpperLookup[facingSide.Id] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[]? data = m_vertexUpperLookup[facingSide.Id];

                if (facingSide.OffsetChanged || facingSide.Sector.DataChanged || otherSide.Sector.DataChanged || data == null)
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

        private void RenderSkySide(Side facingSide, TwoSided? twoSided, GLLegacyTexture texture)
        {
            if (!TextureManager.Instance.IsSkyTexture(facingSide.Sector.Ceiling.TextureHandle))
                return;

            bool isFront = twoSided == null || twoSided.IsFront;
            WallVertices wall;

            if (twoSided != null && LineOpening.IsRenderingBlocked(twoSided.Line) && twoSided.Upper.TextureHandle == Constants.NoTextureIndex)
            {
                wall = WorldTriangulator.HandleOneSided(facingSide, texture.UVInverse, m_tickFraction,
                    overrideFloor: twoSided.PartnerSide.Sector.Floor.Z, overrideCeiling: MaxSky, isFront);
            }
            else
            {
                wall = WorldTriangulator.HandleOneSided(facingSide, texture.UVInverse, m_tickFraction,
                    overrideFloor: facingSide.Sector.Ceiling.Z, overrideCeiling: MaxSky, isFront);
            }

            var skyData = CreateSkyWallVertices(wall);
            m_skyRenderer.DefaultSky.Add(skyData);
        }

        private void RenderTwoSidedMiddle(TwoSided facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            Wall middleWall = facingSide.Middle;
            GLLegacyTexture texture = m_textureManager.GetTexture(middleWall.TextureHandle);
            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);
            LegacyVertex[]? data = m_vertexLookup[facingSide.Id];

            if (facingSide.OffsetChanged || facingSide.Sector.DataChanged || otherSide.Sector.DataChanged || data == null)
            {
                (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSide.Sector, otherSide.Sector);
                WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide,
                    texture.Dimension, texture.UVInverse, bottomZ, topZ, isFrontSide, out bool nothingVisible, m_tickFraction);

                // If the texture can't be drawn because the level has offsets that
                // are messed up (ex: offset causes it to be completely missing) we
                // can exit early since nothing can be drawn.
                if (nothingVisible)
                    data = Array.Empty<LegacyVertex>();
                else
                    data = GetWallVertices(wall, facingSide.Sector.LightLevel / 256.0f);

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
            bool isSky = TextureManager.Instance.IsSkyTexture(flat.TextureHandle);
            GLLegacyTexture texture = m_textureManager.GetTexture(flat.TextureHandle);
            RenderWorldData renderData = m_worldDataManager.GetRenderData(texture);

            if (isSky)
            {
                SkyGeometryVertex[]? data = floor ? m_skyFloorVertexLookup[subsector.Id] : m_skyCeilingVertexLookup[subsector.Id];

                if (flat.Sector.DataChanged || data == null)
                {
                    // TODO: A lot of calculations aren't needed for sky coordinates, waste of computation.
                    // Note that the subsector triangulator is supposed to realize when
                    // we're passing it a floor or ceiling and order the vertices for
                    // us such that it's always in counter-clockwise order.
                    WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices,
                        floor ? flat.Z : MaxSky);
                    WorldVertex root = m_subsectorVertices[0];
                    List<SkyGeometryVertex> subData = new List<SkyGeometryVertex>();
                    for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                    {
                        WorldVertex second = m_subsectorVertices[i];
                        WorldVertex third = m_subsectorVertices[i + 1];
                        subData.AddRange(CreateSkyFlatVertices(root, second, third));
                    }

                    data = subData.ToArray();
                    if (floor)
                        m_skyFloorVertexLookup[subsector.Id] = data;
                    else
                        m_skyCeilingVertexLookup[subsector.Id] = data;
                }

                m_skyRenderer.DefaultSky.Add(data);
            }
            else
            {
                LegacyVertex[]? data = floor ? m_vertexFloorLookup[subsector.Id] : m_vertexCeilingLookup[subsector.Id];

                if (flat.Sector.DataChanged || data == null)
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
                        m_vertexFloorLookup[subsector.Id] = data;
                    else
                        m_vertexCeilingLookup[subsector.Id] = data;
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
            data[0].Alpha = 1.0f;
            data[0].R = 1.0f;
            data[0].G = 1.0f;
            data[0].B = 1.0f;
            data[0].Fuzz = 0;

            data[1].LightLevelUnit = lightLevel;
            data[1].X = wv.BottomLeft.X;
            data[1].Y = wv.BottomLeft.Y;
            data[1].Z = wv.BottomLeft.Z;
            data[1].U = wv.BottomLeft.U;
            data[1].V = wv.BottomLeft.V;
            data[1].Alpha = 1.0f;
            data[1].R = 1.0f;
            data[1].G = 1.0f;
            data[1].B = 1.0f;
            data[1].Fuzz = 0;

            data[2].LightLevelUnit = lightLevel;
            data[2].X = wv.TopRight.X;
            data[2].Y = wv.TopRight.Y;
            data[2].Z = wv.TopRight.Z;
            data[2].U = wv.TopRight.U;
            data[2].V = wv.TopRight.V;
            data[2].Alpha = 1.0f;
            data[2].R = 1.0f;
            data[2].G = 1.0f;
            data[2].B = 1.0f;
            data[2].Fuzz = 0;

            data[3].LightLevelUnit = lightLevel;
            data[3].X = wv.TopRight.X;
            data[3].Y = wv.TopRight.Y;
            data[3].Z = wv.TopRight.Z;
            data[3].U = wv.TopRight.U;
            data[3].V = wv.TopRight.V;
            data[3].Alpha = 1.0f;
            data[3].R = 1.0f;
            data[3].G = 1.0f;
            data[3].B = 1.0f;
            data[3].Fuzz = 0;

            data[4].LightLevelUnit = lightLevel;
            data[4].X = wv.BottomLeft.X;
            data[4].Y = wv.BottomLeft.Y;
            data[4].Z = wv.BottomLeft.Z;
            data[4].U = wv.BottomLeft.U;
            data[4].V = wv.BottomLeft.V;
            data[4].Alpha = 1.0f;
            data[4].R = 1.0f;
            data[4].G = 1.0f;
            data[4].B = 1.0f;
            data[4].Fuzz = 0;

            data[5].LightLevelUnit = lightLevel;
            data[5].X = wv.BottomRight.X;
            data[5].Y = wv.BottomRight.Y;
            data[5].Z = wv.BottomRight.Z;
            data[5].U = wv.BottomRight.U;
            data[5].V = wv.BottomRight.V;
            data[5].Alpha = 1.0f;
            data[5].R = 1.0f;
            data[5].G = 1.0f;
            data[5].B = 1.0f;
            data[5].Fuzz = 0;

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
            data[0].Alpha = 1.0f;
            data[0].R = 1.0f;
            data[0].G = 1.0f;
            data[0].B = 1.0f;
            data[0].Fuzz = 0;

            data[1].LightLevelUnit = lightLevel;
            data[1].X = second.X;
            data[1].Y = second.Y;
            data[1].Z = second.Z;
            data[1].U = second.U;
            data[1].V = second.V;
            data[1].Alpha = 1.0f;
            data[1].R = 1.0f;
            data[1].G = 1.0f;
            data[1].B = 1.0f;
            data[1].Fuzz = 0;

            data[2].LightLevelUnit = lightLevel;
            data[2].X = third.X;
            data[2].Y = third.Y;
            data[2].Z = third.Z;
            data[2].U = third.U;
            data[2].V = third.V;
            data[2].Alpha = 1.0f;
            data[2].R = 1.0f;
            data[2].G = 1.0f;
            data[2].B = 1.0f;
            data[2].Fuzz = 0;

            return data;
        }

        private void ReleaseUnmanagedResources()
        {
            m_skyRenderer.Dispose();
        }
    }
}
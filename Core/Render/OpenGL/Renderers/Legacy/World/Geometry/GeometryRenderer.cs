using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Subsectors;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry
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
            RenderFlats(subsector);
        }
        
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void PreloadAllTextures(WorldBase world)
        {
            var textures =
            world.Lines.SelectMany(line => line.Sides)
                .SelectMany(side => side.Walls)
                .Select(wall => wall.Texture)
                .Distinct().ToList();

            textures.AddRange(
            world.Sectors.SelectMany(sector => new[] { sector.Ceiling, sector.Floor })
                .Select(flat => flat.Texture)
                .Distinct());

            TextureManager.Instance.LoadTextureImages(textures);
        }

        private void RenderWalls(Subsector subsector, in Vec2D position)
        {
            List<SubsectorSegment> edges = subsector.ClockwiseEdges;
            for (int i = 0; i < edges.Count; i++)
            {
                SubsectorSegment edge = edges[i];
                if (edge.Line == null || m_lineDrawnTracker.HasDrawn(edge.Line))
                    continue;

                bool onFrontSide = edge.Line.Segment.OnRight(position);
                if (!onFrontSide && edge.Line.OneSided)
                    continue;

                Side? side = onFrontSide ? edge.Line.Front : edge.Line.Back;
                if (side == null)
                    throw new NullReferenceException("Trying to draw the wrong side of a one sided line (or a miniseg)");

                RenderSide(side, onFrontSide);
                
                m_lineDrawnTracker.MarkDrawn(edge.Line);
                if (edge.Line.OneSided)
                    m_viewClipper.AddLine(edge.Start, edge.End);
            }
        }

        private void RenderSide(Side side, bool isFrontSide)
        {
            if (!(side is TwoSided twoSided))
                RenderOneSided(side);
            else
                RenderTwoSided(twoSided, isFrontSide);
        }

        private void RenderOneSided(Side side)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            Sector sector = side.Sector;
            short lightLevel = sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetTexture(side.Middle.Texture);
            WallVertices wall = WorldTriangulator.HandleOneSided(side, texture.UVInverse, m_tickFraction);
            
            RenderWorldData renderData = m_worldDataManager[texture];
            
            // Our triangle is added like:
            //    0--2
            //    | /  3
            //    |/  /|
            //    1  / |
            //      4--5
            // TODO: Do some kind of stackalloc here to avoid calling it 6x.
            renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));
        }

        private void RenderTwoSided(TwoSided facingSide, bool isFrontSide)
        {
            TwoSided otherSide = facingSide.PartnerSide; 
            Sector facingSector = facingSide.Sector;
            Sector otherSector = otherSide.Sector;

            if (LowerIsVisible(facingSector, otherSector))
                RenderTwoSidedLower(facingSide, otherSide, isFrontSide);
            if (facingSide.Middle.Texture != Constants.NoTextureIndex)
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

        private void RenderTwoSidedLower(TwoSided facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            bool isSky = TextureManager.Instance.IsSkyTexture(otherSide.Sector.Floor.Texture);
            short lightLevel = facingSide.Sector.LightLevel;

            GLLegacyTexture texture = m_textureManager.GetTexture(facingSide.Lower.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(facingSide, otherSide, texture.UVInverse, 
                isFrontSide, m_tickFraction);
            
            if (isSky)
            {
                m_skyRenderer.DefaultSky.Add(wall.TopLeft, wall.BottomLeft, wall.TopRight);
                m_skyRenderer.DefaultSky.Add(wall.TopRight, wall.BottomLeft, wall.BottomRight);
            }
            else
            {
                // See RenderOneSided() for an ASCII image of why we do this.
                // TODO: Do some kind of stackalloc here to avoid calling it 6x.
                renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));
            }
        }

        private void RenderTwoSidedMiddle(TwoSided facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSide.Sector, otherSide.Sector);
            short lightLevel = facingSide.Sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetTexture(facingSide.Middle.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(facingSide, otherSide, 
                texture.Dimension, texture.UVInverse, bottomZ, topZ, isFrontSide, out bool nothingVisible);
            
            // If the texture can't be drawn because the level has offsets that
            // are messed up (ex: offset causes it to be completely missing) we
            // can exit early since nothing can be drawn.
            if (nothingVisible)
                return;
            
            // See RenderOneSided() for an ASCII image of why we do this.
            // TODO: Do some kind of stackalloc here to avoid calling it 6x.
            renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));
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

        private void RenderTwoSidedUpper(TwoSided facingSide, TwoSided otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            bool isSky = TextureManager.Instance.IsSkyTexture(otherSide.Sector.Ceiling.Texture);
            short lightLevel = facingSide.Sector.LightLevel;

            GLLegacyTexture texture = m_textureManager.GetTexture(facingSide.Upper.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(facingSide, otherSide, texture.UVInverse, 
                isFrontSide, m_tickFraction);

            if (isSky)
            {
                m_skyRenderer.DefaultSky.Add(wall.TopLeft, wall.BottomLeft, wall.TopRight);
                m_skyRenderer.DefaultSky.Add(wall.TopRight, wall.BottomLeft, wall.BottomRight);
            }
            else
            {
                // See RenderOneSided() for an ASCII image of why we do this.
                // TODO: Do some kind of stackalloc here to avoid calling it 6x.
                renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
                renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));   
            }
        }

        private void RenderFlats(Subsector subsector)
        {
            RenderFlat(subsector, subsector.Sector.Floor);
            RenderFlat(subsector, subsector.Sector.Ceiling);
        }

        private void RenderFlat(Subsector subsector, SectorPlane flat)
        {
            // TODO: If we can't see it (dot product the plane) then exit.
            bool isSky = TextureManager.Instance.IsSkyTexture(flat.Texture);

            // TODO: A lot of calculations aren't needed for sky coordinates, waste of computation.
            short lightLevel = flat.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetTexture(flat.Texture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            // Note that the subsector triangulator is supposed to realize when
            // we're passing it a floor or ceiling and order the vertices for
            // us such that it's always in counter-clockwise order.
            WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices);

            WorldVertex root = m_subsectorVertices[0];

            if (isSky)
            {
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];
                    m_skyRenderer.DefaultSky.Add(root, second, third);
                }
            }
            else
            {
                for (int i = 1; i < m_subsectorVertices.Length - 1; i++)
                {
                    WorldVertex second = m_subsectorVertices[i];
                    WorldVertex third = m_subsectorVertices[i + 1];

                    // TODO: Do some kind of stackalloc here to avoid calling it 3x.
                    renderData.Vbo.Add(new LegacyVertex(root, lightLevel)); 
                    renderData.Vbo.Add(new LegacyVertex(second, lightLevel));
                    renderData.Vbo.Add(new LegacyVertex(third, lightLevel));
                }   
            }
        }

        private void ReleaseUnmanagedResources()
        {
            m_skyRenderer.Dispose();
        }
    }
}
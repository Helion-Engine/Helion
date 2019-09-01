using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using MoreLinq;
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

        public void RenderSubsector(Subsector subsector, Vec2D position)
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
            world.Map.Lines.SelectMany(GetSides)
                .SelectMany(side => new[] { side.UpperTexture.ToString(), side.MiddleTexture.ToString(), side.LowerTexture.ToString() })
                .Distinct()
                .ForEach(texName => m_textureManager.GetWall(texName));

            world.Map.Sectors.SelectMany(sector => new[] { sector.Ceiling, sector.Floor })
                .Select(flat => flat.Texture.ToString())
                .Distinct()
                .ForEach(texName => m_textureManager.GetFlat(texName));

            // TODO: Can we work this into the Line object itself?
            Side[] GetSides(Line line) => line.Back != null ? new[] { line.Front, line.Back } : new[] { line.Front };
        }

        private void RenderWalls(Subsector subsector, Vec2D position)
        {
            List<SubsectorEdge> edges = subsector.ClockwiseEdges;
            for (int i = 0; i < edges.Count; i++)
            {
                SubsectorEdge edge = edges[i];
                if (edge.Line == null || m_lineDrawnTracker.HasDrawn(edge.Line))
                    continue;

                bool onFrontSide = edge.Line.Segment.OnRight(position);
                if (!onFrontSide && edge.Line.OneSided)
                    continue;

                Side? side = onFrontSide ? edge.Line.Front : edge.Line.Back;
                if (side == null)
                    throw new NullReferenceException("Trying to draw the wrong side of a one sided line (or a miniseg)");

                RenderSide(edge.Line, side, onFrontSide);
                
                m_lineDrawnTracker.MarkDrawn(edge.Line);
                if (edge.Line.OneSided)
                    m_viewClipper.AddLine(edge.Start, edge.End);
            }
        }

        private void RenderSide(Line line, Side side, bool isFrontSide)
        {
            // TODO: All of the following functions and their children can be heavily refactored!
            if (side.PartnerSide == null)
                RenderOneSided(line, side);
            else
                RenderTwoSided(line, side, side.PartnerSide, isFrontSide);
        }

        private void RenderOneSided(Line line, Side side)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            Sector sector = side.Sector;
            short lightLevel = sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetWall(side.MiddleTexture);
            WallVertices wall = WorldTriangulator.HandleOneSided(line, side, texture.UVInverse, m_tickFraction);
            
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

        private void RenderTwoSided(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            Sector facingSector = facingSide.Sector;
            Sector otherSector = otherSide.Sector;

            if (LowerIsVisible(facingSector, otherSector))
                RenderTwoSidedLower(line, facingSide, otherSide, isFrontSide);
            if (facingSide.MiddleTexture != Constants.NoTexture)
                RenderTwoSidedMiddle(line, facingSide, otherSide, isFrontSide);
            if (UpperIsVisible(facingSector, otherSector))
                RenderTwoSidedUpper(line, facingSide, otherSide, isFrontSide);
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

        private void RenderTwoSidedLower(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            bool isSky = otherSide.Sector.Floor.Texture == Constants.SkyTexture;
            short lightLevel = facingSide.Sector.LightLevel;
            
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.LowerTexture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(line, facingSide, otherSide, 
                texture.UVInverse, isFrontSide, m_tickFraction);
            
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

        private void RenderTwoSidedMiddle(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            Precondition(facingSide.MiddleTexture != Constants.NoTexture, "Should not be rendering a two sided middle with no texture");

            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSide.Sector, otherSide.Sector);
            short lightLevel = facingSide.Sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.MiddleTexture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedMiddle(line, facingSide, otherSide, 
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
            SectorFlat facingFloor = facingSector.Floor;
            SectorFlat facingCeiling = facingSector.Ceiling;
            SectorFlat otherFloor = otherSector.Floor;
            SectorFlat otherCeiling = otherSector.Ceiling;

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

        private void RenderTwoSidedUpper(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            // TODO: If we can't see it (dot product and looking generally horizontally), don't draw it.
            
            bool isSky = otherSide.Sector.Ceiling.Texture == Constants.SkyTexture;
            short lightLevel = facingSide.Sector.LightLevel;
            
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.UpperTexture);
            RenderWorldData renderData = m_worldDataManager[texture];
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(line, facingSide, otherSide, 
                texture.UVInverse, isFrontSide, m_tickFraction);

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
            List<SectorFlat> flats = subsector.Sector.Flats;
            for (int i = 0; i < flats.Count; i++)
                RenderFlat(subsector, flats[i]);
        }

        private void RenderFlat(Subsector subsector, SectorFlat flat)
        {
            // TODO: If we can't see it (dot product the plane) then exit.
            
            bool isSky = flat.Texture == Constants.SkyTexture;

            // TODO: A lot of calculations aren't needed for sky coordinates, waste of computation.
            short lightLevel = flat.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetFlat(flat.Texture);
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
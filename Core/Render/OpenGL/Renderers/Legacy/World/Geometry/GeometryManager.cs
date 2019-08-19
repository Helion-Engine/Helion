using System;
using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry
{
    public class GeometryManager : IDisposable
    {
        private readonly IGLFunctions gl;
        private readonly GLCapabilities m_capabilities;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly Dictionary<GLLegacyTexture, RenderGeometryData> m_textureToGeometry = new Dictionary<GLLegacyTexture, RenderGeometryData>();
        private readonly LineDrawnTracker m_lineDrawnTracker = new LineDrawnTracker();
        private readonly DynamicArray<WorldVertex> m_subsectorVertices = new DynamicArray<WorldVertex>();
        private double m_tickFraction;
        
        public GeometryManager(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            m_capabilities = capabilities;
            gl = functions;
            m_textureManager = textureManager;
        }
        
        ~GeometryManager()
        {
            ReleaseUnmanagedResources();
        }

        public void UpdateTo(WorldBase world)
        {
            m_textureToGeometry.Values.ForEach(geometryData => geometryData.Dispose());
            m_textureToGeometry.Clear();
            
            m_lineDrawnTracker.UpdateToWorld(world);
        }

        public void Render(WorldBase world, RenderInfo renderInfo)
        {
            m_lineDrawnTracker.ClearDrawnLines();
            
            m_textureToGeometry.Values.ForEach(geometryData => geometryData.Clear());

            m_tickFraction = renderInfo.TickFraction;
            Vec2D position = renderInfo.Camera.Position.To2D().ToDouble();
            RecursivelyRenderBSP(world.BspTree.Root, position, world);
            
            m_textureToGeometry.Values.ForEach(geometryData => geometryData.Draw());
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void RecursivelyRenderBSP(BspNodeCompact node, Vec2D position, WorldBase world)
        {
            // TODO: This is probably a performance issue, consider optimizing.
            // TODO: Consider changing to xor trick to avoid branching?
            if (node.Splitter.OnRight(position))
            {
                if (node.IsRightSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector], position);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.RightChild], position, world);
                
                if (node.IsLeftSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector], position);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.LeftChild], position, world);
            }
            else
            {
                if (node.IsLeftSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector], position);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.LeftChild], position, world);
                
                if (node.IsRightSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector], position);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.RightChild], position, world);
            }
        }

        private void RenderSubsector(Subsector subsector, Vec2D position)
        {
            RenderWalls(subsector, position);
            RenderFlats(subsector);
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
            }
        }

        private void RenderSide(Line line, Side side, bool isFrontSide)
        {
            // TODO: All of the following functions and their children can be
            //       heavily refactored!
            if (side.PartnerSide == null)
                RenderOneSided(line, side);
            else
                RenderTwoSided(line, side, side.PartnerSide, isFrontSide);
        }

        private void RenderOneSided(Line line, Side side)
        {
            Sector sector = side.Sector;
            byte lightLevel = sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetWall(side.MiddleTexture);
            WallVertices wall = WorldTriangulator.HandleOneSided(line, side, texture.UVInverse, m_tickFraction);
            
            RenderGeometryData renderData = FindOrCreateRenderGeometryData(texture);
            
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

            if (facingSector.Floor.Z < otherSector.Floor.Z)
                RenderTwoSidedLower(line, facingSide, otherSide, isFrontSide);
            if (facingSide.MiddleTexture != Constants.NoTexture)
                RenderTwoSidedMiddle(line, facingSide, otherSide, isFrontSide);
            if (facingSector.Ceiling.Z > otherSector.Ceiling.Z)
                RenderTwoSidedUpper(line, facingSide, otherSide, isFrontSide);
        }

        private void RenderTwoSidedLower(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            byte lightLevel = facingSide.Sector.LightLevel;
            
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.LowerTexture);
            RenderGeometryData renderData = FindOrCreateRenderGeometryData(texture);
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedLower(line, facingSide, otherSide, 
                texture.UVInverse, isFrontSide, m_tickFraction);
            
            // See RenderOneSided() for an ASCII image of why we do this.
            // TODO: Do some kind of stackalloc here to avoid calling it 6x.
            renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));
        }

        private void RenderTwoSidedMiddle(Line line, Side facingSide, Side otherSide, bool isFrontSide)
        {
            Precondition(facingSide.MiddleTexture != Constants.NoTexture, "Should not be rendering a two sided middle with no texture");

            (double bottomZ, double topZ) = FindOpeningFlatsInterpolated(facingSide.Sector, otherSide.Sector);
            byte lightLevel = facingSide.Sector.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.MiddleTexture);
            RenderGeometryData renderData = FindOrCreateRenderGeometryData(texture);
            
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
            byte lightLevel = facingSide.Sector.LightLevel;
            
            GLLegacyTexture texture = m_textureManager.GetWall(facingSide.UpperTexture);
            RenderGeometryData renderData = FindOrCreateRenderGeometryData(texture);
            
            WallVertices wall = WorldTriangulator.HandleTwoSidedUpper(line, facingSide, otherSide, 
                texture.UVInverse, isFrontSide, m_tickFraction);
            
            // See RenderOneSided() for an ASCII image of why we do this.
            // TODO: Do some kind of stackalloc here to avoid calling it 6x.
            renderData.Vbo.Add(new LegacyVertex(wall.TopLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.TopRight, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomLeft, lightLevel));
            renderData.Vbo.Add(new LegacyVertex(wall.BottomRight, lightLevel));
        }

        private void RenderFlats(Subsector subsector)
        {
            List<SectorFlat> flats = subsector.Sector.Flats;
            for (int i = 0; i < flats.Count; i++)
                RenderFlat(subsector, flats[i]);
        }

        private void RenderFlat(Subsector subsector, SectorFlat flat)
        {
            byte lightLevel = flat.LightLevel;
            GLLegacyTexture texture = m_textureManager.GetFlat(flat.Texture);
            RenderGeometryData renderData = FindOrCreateRenderGeometryData(texture);
            
            // Note that the subsector triangulator is supposed to realize when
            // we're passing it a floor or ceiling and order the vertices for
            // us such that it's always in counter-clockwise order.
            WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension, m_tickFraction, m_subsectorVertices);

            WorldVertex root = m_subsectorVertices[0];
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

        private RenderGeometryData FindOrCreateRenderGeometryData(GLLegacyTexture texture)
        {
            if (m_textureToGeometry.TryGetValue(texture, out RenderGeometryData data))
                return data;
            
            RenderGeometryData newData = new RenderGeometryData(m_capabilities, gl, texture);
            m_textureToGeometry[texture] = newData;
            return newData;
        }

        private void ReleaseUnmanagedResources()
        {
            m_textureToGeometry.Values.ForEach(geometryData => geometryData.Dispose());
        }
    }
}
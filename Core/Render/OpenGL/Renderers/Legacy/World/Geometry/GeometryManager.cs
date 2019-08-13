using System;
using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using MoreLinq;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry
{
    public class GeometryManager : IDisposable
    {
        private readonly IGLFunctions gl;
        private readonly GLCapabilities m_capabilities;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly Dictionary<GLLegacyTexture, RenderGeometryData> m_textureToGeometry = new Dictionary<GLLegacyTexture, RenderGeometryData>();

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
        }

        public void Render(WorldBase world, RenderInfo renderInfo)
        {
            m_textureToGeometry.Values.ForEach(geometryData => geometryData.Clear());
            
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
                    RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector]);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.RightChild], position, world);
                
                if (node.IsLeftSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector]);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.LeftChild], position, world);
            }
            else
            {
                if (node.IsLeftSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.LeftChildAsSubsector]);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.LeftChild], position, world);
                
                if (node.IsRightSubsector)
                    RenderSubsector(world.BspTree.Subsectors[node.RightChildAsSubsector]);
                else
                    RecursivelyRenderBSP(world.BspTree.Nodes[node.RightChild], position, world);
            }
        }

        private void RenderSubsector(Subsector subsector)
        {
            RenderWalls(subsector);
            RenderFlats(subsector);
        }

        private void RenderWalls(Subsector subsector)
        {
            // TODO
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
            RenderGeometryData renderData = FindRenderGeometryData(texture);
            
            // Note that the subsector triangulator is supposed to realize when
            // we're passing it a floor or ceiling and order the vertices for
            // us such that it's always in counter-clockwise order.
            List<WorldVertex> vertices = WorldTriangulator.HandleSubsector(subsector, flat, texture.Dimension);

            WorldVertex root = vertices[0];
            for (int i = 1; i < vertices.Count - 1; i++)
            {
                WorldVertex second = vertices[i];
                WorldVertex third = vertices[i + 1];
                
                renderData.Vbo.Add(new LegacyVertex(root, lightLevel), 
                    new LegacyVertex(second, lightLevel),
                    new LegacyVertex(third, lightLevel));
            }
        }

        private RenderGeometryData FindRenderGeometryData(GLLegacyTexture texture)
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
using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Bsp.Geometry;
using Helion.Bsp.Node;
using Helion.Maps;
using Helion.Maps.Components.GL;
using Helion.Util.Assertion;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using NLog;
using GLBspNode = Helion.Maps.Components.GL.GLNode;

// TODO: We can refactor the read[Geometry]V[N] functions into a unified one.
//       There's very little differences between ReadAbcV1 and ReadAbcV2.

namespace Helion.Bsp.Builder.GLBSP
{
    /// <summary>
    /// An implementation of a BSP builder that takes GL nodes from the GLBSP
    /// application and builds a BSP tree from it that we can use.
    /// </summary>
    public class GLBspBuilder : IBspBuilder
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private readonly List<GLVertex> m_glVertices = new();
        private readonly List<BspSegment> m_segments = new();
        private readonly List<BspNode> m_subsectors = new();
        private readonly List<BspNode> m_nodes = new();
        private readonly IMap m_map;

        public GLBspBuilder(IMap map)
        {
            m_map = map;
        }

        public BspNode? Build()
        {
            if (m_map.GL == null)
                return null;

            if (m_map.GL.Subsectors.Empty())
            {
                log.Warn("Cannot make BSP tree from a map with zero subsectors");
                return null;
            }

            try
            {
                CreateVertices(m_map.GL.Vertices);
                CreateSegments(m_map.GL.Segments);
                CreateSubsectors(m_map.GL.Subsectors);
                CreateNodes(m_map.GL.Nodes);
            }
            catch (AssertionException)
            {
                // We want to catch everything except assertion exceptions as
                // they should never be triggered unless we screwed up.
                throw;
            }
            catch (Exception e)
            {
                log.Error("Cannot read GLBSP data, components are malformed (reason: {0})", e.Message);
                return null;
            }

            BspNode root = m_nodes[^1];
            return root.IsDegenerate ? null : root;
        }

        private void CreateVertices(List<Vec2D> glVertices)
        {
            IEnumerable<GLVertex> vertices = glVertices.Select(v => new GLVertex(new Fixed(v.X), new Fixed(v.Y)));
            m_glVertices.AddRange(vertices);
        }

        private void CreateSegments(List<GLSegment> segments)
        {
            // TODO
        }

        private void CreateSubsectors(List<GLSubsector> subsectors)
        {
            // TODO
        }

        private void CreateNodes(List<GLBspNode> glNodes)
        {
            // TODO
        }
    }
}
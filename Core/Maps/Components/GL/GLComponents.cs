using System.Collections.Generic;
using Helion.Util.Geometry.Vectors;
using GLNode = Helion.Maps.Components.GL.GLNode;

namespace Helion.Maps.Components.GL
{
    public class GLComponents
    {
        public readonly List<Vec2D> Vertices = new();
        public readonly List<GLSegment> Segments = new();
        public readonly List<GLSubsector> Subsectors = new();
        public readonly List<GLNode> Nodes = new();
        public int Version { get; private set; }

        private GLComponents(MapEntryCollection entryCollection)
        {
            ReadVertices(entryCollection.GLVertices!);
            ReadSegments(entryCollection.GLSegments!);
            ReadSubsectors(entryCollection.GLSubsectors!);
            ReadNodes(entryCollection.GLNodes!);
        }

        public static GLComponents? ReadOrThrow(MapEntryCollection entryCollection)
        {
            return entryCollection.HasAllGLComponents ? new GLComponents(entryCollection) : null;
        }

        private void ReadVertices(byte[] vertexData)
        {
            // TODO
        }

        private void ReadSegments(byte[] segmentData)
        {
            // TODO
        }

        private void ReadSubsectors(byte[] subsectorData)
        {
            // TODO
        }

        private void ReadNodes(byte[] nodeData)
        {
            // TODO
        }
    }
}

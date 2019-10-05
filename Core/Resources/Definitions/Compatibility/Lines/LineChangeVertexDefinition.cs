using Helion.Util.Geometry.Segments.Enums;

namespace Helion.Resources.Definitions.Compatibility.Lines
{
    public class LineChangeVertexDefinition : ILineDefinition
    {
        public int Id { get; }
        public readonly Endpoint Endpoint;
        public readonly int NewVertexId;

        public LineChangeVertexDefinition(int id, Endpoint endpoint, int newVertexId)
        {
            Id = id;
            Endpoint = endpoint;
            NewVertexId = newVertexId;
        }
    }
}
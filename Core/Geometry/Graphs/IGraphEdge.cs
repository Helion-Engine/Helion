namespace Helion.Geometry.Graphs
{
    public interface IGraphEdge
    {
        IGraphVertex GetStart();
        IGraphVertex GetEnd();
    }
}
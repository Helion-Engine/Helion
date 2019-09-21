namespace Helion.Util.Geometry.Graphs
{
    public interface IGraphEdge
    {
        IGraphVertex GetStart();
        IGraphVertex GetEnd();
    }
}
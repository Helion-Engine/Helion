using Helion.Maps.Bsp.Node;

namespace Helion.Maps.Bsp;

public interface IBspBuilder
{
    int GetNodeCount();
    int GetSubsectorCount();
    int GetSegmentCount();
    BspNode? Build();
}

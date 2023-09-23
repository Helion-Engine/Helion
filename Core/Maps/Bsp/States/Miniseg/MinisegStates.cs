using System.Collections.Generic;
using Helion.Maps.Bsp.Geometry;

namespace Helion.Maps.Bsp.States.Miniseg;

/// <summary>
/// All the states for the miniseg creator.
/// </summary>
public class MinisegStates
{
    /// <summary>
    /// The current state of the miniseg generation.
    /// </summary>
    public MinisegState State = MinisegState.Loaded;

    /// <summary>
    /// Whether we're tracing inside or outside the map in the void.
    /// </summary>
    public VoidStatus VoidStatus = VoidStatus.NotStarted;

    /// <summary>
    /// All the vertices that lay along the splitter, to which we may have
    /// to fill in with a miniseg if it's inside the level.
    /// </summary>
    public List<VertexSplitterTime> Vertices = new List<VertexSplitterTime>();

    /// <summary>
    /// All the generated minisegs.
    /// </summary>
    public List<BspSegment> Minisegs = new List<BspSegment>();

    /// <summary>
    /// The current vertex we are on.
    /// </summary>
    public int CurrentVertexListIndex = 0;
}

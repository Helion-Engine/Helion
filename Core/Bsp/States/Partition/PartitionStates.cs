using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.States.Partition;

/// <summary>
/// Stateful information for the partitioner.
/// </summary>
public class PartitionStates
{
    /// <summary>
    /// The current state of the splitter.
    /// </summary>
    public PartitionState State = PartitionState.Loaded;

    /// <summary>
    /// The splitter being used.
    /// </summary>
    public BspSegment? Splitter;

    /// <summary>
    /// The current segment in the list of segments to split that we are
    /// currently at.
    /// </summary>
    public int CurrentSegToPartitionIndex = 0;

    /// <summary>
    /// All the segments we are going to consider splitting.
    /// </summary>
    public List<BspSegment> SegsToSplit = new List<BspSegment>();

    /// <summary>
    /// Segments that are on the left of the splitter (or lines that were
    /// split and contain the left side of the split).
    /// </summary>
    public List<BspSegment> LeftSegments = new List<BspSegment>();

    /// <summary>
    /// Segments that are on the right of the splitter (or lines that were
    /// split and contain the right side of the split).
    /// </summary>
    public List<BspSegment> RightSegments = new List<BspSegment>();

    /// <summary>
    /// A series of collinear vertex indices which can be looked up in the
    /// vertex allocator.
    /// </summary>
    public HashSet<BspVertex> CollinearVertices = new HashSet<BspVertex>();
}


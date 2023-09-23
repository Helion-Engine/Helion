using System;
using Helion.Maps.Bsp.Geometry;

namespace Helion.Maps.Bsp.States.Miniseg;

/// <summary>
/// A simple wrapper around the time that a vertex was created.
/// </summary>
public class VertexSplitterTime : IComparable<VertexSplitterTime>
{
    public readonly BspVertex Vertex;

    /// <summary>
    /// The time along the splitter segment that this vertex was made.
    /// </summary>
    public readonly double SplitterTime;

    /// <summary>
    /// Creates an index/time pair.
    /// </summary>
    /// <param name="vertex">The vertex.</param>
    /// <param name="splitterTime">The time this is relative to the
    /// splitter.</param>
    public VertexSplitterTime(BspVertex vertex, double splitterTime)
    {
        Vertex = vertex;
        SplitterTime = splitterTime;
    }

    public int CompareTo(VertexSplitterTime? other) => SplitterTime.CompareTo(other?.SplitterTime);
}

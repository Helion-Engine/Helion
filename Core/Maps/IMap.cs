using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Resources.Archives;
using Helion.Util.Container;

namespace Helion.Maps;

/// <summary>
/// The interface for a map with map components. This can be loaded by a
/// things like a world, map editor, resource editor... etc.
/// </summary>
public interface IMap
{
    string Name { get; }
    MapType MapType { get; }
    public Archive Archive { get; }
    IReadOnlyList<ILine> GetLines();
    IReadOnlyList<INode> GetNodes();
    IReadOnlyList<ISector> GetSectors();
    IReadOnlyList<ISide> GetSides();
    IReadOnlyList<IThing> GetThings();
    IReadOnlyList<IVertex> GetVertices();
    GLComponents? GL { get; }
    byte[]? Reject { get; set; }
}

using System.Collections.Generic;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Resources.Archives;
using Helion.Resources.Definitions.Compatibility;

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
    public string MD5 { get; set; }
    IReadOnlyList<ILine> GetLines();
    IReadOnlyList<INode> GetNodes();
    IReadOnlyList<ISector> GetSectors();
    IReadOnlyList<ISide> GetSides();
    IReadOnlyList<IThing> GetThings();
    IReadOnlyList<IVertex> GetVertices();
    GLComponents? GL { get; }
    byte[]? Reject { get; set; }
    
    public static IMap? Read(Archive archive, MapEntryCollection mapEntries, CompatibilityMapDefinition? compatibility = null)
    {
        var map = Create(archive, mapEntries, compatibility);
        if (map != null)
            map.MD5 = mapEntries.GetMD5();
        return map;
    }

    private static IMap? Create(Archive archive, MapEntryCollection mapEntries, CompatibilityMapDefinition? compatibility = null)
    {
        return mapEntries.MapType switch
        {
            MapType.Doom => DoomMap.Create(archive, mapEntries, compatibility),
            MapType.Hexen => HexenMap.Create(archive, mapEntries, compatibility),
            _ => null
        };
    }
}

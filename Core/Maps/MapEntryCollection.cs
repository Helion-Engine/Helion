using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Bytes;
using System.IO;

namespace Helion.Maps;

/// <summary>
/// Contains all the entries that belong to a map collection. This is *not*
/// intended to be a valid set of data, but a temporary one that is being
/// built. This should never be passed around as this type, but instead you
/// should try to convert it into a ValidMapEntryCollection when you will
/// no longer be adding components.
/// </summary>
public class MapEntryCollection
{
    public string Name = "";
    public Entry? Vertices { get; set; }
    public Entry? Sectors { get; set; }
    public Entry? Sidedefs { get; set; }
    public Entry? Linedefs { get; set; }
    public Entry? Segments { get; set; }
    public Entry? Subsectors { get; set; }
    public Entry? Nodes { get; set; }
    public Entry? Things { get; set; }
    public Entry? Blockmap { get; set; }
    public Entry? Reject { get; set; }
    public Entry? Scripts { get; set; }
    public Entry? Behavior { get; set; }
    public Entry? Dialogue { get; set; }
    public Entry? Textmap { get; set; }
    public Entry? Znodes { get; set; }
    public Entry? Endmap { get; set; }
    public Entry? GLMap { get; set; }
    public Entry? GLVertices { get; set; }
    public Entry? GLSegments { get; set; }
    public Entry? GLSubsectors { get; set; }
    public Entry? GLNodes { get; set; }
    public Entry? GLPVS { get; set; }

    public string GetMD5()
    {
        using var stream = new MemoryStream();        
        if (Vertices != null)
            stream.Write(Vertices.ReadData());
        if (Things != null)
            stream.Write(Things.ReadData());
        if (Linedefs != null)
            stream.Write(Linedefs.ReadData());
        if (Sidedefs != null)
            stream.Write(Sidedefs.ReadData());
        if (Sectors != null)
            stream.Write(Sectors.ReadData());
        return Files.CalculateMD5(stream);
    }

    public bool IsDoomMap => Vertices != null && Sectors != null && Sidedefs != null && Linedefs != null && Things != null;
    public bool IsHexenMap => IsDoomMap && Behavior != null;
    public bool IsUDMFMap => Textmap != null;
    public bool HasAllGLComponents => GLVertices != null && GLSegments != null && GLSubsectors != null && GLNodes != null;
    public MapType MapType => IsUDMFMap ? MapType.UDMF : (IsHexenMap ? MapType.Hexen : MapType.Doom);

    /// <summary>
    /// Checks if this is a well formed map entry collection that is
    /// eligible to be converted into a valid map entry collection.
    /// </summary>
    /// <returns>True if it's got the required components as per map type,
    /// false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(Name))
            return false;

        return MapType switch
        {
            MapType.Doom => IsDoomMap,
            MapType.Hexen => IsHexenMap,
            MapType.UDMF => IsUDMFMap,
            _ => false,
        };
    }
}

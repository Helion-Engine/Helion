namespace Helion.Entries.Tree.Archive
{
    /// <summary>
    /// An enumeration for the different types of supported archive formats.
    /// </summary>
    public enum ArchiveType
    {
        Wad,
        Pk3
    }

    /// <summary>
    /// The type of wad from its header.
    /// </summary>
    public enum WadType
    {
        Unknown,
        Iwad,
        Pwad
    }
}

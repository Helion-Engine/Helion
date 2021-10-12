namespace Helion.Resources.Archives;

public struct WadHeader
{
    public bool IsIwad;
    public int EntryCount;
    public int DirectoryTableOffset;

    public WadHeader(bool isIwad, int entryCount, int directoryTableOffset)
    {
        IsIwad = isIwad;
        EntryCount = entryCount;
        DirectoryTableOffset = directoryTableOffset;
    }
}

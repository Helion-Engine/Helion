using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Archives;

public class WadEntry : Entry
{
    public readonly Wad Parent;
    public readonly int Offset;
    public readonly int Size;

    public WadEntry(Wad wad, int offset, int size, IEntryPath path, ResourceNamespace resourceNamespace)
        : base(path, resourceNamespace)
    {
        Parent = wad;
        Offset = offset;
        Size = size;
    }

    public override byte[] ReadData()
    {
        return Parent.ReadData(this);
    }
}


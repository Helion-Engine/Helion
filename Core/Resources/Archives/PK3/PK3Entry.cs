using System.IO.Compression;
using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Archives;

public class PK3Entry : Entry
{
    public readonly PK3 Parent;
    public readonly ZipArchiveEntry ZipEntry;

    public PK3Entry(PK3 pk3, ZipArchiveEntry zipEntry, IEntryPath path, ResourceNamespace resourceNamespace)
        : base(path, resourceNamespace)
    {
        Parent = pk3;
        ZipEntry = zipEntry;
    }

    public override byte[] ReadData()
    {
        return Parent.ReadData(this);
    }
}

using System.IO.Compression;
using Helion.Resources.Archives.Entries;

namespace Helion.Resources.Archives;

public class PK3Entry : Entry
{
    public override PK3 Parent { get; }
    public readonly ZipArchiveEntry ZipEntry;

    public PK3Entry(PK3 pk3, ZipArchiveEntry zipEntry, IEntryPath path, ResourceNamespace resourceNamespace, int index)
        : base(path, resourceNamespace, index)
    {
        Parent = pk3;
        ZipEntry = zipEntry;
    }

    public override byte[] ReadData()
    {
        return Parent.ReadData(this);
    }

    public override void ExtractToFile(string path)
    {
        ZipEntry.ExtractToFile(path, true);
    }
}

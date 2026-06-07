using Helion.Resources.Archives.Entries;
using System.IO;
using System.IO.Compression;

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
        using var stream = ZipEntry.Open();
        var entryLength = ZipEntry.Length;
        byte[] data = new byte[entryLength];
        int writeLength = 0;
        while (writeLength < entryLength)
            writeLength += stream.Read(data, writeLength, data.Length - writeLength);
        return data;
    }

    public override byte[] ReadDataAsync()
    {
        return ReadData();
    }

    public override Stream GetStream()
    {
        return ZipEntry.Open();
    }

    public override void ExtractToFile(string path)
    {
        ZipEntry.ExtractToFile(path, true);
    }
}

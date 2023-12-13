using System;
using System.IO;
using System.IO.Compression;
using Helion.Resources.Archives.Entries;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives;

/// <summary>
/// A PK3 specific archive implementation.
/// </summary>
public class PK3 : Archive, IDisposable
{
    private readonly ZipArchive m_zipArchive;
    private readonly IIndexGenerator m_indexGenerator;

    public PK3(IEntryPath path, IIndexGenerator indexGenerator) : base(path)
    {
        m_zipArchive = new(File.Open(Path.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        m_indexGenerator = indexGenerator;
        Pk3EntriesFromData();
    }

    public override void Dispose()
    {
        m_zipArchive.Dispose();
        GC.SuppressFinalize(this);
    }

    public byte[] ReadData(PK3Entry entry)
    {
        Invariant(entry.Parent == this, "Bad entry parent");

        using var stream = entry.ZipEntry.Open();
        var entryLength = entry.ZipEntry.Length;
        byte[] data = new byte[entryLength];
        int writeLength = 0;
        while(writeLength < entryLength)
            writeLength += stream.Read(data, writeLength, data.Length - writeLength);
        return data;
    }

    private static bool ZipEntryDirectory(ZipArchiveEntry entry)
    {
        bool isSeparator = entry.FullName.EndsWith(DirectorySeparatorChar) ||
                           entry.FullName.EndsWith(AltDirectorySeparatorChar);
        return entry.Length == 0 && isSeparator;
    }

    private void ZipDataToEntry(ZipArchiveEntry zipEntry)
    {
        if (ZipEntryDirectory(zipEntry))
            return;

        EntryPath entryPath = new(zipEntry.FullName);
        ResourceNamespace resourceNamespace = NamespaceFromEntryPath(entryPath.FullPath);
        Entries.Add(new PK3Entry(this, zipEntry, entryPath, resourceNamespace, m_indexGenerator.GetIndex(this)));
    }

    private void Pk3EntriesFromData()
    {
        try
        {
            foreach (var entry in m_zipArchive.Entries)
                ZipDataToEntry(entry);
        }
        catch (Exception e)
        {
            throw new Exception($"Unexpected error when reading PK3: {e.Message}");
        }
    }
}

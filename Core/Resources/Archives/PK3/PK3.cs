using System;
using System.IO;
using System.IO.Compression;
using Helion.Resources.Archives.Entries;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Archives;

/// <summary>
/// A PK3 specific archive implementation.
/// </summary>
public class PK3 : Archive, IDisposable
{
    private readonly ZipArchive m_zipArchive;

    public PK3(IEntryPath path) : base(path)
    {
        m_zipArchive = new ZipArchive(File.Open(Path.FullPath, FileMode.Open));
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
        MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream.ToArray();
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

        string forwardSlashPath = CleanPath(zipEntry.FullName);
        EntryPath entryPath = new(forwardSlashPath);
        ResourceNamespace resourceNamespace = NamespaceFromEntryPath(forwardSlashPath);
        Entries.Add(new PK3Entry(this, zipEntry, entryPath, resourceNamespace, Entries.Count));
    }

    private void Pk3EntriesFromData()
    {
        try
        {
            m_zipArchive.Entries.ForEach(ZipDataToEntry);
        }
        catch (Exception e)
        {
            throw new Exception($"Unexpected error when reading PK3: {e.Message}");
        }
    }
}

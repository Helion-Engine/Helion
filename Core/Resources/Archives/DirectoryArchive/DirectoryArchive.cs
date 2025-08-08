using System.IO;
using static Helion.Util.Assertion.Assert;
using Helion.Resources.Archives.Entries;
using NLog;
using System;

namespace Helion.Resources.Archives.Directories;

public class DirectoryArchive : Archive
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public DirectoryArchive(IEntryPath path)
        : base(path)
    {
        var fullPath = System.IO.Path.GetFullPath(path.FullPath);
        RecursivelyIterateDirectory(fullPath, fullPath);
    }

    public byte[] ReadData(DirectoryArchiveEntry entry)
    {
        Invariant(entry.Parent == this, "Bad entry parent");
        return File.ReadAllBytes(entry.FilePath);
    }

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    void RecursivelyIterateDirectory(string mainDirectory, string directory)
    {
        try
        {
            foreach (string file in Directory.GetFiles(directory))
            {
                var localPathName = file.AsSpan(mainDirectory.Length + 1);
                var resourceNamespace = NamespaceFromEntryPath(localPathName);
                var entryPath = EntryPath.CreatePathedEntry(file, resourceNamespace);
                Entries.Add(new DirectoryArchiveEntry(this, entryPath.FullPath, entryPath, resourceNamespace, Entries.Count));
            }

            foreach (string dir in Directory.GetDirectories(directory))
                RecursivelyIterateDirectory(mainDirectory, dir);
        }
        catch
        {
            Log.Error($"Failed to read directory: {directory}");
        }
    }
}

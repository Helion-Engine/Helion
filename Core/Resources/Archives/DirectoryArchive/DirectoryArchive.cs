using System.IO;
using static Helion.Util.Assertion.Assert;
using Helion.Resources.Archives.Entries;
using NLog;
using System;

namespace Helion.Resources.Archives.Directories
{
    public class DirectoryArchive : Archive
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public DirectoryArchive(IEntryPath path)
            : base(path)
        {
            RecursivelyIterateDirectory(path.FullPath);
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

        void RecursivelyIterateDirectory(string directory)
        {
            try
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    EntryPath entryPath = new(file);
                    Entries.Add(new DirectoryArchiveEntry(this, entryPath.FullPath, entryPath, NamespaceFromEntryPath(entryPath.FullPath), Entries.Count));
                }

                foreach (string dir in Directory.GetDirectories(directory))
                    RecursivelyIterateDirectory(dir);
            }
            catch
            {
                Log.Error($"Failed to read directory: {directory}");
            }
        }
    }
}

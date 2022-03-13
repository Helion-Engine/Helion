using Helion.Resources.Archives.Entries;
using System.IO;

namespace Helion.Resources.Archives.Directories
{
    public class DirectoryArchiveEntry : Entry
    {
        public override DirectoryArchive Parent { get; }
        public readonly string FilePath;

        public DirectoryArchiveEntry(DirectoryArchive parent, string file, IEntryPath path, ResourceNamespace resourceNamespace, int index)
            : base(path, resourceNamespace, index)
        {
            Parent = parent;
            FilePath = file;
        }

        public override byte[] ReadData()
        {
            return Parent.ReadData(this);
        }

        public override void ExtractToFile(string path)
        {
            File.Copy(FilePath, path, true);
        }

        public override bool IsDirectFile() => true;
    }
}

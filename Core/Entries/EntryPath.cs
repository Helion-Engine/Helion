using Helion.Util.Extensions;
using System.IO;

namespace Helion.Entries
{
    /// <summary>
    /// Represents a path for an entry inside of an archive.
    /// </summary>
    public class EntryPath : IEntryPath
    {
        public string FullPath { get; protected set; }
        public string Name { get; protected set; }
        public string Extension { get; protected set; }
        public bool HasExtension => !string.IsNullOrEmpty(Extension);
        public bool IsDirectory => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Extension);

        public EntryPath(string path = "")
        {
            FullPath = path;
            Name = Path.GetFileNameWithoutExtension(FullPath);
            Extension = Path.GetExtension(FullPath);
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
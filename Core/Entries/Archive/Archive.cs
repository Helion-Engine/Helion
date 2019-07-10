using System.Collections;
using System.Collections.Generic;

namespace Helion.Entries.Archive
{
    /// <summary>
    /// A container of entries. Intended to be the representation of a wad/pk3.
    /// </summary>
    public abstract class Archive
    {
        /// <summary>
        /// All the entries in this archive.
        /// </summary>
        public List<Entry> Entries { get; } = new List<Entry>();

        public readonly IEntryPath Path;

        protected Archive(IEntryPath path)
        {
            Path = path;
        }

        public abstract ArchiveType GetArchiveType();
    }
}
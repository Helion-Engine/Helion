using Helion.Entries.Tree.Archive.Iterator;
using Helion.Util;
using Helion.Util.Extensions;
using System.Collections;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Entries.Tree.Archive
{
    /// <summary>
    /// A container of entries. Intended to be the representation of a wad/pk3.
    /// </summary>
    public abstract class Archive : DirectoryEntry, IEnumerable<Entry>
    {
        protected Dictionary<UpperString, Entry> nameToEntry = new Dictionary<UpperString, Entry>();

        protected Archive(EntryId id, EntryPath path) :
            base(id, path)
        {
        }

        /// <summary>
        /// Adds the entry to the archive recursively. It will place it based
        /// off of the path it contains.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        /// <param name="idAllocator">The allocator for creating new directory
        /// entries.</param>
        protected void AddEntry(Entry entry, EntryIdAllocator idAllocator)
        {
            RecursivelyAdd(entry, idAllocator, new EntryPath());
            nameToEntry[entry.Path.Name.ToUpperString()] = entry;
        }

        /// <summary>
        /// Gets the most recent entry by name. This ignores extensions.
        /// </summary>
        /// <param name="name">The name of the entry to get.</param>
        /// <returns>The entry if it exists, or an empty value otherwise.
        /// </returns>
        public Entry? GetByName(UpperString name)
        {
            return nameToEntry.TryGetValue(name, out Entry entry) ? entry : null;
        }

        /// <summary>
        /// A convenience function that gets both by name (not including the
        /// extension) and also makes sure it is of the type provided. If it
        /// matches by name but not by type, the result will be empty.
        /// </summary>
        /// <remarks>
        /// This is intended to be a convenience method so we don't have to
        /// write out casting commands. If you do not care about the type, it
        /// is better to use <see cref="GetByName"/> method.
        /// </remarks>
        /// <typeparam name="T">The type the entry should be if it exists.
        /// </typeparam>
        /// <param name="name">The name of the entry (which should not contain
        /// an extension).</param>
        /// <returns>The optional value with the entry and casted type if both
        /// conditions are met, an empty value otherwise.</returns>
        public T? GetByNameType<T>(UpperString name) where T : Entry
        {
            Precondition(typeof(T) != typeof(Entry), "Should be using a specialization class, not the abstract parent");

            if (nameToEntry.TryGetValue(name, out Entry entry))
                if (entry is T entryOfType)
                    return entryOfType;
            return null;
        }

        public abstract ArchiveType GetArchiveType();

        public IEnumerator<Entry> GetEnumerator()
        {
            foreach (Entry entry in new ArchiveEntryIterator(this))
                yield return entry;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

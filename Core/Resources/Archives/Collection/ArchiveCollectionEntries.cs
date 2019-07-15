using System.Collections.Generic;
using Helion.Resources.Archives.Entries;
using Helion.Util;

namespace Helion.Resources.Archives.Collection
{
    /// <summary>
    /// Tracks all the entries in a collection by both name and namespace for
    /// O(1) lookup.
    /// </summary>
    public class ArchiveCollectionEntries
    {
        /// <summary>
        /// A mapping of an upper case string to the most recently loaded
        /// entry.
        /// </summary>
        private readonly Dictionary<CIString, Entry> m_nameToEntries = new Dictionary<CIString, Entry>();
        
        /// <summary>
        /// A mapping of upper case name and namespace to the most recent entry
        /// for that pair of keys.
        /// </summary>
        private readonly ResourceTracker<Entry> m_namespaceNameEntries = new ResourceTracker<Entry>();
        
        /// <summary>
        /// Tracks a new entry, meaning the entry provided will be accessible
        /// to any callers assuming it is not overridden by another entry with
        /// the same name.
        /// </summary>
        /// <param name="entry">The entry to track.</param>
        public void Track(Entry entry)
        {
            m_nameToEntries[entry.Path.Name] = entry;
            m_namespaceNameEntries.Insert(entry.Path.Name, entry.Namespace, entry);
        }
        
        /// <summary>
        /// Finds the entry if it exists. This is case insensitive.
        /// </summary>
        /// <param name="name">The entry name.</param>
        /// <returns>The most recently loaded entry with the name provided, or
        /// null if it does not exist.</returns>
        public Entry? Find(CIString name)
        {
            return m_nameToEntries.TryGetValue(name, out Entry entry) ? entry : null;
        }
        
        /// <summary>
        /// Finds the entry by looking it up relative to some namespace, and
        /// then the case insensitive name.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="priorityNamespace">The namespace to look in first
        /// before any other namespaces.</param>
        /// <returns>The entry if it exists, null if not.</returns>
        public Entry? Find(CIString name, ResourceNamespace priorityNamespace)
        {
            return m_namespaceNameEntries.Get(name, priorityNamespace);
        }
    }
}
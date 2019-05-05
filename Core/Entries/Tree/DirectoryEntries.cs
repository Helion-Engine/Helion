using Helion.Util;
using System.Collections;
using System.Collections.Generic;
using static Helion.Util.Assert;

namespace Helion.Entries.Tree
{
    /// <summary>
    /// A wrapper around a series of entries.
    /// </summary>
    /// <remarks>
    /// It is also intended to make it so any lookups, entry addition, etc, are
    /// performance in O(1) instead of something worse (like O(n)).
    /// </remarks>
    public class DirectoryEntries : IEnumerable<Entry>
    {
        private LinkedList<Entry> entries = new LinkedList<Entry>();
        private Dictionary<EntryId, LinkedListNode<Entry>> idToNode = new Dictionary<EntryId, LinkedListNode<Entry>>();

        public int Count => entries.Count;

        /// <summary>
        /// Gets an entry for the ID provided.
        /// </summary>
        /// <param name="id">The ID to look up.</param>
        /// <returns>The entry if the ID exists</returns>
        public Optional<Entry> this[EntryId id] {
            get {
                if (idToNode.TryGetValue(id, out LinkedListNode<Entry> node))
                    return node.Value;
                return Optional.Empty;
            }
        }

        /// <summary>
        /// Adds the entry to the end of the list.
        /// </summary>
        /// <remarks>
        /// The entry ID should not already belong to this data structure.
        /// </remarks>
        /// <param name="entry">The entry to add.</param>
        public void AddLast(Entry entry)
        {
            Precondition(!idToNode.ContainsKey(entry.Id), $"Trying to add duplicate ID for entry {entry}");

            entries.AddLast(entry);
            idToNode[entry.Id] = entries.Last;
        }

        public IEnumerator<Entry> GetEnumerator() => entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return entries.GetEnumerator();
        }
    }
}

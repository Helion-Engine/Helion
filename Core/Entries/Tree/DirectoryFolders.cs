using Helion.Util;
using System.Collections;
using System.Collections.Generic;

namespace Helion.Entries.Tree
{
    /// <summary>
    /// An encapsulation of the all the folders that a directory entry may
    /// contain.
    /// </summary>
    public class DirectoryFolders : IEnumerable<DirectoryEntry>
    {
        private LinkedList<DirectoryEntry> directories = new LinkedList<DirectoryEntry>();
        private Dictionary<string, LinkedListNode<DirectoryEntry>> nameToNode = new Dictionary<string, LinkedListNode<DirectoryEntry>>();

        /// <summary>
        /// Gets the directory with the name provided. This is case sensitive.
        /// </summary>
        /// <param name="name">The name of the directory.</param>
        /// <returns>The directory entry for the name, or an empty value if no
        /// directory with the name provided exists under this.</returns>
        public DirectoryEntry? this[string name] {
            get {
                if (nameToNode.TryGetValue(name, out LinkedListNode<DirectoryEntry> node))
                    return node.Value;
                return null;
            }
        }

        /// <summary>
        /// Either gets the directory if it exists, or creates the directory
        /// </summary>
        /// <param name="basePath">The path that should be pointing to this
        /// directory. It will be used in construction of any chlid directory
        /// entry's path.</param>
        /// <param name="folderName">The name of the folder that should be name
        /// which should not be part of the base path argument.</param>
        /// <param name="idAllocator">The entry ID allocator to which the child
        /// directory entry will be made from (if needed).</param>
        /// <returns>Either an existing directory entry with the base path plus
        /// the folder name, or a new directory made based on the arguments.
        /// </returns>
        public DirectoryEntry GetOrCreate(EntryPath basePath, string folderName, EntryIdAllocator idAllocator)
        {
            DirectoryEntry? storedEntry = this[folderName];
            if (storedEntry != null)
                return storedEntry;

            EntryPath path = basePath.AppendDirectory(folderName);
            DirectoryEntry entry = new DirectoryEntry(idAllocator.AllocateId(), path);

            directories.AddLast(entry);
            nameToNode[folderName] = directories.Last;

            return entry;
        }

        public IEnumerator<DirectoryEntry> GetEnumerator() => directories.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return directories.GetEnumerator();
        }
    }
}

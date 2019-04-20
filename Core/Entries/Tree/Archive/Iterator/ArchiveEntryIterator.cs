using Helion.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Entries.Tree.Archive.Iterator
{
    /// <summary>
    /// A helper class responsible for iterating over all the enrties in an 
    /// archive.
    /// </summary>
    /// <remarks>
    /// This will recursively traverse the entire tree, so nested archives in
    /// archives will be properly traversed. This does not return any directory
    /// entries or archives, only the entries that are part of each.
    /// </remarks>
    public class ArchiveEntryIterator : IEnumerable<Entry>
    {
        private LinkedList<DirectoryEntry> directoriesToVisit = new LinkedList<DirectoryEntry>();

        /// <summary>
        /// Creates an iterator for the provided archive.
        /// </summary>
        /// <param name="archive">The archive for this iterator to operate on.
        /// </param>
        public ArchiveEntryIterator(Archive archive)
        {
            AddAllDirectoriesRecursive(archive);
        }

        private void AddAllDirectoriesRecursive(DirectoryEntry directoryEntry)
        {
            directoriesToVisit.AddLast(directoryEntry);
            foreach (DirectoryEntry childDirectory in directoryEntry.Folders)
                AddAllDirectoriesRecursive(childDirectory);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            while (directoriesToVisit.Any())
            {
                DirectoryEntry directoryEntry = directoriesToVisit.First.Value;
                directoriesToVisit.RemoveFirst();

                foreach (Entry entry in directoryEntry.Entries)
                {
                    // If we run into an archive, that needs to go onto our 
                    // list of things to iterate over. We also do not want
                    // to return it since this is only for entries we can use.
                    if (entry.IsDirectory())
                    {
                        if (entry is Archive archive)
                        {
                            directoriesToVisit.AddFirst(archive);
                        }
                        else
                        {
                            Assert.Fail("Ran into a directory that isn't an archive in the Entries list");
                        }
                    }
                    else
                        yield return entry;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

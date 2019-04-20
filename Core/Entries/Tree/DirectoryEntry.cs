using Helion.Resources;
using System.Linq;

namespace Helion.Entries.Tree
{
    /// <summary>
    /// A representation of a directory in an archive.
    /// </summary>
    public class DirectoryEntry : Entry
    {
        /// <summary>
        /// All the child directory entries that are under this node.
        /// </summary>
        /// <remarks>
        /// This does not include archives, they are part of the 
        /// <see cref="Entries"/> field.
        /// </remarks>
        public DirectoryFolders Folders { get; } = new DirectoryFolders();

        /// <summary>
        /// All the entries in this directory.
        /// </summary>
        /// <remarks>
        /// This contains archives as well, which means any iteration that must
        /// recursively visit all entries or maps will have to also evaluate
        /// every entry here to see if an archive is present.
        /// </remarks>
        public DirectoryEntries Entries { get; } = new DirectoryEntries();

        public DirectoryEntry(EntryId id, EntryPath path) : base(id, path, new byte[]{}, ResourceNamespace.Global)
        {
        }

        /// <summary>
        /// Recursively adds the entry until it finds/creates a directory that
        /// it can be added to.
        /// </summary>
        /// <remarks>
        /// This assumes that the path is not based off any parent paths. For
        /// example, suppose that the entry is under
        /// <code>
        ///     wads/mywad.wad/textures/mytexture.png
        /// </code>
        /// then the path for this entry under the wad file would be:
        /// <code>
        ///     textures/mytexture.png
        /// </code>
        /// This means when recursiveDepth = 0, we are dealing with "textures"
        /// and should create a directory. It will handle creating directories
        /// all the way down, or reusing directories appropriately.
        /// </remarks>
        /// <param name="entry">The entry to add.</param>
        /// <param name="idAllocator">The entry ID allocator to make directory
        /// entries from.</param>
        /// <param name="pathSoFar">The path thus far that has been traversed.
        /// </param>
        /// <param name="recursiveDepth">The depth into the entry's path of
        /// folders we are currently at.</param>
        protected void RecursivelyAdd(Entry entry, EntryIdAllocator idAllocator, EntryPath pathSoFar,
            int recursiveDepth = 0)
        {
            if (recursiveDepth >= entry.Path.Folders.Count)
            {
                Entries.AddLast(entry);
                return;
            }

            string folderName = entry.Path.Folders.ElementAt(recursiveDepth);
            DirectoryEntry directoryEntry = Folders.GetOrCreate(pathSoFar, folderName, idAllocator);

            directoryEntry.RecursivelyAdd(entry, idAllocator, directoryEntry.Path, recursiveDepth + 1);
        }

        public override bool IsDirectory() => true;

        public override ResourceType GetResourceType() => ResourceType.Directory;
    }
}

using Helion.Resources;

namespace Helion.ResourcesNew.Archives
{
    /// <summary>
    /// An entry in an archive.
    /// </summary>
    public abstract class Entry
    {
        public readonly EntryPath Path;
        public readonly Namespace Namespace;

        public Entry(EntryPath path, Namespace resourceNamespace)
        {
            Path = path;
            Namespace = resourceNamespace;
        }

        /// <summary>
        /// Gets the data for the entry. If this is backed by an external
        /// resource (ex: a file) then there will be a read to that resource.
        /// This should do its best to cache the data.
        /// </summary>
        /// <returns>The data for the entry.</returns>
        public abstract byte[] ReadData();
    }
}

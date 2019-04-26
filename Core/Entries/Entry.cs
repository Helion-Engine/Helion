using Helion.Resources;

namespace Helion.Entries
{
    /// <summary>
    /// The base class for every entry.
    /// </summary>
    public abstract class Entry
    {
        /// <summary>
        /// A unique identifier that no other entry in the project should have.
        /// </summary>
        public EntryId Id { get; }

        /// <summary>
        /// The path within some archive to this entry.
        /// </summary>
        public EntryPath Path { get; protected set; }

        /// <summary>
        /// The raw data for this entry.
        /// </summary>
        /// <remarks>
        /// The data is not intended to be perfectly in sync with every update
        /// that is done (for example if an image is being painted against in
        /// some image editor, we will not update this every time a pixel is
        /// added). However this is intended to be written to after editing of
        /// the entry is done. This means it should be reasonably up to date.
        /// </remarks>
        public byte[] Data { get; protected set; }

        /// <summary>
        /// The namespace this entry was located in.
        /// </summary>
        public ResourceNamespace Namespace { get; protected set; }

        /// <summary>
        /// True if this entry is corrupt and should not be read from or used,
        /// false if it can be used without any issues.
        /// </summary>
        /// <remarks>
        /// Corruption is intended to imply that this entry is not safe and the
        /// data structures inside should not be used.
        /// </remarks>
        public bool Corrupt { get; protected set; } = false;

        /// <summary>
        /// True if the data was modified, false otherwise.
        /// </summary>
        /// <remarks>
        /// A value of true means that it's been changed from its original data
        /// or definition. This is primarily to help the resource editor know
        /// when something has changed. It also signals to an archive composer
        /// that before writing all the entries into an archive to disk (or
        /// wherever) that it may need to rebuild the <see cref="Data"/> field
        /// before continuing.
        /// </remarks>
        public bool Changed { get; protected set; } = false;

        protected Entry(EntryId id, EntryPath path, byte[] data, ResourceNamespace resourceNamespace)
        {
            Id = id;
            Path = path;
            Data = data;
            Namespace = resourceNamespace;
        }

        /// <summary>
        /// Gets the resource type that this entry reflects.
        /// </summary>
        /// <returns>The resource type.</returns>
        public abstract ResourceType GetResourceType();

        /// <summary>
        /// Whether this entry has children entries or not.
        /// </summary>
        /// <remarks>
        /// Children entries are an implementation detail of the inheriting
        /// class.
        /// </remarks>
        /// <returns>True if it is a directory and will have zero or more child
        /// entries, false otherwise (implying it's a terminal node in the 
        /// archive tree).</returns>
        public virtual bool IsDirectory() => false;

        public override string ToString() => Path.ToString();
    }
}

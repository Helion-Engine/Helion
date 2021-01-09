namespace Helion.Resources.Archives.Entries
{
    /// <summary>
    /// The base class for every entry.
    /// </summary>
    public abstract class Entry
    {
        /// <summary>
        /// The path within some archive to this entry.
        /// </summary>
        public IEntryPath Path { get; }

        /// <summary>
        /// Reads all the raw data for this entry.
        /// </summary>
        public abstract byte[] ReadData();

        public string ReadDataAsString() => System.Text.Encoding.UTF8.GetString(ReadData());

        /// <summary>
        /// The namespace this entry was located in.
        /// </summary>
        public ResourceNamespace Namespace { get; }

        protected Entry(IEntryPath path, ResourceNamespace resourceNamespace)
        {
            Path = path;
            Namespace = resourceNamespace;
        }

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

        public override string ToString() => $"{Path}";
    }
}
namespace Helion.ResourcesNew.Archives
{
    /// <summary>
    /// An archive that is sourced from a file.
    /// </summary>
    public interface IFileArchive
    {
        /// <summary>
        /// The path this archive was loaded from.
        /// </summary>
        string Path { get; }
    }
}

namespace Helion.Resources.Archives.Entries
{
    public interface IEntryPath
    {
        /// <summary>
        /// The full path including all folders, name, and extension.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// The name of the entry. This may contain periods if the name of the
        /// entry has two or more periods. It is empty if it's a directory.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The extension of the entry, if any. Contains the period if there is an extension.
        /// Empty if this is extensionless or a directory.
        /// </summary>
        string Extension { get; }
        
        /// <summary>
        /// Creates a string with the name and appends the extension (if any).
        /// </summary>
        string NameWithExtension { get; }

        /// <summary>
        /// Checks if it has an extension.
        /// </summary>
        bool HasExtension { get; }

        /// <summary>
        /// Checks if this is a directory entry, is defined as no name and no
        /// extension.
        /// </summary>
        bool IsDirectory { get; }
    }
}
using Helion.Util;
using Helion.Util.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helion.Entries
{
    /// <summary>
    /// Represents a path for an entry inside of an archive.
    /// </summary>
    public class EntryPath
    {
        private static readonly char BACKWARD_SEPARATOR = '\\';
        private static readonly char EXTENSION_TOKEN = '.';
        public static readonly char SEPARATOR = '/';

        /// <summary>
        /// The name of the entry. This may contain periods if the name of the
        /// entry has two or more periods. It is empty if it's a directory.
        /// </summary>
        public string Name { get; private set; } = "";

        /// <summary>
        /// The extension of the entry, if any. Does not contain the period.
        /// Empty if this is extensionless or a directory.
        /// </summary>
        public string Extension { get; private set; } = "";

        /// <summary>
        /// A list of all the folders that are the subpaths (the directory
        /// listing for the path).
        /// </summary>
        public LinkedList<string> Folders { get; } = new LinkedList<string>();

        /// <summary>
        /// Checks if it has an extension.
        /// </summary>
        public bool HasExtension => Extension.NotEmpty();

        /// <summary>
        /// Checks if this is a directory entry, is defined as no name and no
        /// extension.
        /// </summary>
        public bool IsDirectory => Name.Empty() && Extension.Empty();

        /// <summary>
        /// Gets the root (first) folder, if any.
        /// </summary>
        public Optional<string> RootFolder => Folders.Any() ? Folders.First() : Optional<string>.Empty();

        /// <summary>
        /// The last folder, if any.
        /// </summary>
        public Optional<string> LastFolder => Folders.Any() ? Folders.Last() : Optional<string>.Empty();

        public EntryPath(string path = "")
        {
            ExtractFoldersAndName(path);
            ExtractExtensionFromName();
        }

        public EntryPath(EntryPath path)
        {
            Name = path.Name;
            Extension = path.Extension;
            foreach (string folder in path.Folders)
                Folders.AddLast(folder);
        }

        private void ExtractFoldersAndName(string path)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in path)
            {
                if (InvalidFolderCharacter(c))
                    continue;

                if (c == SEPARATOR || c == BACKWARD_SEPARATOR)
                {
                    string folderName = builder.ToString();
                    if (folderName.Length != 0)
                        Folders.AddLast(folderName);
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }

            string remainingChars = builder.ToString();
            if (remainingChars.Length != 0)
                Name = remainingChars;
        }

        private void ExtractExtensionFromName()
        {
            int lastIndex = Name.LastIndexOf(EXTENSION_TOKEN);
            if (lastIndex == -1)
                return;

            Extension = Name.SubstringFrom(lastIndex + 1);
            Name = Name.Substring(0, lastIndex);
        }

        private static bool InvalidFolderCharacter(char c) => c < 32 || c > 126;

        /// <summary>
        /// Adds the two paths together, such that this path has the provided
        /// path concatenated onto the end of this path. This does not mutate
        /// the current object.
        /// </summary>
        /// <remarks>
        /// This implies that the file/extension in this object is lost. For
        /// example:
        /// <code>
        ///     PathA = "some/path/file.txt"
        ///     PathB = "somedir/hi.png"
        ///     PathA.Append(PathB) => "/some/path/somedir/hi.png"
        /// </code>
        /// </remarks>
        /// <param name="path">The path to append onto this.</param>
        /// <returns>The combined path.</returns>
        public EntryPath Append(EntryPath path)
        {
            EntryPath newPath = new EntryPath(this);

            foreach (string folder in path.Folders)
                newPath.Folders.AddLast(folder);
            newPath.Name = path.Name;
            newPath.Extension = path.Extension;

            return newPath;
        }

        /// <summary>
        /// Appends the file name (which may or may not have an extension) onto
        /// this path and returns a new path. This does not mutate the current
        /// object.
        /// </summary>
        /// <remarks>
        /// An example of its usage is as follows:
        /// <code>
        ///     PathA = "some/path/file.txt"
        ///     PathA.Append("yes.png") => "/some/path/yes.png"
        /// </code>
        /// This also can be used to create a directory path from a non-
        /// directory-path by using an empty string.
        /// </remarks>
        /// <param name="fileAndExtension">The file/extension string to use
        /// in place of the one at this path.</param>
        /// <returns>A new path with the file and extension provided.</returns>
        public EntryPath AppendFile(string fileAndExtension)
        {
            EntryPath newPath = new EntryPath(this);
            newPath.Name = fileAndExtension;
            newPath.ExtractExtensionFromName();
            return newPath;
        }

        /// <summary>
        /// Takes the file/extension on the provided path and uses it in place
        /// of the current one, and returns a new copy of that path. This does
        /// not mutate the current object.
        /// </summary>
        /// <remarks>
        /// An example of its usage is as follows:
        /// <code>
        ///     PathA = "some/path/file.txt"
        ///     PathB = "a/long/path/here/oh/my/yes.png"
        ///     PathA.AppendFile("yes.png") => "/some/path/yes.png"
        /// </code>
        /// </remarks>
        /// <param name="pathWithFileExtension">The path to use the file and
        /// extension from.</param>
        /// <returns>A new path with the directory from this object and the
        /// file/extension from the provided argument.</returns>
        public EntryPath AppendFile(EntryPath pathWithFileExtension)
        {
            EntryPath newPath = new EntryPath(this);
            newPath.Name = pathWithFileExtension.Name;
            newPath.Extension = pathWithFileExtension.Extension;
            return newPath;
        }

        public EntryPath AppendDirectory(string directoryName)
        {
            EntryPath newPath = new EntryPath(this);
            if (directoryName.NotEmpty())
                newPath.Folders.AddLast(directoryName);
            return newPath;
        }

        /// <summary>
        /// A convenience method to convert the name/extension into a directory
        /// folder. This creates a new object and does not mutate the current
        /// object.
        /// </summary>
        /// <remarks>
        /// This creates a new copy of the path if it's already a directory
        /// entry.
        /// </remarks>
        /// <returns>A new entry path with the name converted into a path that
        /// uses the name/extension as a last path (or nothing changes if this
        /// is already a directory path).</returns>
        public EntryPath MoveNameToLastDirectory()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Name);
            if (Extension.NotEmpty())
                stringBuilder.Append(EXTENSION_TOKEN).Append(Extension);

            EntryPath newPath = new EntryPath(this);
            newPath.Name = "";
            newPath.Extension = "";

            string newFolderName = stringBuilder.ToString();
            if (newFolderName.NotEmpty())
                newPath.Folders.AddLast(newFolderName);

            return newPath;
        }

        /// <summary>
        /// Converts the folders into a string that could be used in the
        /// constructor of a new Path to get only the directory.
        /// </summary>
        /// <returns>The folders in a string path form.</returns>
        public string FoldersAsPathText()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string folder in Folders)
                builder.Append(folder).Append(SEPARATOR);
            return builder.ToString();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(FoldersAsPathText()).Append(Name);
            if (Extension.NotEmpty())
                builder.Append(EXTENSION_TOKEN).Append(Extension);

            return builder.ToString();
        }
    }
}

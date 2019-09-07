using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives.Entries;
using Helion.Util.Configuration;
using Helion.Util.Extensions;
using MoreLinq;
using NLog;

namespace Helion.Resources.Archives.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : IArchiveLocator
    {
        private const char Separator = ';';
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The search paths for files.
        /// </summary>
        /// <remarks>
        /// This contains an empty string because we want to search the current
        /// directory first, or if the user provides a full path then we want
        /// searching to be done at the path first. This is also a list because
        /// we assume priority is meant to be given to the beginning of what is
        /// provided.
        /// </remarks>
        private readonly IList<string> m_paths = new List<string> { "" };

        /// <summary>
        /// Creates a file system locator that only looks in the working
        /// directory.
        /// </summary>
        public FilesystemArchiveLocator()
        {
        }
        
        /// <summary>
        /// Creates a file system locator that looks in the working directory
        /// and any additional directories that are in the config.
        /// </summary>
        /// <param name="config">The config to get the additional directories
        /// from.</param>
        public FilesystemArchiveLocator(Config config)
        {
            config.Engine.Files.Directories.Get().Split(Separator)
                .Where(p => !p.Empty())
                .Select(EnsureEndsWithDirectorySeparator)
                .ForEach(m_paths.Add);
        }

        public Archive? Locate(string uri)
        {
            string? extension = Path.GetExtension(uri);
            if (extension == null)
            {
                Log.Error("Missing extension, cannot determine archive type from: {0}", uri);
                return null;
            }

            foreach (string basePath in m_paths)
            {
                string path = basePath + uri;

                if (!File.Exists(path))
                    continue;
                
                try
                {
                    if (extension.Equals(".wad", StringComparison.OrdinalIgnoreCase))
                        return new Wad(new EntryPath(path));
                    if (extension.Equals(".pk3", StringComparison.OrdinalIgnoreCase))
                        return new PK3(new EntryPath(path));
                }
                catch (Exception e)
                {
                    Log.Error("Unexpected error when loading {0}: {1}", uri, e.Message);
                    return null;
                }
            }

            Log.Error("Either could not find {0}, or the extension is not supported", uri);
            return null;
        }
        
        private static string EnsureEndsWithDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
        }
    }
}
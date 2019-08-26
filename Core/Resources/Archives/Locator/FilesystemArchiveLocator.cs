using System;
using System.IO;
using Helion.Resources.Archives.Entries;
using NLog;

namespace Helion.Resources.Archives.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : IArchiveLocator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Archive? Locate(string uri)
        {
            string? extension = Path.GetExtension(uri);
            if (extension == null)
            {
                Log.Error("Missing extension, cannot determine archive type from: {0}", uri);
                return null;
            }

            try
            {
                if (extension.Equals(".wad", StringComparison.OrdinalIgnoreCase))
                    return new Wad(new EntryPath(uri));
                if (extension.Equals(".pk3", StringComparison.OrdinalIgnoreCase))
                    return new PK3(new EntryPath(uri));
            }
            catch (Exception e)
            {
                Log.Error("Unexpected error when loading {0}: {1}", uri, e.Message);
                return null;
            }

            Log.Error("Archive file extension is not supported for {0}", uri);
            return null;
        }
    }
}
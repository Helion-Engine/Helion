using System;
using System.IO;
using Helion.Resources.Archives;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Archives.Locator;
using Helion.Util;

namespace Helion.Entries.Archives.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : IArchiveLocator
    {
        public Expected<Archive> Locate(string uri)
        {
            string? extension = Path.GetExtension(uri);
            if (extension == null)
                return $"Unable to get extension from {uri}";

            try
            {
                if (extension.Equals(".wad", StringComparison.OrdinalIgnoreCase))
                    return new Wad(new EntryPath(uri));
                if (extension.Equals(".pk3", StringComparison.OrdinalIgnoreCase))
                    return new PK3(new EntryPath(uri));
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return $"Archive file extension is not supported: {uri}";
        }
    }
}
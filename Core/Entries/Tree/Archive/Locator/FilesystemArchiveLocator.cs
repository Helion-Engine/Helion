using Helion.Util;
using System;
using System.IO;

namespace Helion.Entries.Tree.Archive.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : IArchiveLocator
    {
        public Expected<Archive> Locate(string uri)
        {
            string extension = Path.GetExtension(uri);

            try
            {
                if (extension.Equals(".wad", StringComparison.OrdinalIgnoreCase))
                {
                    return new Wad(new EntryPath(uri));
                }
                else if (extension.Equals(".pk3", StringComparison.OrdinalIgnoreCase))
                {
                    return new Pk3(new EntryPath(uri));
                }
            }
            catch(Exception e)
            {
                return e.Message;
            }

            return $"Archive file extension is not supported: {uri}";
        }
    }
}

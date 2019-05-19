using Helion.Util;

namespace Helion.Entries.Tree.Archive.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : IArchiveLocator
    {
        public Expected<Archive> Locate(string uri, EntryClassifier classifier,
            EntryIdAllocator idAllocator)
        {
            UpperString upperUri = uri;

            if (upperUri.EndsWith("WAD"))
            {
                Expected<Wad> wad = Wad.FromFile(uri, idAllocator, classifier);
                if (wad.Value != null)
                    return wad.Value;
                else
                    return wad.Error;
            }

            if (upperUri.EndsWith("PK3"))
            {
                Expected<Pk3> pk3 = Pk3.FromFile(uri, idAllocator, classifier);
                if (pk3.Value != null)
                    return pk3.Value;
                else
                    return pk3.Error;
            }

            return $"Archive file extension is not supported: {uri}";
        }
    }
}

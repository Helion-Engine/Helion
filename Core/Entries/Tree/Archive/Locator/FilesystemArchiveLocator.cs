using Helion.Util;

namespace Helion.Entries.Tree.Archive.Locator
{
    /// <summary>
    /// Searches the local file system for archives. This functions off of full
    /// paths as URIs.
    /// </summary>
    public class FilesystemArchiveLocator : ArchiveLocator
    {
        public Expected<Archive, string> Locate(string uri, EntryClassifier classifier, 
            EntryIdAllocator idAllocator)
        {
            UpperString upperUri = uri;

            if (upperUri.EndsWith("WAD"))
            {
                Expected<Wad, string> wad = Wad.FromFile(uri, idAllocator, classifier);
                if (wad)
                    return wad.Value;
                else
                    return wad.Error;
            }

            if (upperUri.EndsWith("PK3"))
            {
                Expected<Pk3, string> pk3 = Pk3.FromFile(uri, idAllocator, classifier);
                if (pk3)
                    return pk3.Value;
                else
                    return pk3.Error;
            }

            return $"Archive file extension is not supported: {uri}";
        }
    }
}

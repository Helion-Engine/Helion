using System;
using System.IO;
using Helion.Bsp.External;
using Helion.Resource.Archives;
using Helion.Util;
using NLog;

namespace Helion.Resource
{
    /// <summary>
    /// A helper class that makes sure we're drawing from the caches.
    /// </summary>
    public static class Caches
    {
        private const string CachePath = "cache";

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Loads an archive from the cache, or generates it in the cache and
        /// returns that.
        /// </summary>
        /// <param name="path">The path to the non-cached file. This should be
        /// the standard archive you want to load.</param>
        /// <returns>The archive, or null on failure.</returns>
        public static Archive? Load(string path)
        {
            if (!path.EndsWith("wad", StringComparison.OrdinalIgnoreCase))
                return Archive.Open(path);

            try
            {
                EnsureCacheFolderOrThrow();
                string? md5 = Files.CalculateMD5(path)?.ToUpper();
                if (md5 == null)
                {
                    Log.Warn("Cannot open file for MD5 hashing at: {0}", path);
                    return null;
                }

                string cachePath = $"{CachePath}/{md5}.wad";
                if (File.Exists(cachePath))
                    return Archive.Open(cachePath);

                if (!ZdbspDownloader.Download())
                {
                    Log.Warn("Unable to download ZDBSP");
                    return null;
                }

                Zdbsp zdbsp = new(ZdbspDownloader.BspExePath, path, cachePath);
                zdbsp.Run();

                return Archive.Open(cachePath);
            }
            catch
            {
                Log.Error("Unexpected error when loading file cache at: {0}", path);
                return null;
            }
        }

        private static void EnsureCacheFolderOrThrow()
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);

            if (!Directory.Exists(CachePath))
                throw new Exception($"Cannot create cache path at: {CachePath}");
        }
    }
}

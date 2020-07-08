using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace Helion.Bsp.External
{
    /// <summary>
    /// A helper class to make sure we have ZDBSP and that it can be used.
    /// </summary>
    public static class ZdbspDownloader
    {
        private const string Folder = "bsp";
        private const string Executable = "zdbsp.exe";
        private const string ZipFileName = "zdbsp-1.19.zip";
        private static readonly string DownloadUrl = $"https://zdoom.org/files/utils/zdbsp/{ZipFileName}";
        private static readonly string BspExePath = $"{Folder}/{Executable}";
        private static readonly string ZipFilePath = $"{Folder}/{ZipFileName}";
        private static readonly List<string> ResidualZipFiles = new List<string>
        {
            $"{Folder}/zdbsp.html",
            $"{Folder}/poly_bad.png",
            $"{Folder}/poly_mov.png",
            $"{Folder}/poly_new.png"
        };

        /// <summary>
        /// Checks if the executable exists at `bsp/zdbsp.exe`.
        /// </summary>
        /// <returns>True if it exists, false if not.</returns>
        public static bool HasZdbsp()
        {
            return File.Exists(BspExePath);
        }

        /// <summary>
        /// Downloads the bsp builder if it does not exist.
        /// </summary>
        /// <returns>True if successfully downloaded and unzipped (or if it
        /// already was downloaded), false on any error.</returns>
        public static bool Download()
        {
            if (HasZdbsp())
                return true;

            try
            {
                // Unfortunately .NET will throw an exception if the files
                // already exist when unzipping, so we need to clean up before
                // just in case some previous call failed.
                return CreateFolderIfMissing() &&
                       DownloadZipIfMissing() &&
                       DeleteExtraUnzippedFiles() &&
                       UnzipDownload() &&
                       DeleteAllUnneededFiles() &&
                       HasZdbsp();
            }
            catch
            {
                return false;
            }
        }

        private static bool DownloadZipIfMissing()
        {
            if (HasZdbspZip())
                return true;

            using (var client = new WebClient())
                client.DownloadFile(DownloadUrl, ZipFilePath);
            return HasZdbspZip();
        }

        private static bool CreateFolderIfMissing()
        {
            return Directory.Exists(Folder) || Directory.CreateDirectory(Folder).Exists;
        }

        private static bool HasZdbspZip()
        {
            return File.Exists(ZipFilePath);
        }

        private static bool UnzipDownload()
        {
            ZipFile.ExtractToDirectory(ZipFilePath, Folder);
            return HasZdbsp();
        }

        private static bool DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
            return !File.Exists(path);
        }

        private static bool DeleteExtraUnzippedFiles()
        {
            return ResidualZipFiles.All(DeleteFile);
        }

        private static bool DeleteResidualZipOrThrow()
        {
            return DeleteFile(ZipFilePath);
        }

        private static bool DeleteAllUnneededFiles()
        {
            return DeleteExtraUnzippedFiles() && DeleteResidualZipOrThrow();
        }
    }
}

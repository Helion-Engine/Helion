using System.IO;
using System.Text;

namespace Helion.Util.Bytes
{
    /// <summary>
    /// A collection of helper functions for operating on/with files.
    /// </summary>
    public static class Files
    {
        /// <summary>
        /// Calculates the MD5 hash of some file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The MD5 hash. Can be null if the file is not able to be
        /// opened. Will be lower case.</returns>
        public static string? CalculateMD5(string path)
        {
            try
            {
                using FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(0, SeekOrigin.Begin);

                using var md5 = System.Security.Cryptography.MD5.Create();
                byte[] data = md5.ComputeHash(fileStream);

                StringBuilder hex = new(data.Length * 2);
                foreach (byte b in data)
                    hex.AppendFormat("{0:x2}", b);
                return hex.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
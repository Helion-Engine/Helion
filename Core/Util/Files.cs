using System.IO;
using System.Text;

namespace Helion.Util
{
    /// <summary>
    /// A collection of helper functions for operating on/with files.
    /// </summary>
    public static class Files
    {
        /// <summary>
        /// Calculates the MD5 hash of some file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string? CalculateMD5(string path)
        {
            try
            {
                using FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
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

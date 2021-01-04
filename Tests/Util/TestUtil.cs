using System.IO;
using System.IO.Compression;

namespace Tests.Util
{
    public class TestUtil
    {
        public static void CopyResourceFile(string filename)
        {
            DeleteResourceFile(filename);
            File.Copy(Path.Combine("Resources", filename), filename);
        }

        public static void CopyResourceZip(string filename)
        {
            using (ZipArchive zip = ZipFile.OpenRead(Path.Combine("Resources", filename)))
                zip.ExtractToDirectory(Directory.GetCurrentDirectory(), true);
        }

        public static void DeleteResourceFile(string filename)
        {
            if (File.Exists(filename))
                File.Delete(filename);
        }
    }
}

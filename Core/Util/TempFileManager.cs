using NLog;
using System.Collections.Generic;
using System.IO;

namespace Helion.Util
{
    public static class TempFileManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly List<string> TempFiles = new();

        public static string GetFile()
        {
            string file = Path.GetTempFileName();
            TempFiles.Add(file);
            return file;
        }

        public static void DeleteFile(string file)
        {
            for (int i = 0; i < TempFiles.Count; i++)
            {
                if (!TempFiles[i].Equals(file))
                    continue;

                TryDelete(file);
                TempFiles.RemoveAt(i);
                break;
            }
        }

        public static void DeleteAllFiles()
        {
            foreach (var file in TempFiles)
                TryDelete(file);

            TempFiles.Clear();
        }

        private static void TryDelete(string file)
        {
            try { File.Delete(file); }
            catch { Log.Error($"Failed to delete temporary file: {file}"); }
        }
    }
}

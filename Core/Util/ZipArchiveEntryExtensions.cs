using System.IO;
using System.IO.Compression;
using System.Text;

namespace Helion.Util;

public static class ZipArchiveEntryExtensions
{
    public static string ReadDataAsString(this ZipArchiveEntry entry)
    {
        byte[] data = new byte[entry.Length];
        using Stream stream = entry.Open();
        int totalRead = 0;
        while (totalRead < data.Length)
            totalRead += stream.Read(data, totalRead, (int)entry.Length - totalRead);
        return Encoding.UTF8.GetString(data);
    }
}

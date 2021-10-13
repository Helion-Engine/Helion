using System;
using System.IO;

namespace Helion.Tests.Helpers;

public static class FileHelper
{
    public static string GetUniqueTempFilePath() => $"{Path.GetTempPath()}/{Guid.NewGuid()}.tmp";
}

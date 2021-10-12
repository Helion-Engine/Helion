using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NLog;

namespace Helion.Bsp.External;

public class Zdbsp
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly string m_file;
    private readonly string m_output;

    public Zdbsp(string intpuFile, string outputFile)
    {
        m_file = intpuFile;
        m_output = outputFile;
    }

    public bool Run(string map, out string output)
    {
        output = string.Empty;
        string? path = GetZdbspPath();
        if (string.IsNullOrEmpty(path))
            return false;

        ProcessStartInfo info = new()
        {
            FileName = Path.Combine(path, "zdbsp.exe"),
            Arguments = CreateArgs(map),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        Process? process = Process.Start(info);
        if (process == null)
        {
            Log.Warn("Unable to start ZDBSP process");
            return false;
        }

        StringBuilder sb = new StringBuilder();
        while (!process.StandardOutput.EndOfStream)
            sb.AppendLine(process.StandardOutput.ReadLine());

        output = sb.ToString();
        process.WaitForExit();
        return true;
    }

    private static string? GetZdbspPath()
    {
        string platform;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            platform = "win";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            platform = "linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            platform = "osx";
        else
            return null;

        if (Environment.Is64BitOperatingSystem)
            return Path.Combine("runtimes", $"{platform}-x64", "native");
        else
            return Path.Combine("runtimes", $"{platform}-x86", "native");
    }

    private string CreateArgs(string map)
    {
        return string.Format("--gl-only --output \"{0}\" \"{1}\" --map=\"{2}\"", m_output, m_file, map);
    }
}

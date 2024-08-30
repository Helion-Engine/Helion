using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Resources.Archives.Directories;
using Helion.Resources.Archives.Entries;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Resources.Archives.Locator;

/// <summary>
/// Searches the local file system for archives. This functions off of full
/// paths as URIs.
/// </summary>
public class FilesystemArchiveLocator : IArchiveLocator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The search paths for files.
    /// </summary>
    /// <remarks>
    /// This contains an empty string because we want to search the current
    /// directory first, or if the user provides a full path then we want
    /// searching to be done at the path first. This is also a list because
    /// we assume priority is meant to be given to the beginning of what is
    /// provided.
    /// </remarks>
    private readonly IList<string> m_paths = new List<string> { "" };
    private readonly IndexGenerator m_indexGenerator = new();

    /// <summary>
    /// Creates a file system locator that only looks in the working
    /// directory.
    /// </summary>
    public FilesystemArchiveLocator()
    {
    }

    /// <summary>
    /// Creates a file system locator that looks in the working directory
    /// and any additional directories that are in the config or commonly used envvars.
    /// </summary>
    /// <param name="config">The config to get the additional directories
    /// from.</param>
    public FilesystemArchiveLocator(IConfig config)
    {
        List<string> paths = config.Files.Directories.Value;
        var envPaths = GetWadDirsFromEnvVars();
        foreach (string path in paths.Concat(envPaths).Where(p => !p.Empty()).Select(EnsureEndsWithDirectorySeparator).Distinct())
            m_paths.Add(path);
    }

    public Archive? Locate(string uri)
    {
        bool exists = false;
        foreach (string basePath in m_paths)
        {
            string path = Path.Combine(basePath, uri);
            if (!CheckPathExists(path))
                continue;

            exists = true;

            try
            {
                if (IsDirectory(path))
                    return new DirectoryArchive(new EntryPath(path));
                if (IsWad(path))
                    return new Wad(new EntryPath(path), m_indexGenerator);
                if (IsPk3(path))
                    return new PK3(new EntryPath(path), m_indexGenerator);
            }
            catch (Exception e)
            {
                Log.Error("Unexpected error when loading {0}: {1}", uri, e.Message);
                return null;
            }
        }

        Log.Error("Could not load {0}. The file {1}.", uri, exists ? "type is not supported" : "does not exist");
        return null;
    }

    private static bool IsDirectory(string path)
    {
        FileAttributes attr = File.GetAttributes(path);
        return (attr & FileAttributes.Directory) == FileAttributes.Directory;
    }

    private static bool CheckPathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private static BinaryReader GetBinaryReader(string path)
    {
        return new(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
    }

    private static bool IsWad(string path)
    {
        using BinaryReader reader = GetBinaryReader(path);
        const int iwadValue = 1145132873;
        const int pwadVallue = 1145132880;
        if (reader.BaseStream.Length < 4)
            return false;
        uint headerValue = reader.ReadUInt32();
        return headerValue == iwadValue || headerValue == pwadVallue;
    }

    private static bool IsPk3(string path)
    {
        using BinaryReader reader = GetBinaryReader(path);
        if (reader.BaseStream.Length < 4)
            return false;
        return reader.ReadUInt32() == 0x04034b50;
    }

    private static string EnsureEndsWithDirectorySeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }

    /// <remarks>
    /// https://doomwiki.org/wiki/Environment_variables
    /// </remarks>
    public static List<string> GetWadDirsFromEnvVars()
    {
        List<string> envPaths = [];
        string? envDOOMWADDIR = Environment.GetEnvironmentVariable("DOOMWADDIR");
        if (envDOOMWADDIR != null)
            envPaths.Add(envDOOMWADDIR);
        string? envDOOMWADPATH = Environment.GetEnvironmentVariable("DOOMWADPATH");
        if (envDOOMWADPATH != null)
        {
            char separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            foreach (string path in envDOOMWADPATH.Split(separator))
                envPaths.Add(path);
        }
        return envPaths;
    }
}

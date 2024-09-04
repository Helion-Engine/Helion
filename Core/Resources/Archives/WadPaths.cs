using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Helion.Resources.Archives;

/// <summary>
/// Utility class for getting WAD paths.
/// </summary>
public static class WadPaths
{
    private static readonly string[] SteamDoomDirs = [
        "steamapps/common/Ultimate Doom/rerelease",
        "steamapps/common/Doom 2/base",
        "steamapps/common/Doom 2/masterbase",
        "steamapps/common/Doom 2/finaldoombase",
        "steamapps/common/Final Doom/base",
        "steamapps/common/DOOM 3 BFG Edition/base/wads",
    ];

    private static readonly string[] LinuxDoomDirs = [
        "/usr/local/share/doom",
        "/usr/local/share/games/doom",
        "/usr/share/doom",
        "/usr/share/games/doom",
        "/usr/share/games/doom3bfg/base/wads",
    ];

    /// <remarks>
    /// https://doomwiki.org/wiki/Environment_variables
    /// </remarks>
    public static List<string> GetFromEnvVars()
    {
        List<string> paths = [];

        string? envDOOMWADDIR = Environment.GetEnvironmentVariable("DOOMWADDIR");
        if (!string.IsNullOrEmpty(envDOOMWADDIR))
            paths.Add(envDOOMWADDIR);

        string? envDOOMWADPATH = Environment.GetEnvironmentVariable("DOOMWADPATH");
        if (envDOOMWADPATH != null)
        {
            char separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
            paths.AddRange(envDOOMWADPATH.Split(separator, StringSplitOptions.RemoveEmptyEntries));
        }
        return paths;
    }

    public static List<string> GetFromSteamAndLinuxDirs()
    {
        List<string> paths = [];

        var steamPath = GetSteamPath();
        if (Directory.Exists(steamPath))
        {
            paths.AddRange(SteamDoomDirs.Select(dir => Path.Combine(steamPath, dir)));
        }

        if (OperatingSystem.IsLinux())
        {
            paths.AddRange(LinuxDoomDirs);
            paths.AddRange(GetLinuxUserPaths());
        }

        return paths;
    }

    // TODO: this will get the base path, but doom may be in another library folder,
    // consider parsing libraryfolders.vdf
    private static string? GetSteamPath()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            }
            catch (Exception)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/");
            }
        }

        if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetEnvironmentVariable("HOME");

            if (!string.IsNullOrWhiteSpace(home))
                return $"{home}/.local/share/Steam";
        }

        return null;
    }

    private static List<string> GetLinuxUserPaths()
    {
        var paths = new List<string>();

        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
            paths.Add($"{xdgConfigHome}/helion");

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            paths.Add($"{xdgDataHome}/doom");
            paths.Add($"{xdgDataHome}/games/doom");
        }

        var home = Environment.GetEnvironmentVariable("HOME");

        if (!string.IsNullOrWhiteSpace(home))
        {
            paths.Add($"{home}/.config/helion");
            paths.Add($"{home}/.local/share/doom");
            paths.Add($"{home}/.local/share/games/doom");
        }

        return paths;
    }
}

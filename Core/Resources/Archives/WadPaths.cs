using Helion.Util.Parser;
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
    private const string SteamLibraryFile = "config/libraryfolders.vdf";
    private const string SteamAppsCommon = "steamapps/common";
    private static readonly Dictionary<string, string[]> SteamIdLocationLookup = new() {
        { "2280", [
            "Ultimate Doom/base",
            "Ultimate Doom/base/doom2",
            "Ultimate Doom/base/tnt",
            "Ultimate Doom/base/plutonia",
            "Ultimate Doom/rerelease"
        ] },
        { "2290", [
            "Final Doom/base"
        ] },
        { "2300", [
            "Doom 2/base",
            "Doom 2/masterbase",
            "Doom 2/finaldoombase"
        ] },
        { "208200", [
            "DOOM 3 BFG Edition/base/wads"
        ] },
    };

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
            paths.AddRange(GetPathsFromSteamLibraries(steamPath));
        }

        if (OperatingSystem.IsLinux())
        {
            paths.AddRange(LinuxDoomDirs);
            paths.AddRange(GetLinuxUserPaths());
        }

        return paths;
    }

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

    private static List<string> GetPathsFromSteamLibraries(string steamBasePath)
    {
        List<string> foundPaths = [];
        try
        {
            string libraryInfoPath = Path.Combine(steamBasePath, SteamLibraryFile);
            string libraryInfo = File.ReadAllText(libraryInfoPath);
            SimpleParser parser = new();
            parser.Parse(libraryInfo);
            string? currentLibraryPath = null;
            while (!parser.IsDone())
            {
                string token = parser.ConsumeString();
                if (token == "path")
                    currentLibraryPath = parser.ConsumeString().Replace("\\\\", "\\");
                if (token == "apps" && parser.PeekString() == "{")
                {
                    parser.ConsumeString();
                    while (parser.PeekString() != "}")
                    {
                        string appId = parser.ConsumeString();
                        if (SteamIdLocationLookup.TryGetValue(appId, out string[]? appPaths) && currentLibraryPath != null)
                            foundPaths.AddRange(appPaths.Select(x => Path.Combine(currentLibraryPath, SteamAppsCommon, x)).Where(Directory.Exists));
                        parser.ConsumeString();
                    }
                }
            }
        }
        catch { }
        return foundPaths;
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

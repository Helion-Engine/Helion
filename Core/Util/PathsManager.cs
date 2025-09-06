using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Parser;
using Microsoft.Win32;

namespace Helion.Util;

/// <summary>
/// Manages folders for assets, user data/configs, WADs, except for any paths listed in the config.
/// </summary>
public class PathsManager
{
    private readonly string m_workingDirectory;
    private readonly string m_userDataFolder;
    private readonly List<string> m_applicationFolders;
    private readonly List<string> m_wadEnvFolders;
    private readonly List<string> m_wadCommonFolders;

    /// <summary>
    /// The working directory when Helion was launched
    /// </summary>
    public string LaunchFolder => m_workingDirectory;

    /// <summary>
    /// Where the config, save games, screenshots etc. are stored.
    /// The save dir can still be overridden by the -savedir arg.
    /// </summary>
    public string UserDataFolder => m_userDataFolder;

    /// <summary>
    /// Helion's folder as well as its resource folder on Linux
    /// </summary>
    public List<string> ApplicationFolders => m_applicationFolders;

    /// <summary>
    /// Soundfonts can be in the user data folder or application folders.
    /// The paths here do not include the /SoundFonts subfolder.
    /// </summary>
    public List<string> SoundFontsFolders => [m_userDataFolder, .. m_applicationFolders];

    /// <summary>
    /// Environment variable search paths
    /// </summary>
    public List<string> WadEnvFolders => m_wadEnvFolders;

    /// <summary>
    /// Doom installations (e.g. Steam)
    /// </summary>
    public List<string> WadCommonFolders => m_wadCommonFolders;

    private const string WindowsShellFoldersKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";
    private const string WindowsSavedGamesFolderGuid = "{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}";
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

    public PathsManager(string workingDirectory = ".", bool forcePortableMode = false)
    {
        m_workingDirectory = StandardizePath(workingDirectory);
        string portableConfigFile = Path.Combine(AppContext.BaseDirectory, FileConfig.IniFile);
        m_userDataFolder = StandardizePath(GetConfigFolder(forcePortableMode || File.Exists(portableConfigFile)));
        m_applicationFolders = [StandardizePath(AppContext.BaseDirectory)];
        if (OperatingSystem.IsLinux())
        {
            // There are three folder structures we'll consider on Linux:
            //
            // 1. Everything in one folder, just like on Windows.
            //    On Linux this would be e.g. `/opt/Helion` or some random folder created by the user.
            //    No special behavior is needed here, we added that path above.
            //
            // 2. Installed according to the Filesystem Hierarchy Standard.
            //    The executable goes in `/usr/bin` and assets go in `/usr/share/Helion`.
            //
            // 3. In an AppImage/FlatPak. The FHS is still followed here except relative to the package's mount point.
            const string LinuxFhsResourcesPath = "/usr/share/Helion";

            // For AppImage, the package's filesystem will be mounted in `/tmp/.mount_<app>-<random>`,
            // and for FlatPak in `/app` (from Helion's perspective).
            // Relative paths are used here (the executable's directory would be `<mount>/usr/bin`).
            if (Environment.GetEnvironmentVariable("APPIMAGE") != null
                || Environment.GetEnvironmentVariable("FLATPAK_ID") != null)
            {
                // don't use Path.Combine() here due to absolute path behavior
                m_applicationFolders.Add(StandardizePath(Path.Join(AppContext.BaseDirectory, "../..", LinuxFhsResourcesPath)));
            }
            // Otherwise, add the FHS resources path. The executable's directory still comes first,
            // in the case that there's both a user and system Helion installation.
            else
            {
                m_applicationFolders.Add(StandardizePath(LinuxFhsResourcesPath));
            }
        }
        m_wadEnvFolders = [.. GetWadFoldersFromEnvVars().Select(StandardizePath)];
        m_wadCommonFolders = [.. GetWadFoldersFromSteamAndLinuxDirs().Select(StandardizePath)];
    }

    /// <summary>
    /// Converts relative to absolute paths if needed, and removes any trailing directory separators.
    /// </summary>
    private static string StandardizePath(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);

    /// <summary>
    /// Builds a folder search list for WADs and other archives:
    /// <list type="bullet">
    /// <item>launch directory</item>
    /// <item>user config folders (relative to the user data folder)</item>
    /// <item>Helion application folders</item>
    /// <item>WAD envvar folders (e.g. DOOMWADDIR)</item>
    /// <item>DOOM install/common folders (e.g. Steam), if enabled in the config</item>
    /// </list>
    /// </summary>
    public List<string> GetArchiveFolders(IConfig config)
    {
        // relative config folders are relative to the user data folder;
        // convert them to full paths since our CWD is likely elsewhere
        var configFolders = config.Files.Directories.Value
            .Select(x => Path.IsPathRooted(x) ? x : Path.Combine(m_userDataFolder, x));
        List<string> allFolders = [
            LaunchFolder,
            .. configFolders,
            .. ApplicationFolders,
            .. WadEnvFolders,
            .. config.Files.SearchCommonDirectories ? WadCommonFolders : []
        ];
        // if launch dir == app dir, or when running in portable mode, we can have dupes
        List<string> deduped = [.. allFolders.Select(StandardizePath).Distinct()];
        return deduped;
    }

    private static string GetConfigFolder(bool portableMode = false)
    {
        if (portableMode)
            return AppContext.BaseDirectory;

        string? folder = null;

        // on Windows, use "~/Saved Games/Helion"
        if (OperatingSystem.IsWindows())
        {
            var registrySavedGamesFolder = (string?)Registry.GetValue(WindowsShellFoldersKey, WindowsSavedGamesFolderGuid, null);
            if (registrySavedGamesFolder != null)
                folder = Path.Combine(registrySavedGamesFolder, "Helion");
            else
            {
                var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
                if (userDir != null)
                    folder = Path.Combine(userDir, "Saved Games", "Helion");
            }
        }

        // On Linux, default to "$XDG_CONFIG_HOME/Helion"
        else if (OperatingSystem.IsLinux())
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrWhiteSpace(configHome))
            {
                // Fallback to "$HOME/.config"
                var home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrWhiteSpace(home))
                    configHome = Path.Combine(home, ".config");
            }
            if (!string.IsNullOrWhiteSpace(configHome))
            {
                // previous versions used a lowercase folder, continue using them if they exist
                string legacyFolder = Path.Combine(configHome, "helion");
                if (Path.Exists(legacyFolder))
                    folder = legacyFolder;
                else
                    folder = Path.Combine(configHome, "Helion");
            }
        }

        // ensure the folder exists
        if (folder != null)
        {
            try
            {
                Directory.CreateDirectory(folder);
                return folder;
            }
            catch { }
        }

        // fall back to portable mode
        return AppContext.BaseDirectory;
    }

    /// <remarks>
    /// https://doomwiki.org/wiki/Environment_variables
    /// </remarks>
    public static List<string> GetWadFoldersFromEnvVars()
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

    public static List<string> GetWadFoldersFromSteamAndLinuxDirs()
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
            paths.AddRange(GetLinuxUserDoomDirs());
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

    private static List<string> GetLinuxUserDoomDirs()
    {
        List<string> paths = [];

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            paths.Add($"{xdgDataHome}/doom");
            paths.Add($"{xdgDataHome}/games/doom");
        }

        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
        {
            paths.Add($"{home}/.local/share/doom");
            paths.Add($"{home}/.local/share/games/doom");
        }

        return paths;
    }
}

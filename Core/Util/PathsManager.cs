using System;
using System.Collections.Generic;
using System.IO;
using Helion.Util.Configs.Impl;
using Microsoft.Win32;

namespace Helion.Util;

/// <summary>
/// Tracks folders for assets and configs (and saves, screenshots, soundfonts etc.)
/// </summary>
public class PathsManager
{
    private readonly string m_configFolder;
    private readonly List<string> m_assetsFolders;
    private readonly List<string> m_soundFontsFolders;

    public string ConfigFolder => m_configFolder;
    public List<string> AssetsFolders => m_assetsFolders;
    /// <summary>
    /// Soundfonts can be in assets or config folders. The paths here do not include the /SoundFonts subfolder.
    /// </summary>
    public List<string> SoundFontsFolders => m_soundFontsFolders;

    private const string WindowsShellFoldersKey = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";
    private const string WindowsSavedGamesFolderGuid = "{4C5C32FF-BB9D-43b0-B5B4-2D72E54EAAA4}";

    public PathsManager(bool forcePortableMode = false)
    {
        var portableConfigFile = Path.Combine(AppContext.BaseDirectory, FileConfig.IniFile);
        m_configFolder = GetConfigFolder(forcePortableMode || File.Exists(portableConfigFile));
        m_assetsFolders = [AppContext.BaseDirectory];
        if (OperatingSystem.IsLinux())
            m_assetsFolders.Add("/usr/share/helion");
        m_soundFontsFolders = [.. m_assetsFolders, m_configFolder];
    }

    private static string GetConfigFolder(bool portableMode = false)
    {
        if (portableMode)
            return AppContext.BaseDirectory;

        // on Windows, use "~/Saved Games/Helion"
        if (OperatingSystem.IsWindows())
        {
            var registrySavedGamesFolder = (string?)Registry.GetValue(WindowsShellFoldersKey, WindowsSavedGamesFolderGuid, null);
            if (registrySavedGamesFolder != null)
                return Path.Combine(registrySavedGamesFolder, "Helion");
            var userDir = Environment.GetEnvironmentVariable("USERPROFILE");
            if (userDir != null)
                return Path.Combine(userDir, "Saved Games", "Helion");
        }

        // On Linux, default to "$XDG_CONFIG_HOME/helion"
        else if (OperatingSystem.IsLinux())
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                return $"{xdgConfigHome}/helion";

            // Fallback to "$HOME/.config/helion"
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(home))
                return $"{home}/.config/helion";
        }

        return AppContext.BaseDirectory;
    }
}
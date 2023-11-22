using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Resources.IWad;

public class IWadLocator
{
    private static readonly string[] SteamDoomDirs = new[]
    {
        "steamapps/common/ultimate doom/base",
        "steamapps/common/doom 2/base",
        "steamapps/common/Doom 2/masterbase",
        "steamapps/common/Doom 2/finaldoombase",
        "steamapps/common/final doom/base",
        "steamapps/common/DOOM 3 BFG Edition/base/wads",
    };

    private readonly List<string> m_directories;

    public static IWadLocator CreateDefault()
    {
        List<string> paths = new() { Directory.GetCurrentDirectory() };

        string steamPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam/");
        if (Directory.Exists(steamPath))
        {
            foreach (var dir in SteamDoomDirs)
                paths.Add(Path.Combine(steamPath, dir));
        }

        return new IWadLocator(paths);
    }

    public IWadLocator(IEnumerable<string> directories)
    {
        m_directories = directories.ToList();
    }

    public List<IWadPath> Locate()
    {
        List<IWadPath> iwads = new();
        HashSet<IWadType> foundTypes = new();
        foreach (var dir in m_directories)
        {
            if (!Directory.Exists(dir))
                continue;

            EnumerateDirectory(iwads, foundTypes, dir);
        }

        iwads.Sort((i1, i2) => i1.Info.IWadType.CompareTo(i2.Info.IWadType));
        return iwads;
    }

    private static void EnumerateDirectory(List<IWadPath> iwads, HashSet<IWadType> foundTypes, string dir)
    {
        try
        {
            var files = Directory.EnumerateFiles(dir, "*")
                .Where(x => Path.GetExtension(x).Equals(".wad", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                IWadInfo iwadInfo = IWadInfo.GetIWadInfo(file);
                if (iwadInfo != IWadInfo.DefaultIWadInfo && !foundTypes.Contains(iwadInfo.IWadType))
                {
                    foundTypes.Add(iwadInfo.IWadType);
                    iwads.Add(new(file, iwadInfo));
                }
            }
        }
        catch { }
    }
}

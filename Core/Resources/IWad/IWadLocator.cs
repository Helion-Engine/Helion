using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Resources.Archives;

namespace Helion.Resources.IWad;

public class IWadLocator
{
    private readonly List<string> m_directories;

    public static IWadLocator CreateDefault(IEnumerable<string> configDirectories)
    {
        List<string> paths = [Directory.GetCurrentDirectory(), .. configDirectories,
            .. WadPaths.GetFromSteamAndLinuxDirs(), .. WadPaths.GetFromEnvVars()];
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

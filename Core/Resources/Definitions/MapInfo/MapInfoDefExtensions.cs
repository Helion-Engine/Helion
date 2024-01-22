using Helion.Resources.Archives.Collection;
using System.Text.RegularExpressions;

namespace Helion.Resources.Definitions.MapInfo;

public static class MapInfoDefExtensions
{
    private static readonly Regex[] PrefixRegex = new Regex[]
    {
         new(@"^\S+: ", RegexOptions.IgnoreCase | RegexOptions.Compiled),
         new(@"^level .+[:-]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
         new(@"^map .+[:-]", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public static string GetDisplayNameWithPrefix(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
    {
        if (mapInfo.DisplayNameWithPrefix != null)
            return mapInfo.DisplayNameWithPrefix;

        string displayName = mapInfo.GetNiceNameOrLookup(archiveCollection);
        mapInfo.DisplayName = ReplaceMapNamePrefix(mapInfo, displayName);
        mapInfo.DisplayNameWithPrefix = displayName;
        if (ShouldAddMapPrefix(mapInfo, displayName))
            mapInfo.DisplayNameWithPrefix = $"{mapInfo.MapName}: {displayName}";
        return mapInfo.DisplayNameWithPrefix;
    }

    public static string GetMapNameWithPrefix(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
    {
        return mapInfo.GetDisplayNameWithPrefix(archiveCollection);    
    }

    private static string GetNiceNameOrLookup(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
    {
        if (mapInfo.DisplayName != null)
            return mapInfo.DisplayName;

        string displayName = string.Empty;
        if (mapInfo.NiceName.Length > 0)
            displayName = mapInfo.NiceName;
        else if (mapInfo.LookupName.Length > 0)
            displayName = archiveCollection.Definitions.Language.GetMessage(mapInfo.LookupName);

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Unknown";

        return displayName;
    }

    private static bool ShouldAddMapPrefix(MapInfoDef mapInfo, string displayName)
    {
        foreach (Regex regex in PrefixRegex)
        {
            Match match = regex.Match(displayName);
            if (match.Success && match.Index == 0)
                return false;
        }

        return true;
    }

    private static string ReplaceMapNamePrefix(MapInfoDef mapInfo, string displayName)
    {
        if (displayName.StartsWith(mapInfo.MapName))
        {
            displayName = displayName.Replace(mapInfo.MapName, string.Empty).Trim();
            displayName = displayName.TrimStart(':').TrimStart('-').Trim();
            return displayName;
        }

        foreach (Regex regex in PrefixRegex)
        {
            Match match = regex.Match(displayName);
            if (!match.Success)
                continue;
            displayName = displayName.Replace(match.Value, string.Empty).Trim();
            displayName = displayName.TrimStart(':').TrimStart('-').Trim();
        }

        return displayName;
    }
}

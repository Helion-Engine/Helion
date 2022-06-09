using Helion.Resources.Archives.Collection;
using System.Text.RegularExpressions;

namespace Helion.Resources.Definitions.MapInfo;

public static class MapInfoDefExtensions
{
    private static readonly Regex[] PrefixRegex = new Regex[]
    {
         new(@"^level .+[:-]", RegexOptions.IgnoreCase),
         new(@"^map .+[:-]", RegexOptions.IgnoreCase)
    };

    public static string GetNiceNameOrLookup(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
    {
        string displayName = mapInfo.NiceName;
        if (mapInfo.LookupName.Length > 0)
            displayName = archiveCollection.Definitions.Language.GetMessage(mapInfo.LookupName);

        if (mapInfo.MapName.Length > 0)
            displayName = ReplaceMapNamePrefix(mapInfo, displayName);

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Unknown";

        return displayName;
    }

    public static string GetMapNameWithPrefix(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
    {
        string displayName = GetNiceNameOrLookup(mapInfo, archiveCollection);
        return $"{mapInfo.MapName}: {displayName}";
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

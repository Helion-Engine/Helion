using Helion.Resources.Definitions.Language;
using Helion.Util.Extensions;
using System;
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

    public static string GetDisplayNameWithPrefix(this MapInfoDef mapInfo, LanguageDefinition language)
    {
        if (mapInfo.DisplayNameWithPrefix != null)
            return mapInfo.DisplayNameWithPrefix;

        string displayName = mapInfo.GetNiceNameOrLookup(language);
        mapInfo.DisplayName = ReplaceMapNamePrefix(mapInfo, displayName);
        mapInfo.DisplayNameWithPrefix = displayName;
        if (ShouldAddMapPrefix(mapInfo, displayName))
            mapInfo.DisplayNameWithPrefix = $"{mapInfo.Label}: {displayName}";
        return mapInfo.DisplayNameWithPrefix;
    }

    public static string GetMapNameWithPrefix(this MapInfoDef mapInfo, LanguageDefinition language)
    {
        return mapInfo.GetDisplayNameWithPrefix(language);    
    }

    public static string GetNiceNameOrLookup(this MapInfoDef mapInfo, LanguageDefinition language)
    {
        if (mapInfo.DisplayName != null)
            return mapInfo.DisplayName;

        string displayName = string.Empty;
        if (mapInfo.NiceName.Length > 0)
            displayName = mapInfo.NiceName;
        else if (mapInfo.LookupName.Length > 0)
            displayName = language.GetMessage(mapInfo.LookupName);

        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Unknown";

        return displayName;
    }

    private static bool ShouldAddMapPrefix(MapInfoDef mapInfo, string displayName)
    {
        if (string.IsNullOrEmpty(mapInfo.Label))
            return false;

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
        if (displayName.StartsWithIgnoreCase(mapInfo.MapName))
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

using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Language;

namespace Helion.Resources.Definitions.MapInfo
{
    public static class MapInfoDefExtensions
    {
        public static string GetNiceNameOrLookup(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
        {
            string displayName = mapInfo.NiceName;
            if (mapInfo.LookupName.Length > 0)
            {
                displayName = archiveCollection.Definitions.Language.GetIWadMessage(mapInfo.LookupName,
                    archiveCollection.GetIWadInfo().IWadBaseType, IWadLanguageMessageType.LevelName);
            }

            if (mapInfo.MapName.Length > 0 && displayName.StartsWith(mapInfo.MapName))
            {
                displayName = displayName.Replace(mapInfo.MapName, string.Empty).Trim();
                displayName = displayName.TrimStart(':').TrimStart('-').Trim();
            }

            return displayName;
        }

        public static string GetMapNameWithPrefix(this MapInfoDef mapInfo, ArchiveCollection archiveCollection)
        {
            string displayName = GetNiceNameOrLookup(mapInfo, archiveCollection);
            return $"{mapInfo.MapName}: {displayName}";
        }
    }
}

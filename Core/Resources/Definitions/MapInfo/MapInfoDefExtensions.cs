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

            return displayName;
        }
    }
}

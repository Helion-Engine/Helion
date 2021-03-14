namespace Helion.Resources.IWad
{
    public enum IWadType
    {
        None,
        Doom2,
        Plutonia,
        TNT,
        UltimateDoom,
        DoomShareware,
        ChexQuest
    }
    
    public static class IWadTypeExtensions 
    {
        /// <summary>
        /// Checks if this is a Doom 1 styled wad.
        /// </summary>
        /// <param name="iwadType">The IWad type.</param>
        /// <returns>If it is like Doom 1 (episodes, same lump names), or not.
        /// </returns>
        public static bool IsDoom1(this IWadType iwadType)
        {
            return iwadType == IWadType.UltimateDoom || 
                   iwadType == IWadType.DoomShareware ||
                   iwadType == IWadType.ChexQuest;
        }
    }
}

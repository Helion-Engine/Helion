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
        public static bool IsDoom1(this IWadType iwadType)
        {
            return iwadType == IWadType.UltimateDoom || iwadType == IWadType.DoomShareware;
        }
    }
}

namespace Helion.Resources.IWad;

public enum IWadBaseType
{
    None,
    Doom2,
    Plutonia,
    TNT,
    Doom1,
    ChexQuest
}

public static class IWadEpisodes 
{
    public static bool HasEpisodes(this IWadBaseType iwadType)
    {
        return iwadType is IWadBaseType.Doom1 or IWadBaseType.ChexQuest;
    }
}

using System;

namespace Helion.Resources.Definitions.MapInfo;

[Flags]
public enum FindMapResultOptions
{
    None,
    EndGame
}

public struct FindMapResult
{
    public MapInfoDef? MapInfo;
    public string MapName;
    public string Error;
    public FindMapResultOptions Options;

    public static FindMapResult CreateEndGame(string mapName) => new(mapName, FindMapResultOptions.EndGame);

    public static FindMapResult Create(MapInfoDef mapInfo, string mapName)
    {
        return new()
        {
            MapInfo = mapInfo,
            MapName = mapName,
            Error = string.Empty
        };
    }

    public static FindMapResult CreateMapNameError(string mapName)
    {
        return new()
        {
            MapInfo = null,
            Error = $"Failed to find map {mapName}"
        };
    }

    public static FindMapResult CreateError(string error)
    {
        return new()
        {
            MapInfo = null,
            Error = error
        };
    }

    public FindMapResult(string mapName, FindMapResultOptions options)
    {
        MapName = mapName;
        Options = options;
        Error = string.Empty;
    }
}
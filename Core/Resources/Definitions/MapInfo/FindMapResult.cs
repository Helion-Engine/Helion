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

    public static FindMapResult CreateEmptyResult(string mapName, FindMapResultOptions options) => new(mapName, options);

    public static FindMapResult Create(MapInfoDef? mapInfo, string mapName)
    {
        var result = new FindMapResult();
        result.MapInfo = mapInfo;
        result.MapName = mapName;

        if (mapInfo == null)
            result.Error = $"Failed to find map {mapName}";
        else
            result.Error = string.Empty;
        return result;
    }

    public FindMapResult(string mapName, FindMapResultOptions options)
    {
        MapName = mapName;
        Options = options;
        Error = string.Empty;
    }
}
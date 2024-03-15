using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MusInfo;

public class MusInfoDef
{
    public readonly int Number;
    public readonly string Name;
    public byte[]? MusicData;

    public MusInfoDef(int number, string name)
    {
        Number = number;
        Name = name;
    }
}

public class MusInfoMap
{
    public string MapName {  get; set; }
    public readonly List<MusInfoDef> Music = new();

    public MusInfoMap(string mapName)
    {
        MapName = mapName;
    }
}

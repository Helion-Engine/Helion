using System;

namespace Helion.Resources.Definitions.MapInfo;

public class SkyDef : ICloneable
{
    public string Name { get; set; } = string.Empty;
    public int ScrollSpeed { get; set; }

    public object Clone()
    {
        return new SkyDef
        {
            Name = Name,
            ScrollSpeed = ScrollSpeed
        };
    }
}


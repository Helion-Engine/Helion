using Helion.Resources.Definitions.MapInfo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.World;

public class GlobalData
{
    public IList<MapInfoDef> VisitedMaps { get; set; } = new List<MapInfoDef>();
    public int TotalTime { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not GlobalData data)
            return false;

        if (data.VisitedMaps.Count != VisitedMaps.Count)
            return false;

        if (data.TotalTime != TotalTime)
            return false;

        foreach (var map in data.VisitedMaps)
        {
            if (!VisitedMaps.Any(x => x.MapName == map.MapName))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

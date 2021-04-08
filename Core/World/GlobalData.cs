using Helion.Resources.Definitions.MapInfo;
using System;
using System.Collections.Generic;

namespace Helion.World
{
    public class GlobalData
    {
        public IList<MapInfoDef> VisitedMaps { get; set; } = new List<MapInfoDef>();
        public int TotalTime { get; set; }
    }
}

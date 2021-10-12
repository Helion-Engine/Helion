using System.Collections.Generic;

namespace Helion.Resources.Definitions.MapInfo;

public class ClusterDef
{
    public int ClusterNum { get; set; }
    public List<string> EnterText { get; set; } = new List<string>();
    public List<string> ExitText { get; set; } = new List<string>();
    public string Music { get; set; } = string.Empty;
    public string Flat { get; set; } = string.Empty;
    public string Pic { get; set; } = string.Empty;
    public bool IsHub { get; set; }
    public bool IsExitTextLump { get; set; }
    public bool AllowIntermission { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is ClusterDef cluster && cluster.ClusterNum == ClusterNum)
            return true;
        return false;
    }

    public override int GetHashCode()
    {
        return ClusterNum;
    }
}

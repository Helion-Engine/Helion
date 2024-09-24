using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo;

public class ClusterDef
{
    public int ClusterNum { get; set; }
    public List<string> EnterText { get; set; } = [];
    public List<string> ExitText { get; set; } = [];
    public List<string> SecretExitText { get; set; } = [];
    public string Music { get; set; } = string.Empty;
    public string Flat { get; set; } = string.Empty;
    public string Pic { get; set; } = string.Empty;
    public bool IsHub { get; set; }
    public bool IsExitTextLump { get; set; }
    public bool AllowIntermission { get; set; }

    public ClusterDef(int number)
    {
        ClusterNum = number;
    }

    public ClusterDef Clone(int newClusterNum)
    {
        return new(newClusterNum)
        {
            EnterText = [.. EnterText],
            ExitText = [.. ExitText],
            SecretExitText = [.. SecretExitText],
            Flat = Flat,
            Pic = Pic,
            IsHub = IsHub,
            IsExitTextLump = IsExitTextLump,
            AllowIntermission = AllowIntermission
        };
    }

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

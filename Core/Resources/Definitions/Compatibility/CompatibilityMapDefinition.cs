using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;

namespace Helion.Resources.Definitions.Compatibility;

public class CompatibilityMapDefinition
{
    public readonly List<ILineDefinition> Lines = [];
    public readonly List<ISideDefinition> Sides = [];
    public readonly List<int> MidTextureHackSectors = [];
    public readonly List<int> NoRenderFloorSectors = [];
    public readonly List<int> NoRenderCeilingSectors = [];
    public readonly List<int> MaxDistanceOverrideTags = [];
    public int MaxDistanceOverride;
}

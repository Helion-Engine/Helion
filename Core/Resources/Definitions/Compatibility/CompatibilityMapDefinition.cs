using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;

namespace Helion.Resources.Definitions.Compatibility;

public class CompatibilityMapDefinition
{
    public readonly List<ILineDefinition> Lines = new();
    public readonly List<ISideDefinition> Sides = new();
    public readonly List<int> MidTextureHackSectors = new();
    public readonly List<int> NoRenderFloorSectors = new();
    public readonly List<int> NoRenderCeilingSectors = new();
}

using System.Collections.Generic;
using Helion.Resources.Definitions.Compatibility.Lines;
using Helion.Resources.Definitions.Compatibility.Sides;

namespace Helion.Resources.Definitions.Compatibility;

public class CompatibilityMapDefinition
{
    public readonly List<ILineDefinition> Lines = new List<ILineDefinition>();
    public readonly List<ISideDefinition> Sides = new List<ISideDefinition>();
}

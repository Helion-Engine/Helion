using System.Collections.Generic;
using Helion.Resource.Definitions.Compatibility.Lines;
using Helion.Resource.Definitions.Compatibility.Sides;

namespace Helion.Resource.Definitions.Compatibility
{
    public class CompatibilityMapDefinition
    {
        public readonly List<ILineDefinition> Lines = new List<ILineDefinition>();
        public readonly List<ISideDefinition> Sides = new List<ISideDefinition>();
    }
}
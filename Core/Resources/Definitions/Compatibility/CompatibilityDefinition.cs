using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resources.Definitions.Compatibility
{
    public class CompatibilityDefinition
    {
        public readonly Dictionary<CIString, CompatibilityMapDefinition> MapDefinitions = new Dictionary<CIString, CompatibilityMapDefinition>();
    }
}
using System.Collections.Generic;
using Helion.Util;

namespace Helion.Resource.Definitions.Compatibility
{
    public class CompatibilityDefinition
    {
        public readonly Dictionary<CIString, CompatibilityMapDefinition> MapDefinitions = new Dictionary<CIString, CompatibilityMapDefinition>();
    }
}
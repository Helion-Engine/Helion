using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Compatibility
{
    public class CompatibilityDefinition
    {
        public readonly Dictionary<string, CompatibilityMapDefinition> MapDefinitions = new(StringComparer.OrdinalIgnoreCase);
    }
}
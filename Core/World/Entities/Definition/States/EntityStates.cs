using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition.States;

public class EntityStates
{
    public int FrameCount { get; set; }
    public Dictionary<string, int> Labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}

using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition.States
{
    public class EntityStates
    {
        public Dictionary<string, int> Labels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public List<EntityFrame> Frames = new List<EntityFrame>();
    }
}
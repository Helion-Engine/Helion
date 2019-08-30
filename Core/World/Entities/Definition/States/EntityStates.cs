using System.Collections.Generic;
using Helion.Util;

namespace Helion.World.Entities.Definition.States
{
    public class EntityStates
    {
        public Dictionary<CIString, int> Labels = new Dictionary<CIString, int>();
        public List<EntityFrame> Frames = new List<EntityFrame>();
    }
}
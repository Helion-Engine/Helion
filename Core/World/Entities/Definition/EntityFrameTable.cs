using Helion.World.Entities.Definition.States;
using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition
{
    public class EntityFrameTable
    {
        // Lookup for dehacked
        // e.g. key = "zombieman::spawn", "shotgunguy:missile"
        public Dictionary<string, FrameSet> FrameSets = new(StringComparer.OrdinalIgnoreCase);

        // Master frame table
        public List<EntityFrame> Frames = new();
    }
}

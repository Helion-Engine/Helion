using Helion.World.Entities.Definition.States;
using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition
{
    public static class EntityFrameTable
    {
        // Lookup for dehacked
        // e.g. key = "zombieman::spawn", "shotgunguy:missile"
        public static Dictionary<string, FrameSet> FrameSets = new(StringComparer.OrdinalIgnoreCase);

        // Master frame table
        public static List<EntityFrame> Frames = new();
    }
}

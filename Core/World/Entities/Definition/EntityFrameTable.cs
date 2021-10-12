using Helion.World.Entities.Definition.States;
using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition;

public class EntityFrameTable
{
    private readonly Dictionary<string, FrameSet> m_frameSets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EntityFrame> m_frames = new();
    private readonly Dictionary<int, EntityFrame> m_vanillaFrameMap = new();

    // Lookup for dehacked
    // e.g. key = "zombieman::spawn", "shotgunguy:missile"
    public Dictionary<string, FrameSet> FrameSets => m_frameSets;
    // Master frame table
    public List<EntityFrame> Frames => m_frames;
    // Lookup by vanilla frame index
    public IDictionary<int, EntityFrame> VanillaFrameMap => m_vanillaFrameMap;

    public void AddFrame(EntityFrame entityFrame)
    {
        entityFrame.MasterFrameIndex = Frames.Count;
        Frames.Add(entityFrame);
        m_vanillaFrameMap[entityFrame.VanillaIndex] = entityFrame;
    }
}


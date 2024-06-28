using Helion.Util;
using Helion.World.Entities.Definition.States;
using System;
using System.Collections.Generic;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.World.Entities.Definition;

public class EntityFrameTable
{
    private readonly Dictionary<string, FrameSet> m_frameSets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EntityFrame> m_frames = new();
    private readonly Dictionary<int, EntityFrame> m_vanillaFrameMap = new();
    private readonly Dictionary<string, int> m_spriteNameToIndex = new(StringComparer.OrdinalIgnoreCase);
    private int m_spriteIndex;
    private bool m_vileHealFrameSet;
    private EntityFrame? m_vileHealFrame;
    private int m_bloodIndex = -1;

    // Lookup for dehacked
    // e.g. key = "zombieman::spawn", "shotgunguy:missile"
    public Dictionary<string, FrameSet> FrameSets => m_frameSets;
    // Master frame table
    public List<EntityFrame> Frames => m_frames;
    // Lookup by vanilla frame index
    public IDictionary<int, EntityFrame> VanillaFrameMap => m_vanillaFrameMap;

    public int ClosetLookFrameIndex { get; set; }
    public int ClosetChaseFrameIndex { get; set; }

    public EntityFrame? GetVileHealFrame()
    {
        if (m_vileHealFrameSet)
            return m_vileHealFrame;

        m_vileHealFrameSet = true;
        VanillaFrameMap.TryGetValue((int)ThingState.VILE_HEAL1, out m_vileHealFrame);
        return m_vileHealFrame;
    }

    public int GetBloodIndex()
    {
        if (m_bloodIndex != -1)
            return m_bloodIndex;

        m_bloodIndex = Constants.NullFrameIndex;
        if (VanillaFrameMap.TryGetValue((int)ThingState.BLOOD1, out var frame))
            m_bloodIndex = frame.MasterFrameIndex;
        return m_bloodIndex;
    }

    public int GetSpriteIndex(string spriteName)
    {
        if (!m_spriteNameToIndex.TryGetValue(spriteName, out int spriteIndex))
        {
            spriteIndex = m_spriteIndex;
            m_spriteNameToIndex[spriteName] = spriteIndex;
            m_spriteIndex++;
        }

        return spriteIndex;
    }

    public int SpriteIndexCount => m_spriteNameToIndex.Count;

    public void AddFrame(EntityFrame entityFrame)
    {
        entityFrame.MasterFrameIndex = Frames.Count;
        Frames.Add(entityFrame);
        m_vanillaFrameMap[entityFrame.VanillaIndex] = entityFrame;
        entityFrame.SpriteIndex = GetSpriteIndex(entityFrame.Sprite);
    }

    public void AddCustomFrames()
    {
        EntityFrame entityFrame = new(this, Constants.InvisibleSprite, 0, 16, EntityFrameProperties.Default, EntityActionFunctions.A_ClosetLook, 0, string.Empty);
        AddFrame(entityFrame);
        entityFrame.NextFrameIndex = entityFrame.MasterFrameIndex;
        ClosetLookFrameIndex = entityFrame.MasterFrameIndex;

        entityFrame = new(this, Constants.InvisibleSprite, 0, 16, EntityFrameProperties.Default, EntityActionFunctions.A_ClosetChase, 0, string.Empty);
        AddFrame(entityFrame);
        entityFrame.NextFrameIndex = entityFrame.MasterFrameIndex;
        ClosetChaseFrameIndex = entityFrame.MasterFrameIndex;
    }
}

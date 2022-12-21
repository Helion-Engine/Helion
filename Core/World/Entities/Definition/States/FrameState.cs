using Helion.Resources.Definitions.Decorate.States;
using Helion.Models;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;
using System;

namespace Helion.World.Entities.Definition.States;

/// <summary>
/// A simple state wrapper that allows us to advance the state.
/// </summary>
public struct FrameState
{
    private const int InfiniteLoopLimit = 10000;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EntityFrame Frame;
    private Entity m_entity;
    private EntityDefinition m_definition;
    private EntityManager m_entityManager;
    private EntityFrameTable m_frameTable;
    private bool m_destroyOnStop;
    private int m_frameIndex;
    private int m_tics;

    public int CurrentTick => m_tics;

    public FrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, bool destroyOnStop = true)
    {
        m_entity = entity;
        m_definition = definition;
        m_frameTable = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable;
        m_entityManager = entityManager;
        m_destroyOnStop = destroyOnStop;
        Frame = m_frameTable.Frames[m_frameIndex];
    }

    public FrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, FrameStateModel frameStateModel)
    {
        m_entity = entity;
        m_definition = definition;
        m_frameTable = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable;
        m_entityManager = entityManager;
        m_frameIndex = frameStateModel.FrameIndex;
        m_tics = frameStateModel.Tics;
        m_destroyOnStop = frameStateModel.Destroy;
        Frame = m_frameTable.Frames[m_frameIndex];
    }

    public EntityFrame? GetStateFrame(string label)
    {
        if (m_definition.States.Labels.TryGetValue(label, out int index))
            return m_frameTable.Frames[index];

        return null;
    }

    // Only for end game cast - really shouldn't be used.
    public void SetFrameIndexByLabel(string label)
    {
        if (m_definition.States.Labels.TryGetValue(label, out int index))
            SetFrameIndexMember(index);
    }

    public void SetFrameIndex(int index)
    {
        if (index < 0 || index >= m_frameTable.Frames.Count)
            return;

        SetFrameIndexMember(index);
        SetFrameIndexInternal(index, null);
    }

    public void SetFrameIndexNoAction(int index)
    {
        if (index < 0 || index >= m_frameTable.Frames.Count)
            return;

        SetFrameIndexMember(index);
        m_tics = Frame.Ticks;
    }

    public bool SetState(string label, int offset = 0, bool warn = true, bool executeStateFunctions = true, Action<EntityFrame>? onSet = null)
    {
        if (!executeStateFunctions)
            return SetStateNoAction(label, offset, warn);

        if (m_definition.States.Labels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < m_frameTable.Frames.Count)
                SetFrameIndexInternal(index + offset, onSet);
            else
                SetFrameIndexInternal(index, onSet);

            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, m_definition.Name);

        return false;
    }

    public bool SetStateNoAction(string label, int offset = 0, bool warn = true)
    {
        if (m_definition.States.Labels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < m_frameTable.Frames.Count)
                SetFrameIndexMember(index + offset);
            else
                SetFrameIndexMember(m_frameIndex = index);

            m_tics = Frame.Ticks;
            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, m_definition.Name);

        return false;
    }

    public void SetState(EntityFrame entityFrame) =>
        SetFrameIndexInternal(entityFrame.MasterFrameIndex, null);

    public bool IsState(string label)
    {
        if (m_definition.States.Labels.TryGetValue(label, out int index))
            return m_frameIndex == index;

        return false;
    }

    public void SetTics(int tics)
    {
        if (tics < 1)
            tics = 1;
        m_tics = tics;
    }

    private void SetFrameIndexMember(int index)
    {
        m_frameIndex = index;
        Frame = m_frameTable.Frames[m_frameIndex];
    }

    private void SetFrameIndexInternal(int index, Action<EntityFrame>? onSet)
    {
        int loopCount = 0;

        while (true)
        {
            SetFrameIndexMember(index);
            m_tics = Frame.Ticks;

            if (m_entity.World.IsFastMonsters && Frame.Properties.Fast)
                m_tics /= 2;

            if (m_entity.World.SkillDefinition.SlowMonsters && Frame.Properties.Slow)
                m_tics *= 2;

            onSet?.Invoke(Frame);

            if (m_destroyOnStop && Frame.IsNullFrame)
            {
                m_entityManager.Destroy(m_entity);
                return;
            }

            loopCount++;
            if (loopCount > InfiniteLoopLimit)
            {
                LogStackError();
                return;
            }

            Frame.ActionFunction?.Invoke(m_entity);
            if (m_entity == null || m_frameIndex == Constants.NullFrameIndex)
                return;

            if (Frame.BranchType == ActorStateBranch.Stop && Frame.Ticks >= 0)
            {
                if (m_destroyOnStop)
                {
                    m_entityManager.Destroy(m_entity);
                    return;
                }
                break;
            }

            if (Frame.Ticks != 0)
                break;

            index = Frame.NextFrameIndex;
        }
    }

    private void LogStackError()
    {
        string method = string.Empty;
        if (Frame.ActionFunction != null)
            method = $"function '{Frame.ActionFunction.Method.Name}'";

        Log.Error($"Stack limit reached for '{m_entity.Definition.Name}' {method}");
    }

    public void Tick()
    {
        Precondition(m_frameIndex >= 0 && m_frameIndex < m_frameTable.Frames.Count, "Out of range frame index for entity");
        if (m_tics == -1)
            return;

        m_tics--;
        if (m_tics <= 0)
            SetFrameIndexInternal(Frame.NextFrameIndex, null);
    }

    public FrameStateModel ToFrameStateModel()
    {
        return new FrameStateModel()
        {
            FrameIndex = m_frameIndex,
            Tics = m_tics,
            Destroy = m_destroyOnStop
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FrameState frameState)
            return false;

        return frameState.m_entity.Id == m_entity.Id &&
            frameState.m_frameIndex == m_frameIndex &&
            frameState.m_tics == m_tics &&
            frameState.m_destroyOnStop == m_destroyOnStop;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

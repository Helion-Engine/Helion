using Helion.Resources.Definitions.Decorate.States;
using Helion.Models;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;
using System;
using System.Collections.Generic;

namespace Helion.World.Entities.Definition.States;

/// <summary>
/// A simple state wrapper that allows us to advance the state.
/// </summary>
public struct FrameState
{
    private const int InfiniteLoopLimit = 10000;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static int SlowTickOffsetChase = 0;
    private static int SlowTickOffsetLook = 0;
    private static int SlowTickOffsetTracer = 0;

    public EntityFrame Frame;
    private Entity m_entity;
    private Dictionary<string, int> m_stateLabels;
    private EntityManager m_entityManager;
    private List<EntityFrame> m_frames;
    private bool m_destroyOnStop;

    public int CurrentTick;
    public int FrameIndex;

    public FrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, bool destroyOnStop = true)
    {
        m_entity = entity;
        m_stateLabels = definition.States.Labels;
        m_frames = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable.Frames;
        m_entityManager = entityManager;
        m_destroyOnStop = destroyOnStop;
        Frame = m_frames[FrameIndex];
    }

    public FrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, FrameStateModel frameStateModel)
    {
        m_entity = entity;
        m_stateLabels = definition.States.Labels;
        m_frames = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable.Frames;
        m_entityManager = entityManager;
        FrameIndex = frameStateModel.FrameIndex;
        CurrentTick = frameStateModel.Tics;
        m_destroyOnStop = frameStateModel.Destroy;
        Frame = m_frames[FrameIndex];
    }

    public EntityFrame? GetStateFrame(string label)
    {
        if (m_stateLabels.TryGetValue(label, out int index))
            return m_frames[index];

        return null;
    }

    // Only for end game cast - really shouldn't be used.
    public void SetFrameIndexByLabel(string label)
    {
        if (m_stateLabels.TryGetValue(label, out int index))
            SetFrameIndexMember(index);
    }

    public void SetFrameIndex(int index)
    {
        if (index < 0 || index >= m_frames.Count)
            return;

        SetFrameIndexMember(index);
        SetFrameIndexInternal(index, null);
    }

    public void SetFrameIndexNoAction(int index)
    {
        if (index < 0 || index >= m_frames.Count)
            return;

        SetFrameIndexMember(index);
        CurrentTick = Frame.Ticks;
    }

    public bool SetState(string label, int offset = 0, bool warn = true, bool executeStateFunctions = true, Action<EntityFrame>? onSet = null)
    {
        if (!executeStateFunctions)
            return SetStateNoAction(label, offset, warn);

        if (m_stateLabels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < m_frames.Count)
                SetFrameIndexInternal(index + offset, onSet);
            else
                SetFrameIndexInternal(index, onSet);

            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, m_entity.Definition.Name);

        return false;
    }

    public bool SetStateNoAction(string label, int offset = 0, bool warn = true)
    {
        if (m_stateLabels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < m_frames.Count)
                SetFrameIndexMember(index + offset);
            else
                SetFrameIndexMember(FrameIndex = index);

            CurrentTick = Frame.Ticks;
            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, m_entity.Definition.Name);

        return false;
    }

    public void SetState(EntityFrame entityFrame) =>
        SetFrameIndexInternal(entityFrame.MasterFrameIndex, null);

    public bool IsState(string label)
    {
        if (m_stateLabels.TryGetValue(label, out int index))
            return FrameIndex == index;

        return false;
    }

    public void SetTics(int tics)
    {
        if (tics < 1)
            tics = 1;
        CurrentTick = tics;
    }

    private void SetFrameIndexMember(int index)
    {
        FrameIndex = index;
        Frame = m_frames[FrameIndex];
    }

    private void SetFrameIndexInternal(int index, Action<EntityFrame>? onSet)
    {
        int loopCount = 0;

        while (true)
        {
            SetFrameIndexMember(index);
            CurrentTick = Frame.Ticks;

            if (EntityStatic.IsFastMonsters && Frame.Properties.Fast)
                CurrentTick /= 2;

            if (EntityStatic.IsSlowMonsters && Frame.Properties.Slow)
                CurrentTick *= 2;

            CheckSlowTickDistance();
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
            if (m_entity == null || FrameIndex == Constants.NullFrameIndex)
                return;

            if (Frame.BranchType == ActorStateBranch.Stop && Frame.Ticks >= 0)
            {
                if (m_destroyOnStop)
                    return;
                break;
            }

            if (Frame.Ticks != 0)
                break;

            index = Frame.NextFrameIndex;
        }
    }

    private void CheckSlowTickDistance()
    {
        m_entity.SlowTickMultiplier = 1;
        if (EntityStatic.SlowTickDistance <= 0)
            return;

        if (m_entity.InMonsterCloset)
            return;

        if (CurrentTick > 0 &&
            (Frame.IsSlowTickTracer || Frame.IsSlowTickChase || Frame.IsSlowTickLook) &&
            (m_entity.LastRenderDistanceSquared > EntityStatic.SlowTickDistance * EntityStatic.SlowTickDistance ||
            m_entity.LastRenderGametick != m_entity.World.Gametick))
        {
            // Stagger the frame ticks using SlowTickOffset so they don't all run on the same gametick
            // Sets to a range of -1 to +2
            int offset = 0;
            if (Frame.IsSlowTickTracer)
            {
                m_entity.SlowTickMultiplier = EntityStatic.SlowTickTracerMultiplier;
                offset = (SlowTickOffsetTracer++ & 3) - 1;
            }
            else if (Frame.IsSlowTickChase)
            {
                m_entity.SlowTickMultiplier = EntityStatic.SlowTickChaseMultiplier;
                offset = (SlowTickOffsetChase++ & 3) - 1;
            }
            else if (Frame.IsSlowTickLook)
            {
                m_entity.SlowTickMultiplier = EntityStatic.SlowTickLookMultiplier;
                offset = (SlowTickOffsetLook++ & 3) - 1;
            }

            CurrentTick *= m_entity.SlowTickMultiplier + offset;
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
        Precondition(FrameIndex >= 0 && FrameIndex < m_frames.Count, "Out of range frame index for entity");
        if (CurrentTick == -1)
            return;

        CurrentTick--;
        if (CurrentTick <= 0)
        {
            if (Frame.BranchType == ActorStateBranch.Stop && m_destroyOnStop)
            {
                m_entityManager.Destroy(m_entity);
                return;
            }

            SetFrameIndexInternal(Frame.NextFrameIndex, null);
        }
    }

    public FrameStateModel ToFrameStateModel()
    {
        return new FrameStateModel()
        {
            FrameIndex = FrameIndex,
            Tics = CurrentTick,
            Destroy = m_destroyOnStop
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FrameState frameState)
            return false;

        return frameState.m_entity.Id == m_entity.Id &&
            frameState.FrameIndex == FrameIndex &&
            frameState.CurrentTick == CurrentTick &&
            frameState.m_destroyOnStop == m_destroyOnStop;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

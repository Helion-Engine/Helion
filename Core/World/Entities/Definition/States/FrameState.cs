using Helion.Resources.Definitions.Decorate.States;
using Helion.Models;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;
using System;
using System.Collections.Generic;
using Helion.World;

namespace Helion.World.Entities.Definition.States;

/// <summary>
/// A simple state wrapper that allows us to advance the state.
/// </summary>
public struct FrameState
{
    private const int InfiniteLoopLimit = 10000;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static int SlowTickOffsetChase;
    private static int SlowTickOffsetLook;
    private static int SlowTickOffsetTracer;

    public EntityFrame Frame;
    private readonly Entity m_entity;
    private readonly Dictionary<string, int> m_stateLabels;
    private readonly bool m_destroyOnStop;

    public int CurrentTick;
    public int FrameIndex;

    public FrameState(Entity entity, EntityDefinition definition,bool destroyOnStop = true)
    {
        m_entity = entity;
        m_stateLabels = definition.States.Labels;
        m_destroyOnStop = destroyOnStop;
        Frame = WorldStatic.Frames[FrameIndex];
    }

    public FrameState(Entity entity, EntityDefinition definition, FrameStateModel frameStateModel)
    {
        m_entity = entity;
        m_stateLabels = definition.States.Labels;
        FrameIndex = frameStateModel.FrameIndex;
        CurrentTick = frameStateModel.Tics;
        m_destroyOnStop = frameStateModel.Destroy;
        Frame = WorldStatic.Frames[FrameIndex];
    }

    public EntityFrame? GetStateFrame(string label)
    {
        if (m_stateLabels.TryGetValue(label, out int index))
            return WorldStatic.Frames[index];

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
        if (index < 0 || index >= WorldStatic.Frames.Count)
            return;

        SetFrameIndexMember(index);
        SetFrameIndexInternal(index);
    }

    public void SetFrameIndexNoAction(int index)
    {
        if (index < 0 || index >= WorldStatic.Frames.Count)
            return;

        SetFrameIndexMember(index);
        CurrentTick = Frame.Ticks;
    }

    public bool SetState(string label, int offset = 0, bool warn = true, bool executeStateFunctions = true)
    {
        if (!executeStateFunctions)
            return SetStateNoAction(label, offset, warn);

        if (m_stateLabels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < WorldStatic.Frames.Count)
                SetFrameIndexInternal(index + offset);
            else
                SetFrameIndexInternal(index);

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
            if (index + offset >= 0 && index + offset < WorldStatic.Frames.Count)
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
        SetFrameIndexInternal(entityFrame.MasterFrameIndex);

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
        Frame = WorldStatic.Frames[FrameIndex];
        m_entity.IsClosetLook = Frame.MasterFrameIndex == WorldStatic.ClosetLookFrameIndex;
        m_entity.IsClosetChase = Frame.MasterFrameIndex == WorldStatic.ClosetChaseFrameIndex;
    }

    private void SetFrameIndexInternal(int index)
    {
        int loopCount = 0;
        while (true)
        {
            SetFrameIndexMember(index);
            CurrentTick = Frame.Ticks;

            if (WorldStatic.IsFastMonsters && Frame.Properties.Fast)
                CurrentTick /= 2;

            if (WorldStatic.IsSlowMonsters && Frame.Properties.Slow)
                CurrentTick *= 2;

            CheckSlowTickDistance();
            // Doom set the offsets only if misc1 wasn't zero. Only was applied through the player sprite code.
            if (Frame.DehackedMisc1 != 0 && m_entity.PlayerObj != null)
            {
                m_entity.PlayerObj.WeaponOffset.X = Frame.DehackedMisc1;
                m_entity.PlayerObj.WeaponOffset.Y = Frame.DehackedMisc2;
            }

            if (m_destroyOnStop && Frame.IsNullFrame)
            {
                WorldStatic.EntityManager.Destroy(m_entity);
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
                break;

            if (Frame.Ticks != 0)
                break;

            index = Frame.NextFrameIndex;
        }
    }

    private void CheckSlowTickDistance()
    {
        m_entity.SlowTickMultiplier = 1;
        if (!WorldStatic.SlowTickEnabled || WorldStatic.SlowTickDistance <= 0)
            return;

        if (m_entity.InMonsterCloset || m_entity.IsPlayer)
            return;

        if (CurrentTick > 0 &&
            (Frame.IsSlowTickTracer || Frame.IsSlowTickChase || Frame.IsSlowTickLook) &&
            (m_entity.RenderDistanceSquared > WorldStatic.SlowTickDistance * WorldStatic.SlowTickDistance ||
            m_entity.LastRenderGametick != WorldStatic.World.Gametick))
        {
            // Stagger the frame ticks using SlowTickOffset so they don't all run on the same gametick
            // Sets to a range of -1 to +2
            int offset = 0;
            if (Frame.IsSlowTickChase && WorldStatic.SlowTickChaseMultiplier > 0)
            {
                m_entity.SlowTickMultiplier = WorldStatic.SlowTickChaseMultiplier;
                offset = (SlowTickOffsetChase++ & 3) - 1;
            }
            else if (Frame.IsSlowTickLook && WorldStatic.SlowTickLookMultiplier > 0)
            {
                m_entity.SlowTickMultiplier = WorldStatic.SlowTickLookMultiplier;
                offset = (SlowTickOffsetLook++ & 3) - 1;
            }
            else if (Frame.IsSlowTickTracer && WorldStatic.SlowTickTracerMultiplier > 0)
            {
                m_entity.SlowTickMultiplier = WorldStatic.SlowTickTracerMultiplier;
                offset = (SlowTickOffsetTracer++ & 3) - 1;
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
        Precondition(FrameIndex >= 0 && FrameIndex < WorldStatic.Frames.Count, "Out of range frame index for entity");
        if (CurrentTick == -1)
            return;

        CurrentTick--;
        if (CurrentTick <= 0)
        {
            if (Frame.BranchType == ActorStateBranch.Stop && m_destroyOnStop)
            {
                WorldStatic.EntityManager.Destroy(m_entity);
                return;
            }

            SetFrameIndexInternal(Frame.NextFrameIndex);
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

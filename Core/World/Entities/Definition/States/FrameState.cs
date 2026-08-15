using Helion.Resources.Definitions.Decorate.States;
using Helion.Models;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;
using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;

namespace Helion.World.Entities.Definition.States;

[Flags]
public enum FrameStateOptions
{
    None,
    DestroyOnStop = 1,
    PlayerSprite = 2
}

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
    public readonly FrameStateOptions Options;

    public int CurrentTick;
    public int FrameIndex;

    public FrameState(FrameStateOptions options)
    {
        Options = options;
        Frame = WorldStatic.Frames[FrameIndex];
    }

    public FrameState(Entity entity, FrameStateModel frameStateModel)
    {
        FrameIndex = frameStateModel.FrameIndex;
        CurrentTick = frameStateModel.Tics;

        if (frameStateModel.Destroy)
            Options |= FrameStateOptions.DestroyOnStop;
        if (frameStateModel.PlayerSprite)
            Options |= FrameStateOptions.PlayerSprite;

        Frame = WorldStatic.Frames[FrameIndex];
        if (Frame.MasterFrameIndex == WorldStatic.ClosetLookFrameIndex)
            entity.ClosetFlags |= ClosetFlags.ClosetLook;
        if (Frame.MasterFrameIndex == WorldStatic.ClosetChaseFrameIndex)
            entity.ClosetFlags |= ClosetFlags.ClosetChase;

        if ((entity.ClosetFlags & (ClosetFlags.ClosetLook | ClosetFlags.ClosetChase)) != 0)
            entity.ClosetFlags |= ClosetFlags.MonsterCloset;
    }

    public static EntityFrame? GetStateFrame(EntityDefinition def, string label)
    {
        if (def.States.Labels.TryGetValue(label, out int index))
            return WorldStatic.Frames[index];

        return null;
    }

    // Only for end game cast - really shouldn't be used.
    public void SetFrameIndexByLabel(Entity entity, string label)
    {
        if (entity.Definition.States.Labels.TryGetValue(label, out int index))
            SetFrameIndexMember(entity, index);
    }

    public void SetFrameIndex(Entity entity, int index)
    {
        if (index < 0 || index >= WorldStatic.Frames.Count)
            return;

        SetFrameIndexInternal(entity, index);
    }

    public void SetFrameIndexNoAction(Entity entity, int index)
    {
        if (index < 0 || index >= WorldStatic.Frames.Count)
            return;

        SetFrameIndexMember(entity, index);
        CurrentTick = Frame.Ticks;
    }

    public bool SetState(Entity entity, EntityDefinition def, string label, int offset = 0, bool warn = true, bool executeStateFunctions = true)
    {
        if (!executeStateFunctions)
            return SetStateNoAction(entity, label, offset, warn);

        if (def.States.Labels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < WorldStatic.Frames.Count)
                SetFrameIndexInternal(entity, index + offset);
            else
                SetFrameIndexInternal(entity, index);

            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, def.Name);

        return false;
    }

    public bool SetStateNoAction(Entity entity, string label, int offset = 0, bool warn = true)
    {
        if (entity.Definition.States.Labels.TryGetValue(label, out int index))
        {
            if (index + offset >= 0 && index + offset < WorldStatic.Frames.Count)
                SetFrameIndexMember(entity, index + offset);
            else
                SetFrameIndexMember(entity, FrameIndex = index);

            CurrentTick = Frame.Ticks;
            return true;
        }

        if (warn)
            Log.Warn("Unable to find state label {0} for actor {1}", label, entity.Definition.Name);

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(Entity entity, EntityFrame entityFrame) =>
        SetFrameIndexInternal(entity, entityFrame.MasterFrameIndex);

    public bool IsState(EntityDefinition def, string label)
    {
        if (def.States.Labels.TryGetValue(label, out int index))
            return FrameIndex == index;

        return false;
    }

    public void SetTics(int tics)
    {
        if (tics < 1)
            tics = 1;
        CurrentTick = tics;
    }

    private void SetFrameIndexMember(Entity entity, int index)
    {
        FrameIndex = index;
        Frame = WorldStatic.Frames[FrameIndex];

        if (entity.ClosetFlags == ClosetFlags.None)
            return;

        if (Frame.MasterFrameIndex == WorldStatic.ClosetLookFrameIndex)
            entity.ClosetFlags |= ClosetFlags.ClosetLook;
        if (Frame.MasterFrameIndex == WorldStatic.ClosetChaseFrameIndex)
            entity.ClosetFlags |= ClosetFlags.ClosetChase;
    }

    private void SetFrameIndexInternal(Entity entity, int index)
    {
        int loopCount = 0;
        while (true)
        {
            SetFrameIndexMember(entity, index);
            CurrentTick = Frame.Ticks;

            if (WorldStatic.IsFastMonsters && Frame.Properties.Fast)
                CurrentTick /= 2;

            if (WorldStatic.IsSlowMonsters && Frame.Properties.Slow)
                CurrentTick *= 2;

            CheckSlowTickDistance(entity);
            // Doom set the offsets only if misc1 wasn't zero. Only was applied through the player sprite code.
            if ((Options & FrameStateOptions.PlayerSprite) != 0 && Frame.DehackedMisc1 != 0 && entity.PlayerObj != null)
            {
                entity.PlayerObj.WeaponOffset.X = Frame.DehackedMisc1;
                entity.PlayerObj.WeaponOffset.Y = Frame.DehackedMisc2;
                entity.PlayerObj.PrevWeaponOffset.X = Frame.DehackedMisc1;
                entity.PlayerObj.PrevWeaponOffset.Y = Frame.DehackedMisc2;
            }

            if ((Options & FrameStateOptions.DestroyOnStop) != 0 && Frame.IsNullFrame)
            {
                WorldStatic.EntityManager.Destroy(entity);
                return;
            }

            loopCount++;
            if (loopCount > InfiniteLoopLimit)
            {
                LogStackError(entity.Definition);
                return;
            }

            Frame.ActionFunction?.Invoke(entity);
            if (entity == null || FrameIndex == Constants.NullFrameIndex)
                return;

            if (Frame.BranchType == ActorStateBranch.Stop && Frame.Ticks >= 0)
                break;

            if (Frame.Ticks != 0)
                break;

            index = Frame.NextFrameIndex;
        }
    }

    private void CheckSlowTickDistance(Entity entity)
    {
        entity.SlowTickMultiplier = 1;
        if (!WorldStatic.SlowTickEnabled || WorldStatic.SlowTickDistance <= 0)
            return;

        if (entity.ClosetFlags != ClosetFlags.None || entity.IsPlayer)
            return;

        if (CurrentTick > 0 &&
            (Frame.IsSlowTickTracer || Frame.IsSlowTickChase || Frame.IsSlowTickLook) &&
            (entity.RenderDistanceSquared > WorldStatic.SlowTickDistance * WorldStatic.SlowTickDistance ||
            entity.LastRenderGametick != WorldStatic.World.Gametick))
        {
            // Stagger the frame ticks using SlowTickOffset so they don't all run on the same gametick
            // Sets to a range of -1 to +2
            int offset = 0;
            if (Frame.IsSlowTickChase && WorldStatic.SlowTickChaseMultiplier > 0)
            {
                entity.SlowTickMultiplier = WorldStatic.SlowTickChaseMultiplier;
                offset = (SlowTickOffsetChase++ & 3) - 1;
            }
            else if (Frame.IsSlowTickLook && WorldStatic.SlowTickLookMultiplier > 0)
            {
                entity.SlowTickMultiplier = WorldStatic.SlowTickLookMultiplier;
                offset = (SlowTickOffsetLook++ & 3) - 1;
            }
            else if (Frame.IsSlowTickTracer && WorldStatic.SlowTickTracerMultiplier > 0)
            {
                entity.SlowTickMultiplier = WorldStatic.SlowTickTracerMultiplier;
                offset = (SlowTickOffsetTracer++ & 3) - 1;
            }

            CurrentTick *= entity.SlowTickMultiplier + offset;
        }
    }

    private void LogStackError(EntityDefinition def)
    {
        string method = string.Empty;
        if (Frame.ActionFunction != null)
            method = $"function '{Frame.ActionFunction.Method.Name}'";

        Log.Error($"Stack limit reached for '{def.Name}' {method}");
    }

    public void Tick(Entity entity)
    {
        Precondition(FrameIndex >= 0 && FrameIndex < WorldStatic.Frames.Count, "Out of range frame index for entity");
        if (CurrentTick == -1)
            return;

        CurrentTick--;
        if (CurrentTick == 0)
        {
            if (Frame.BranchType == ActorStateBranch.Stop && (Options & FrameStateOptions.DestroyOnStop) != 0)
            {
                WorldStatic.EntityManager.Destroy(entity);
                return;
            }

            SetFrameIndexInternal(entity, Frame.NextFrameIndex);
        }
    }

    public FrameStateModel ToFrameStateModel()
    {
        return new FrameStateModel()
        {
            FrameIndex = FrameIndex,
            Tics = CurrentTick,
            Destroy = (Options & FrameStateOptions.DestroyOnStop) != 0,
            PlayerSprite = (Options & FrameStateOptions.PlayerSprite) != 0
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not FrameState frameState)
            return false;

        return
            frameState.FrameIndex == FrameIndex &&
            frameState.CurrentTick == CurrentTick &&
            frameState.Options == Options;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(FrameState left, FrameState right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FrameState left, FrameState right)
    {
        return !(left == right);
    }
}

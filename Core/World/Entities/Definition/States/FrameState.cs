using Helion.Resources.Definitions.Decorate.States;
using Helion.Models;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;
using System;

namespace Helion.World.Entities.Definition.States
{
    /// <summary>
    /// A simple state wrapper that allows us to advance the state.
    /// </summary>
    public class FrameState : ITickable
    {
        private const int InfiniteLoopLimit = 10000;
        private const int StackLimit = 100;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public EntityFrame Frame => m_frameTable.Frames[m_frameIndex];
        private readonly Entity m_entity;
        private readonly EntityDefinition m_definition;
        private readonly EntityManager m_entityManager;
        private readonly EntityFrameTable m_frameTable;
        private readonly bool m_destroyOnStop;
        private int m_frameIndex;
        private int m_tics;
        private int m_stackCount;

        public int CurrentTick => m_tics;

        public FrameState(Entity entity, EntityDefinition definition,
            EntityManager entityManager, bool destroyOnStop = true)
        {
            m_entity = entity;
            m_definition = definition;
            m_frameTable = entityManager.World.ArchiveCollection.Definitions.EntityFrameTable;
            m_entityManager = entityManager;
            m_destroyOnStop = destroyOnStop;
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
        }

        public EntityFrame? GetStateFrame(string label)
        {
            if (m_definition.States.Labels.TryGetValue(label, out int index))
                return m_frameTable.Frames[index];

            return null;
        }

        public bool SetState(string label, int offset = 0, bool warn = true, bool executeActionFunction = true)
        {
            if (m_definition.States.Labels.TryGetValue(label, out int index))
            {
                if (index + offset >= 0 && index + offset < m_frameTable.Frames.Count)
                    SetFrameIndex(index + offset, executeActionFunction);
                else
                    SetFrameIndex(index, executeActionFunction);

                return true;
            }
            
            if (warn)
                Log.Warn("Unable to find state label {0} for actor {1}", label, m_definition.Name);

            return false;
        }

        public void SetState(EntityFrame entityFrame) =>
            SetFrameIndex(entityFrame.MasterFrameIndex, true);

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

        private void SetFrameIndex(int index, bool executeActionFunction)
        {
            m_frameIndex = index;
            m_tics = Frame.Ticks;

            if (m_entity.World.SkillDefinition.IsFastMonsters(m_entity.World.Config) && Frame.Properties.Fast)
                m_tics /= 2;

            if (m_entity.World.SkillDefinition.SlowMonsters && Frame.Properties.Slow)
                m_tics *= 2;

            EntityFrame frame = Frame;
            if (executeActionFunction)
            {
                m_stackCount++;
                if (m_stackCount > StackLimit)
                {
                    LogStackError();
                    return;
                }
                frame.ActionFunction?.Invoke(m_entity);
            }

            // Vanilla just forced the state, if executeActionFunction is false then don't check to remove the entity.
            if (executeActionFunction && m_destroyOnStop && frame.IsNullFrame)
                m_entityManager.Destroy(m_entity);
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

            m_stackCount = 0;
            if (m_tics == -1)
                return;

            int frameCounter = 0;
            while (frameCounter < InfiniteLoopLimit)
            {
                EntityFrame frame = Frame;
                m_tics--;
                if (m_tics <= 0)
                {
                    // If we don't have a Stop label at a -1 frame, then the
                    // entity should be removed from the map.
                    if (frame.BranchType == ActorStateBranch.Stop && frame.Ticks >= 0)
                    {
                        if (m_destroyOnStop)
                            m_entityManager.Destroy(m_entity);
                        return;
                    }

                    SetFrameIndex(frame.NextFrameIndex, true);
                }

                // We need to keep looping if this frame has no tick length
                // for consumption. To achieve this, we only break out if
                // the frame we just executed has at least 1 tick duration.
                if (frame.Ticks != 0)
                    break;

                frameCounter++;
            }

            if (frameCounter >= InfiniteLoopLimit)
            {
                Log.Warn("Infinite loop detected in actor {0}, removing actor", m_definition.Name);
                m_entityManager.Destroy(m_entity);
            }
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
    }
}
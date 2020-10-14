using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition.States
{
    /// <summary>
    /// A simple state wrapper that allows us to advance the state.
    /// </summary>
    public class FrameState : ITickable
    {
        private const int InfiniteLoopLimit = 10000;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public EntityFrame Frame => m_definition.States.Frames[m_frameIndex];
        private readonly Entity m_entity;
        private readonly EntityDefinition m_definition;
        private readonly EntityManager m_entityManager;
        private int m_frameIndex;
        private int m_tics;

        public FrameState(Entity entity, EntityDefinition definition, EntityManager entityManager)
        {
            m_entity = entity;
            m_definition = definition;
            m_entityManager = entityManager;
        }

        public bool SetState(FrameStateLabel label) => SetState(label.ToString());

        public bool SetState(string label)
        {
            if (m_definition.States.Labels.TryGetValue(label, out int index))
            {
                SetFrameIndex(index);
                return true;
            }

            Log.Warn("Unable to find state label '{0}' for actor {1}", label, m_definition.Name);
            return false;
        }

        public void SetTics(int tics)
        {
            if (tics < 1)
                tics = 1;
            m_tics = tics;
        }

        private void SetFrameIndex(int index)
        {
            m_frameIndex = index;
            m_tics = Frame.Ticks;
            Frame.ActionFunction?.Invoke(m_entity);
        }

        public void Tick()
        {
            Precondition(m_frameIndex >= 0 && m_frameIndex < m_definition.States.Frames.Count, "Out of range frame index for entity");

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
                        m_entityManager.Destroy(m_entity);
                        return;
                    }

                    SetFrameIndex(frame.NextFrameIndex);
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
    }
}
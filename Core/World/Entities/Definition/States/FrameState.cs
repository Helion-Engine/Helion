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
        private int m_ticksInFrame;
        private bool AtFrameBeginning => m_ticksInFrame == 0;

        public FrameState(Entity entity, EntityDefinition definition, EntityManager entityManager)
        {
            m_entity = entity;
            m_definition = definition;
            m_entityManager = entityManager;

            FindInitialFrameIndex();
        }

        public bool SetState(FrameStateLabel label) => SetState(label.ToString());

        public bool SetState(string label)
        {
            if (m_definition.States.Labels.TryGetValue(label, out int index))
            {
                m_frameIndex = index;
                m_ticksInFrame = 0;
                return true;
            }

            Log.Warn("Unable to find state label '{0}' for actor {1}", label, m_definition.Name);
            return false;
        }

        public void Tick()
        {
            Precondition(m_frameIndex >= 0 && m_frameIndex < m_definition.States.Frames.Count, "Out of range frame index for entity");

            int frameCounter = 0;
            while (frameCounter < InfiniteLoopLimit)
            {
                EntityFrame frame = Frame;

                if (AtFrameBeginning)
                {
                    frame.ActionFunction?.Invoke(m_entity);

                    // It may be the case that the actor invokes some action
                    // function that removes itself from existence. In such a
                    // case, we want to abort and not potentially try running
                    // more states when it's cleaned up after itself already.
                    if (m_entity.IsDisposed)
                        return;
                }

                m_ticksInFrame++;
                if (m_ticksInFrame > frame.Ticks)
                {
                    // If we don't have a Stop label at a -1 frame, then the
                    // entity should be removed from the map.
                    if (frame.BranchType == ActorStateBranch.Stop && frame.Ticks >= 0)
                    {
                        m_entityManager.Destroy(m_entity);
                        return;
                    }

                    m_frameIndex = frame.NextFrameIndex;
                    m_ticksInFrame = 0;
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

        private void FindInitialFrameIndex()
        {
            // Every actor must have at least one frame, so if we can't find
            // the spawn frame somehow, we'll assume we start at index zero.
            if (!SetState(FrameStateLabel.Spawn))
            {
                m_frameIndex = 0;
                m_ticksInFrame = 0;
            }
        }
    }
}
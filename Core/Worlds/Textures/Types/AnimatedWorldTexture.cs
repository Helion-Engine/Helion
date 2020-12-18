using System.Collections.Generic;
using System.Linq;
using Helion.Resource.Textures;
using Helion.Util;

namespace Helion.Worlds.Textures.Types
{
    /// <summary>
    /// A texture in a world that is animated.
    /// </summary>
    public class AnimatedWorldTexture : IWorldTexture, ITickable
    {
        public CIString Name { get; }
        public Texture Texture { get; private set; }
        public bool IsMissing => false;
        public bool IsSky => false;
        private readonly List<AnimatedTextureFrame> m_frames;
        private int m_ticksLeftInFrame;
        private int m_frameIndex;

        public AnimatedWorldTexture(CIString name, List<AnimatedTextureFrame> frames)
        {
            Name = name;
            Texture = frames[0].Texture;
            m_frames = frames.ToList();
            m_ticksLeftInFrame = frames[0].DurationTicks;
        }

        public AnimatedWorldTexture(AnimatedWorldTexture parent)
        {
            Name = parent.Name;
            Texture = parent.Texture;
            m_frames = parent.m_frames.ToList();
            m_ticksLeftInFrame = parent.m_ticksLeftInFrame;
            m_frameIndex = parent.m_frameIndex;
        }

        public void Tick()
        {
            m_ticksLeftInFrame--;
            if (m_ticksLeftInFrame > 0)
                return;

            m_frameIndex = (m_frameIndex + 1) % m_frames.Count;

            AnimatedTextureFrame frame = m_frames[m_frameIndex];
            Texture = frame.Texture;
            m_ticksLeftInFrame = frame.DurationTicks;
        }

        public override string ToString() => $"{Name} [index: {m_frameIndex}, duration: {m_ticksLeftInFrame}]";
    }
}

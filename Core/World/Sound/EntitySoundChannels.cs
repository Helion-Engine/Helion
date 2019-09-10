using System;
using Helion.Audio;
using Helion.Util;
using Helion.World.Entities;
using MoreLinq;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Sound
{
    public class EntitySoundChannels : IDisposable, ITickable
    {
        public static readonly int MaxChannels = Enum.GetValues(typeof(SoundChannelType)).Length;

        private readonly IAudioSource?[] m_channels = new IAudioSource[MaxChannels];
        private readonly Entity m_owner;

        public EntitySoundChannels(Entity owner)
        {
            m_owner = owner;
        }

        public void DestroyChannelSound(SoundChannelType channel)
        {
            int channelIndex = (int)channel;
            Precondition(channelIndex < MaxChannels, "ZDoom extra channel flags unsupported currently");

            m_channels[channelIndex]?.Dispose();
            m_channels[channelIndex] = null;
        }

        public void Add(IAudioSource audioSource, SoundChannelType channel)
        {
            int channelIndex = (int)channel;
            Precondition(channelIndex < MaxChannels, "ZDoom extra channel flags unsupported currently");

            DestroyChannelSound(channel);
            m_channels[channelIndex] = audioSource;
        }

        public void Tick()
        {
            for (int i = 0; i < MaxChannels; i++)
            {
                IAudioSource? channelSound = m_channels[i];
                if (channelSound == null)
                    continue;
                
                channelSound.Position = m_owner.Position.ToFloat();
                channelSound.Velocity = m_owner.Position.ToFloat();
            }
        }

        public void Dispose()
        {
            m_channels.ForEach(snd => snd?.Dispose());
        }
    }
}
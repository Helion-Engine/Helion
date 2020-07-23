using Helion.Util.Geometry.Boxes;
using Helion.World.Geometry.Sectors;
using System;

namespace Helion.Audio
{
    // TODO these values are all test values that sound ok for now
    public class SoundParams
    {
        public static readonly float MaxVolume = 1.0f;
        public static readonly float DefaultMaxDistance = 2048.0f;

        public readonly Attenuation Attenuation;
        public readonly float Volume;
        public readonly float RolloffFactor;
        public readonly float ReferenceDistance;
        public readonly float MaxDistance;
        public readonly bool Loop;

        public SoundParams(Attenuation attenuation, float volume, float rolloff, float reference, float maxDistance, bool loop)
        {
            Attenuation = attenuation;
            Volume = volume;
            RolloffFactor = rolloff;
            ReferenceDistance = reference;
            MaxDistance = maxDistance;
            Loop = loop;
        }

        public static SoundParams Create(Sector sector, bool loop = false)
        {
            return new SoundParams(Attenuation.Default, 1.0f, 1.75f, 128.0f, DefaultMaxDistance, loop);
        }

        public static SoundParams Create(Attenuation attenuation, float reference = 128.0f, float volume = 1.0f, bool loop = false)
        {
            switch (attenuation)
            {
                case Attenuation.None:
                    return new SoundParams(attenuation, volume, 0.0f, 0.0f, 0.0f, loop);
                case Attenuation.Rapid:
                    return new SoundParams(attenuation, volume, 2.5f, reference, DefaultMaxDistance, loop);
                case Attenuation.Default:
                default:
                    break;
            }

            return new SoundParams(attenuation, volume, 1.7f, reference, reference + DefaultMaxDistance, loop);
        }
    }
}

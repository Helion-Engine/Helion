using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.Audio
{
    // TODO these values are all test values that sound ok for now
    public class SoundParams
    {
        public const float MaxVolume = 1.0f;
        public const float DefaultRolloff = 2.5f;
        public const float DefaultReference = 296.0f;
        public const float DefaultMaxDistance = 1752.0f;
        public const float DefaultRadius = 32.0f;

        public readonly Attenuation Attenuation;
        public readonly float Volume;
        public readonly float Radius;
        public readonly float RolloffFactor;
        public readonly float ReferenceDistance;
        public readonly float MaxDistance;
        public readonly bool Loop;
        public float Pitch = 1.0f;

        public SoundParams(Attenuation attenuation, float volume, float radius, float rolloff, float reference, float maxDistance, bool loop)
        {
            Attenuation = attenuation;
            Volume = volume;
            Radius = radius;
            RolloffFactor = rolloff;
            ReferenceDistance = reference;
            MaxDistance = maxDistance;
            Loop = loop;
        }

        public static SoundParams Create(Entity entity, bool loop = false)
        {
            return new SoundParams(Attenuation.Default, MaxVolume, (float)entity.Radius + 16, DefaultRolloff, DefaultReference, DefaultMaxDistance, loop);
        }

        public static SoundParams Create(Sector sector, bool loop = false)
        {
            return new SoundParams(Attenuation.Default, MaxVolume, 128.0f, DefaultRolloff, DefaultReference, DefaultMaxDistance, loop);
        }

        public static SoundParams Create(Attenuation attenuation, float volume = MaxVolume, float radius = DefaultRadius, float reference = DefaultReference, bool loop = false)
        {
            switch (attenuation)
            {
                case Attenuation.None:
                    return new SoundParams(attenuation, volume, 0.0f, 0.0f, 0.0f, 0.0f, loop);
                case Attenuation.Rapid:
                    return new SoundParams(attenuation, volume, radius, 4.0f, reference, DefaultMaxDistance, loop);
                case Attenuation.Default:
                default:
                    break;
            }

            return new SoundParams(attenuation, volume, radius, DefaultRolloff, reference, DefaultMaxDistance, loop);
        }
    }
}

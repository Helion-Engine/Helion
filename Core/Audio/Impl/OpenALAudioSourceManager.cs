using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Impl.Components;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using MoreLinq.Extensions;
using NLog;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl
{
    public class OpenALAudioSourceManager : IAudioSourceManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ArchiveCollection m_archiveCollection;
        private readonly OpenALAudioSystem m_owner;
        private readonly HashSet<OpenALAudioSource> m_sources = new();
        private readonly Dictionary<string, OpenALBuffer> m_nameToBuffer = new();
        private readonly DynamicArray<int> m_playGroup = new();

        public OpenALAudioSourceManager(OpenALAudioSystem owner, ArchiveCollection archiveCollection)
        {
            m_owner = owner;
            m_archiveCollection = archiveCollection;
            OpenALExecutor.Run("Setting distance model", () =>
            {
                AL.DistanceModel(ALDistanceModel.ExponentDistance);
            });

            owner.DeviceChanging += Owner_DeviceChanging;
        }

        private void Owner_DeviceChanging(object? sender, EventArgs e)
        {
            foreach (var source in m_sources.ToList())
            {
                Unlink(source);
                source.Stop();
                source.Dispose();
            }

            foreach (var buffer in m_nameToBuffer.Values)
                buffer.Dispose();

            m_nameToBuffer.Clear();
        }

        public void SetListener(Vec3D pos, double angle, double pitch)
        {
            Vec3D vec = Vec3D.Unit(angle, pitch);
            Vector3 up = new Vector3(0, 0, 1);
            Vector3 at = new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);

            OpenALExecutor.Run("Setting source manager position and orientation", () =>
            {
                AL.Listener(ALListenerfv.Orientation, ref at, ref up);
                AL.Listener(ALListener3f.Position, (float)pos.X, (float)pos.Y, (float)pos.Z);
            });
        }

        public void SetListenerVelocity(System.Numerics.Vector3 velocity)
        {
            OpenALExecutor.Run("Setting listener velocity", () =>
            {
                AL.Listener(ALListener3f.Velocity, velocity.X, velocity.Y, velocity.Z);
            });
        }

        ~OpenALAudioSourceManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public IAudioSource? Create(string sound, AudioData audioData, SoundParams soundParams)
        {
            OpenALBuffer? buffer = GetBuffer(sound);
            if (buffer == null)
                return null;

            OpenALAudioSource source = new(this, buffer, audioData, soundParams);
            m_sources.Add(source);
            return source;
        }

        public void PlayGroup(IEnumerable<IAudioSource> audioSources)
        {
            foreach (IAudioSource audioSource in audioSources)
               m_playGroup.Add(((OpenALAudioSource)audioSource).ID);

            OpenALExecutor.Run("Playing audio group", () =>
            {
                AL.SourcePlay(m_playGroup.Length, m_playGroup.Data);
            });

            m_playGroup.Clear();
        }

        private OpenALBuffer? GetBuffer(string sound)
        {
            string upperSound = sound.ToUpper();
            if (m_nameToBuffer.TryGetValue(upperSound, out OpenALBuffer? existingBuffer))
                return existingBuffer;

            Entry? entry = m_archiveCollection.Entries.FindByNamespace(upperSound, ResourceNamespace.Sounds);
            if (entry == null)
            {
                Log.Warn("Cannot find sound: {0}", sound);
                return null;
            }

            OpenALBuffer? buffer = OpenALBuffer.Create(entry.ReadData());
            if (buffer == null)
            {
                Log.Warn("Sound {0} is either corrupt or not a DMX sound", sound);
                return null;
            }

            m_nameToBuffer[upperSound] = buffer;
            return buffer;
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        internal void Unlink(OpenALAudioSource source)
        {
            m_sources.Remove(source);
        }

        private void PerformDispose()
        {
            m_owner.Unlink(this);

            // We create a copy list because disposing will mutate the list
            // that it belongs to, since it has no idea if we're disposing it
            // manually or by disposal of its manager.
            m_sources.ToList().ForEach(src => src.Dispose());
            Invariant(m_sources.Empty(), "Disposal of AL audio source children should empty out of the context container");

            m_nameToBuffer.ForEach(pair => pair.Value.Dispose());
        }
    }
}

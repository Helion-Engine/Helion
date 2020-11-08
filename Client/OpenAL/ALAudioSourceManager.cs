using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio;
using Helion.Client.OpenAL.Components;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Vectors;
using MoreLinq;
using NLog;
using OpenTK.Audio.OpenAL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.OpenAL
{
    public class ALAudioSourceManager : IAudioSourceManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ArchiveCollection m_archiveCollection;
        private readonly ALAudioSystem m_owner;
        private readonly HashSet<ALAudioSource> m_sources = new HashSet<ALAudioSource>();
        private readonly Dictionary<string, ALBuffer> m_nameToBuffer = new Dictionary<string, ALBuffer>();

        public ALAudioSourceManager(ALAudioSystem owner, ArchiveCollection archiveCollection)
        {
            m_owner = owner;
            m_archiveCollection = archiveCollection;
            AL.DistanceModel(ALDistanceModel.ExponentDistance);

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
            OpenTK.Vector3 up = new OpenTK.Vector3(0, 0, 1);
            OpenTK.Vector3 at = new OpenTK.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);

            AL.Listener(ALListenerfv.Orientation, ref at, ref up);
            AL.Listener(ALListener3f.Position, (float)pos.X, (float)pos.Y, (float)pos.Z);
        }

        public void SetListenerVelocity(System.Numerics.Vector3 velocity)
        {
            AL.Listener(ALListener3f.Velocity, velocity.X, velocity.Y, velocity.Z);
        }

        ~ALAudioSourceManager()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public IAudioSource? Create(string sound, SoundParams soundParams)
        {
            ALBuffer? buffer = GetBuffer(sound);
            if (buffer == null)
                return null;
            
            ALAudioSource source = new ALAudioSource(this, buffer, soundParams);
            m_sources.Add(source);
            return source;
        }

        public void PlayGroup(List<IAudioSource> audioSources)
        {
            int[] ids = new int[audioSources.Count];
            for (int i = 0; i < audioSources.Count; i++)
                ids[i] = ((ALAudioSource)audioSources[i]).ID;

            AL.SourcePlay(ids.Length, ids);
        }

        private ALBuffer? GetBuffer(string sound)
        {
            string upperSound = sound.ToUpper();
            if (m_nameToBuffer.TryGetValue(upperSound, out ALBuffer? existingBuffer))
                return existingBuffer;

            Entry? entry = m_archiveCollection.Entries.FindByNamespace(upperSound, ResourceNamespace.Sounds);
            if (entry == null)
            {
                Log.Warn("Cannot find sound: {0}", sound);
                return null;
            }
            
            ALBuffer? buffer = ALBuffer.Create(entry.ReadData());
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

        internal void Unlink(ALAudioSource source)
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
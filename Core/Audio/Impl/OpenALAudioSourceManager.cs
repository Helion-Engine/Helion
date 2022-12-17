using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Audio.Impl.Components;
using Helion.Geometry.Vectors;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.Util.Extensions;
using MoreLinq.Extensions;
using NLog;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Audio.Impl;

public class OpenALAudioSourceManager : IAudioSourceManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int MaxSounds = 256;

    private readonly ArchiveCollection m_archiveCollection;
    private readonly OpenALAudioSystem m_owner;
    private readonly HashSet<OpenALAudioSource> m_sources = new();
    private readonly Dictionary<string, OpenALBuffer> m_nameToBuffer = new(StringComparer.OrdinalIgnoreCase);
    private readonly DynamicArray<int> m_playGroup = new();
    private readonly IConfig m_config;

    public OpenALAudioSourceManager(OpenALAudioSystem owner, ArchiveCollection archiveCollection, IConfig config)
    {
        m_owner = owner;
        m_archiveCollection = archiveCollection;
        m_config = config;
        OpenALDebug.Start("Setting distance model");
        AL.DistanceModel(ALDistanceModel.ExponentDistance);
        OpenALDebug.End("Setting distance model");
    }

    public void DeviceChanging()
    {
        foreach (var buffer in m_nameToBuffer.Values)
            buffer.Dispose();

        m_nameToBuffer.Clear();
    }

    public void SetListener(Vec3D pos, double angle, double pitch)
    {
        Vec3D vec = Vec3D.UnitSphere(angle, pitch);
        Vector3 up = new Vector3(0, 0, 1);
        Vector3 at = new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);

        OpenALDebug.Start("Setting source manager position and orientation");
        AL.Listener(ALListenerfv.Orientation, ref at, ref up);
        AL.Listener(ALListener3f.Position, (float)pos.X, (float)pos.Y, (float)pos.Z);
        OpenALDebug.End("Setting source manager position and orientation");
    }

    public void SetListenerVelocity(System.Numerics.Vector3 velocity)
    {
        OpenALDebug.Start("Setting listener velocity");
        AL.Listener(ALListener3f.Velocity, velocity.X, velocity.Y, velocity.Z);
        OpenALDebug.End("Setting listener velocity");
    }

    ~OpenALAudioSourceManager()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void CacheSound(string sound)
    {
        GetBuffer(sound);
    }

    public IAudioSource? Create(string sound, in AudioData audioData)
    {
        if (m_sources.Count >= MaxSounds)
            return null;

        OpenALBuffer? buffer = GetBuffer(sound);
        if (buffer == null)
            return null;

        OpenALAudioSource source = m_archiveCollection.DataCache.GetAudioSource(this, buffer, audioData);
        m_sources.Add(source);
        return source;
    }

    public void PlayGroup(IEnumerable<IAudioSource> audioSources)
    {
        foreach (IAudioSource audioSource in audioSources)
           m_playGroup.Add(((OpenALAudioSource)audioSource).ID);

        OpenALDebug.Start("Playing audio group");
        AL.SourcePlay(m_playGroup.Length, m_playGroup.Data);
        OpenALDebug.End("Playing audio group");

        m_playGroup.Clear();
    }

    private OpenALBuffer? GetBuffer(string sound)
    {
        if (m_nameToBuffer.TryGetValue(sound, out OpenALBuffer? existingBuffer))
            return existingBuffer;

        Entry? entry = m_archiveCollection.Entries.FindByNamespace(sound, ResourceNamespace.Sounds);
        if (entry == null)
        {
            Log.Warn("Cannot find sound: {0}", sound);
            return null;
        }

        OpenALBuffer? buffer = OpenALBuffer.Create(entry.ReadData(), out string? error);
        if (buffer == null)
        {
            if (error != null && m_config.Audio.LogErrors)
                Log.Warn($"Error playing sound {sound}: {error}");
            return null;
        }

        m_nameToBuffer[sound] = buffer;
        return buffer;
    }

    public void Tick()
    {

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

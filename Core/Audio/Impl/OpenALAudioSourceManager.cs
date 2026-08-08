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
    private readonly List<OpenALAudioSource> m_sources = [];
    private readonly Dictionary<string, OpenALBuffer> m_nameToBuffer = new(StringComparer.OrdinalIgnoreCase);
    private readonly DynamicArray<int> m_playGroup = new();
    private readonly IConfig m_config;

    private OpenALSoftResamplerType? m_currentResampler;

    public readonly OpenALAudioSystem AudioSystem;

    public OpenALAudioSourceManager(OpenALAudioSystem owner, ArchiveCollection archiveCollection, IConfig config)
    {
        AudioSystem = owner;
        m_archiveCollection = archiveCollection;
        m_config = config;
        m_config.Audio.Resampler.OnChanged += Resampler_OnChanged;
        SetAllResamplers(m_config.Audio.Resampler);
        OpenALDebug.Start("Setting distance model");
        AL.DistanceModel(ALDistanceModel.ExponentDistance);
        OpenALDebug.End("Setting distance model");
    }

    public void Clear()
    {

    }

    public void DeviceChanging()
    {
        foreach (var buffer in m_nameToBuffer.Values)
            buffer.Dispose();

        m_nameToBuffer.Clear();
    }

    public void SetGain(float gain)
    {
        foreach(var source in m_sources)
        {
            source?.SetGain(gain);
        }
    }

    public void SetListener(Vec3D pos, double angle, double pitch)
    {
        var vec = Vec2D.UnitCircle(angle);
        var up = new Vector3(0, 0, 1);
        var at = new Vector3((float)vec.X, (float)vec.Y, 0);

        OpenALDebug.Start("Setting source manager position and orientation");
        AL.Listener(ALListenerfv.Orientation, ref at, ref up);
        AL.Listener(ALListener3f.Position, (float)pos.X, (float)pos.Y, (float)pos.Z);
        OpenALDebug.End("Setting source manager position and orientation");
    }

    public static void SetListenerVelocity(Vector3 velocity)
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
        GetBuffer(sound, logErrors: false);
    }

    public IAudioSource? Create(string sound, in AudioData audioData)
    {
        if (m_sources.Count >= MaxSounds)
            return null;

        var buffer = GetBuffer(sound);
        if (buffer == null)
            return null;

        var source = m_archiveCollection.DataCache.GetAudioSource(this, buffer, audioData);
        if (m_currentResampler != null)
            source.SetResampler(m_currentResampler);
        source.SetGain((float)AudioSystem.Gain);
        m_sources.Add(source);
        return source;
    }

    private void Resampler_OnChanged(object? sender, string e) => SetAllResamplers(e);
    private void SetAllResamplers(string name)
    {
        m_currentResampler = OpenALSoftResamplerType.FromName(name);
        if (m_currentResampler != null)
        {
            foreach (var source in m_sources)
            {
                source.SetResampler(m_currentResampler);
            }
        }
    }

    public void PlayGroup(SoundList audioSources)
    {
        var node = audioSources.Head;
        while (node != null)
        {
            m_playGroup.Add(((OpenALAudioSource)node).ID);
            node = node.Next;
        }

        OpenALDebug.Start("Playing audio group");
        AL.SourcePlay(m_playGroup.Length, m_playGroup.Data);
        OpenALDebug.End("Playing audio group");

        m_playGroup.Clear();
    }

    private OpenALBuffer? GetBuffer(string sound, bool logErrors = true)
    {
        if (m_nameToBuffer.TryGetValue(sound, out OpenALBuffer? existingBuffer))
            return existingBuffer;

        var entry = m_archiveCollection.Entries.FindByNamespace(sound, ResourceNamespace.Sounds);
        if (entry == null)
        {
            if (logErrors && m_config.Audio.LogErrors)
                Log.Warn("Cannot find sound: {0}", sound);
            return null;
        }

        var buffer = OpenALBuffer.Create(entry.ReadData(), out string? error);
        if (buffer == null)
        {
            if (error != null && logErrors && m_config.Audio.LogErrors)
                Log.Warn($"Error playing sound {sound}: {error}");
            return null;
        }

        m_nameToBuffer[sound] = buffer;
        return buffer;
    }

    public void Tick()
    {
        AudioSystem.Tick();
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
        AudioSystem.Unlink(this);

        // We create a copy list because disposing will mutate the list
        // that it belongs to, since it has no idea if we're disposing it
        // manually or by disposal of its manager.
        m_sources.ForEach(src => src.Dispose());
        Invariant(m_sources.Empty(), "Disposal of AL audio source children should empty out of the context container");

        foreach (var item in m_nameToBuffer)
            item.Value.Dispose();
    }
}

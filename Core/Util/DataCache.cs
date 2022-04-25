using Helion.Audio;
using Helion.Audio.Impl;
using Helion.Audio.Impl.Components;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using System.Collections.Generic;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Renderers.Legacy.World.Data;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Definition;
using Helion.Models;
using Helion.Geometry.Vectors;
using NLog;
using Helion.Util.Assertion;

namespace Helion.Util;

public class DataCache
{
    private const int DefaultLength = 1024;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new(DefaultLength);
    private readonly DynamicArray<List<LinkableNode<Entity>>> m_entityListNodes = new(DefaultLength);
    private readonly DynamicArray<List<Sector>> m_sectorLists = new(DefaultLength);
    private readonly DynamicArray<FrameState> m_frameStates = new(DefaultLength);
    private readonly DynamicArray<EntityBox> m_entityBoxes = new(DefaultLength);
    private readonly DynamicArray<IAudioSource?[]> m_entityAudioSources = new(DefaultLength);
    private readonly DynamicArray<List<BlockmapIntersect>> m_blockmapLists = new();
    private readonly DynamicArray<HashSet<Sector>> m_sectorSet = new();
    private readonly Dictionary<GLLegacyTexture, DynamicArray<RenderWorldData>> m_alphaRender = new();
    private readonly DynamicArray<AudioData> m_audioData = new();
    private readonly DynamicArray<SoundParams> m_soundParams = new();
    private readonly DynamicArray<IAudioSource> m_audioSources = new();
    private readonly DynamicArray<WeakEntity> m_weakEntities = new();
    private readonly DynamicArray<LinkedList<WeakEntity>> m_weakEntitiesList = new();

    // Unit Test accessors
    public int WeakEntitiesCount => m_weakEntities.Length;
    public int WeakEntitiesListCount => m_weakEntitiesList.Length;

    public LinkableNode<Entity> GetLinkableNodeEntity(Entity entity)
    {
        LinkableNode<Entity> node;
        if (m_entityNodes.Length > 0)
        {
            node = m_entityNodes.RemoveLast();
            node.Value = entity;
            return node;
        }
        
        return new LinkableNode<Entity> { Value = entity };
    }

    public void FreeLinkableNodeEntity(LinkableNode<Entity> node)
    {
        node.Previous = null!;
        node.Next = null;
        node.Value = null!;
        m_entityNodes.Add(node);
    }

    public List<LinkableNode<Entity>> GetLinkableNodeEntityList()
    {
        if (m_entityListNodes.Length > 0)
            return m_entityListNodes.RemoveLast();

        return new List<LinkableNode<Entity>>();
    }

    public void FreeLinkableNodeEntityList(List<LinkableNode<Entity>> list)
    {
        list.Clear();
        m_entityListNodes.Add(list);
    }

    public List<Sector> GetSectorList()
    {
        if (m_sectorLists.Length > 0)
            return m_sectorLists.RemoveLast();

        return new List<Sector>();
    }

    public void FreeSectorList(List<Sector> list)
    {
        list.Clear();
        m_sectorLists.Add(list);
    }

    public FrameState GetFrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, bool destroyOnStop = true)
    {
        if (m_frameStates.Length > 0)
        {
            FrameState frameState = m_frameStates.RemoveLast();
            frameState.Set(entity, definition, entityManager, destroyOnStop);
            return frameState;
        }

        return new FrameState(entity, definition, entityManager, destroyOnStop);
    }

    public FrameState GetFrameState(Entity entity, EntityDefinition definition,
        EntityManager entityManager, FrameStateModel frameStateModel)
    {
        if (m_frameStates.Length > 0)
        {
            FrameState frameState = m_frameStates.RemoveLast();
            frameState.Set(entity, definition, entityManager, frameStateModel);
            return frameState;
        }

        return new FrameState(entity, definition, entityManager, frameStateModel);
    }

    public void FreeFrameState(FrameState frameState)
    {
        frameState.Clear();
        m_frameStates.Add(frameState);
    }

    public EntityBox GetEntityBox(Vec3D centerBottom, double radius, double height)
    {
        if (m_entityBoxes.Length > 0)
        {
            EntityBox box = m_entityBoxes.RemoveLast();
            box.Set(centerBottom, radius, height);
            return box;
        }

        return new EntityBox(centerBottom, radius, height);
    }

    public void FreeEntityBox(EntityBox box)
    {
        m_entityBoxes.Add(box);
    }

    public IAudioSource?[] GetEntityAudioSources()
    {
        if (m_entityAudioSources.Length > 0)
            return m_entityAudioSources.RemoveLast();

        return new IAudioSource[Entity.MaxSoundChannels];
    }

    public void FreeEntityAudioSources(IAudioSource?[] sources)
    {
        for (int i = 0; i < sources.Length; i++)
            sources[i] = null;
        m_entityAudioSources.Add(sources);
    }

    public List<BlockmapIntersect> GetBlockmapIntersectList()
    {
        if (m_blockmapLists.Length > 0)
            return m_blockmapLists.RemoveLast();

        return new List<BlockmapIntersect>();
    }

    public void FreeBlockmapIntersectList(List<BlockmapIntersect> list)
    {
        list.Clear();
        m_blockmapLists.Add(list);
    }

    public RenderWorldData GetAlphaRenderWorldData(IGLFunctions functions, GLCapabilities capabilities, GLLegacyTexture texture)
    {
        if (m_alphaRender.TryGetValue(texture, out var data))
        {
            if (data.Length > 0)
                return data.RemoveLast();

            return new RenderWorldData(capabilities, functions, texture);
        }
        else
        {
            RenderWorldData renderWorldData = new(capabilities, functions, texture);
            m_alphaRender.Add(texture, new DynamicArray<RenderWorldData>());
            return renderWorldData;
        }
    }

    public void FreeAlphaRenderWorldData(RenderWorldData renderWorldData)
    {
        renderWorldData.Clear();
        m_alphaRender[renderWorldData.Texture].Add(renderWorldData);
    }

    public HashSet<Sector> GetSectorSet()
    {
        if (m_sectorSet.Length > 0)
            return m_sectorSet.RemoveLast();

        return new HashSet<Sector>();
    }

    public void FreeSectorSet(HashSet<Sector> set)
    {
        set.Clear();
        m_sectorSet.Add(set);
    }

    public AudioData GetAudioData(ISoundSource soundSource, SoundInfo soundInfo, SoundChannelType channel, Attenuation attenuation,
        int priority, bool loop)
    {
        if (m_audioData.Length > 0)
        {
            AudioData audioData = m_audioData.RemoveLast();
            audioData.SoundSource = soundSource;
            audioData.SoundInfo = soundInfo;
            audioData.SoundChannelType = channel;
            audioData.Attenuation = attenuation;
            audioData.Priority = priority;
            audioData.Loop = loop;
            return audioData;
        }

        return new AudioData(soundSource, soundInfo, channel, attenuation, priority, loop);
    }

    public void FreeAudioData(AudioData audioData)
    {
        audioData.SoundSource = null!;
        audioData.SoundInfo = null!;
        m_audioData.Add(audioData);
    }

    public SoundParams GetSoundParams(ISoundSource soundSource, bool loop = false, Attenuation attenuation = Attenuation.Default,
        float volume = SoundParams.MaxVolume, SoundType type = SoundType.Default)
    {
        if (m_soundParams.Length > 0)
        {
            SoundParams soundParams = m_soundParams.RemoveLast();
            soundParams.SoundSource = soundSource;
            soundParams.Loop = loop;
            soundParams.Attenuation = attenuation;
            soundParams.Volume = volume;
            soundParams.SoundType = type;            
            return soundParams;
        }

        return new SoundParams(soundSource, loop, attenuation, volume);
    }

    public void FreeSoundParams(SoundParams soundParams)
    {
        soundParams.SoundSource = null!;
        soundParams.SoundInfo = null;
        m_soundParams.Add(soundParams);
    }

    public OpenALAudioSource GetAudioSource(OpenALAudioSourceManager owner, OpenALBuffer buffer, AudioData audioData, SoundParams soundParams)
    {
        if (m_audioSources.Length > 0)
        {
            OpenALAudioSource audioSource = (OpenALAudioSource)m_audioSources.RemoveLast();
            audioSource.Set(owner, buffer, audioData, soundParams);            
            return audioSource;
        }

        return new OpenALAudioSource(owner, buffer, audioData, soundParams, this);
    }

    public void FreeAudioSource(IAudioSource audioSource)
    {
        audioSource.AudioData.SoundSource.ClearSound(audioSource, audioSource.AudioData.SoundChannelType);
        audioSource.CacheFree();
        audioSource.AudioData = null!;
        m_audioSources.Add(audioSource);
    }

    public WeakEntity GetWeakEntity()
    {
        if (m_weakEntities.Length > 0)
            return m_weakEntities.RemoveLast();

        return new WeakEntity();
    }

    public void FreeWeakEntity(WeakEntity weakEntity)
    {
        Assert.Precondition(!ReferenceEquals(weakEntity, WeakEntity.Default), "Tried to free static default instance.");

        weakEntity.Entity = null;
        m_weakEntities.Add(weakEntity);
    }

    public LinkedList<WeakEntity> GetWeakEntityList()
    {
        if (m_weakEntitiesList.Length > 0)
            return m_weakEntitiesList.RemoveLast();

        return new LinkedList<WeakEntity>();
    }

    public void FreeWeakEntityList(LinkedList<WeakEntity> weakEntities)
    {
        weakEntities.Clear();
        m_weakEntitiesList.Add(weakEntities);
    }

    // Unit Test functions
    public void ClearWeakEntities() => m_weakEntities.Clear();
    public void ClearWeakEntityLists() => m_weakEntitiesList.Clear();
}

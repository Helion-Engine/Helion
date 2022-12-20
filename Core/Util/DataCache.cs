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
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Definition;
using Helion.Models;
using Helion.Geometry.Vectors;
using NLog;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Graphics.Fonts;
using Helion.Render.OpenGL.Renderers.Legacy.Hud;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.Common.Enums;
using Helion.Render.OpenGL.Shader;
using Helion.World.Special.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.World.Physics;
using Helion.World;

namespace Helion.Util;

public class DataCache
{
    private const int DefaultLength = 1024;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new(DefaultLength);
    private readonly DynamicArray<LinkableNode<Sector>> m_sectorNodes = new(DefaultLength);
    private readonly DynamicArray<List<LinkableNode<Entity>>> m_entityListNodes = new(DefaultLength);
    private readonly DynamicArray<List<Sector>> m_sectorLists = new(DefaultLength);
    private readonly DynamicArray<FrameState> m_frameStates = new(DefaultLength);
    private readonly DynamicArray<IAudioSource?[]> m_entityAudioSources = new(DefaultLength);
    private readonly DynamicArray<List<BlockmapIntersect>> m_blockmapLists = new();
    private readonly DynamicArray<IAudioSource> m_audioSources = new();
    private readonly DynamicArray<List<Entity>> m_entityLists = new();
    private readonly DynamicArray<List<RenderableGlyph>> m_glyphs = new();
    private readonly DynamicArray<List<RenderableSentence>> m_sentences = new();
    private readonly DynamicArray<RenderableString> m_strings = new();
    private readonly DynamicArray<HudDrawBufferData> m_hudDrawBufferData = new();
    private readonly DynamicArray<LinkedListNode<ClipSpan>> m_clipSpans = new();
    public WeakEntity?[] WeakEntities = new WeakEntity?[DefaultLength];

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

    public LinkableNode<Sector> GetLinkableNodeSector(Sector sector)
    {
        LinkableNode<Sector> node;
        if (m_sectorNodes.Length > 0)
        {
            node = m_sectorNodes.RemoveLast();
            node.Value = sector;
            return node;
        }

        return new LinkableNode<Sector> { Value = sector };
    }

    public void FreeLinkableNodeSector(LinkableNode<Sector> node)
    {
        node.Previous = null!;
        node.Next = null;
        node.Value = null!;
        m_sectorNodes.Add(node);
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

    public OpenALAudioSource GetAudioSource(OpenALAudioSourceManager owner, OpenALBuffer buffer, in AudioData audioData)
    {
        if (m_audioSources.Length > 0)
        {
            OpenALAudioSource audioSource = (OpenALAudioSource)m_audioSources.RemoveLast();
            audioSource.Set(owner, buffer, audioData);            
            return audioSource;
        }

        return new OpenALAudioSource(owner, buffer, audioData);
    }

    public void FreeAudioSource(IAudioSource audioSource)
    {
        if (audioSource is not OpenALAudioSource)
            return;

        audioSource.AudioData.SoundSource.ClearSound(audioSource, audioSource.AudioData.SoundChannelType);
        audioSource.CacheFree();
        m_audioSources.Add(audioSource);
    }

    public List<Entity> GetEntityList()
    {
        if (m_entityLists.Length > 0)
            return m_entityLists.RemoveLast();

        return new List<Entity>();
    }

    public void FreeEntityList(List<Entity> list)
    {
        list.Clear();
        m_entityLists.Add(list);
    }

    public List<RenderableSentence> GetRenderableSentences()
    {
        if (m_sentences.Length > 0)
            return m_sentences.RemoveLast();

        return new List<RenderableSentence>();
    }

    private void FreeRenderableSentences(List<RenderableSentence> list)
    {
        list.Clear();
        m_sentences.Add(list);
    }

    public List<RenderableGlyph> GetRenderableGlyphs()
    {
        if (m_glyphs.Length > 0)
            return m_glyphs.RemoveLast();

        return new List<RenderableGlyph>();
    }

    private void FreeRenderableGlyphs(List<RenderableGlyph> list)
    {
        list.Clear();
        m_glyphs.Add(list);
    }

    public RenderableString GetRenderableString(string str, Font font, int fontSize, TextAlign align = TextAlign.Left,
        int maxWidth = int.MaxValue)
    {
        if (m_strings.Length > 0)
        {
            var renderableString = m_strings.RemoveLast();
            renderableString.Set(this, str, font, fontSize, align, maxWidth);
            return renderableString;
        }

        return new RenderableString(this, str, font, fontSize, align, maxWidth);
    }

    public void FreeRenderableString(RenderableString renderableString)
    {
        for (int i = 0; i < renderableString.Sentences.Count; i++)
            FreeRenderableGlyphs(renderableString.Sentences[i].Glyphs);
        FreeRenderableSentences(renderableString.Sentences);

        renderableString.Sentences = null!;
        renderableString.Font = null!;
        m_strings.Add(renderableString);
    }

    public HudDrawBufferData GetDrawHudBufferData(GLLegacyTexture texture)
    {
        if (m_hudDrawBufferData.Length > 0)
        {
            var buffer = m_hudDrawBufferData.RemoveLast();
            buffer.Set(texture);
            return buffer;
        }

        return new HudDrawBufferData(texture);
    }

    public void FreeDrawHudBufferData(HudDrawBufferData data)
    {
        data.Texture = null!;
        data.Vertices.Clear();
        m_hudDrawBufferData.Add(data);
    }

    public LinkedListNode<ClipSpan> GetClipSpan(ClipSpan clipSpan)
    {
        if (m_clipSpans.Length > 0)
        {
            var node = m_clipSpans.RemoveLast();
            node.Value = clipSpan;
            return node;
        }

        return new LinkedListNode<ClipSpan>(clipSpan);
    }

    public void FreeClipSpan(LinkedListNode<ClipSpan> clipSpan)
    {
        m_clipSpans.Add(clipSpan);
    }
}

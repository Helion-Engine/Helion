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

    private readonly DynamicArray<Entity> m_entities = new(DefaultLength);
    private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new(DefaultLength);
    private readonly DynamicArray<LinkableNode<Sector>> m_sectorNodes = new(DefaultLength);
    private readonly DynamicArray<DynamicArray<BlockmapIntersect>> m_blockmapLists = new();
    private readonly DynamicArray<IAudioSource> m_audioSources = new();
    private readonly DynamicArray<DynamicArray<Entity>> m_entityLists = new();
    private readonly DynamicArray<DynamicArray<RenderableGlyph>> m_glyphs = new();
    private readonly DynamicArray<List<RenderableSentence>> m_sentences = new();
    private readonly DynamicArray<RenderableString> m_strings = new();
    private readonly DynamicArray<HudDrawBufferData> m_hudDrawBufferData = new();
    private readonly DynamicArray<LinkedListNode<ClipSpan>> m_clipSpans = new();
    public WeakEntity?[] WeakEntities = new WeakEntity?[DefaultLength];

    public bool CacheEntities = true;

    // Clear pointers to references that could keep the world around and prevent garbage collection.
    public void FlushReferences()
    {
        for (int i = 0; i < m_entities.Capacity; i++)
        {
            Entity? entity = m_entities[i];
            if (entity == null)
                continue;
            entity.IntersectSectors.FlushReferences();
        }

        for (int i = 0; i < m_blockmapLists.Capacity; i++)
        {
            if (m_blockmapLists[i] == null)
                continue;
            for (int j = 0; j < m_blockmapLists[i].Capacity; j++)
            {
                m_blockmapLists[i].Data[j].Entity = null;
                m_blockmapLists[i].Data[j].Line = null;
            }
        }

        for (int i = 0; i < m_entityLists.Capacity; i++)
        {
            if (m_entityLists[i] == null)
                continue;
            m_entityLists[i].FlushReferences();
        }
    }

    public Entity GetEntity(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world)
    {
        if (m_entities.Length > 0)
        {
            var entity = m_entities.RemoveLast();
            entity.Set(id, thingId, definition, position, angleRadians, sector, world);
            return entity;
        }

        Entity newEnity = new();
        newEnity.Set(id, thingId, definition, position, angleRadians, sector, world);
        return newEnity;
    }

    public Entity GetEntity(EntityModel entityModel, EntityDefinition definition, IWorld world)
    {
        if (m_entities.Length > 0)
        {
            var entity = m_entities.RemoveLast();
            entity.Set(entityModel, definition, world);
            return entity;
        }

        Entity newEnity = new();
        newEnity.Set(entityModel, definition, world);
        return newEnity;
    }

    public bool FreeEntity(Entity entity)
    {
        if (!CacheEntities || entity.IsPlayer)
            return false;

        m_entities.Add(entity);
        return true;
    }

    public LinkableNode<Entity> GetLinkableNodeEntity(Entity entity)
    {
        if (m_entityNodes.Length > 0)
        {
            var node = m_entityNodes.RemoveLast();
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

    public DynamicArray<BlockmapIntersect> GetBlockmapIntersectList()
    {
        if (m_blockmapLists.Length > 0)
            return m_blockmapLists.RemoveLast();

        return new DynamicArray<BlockmapIntersect>();
    }

    public void FreeBlockmapIntersectList(DynamicArray<BlockmapIntersect> list)
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

    public DynamicArray<Entity> GetEntityList()
    {
        if (m_entityLists.Length > 0)
            return m_entityLists.RemoveLast();

        return new DynamicArray<Entity>();
    }

    public void FreeEntityList(DynamicArray<Entity> list)
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

    public DynamicArray<RenderableGlyph> GetRenderableGlyphs()
    {
        if (m_glyphs.Length > 0)
            return m_glyphs.RemoveLast();

        return new DynamicArray<RenderableGlyph>();
    }

    private void FreeRenderableGlyphs(DynamicArray<RenderableGlyph> list)
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

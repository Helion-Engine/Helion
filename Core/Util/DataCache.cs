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
using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Legacy.Texture.Legacy;

namespace Helion.Util
{
    public class DataCache
    {
        public static DataCache Instance { get; } = new DataCache();

        private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new DynamicArray<LinkableNode<Entity>>(1024);
        private readonly DynamicArray<List<BlockmapIntersect>> m_blockmapLists = new();
        private readonly DynamicArray<HashSet<Sector>> m_sectorSet = new();
        private readonly Dictionary<GLLegacyTexture, DynamicArray<RenderWorldData>> m_alphaRender = new();
        private readonly DynamicArray<AudioData> m_audioData = new();
        private readonly DynamicArray<SoundParams> m_soundParams = new();
        private readonly DynamicArray<IAudioSource> m_audioSources = new();

        public LinkableNode<Entity> GetLinkableNodeEntity(Entity entity)
        {
            LinkableNode<Entity> node;
            if (m_entityNodes.Length > 0)
            {
                node = m_entityNodes.Data[m_entityNodes.Length - 1];
                node.Value = entity;
                m_entityNodes.RemoveLast();
            }
            else
            {
                node = new LinkableNode<Entity> { Value = entity };
            }

            return node;
        }

        public void FreeLinkableNodeEntity(LinkableNode<Entity> node)
        {
            node.Previous = null;
            node.Next = null;
            node.Value = null;
            m_entityNodes.Add(node);
        }

        public List<BlockmapIntersect> GetBlockmapIntersectList()
        {
            if (m_blockmapLists.Length > 0)
            {
                List<BlockmapIntersect> list = m_blockmapLists.Data[m_blockmapLists.Length - 1];
                m_blockmapLists.RemoveLast();
                return list;
            }

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
                {
                    var renderWorldData = data.Data[data.Length - 1];
                    data.RemoveLast();
                    return renderWorldData;
                }

                return new RenderWorldData(capabilities, functions, texture);
            }
            else
            {
                RenderWorldData renderWorldData = new RenderWorldData(capabilities, functions, texture);
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
            {
                HashSet<Sector> set = m_sectorSet.Data[m_sectorSet.Length - 1];
                m_sectorSet.RemoveLast();
                return set;
            }

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
                AudioData audioData = m_audioData.Data[m_audioData.Length - 1];
                audioData.SoundSource = soundSource;
                audioData.SoundInfo = soundInfo;
                audioData.SoundChannelType = channel;
                audioData.Attenuation = attenuation;
                audioData.Priority = priority;
                audioData.Loop = loop;
                m_audioData.RemoveLast();
                return audioData;
            }

            return new AudioData(soundSource, soundInfo, channel, attenuation, priority, loop);
        }

        public void FreeAudioData(AudioData audioData)
        {
            audioData.SoundSource = null;
            audioData.SoundInfo = null;
            m_audioData.Add(audioData);
        }

        public SoundParams GetSoundParams(ISoundSource soundSource, bool loop = false, Attenuation attenuation = Attenuation.Default, 
            float volume = SoundParams.MaxVolume)
        {
            if (m_soundParams.Length > 0)
            {
                SoundParams soundParams = m_soundParams[m_soundParams.Length - 1];
                soundParams.SoundSource = soundSource;
                soundParams.Loop = loop;
                soundParams.Attenuation = attenuation;
                soundParams.Volume = volume;
                m_soundParams.RemoveLast();
                return soundParams;
            }

            return new SoundParams(soundSource, loop, attenuation, volume);
        }

        public void FreeSoundParams(SoundParams soundParams)
        {
            soundParams.SoundSource = null;
            soundParams.SoundInfo = null;
            m_soundParams.Add(soundParams);
        }

        public OpenALAudioSource GetAudioSource(OpenALAudioSourceManager owner, OpenALBuffer buffer, AudioData audioData, SoundParams soundParams)
        {
            if (m_audioSources.Length > 0)
            {
                OpenALAudioSource audioSource = (OpenALAudioSource)m_audioSources[m_audioSources.Length - 1];
                audioSource.Set(owner, buffer, audioData, soundParams);
                m_audioSources.RemoveLast();
                return audioSource;
            }

            return new OpenALAudioSource(owner, buffer, audioData, soundParams);
        }

        public void FreeAudioSource(IAudioSource audioSource)
        {
            audioSource.AudioData.SoundSource.ClearSound(audioSource, audioSource.AudioData.SoundChannelType);
            audioSource.CacheFree();
            audioSource.AudioData = null;
            m_audioSources.Add(audioSource);
        }
    }
}

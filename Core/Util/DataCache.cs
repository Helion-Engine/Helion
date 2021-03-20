using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics.Blockmap;
using System.Collections.Generic;

namespace Helion.Util
{
    public class DataCache
    {
        public static DataCache Instance { get; } = new DataCache();

        private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new DynamicArray<LinkableNode<Entity>>(1024);
        private readonly DynamicArray<List<BlockmapIntersect>> m_blockmapLists = new DynamicArray<List<BlockmapIntersect>>();
        private readonly DynamicArray<HashSet<Sector>> m_sectorSet = new DynamicArray<HashSet<Sector>>();
        private readonly Dictionary<GLLegacyTexture, DynamicArray<RenderWorldData>> m_alphaRender = new Dictionary<GLLegacyTexture, DynamicArray<RenderWorldData>>();
        
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
    }
}

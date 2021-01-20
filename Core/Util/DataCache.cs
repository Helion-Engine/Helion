using Helion.Util.Container;
using Helion.Util.Container.Linkable;
using Helion.World.Entities;

namespace Helion.Util
{
    public class DataCache
    {
        public static DataCache Instance { get; } = new DataCache();

        private readonly DynamicArray<LinkableNode<Entity>> m_entityNodes = new DynamicArray<LinkableNode<Entity>>(1024);

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
    }
}

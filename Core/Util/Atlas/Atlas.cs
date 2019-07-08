using System;
using System.Collections.Generic;
using Helion.Util.Geometry;

namespace Helion.Util.Atlas
{
    public class Atlas
    {
        public Dimension Dimension;
        private readonly AtlasNode m_root;
        private readonly HashSet<AtlasHandle> m_handles = new HashSet<AtlasHandle>();

        public Atlas(Dimension dimension)
        {
            Dimension = dimension;
            m_root = new AtlasNode(dimension);
        }

        public AtlasHandle? Add(Dimension dimension)
        {
            if (dimension.Width == 0 || dimension.Height == 0)
                return null;

            AtlasNode? node = m_root.RecursivelyAdd(dimension);
            if (node == null)
                return null;

            AtlasHandle handle = new AtlasHandle(node);
            m_handles.Add(handle);
            return handle;
        }

        public void Remove(AtlasHandle handle)
        {
            if (!m_handles.Contains(handle)) 
                return;
            
            m_handles.Remove(handle);
            
            // TODO: We didn't implement this yet.
            throw new NotImplementedException("Still need to implement removing from the atlas");
        }
    }
}
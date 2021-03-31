using System;
using System.Collections.Generic;
using Helion.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Atlas
{
    /// <summary>
    /// A two dimensional atlas that tracks consumed space.
    /// </summary>
    public class Atlas2D
    {
        /// <summary>
        /// The dimensions of the atlas.
        /// </summary>
        public Dimension Dimension;

        private readonly int m_maxSize;
        private readonly HashSet<AtlasHandle> m_handles = new HashSet<AtlasHandle>();
        private AtlasNode m_root;

        /// <summary>
        /// Creates a new atlas.
        /// </summary>
        /// <param name="initialDimension">The starting dimension.</param>
        /// <param name="maxSize">The max size beyond which we will not expand.
        /// This is optional, and if null it will be set to the smallest side
        /// of the initial dimension. If provided, it should also be positive.
        /// </param>
        public Atlas2D(Dimension initialDimension, int? maxSize = null)
        {
            Precondition(initialDimension.Width > 0 && initialDimension.Height > 0, "Bad atlas dimensions");
            
            if (maxSize == null)
                maxSize = Math.Min(initialDimension.Width, initialDimension.Height);
                
            Dimension = initialDimension;
            m_maxSize = maxSize.Value;
            m_root = new AtlasNode(initialDimension);
        }

        /// <summary>
        /// Consumes a space in the atlas, or returns null if it cannot or if
        /// the dimension is malformed. This will resize the tree if there is
        /// not enough space, but enough space to resize and accomodate it.
        /// </summary>
        /// <param name="dimension">The size to consume.</param>
        /// <returns>A handle to the newly consumed size with the information,
        /// or null if the area could not be allocated to the caller.</returns>
        public AtlasHandle? Add(Dimension dimension)
        {
            if (dimension.Width == 0 || dimension.Height == 0 || TooLarge(dimension))
                return null;

            AtlasNode? node = TryAdd(dimension);
            if (node == null)
                return null;

            AtlasHandle handle = new AtlasHandle(node);
            m_handles.Add(handle);
            return handle;
        }

        /// <summary>
        /// Deletes the handle for some tree, freeing up space so that other
        /// calls can use it.
        /// </summary>
        /// <param name="handle">The handle to free, which must be owned by
        /// this atlas.</param>
        public void Remove(AtlasHandle handle)
        {
            Precondition(m_handles.Contains(handle), "Trying to remove an atlas handle this atlas doesn't own");

            handle.Node.Delete();
            m_handles.Remove(handle);
        }

        private bool TooLarge(Dimension dimension)
        {
            return dimension.Width > m_maxSize || dimension.Height > m_maxSize;
        }

        private AtlasNode? TryAdd(Dimension dimension)
        {
            AtlasNode? node = m_root.RecursivelyAdd(dimension);
            if (node != null)
                return node;

            Dimension nextDimension = FindSmallestAccomodatingDimension(dimension);
            if (TooLarge(nextDimension))
                return null;

            ResizeAtlas(nextDimension);
            node = m_root.RecursivelyAdd(dimension);
            
            Postcondition(node != null, "Should not fail when we made sure enough space exists to add to atlas");
            return node;
        }

        private Dimension FindSmallestAccomodatingDimension(Dimension dimension)
        {
            Precondition(dimension.Width > 0 || dimension.Height > 0, "Dimension for atlas cannot be negative or zero");
            Precondition(dimension.Width < int.MaxValue / 2, "Width too large for atlas component, risking integer overflow");
            Precondition(dimension.Height < int.MaxValue / 2, "Height too large for atlas component, risking integer overflow");
            
            // We either want to pick the doubled size of the current dimension
            // (in case we're having trouble adding something small) or the
            // dimension from something that is larger than the current atlas.
            int desiredWidth = Math.Max(Dimension.Width * 2, dimension.Width);
            int desiredHeight = Math.Max(Dimension.Height * 2, dimension.Height);
            
            int width = FindNextLargestOrEqual(Dimension.Width, desiredWidth);
            int height = FindNextLargestOrEqual(Dimension.Height, desiredHeight);
            return new Dimension(width, height);

            int FindNextLargestOrEqual(int current, int target)
            {
                while (current < target)
                    current *= 2;
                return current;
            } 
        }

        private void ResizeAtlas(Dimension newDimension)
        {
            Dimension prevDimension = Dimension;
            AtlasNode prevRoot = m_root;

            Dimension = newDimension;
            m_root = new AtlasNode(newDimension);
            
            AtlasNode? node = m_root.RecursivelyAdd(prevDimension);
            Invariant(node != null, "Should never fail to add previous atlas when we make the atlas larger");

            node?.EmplaceExistingTree(prevRoot);
        }
    }
}
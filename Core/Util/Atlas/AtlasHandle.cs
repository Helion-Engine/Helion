using Helion.Util.Geometry;

namespace Helion.Util.Atlas
{
    /// <summary>
    /// A handle to a location in the tree.
    /// </summary>
    public class AtlasHandle
    {
        /// <summary>
        /// The node that this wraps around.
        /// </summary>
        internal AtlasNode Node;

        /// <summary>
        /// The location on the atlas.
        /// </summary>
        public Box2I Location => Node.Location;

        /// <summary>
        /// Creates a new handle from an existing node.
        /// </summary>
        /// <param name="node">The node that was allocated.</param>
        internal AtlasHandle(AtlasNode node)
        {
            Node = node;
        }
    }
}
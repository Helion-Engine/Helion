using System;
using Helion.Util.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Texture
{
    /// <summary>
    /// A single patch that makes up a Texture1/2/3 data structure.
    /// </summary>
    public class TextureXPatch
    {
        /// <summary>
        /// The index into the Pnames data for this texture.
        /// </summary>
        public short PnamesIndex { get; }

        /// <summary>
        /// The X/Y offset of this texture.
        /// </summary>
        public Vec2I Offset { get; }

        public TextureXPatch(short pnamesIndex, Vec2I offset)
        {
            Precondition(pnamesIndex >= 0, "Texture X patch has a negative pnames index");

            PnamesIndex = Math.Max((short)0, pnamesIndex);
            Offset = offset;
        }
    }
}
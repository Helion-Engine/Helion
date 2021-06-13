using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Util.Extensions;

namespace Helion.Render.Common
{
    /// <summary>
    /// Information for a virtual resolution when rendering.
    /// </summary>
    public readonly struct VirtualResolutionInfo
    {
        public readonly Dimension Dimension;
        public readonly ResolutionScale Scale;
        private readonly Vec2F m_scaling;
        private readonly Vec2F m_gutter;

        public VirtualResolutionInfo(Dimension dimension, ResolutionScale scale, Dimension parentDimension)
        {
            Vec2F parent = parentDimension.Vector.Float;

            Dimension = dimension;
            Scale = scale;
            m_scaling = parent / dimension.Vector.Float;
            m_gutter = Vec2F.Zero;

            float minScale = m_scaling.X.Min(m_scaling.Y);
            switch (scale)
            {
            case ResolutionScale.None:
                m_scaling = (minScale, minScale);
                break;
            case ResolutionScale.Center:
                m_scaling = (minScale, minScale);
                m_gutter = (parent - (m_scaling * dimension.Vector.Float)) / 2;
                break;
            case ResolutionScale.Stretch:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scale), scale, null);
            }
        }

        /// <summary>
        /// Translates a point to a location based on this virtual dimension
        /// with respect to a parent dimension.
        /// </summary>
        /// <param name="point">The point to translate.</param>
        /// <param name="window">The alignment to the window.</param>
        /// <returns>The translated point.</returns>
        public Vec2I Translate(Vec2I point, Align window)
        {
            Vec2F alignedPos = window.Translate(point, Dimension).Float;
            return ((alignedPos * m_scaling) + m_gutter).Int;
        }
    }
}

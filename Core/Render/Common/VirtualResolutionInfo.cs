using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;

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

        public VirtualResolutionInfo(Dimension dimension, ResolutionScale scale, Dimension parentDimension,
            float? aspectRatioOverride = null)
        {
            Vec2F parent = parentDimension.Vector.Float;
            
            Dimension = ((int)(dimension.Width * (aspectRatioOverride ?? 1.0f)), dimension.Height);
            Scale = scale;
            m_scaling = parent / Dimension.Vector.Float;
            m_gutter = Vec2F.Zero;

            int scaledX = (int)(Dimension.Width * m_scaling.Y);
            m_gutter = (Math.Max(0, parent.X - scaledX), 0);
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

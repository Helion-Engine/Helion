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
        public readonly ResolutionScale ResolutionScale;
        public readonly Vec2F Scale;
        public readonly Vec2F Gutter;

        public VirtualResolutionInfo(Dimension dimension, ResolutionScale resolutionScale, Dimension parentDimension,
            float? aspectRatioOverride = null)
        {
            Vec2F parent = parentDimension.Vector.Float;
            
            Dimension = ((int)(dimension.Width * (aspectRatioOverride ?? 1.0f)), dimension.Height);
            ResolutionScale = resolutionScale;
            Scale = parent / Dimension.Vector.Float;
            Gutter = Vec2F.Zero;

            int scaledX = (int)(Dimension.Width * Scale.Y);
            Gutter = (Math.Max(0, parent.X - scaledX), 0);
        }

        /// <summary>
        /// Translates the box from it's local position into its view position.
        /// The result of this function will be the correct area that it should
        /// take up in the absolute coordinates of the virtual space.
        /// </summary>
        /// <remarks>To map this into the parent space, use the function
        /// <see cref="VirtualToParent"/>.</remarks>
        /// <param name="box">The box in the virtual space to translate relative
        /// to the given alignment parameters.</param>
        /// <param name="window">The window alignment.</param>
        /// <param name="anchor">The anchor alignment.</param>
        /// <returns></returns>
        public HudBox VirtualTranslate(HudBox box, Align window, Align anchor)
        {
            Vec2I windowAnchor = window.Translate(Dimension);
            Vec2I originDelta = anchor.AnchorDelta(box.Dimension);
            Vec2I topLeft = windowAnchor + originDelta + box.TopLeft;
            return (topLeft, topLeft + box.Dimension);
        }

        /// <summary>
        /// Takes a box in the virtual space of this resolution, and transforms
        /// it into the parent space. This is the second step, where the first
        /// is <see cref="VirtualTranslate"/>.
        /// </summary>
        /// <remarks>The result from this can be used with the provided viewport
        /// that one is translating to.</remarks>
        /// <param name="virtualBox">The box to transform into the parent space.
        /// </param>
        /// <returns>The result that can be used.</returns>
        public HudBox VirtualToParent(HudBox virtualBox)
        {
            Vec2I topLeft = ((virtualBox.TopLeft.Float * Scale) + Gutter).Int;
            Vec2I dimension = (virtualBox.Sides.Float * Scale).Int;
            return (topLeft, topLeft + dimension);
        }
    }
}

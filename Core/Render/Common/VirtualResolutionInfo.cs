using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;

namespace Helion.Render.Common;

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
        Dimension = dimension;
        ResolutionScale = resolutionScale;

        // The aspect ratio override is used to stretch the size of the parent
        // viewport along its X axis, to which we use for the rest of the
        // calculations. Otherwise if it's not set, then using the dimension's
        // aspect ratio changes nothing.
        float aspectRatio = (aspectRatioOverride ?? dimension.AspectRatio);
        float viewWidth = parentDimension.Height * aspectRatio;
        float scaleWidth = viewWidth / dimension.Width;
        float scaleHeight = parentDimension.Height / (float)dimension.Height;
        Scale = (scaleWidth, scaleHeight);
        Gutter = (0, 0);

        // By default we're stretching, but if we're centering, our values
        // have to change to accomodate a gutter if the aspect ratios are
        // different.
        if (resolutionScale == ResolutionScale.Center && parentDimension.AspectRatio > aspectRatio)
        {
            // We only want to do centering if we will end up with gutters
            // on the side. This can only happen if the virtual dimension
            // has a smaller aspect ratio. We have to exit out if not since
            // it will cause weird overdrawing otherwise.
            Gutter.X = (parentDimension.Width - (int)(dimension.Width * Scale.X)) / 2.0f;
        }
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


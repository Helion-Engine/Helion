using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;

namespace Helion.Render.Common;

/// <summary>
/// Cached information that lets a HUD renderer apply transformations for
/// an image based on offsets for a virtual dimension.
/// </summary>
public readonly struct RenderDimensions
{
    public readonly int Width;
    public readonly int Height;
    public readonly ResolutionScale ScaleType;
    private readonly Vec2D m_inverseScale;

    public RenderDimensions(Dimension dimension, ResolutionScale scale = ResolutionScale.None) :
        this(dimension.Width, dimension.Height, scale)
    {
    }

    public RenderDimensions(int width, int height, ResolutionScale scale = ResolutionScale.None)
    {
        Width = width;
        Height = height;
        ScaleType = scale;
        m_inverseScale = (1.0 / width, 1.0 / height);
    }

    public static implicit operator Dimension(RenderDimensions dim) => (dim.Width, dim.Height);

    /// <summary>
    /// Translates Doom-specific HUD offsets.
    /// </summary>
    /// <param name="offset">The offset of the image.</param>
    /// <returns>The translated point.</returns>
    public static Vec2I TranslateDoomOffset(Vec2I offset) => (-offset.X, -offset.Y);

    public Vec2I Translate(Vec2I point, Dimension parentViewport)
    {
        Vec2D gutter = Vec2D.Zero;
        Vec2D scale = parentViewport.Vector.Double * m_inverseScale;

        if (ScaleType == ResolutionScale.Stretch)
            return (point.Double * scale).Int;

        // If the scaling is not equal, then we have a gutter to worry about.
        //
        // The idea is as follows (assuming the virtual is smaller):
        //
        // Place your resolutions both at the top left corner, and grab the
        // bottom right corner of the virtual resolution and stretch it by
        // the same scaling in both directions until it touches an edge.
        // It will either touch the bottom, or the right, or both. If it
        // does not touch the bottom right, then the aspect ratio's mismatch.
        // The solution then is to take the smaller scale, and use that for
        // both axes.
        //
        // What happens is that it works fine for one axis, but not the other
        // axis. There's extra space, which we call the 'gutter'. This has to
        // be handled by finding out how wide or tall it is, and adding it if
        // needed. If we're centering, then the solution is to add half of the
        // gutter to the result.
        //
        // Therefore, our gutter formula is:
        //      gutter = fullDimension - (dimension * smallerScale)
        if (scale.X > scale.Y)
        {
            if (ScaleType == ResolutionScale.Center)
                gutter.X = (parentViewport.Width - (Width * scale.Y)) / 2;
            scale.X = scale.Y;
        }
        else if (scale.X < scale.Y)
        {
            if (ScaleType == ResolutionScale.Center)
                gutter.Y = (parentViewport.Height - (Height * scale.X)) / 2;
            scale.Y = scale.X;
        }

        Vec2D scaledPoint = point.Double * scale;
        return (scaledPoint + gutter).Int;
    }
}


using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;

namespace Helion.Render.Common.Enums;

/// <summary>
/// An alignment position to anchor from.
/// </summary>
/// <remarks>
/// The first word is the Y position, second word is the X position.
///
///                     (TopMiddle)
/// (TopLeft)    o-----------o-----------o (TopRight)
///              |                       |
///              |        (Center)       |
/// (MiddleLeft) o           o           o (MiddleRight)
///              |                       |
///              |                       |
/// (BottomLeft) o-----------o-----------o (BottomRight)
///                   (BottomMiddle)
/// </remarks>
public enum Align
{
    TopLeft,
    TopMiddle,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomMiddle,
    BottomRight
}

public static class AlignHelper
{
    /// <summary>
    /// This takes offsets, image dimensions, alignment values, and it
    /// calculates the position on the virtual window where it should be
    /// drawn. See the remarks section for all of the logic.
    /// </summary>
    /// <remarks>
    /// The X/Y offsets are always relative to the origin's direction. This
    /// means if they're negative, it moves up/left on the screen, and also
    /// down/right if positive. These are applied after the image is placed
    /// at its location based on alignment.
    ///
    /// The alignment works as follows. It selects the point on the image
    /// to which the point corresponds to. The image below demonstrates
    /// this (these are also straightforward, but here for completeness'
    /// sake):
    ///                     (TopMiddle)
    /// (TopLeft)    o-----------o-----------o (TopRight)
    ///              |                       |
    ///              |        (Center)       |
    /// (MiddleLeft) o           o           o (MiddleRight)
    ///              |                       |
    ///              |                       |
    /// (BottomLeft) o-----------o-----------o (BottomRight)
    ///                   (BottomMiddle)
    ///
    /// The window alignment finds the point above on the screen. Then the
    /// same thing is done for the image, and then the image 'pivot' is
    /// placed on top of the window pivot.
    ///
    /// For example, suppose we had a window alignment of "Center", and an
    /// image alignment of "BottomMiddle". The image alignment says that
    /// the pivot point is at the bottom middle of the image. Then we place
    /// this pivot point onto the window point. It would draw like so, and
    /// both pivot points are represented by an 'o' (which there's only one
    /// since we place the pivots on top of each other), and the image to be
    /// drawn is represented by dots:
    ///
    ///              +-----------------------+
    ///              |                       |
    ///              |         .....         |
    ///              |         .....         |   This is the entire screen.
    ///              |         ..o..         |
    ///              |                       |   The pivot point in the image
    ///              |                       |   is always placed on top of
    ///              |                       |   the window one.
    ///              +-----------------------+
    /// </remarks>
    /// <param name="align">The offsets (positive is left, and down).</param>
    /// <param name="dimension">The dimensions to align to.</param>
    /// <returns>The delta that the point should be moved based on the
    /// provided dimension.</returns>
    public static Vec2I Translate(this Align align, Dimension dimension)
    {
        (int w, int h) = dimension;

        Vec2I anchor = align switch
        {
            Align.TopLeft => (0, 0),
            Align.TopMiddle => (w / 2, 0),
            Align.TopRight => (w, 0),
            Align.MiddleLeft => (0, h / 2),
            Align.Center => (w / 2, h / 2),
            Align.MiddleRight => (w, h / 2),
            Align.BottomLeft => (0, h),
            Align.BottomMiddle => (w / 2, h),
            Align.BottomRight => (w, h),
            _ => throw new Exception($"Unsupported alignment: {align}")
        };

        return anchor;
    }

    public static Vec2I AnchorDelta(this Align align, Dimension dimension)
    {
        return -Translate(align, dimension);
    }
}


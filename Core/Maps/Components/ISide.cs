using Helion.Geometry.Vectors;
using Helion.Maps.Specials;

namespace Helion.Maps.Components;

/// <summary>
/// A side of a line.
/// </summary>
public interface ISide
{
    /// <summary>
    /// A unique ID for the side.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// The texture offsets.
    /// </summary>
    Vec2F UpperOffset { get; }
    Vec2F MiddleOffset { get; }
    Vec2F BottomOffset { get; }

    Vec2F UpperScale { get; }
    Vec2F MiddleScale { get; }
    Vec2F BottomScale { get; }

    /// <summary>
    /// The upper texture name.
    /// </summary>
    string UpperTexture { get; }

    /// <summary>
    /// The middle texture name.
    /// </summary>
    string MiddleTexture { get; }

    /// <summary>
    /// The lower texture name.
    /// </summary>
    string LowerTexture { get; }

    /// <summary>
    /// Gets the sector this side references.
    /// </summary>
    /// <returns>The sector this side references.</returns>
    ISector GetSector();
}

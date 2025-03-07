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
    ref Vec2I Offset { get; }
    ref Vec2F UpperOffset { get; }
    ref Vec2F MiddleOffset { get; }
    ref Vec2F BottomOffset { get; }

    ref Vec2F UpperScale { get; }
    ref Vec2F MiddleScale { get; }
    ref Vec2F BottomScale { get; }

    int LightLevel { get; }
    int LightLevelUpper { get; }
    int LightLevelMiddle { get; }
    int LightLevelLower { get; }

    bool LightLevelAbsolute { get; }
    bool LightLevelUpperAbsolute { get; }
    bool LightLevelMiddleAbsolute { get; }
    bool LightLevelLowerAbsolute { get; }

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

using Helion.Graphics;
using Helion.Graphics.Geometry;

namespace Helion.Render.OpenGL.Texture.Fonts;

/// <summary>
/// A glyph to be rendered.
/// </summary>
public readonly struct RenderableGlyph
{
    /// <summary>
    /// The character.
    /// </summary>
    public readonly char Character;

    /// <summary>
    /// The integer-based original placement in the original draw area.
    /// This is not to be used in rendering, but is there for areas to
    /// be calculated from. These will be out of sync after transformation.
    /// Finally, note that this has the origin at the top left, meaning
    /// the Left/Right/Top/Bottom fields will be wrong.
    /// </summary>
    public readonly ImageBox2I Coordinates;

    // The coordinates used to determine the render area of the box for alignment.
    // Required to handle weirdness of how Doom rendered the LargeHudFont being a fixed width to the size of the 0 char.
    public readonly ImageBox2I AreaCoordinates;

    /// <summary>
    /// The location in the font's atlas as normalized coordinates. This is
    /// normalized since renderers will need to scale it based on the size
    /// that was scaled. This prevents the renderer from having to go into
    /// each character and manually set it. This has its origin at the top
    /// left of the character.
    /// </summary>
    public readonly ImageBox2D Location;

    /// <summary>
    /// The UV coordinates in the font's atlas.
    /// </summary>
    public readonly ImageBox2D UV;

    /// <summary>
    /// The color of the letter.
    /// </summary>
    public readonly Color Color;

    public RenderableGlyph(char character, ImageBox2I areaCoordinates, ImageBox2I coordinates, ImageBox2D location, ImageBox2D uv, Color color)
    {
        Character = character;
        AreaCoordinates = areaCoordinates;
        Coordinates = coordinates;
        Location = location;
        UV = uv;
        Color = color;
    }

    public RenderableGlyph(RenderableGlyph parent, ImageBox2D newLocation)
    {
        Character = parent.Character;
        Coordinates = parent.Coordinates;
        AreaCoordinates = parent.AreaCoordinates;
        Location = newLocation;
        UV = parent.UV;
        Color = parent.Color;
    }
}

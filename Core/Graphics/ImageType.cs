namespace Helion.Graphics;

public enum ImageType
{
    Palette,
    Argb,
    // Allows for side by side storage of both argb and palette indices
    PaletteWithArgb,
    // Currently only used for screenshots to skip transformation of rgba -> argb
    Rgba
}

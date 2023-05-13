namespace Helion.Geometry;

/// <summary>
/// A stand-in for System.Drawing.Rectangle.
/// </summary>
public record struct Rectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;
}

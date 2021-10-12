namespace Helion.Resources.Definitions.Animdefs.Textures;

public class AnimatedCameraTexture
{
    public readonly string Name;
    public readonly int Width;
    public readonly int Height;
    public readonly int? FitWidth;
    public readonly int? FitHeight;
    public readonly bool WorldPanning;
    public AnimatedCameraTexture(string name, int width, int height, int? fitWidth, int? fitHeight, bool worldPanning)
    {
        Name = name;
        Width = width;
        Height = height;
        FitWidth = fitWidth;
        FitHeight = fitHeight;
        WorldPanning = worldPanning;
    }
}


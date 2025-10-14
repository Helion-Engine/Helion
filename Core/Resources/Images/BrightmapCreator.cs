using Helion.Graphics;
namespace Helion.Resources.Images;

public static class BrightmapCreator
{
    public static Image Create(Image image, bool[] fullBrightLookup)
    {
        var brightmapImage = new Image(image.Dimension, ImageType.Argb);
        var transparent = Color.Transparent.Uint;
        var white = Color.White.Uint;

        var indices = image.Indices;
        var brightmapPixels = brightmapImage.Pixels;
        for (int i = 0; i < indices.Length; i++)
        {
            var index = indices[i];
            if (index < fullBrightLookup.Length && fullBrightLookup[index])
                brightmapPixels[i] = white;
            else
                brightmapPixels[i] = transparent;
        }

        return brightmapImage;
    }
}

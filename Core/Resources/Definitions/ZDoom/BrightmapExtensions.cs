using Helion.Graphics;
using Helion.Resources.Definitions.Zdoom;
using Helion.Resources.Images;

namespace Helion.Resources.Definitions.ZDoom;

public static class BrightmapExtensions
{
    public static Image? GetImage(this BrightmapDefinition brightmap, IImageRetriever imageRetriever)
    {
        if (brightmap.BrightmapName == null)
            return null;

        if (brightmap.IsFullPath)
            return imageRetriever.GetByFullPath(brightmap.BrightmapName, ResourceNamespace.Brightmaps);

        return imageRetriever.GetOnly(brightmap.BrightmapName, ResourceNamespace.Brightmaps);
    }
}

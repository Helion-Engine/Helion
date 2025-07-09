using Helion.Resources.Archives.Locator;

namespace Helion.Resources.Archives.Collection;

public enum LoadArchiveOptions
{
    Default = 0,
    CalculateMd5 = 1,
    IgnoreLoadEvent = 2,
    // If this file is bundled with Helion like assets.pk3. (i.e. we should restrict the search paths)
    IsBundled = 4,
    IgnoreError = 8
}

public static class LoadArchiveOptionsExtensions
{
    public static ArchiveLocatorOptions ToArchiveLocatorOptions(this LoadArchiveOptions options)
    {
        var locatorOptions = ArchiveLocatorOptions.Default;
        if ((options & LoadArchiveOptions.IsBundled) != 0)
            locatorOptions |= ArchiveLocatorOptions.IsBundled;
        if ((options & LoadArchiveOptions.IgnoreError) != 0)
            locatorOptions |= ArchiveLocatorOptions.IgnoreError;

        return locatorOptions;
    }
}
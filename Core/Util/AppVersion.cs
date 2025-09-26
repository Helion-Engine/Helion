using System.Reflection;

namespace Helion.Util;

public class AppVersion
{
    public static readonly AppVersion Current = new();

    public AppVersion()
    {
        VersionString = GetAppVersionString();
    }

    private static string GetAppVersionString()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        if (assemblyName.Version == null)
            return string.Empty;

        return assemblyName.Version.ToString();
    }

    public readonly string VersionString;
}

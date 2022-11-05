using System.Reflection;

namespace Helion.Client;

public class AppInfo
{
    public AppInfo()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName();
        if (assemblyName.Name != null)
            ApplicationName = assemblyName.Name;
        if (assemblyName.Version != null)
            ApplicationVersion = assemblyName.Version.ToString();
    }

    public string ApplicationName { get; set; } = "Helion";
    public string ApplicationVersion { get; set; } = "Version Unknown";
}

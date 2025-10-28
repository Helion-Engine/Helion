using System;
using System.Reflection;

namespace Helion.Client;

public class AppInfo
{
    public AppInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName();
        if (assemblyName.Name != null)
            ApplicationName = assemblyName.Name;
        if (assemblyName.Version != null)
            ApplicationVersion = assemblyName.Version.ToString();

        ApplicationDirectory = AppContext.BaseDirectory;
        ApplicationExe = AppDomain.CurrentDomain.FriendlyName + ".exe";
    }

    public string ApplicationName { get; set; } = "Helion";
    public string ApplicationVersion { get; set; } = "Version Unknown";
    public string ApplicationDirectory { get; set; }
    public string ApplicationExe { get; set; }
}
using System.Collections.Generic;

namespace Helion.Util;

public interface IAppState { }

public class EnvironmentAppState : IAppState
{
    public HashSet<string> InstallPaths { get; set; } = [];
    public bool ShouldSetGpuPreference { get; set; } = true;
}

public class InstallAppState : IAppState
{
    public string LastRunVersion { get; set; } = string.Empty;
}

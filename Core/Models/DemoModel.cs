using Helion.Demo;
using System;
using System.Collections.Generic;

namespace Helion.Models;

public enum DemoVersion
{
    Alpha = 1,
}

public class DemoMap
{
    public string Map { get; set; } = string.Empty;
    public int CommandIndex { get; set; }
    // This is the random index before the map has loaded
    public int RandomIndex { get; set; }
    public PlayerModel? PlayerModel { get; set; }
}

public class DemoModel
{
    public string AppVersion { get; set; } = string.Empty;
    public DemoVersion Version { get; set; }
    public GameFilesModel GameFiles { get; set; }
    public IList<DemoMap> Maps { get; set; } = Array.Empty<DemoMap>();
    public IList<DemoCheat> Cheats { get; set; } = Array.Empty<DemoCheat>();
    public IList<ConfigValueModel> ConfigValues { get; set; } = Array.Empty<ConfigValueModel>();
}

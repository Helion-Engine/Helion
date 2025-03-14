using Helion.Graphics;
using Helion.Maps.Specials.ZDoom;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Locks;

public class LockDef
{
    public ZDoomKeyType KeyNumber { get; set; }
    public string DoorMessage { get; set; } = string.Empty;
    public string ObjectMessage { get; set; } = string.Empty;
    public string RemoteMessage { get; set; } = string.Empty;
    public Color MapColor { get; set; }
    public List<string> KeyDefinitionNames { get; set; } = [];
    public List<List<string>> AnyKeyDefinitionNames { get; set; } = [];
}

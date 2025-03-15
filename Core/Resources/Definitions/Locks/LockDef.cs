using Helion.Graphics;
using Helion.Maps.Specials.ZDoom;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.Locks;

public class LockDef
{
    private Color? m_keyImageColor;

    public ZDoomKeyType KeyNumber { get; set; }
    public string DoorMessage { get; set; } = string.Empty;
    public string ObjectMessage { get; set; } = string.Empty;
    public string RemoteMessage { get; set; } = string.Empty;
    public Color MapColor { get; set; }
    public Color KeyImageColor
    {
        get
        {
            if (m_keyImageColor.HasValue)
                return m_keyImageColor.Value;
            return MapColor;
        }
        set
        {
            m_keyImageColor = value;
        }
    }
    public List<string> KeyDefinitionNames { get; set; } = [];
    public List<List<string>> AnyKeyDefinitionNames { get; set; } = [];
}

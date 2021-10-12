using System.Drawing;

namespace Helion.Resources.Definitions.Decorate.Properties;

public class PlayerDamageScreenProperty
{
    public readonly Color Color;
    public readonly double? Intensity;
    public readonly string? DamageType;

    public PlayerDamageScreenProperty(Color color, double? intensity, string? damageType)
    {
        Color = color;
        Intensity = intensity;
        DamageType = damageType;
    }
}


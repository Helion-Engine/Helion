using Helion.Graphics;

namespace Helion.Models;

public class PowerupModel
{
    public string Name { get; set; } = string.Empty;
    public int PowerupType { get; set; }
    public ColorModel? DrawColor { get; set; }
    public float DrawAlpha { get; set; }
    public bool DrawPowerupEffect { get; set; } = true;
    public bool DrawEffectActive { get; set; } = true;
    public int EffectType { get; set; }
    public int Tics { get; set; }
    public int EffectTics { get; set; }
    public float SubAlpha { get; set; }
}

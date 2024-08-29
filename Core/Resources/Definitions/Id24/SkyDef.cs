using Helion.Geometry.Vectors;

namespace Helion.Resources.Definitions.Id24;

public class SkyFire
{
    public int[] Palette { get; set; } = [];
    public double UpdateTime { get; set; }
}

public class SkyForeTex
{
    public string Name { get; set; } = string.Empty;
    public double Mid { get; set; }
    public double ScrollX { get; set; }
    public double ScrollY { get; set; }
    public double ScaleX { get; set; }
    public double ScaleY { get; set; }
}

public enum SkyType
{
    Normal,
    Fire,
    WithForeground
}

public class SkyDef
{
    public SkyType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Mid { get; set; }
    public double ScrollX { get; set; }
    public double ScrollY { get; set; }
    public double ScaleX { get; set; }
    public double ScaleY { get; set; }
    public SkyFire? Fire { get; set; }
    public SkyForeTex? ForegroundTex { get; set; }

    public bool Validate(out string error)
    {
        if (Type == SkyType.Fire && Fire == null)
        {
            error = $"Sky {Name} was defined as Fire type without a fire definition.";
            return false;
        }

        if (Type == SkyType.WithForeground && ForegroundTex == null)
        {
            error = $"Sky {Name} was defined as WithForeground type without a ForegroundTex definition.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}

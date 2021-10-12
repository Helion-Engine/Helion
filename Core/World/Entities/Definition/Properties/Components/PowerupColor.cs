namespace Helion.World.Entities.Definition.Properties.Components;

public class PowerupColor
{
    public string Color;
    public double Alpha;

    public PowerupColor(string color, double alpha = 1.0)
    {
        Color = color;
        Alpha = alpha;
    }
}


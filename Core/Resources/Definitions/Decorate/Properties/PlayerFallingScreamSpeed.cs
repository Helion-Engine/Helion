using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate.Properties;

public class PlayerFallingScreamSpeed
{
    private double Min;
    private double Max;

    public PlayerFallingScreamSpeed(double min, double max)
    {
        Precondition(min <= max, "Min/max of falling scream speed is ordered wrong");

        Min = min;
        Max = max;
    }
}

